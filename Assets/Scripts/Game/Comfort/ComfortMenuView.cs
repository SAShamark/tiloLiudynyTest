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
