using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Comfort
{
    /// <summary>
    /// World-space comfort panel. All visuals live in the ComfortMenu prefab; this class only wires
    /// the controls to events and reflects the current settings.
    /// </summary>
    public sealed class ComfortMenuView : MonoBehaviour
    {
        [Serializable]
        private sealed class ModeButton
        {
            public MovementMode Mode;
            public Button Button;
            public Image Background;
        }

        [Serializable]
        private sealed class TurnButton
        {
            public TurnMode Mode;
            public Button Button;
            public Image Background;
        }

        /// <summary>Two-button on/off switch (e.g. "Stand" / "Walk").</summary>
        [Serializable]
        private sealed class ToggleRow
        {
            public Button OnButton;
            public Image OnBackground;
            public Button OffButton;
            public Image OffBackground;
        }

        [SerializeField] private Canvas _canvas;
        [SerializeField] private ModeButton[] _modeButtons;
        [SerializeField] private Slider _vignetteSlider;
        [SerializeField] private TMP_Text _vignetteLabel;
        [SerializeField] private string _vignetteFormat = "Stick vignette  {0:0}%";

        [Header("Turning")]
        [SerializeField] private TurnButton[] _turnButtons;
        [SerializeField] private Slider _turnSlider;
        [SerializeField] private TMP_Text _turnLabel;
        [SerializeField] private string _snapFormat = "Snap angle  {0:0}°";
        [SerializeField] private string _smoothFormat = "Turn speed  {0:0}°/s";
        [SerializeField] private Vector2 _snapRange = new(15f, 90f);
        [SerializeField] private Vector2 _smoothRange = new(30f, 180f);

        [Header("Participant & safety")]
        [Tooltip("On = participant stands idle, Off = participant walks the route.")]
        [SerializeField] private ToggleRow _participantIdle;
        [Tooltip("On = proximity warnings also during stick movement.")]
        [SerializeField] private ToggleRow _stickWarnings;
        [Tooltip("On = show the player's own body.")]
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

        public event Action<MovementMode> OnModeSelected;
        public event Action<float> OnVignetteChanged;
        public event Action<TurnMode> OnTurnModeSelected;
        public event Action<float> OnTurnAmountChanged;
        public event Action<bool> OnParticipantIdleSelected;
        public event Action<bool> OnStickWarningsSelected;
        public event Action<bool> OnShowBodySelected;
        /// <summary>Height offset in centimetres.</summary>
        public event Action<float> OnHeightChanged;
        public event Action OnReturnClicked;
        public event Action OnCloseClicked;

        public void Init(Camera eventCamera, float initialVignette)
        {
            _canvas.worldCamera = eventCamera;

            foreach (var modeButton in _modeButtons)
            {
                MovementMode mode = modeButton.Mode;
                modeButton.Button.onClick.AddListener(() => OnModeSelected?.Invoke(mode));
            }

            foreach (var turnButton in _turnButtons)
            {
                TurnMode mode = turnButton.Mode;
                turnButton.Button.onClick.AddListener(() => OnTurnModeSelected?.Invoke(mode));
            }

            _vignetteSlider.SetValueWithoutNotify(initialVignette);
            _vignetteSlider.onValueChanged.AddListener(HandleVignetteChanged);
            _turnSlider.wholeNumbers = true;
            _turnSlider.onValueChanged.AddListener(HandleTurnAmountChanged);
            _returnButton.onClick.AddListener(HandleReturnClicked);
            _closeButton.onClick.AddListener(HandleCloseClicked);
            Bind(_participantIdle, value => OnParticipantIdleSelected?.Invoke(value));
            Bind(_stickWarnings, value => OnStickWarningsSelected?.Invoke(value));
            Bind(_showBody, value => OnShowBodySelected?.Invoke(value));
            if (_heightSlider != null)
            {
                _heightSlider.wholeNumbers = true;
                _heightSlider.onValueChanged.AddListener(HandleHeightChanged);
            }

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

        public void ShowVignetteStrength(float percent) => _vignetteLabel.text = string.Format(_vignetteFormat, percent);

        /// <summary>Shows the turn mode and the amount that applies to it (snap angle or smooth speed).</summary>
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
            if (_heightSlider != null)
                _heightSlider.SetValueWithoutNotify(centimetres);
            if (_heightLabel != null)
                _heightLabel.text = string.Format(_heightFormat, centimetres);
        }

        private void HandleHeightChanged(float centimetres)
        {
            ShowHeight(centimetres);
            OnHeightChanged?.Invoke(centimetres);
        }

        private static void Bind(ToggleRow row, Action<bool> onSelected)
        {
            if (row?.OnButton == null || row.OffButton == null)
                return;

            row.OnButton.onClick.AddListener(() => onSelected(true));
            row.OffButton.onClick.AddListener(() => onSelected(false));
        }

        private static void Unbind(ToggleRow row)
        {
            row?.OnButton?.onClick.RemoveAllListeners();
            row?.OffButton?.onClick.RemoveAllListeners();
        }

        private void ShowToggle(ToggleRow row, bool on)
        {
            if (row?.OnBackground == null || row.OffBackground == null)
                return;

            row.OnBackground.color = on ? _selectedColor : _normalColor;
            row.OffBackground.color = on ? _normalColor : _selectedColor;
        }

        private void ShowTurnAmount(float amount) =>
            _turnLabel.text = string.Format(_turnMode == TurnMode.Snap ? _snapFormat : _smoothFormat, amount);

        private void OnDestroy()
        {
            foreach (var modeButton in _modeButtons)
                modeButton.Button.onClick.RemoveAllListeners();
            foreach (var turnButton in _turnButtons)
                turnButton.Button.onClick.RemoveAllListeners();

            _vignetteSlider.onValueChanged.RemoveListener(HandleVignetteChanged);
            _turnSlider.onValueChanged.RemoveListener(HandleTurnAmountChanged);
            _returnButton.onClick.RemoveListener(HandleReturnClicked);
            _closeButton.onClick.RemoveListener(HandleCloseClicked);
            Unbind(_participantIdle);
            Unbind(_stickWarnings);
            Unbind(_showBody);
            if (_heightSlider != null)
                _heightSlider.onValueChanged.RemoveListener(HandleHeightChanged);
        }

        private void HandleVignetteChanged(float value) => OnVignetteChanged?.Invoke(value);

        private void HandleTurnAmountChanged(float value)
        {
            ShowTurnAmount(value);
            OnTurnAmountChanged?.Invoke(value);
        }

        private void HandleReturnClicked() => OnReturnClicked?.Invoke();

        private void HandleCloseClicked() => OnCloseClicked?.Invoke();
    }
}
