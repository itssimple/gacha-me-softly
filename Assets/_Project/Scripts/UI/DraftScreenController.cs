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
    /// The draft screen: renders the three offered upgrades (localized name +
    /// description + rarity), publishes <see cref="UpgradePickRequested"/> on
    /// pick, hides on <see cref="UpgradeDrafted"/>.
    /// </summary>
    public sealed class DraftScreenController : MonoBehaviour, IServiceConsumer
    {
        private IEventBus _bus;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        private bool _uiBuilt;
        private Font _font;
        private Canvas _canvas;
        private Text _titleText;
        private RectTransform _choicesContainer;

        public void InitServices(ServiceRegistry services)
        {
            _bus = services.Resolve<IEventBus>();
            _subscriptions.Add(_bus.Subscribe<UpgradeChoicesReady>(evt => Render(evt.Choices)));
            _subscriptions.Add(_bus.Subscribe<UpgradeDrafted>(_ => Hide()));
            _subscriptions.Add(_bus.Subscribe<RunEnded>(_ => Hide()));
        }

        private void OnDestroy()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }

        private void Hide()
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(false);
            }
        }

        private void Render(List<UpgradeData> choices)
        {
            if (!_uiBuilt)
            {
                BuildUi();
                _uiBuilt = true;
            }

            _canvas.gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(RenderRoutine(choices));
        }

        private IEnumerator RenderRoutine(List<UpgradeData> choices)
        {
            yield return LocalizedTextUtility.SetText(_titleText, StoryKeys.Table, UiKeys.DraftTitle);

            for (int i = _choicesContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_choicesContainer.GetChild(i).gameObject);
            }

            foreach (UpgradeData choice in choices)
            {
                Button button = RuntimeUiFactory.CreateButton(_choicesContainer, $"Choice_{choice.Id}", _font,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, out Text label);
                label.fontSize = 30;

                var layout = button.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 190f;

                string upgradeId = choice.Id;
                button.onClick.AddListener(() => _bus.Publish(new UpgradePickRequested { UpgradeId = upgradeId }));

                string upgradeName = null;
                yield return LocalizedTextUtility.Get(StoryKeys.Table, choice.NameKey, null, v => upgradeName = v);
                string description = null;
                yield return LocalizedTextUtility.Get(StoryKeys.Table, choice.DescriptionKey, null, v => description = v);

                if (upgradeName != null && description != null)
                {
                    yield return LocalizedTextUtility.SetText(label, StoryKeys.Table, RarityKey(choice.Rarity),
                        new object[] { upgradeName, description });
                }
            }
        }

        private static string RarityKey(UpgradeRarity rarity)
        {
            switch (rarity)
            {
                case UpgradeRarity.Epic: return UiKeys.DraftChoiceEpic;
                case UpgradeRarity.Rare: return UiKeys.DraftChoiceRare;
                default: return UiKeys.DraftChoiceCommon;
            }
        }

        private void BuildUi()
        {
            _font = RuntimeUiFactory.LoadDefaultFont();
            RuntimeUiFactory.EnsureEventSystem();
            _canvas = RuntimeUiFactory.CreateCanvas(transform);

            _titleText = RuntimeUiFactory.CreateText(_canvas.transform, "Title", _font, 52,
                new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);

            var container = new GameObject("Choices");
            container.transform.SetParent(_canvas.transform, false);
            _choicesContainer = container.AddComponent<RectTransform>();
            _choicesContainer.anchorMin = new Vector2(0.1f, 0.15f);
            _choicesContainer.anchorMax = new Vector2(0.9f, 0.82f);
            _choicesContainer.offsetMin = Vector2.zero;
            _choicesContainer.offsetMax = Vector2.zero;

            var layoutGroup = container.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 24f;
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;

            _canvas.gameObject.SetActive(false);
        }
    }
}
