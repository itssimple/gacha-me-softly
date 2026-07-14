using System;
using System.Collections.Generic;
using Chris.PachiRogue.Core;
using Chris.PachiRogue.Physics;
using Chris.PachiRogue.ProcGen;
using UnityEngine;

namespace Chris.PachiRogue.Run
{
    /// <summary>
    /// The run state machine (Aiming → Resolving → StageClear → Interlude →
    /// Drafting → Advancing → … → RunEnd), driven entirely by events on the
    /// bus. Owns run-scoped state, builds stages from sequential per-stage
    /// seeds forked off the run seed, applies drafted upgrades (mutators +
    /// stat deltas), persists mid-run resume snapshots, and awards
    /// meta-currency on run end. Replaces the Phase-1 DemoRunDriver.
    /// </summary>
    public sealed class RunDirector : MonoBehaviour, IServiceConsumer
    {
        [SerializeField] private List<ChunkSO> chunks = new List<ChunkSO>();
        [SerializeField] private List<StageRecipeSO> recipes = new List<StageRecipeSO>(); // index = act
        [SerializeField] private List<UpgradeSO> upgradePool = new List<UpgradeSO>();
        [SerializeField] private List<PassiveAbilitySO> abilities = new List<PassiveAbilitySO>();
        [SerializeField] private Camera sceneCamera;

        private ISaveService _saveService;
        private IEventBus _bus;
        private IRngService _rng;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        private readonly Dictionary<string, ChunkSO> _chunkAssets = new Dictionary<string, ChunkSO>();
        private readonly List<ChunkData> _chunkData = new List<ChunkData>();
        private readonly Dictionary<string, UpgradeSO> _upgradeAssets = new Dictionary<string, UpgradeSO>();
        private List<AbilityData> _abilityData;

        private PhysicsBody _body;
        private LauncherController _launcher;
        private StageBuilder _builder;
        private CameraFollow _cameraFollow;

        private RunPhase _phase = RunPhase.Idle;
        private bool _runActive;
        private ulong _runSeed;
        private int _stage;
        private RecipeData _recipe;
        private long _gate;
        private long _stageScore;
        private int _launches;
        private int _hp;
        private int _maxHp;
        private readonly List<string> _ownedUpgradeIds = new List<string>();
        private List<UpgradeData> _currentChoices;

        private long _launchAccum;
        private float _cupMultiplier;

        public void InitServices(ServiceRegistry services)
        {
            _saveService = services.Resolve<ISaveService>();
            _bus = services.Resolve<IEventBus>();
            _rng = services.Resolve<IRngService>();

            foreach (ChunkSO chunk in chunks)
            {
                ChunkData data = chunk.ToData();
                _chunkAssets[data.Id] = chunk;
                _chunkData.Add(data);
            }

            foreach (UpgradeSO upgrade in upgradePool)
            {
                _upgradeAssets[upgrade.Id] = upgrade;
            }

            _abilityData = new List<AbilityData>(abilities.Count);
            foreach (PassiveAbilitySO ability in abilities)
            {
                _abilityData.Add(ability.ToData());
            }

            _subscriptions.Add(_bus.Subscribe<StoryIntroCompleted>(_ => OnIntroCompleted()));
            _subscriptions.Add(_bus.Subscribe<InterludeCompleted>(OnInterludeCompleted));
            _subscriptions.Add(_bus.Subscribe<UpgradePickRequested>(OnUpgradePicked));
            _subscriptions.Add(_bus.Subscribe<RunRestartRequested>(_ => StartNewRun()));
            _subscriptions.Add(_bus.Subscribe<LaunchStarted>(_ => OnLaunchStarted()));
            _subscriptions.Add(_bus.Subscribe<LaunchResolved>(_ => OnLaunchResolved()));
            _subscriptions.Add(_bus.Subscribe<PegHit>(evt => _launchAccum += evt.Value));
            _subscriptions.Add(_bus.Subscribe<BumperHit>(evt => _launchAccum += evt.Value));
            _subscriptions.Add(_bus.Subscribe<CupEntered>(evt => _cupMultiplier = evt.Multiplier));
            _subscriptions.Add(_bus.Subscribe<HazardHit>(OnHazardHit));
        }

