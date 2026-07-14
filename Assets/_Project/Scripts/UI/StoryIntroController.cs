using System;
using System.Collections;
using Chris.PachiRogue.Core;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Chris.PachiRogue.UI
{
    /// <summary>
    /// Drives the story intro in the Boot scene: first boot asks the player
    /// for their name, weaves it into the intro pages, and persists it;
    /// returning players get a personalized welcome-back beat. All
    /// player-facing text comes from the "Story" localization table (en+sv);
    /// the uGUI hierarchy is built at runtime by <see cref="RuntimeUiFactory"/>.
    /// Publishes <see cref="StoryIntroCompleted"/> when the player begins.
    /// </summary>
    public sealed class StoryIntroController : MonoBehaviour, IServiceConsumer
    {
        private ISaveService _saveService;
        private IEventBus _eventBus;

        private StoryIntroModel _model;
        private string _playerName = "";
        private bool _returningPlayer;

        private Font _font;
        private GameObject _namePanel;
        private GameObject _storyPanel;
        private Text _promptText;
        private Text _placeholderText;
        private InputField _nameInput;
        private Text _confirmLabel;
        private Text _storyText;
        private Button _storyButton;
        private Text _storyButtonLabel;

        public void InitServices(ServiceRegistry services)
        {
            _saveService = services.Resolve<ISaveService>();
            _eventBus = services.Resolve<IEventBus>();

            if (_saveService.TryLoad(out SaveModelV1 save) && !string.IsNullOrEmpty(save.playerName))
            {
                _playerName = save.playerName;
                _returningPlayer = true;
            }
        }

        private void Start()
        {
            if (_saveService == null)
            {
                Debug.LogError(
                    "StoryIntroController received no services — is GameBootstrap present in the Boot scene?");
                return;
            }

            StartCoroutine(RunIntro());
        }

        private IEnumerator RunIntro()
        {
            if (!LocalizationSettings.HasSettings)
            {
                Debug.LogError(
                    "Localization is not configured. Run 'PachiRogue > Localization > Create Story Tables' once in the editor.");
                yield break;
            }

            yield return LocalizationSettings.InitializationOperation;

            if (LocalizationSettings.SelectedLocale == null &&
                LocalizationSettings.AvailableLocales.Locales.Count > 0)
            {
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[0];
            }

            BuildUi();

            if (_returningPlayer)
            {
                yield return EnterStory();
            }
            else
            {
                yield return EnterNameEntry();
            }
        }

        private void BuildUi()
        {
            _font = RuntimeUiFactory.LoadDefaultFont();
            RuntimeUiFactory.EnsureEventSystem();
            Canvas canvas = RuntimeUiFactory.CreateCanvas(transform);

            _namePanel = new GameObject("NamePanel");
            _namePanel.transform.SetParent(canvas.transform, false);
            StretchFull(_namePanel.AddComponent<RectTransform>());

            _promptText = RuntimeUiFactory.CreateText(_namePanel.transform, "Prompt", _font, 52,
                new Vector2(0.08f, 0.55f), new Vector2(0.92f, 0.78f), Vector2.zero, Vector2.zero);

            _nameInput = RuntimeUiFactory.CreateInputField(_namePanel.transform, "NameInput", _font,
                new Vector2(0.15f, 0.42f), new Vector2(0.85f, 0.50f), Vector2.zero, Vector2.zero,
                out _placeholderText);

            Button confirm = RuntimeUiFactory.CreateButton(_namePanel.transform, "ConfirmButton", _font,
                new Vector2(0.30f, 0.28f), new Vector2(0.70f, 0.36f), Vector2.zero, Vector2.zero,
                out _confirmLabel);
            confirm.onClick.AddListener(OnConfirmName);

            _storyPanel = new GameObject("StoryPanel");
            _storyPanel.transform.SetParent(canvas.transform, false);
            StretchFull(_storyPanel.AddComponent<RectTransform>());

            _storyText = RuntimeUiFactory.CreateText(_storyPanel.transform, "StoryText", _font, 48,
                new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.82f), Vector2.zero, Vector2.zero);

            _storyButton = RuntimeUiFactory.CreateButton(_storyPanel.transform, "AdvanceButton", _font,
                new Vector2(0.30f, 0.15f), new Vector2(0.70f, 0.23f), Vector2.zero, Vector2.zero,
                out _storyButtonLabel);
            _storyButton.onClick.AddListener(OnStoryButtonClicked);

            _namePanel.SetActive(false);
            _storyPanel.SetActive(false);
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private IEnumerator EnterNameEntry()
        {
            _namePanel.SetActive(true);
            _storyPanel.SetActive(false);

            yield return SetLocalizedText(_promptText, StoryKeys.NamePrompt);
            yield return SetLocalizedText(_placeholderText, StoryKeys.NamePlaceholder);
            yield return SetLocalizedText(_confirmLabel, StoryKeys.ConfirmName);

            _nameInput.ActivateInputField();
        }

        private void OnConfirmName()
        {
            StartCoroutine(CommitNameAndEnterStory(StoryIntroModel.SanitizeName(_nameInput.text)));
        }

        private IEnumerator CommitNameAndEnterStory(string sanitizedName)
        {
            if (sanitizedName.Length == 0)
            {
                yield return GetLocalized(StoryKeys.DefaultName, value => sanitizedName = value);
            }

            _playerName = sanitizedName;
            PersistPlayerName();

            yield return EnterStory();
        }

        private void PersistPlayerName()
        {
            if (!_saveService.TryLoad(out SaveModelV1 save))
            {
                save = new SaveModelV1();
            }

            save.playerName = _playerName;
            save.savedAtUnixUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _saveService.Save(save);
        }

        private IEnumerator EnterStory()
        {
            _model = new StoryIntroModel(_returningPlayer);

            _namePanel.SetActive(false);
            _storyPanel.SetActive(true);

            yield return ShowCurrentPage();
        }

        private IEnumerator ShowCurrentPage()
        {
            yield return SetLocalizedText(_storyText, _model.CurrentPageKey, NameArgs());
            yield return SetLocalizedText(_storyButtonLabel,
                _model.IsLastPage ? StoryKeys.Begin : StoryKeys.Next);
        }

        private void OnStoryButtonClicked()
        {
            if (_model.Advance())
            {
                StartCoroutine(ShowCurrentPage());
            }
            else
            {
                CompleteIntro();
            }
        }

        private void CompleteIntro()
        {
            _eventBus.Publish(new StoryIntroCompleted { PlayerName = _playerName });

            // Terminal beat until Phase 2 delivers the first launch: the
            // outro stays on screen with no further input.
            _storyButton.gameObject.SetActive(false);
            StartCoroutine(SetLocalizedText(_storyText, StoryKeys.Outro, NameArgs()));
        }

        private object[] NameArgs()
        {
            return new object[] { _playerName };
        }

        private IEnumerator SetLocalizedText(Text target, string key, object[] arguments = null)
        {
            yield return GetLocalized(key, value => target.text = value, arguments);
        }

        private IEnumerator GetLocalized(string key, Action<string> onDone, object[] arguments = null)
        {
            // Yield on the async handle — WaitForCompletion is unsupported on
            // WebGL (Phase 0 note in NOTES.md).
            AsyncOperationHandle<string> handle =
                LocalizationSettings.StringDatabase.GetLocalizedStringAsync(StoryKeys.Table, key, arguments);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded && !string.IsNullOrEmpty(handle.Result))
            {
                onDone(handle.Result);
            }
            else
            {
                Debug.LogError(
                    $"Missing localized string '{key}' in table '{StoryKeys.Table}'. " +
                    "Run 'PachiRogue > Localization > Create Story Tables' in the editor.");
            }
        }
    }
}
