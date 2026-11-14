using System;
using UnityEngine;

namespace Gameplay.Entities.Interactive.BaseLogic.Items
{
    public abstract class ReturningItem : GrabItem
    {
        [SerializeField] private DropRecovery _recovery = new();

        public event Action<ReturningItem> Recovered;

        protected override void Awake()
        {
            base.Awake();
            _recovery.Initialize(Body, Grab);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _recovery.Reset();
        }

        protected virtual void FixedUpdate()
        {
            if (!_recovery.FixedTick(Time.fixedDeltaTime))
            {
                return;
            }

            ForgetController();
            Recovered?.Invoke(this);
        }

        public void SetHome(Vector3 position, Quaternion rotation) => _recovery.SetHome(position, rotation);

        public void ConfirmInsertion(bool playHaptics)
        {
            if (playHaptics)
            {
                SendHaptics(0.45f, 0.12f);
            }

            ForgetController();
        }
    }
}
