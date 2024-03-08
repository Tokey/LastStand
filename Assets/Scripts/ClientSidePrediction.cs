/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using FishNet.Object.Prediction;
using Demo.Scripts.Runtime;
using static ClientSidePrediction;
using static UnityEngine.Rendering.PostProcessing.HistogramMonitor;
using System.Globalization;
using System;
using Unity.Netcode;
using UnityEngine.TextCore.Text;
*/
using UnityEngine;

public class ClientSidePrediction : MonoBehaviour
{
    
}

/*
    #region STRUCTS

    public struct InputData : IReplicateData
    {
        public Vector3 moveInput;
        public Vector2 lookInput;
        public Quaternion rotation;

        public const uint BUTTON_JUMP = 1 << 0;
        public const uint BUTTON_CROUCH = 1 << 1;
        public const uint BUTTON_SPRINT = 1 << 2;

        public const uint BUTTON_LEANLEFT = 1 << 3;
        public const uint BUTTON_LEANRIGHT = 1 << 4;
        public const uint BUTTON_AIM = 1 << 5;
        public const uint SET_POINTAIM = 1 << 6;
        public const uint BUTTON_CHANGESCOPE = 1 << 7;

        public const uint BUTTON_FIRE = 1 << 8;
        public const uint BUTTON_RELOAD = 1 << 9;
        public const uint BUTTON_NEXTWEAPON = 1 << 10;
        public const uint BUTTON_GRENADE = 1 << 11;
        public const uint BUTTON_UNASSIGNED3 = 1 << 12;
        public const uint BUTTON_UNASSIGNED2 = 1 << 14;
        public const uint BUTTON_UNASSIGNED1 = 1 << 15;

        public uint buttons;

        public bool IsPressed(uint button) => (buttons & button) == button;

        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public struct ReconcileData : IReconcileData
    {
        public Vector3 position;

        public Vector3 velocity;

        public bool isConstrainedToGround;
        public float unconstrainedTimer;

        public bool hitGround;
        public bool isWalkable;

        public Vector3 groundNormal;

        // Movement mode

        public MovementMode movementMode;

        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    #endregion

    #region FIELDS

    private MyCharacter _character;
    private MyFPSController _fpsController;

    private Vector2 _lookInput;
    private float _lastSyncTime;
    private Vector2 _syncRate; // Used for smoothing lookInputSync for clients over time

    #endregion

    #region PROPERTIES

    public Vector2 LookInput => _lookInput; // Used by FPSController on remote clients to set the aim

    #endregion

    #region NETWORK FIELDS

    [SyncVar(Channel = Channel.Unreliable, ReadPermissions = ReadPermission.ExcludeOwner, OnChange = nameof(on_lookInputSync))] private Vector2 _lookInputSync;
    [SyncVar(Channel = Channel.Reliable, ReadPermissions = ReadPermission.ExcludeOwner, OnChange = nameof(on_buttonInputSync))] private uint _buttonInputSync;

    #endregion

    #region PRIVATE METHODS

    private void CacheComponents()
    {
        _character = GetComponent<MyCharacter>();

        if (_character)
        {
            _character.handleInput = false;
            _character.enableLateFixedUpdate = false;
        }

        _fpsController = GetComponent<MyFPSController>();
    }

    private void ReadInput(out InputData inputData)
    {
        inputData = default;

        inputData.moveInput = _fpsController.MoveInput;
        _fpsController.MoveInput = Vector3.zero;

        inputData.lookInput = _fpsController.LookInput;
        inputData.rotation = transform.rotation;
        inputData.buttons = _fpsController.ButtonsPressed;
    }

    [Replicate]
    private void Simulate(InputData inputData, bool asServer, Channel channel = Channel.Unreliable, bool replaying = false)
    {
        float deltaTime = (float)TimeManager.TickDelta;

        // normalize inputs to prevent errors
        inputData.moveInput = inputData.moveInput.normalized;
        inputData.rotation = inputData.rotation.normalized;

        _character.SetMovementDirection(inputData.moveInput);

        if (IsServer)
        {
            _lookInput = inputData.lookInput;
            _lookInputSync = inputData.lookInput;
            _buttonInputSync = inputData.buttons;
        }

        if (!IsOwner) { _character.SetRotation(inputData.rotation); }

        _character.Simulate(deltaTime);
        if (!replaying) { _fpsController.SimulateActions(inputData.buttons); }
    }

    [Reconcile]
    private void Reconcile(ReconcileData reconcileData, bool asServer, Channel channel = Channel.Unreliable)
    {
        MyCharacter.CharacterState characterState = new MyCharacter.CharacterState
        {
            // CharacterMovement state

            position = reconcileData.position,
            rotation = transform.rotation,

            velocity = reconcileData.velocity,

            isConstrainedToGround = reconcileData.isConstrainedToGround,
            unconstrainedTimer = reconcileData.unconstrainedTimer,

            hitGround = reconcileData.hitGround,
            isWalkable = reconcileData.isWalkable,

            groundNormal = reconcileData.groundNormal,

            movementMode = reconcileData.movementMode,
        };

        _character.SetState(characterState);
    }

    // Whenever the server sends a new aim sync, calculate a new MoveTowards rate for a smooth Update
    private void on_lookInputSync(Vector2 prev, Vector2 next, bool asServer)
    {
        //if (IsServer) { return; }

        float past = (float)base.TimeManager.TickDelta;
        float syncDelta = Time.time - _lastSyncTime;
        past = Mathf.Max(past, syncDelta); // Need greater for clients that miss packets. Animator stops working when past < TickDelta.
        _lastSyncTime = Time.time;

        float rateX = Mathf.Abs(_lookInput.x - next.x) / past;
        float rateY = Mathf.Abs(_lookInput.y - next.y) / past;
        _syncRate.x = rateX;
        _syncRate.y = rateY;
    }

    private void on_buttonInputSync(uint prev, uint next, bool asServer)
    {
        if (IsServer) { return; }

        _fpsController.SimulateActions(next);
    }

    #endregion

    #region NETWORKBEHAVIOUR

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        if (base.IsServer || base.IsClient)
            TimeManager.OnTick += OnTick;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner) { _fpsController.IsOwner = true; }
        if (IsServer) { _fpsController.IsServer = true; }
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (base.TimeManager != null) { TimeManager.OnTick -= OnTick; }
    }

    private void OnTick()
    {
        if (IsOwner)
        {
            Reconcile(default, false);
            ReadInput(out InputData inputData);
            Simulate(inputData, false);
            return;
        }

        if (IsServer)
        {
            Simulate(default, true);

            MyCharacter.CharacterState characterState = _character.GetState();
            ReconcileData reconcileData = new ReconcileData
            {
                position = characterState.position,

                velocity = characterState.velocity,

                isConstrainedToGround = characterState.isConstrainedToGround,
                unconstrainedTimer = characterState.unconstrainedTimer,

                hitGround = characterState.hitGround,
                isWalkable = characterState.isWalkable,

                groundNormal = characterState.groundNormal,

                movementMode = characterState.movementMode,
            };

            Reconcile(reconcileData, true);
        }
    }

    #endregion

    #region MONOBEHAVIOUR

    private void Awake()
    {
        CacheComponents();
    }

    private void Update()
    {
        //if (IsServer) { return; }

        if (!IsOwner)
        {
            float deltaTime = Time.deltaTime;
            _lookInput.x = Mathf.MoveTowards(_lookInput.x, _lookInputSync.x, _syncRate.x * deltaTime);
            _lookInput.y = Mathf.MoveTowards(_lookInput.y, _lookInputSync.y, _syncRate.y * deltaTime);
        }
    }

    #endregion
}*/