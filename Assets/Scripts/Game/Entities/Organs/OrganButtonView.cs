using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Entities.Organs
{
    public sealed class OrganButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _background;
        [SerializeField] private Color _normalColor = new(0.13f, 0.20f, 0.28f);
        [SerializeField] private Color _selectedColor = new(0.12f, 0.54f, 0.50f);

        public OrganConfig Organ { get; private set; }

        public void Bind(OrganConfig organ, Action<OrganConfig> onClick)
        {
            Organ = organ;
            _label.text = organ.DisplayName;
            _button.onClick.AddListener(() => onClick(organ));
            SetSelected(false);
        }

        public void SetSelected(bool selected) => _background.color = selected ? _selectedColor : _normalColor;

        private void OnDestroy() => _button.onClick.RemoveAllListeners();
    }
}