        private void OnDestroy()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }

        private void OnApplicationPause(bool paused)
        {
            // Mobile auto-save on pause (PLAN.md Phase 5).
            if (paused && _runActive)
            {
                SaveRun();
            }
        }

        private void OnIntroCompleted()
        {
            SaveModelV1 model = LoadOrNew();
            if (model.isRunActive && model.stageIndex >= 1 && model.stageIndex <= InterludePlanner.FinalStage)
            {
                ResumeRun(model);
            }
            else
            {
                StartNewRun();
            }
        }

        private void StartNewRun()
        {
            _runSeed = _rng.Fork("run").NextULong() ^ (ulong)Environment.TickCount;
            _ownedUpgradeIds.Clear();
            _stageScore = 0;
            _maxHp = (int)EffectiveStat(StatType.MaxHealth);
            _hp = _maxHp;

            _bus.Publish(new RunStarted { Seed = _runSeed });
            StartStage(1, resuming: false);
        }

        private void ResumeRun(SaveModelV1 model)
        {
            _runSeed = model.RunSeed;
            _ownedUpgradeIds.Clear();
            _ownedUpgradeIds.AddRange(model.acquiredUpgradeIds);
            _maxHp = (int)EffectiveStat(StatType.MaxHealth);
            _hp = model.hp > 0 ? Math.Min(model.hp, _maxHp) : _maxHp;
            _stageScore = model.score;

            _bus.Publish(new RunStarted { Seed = _runSeed });
            StartStage(model.stageIndex, resuming: model.launchesRemaining > 0);
            if (model.launchesRemaining > 0)
            {
                _launches = model.launchesRemaining;
                _bus.Publish(new LaunchesChanged { Remaining = _launches });
            }
        }

        private void StartStage(int stageIndex, bool resuming)
        {
            EnsureRuntimeObjects();

            _runActive = true;
            _stage = stageIndex;
            _recipe = recipes[InterludePlanner.ActForStage(stageIndex)].ToData();
            _gate = RunLogic.ScoreGate(_recipe, stageIndex);

            if (!resuming)
            {
                _launches = _recipe.LaunchesPerStage;
                _stageScore = 0;
            }

            SetPhase(RunPhase.Advancing);

            // Deterministic per-stage seed forked off the run seed (GDD §4).
            IRngService stageRng = new RngService(_runSeed).Fork($"stage{stageIndex}");
            StagePlan plan = StageAssembler.Assemble(stageRng, _recipe, _chunkData);
            _builder.Build(plan, _chunkAssets);

            PrepareBodyForStage();

            bool isBoss = RunLogic.IsBossStage(stageIndex);
            if (isBoss)
            {
                SetPhase(RunPhase.BossIntro);
            }

            _bus.Publish(new StageStarted { StageIndex = stageIndex, IsBoss = isBoss, ScoreGate = _gate });
            _bus.Publish(new ScoreChanged { StageScore = _stageScore, Gate = _gate });
            _bus.Publish(new LaunchesChanged { Remaining = _launches });
            _bus.Publish(new HealthChanged { Current = _hp, Max = _maxHp });

            SaveRun();
            BeginAiming();
        }

