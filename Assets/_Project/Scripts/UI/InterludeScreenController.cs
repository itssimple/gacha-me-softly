using System;
using System.Collections;
using System.Collections.Generic;
using Chris.PachiRogue.Core;
using Chris.PachiRogue.Run;
using UnityEngine;
using UnityEngine.UI;

namespace Chris.PachiRogue.UI
{
    /// <summary>
    /// Renders the between-stages interludes: the story beat for the run so
    /// far (player name woven in), NPC meetings, and passive-ability upgrade
    /// offers. Purely reactive — it renders <see cref="InterludePlan"/>
    /// snapshots from <see cref="InterludeReady"/>/<see cref="InterludeUpdated"/>
    /// and publishes <see cref="UpgradeRequested"/>/<see cref="InterludeCompleted"/>;
    /// all state lives in the Run assembly.
    /// </summary>
    public sealed class InterludeScreenController : MonoBehaviour, IServiceConsumer
    {
        private IEventBus _eventBus;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        private InterludePlan _plan;
        private bool _uiBuilt;
        private Font _font;
        private Canvas _canvas;
        private Text _headerText;
        private Text _shardText;
        private Text _beatText;
        private Text _npcLineText;
        private Text _joinedText;
        private Text _upgradeTitleText;
        private RectTransform _offersContainer;
        private Button _continueButton;
        private Text _continueLabel;

        public void InitServices(ServiceRegistry services)
        {
            _eventBus = services.Resolve<IEventBus>();

            _subscriptions.Add(_eventBus.Subscribe<InterludeReady>(evt => Render(evt.Plan, fullRender: true)));
            _subscriptions.Add(_eventBus.Subscribe<InterludeUpdated>(evt => Render(evt.Plan, fullRender: false)));
        }

        private void OnDestroy()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }

        private void Render(InterludePlan plan, bool fullRender)
        {
            _plan = plan;

            if (!_uiBuilt)
            {
                BuildUi();
                _uiBuilt = true;
            }

            _canvas.gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(RenderRoutine(fullRender));
        }

        private IEnumerator RenderRoutine(bool fullRender)
        {
            InterludePlan plan = _plan;
            object[] nameArgs = { plan.PlayerName };

            yield return LocalizedTextUtility.SetText(_shardText, StoryKeys.Table, UiKeys.ShardReport,
                new object[] { plan.ShardsAwarded, plan.ShardBalance });

            if (fullRender)
            {
                if (plan.IsVictory)
                {
                    yield return LocalizedTextUtility.SetText(_headerText, StoryKeys.Table, UiKeys.VictoryHeader);
                }
                else
                {
                    yield return LocalizedTextUtility.SetText(_headerText, StoryKeys.Table, UiKeys.InterludeHeader,
                        new object[] { plan.ClearedStageIndex });
                }

                yield return LocalizedTextUtility.SetText(_beatText, StoryKeys.Table, plan.BeatKey, nameArgs);

                bool metSomeone = plan.MetNpc != null;
                _npcLineText.gameObject.SetActive(metSomeone);
                _joinedText.gameObject.SetActive(metSomeone);
                if (metSomeone)
                {
                    yield return LocalizedTextUtility.SetText(_npcLineText, StoryKeys.Table,
                        plan.MetNpc.MeetLineKey, nameArgs);

                    string npcName = null;
                    yield return LocalizedTextUtility.Get(StoryKeys.Table, plan.MetNpc.NameKey, null,
                        value => npcName = value);
                    if (npcName != null)
                    {
                        yield return LocalizedTextUtility.SetText(_joinedText, StoryKeys.Table, UiKeys.Joined,
                            new object[] { npcName });
                    }
                }

                // The victory interlude is the terminal screen of the demo
                // flow — the run is over, nothing to continue into yet.
                _continueButton.gameObject.SetActive(!plan.IsVictory);
                if (!plan.IsVictory)
                {
                    yield return LocalizedTextUtility.SetText(_continueLabel, StoryKeys.Table, UiKeys.Continue);
                }
            }

            bool anyOffers = plan.Offers.Count > 0;
            _upgradeTitleText.gameObject.SetActive(anyOffers);
            if (anyOffers)
            {
                yield return LocalizedTextUtility.SetText(_upgradeTitleText, StoryKeys.Table, UiKeys.UpgradeTitle);
            }

            yield return RenderOffers();
        }

