using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Entities.Organs
{
    /// <summary>
    /// World-space panel with one button per organ in the catalog.
    /// Buttons are cloned from a template inside the prefab, so the look is edited in the prefab.
    /// </summary>
    public sealed class OrganPanel : MonoBehaviour, IOrganSelector
    {
        [SerializeField] private OrganButtonView _buttonTemplate;
        [SerializeField] private Transform _buttonsRoot;

        private readonly List<OrganButtonView> _buttons = new();

        public event Action<OrganConfig> OnOrganSelected;

        public void Init(IReadOnlyList<OrganConfig> organs, Camera eventCamera)
        {
            if (TryGetComponent(out Canvas canvas))
                canvas.worldCamera = eventCamera;

            _buttonTemplate.gameObject.SetActive(false);
            foreach (var organ in organs)
            {
                var button = Instantiate(_buttonTemplate, _buttonsRoot);
                button.gameObject.SetActive(true);
                button.Bind(organ, HandleClicked);
                _buttons.Add(button);
            }
        }

        public void Highlight(OrganConfig organ)
        {
            foreach (var button in _buttons)
                button.SetSelected(button.Organ == organ);
        }

        private void HandleClicked(OrganConfig organ) => OnOrganSelected?.Invoke(organ);
    }
}
