using System;
using UnityEngine;

namespace Gameplay.Entities.Interactive.Organs
{
    public sealed class FlaskButtonPanel : MonoBehaviour
    {
        [SerializeField] private OrganButton[] _buttons;

        public event Action<OrganConfig> OrganSelected;

        private void Awake()
        {
            foreach (var button in _buttons)
            {
                var captured = button;
                button.Pressed += () => OrganSelected?.Invoke(captured.Organ);
            }
        }

        public void Highlight(OrganConfig organ)
        {
            foreach (var button in _buttons)
            {
                button.SetSelected(button.Organ == organ);
            }
        }
    }
}
