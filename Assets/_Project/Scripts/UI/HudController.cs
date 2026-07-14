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
    /// In-run HUD (stage, score/gate, launches, HP): subscribes to Run events
    /// only, never reaches into Run internals. Top bar respects the device
    /// safe area (PLAN.md Phase 5).
    /// </summary>
    public sealed class HudController : MonoBehaviour, IServiceConsumer
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        private bool _uiBuilt;
        private Canvas _canvas;
        private Text _stageText;
        private Text _scoreText;
        private Text _launchesText;
        private Text _hpText;

        public void InitServices(ServiceRegistry services)
        {
            IEventBus bus = services.Resolve<IEventBus>();

            _subscriptions.Add(bus.Subscribe<StageStarted>(evt =>
                Show(() => LocalizedTextUtility.SetText(_stageText, StoryKeys.Table, UiKeys.HudStage,
                    new object[] { evt.StageIndex }))));

            _subscriptions.Add(bus.Subscribe<ScoreChanged>(evt =>
                Show(() => LocalizedTextUtility.SetText(_scoreText, StoryKeys.Table, UiKeys.HudScore,
                    new object[] { evt.StageScore, evt.Gate }))));

            _subscriptions.Add(bus.Subscribe<LaunchesChanged>(evt =>
                Show(() => LocalizedTextUtility.SetText(_launchesText, StoryKeys.Table, UiKeys.HudLaunches,
                    new object[] { evt.Remaining }))));

            _subscriptions.Add(bus.Subscribe<HealthChanged>(evt =>
                Show(() => LocalizedTextUtility.SetText(_hpText, StoryKeys.Table, UiKeys.HudHealth,
                    new object[] { evt.Current, evt.Max }))));

            _subscriptions.Add(bus.Subscribe<RunEnded>(_ => { if (_canvas != null) _canvas.gameObject.SetActive(false); }));
        }

        private void OnDestroy()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }

        private void Show(Func<IEnumerator> routine)
        {
            if (!_uiBuilt)
            {
                BuildUi();
                _uiBuilt = true;
            }

            _canvas.gameObject.SetActive(true);
            StartCoroutine(routine());
        }

        private void BuildUi()
        {
            Font font = RuntimeUiFactory.LoadDefaultFont();
            _canvas = RuntimeUiFactory.CreateCanvas(transform, withBackground: false);

            var bar = new GameObject("TopBar");
            bar.transform.SetParent(_canvas.transform, false);
            var barRect = bar.AddComponent<RectTransform>();

            // Safe-area aware top strip (PLAN.md Phase 5).
            Rect safe = Screen.safeArea;
            float topInset = Screen.height > 0 ? 1f - (safe.yMax / Screen.height) : 0f;
            barRect.anchorMin = new Vector2(0f, 0.955f - topInset);
            barRect.anchorMax = new Vector2(1f, 1f - topInset);
            barRect.offsetMin = Vector2.zero;
            barRect.offsetMax = Vector2.zero;

            var barImage = bar.AddComponent<Image>();
            barImage.color = new Color(0.1f, 0.09f, 0.16f, 0.85f);

            _stageText = RuntimeUiFactory.CreateText(bar.transform, "Stage", font, 30,
                new Vector2(0f, 0f), new Vector2(0.25f, 1f), Vector2.zero, Vector2.zero);
            _scoreText = RuntimeUiFactory.CreateText(bar.transform, "Score", font, 30,
                new Vector2(0.25f, 0f), new Vector2(0.6f, 1f), Vector2.zero, Vector2.zero);
            _launchesText = RuntimeUiFactory.CreateText(bar.transform, "Launches", font, 30,
                new Vector2(0.6f, 0f), new Vector2(0.8f, 1f), Vector2.zero, Vector2.zero);
            _hpText = RuntimeUiFactory.CreateText(bar.transform, "HP", font, 30,
                new Vector2(0.8f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        }
    }
}
