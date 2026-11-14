using Game.Comfort;
using Game.Entities.Environment;
using Game.Entities.Organs;
using Game.Entities.Participants;
using Game.Entities.Player;
using Game.Entities.Power;
using Game.Entities.Station;
using Game.Safety;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Scene composition root: owns references to scene objects and initializes them in a fixed order.
    /// </summary>
    public sealed class EntryPoint : MonoBehaviour
    {
        [SerializeField] private XROrigin _xrOrigin;
        [SerializeField] private ComfortMenu _comfortMenu;
        [SerializeField] private ParticipantControl _participant;
        [SerializeField] private HeartStationControl _heartStation;
        [SerializeField] private Probe _probe;
        [SerializeField] private PowerUnit _powerUnit;
        [SerializeField] private OrganCatalog _organCatalog;
        [SerializeField] private OrganPanel _organPanel;
        [SerializeField] private FlaskDispenser _flaskDispenser;
        [SerializeField] private EcgMonitor[] _ecgMonitors;

        private PlayerRig _playerRig;
        private SafetyController _safetyController;
        private OrganSelectionController _organSelection;

        private void Start()
        {
            if (_xrOrigin == null || _xrOrigin.Camera == null)
            {
                Debug.LogError("EntryPoint requires an XR Origin with a camera.", this);
                return;
            }

            InitializePlayer();
            InitializeComfort();
            InitializeParticipants();
            InitializeStation();
        }

        private void InitializePlayer()
        {
            _playerRig = new PlayerRig(_xrOrigin);
        }

        private void InitializeComfort()
        {
            _comfortMenu.Init(_playerRig);
        }

        private void InitializeParticipants()
        {
            _participant.Init(_playerRig);

            if (_comfortMenu.Locomotion == null)
                return;

            _safetyController = new SafetyController(_comfortMenu.Locomotion, _participant);
            _safetyController.Init();
        }

        private void InitializeStation()
        {
            _heartStation.Init(_probe, _powerUnit, _participant);
            foreach (var monitor in _ecgMonitors)
                monitor.Init(_heartStation);

            if (_organPanel == null || _organCatalog == null || _flaskDispenser == null)
                return;

            _flaskDispenser.Init();
            _organPanel.Init(_organCatalog.Organs, _playerRig.Camera);
            _organSelection = new OrganSelectionController(_organPanel, _flaskDispenser, _heartStation);
            _organSelection.Init();
        }

        private void OnDestroy()
        {
            _safetyController?.Dispose();
            _organSelection?.Dispose();
        }
    }
}
