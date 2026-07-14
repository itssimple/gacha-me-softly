using System;
using System.Collections.Generic;
using Chris.PachiRogue.Core;
using UnityEngine;

namespace Chris.PachiRogue.Run
{
    /// <summary>
    /// Scene glue for the between-stages interludes. Listens for
    /// <see cref="StageCleared"/>, applies shard awards (scaled by the
    /// ShardGain stat), plans the interlude, persists new friends, and
    /// validates <see cref="UpgradeRequested"/> purchases. The UI only ever
    /// sees <see cref="InterludePlan"/> snapshots via events.
    /// </summary>
    public sealed class InterludeDirector : MonoBehaviour, IServiceConsumer
    {
        [Tooltip("All NPC content assets, in meeting-priority order.")]
        [SerializeField] private List<NpcSO> npcs = new List<NpcSO>();

        [Tooltip("All passive ability content assets.")]
        [SerializeField] private List<PassiveAbilitySO> abilities = new List<PassiveAbilitySO>();

        private ISaveService _saveService;
        private IEventBus _eventBus;
        private IRngService _rng;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        private List<NpcData> _npcData;
        private List<AbilityData> _abilityData;
        private int _currentStage;

        public void InitServices(ServiceRegistry services)
        {
            _saveService = services.Resolve<ISaveService>();
            _eventBus = services.Resolve<IEventBus>();
            _rng = services.Resolve<IRngService>();

            _npcData = new List<NpcData>(npcs.Count);
            foreach (NpcSO npc in npcs)
            {
                _npcData.Add(npc.ToData());
            }

            _abilityData = new List<AbilityData>(abilities.Count);
            foreach (PassiveAbilitySO ability in abilities)
            {
                _abilityData.Add(ability.ToData());
            }

            _subscriptions.Add(_eventBus.Subscribe<StageCleared>(OnStageCleared));
            _subscriptions.Add(_eventBus.Subscribe<UpgradeRequested>(OnUpgradeRequested));
        }

        private void OnDestroy()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }

        private void OnStageCleared(StageCleared evt)
        {
            SaveModelV1 model = LoadOrNew();
            var progression = new MetaProgression(model);

            float shardGain = PlayerStats.GetStat(StatType.ShardGain, _abilityData, progression);
            int award = Math.Max(0, (int)Math.Round(evt.ShardsAwarded * shardGain));
            progression.AwardShards(award);

            InterludePlan plan = InterludePlanner.Plan(
                evt.StageIndex, model.playerName, award, _npcData, _abilityData, progression,
                _rng.Fork($"interlude.stage{evt.StageIndex}"));

            if (plan.MetNpc != null)
            {
                progression.MeetNpc(plan.MetNpc.Id);
            }

            Persist(model);
            _currentStage = evt.StageIndex;

            if (plan.MetNpc != null)
            {
                _eventBus.Publish(new FriendMet { NpcId = plan.MetNpc.Id });
            }

            _eventBus.Publish(new InterludeReady { Plan = plan });
        }

        private void OnUpgradeRequested(UpgradeRequested evt)
        {
            SaveModelV1 model = LoadOrNew();
            var progression = new MetaProgression(model);

            AbilityData ability = _abilityData.Find(a => a.Id == evt.AbilityId);
            if (ability == null)
            {
                Debug.LogWarning($"UpgradeRequested for unknown ability '{evt.AbilityId}'.");
                return;
            }

            int level = progression.GetAbilityLevel(ability.Id);
            if (level >= ability.MaxLevel)
            {
                return;
            }

            if (!progression.TrySpendShards(ability.UpgradeCost(level)))
            {
                return;
            }

            progression.SetAbilityLevel(ability.Id, level + 1);
            Persist(model);

            _eventBus.Publish(new PassiveAbilityUpgraded { AbilityId = ability.Id, NewLevel = level + 1 });

            // Refresh the open interlude with updated offers/balance. No new
            // meeting on a replan (allowMeet: false).
            InterludePlan plan = InterludePlanner.Plan(
                _currentStage, model.playerName, 0, _npcData, _abilityData, progression,
                _rng.Fork($"interlude.stage{_currentStage}"), allowMeet: false);

            _eventBus.Publish(new InterludeUpdated { Plan = plan });
        }

        private SaveModelV1 LoadOrNew()
        {
            return _saveService.TryLoad(out SaveModelV1 model) ? model : new SaveModelV1();
        }

        private void Persist(SaveModelV1 model)
        {
            model.savedAtUnixUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _saveService.Save(model);
        }
    }
}
