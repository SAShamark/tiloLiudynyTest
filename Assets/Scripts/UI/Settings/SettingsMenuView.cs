using System;
using Gameplay.Entities.Player.Movement;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace UI.Settings
{
    public sealed class SettingsMenuView : MonoBehaviour
    {
        [Serializable]
        private sealed class OptionButton<T>
        {
            public T Mode;
            public Button Button;
            public Image Background;
        }

        [Serializable]
        private sealed class ToggleRow
        {
            public Button OnButton;
            public Image OnBackground;
            public Button OffButton;
            public Image OffBackground;
        }

        [SerializeField] private Canvas _canvas;
        [SerializeField] private OptionButton<MovementMode>[] _modeButtons;
        [SerializeField] private Slider _vignetteSlider;
        [SerializeField] private TMP_Text _vignetteLabel;
        [SerializeField] private string _vignetteFormat = "Stick vignette  {0:0}%";

        [Header("Turning")]
        [SerializeField] private OptionButton<TurnMode>[] _turnButtons;
        [SerializeField] private Slider _turnSlider;
        [SerializeField] private TMP_Text _turnLabel;
        [SerializeField] private string _snapFormat = "Snap angle  {0:0}°";
        [SerializeField] private string _smoothFormat = "Turn speed  {0:0}°/s";
        [SerializeField] private Vector2 _snapRange = new(15f, 90f);
        [SerializeField] private Vector2 _smoothRange = new(30f, 180f);

        [Header("Participant & safety")]
        [SerializeField] private ToggleRow _participantIdle;
        [SerializeField] private ToggleRow _stickWarnings;
        [SerializeField] private ToggleRow _showBody;

        [Header("Height")]
        [SerializeField] private Slider _heightSlider;
        [SerializeField] private TMP_Text _heightLabel;
        [SerializeField] private string _heightFormat = "Height  {0:+0;-0;0} cm";

        [SerializeField] private Button _returnButton;
        [SerializeField] private Button _closeButton;

        [Header("Toggle colours")]
        [SerializeField] private Color _normalColor = new(0.16f, 0.24f, 0.33f);
        [SerializeField] private Color _selectedColor = new(0.12f, 0.62f, 0.58f);

        private TurnMode _turnMode;

        public bool IsOpen => gameObject.activeSelf;

        public event Action<MovementMode> ModeSelected;
        public event Action<float> VignetteChanged;
        public event Action<TurnMode> TurnModeSelected;
        public event Action<float> TurnAmountChanged;
        public event Action<bool> ParticipantIdleSelected;
        public event Action<bool> StickWarningsSelected;
        public event Action<bool> ShowBodySelected;
        public event Action<float> HeightChanged;
        public event Action ReturnClicked;
        public event Action CloseClicked;

        public void Initialize(Camera eventCamera, float initialVignette)
        {
            _canvas.worldCamera = eventCamera;

            foreach (var modeButton in _modeButtons)
            {
                MovementMode mode = modeButton.Mode;
                modeButton.Button.onClick.AddListener(() =>
                {
                    ShowMode(mode);
                    ModeSelected?.Invoke(mode);
                });
            }

            foreach (var turnButton in _turnButtons)
            {
                TurnMode mode = turnButton.Mode;
                turnButton.Button.onClick.AddListener(() => TurnModeSelected?.Invoke(mode));
            }

            Bind(_participantIdle, value => ParticipantIdleSelected?.Invoke(value));
            Bind(_stickWarnings, value => StickWarningsSelected?.Invoke(value));
            Bind(_showBody, value => ShowBodySelected?.Invoke(value));

            _vignetteSlider.SetValueWithoutNotify(initialVignette);
            _vignetteSlider.onValueChanged.AddListener(value =>
            {
                ShowVignetteStrength(value);
                VignetteChanged?.Invoke(value);
            });
            _turnSlider.wholeNumbers = true;
            _turnSlider.onValueChanged.AddListener(value =>
            {
                ShowTurnAmount(value);
                TurnAmountChanged?.Invoke(value);
            });
            _heightSlider.wholeNumbers = true;
            _heightSlider.onValueChanged.AddListener(value =>
            {
                ShowHeight(value);
                HeightChanged?.Invoke(value);
            });
            _returnButton.onClick.AddListener(() => ReturnClicked?.Invoke());
            _closeButton.onClick.AddListener(() => CloseClicked?.Invoke());

            ShowVignetteStrength(initialVignette);
            gameObject.SetActive(false);
        }

        public void Open(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            gameObject.SetActive(true);
        }

        public void Close() => gameObject.SetActive(false);

        public void ShowMode(MovementMode mode)
        {
            foreach (var modeButton in _modeButtons)
                modeButton.Background.color = modeButton.Mode == mode ? _selectedColor : _normalColor;
        }

        public void ShowTurn(TurnMode mode, float amount)
        {
            _turnMode = mode;
            foreach (var turnButton in _turnButtons)
                turnButton.Background.color = turnButton.Mode == mode ? _selectedColor : _normalColor;

            Vector2 range = mode == TurnMode.Snap ? _snapRange : _smoothRange;
            _turnSlider.minValue = range.x;
            _turnSlider.maxValue = range.y;
            _turnSlider.SetValueWithoutNotify(amount);
            ShowTurnAmount(amount);
        }

        public void ShowParticipantIdle(bool idle) => ShowToggle(_participantIdle, idle);

        public void ShowStickWarnings(bool enabled) => ShowToggle(_stickWarnings, enabled);

        public void ShowBody(bool visible) => ShowToggle(_showBody, visible);

        public void ShowHeight(float centimetres)
        {
            _heightSlider.SetValueWithoutNotify(centimetres);
            _heightLabel.text = string.Format(_heightFormat, centimetres);
        }

        private void Bind(ToggleRow row, Action<bool> onSelected)
        {
            row.OnButton.onClick.AddListener(() => Select(true));
            row.OffButton.onClick.AddListener(() => Select(false));

            void Select(bool on)
            {
                ShowToggle(row, on);
                onSelected(on);
            }
        }

        private void ShowToggle(ToggleRow row, bool on)
        {
            row.OnBackground.color = on ? _selectedColor : _normalColor;
            row.OffBackground.color = on ? _normalColor : _selectedColor;
        }

        private void ShowVignetteStrength(float percent) => _vignetteLabel.text = string.Format(_vignetteFormat, percent);

        private void ShowTurnAmount(float amount) =>
            _turnLabel.text = string.Format(_turnMode == TurnMode.Snap ? _snapFormat : _smoothFormat, amount);
    }
}