        private IEnumerator RenderOffers()
        {
            for (int i = _offersContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_offersContainer.GetChild(i).gameObject);
            }

            foreach (AbilityOffer offer in _plan.Offers)
            {
                Button button = RuntimeUiFactory.CreateButton(_offersContainer, $"Offer_{offer.AbilityId}", _font,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, out Text label);
                label.fontSize = 34;

                var layout = button.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 88f;

                button.interactable = offer.Affordable;

                string abilityId = offer.AbilityId;
                button.onClick.AddListener(() => _eventBus.Publish(new UpgradeRequested { AbilityId = abilityId }));

                string abilityName = null;
                yield return LocalizedTextUtility.Get(StoryKeys.Table, offer.NameKey, null,
                    value => abilityName = value);
                if (abilityName == null)
                {
                    continue;
                }

                if (offer.IsMaxed)
                {
                    yield return LocalizedTextUtility.SetText(label, StoryKeys.Table, UiKeys.UpgradeMaxed,
                        new object[] { abilityName, offer.MaxLevel });
                }
                else
                {
                    yield return LocalizedTextUtility.SetText(label, StoryKeys.Table, UiKeys.UpgradeButton,
                        new object[] { abilityName, offer.CurrentLevel, offer.MaxLevel, offer.UpgradeCost });
                }
            }
        }

        private void OnContinueClicked()
        {
            int stage = _plan.ClearedStageIndex;
            _canvas.gameObject.SetActive(false);
            _eventBus.Publish(new InterludeCompleted { StageIndex = stage });
        }

        private void BuildUi()
        {
            _font = RuntimeUiFactory.LoadDefaultFont();
            RuntimeUiFactory.EnsureEventSystem();
            _canvas = RuntimeUiFactory.CreateCanvas(transform);

            _headerText = RuntimeUiFactory.CreateText(_canvas.transform, "Header", _font, 56,
                new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.97f), Vector2.zero, Vector2.zero);

            _shardText = RuntimeUiFactory.CreateText(_canvas.transform, "Shards", _font, 36,
                new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.90f), Vector2.zero, Vector2.zero);

            _beatText = RuntimeUiFactory.CreateText(_canvas.transform, "Beat", _font, 42,
                new Vector2(0.07f, 0.66f), new Vector2(0.93f, 0.85f), Vector2.zero, Vector2.zero);

            _npcLineText = RuntimeUiFactory.CreateText(_canvas.transform, "NpcLine", _font, 38,
                new Vector2(0.07f, 0.52f), new Vector2(0.93f, 0.66f), Vector2.zero, Vector2.zero);
            _npcLineText.fontStyle = FontStyle.Italic;

            _joinedText = RuntimeUiFactory.CreateText(_canvas.transform, "Joined", _font, 40,
                new Vector2(0.07f, 0.47f), new Vector2(0.93f, 0.52f), Vector2.zero, Vector2.zero);

            _upgradeTitleText = RuntimeUiFactory.CreateText(_canvas.transform, "UpgradeTitle", _font, 36,
                new Vector2(0.07f, 0.41f), new Vector2(0.93f, 0.46f), Vector2.zero, Vector2.zero);

            var offersGo = new GameObject("Offers");
            offersGo.transform.SetParent(_canvas.transform, false);
            _offersContainer = offersGo.AddComponent<RectTransform>();
            _offersContainer.anchorMin = new Vector2(0.12f, 0.12f);
            _offersContainer.anchorMax = new Vector2(0.88f, 0.41f);
            _offersContainer.offsetMin = Vector2.zero;
            _offersContainer.offsetMax = Vector2.zero;

            var layoutGroup = offersGo.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 14f;
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;

            _continueButton = RuntimeUiFactory.CreateButton(_canvas.transform, "ContinueButton", _font,
                new Vector2(0.30f, 0.03f), new Vector2(0.70f, 0.10f), Vector2.zero, Vector2.zero,
                out _continueLabel);
            _continueButton.onClick.AddListener(OnContinueClicked);

            _canvas.gameObject.SetActive(false);
        }
    }
}