        private void PrepareBodyForStage()
        {
            _body.RemoveAllMutators();

            // Base material from meta stats, then run mutators stack on top.
            float bounciness = 0.45f * EffectiveStat(StatType.Bounciness) / PlayerStats.BaseBounciness;
            _body.Context.MaterialInstance.bounciness = Mathf.Clamp01(bounciness);

            var mutators = new List<PhysicsMutatorSO>();
            foreach (string upgradeId in _ownedUpgradeIds)
            {
                if (_upgradeAssets.TryGetValue(upgradeId, out UpgradeSO upgrade))
                {
                    mutators.AddRange(upgrade.Mutators);
                }
            }

            _body.ApplyMutators(mutators);
            _body.ResetToLaunchPoint(new Vector2(0f, StageBuilder.LaunchY), _builder.KillY);

            _cameraFollow.Configure(_body.transform, StageBuilder.LaunchY + 2f, _builder.BottomY - 2f);
            Vector3 cameraPosition = sceneCamera.transform.position;
            sceneCamera.transform.position = new Vector3(cameraPosition.x, StageBuilder.LaunchY, cameraPosition.z);
        }

        private void BeginAiming()
        {
            SetPhase(RunPhase.Aiming);
            _launcher.SetStatScales(
                EffectiveStat(StatType.LaunchPower) / PlayerStats.BaseLaunchPower,
                EffectiveStat(StatType.AimControl) / PlayerStats.BaseAimControl);
            _launcher.SetAimingEnabled(true);
        }

        private void OnLaunchStarted()
        {
            if (!_runActive)
            {
                return;
            }

            _launcher.SetAimingEnabled(false);
            _launches--;
            _launchAccum = 0;
            _cupMultiplier = 1f;
            _bus.Publish(new LaunchesChanged { Remaining = _launches });
            SetPhase(RunPhase.Resolving);
        }

        private void OnLaunchResolved()
        {
            if (!_runActive || _phase != RunPhase.Resolving)
            {
                return;
            }

            _stageScore += RunLogic.LaunchScore(_launchAccum, _cupMultiplier);
            _bus.Publish(new ScoreChanged { StageScore = _stageScore, Gate = _gate });

            switch (RunLogic.Evaluate(_stageScore, _gate, _launches))
            {
                case LaunchOutcome.StageClear:
                    OnStageCleared();
                    break;

                case LaunchOutcome.Defeat:
                    FinalizeRun(victory: false);
                    break;

                default:
                    _body.ResetToLaunchPoint(new Vector2(0f, StageBuilder.LaunchY), _builder.KillY);
                    BeginAiming();
                    break;
            }
        }

        private void OnStageCleared()
        {
            SetPhase(RunPhase.StageClear);

            // Advance the resume point past the cleared stage; the interlude
            // director scales this base award by the ShardGain stat.
            SaveRun(nextStageIndex: _stage + 1, launchesOverride: 0);

            int award = RunLogic.ShardAward(_stage, _launches);
            _bus.Publish(new StageCleared { StageIndex = _stage, ShardsAwarded = award });
            SetPhase(RunPhase.Interlude);

            if (_stage >= InterludePlanner.FinalStage)
            {
                FinalizeRun(victory: true);
            }
        }

        private void OnHazardHit(HazardHit evt)
        {
            if (!_runActive || _phase != RunPhase.Resolving)
            {
                return;
            }

            _hp = Math.Max(0, _hp - evt.Damage);
            _bus.Publish(new HealthChanged { Current = _hp, Max = _maxHp });

            if (_hp <= 0)
            {
                _body.ResolveNow();
                FinalizeRun(victory: false);
            }
        }

        private void OnInterludeCompleted(InterludeCompleted evt)
        {
            if (!_runActive || _phase != RunPhase.Interlude)
            {
                return;
            }

            SetPhase(RunPhase.Drafting);
            float luck = EffectiveStat(StatType.Luck);
            var pool = new List<UpgradeData>(upgradePool.Count);
            foreach (UpgradeSO upgrade in upgradePool)
            {
                pool.Add(upgrade.ToData());
            }

            _currentChoices = DraftService.PickThree(
                pool, luck, new RngService(_runSeed).Fork($"draft{_stage}"));
            _bus.Publish(new UpgradeChoicesReady { Choices = _currentChoices });
        }

