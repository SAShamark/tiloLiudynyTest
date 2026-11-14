using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Entities.Interactive.Organs
{
    public sealed class OrganPanel : MonoBehaviour, IOrganSelector
    {
        [SerializeField] private OrganButtonView _buttonTemplate;
        [SerializeField] private Transform _buttonsRoot;

        private readonly List<OrganButtonView> _buttons = new();

        public event Action<OrganConfig> OrganSelected;

        public void Initialize(IReadOnlyList<OrganConfig> organs, Camera eventCamera)
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

        private void HandleClicked(OrganConfig organ) => OrganSelected?.Invoke(organ);
    }
}
