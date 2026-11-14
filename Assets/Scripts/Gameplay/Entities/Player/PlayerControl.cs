using Gameplay.Entities.Player.Body;
using Gameplay.Entities.Player.Hands;
using UI.Settings;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace Gameplay.Entities.Player
{
    public sealed class PlayerControl : MonoBehaviour
    {
        [SerializeField] private XROrigin _xrOrigin;
        [SerializeField] private PhysicalHandsControl _hands;
        [SerializeField] private PlayerBodyView _body;
        [SerializeField] private SettingsMenu _settings;

        public PlayerRig Rig { get; private set; }
        public SettingsMenu Settings => _settings;

        public void Initialize()
        {
            Rig = new PlayerRig(_xrOrigin);
            _hands.Initialize(Rig.TrackingSpace);
            _settings.Initialize(Rig);

            _body.Initialize(Rig, _hands);
            _body.SetVisible(_settings.ShowBody);
            _settings.ShowBodyChanged += _body.SetVisible;
        }

        public void Dispose() => _settings.ShowBodyChanged -= _body.SetVisible;
    }
}