        private void OnUpgradePicked(UpgradePickRequested evt)
        {
            if (_phase != RunPhase.Drafting || _currentChoices == null)
            {
                return;
            }

            UpgradeData picked = _currentChoices.Find(c => c.Id == evt.UpgradeId);
            if (picked == null)
            {
                return;
            }

            _ownedUpgradeIds.Add(picked.Id);
            _currentChoices = null;

            // Max-health deltas heal by the same amount on pickup.
            foreach (StatDelta delta in picked.StatDeltas)
            {
                if (delta.stat == StatType.MaxHealth)
                {
                    _maxHp = (int)EffectiveStat(StatType.MaxHealth);
                    _hp = Math.Min(_maxHp, _hp + (int)delta.delta);
                }
            }

            _bus.Publish(new UpgradeDrafted { UpgradeId = picked.Id });
            _bus.Publish(new HealthChanged { Current = _hp, Max = _maxHp });
            StartStage(_stage + 1, resuming: false);
        }

        private void FinalizeRun(bool victory)
        {
            _runActive = false;

            SaveModelV1 model = LoadOrNew();
            var meta = new MetaProgression(model);
            int bonus = RunLogic.RunEndBonus(victory, _stage);
            meta.AwardShards(bonus);
            model.isRunActive = false;
            model.savedAtUnixUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _saveService.Save(model);

            _launcher.SetAimingEnabled(false);
            SetPhase(RunPhase.RunEnd);
            _bus.Publish(new RunEnded { Victory = victory, StageReached = _stage, ShardBonus = bonus });
        }

        /// <summary>Run-scoped effective stat: meta passives + drafted upgrade deltas.</summary>
        private float EffectiveStat(StatType stat)
        {
            SaveModelV1 model = LoadOrNew();
            float value = PlayerStats.GetStat(stat, _abilityData, new MetaProgression(model));

            foreach (string upgradeId in _ownedUpgradeIds)
            {
                if (!_upgradeAssets.TryGetValue(upgradeId, out UpgradeSO upgrade))
                {
                    continue;
                }

                foreach (StatDelta delta in upgrade.ToData().StatDeltas)
                {
                    if (delta.stat == stat)
                    {
                        value += delta.delta;
                    }
                }
            }

            return value;
        }

        private void SetPhase(RunPhase phase)
        {
            _phase = phase;
            _bus.Publish(new RunPhaseChanged { Phase = phase });
        }

        private void SaveRun(int? nextStageIndex = null, int? launchesOverride = null)
        {
            SaveModelV1 model = LoadOrNew();
            model.isRunActive = _runActive;
            model.RunSeed = _runSeed;
            model.stageIndex = nextStageIndex ?? _stage;
            model.launchesRemaining = launchesOverride ?? _launches;
            model.hp = _hp;
            model.score = nextStageIndex.HasValue ? 0 : _stageScore;
            model.acquiredUpgradeIds = new List<string>(_ownedUpgradeIds);
            model.savedAtUnixUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _saveService.Save(model);
        }

        private SaveModelV1 LoadOrNew()
        {
            return _saveService.TryLoad(out SaveModelV1 model) ? model : new SaveModelV1();
        }

        private void EnsureRuntimeObjects()
        {
            if (_body != null)
            {
                return;
            }

            _body = PhysicsBody.CreateRuntime(_bus, new Vector2(0f, StageBuilder.LaunchY));

            var builderGo = new GameObject("Stage");
            _builder = builderGo.AddComponent<StageBuilder>();

            _launcher = LauncherController.CreateRuntime(_body, sceneCamera);

            _cameraFollow = sceneCamera.gameObject.AddComponent<CameraFollow>();

            var gyroGo = new GameObject("GyroAssist");
            gyroGo.AddComponent<GyroAssist>().Configure(_body);
        }
    }
}
