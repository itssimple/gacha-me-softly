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
    /// Run-end presentation. Defeat: full panel (message, shard bonus, run
    /// again). Victory: the interlude's victory beat stays visible as the
    /// payoff screen, so only a compact bottom strip (bonus + run again) is
    /// overlaid.
    /// </summary>
    public sealed class RunEndController : MonoBehaviour, IServiceConsumer
    {
        private IEventBus _bus;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        private string _playerName = "";
        private bool _uiBuilt;
        private Font _font;
        private Canvas _canvas;
        private Image _background;
        private Text _messageText;
        private Text _bonusText;
        private Text _againLabel;

        public void InitServices(ServiceRegistry services)
        {
            _bus = services.Resolve<IEventBus>();
            ISaveService save = services.Resolve<ISaveService>();
            if (save.TryLoad(out SaveModelV1 model))
            {
                _playerName = model.playerName;
            }

            _subscriptions.Add(_bus.Subscribe<RunEnded>(evt => Render(evt)));
            _subscriptions.Add(_bus.Subscribe<RunStarted>(_ => Hide()));
            // Name is entered after InitServices on first boot — track it.
            _subscriptions.Add(_bus.Subscribe<StoryIntroCompleted>(evt => _playerName = evt.PlayerName));
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

        private void Render(RunEnded evt)
        {
            if (!_uiBuilt)
            {
                BuildUi();
                _uiBuilt = true;
            }

            _canvas.gameObject.SetActive(true);
            _background.enabled = !evt.Victory;
            _messageText.gameObject.SetActive(!evt.Victory);

            StopAllCoroutines();
            StartCoroutine(RenderRoutine(evt));
        }

        private IEnumerator RenderRoutine(RunEnded evt)
        {
            if (!evt.Victory)
            {
                yield return LocalizedTextUtility.SetText(_messageText, StoryKeys.Table, UiKeys.RunEndDefeat,
                    new object[] { _playerName, evt.StageReached });
            }

            yield return LocalizedTextUtility.SetText(_bonusText, StoryKeys.Table, UiKeys.RunEndBonus,
                new object[] { evt.ShardBonus });
            yield return LocalizedTextUtility.SetText(_againLabel, StoryKeys.Table, UiKeys.RunAgain);
        }

        private void OnRunAgain()
        {
            Hide();
            _bus.Publish(new RunRestartRequested());
        }

        private void BuildUi()
        {
            _font = RuntimeUiFactory.LoadDefaultFont();
            RuntimeUiFactory.EnsureEventSystem();
            _canvas = RuntimeUiFactory.CreateCanvas(transform, withBackground: false);

            _background = RuntimeUiFactory.CreateFullScreenImage(_canvas.transform, "Background",
                new Color(0.10f, 0.09f, 0.16f, 0.96f));

            _messageText = RuntimeUiFactory.CreateText(_canvas.transform, "Message", _font, 46,
                new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.8f), Vector2.zero, Vector2.zero);

            _bonusText = RuntimeUiFactory.CreateText(_canvas.transform, "Bonus", _font, 36,
                new Vector2(0.08f, 0.17f), new Vector2(0.92f, 0.24f), Vector2.zero, Vector2.zero);

            Button again = RuntimeUiFactory.CreateButton(_canvas.transform, "RunAgain", _font,
                new Vector2(0.30f, 0.05f), new Vector2(0.70f, 0.13f), Vector2.zero, Vector2.zero,
                out _againLabel);
            again.onClick.AddListener(OnRunAgain);

            _canvas.gameObject.SetActive(false);
        }
    }
}
