// Designed by KINEMATION, 2023

using Kinemation.FPSFramework.Runtime.Camera;
using Kinemation.FPSFramework.Runtime.Core.Components;
using Kinemation.FPSFramework.Runtime.Core.Playables;
using Kinemation.FPSFramework.Runtime.Core.Types;
using Kinemation.FPSFramework.Runtime.FPSAnimator;
using Kinemation.FPSFramework.Runtime.Layers;
using Kinemation.FPSFramework.Runtime.Recoil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Netcode;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.Rendering.DebugUI;
using Random = UnityEngine.Random;

namespace Demo.Scripts.Runtime
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class TabAttribute : PropertyAttribute
    {
        public readonly string tabName;

        public TabAttribute(string tabName)
        {
            this.tabName = tabName;
        }
    }

    public enum FPSAimState
    {
        None,
        Ready,
        Aiming,
        PointAiming
    }

    public enum FPSActionState
    {
        None,
        Reloading,
        WeaponChange
    }

    // An example-controller class
    public class FPSController : NetworkBehaviour
    {
        [Tab("Animation")]
        [Header("General")]
        [SerializeField] private Animator animator;

        [Header("Turn In Place")]
        [SerializeField] private float turnInPlaceAngle;
        [SerializeField] private AnimationCurve turnCurve = new AnimationCurve(new Keyframe(0f, 0f));
        [SerializeField] private float turnSpeed = 1f;

        [Header("Leaning")]
        [SerializeField] private float smoothLeanStep = 1f;
        [SerializeField, Range(0f, 1f)] private float startLean = 1f;

        [Header("Dynamic Motions")]
        [SerializeField] private IKAnimation aimMotionAsset;
        [SerializeField] private IKAnimation leanMotionAsset;
        [SerializeField] private IKAnimation crouchMotionAsset;
        [SerializeField] private IKAnimation unCrouchMotionAsset;
        [SerializeField] private IKAnimation onJumpMotionAsset;
        [SerializeField] private IKAnimation onLandedMotionAsset;
        [SerializeField] private IKAnimation onStartStopMoving;

        // Animation Layers
        [SerializeField][HideInInspector] private LookLayer lookLayer;
        [SerializeField][HideInInspector] private AdsLayer adsLayer;
        [SerializeField][HideInInspector] private SwayLayer swayLayer;
        [SerializeField][HideInInspector] private LocomotionLayer locoLayer;
        [SerializeField][HideInInspector] private SlotLayer slotLayer;
        [SerializeField][HideInInspector] private WeaponCollision collisionLayer;
        // Animation Layers

        [Header("General")]
        [Tab("Controller")]
        [SerializeField] private float timeScale = 1f;
        [SerializeField, Min(0f)] private float equipDelay = 0f;

        [Header("Camera")]
        [SerializeField] private Transform mainCamera;
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private Transform firstPersonCamera;
        [SerializeField] private float sensitivity;
        [SerializeField] private Vector2 freeLookAngle;

        [Header("Movement")]
        [SerializeField] private FPSMovement movementComponent;

        [SerializeField]
        [Tab("Weapon")]
        private List<Weapon> weapons;
        public Transform weaponBone;

        private Vector2 _playerInput;

        // Used for free-look
        private Vector2 _freeLookInput;

        private int _index;
        private int _lastIndex;

        private int _bursts;
        private bool _freeLook;

        private FPSAimState aimState;
        private FPSActionState actionState;

        private float smoothCurveAlpha = 0f;

        private static readonly int Crouching = Animator.StringToHash("Crouching");
        private static readonly int OverlayType = Animator.StringToHash("OverlayType");
        private static readonly int TurnRight = Animator.StringToHash("TurnRight");
        private static readonly int TurnLeft = Animator.StringToHash("TurnLeft");
        private static readonly int UnEquip = Animator.StringToHash("Unequip");

        private Vector2 _controllerRecoil;
        private float _recoilStep;
        private bool _isFiring;

        private bool _isUnarmed;

        public GameObject bullet;
        private Transform muzzlePointTransform;

        private float fireTimer;


        //Audio
        public AudioClip weaponChangeSFX;
        public AudioClip ADSInSFX;
        public AudioClip ADSOutSFX;
        public AudioClip hitRegSFX;
        public AudioClip killSFX;
        public AudioClip headshotSFX;
        public AudioSource playerAudioSource;
        


        /// ANIM CONTROLLER STARTS
        private CoreAnimComponent fpsAnimator;
        private FPSCamera fpsCamera;
        private LookLayer internalLookLayer;
        protected RecoilAnimation recoilComponent;
        protected CharAnimData charAnimData;
        protected NetworkVariable<CharAnimData> syncCharAnimData = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public int score = 0;

        public int roundKills = 0;
        public int roundDeaths = 0;

        // Used primarily for function calls from Animation Events
        // Runs once at the beginning of the next update
        protected UnityEvent queuedAnimEvents;

        GameObject enemy;

        public float ADTLOSRange;
        public Transform playerHeadBone;

        public string fileNameSuffix = "";
        public String filenamePerTick = "Data\\ClientDataPerTick.csv";
        public String filenamePerRound = "Data\\ClientDataPerRound.csv";

        public NetworkVariable<bool> isPlayerReady = new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public NetworkVariable<bool> qoeEnabled = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<bool> expQuesEnabled = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public bool otherReady = false;

        public float qoeValue = 0;
        public bool expQuesValue = false;

        public bool isInvincible;
        public float invincibilityTimer;

        Vector3 oldPosition;
        public float distanceTravelledPersession;
        public float distanceTravelledPerRound;

        public NetworkVariable<ulong> playerID = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public int shotsFiredPerRound = 0;
        public int shotsFiredPerSession = 0;

        public int shotsHitPerRound = 0;
        public int shotsHitPerSession = 0;

        public int headshotsHitPerRound = 0;
        public int headshotsHitPerSession = 0;

        public float killCooldown;
        public float regularHitCooldown;
        public float headshotCooldown;


        public PlayerStats playerStats;

        public float takehitTimer=0;

        public bool toQuit;

        float durationWithADTOn = 0;
        float durationWithADTOff = 0;

        bool leftLean = false;
        bool rightLean = false;

        long bulletSpawnTime;
        bool bulletTimerEnabled = false;
        protected CoreAnimGraph GetAnimGraph()
        {
            return fpsAnimator.animGraph;
        }

        // Call this once when the character is initialized
        protected void InitAnimController()
        {
            fpsAnimator = GetComponentInChildren<CoreAnimComponent>();
            fpsAnimator.animGraph.InitPlayableGraph();
            fpsAnimator.InitializeLayers();

            recoilComponent = GetComponentInChildren<RecoilAnimation>();
            charAnimData = new CharAnimData();

            fpsCamera = GetComponentInChildren<FPSCamera>();
            internalLookLayer = GetComponentInChildren<LookLayer>();

            if (fpsCamera != null)
            {
                fpsCamera.rootBone = fpsAnimator.ikRigData.rootBone;
            }
        }

        // Call this once when the character is initialized
        protected void InitAnimController(UnityAction cameraDelegate)
        {
            InitAnimController();
            fpsAnimator.onPostUpdate.AddListener(cameraDelegate);

            if (fpsCamera == null) return;
            fpsAnimator.onPostUpdate.AddListener(fpsCamera.UpdateCamera);
        }

        // Call this when equipping a new weapon
        protected void InitWeapon(FPSAnimWeapon weapon)
        {
            recoilComponent.Init(weapon.weaponAsset.recoilData, weapon.fireRate, weapon.fireMode);
            fpsAnimator.OnGunEquipped(weapon.weaponAsset, weapon.weaponTransformData);

            fpsAnimator.ikRigData.weaponTransform = weapon.weaponAsset.weaponBone;
            internalLookLayer.SetAimOffsetTable(weapon.weaponAsset.aimOffsetTable);

            var pose = weapon.weaponAsset.overlayPose;

            if (pose == null)
            {
                Debug.LogError("FPSAnimController: OverlayPose is null! Make sure to assign it in the weapon prefab.");
                return;
            }

            fpsAnimator.OnPrePoseSampled();
            PlayPose(weapon.weaponAsset.overlayPose);
            fpsAnimator.OnPoseSampled();

            if (fpsCamera != null)
            {
                fpsCamera.cameraData = weapon.weaponAsset.adsData.cameraData;
            }
        }

        // Call this when changing sights
        protected void InitAimPoint(FPSAnimWeapon weapon)
        {
            fpsAnimator.OnSightChanged(weapon.GetAimPoint());
        }

        // Call this during Update after all the gameplay logic
        protected void UpdateAnimController()
        {
            if (queuedAnimEvents != null)
            {
                queuedAnimEvents.Invoke();
                queuedAnimEvents = null;
            }

            if (recoilComponent != null)
            {
                charAnimData.recoilAnim = new LocRot(recoilComponent.OutLoc, Quaternion.Euler(recoilComponent.OutRot));
            }

            if (IsOwner)
            {
                syncCharAnimData.Value = charAnimData;
            }
            else
            {
                LocRot recoilCache = charAnimData.recoilAnim;
                charAnimData = syncCharAnimData.Value;
                charAnimData.recoilAnim = recoilCache;
            }

            fpsAnimator.SetCharData(charAnimData);
            fpsAnimator.animGraph.UpdateGraph();
            fpsAnimator.UpdateCoreComponent();

            if (fpsCamera != null)
            {
                float Pitch = GetAnimGraph().GetCurveValue(CurveLib.Curve_Camera_Pitch);
                float Yaw = GetAnimGraph().GetCurveValue(CurveLib.Curve_Camera_Yaw);
                float Roll = GetAnimGraph().GetCurveValue(CurveLib.Curve_Camera_Roll);

                Quaternion rot = Quaternion.Euler(Pitch, Yaw, Roll);
                fpsCamera.UpdateCameraAnimation(rot.normalized);
            }
        }

        protected void OnInputAim(bool isAiming)
        {
            if (fpsCamera != null)
            {
                fpsCamera.isAiming = isAiming;
            }
        }

        // Call this to play a Camera shake
        protected void PlayCameraShake(FPSCameraShake shake)
        {
            if (fpsCamera != null)
            {
                fpsCamera.PlayShake(shake.shakeInfo);
            }
        }

        protected void PlayController(RuntimeAnimatorController controller, AnimSequence motion)
        {
            if (motion == null) return;
            fpsAnimator.animGraph.PlayController(controller, motion.clip, motion.blendTime.blendInTime);
        }

        // Call this to play a static pose on the character upper body
        protected void PlayPose(AnimSequence motion)
        {
            if (motion == null) return;
            fpsAnimator.animGraph.PlayPose(motion);
        }

        // Call this to play an animation on the character upper body
        protected void PlayAnimation(AnimSequence motion, float startTime = 0f)
        {
            if (motion == null) return;

            fpsAnimator.animGraph.PlayAnimation(motion, startTime);
        }

        protected void StopAnimation(float blendTime = 0f)
        {
            fpsAnimator.animGraph.StopAnimation(blendTime);
        }
        /// ANIM CONTROLLER END

        private void InitLayers()
        {
            InitAnimController();

            animator = GetComponentInChildren<Animator>();
            lookLayer = GetComponentInChildren<LookLayer>();
            adsLayer = GetComponentInChildren<AdsLayer>();
            locoLayer = GetComponentInChildren<LocomotionLayer>();
            swayLayer = GetComponentInChildren<SwayLayer>();
            slotLayer = GetComponentInChildren<SlotLayer>();
            collisionLayer = GetComponentInChildren<WeaponCollision>();
        }

        private bool HasActiveAction()
        {
            return actionState != FPSActionState.None;
        }

        private bool IsAiming()
        {
            return aimState is FPSAimState.Aiming or FPSAimState.PointAiming;
        }

        private void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            moveRotation = transform.rotation;

            movementComponent = GetComponent<FPSMovement>();

            movementComponent.onStartMoving.AddListener(() => slotLayer.PlayMotion(onStartStopMoving));
            movementComponent.onStopMoving.AddListener(() => slotLayer.PlayMotion(onStartStopMoving));

            movementComponent.onCrouch.AddListener(OnCrouch);
            movementComponent.onUncrouch.AddListener(OnUncrouch);

            movementComponent.onJump.AddListener(OnJump);
            movementComponent.onLanded.AddListener(OnLand);

            movementComponent.onSprintStarted.AddListener(OnSprintStarted);
            movementComponent.onSprintEnded.AddListener(OnSprintEnded);

            movementComponent.onSlideStarted.AddListener(OnSlideStarted);
            movementComponent.onSlideEnded.AddListener(OnSlideEnded);

            movementComponent.onProneStarted.AddListener(() => collisionLayer.SetLayerAlpha(0f));
            movementComponent.onProneEnded.AddListener(() => collisionLayer.SetLayerAlpha(1f));

            movementComponent.slideCondition += () => !HasActiveAction();
            movementComponent.sprintCondition += () => !HasActiveAction();
            movementComponent.proneCondition += () => !HasActiveAction();

            actionState = FPSActionState.None;

            InitLayers();
            EquipWeapon();

            

            playerAudioSource = GetComponent<AudioSource>();

            distanceTravelledPersession = 0;
            distanceTravelledPerRound = 0;

            isPlayerReady.Value = false;
            playerID.Value = NetworkManager.LocalClientId;
            playerStats = GetComponent<PlayerStats>();
            playerStats.ownerID = NetworkManager.Singleton.LocalClientId;
            roundKills = 0;
            score = 0;

            takehitTimer = 0;

            toQuit = false;
            durationWithADTOn = 0;
            durationWithADTOff = 0;
        }

        private void UnequipWeapon()
        {
            DisableAim();

            actionState = FPSActionState.WeaponChange;
            animator.CrossFade(UnEquip, 0.1f);
        }

        public void ResetActionState()
        {
            actionState = FPSActionState.None;
        }

        public void RefreshStagedState()
        {
        }

        public void ResetStagedState()
        {
        }

        private void EquipWeapon()
        {
            if (weapons.Count == 0) return;

            weapons[_lastIndex].gameObject.SetActive(false);
            var gun = weapons[_index];

            _bursts = gun.burstAmount;

            StopAnimation(0.1f);
            InitWeapon(gun);
            gun.gameObject.SetActive(true);

            animator.SetFloat(OverlayType, (float)gun.overlayType);
            actionState = FPSActionState.None;
        }

        private void EnableUnarmedState()
        {
            if (weapons.Count == 0) return;

            weapons[_index].gameObject.SetActive(false);
            animator.SetFloat(OverlayType, 0);
        }

        private void NextWeapon_Internal()
        {
            if (movementComponent.PoseState == FPSPoseState.Prone) return;

            if (HasActiveAction()) return;

            OnFireReleasedServerRpc();

            int newIndex = _index;
            newIndex++;
            if (newIndex > weapons.Count - 1)
            {
                newIndex = 0;
            }

            _lastIndex = _index;
            _index = newIndex;

            UnequipWeapon();

            PlayWeaponChangeSFX();

            Invoke(nameof(EquipWeapon), equipDelay);
        }

        private void PreviousWeapon_Internal()
        {
            if (movementComponent.PoseState == FPSPoseState.Prone) return;

            if (HasActiveAction()) return;

            OnFireReleasedServerRpc();

            int newIndex = _index;
            newIndex--;
            if (newIndex < 0)
            {
                newIndex = weapons.Count - 1;
            }

            _lastIndex = _index;
            _index = newIndex;

            UnequipWeapon();
            PlayWeaponChangeSFX();
            Invoke(nameof(EquipWeapon), equipDelay);
        }

        void PlayWeaponChangeSFX()
        {
            playerAudioSource.volume = UnityEngine.Random.Range(0.1f, 0.3f);
            playerAudioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);

            playerAudioSource.PlayOneShot(weaponChangeSFX);
        }

        [ServerRpc(RequireOwnership = false)]
        void NextWeaponServerRpc()
        {
            NextWeaponClientRpc();
        }

        [ClientRpc]
        void NextWeaponClientRpc()
        {
            NextWeapon_Internal();
        }

        [ServerRpc(RequireOwnership = false)]
        void PreviousWeaponServerRpc()
        {
            PreviousWeaponClientRpc();
        }

        [ClientRpc]
        void PreviousWeaponClientRpc()
        {
            PreviousWeapon_Internal();
        }

        private void DisableAim()
        {
            if (!GetGun().canAim) return;

            aimState = FPSAimState.None;
            OnInputAim(false);

            adsLayer.SetAds(false);
            adsLayer.SetPointAim(false);
            swayLayer.SetFreeAimEnable(true);
            swayLayer.SetLayerAlpha(1f);
            slotLayer.PlayMotion(aimMotionAsset);
        }

        public void ToggleAim()
        {
            if (!GetGun().canAim) return;

            if (!IsAiming())
            {
                aimState = FPSAimState.Aiming;
                OnInputAim(true);

                adsLayer.SetAds(true);
                swayLayer.SetFreeAimEnable(false);
                swayLayer.SetLayerAlpha(0.5f);
                slotLayer.PlayMotion(aimMotionAsset);
                PlayADSInSFX();
            }
            else
            {
                DisableAim();
                PlayADSOutSFX();
            }

            recoilComponent.isAiming = IsAiming();
        }

        public void StartADS()
        {
            if (!GetGun().canAim) return;

            if (!IsAiming())
            {
                aimState = FPSAimState.Aiming;
                OnInputAim(true);

                adsLayer.SetAds(true);
                swayLayer.SetFreeAimEnable(false);
                swayLayer.SetLayerAlpha(0.5f);
                slotLayer.PlayMotion(aimMotionAsset);
                PlayADSInSFX();
            }

            recoilComponent.isAiming = IsAiming();
        }

        public void StopADS()
        {
            if (IsAiming())
            {
                DisableAim();
                PlayADSOutSFX();
            }

            recoilComponent.isAiming = IsAiming();
        }

        void PlayADSInSFX()
        {
            playerAudioSource.volume = UnityEngine.Random.Range(0.15f, 0.2f);
            playerAudioSource.pitch = UnityEngine.Random.Range(0.9f, 1.05f);

            playerAudioSource.PlayOneShot(ADSInSFX);
        }

        void PlayADSOutSFX()
        {
            playerAudioSource.volume = UnityEngine.Random.Range(0.15f, 0.2f);
            playerAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);

            playerAudioSource.PlayOneShot(ADSOutSFX);
        }

        void PlayHitRegSFX()
        {
            playerAudioSource.volume = UnityEngine.Random.Range(1f, 1.2f);
            playerAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);

            playerAudioSource.PlayOneShot(hitRegSFX);
        }

        void PlayKillSFX()
        {
            playerAudioSource.volume = UnityEngine.Random.Range(1f, 1.2f);
            playerAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);

            playerAudioSource.PlayOneShot(killSFX);
        }

        void PlayHeadshotSFX()
        {
            playerAudioSource.volume = UnityEngine.Random.Range(1f, 1.2f);
            playerAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);

            playerAudioSource.PlayOneShot(headshotSFX);
        }

        [ServerRpc(RequireOwnership = false)]
        public void LeanServerRpc(bool isRightLean, bool isLeftLean)
        {
            LeanClientRpc(isRightLean, isLeftLean);
        }

        [ClientRpc]
        public void LeanClientRpc(bool isRightLean, bool isLeftLean)
        {
            Lean(isRightLean, isLeftLean);
        }

        public void Lean(bool isRightLean, bool isLeftLean)
        {
            bool wasLeaning = _isLeaning;
            rightLean = isRightLean;
            leftLean = isLeftLean;

            _isLeaning = rightLean || leftLean;

            if (_isLeaning != wasLeaning)
            {
                slotLayer.PlayMotion(leanMotionAsset);
                charAnimData.SetLeanInput(wasLeaning ? 0f : rightLean ? -startLean : startLean);
            }

            if (_isLeaning)
            {
                float leanValue = Input.GetAxis("Mouse ScrollWheel") * smoothLeanStep;
                charAnimData.AddLeanInput(leanValue);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void StopADSServerRpc()
        {
            StopADSClientRpc();
        }

        [ClientRpc]
        public void StopADSClientRpc()
        {
            StopADS();
        }

        [ServerRpc(RequireOwnership = false)]
        public void StartADSServerRpc()
        {
            StartADSClientRpc();
        }

        [ClientRpc]
        public void StartADSClientRpc()
        {
            StartADS();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ToggleAimServerRpc()
        {
            ToggleAimClientRpc();
        }

        [ClientRpc]
        public void ToggleAimClientRpc()
        {
            ToggleAim();
        }

        public void ChangeScope()
        {
            InitAimPoint(GetGun());
        }

        private void SpawnProjectile()
        {

            muzzlePointTransform = GetGun().shootPoint.transform;

            Vector3 targetPoint = muzzlePointTransform.position + muzzlePointTransform.TransformDirection(Vector3.forward);

            Vector3 directionWithoutSpread = targetPoint - muzzlePointTransform.position;

            SpawnProjectileServerRpc(muzzlePointTransform.position, directionWithoutSpread, NetworkManager.Singleton.LocalClientId);

            shotsFiredPerRound++;
            shotsFiredPerSession++;
        }
        [ServerRpc(RequireOwnership = false)]
        public void ConfirmHitServerRpc(int scoreToAdd)
        {
            ConfirmHitClientRpc(scoreToAdd);
        }

        [ClientRpc]
        public void ConfirmHitClientRpc(int scoreToAdd)
        {
            /*shotsHitPerRound.Value++;
            shotsHitPerSession.Value++;*/
            AddToScoreServerRpc(scoreToAdd);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SpawnProjectileServerRpc(Vector3 shootPoint, Vector3 directionWithoutSpread, ulong clientId)
        {
            bullet.GetComponent<Bullet>().ownerID.Value = clientId;
            GameObject currentBullet = Instantiate(bullet, shootPoint, Quaternion.identity);
            currentBullet.transform.forward = directionWithoutSpread.normalized;
            currentBullet.GetComponent<NetworkObject>().Spawn();

            currentBullet.GetComponent<Rigidbody>().AddForce(directionWithoutSpread.normalized * currentBullet.GetComponent<Bullet>().bulletSpeed, ForceMode.Impulse);
        }
        
        private void Fire()
        {
            if (HasActiveAction()) return;
            if (_isFiring && IsOwner)
            {
                bulletSpawnTime = ProfilerUnsafeUtility.Timestamp;
                bulletTimerEnabled = true;
                //SpawnProjectile();
                SpawnProjectile();

            }


            if (GetGun().currentAmmoCount <= 0)
            {
                OnFireReleasedServerRpc();
                return;
            }

            GetGun().currentAmmoCount--;
            fireTimer = 60 / GetGun().fireRate;


            GetGun().OnFire();
            PlayAnimation(GetGun().fireClip);

            PlayCameraShake(GetGun().cameraShake);



            if (GetGun().recoilPattern != null)
            {
                float aimRatio = IsAiming() ? GetGun().recoilPattern.aimRatio : 1f;
                float hRecoil = Random.Range(GetGun().recoilPattern.horizontalVariation.x,
                    GetGun().recoilPattern.horizontalVariation.y);
                _controllerRecoil += new Vector2(hRecoil, _recoilStep) * aimRatio;
            }

            if (recoilComponent == null || GetGun().weaponAsset.recoilData == null)
            {
                return;
            }

            recoilComponent.Play();
            GetGun().weaponAudioSource.PlayOneShot(GetGun().fireSFX);

            if (recoilComponent.fireMode == FireMode.Burst)
            {
                if (_bursts == 0)
                {
                    OnFireReleasedServerRpc();
                    return;
                }

                _bursts--;
            }

            if (recoilComponent.fireMode == FireMode.Semi)
            {
                _isFiring = false;
                return;
            }




            _recoilStep += GetGun().recoilPattern.acceleration;
        }

        private void OnFirePressed()
        {
            if (weapons.Count == 0 || HasActiveAction()) return;

            _bursts = GetGun().burstAmount - 1;

            if (GetGun().recoilPattern != null)
            {
                _recoilStep = GetGun().recoilPattern.step;
            }

            _isFiring = true;
        }

        [ServerRpc(RequireOwnership = false)]
        private void OnFirePressedServerRpc()
        {
            OnFirePressedClientRpc();
        }

        [ClientRpc]
        private void OnFirePressedClientRpc()
        {
            OnFirePressed();
        }
        public Weapon GetGun()
        {
            if (weapons.Count == 0) return null;

            return weapons[_index];
        }

        private void OnFireReleased()
        {
            if (weapons.Count == 0) return;

            if (GetGun().currentAmmoCount <= 0)
                TryReloadServerRpc();

            if (recoilComponent != null)
            {
                recoilComponent.Stop();
            }

            _recoilStep = 0f;
            _isFiring = false;
            //CancelInvoke(nameof(Fire));
        }

        [ServerRpc(RequireOwnership = false)]
        private void OnFireReleasedServerRpc()
        {
            OnFireReleasedClientRpc();
        }

        [ClientRpc]
        private void OnFireReleasedClientRpc()
        {
            OnFireReleased();
        }

        private void OnSlideStarted()
        {
            lookLayer.SetLayerAlpha(0.3f);
        }

        private void OnSlideEnded()
        {
            lookLayer.SetLayerAlpha(1f);
        }

        private void OnSprintStarted()
        {
            OnFireReleasedServerRpc();
            lookLayer.SetLayerAlpha(0.5f);
            adsLayer.SetLayerAlpha(0f);
            locoLayer.SetReadyWeight(0f);

            aimState = FPSAimState.None;

            if (recoilComponent != null)
            {
                recoilComponent.Stop();
            }
        }

        private void OnSprintEnded()
        {
            lookLayer.SetLayerAlpha(1f);
            adsLayer.SetLayerAlpha(1f);
        }

        private void OnCrouch()
        {
            lookLayer.SetPelvisWeight(0f);
            animator.SetBool(Crouching, true);
            slotLayer.PlayMotion(crouchMotionAsset);
        }

        private void OnUncrouch()
        {
            lookLayer.SetPelvisWeight(1f);
            animator.SetBool(Crouching, false);
            slotLayer.PlayMotion(unCrouchMotionAsset);
        }

        private void OnJump()
        {
            slotLayer.PlayMotion(onJumpMotionAsset);
        }

        private void OnLand()
        {
            slotLayer.PlayMotion(onLandedMotionAsset);
        }

        private void TryReload()
        {
            if (HasActiveAction()) return;

            if (GetGun().currentAmmoCount >= GetGun().magSize)
                return;

            GetGun().currentAmmoCount = GetGun().magSize;

            var reloadClip = GetGun().reloadClip;

            if (reloadClip == null) return;

            OnFireReleasedServerRpc();

            PlayAnimation(reloadClip);
            GetGun().Reload();
            actionState = FPSActionState.Reloading;
        }
        [ServerRpc(RequireOwnership = false)]
        void TryReloadServerRpc()
        {
            TryReloadClientRpc();
        }

        [ClientRpc]
        void TryReloadClientRpc()
        {
            TryReload();
        }
        private void TryGrenadeThrow()
        {
            if (HasActiveAction()) return;

            if (GetGun().grenadeClip == null) return;

            OnFireReleasedServerRpc();
            DisableAim();
            PlayAnimation(GetGun().grenadeClip);
            actionState = FPSActionState.Reloading;
        }

        private bool _isLeaning;

        private void UpdateActionInput()
        {
            smoothCurveAlpha = Mathf.Lerp(smoothCurveAlpha, IsAiming() ? 0.5f : 1f,
                FPSAnimLib.ExpDecayAlpha(10f, Time.deltaTime));

            animator.SetLayerWeight(3, smoothCurveAlpha);

            if (movementComponent.MovementState == FPSMovementState.Sprinting)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha0) && !HasActiveAction())
            {
                _isUnarmed = !_isUnarmed;

                if (_isUnarmed)
                {
                    UnequipWeapon();
                    Invoke(nameof(EnableUnarmedState), equipDelay);
                }
                else
                {
                    weapons[_index].gameObject.SetActive(true);
                    animator.SetFloat(OverlayType, (float)GetGun().overlayType);
                }

                lookLayer.SetLayerAlpha(_isUnarmed ? 0.3f : 1f);
            }

            if (_isUnarmed) return;

            if (Input.GetKeyDown(KeyCode.R))
            {
                //TryReload();
                TryReloadServerRpc();
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                //TryGrenadeThrow();
            }

            if (Input.GetAxis("Mouse ScrollWheel") < 0f)
            {
                //ChangeWeapon_Internal();
                //ChangeWeaponServerRpc();
                //NextWeaponServerRpc();
            }

            if (Input.GetAxis("Mouse ScrollWheel") > 0f)
            {
                //ChangeWeapon_Internal();
                //ChangeWeaponServerRpc();
                //PreviousWeaponServerRpc();
            }

            if (aimState != FPSAimState.Ready)
            {
                

                if (Input.GetKey(KeyCode.E) && (IsOwner && isPlayerReady.Value && otherReady && !qoeEnabled.Value && !expQuesEnabled.Value))
                {
                    // Right Lean
                    Pair leanRightPair = new Pair();

                    leanRightPair.value = 1f;
                    leanRightPair.time = System.DateTime.Now.AddMilliseconds(movementComponent.latencyManager.playerLatency);

                    movementComponent.latencyManager.leanRightQueue.Enqueue(leanRightPair);
                }
                if (Input.GetKey(KeyCode.Q) && (IsOwner && isPlayerReady.Value && otherReady && !qoeEnabled.Value && !expQuesEnabled.Value))
                {
                    // Left Lean
                    Pair leanLeftPair = new Pair();

                    leanLeftPair.value = 1f;
                    leanLeftPair.time = System.DateTime.Now.AddMilliseconds(movementComponent.latencyManager.playerLatency);

                    movementComponent.latencyManager.leanLeftQueue.Enqueue(leanLeftPair);
                }

                /*bool wasLeaning = _isLeaning;
                rightLean = Input.GetKey(KeyCode.E);
                leftLean = Input.GetKey(KeyCode.Q);

                _isLeaning = rightLean || leftLean;

                if (_isLeaning != wasLeaning)
                {
                    slotLayer.PlayMotion(leanMotionAsset);
                    charAnimData.SetLeanInput(wasLeaning ? 0f : rightLean ? -startLean : startLean);
                }

                if (_isLeaning)
                {
                    float leanValue = Input.GetAxis("Mouse ScrollWheel") * smoothLeanStep;
                    charAnimData.AddLeanInput(leanValue);
                }*/

                if (Input.GetKeyDown(KeyCode.Mouse0) && (IsOwner && isPlayerReady.Value && otherReady && !qoeEnabled.Value && !expQuesEnabled.Value))
                {
                    Pair firePressedPair = new Pair();

                    firePressedPair.value = 1f;
                    firePressedPair.time = System.DateTime.Now.AddMilliseconds(movementComponent.latencyManager.playerLatency);


                    movementComponent.latencyManager.firePressedQueue.Enqueue(firePressedPair);

                    //OnFirePressedServerRpc();
                }

                if (Input.GetKeyUp(KeyCode.Mouse0) && (IsOwner && isPlayerReady.Value && otherReady && !qoeEnabled.Value && !expQuesEnabled.Value))
                {
                    Pair fireReleasedPair = new Pair();

                    fireReleasedPair.value = 1f;
                    fireReleasedPair.time = System.DateTime.Now.AddMilliseconds(movementComponent.latencyManager.playerLatency);


                    movementComponent.latencyManager.fireReleasedQueue.Enqueue(fireReleasedPair);

                    //OnFireReleasedServerRpc();
                }
                if (Input.GetKeyDown(KeyCode.Mouse2))
                {
                    //RespawnPlayer();
                }

                if (Input.GetKeyDown(KeyCode.Mouse1) && (IsOwner && isPlayerReady.Value && otherReady && !qoeEnabled.Value && !expQuesEnabled.Value))
                {
                    Pair adsPressedPair = new Pair();

                    adsPressedPair.value = 1f;
                    adsPressedPair.time = System.DateTime.Now.AddMilliseconds(movementComponent.latencyManager.playerLatency);

                    movementComponent.latencyManager.ADSPressedQueue.Enqueue(adsPressedPair);

                    //StartADSServerRpc();

                }
                if (Input.GetKeyUp(KeyCode.Mouse1) && (IsOwner && isPlayerReady.Value && otherReady && !qoeEnabled.Value && !expQuesEnabled.Value))
                {
                    Pair adsReleasedPair = new Pair();

                    adsReleasedPair.value = 1f;
                    adsReleasedPair.time = System.DateTime.Now.AddMilliseconds(movementComponent.latencyManager.playerLatency);

                    movementComponent.latencyManager.ADSReleasedQueue.Enqueue(adsReleasedPair);

                    //StopADSServerRpc();
                }

                if (Input.GetKeyDown(KeyCode.V))
                {
                    ChangeScope();
                }

                if (Input.GetKeyDown(KeyCode.B) && IsAiming())
                {
                    if (aimState == FPSAimState.PointAiming)
                    {
                        adsLayer.SetPointAim(false);
                        aimState = FPSAimState.Aiming;
                    }
                    else
                    {
                        adsLayer.SetPointAim(true);
                        aimState = FPSAimState.PointAiming;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                /*if (aimState == FPSAimState.Ready)
                {
                    aimState = FPSAimState.None;
                    locoLayer.SetReadyWeight(0f);
                    lookLayer.SetLayerAlpha(1f);
                }
                else
                {
                    aimState = FPSAimState.Ready;
                    locoLayer.SetReadyWeight(1f);
                    lookLayer.SetLayerAlpha(.5f);
                    OnFireReleasedServerRpc();
                }*/
            }

            // Lag stuffs

            while (movementComponent.latencyManager.fireReleasedQueue.Count > 0 && movementComponent.latencyManager.fireReleasedQueue.First().time <= System.DateTime.Now)
            {
                OnFireReleasedServerRpc();
                movementComponent.latencyManager.fireReleasedQueue.Dequeue();
            }

            while (movementComponent.latencyManager.firePressedQueue.Count > 0 && movementComponent.latencyManager.firePressedQueue.First().time <= System.DateTime.Now)
            {
                OnFirePressedServerRpc();
                movementComponent.latencyManager.firePressedQueue.Dequeue();
            }

            while (movementComponent.latencyManager.ADSPressedQueue.Count > 0 && movementComponent.latencyManager.ADSPressedQueue.First().time <= System.DateTime.Now)
            {
                StartADSServerRpc();
                movementComponent.latencyManager.ADSPressedQueue.Dequeue();
            }

            while (movementComponent.latencyManager.ADSReleasedQueue.Count > 0 && movementComponent.latencyManager.ADSReleasedQueue.First().time <= System.DateTime.Now)
            {
                StopADSServerRpc();
                movementComponent.latencyManager.ADSReleasedQueue.Dequeue();
            }

            bool isLeftLean = false;
            bool isRightLean = false;
            if (movementComponent.latencyManager.leanRightQueue.Count > 0 && movementComponent.latencyManager.leanRightQueue.First().time <= System.DateTime.Now)
            {
                isRightLean = true;
                movementComponent.latencyManager.leanRightQueue.Dequeue();
            }

            if (movementComponent.latencyManager.leanLeftQueue.Count > 0 && movementComponent.latencyManager.leanLeftQueue.First().time <= System.DateTime.Now)
            {
                isLeftLean = true;
                movementComponent.latencyManager.leanLeftQueue.Dequeue();
            }
            LeanServerRpc(isRightLean, isLeftLean);

        }

        private Quaternion desiredRotation;
        private Quaternion moveRotation;
        private float turnProgress = 1f;
        private bool isTurning = false;

        private void TurnInPlace()
        {
            float turnInput = _playerInput.x;
            _playerInput.x = Mathf.Clamp(_playerInput.x, -90f, 90f); // do -90 to 90 for the regular ik
            turnInput -= _playerInput.x;

            float sign = Mathf.Sign(_playerInput.x);
            if (Mathf.Abs(_playerInput.x) > turnInPlaceAngle)
            {
                if (!isTurning)
                {
                    turnProgress = 0f;

                    animator.ResetTrigger(TurnRight);
                    animator.ResetTrigger(TurnLeft);

                    animator.SetTrigger(sign > 0f ? TurnRight : TurnLeft);
                }

                isTurning = true;
            }

            transform.rotation *= Quaternion.Euler(0f, turnInput, 0f);

            float lastProgress = turnCurve.Evaluate(turnProgress);
            turnProgress += Time.deltaTime * turnSpeed;
            turnProgress = Mathf.Min(turnProgress, 1f);

            float deltaProgress = turnCurve.Evaluate(turnProgress) - lastProgress;

            _playerInput.x -= sign * turnInPlaceAngle * deltaProgress;

            transform.rotation *= Quaternion.Slerp(Quaternion.identity,
                Quaternion.Euler(0f, sign * turnInPlaceAngle, 0f), deltaProgress);

            if (Mathf.Approximately(turnProgress, 1f) && isTurning)
            {
                isTurning = false;
            }
        }

        private float _jumpState = 0f;


        private void UpdateLookInput()
        {
            if (!(IsOwner && isPlayerReady.Value && otherReady && !qoeEnabled.Value && !expQuesEnabled.Value)) return;
            //_freeLook = Input.GetKey(KeyCode.X);
            _freeLook = false;
            /*float deltaMouseX = Input.GetAxis("Mouse X") * sensitivity;
            float deltaMouseY = -Input.GetAxis("Mouse Y") * sensitivity;*/

            float deltaMouseX = 0;
            float deltaMouseY = 0;

            Pair lookPairs = new Pair();

            // X axis
            lookPairs.value = Input.GetAxis("Mouse X") * sensitivity;
            lookPairs.time = System.DateTime.Now.AddMilliseconds(movementComponent.latencyManager.playerLatency);
            movementComponent.latencyManager.lookXQueue.Enqueue(lookPairs);

            // Y axis
            lookPairs.value = -Input.GetAxis("Mouse Y") * sensitivity;
            lookPairs.time = System.DateTime.Now.AddMilliseconds(movementComponent.latencyManager.playerLatency);
            movementComponent.latencyManager.lookYQueue.Enqueue(lookPairs);


            while (movementComponent.latencyManager.lookXQueue.Count > 0 && movementComponent.latencyManager.lookXQueue.First().time <= System.DateTime.Now)
            {
                deltaMouseX = movementComponent.latencyManager.lookXQueue.Dequeue().value;
            }

            while (movementComponent.latencyManager.lookYQueue.Count > 0 && movementComponent.latencyManager.lookYQueue.First().time <= System.DateTime.Now)
            {
                deltaMouseY = movementComponent.latencyManager.lookYQueue.Dequeue().value;
            }



            if (_freeLook)
            {
                // No input for both controller and animation component. We only want to rotate the camera

                _freeLookInput.x += deltaMouseX;
                _freeLookInput.y += deltaMouseY;

                _freeLookInput.x = Mathf.Clamp(_freeLookInput.x, -freeLookAngle.x, freeLookAngle.x);
                _freeLookInput.y = Mathf.Clamp(_freeLookInput.y, -freeLookAngle.y, freeLookAngle.y);

                return;
            }

            /*_freeLookInput = Vector2.Lerp(_freeLookInput, Vector2.zero,
                FPSAnimLib.ExpDecayAlpha(15f, Time.deltaTime));*/

            _playerInput.x += deltaMouseX;
            _playerInput.y += deltaMouseY;

            float proneWeight = animator.GetFloat("ProneWeight");
            Vector2 pitchClamp = Vector2.Lerp(new Vector2(-90f, 90f), new Vector2(-30, 0f), proneWeight);

            _playerInput.y = Mathf.Clamp(_playerInput.y, pitchClamp.x, pitchClamp.y);
            moveRotation *= Quaternion.Euler(0f, deltaMouseX, 0f);
            TurnInPlace();

            _jumpState = Mathf.Lerp(_jumpState, movementComponent.IsInAir() ? 1f : 0f,
                FPSAnimLib.ExpDecayAlpha(10f, Time.deltaTime));

            float moveWeight = Mathf.Clamp01(movementComponent.AnimatorVelocity.magnitude);
            transform.rotation = Quaternion.Slerp(transform.rotation, moveRotation, moveWeight);
            transform.rotation = Quaternion.Slerp(transform.rotation, moveRotation, _jumpState);
            _playerInput.x *= 1f - moveWeight;
            _playerInput.x *= 1f - _jumpState;

            charAnimData.SetAimInput(_playerInput);
            charAnimData.AddDeltaInput(new Vector2(deltaMouseX, charAnimData.deltaAimInput.y));
        }

        private Quaternion lastRotation;

        private void OnDrawGizmos()
        {
            if (weaponBone != null)
            {
                Gizmos.DrawWireSphere(weaponBone.position, 0.03f);
            }
        }

        private Vector2 _cameraRecoilOffset;

        private void UpdateRecoil()
        {
            if (Mathf.Approximately(_controllerRecoil.magnitude, 0f)
                && Mathf.Approximately(_cameraRecoilOffset.magnitude, 0f))
            {
                return;
            }

            float smoothing = 8f;
            float restoreSpeed = 8f;
            float cameraWeight = 0f;

            if (GetGun().recoilPattern != null)
            {
                smoothing = GetGun().recoilPattern.smoothing;
                restoreSpeed = GetGun().recoilPattern.cameraRestoreSpeed;
                cameraWeight = GetGun().recoilPattern.cameraWeight;
            }

            _controllerRecoil = Vector2.Lerp(_controllerRecoil, Vector2.zero,
                FPSAnimLib.ExpDecayAlpha(smoothing, Time.deltaTime));

            _playerInput += _controllerRecoil * Time.deltaTime;

            Vector2 clamp = Vector2.Lerp(Vector2.zero, new Vector2(90f, 90f), cameraWeight);
            _cameraRecoilOffset -= _controllerRecoil * Time.deltaTime;
            _cameraRecoilOffset = Vector2.ClampMagnitude(_cameraRecoilOffset, clamp.magnitude);

            if (_isFiring) return;

            _cameraRecoilOffset = Vector2.Lerp(_cameraRecoilOffset, Vector2.zero,
                FPSAnimLib.ExpDecayAlpha(restoreSpeed, Time.deltaTime));
        }

        private void Update()
        {

            /*if (bulletTimerEnabled)
            {
                if(GameObject.FindGameObjectWithTag("Bullet")!=null)
                {
                    long clickToShootTime = ElapsedNanoseconds(bulletSpawnTime);

                    TextWriter textWriter = null;
                    if (playerID.Value == 0)
                        filenamePerRound = "Data\\Logs\\BulletClickToSpawnTime_" + fileNameSuffix + "_" + "PrimaryClient_" + playerID.Value + ".csv";
                    else
                        filenamePerRound = "Data\\Logs\\BulletClickToSpawnTime_" + fileNameSuffix + "_" + "ControlClient_" + playerID.Value + ".csv";

                    while (textWriter == null)
                        textWriter = File.AppendText(filenamePerRound);

                    String roundLogLine =
                       clickToShootTime.ToString();

                    textWriter.WriteLine(roundLogLine);
                    textWriter.Close();

                    bulletTimerEnabled = false;
                }

            }*/

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                //Application.Quit(0);
            }
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                isPlayerReady.Value = true;
            }

            if (IsOwner)
            {
                UpdateActionInput();
                UpdateLookInput();
                UpdateRecoil();
            }

            if (IsOwner && isPlayerReady.Value && otherReady && !qoeEnabled.Value && !expQuesEnabled.Value)
            {
                Time.timeScale = 1;
                if (toQuit)
                    Application.Quit();

                
                UpdateADTLineOfSight();
                LogPerTick();
                UpdateInvincibility();

                Vector3 distanceVector = transform.position - oldPosition;
                distanceTravelledPerRound += distanceVector.magnitude;
                distanceTravelledPersession += distanceVector.magnitude;
                oldPosition = transform.position;

                //Debug.Log("Shots hit" + shotsHitPerRound.Value);
            }
            else if (!isPlayerReady.Value || qoeEnabled.Value || expQuesEnabled.Value || !otherReady)
            {
                Time.timeScale = 0;
            }

            UpdatePlayerReady();

            

            if (_isFiring)
            {
                fireTimer -= Time.deltaTime;

                if (fireTimer < 0)
                {
                    Fire();
                }
            }
            else if (fireTimer > 0)
            {
                fireTimer -= Time.deltaTime;
            }

            //todo: add recoil here to the input
            
            UpdateAnimController();

            charAnimData.moveInput = movementComponent.AnimatorVelocity;
        }

        void UpdateInvincibility()
        {
            if (invincibilityTimer > 0)
            {
                invincibilityTimer -= Time.deltaTime;
                isInvincible = true;
                playerStats.currentHealth = this.playerStats.maxHealth;
                GetGun().currentAmmoCount = GetGun().magSize;
            }
            else
            {
                isInvincible = false;
            }

            if(killCooldown>0)
                killCooldown-= Time.deltaTime;

            if (regularHitCooldown > 0)
                regularHitCooldown -= Time.deltaTime;

            if (headshotCooldown > 0)
                headshotCooldown -= Time.deltaTime;

            if(takehitTimer>0)
                takehitTimer-= Time.deltaTime;
        }
        private void UpdateADTLineOfSight()
        {
            if (enemy == null)
            {
                GameObject[] localPlayerObjects = GameObject.FindGameObjectsWithTag("Player");

                foreach (var playerObj in localPlayerObjects)
                {
                    if (!playerObj.Equals(this.gameObject))
                        enemy = playerObj;
                }
            }
            else
            {
                RaycastHit hit;
                Transform enemyHeadTransform = enemy.GetComponent<FPSController>().playerHeadBone;
                if (Vector3.Distance(playerHeadBone.position, enemyHeadTransform.position) < ADTLOSRange)
                {
                    if (Physics.Raycast(playerHeadBone.position, (enemyHeadTransform.position - playerHeadBone.position), out hit, ADTLOSRange))
                    {
                        //Debug.Log("Rayhit:::" + hit.transform.name);
                        //Debug.Log("lat" + movementComponent.latencyManager.playerLatency);
                        if (hit.transform == enemyHeadTransform)
                        {
                            movementComponent.latencyManager.ADTEnabled = true;
                        }
                        else
                        {
                            movementComponent.latencyManager.ADTEnabled = false;
                        }
                    }
                }
                else
                {
                    movementComponent.latencyManager.ADTEnabled = false;
                }
            }

            if (movementComponent.latencyManager.ADTEnabled)
                durationWithADTOn += Time.deltaTime;
            else
                durationWithADTOff += Time.deltaTime;
        }

        public void UpdatePlayerReady()
        {
            

            if (enemy != null)
            {
                if(enemy.GetComponent<FPSController>().isPlayerReady.Value && !enemy.GetComponent<FPSController>().qoeEnabled.Value && !enemy.GetComponent<FPSController>().expQuesEnabled.Value)
                {
                    //Debug.Log("op" + enemy.GetComponent<FPSController>().isPlayerReady.Value + "qqq" + enemy.GetComponent<FPSController>().qoeEnabled.Value + "exx" + enemy.GetComponent<FPSController>().expQuesEnabled.Value);
                    otherReady = true;
                }
                    
                else
                    otherReady = false;
            }
            else
            {
                otherReady = false;

                GameObject[] localPlayerObjects = GameObject.FindGameObjectsWithTag("Player");

                foreach (var playerObj in localPlayerObjects)
                {
                    if (!playerObj.Equals(this.gameObject))
                        enemy = playerObj;
                }
            }

            if (qoeEnabled.Value || expQuesEnabled.Value || !otherReady)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public void UpdateCameraRotation()
        {
            Vector2 input = _playerInput;
            input += _cameraRecoilOffset;

            (Quaternion, Vector3) cameraTransform =
                (transform.rotation * Quaternion.Euler(input.y, input.x, 0f),
                    firstPersonCamera.position);

            cameraHolder.rotation = cameraTransform.Item1;
            cameraHolder.position = cameraTransform.Item2;

            mainCamera.rotation = cameraHolder.rotation * Quaternion.Euler(_freeLookInput.y, _freeLookInput.x, 0f);
        }

        public override void OnNetworkSpawn()
        {
            SpawnPoints sp = transform.GetComponent<SpawnPoints>();

            float dist = -1;

            Vector3 spawnPoint = sp.spawnPoints[Random.Range(0, sp.spawnPoints.Length - 1)];

            if (enemy != null)
            {
                for (int i = 0; i < sp.spawnPoints.Length; i++)
                {
                    if (Vector3.Distance(sp.spawnPoints[i], enemy.transform.position) > dist)
                    {
                        dist = Vector3.Distance(sp.spawnPoints[i], enemy.transform.position);
                        spawnPoint = sp.spawnPoints[i];
                    }
                }
            }
            
            this.GetComponent<CharacterController>().enabled = false;
            this.transform.SetPositionAndRotation(spawnPoint, Quaternion.identity);

            // Look at wo rotating weirdly

            Vector3 target = sp.centerPoint;
            Vector3 targetPos = new Vector3(target.x, transform.position.y, target.z);
            transform.LookAt(targetPos);

            this.GetComponent<CharacterController>().enabled = true;
        }

        [ServerRpc(RequireOwnership =false)]
        public void RespawnPlayerServerRpc()
        {
            RespawnPlayerClientRpc();
        }

        [ClientRpc]
        public void RespawnPlayerClientRpc()
        {
            RespawnPlayer();
        }
        public void RespawnPlayer()
        {
            if (isInvincible)
                return;

            enemy.GetComponent<FPSController>().AddToKillsServerRpc();
            enemy.GetComponent<FPSController>().AddToScoreServerRpc(10);

            isInvincible = true;
            invincibilityTimer = 1.5f;

            SpawnPoints sp = transform.GetComponent<SpawnPoints>();

            float dist = -1;

            Vector3 spawnPoint = sp.spawnPoints[Random.Range(0, sp.spawnPoints.Length - 1)];

            if (enemy != null)
            {
                for (int i = 0; i < sp.spawnPoints.Length; i++)
                {
                    if (Vector3.Distance(sp.spawnPoints[i], enemy.transform.position) > dist)
                    {
                        dist = Vector3.Distance(sp.spawnPoints[i], enemy.transform.position);
                        spawnPoint = sp.spawnPoints[i];
                    }
                }
            }

            this.GetComponent<CharacterController>().enabled = false;
            //this.GetComponent<FPSMovement>().enabled = false;

            this.transform.SetPositionAndRotation(spawnPoint, Quaternion.identity);

            // Look at wo rotating weirdly
            //NetworkManager.ConnectedClients[cleintID].PlayerObject.GetComponent<FPSController>().transform.position = spawnPoint;

            Vector3 target = sp.centerPoint;
            Vector3 targetPos = new Vector3(target.x, transform.position.y, target.z);
            this.transform.LookAt(targetPos);

            this.GetComponent<CharacterController>().enabled = true;

            
        }

        [ServerRpc(RequireOwnership = false)]
        public void RespawnOnlyPlayerServerRpc()
        {
            RespawnOnlyPlayerClientRpc();
        }

        [ClientRpc]
        public void RespawnOnlyPlayerClientRpc()
        {
            RespawnOnlyPlayer();
        }
        public void RespawnOnlyPlayer()
        {
            if (isInvincible)
                return;
            isInvincible = true;
            invincibilityTimer = 1.5f;

            SpawnPoints sp = transform.GetComponent<SpawnPoints>();

            Vector3 spawnPoint = sp.spawnPoints[Random.Range(0, sp.spawnPoints.Length - 1)];

            if (NetworkManager.LocalClientId==0)
                spawnPoint = sp.spawnPoints[0];
            else
                spawnPoint = sp.spawnPoints[3];


            this.GetComponent<CharacterController>().enabled = false;
            this.transform.SetPositionAndRotation(spawnPoint, Quaternion.identity);

            // Look at wo rotating weirdly
            //NetworkManager.ConnectedClients[cleintID].PlayerObject.GetComponent<FPSController>().transform.position = spawnPoint;

            Vector3 target = sp.centerPoint;
            Vector3 targetPos = new Vector3(target.x, transform.position.y, target.z);
            transform.LookAt(targetPos);

            this.GetComponent<CharacterController>().enabled = true;
        }
    

        [ServerRpc (RequireOwnership =false)]
        public void AddToScoreServerRpc(int value)
        {
            AddToScoreClientRpc(value);
        }

        [ClientRpc]
        public void AddToScoreClientRpc(int value)
        {
          
            if (value == 5)
            {
                PlayHeadshotSFX();
                if (headshotCooldown <= 0)
                { 
                    headshotsHitPerRound++;
                    headshotsHitPerSession++;
                    shotsHitPerRound++;
                    shotsHitPerSession++;
                    score += value;
                }
                headshotCooldown = .23f;
                
            }

            else if (value == 1)
            {
                PlayHitRegSFX();
                if (regularHitCooldown <= 0)
                {
                    shotsHitPerRound++;
                    shotsHitPerSession++;
                    score += value;
                }
                regularHitCooldown = .13f;
            }

            
            
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddToKillsServerRpc()
        {
            AddToKillsClientRpc();
        }
        [ClientRpc]
        public void AddToKillsClientRpc()
        {
            if (killCooldown <= 0)
            {
                roundKills++;
                score += 10;
                killCooldown = 0.3f;
                PlayKillSFX();
            }
        }



        // Log Manager
        public void LogPerTick()
        {

            if (!IsOwner || !IsClient || !isPlayerReady.Value || !otherReady) return;

            PlayerStats stats = GetComponent<PlayerStats>();

            TextWriter textWriter = null;

            if(playerID.Value ==0)
                filenamePerTick = "Data\\Logs\\ClientDataPerTick_" + fileNameSuffix + "_" + "PrimaryClient_" + playerID.Value + ".csv";
            else
                filenamePerTick = "Data\\Logs\\ClientDataPerTick_" + fileNameSuffix + "_" + "ControlClient_" + playerID.Value + ".csv";

            while (textWriter == null)
                textWriter = File.AppendText(filenamePerTick);

            float accuracy = 0;
            if (shotsFiredPerRound > 0)
                accuracy = (float)shotsHitPerRound / (float)shotsFiredPerRound;

            String tickLogLine =
                movementComponent.latencyManager.currentRoundNumber.ToString() + "," +
                NetworkManager.Singleton.LocalClientId + "," +
                System.DateTime.Now.ToString() + "," +
                movementComponent.latencyManager.currentSessionTimer.Value + "," +
                movementComponent.latencyManager.currentRoundTimer.Value + "," +
                this.transform.position.ToString() + "," +
                this.transform.rotation.ToString() + "," +
                enemy.transform.position.ToString() + "," +
                enemy.transform.rotation.ToString() + "," +
                movementComponent.latencyManager.ADTEnabled + "," +
                movementComponent.latencyManager.playerLatency + "," +
                movementComponent.latencyManager.lowLatency + "," +
                movementComponent.latencyManager.highLatency + "," +
                movementComponent.latencyManager.ADTKickInStepMagnitude + "," +
                movementComponent.latencyManager.ADTLetOffStepMagnitude + "," +
                movementComponent.latencyManager.ADTKickInTimer + "," +
                movementComponent.latencyManager.ADTLetOffTimer + "," +
                movementComponent.MovementState.ToString() + "," +
                movementComponent.PoseState.ToString() + "," +
                shotsFiredPerRound + "," +
                shotsHitPerRound + "," +
                shotsHitPerSession + "," +
                headshotsHitPerRound + "," +
                headshotsHitPerSession + "," +
                accuracy.ToString() + "," +
                this.score + "," +
                stats.currentHealth + "," +
                roundKills + "," +
                roundDeaths + "," +
                distanceTravelledPerRound + "," +
                distanceTravelledPersession + "," +
                headshotCooldown + "," +
                killCooldown + "," +
                regularHitCooldown + "," +
                durationWithADTOff + "," +
                durationWithADTOn + ","+
                invincibilityTimer + "," +
                leftLean + "," +
                rightLean
                ;
            //Debug.Log("LOG :: " + tickLogLine);
            textWriter.WriteLine(tickLogLine);
            textWriter.Close();

    }

        public void LogPerRound()
        {
            if (!IsOwner || !IsClient) return;

            PlayerStats stats = GetComponent<PlayerStats>();

            TextWriter textWriter = null;
            if (playerID.Value == 0)
                filenamePerRound = "Data\\Logs\\ClientDataPerRound_" + fileNameSuffix + "_" + "PrimaryClient_"+playerID.Value + ".csv";
            else
                filenamePerRound = "Data\\Logs\\ClientDataPerRound_" + fileNameSuffix + "_" + "ControlClient_" + playerID.Value + ".csv";

            while (textWriter == null)
                textWriter = File.AppendText(filenamePerRound);

            float accuracy = 0;
            if (shotsFiredPerRound > 0)
                accuracy = (float)shotsHitPerRound / (float)shotsFiredPerRound;

            String roundLogLine =
               movementComponent.latencyManager.currentRoundNumber.ToString() + "," +
               NetworkManager.Singleton.LocalClientId + "," +
               System.DateTime.Now.ToString() + "," +
               movementComponent.latencyManager.currentSessionTimer.Value + "," +
               movementComponent.latencyManager.lowLatency + "," +
               movementComponent.latencyManager.highLatency + "," +
               movementComponent.latencyManager.ADTKickInStepMagnitude + "," +
               movementComponent.latencyManager.ADTLetOffStepMagnitude + "," +
               movementComponent.latencyManager.shuffleIndex + "," +
               movementComponent.latencyManager.totalRoundNumber + "," +
               this.score + "," +
               stats.currentHealth + "," +
               shotsFiredPerRound + "," +
               shotsHitPerRound + "," +
               shotsHitPerSession + "," +
               headshotsHitPerRound + "," +
               headshotsHitPerSession + "," +
               accuracy.ToString() + "," +
               roundKills + "," +
               roundDeaths + "," +
               qoeValue + "," +
               expQuesValue + "," +
               distanceTravelledPerRound + "," +
               distanceTravelledPersession + "," +
               durationWithADTOff + "," +
               durationWithADTOn
                ;
            textWriter.WriteLine(roundLogLine);
            textWriter.Close();
        }
        [ServerRpc(RequireOwnership = false)]
        public void EnableQOEServerRpc()
        {
            EnableQOEClientRpc();
        }
        [ClientRpc]
        public void EnableQOEClientRpc()
        {
            qoeEnabled.Value= true;
        }

        [ServerRpc(RequireOwnership = false)]
        public void ResetRoundVarsServerRpc()
        {
            ResetRoundVarsClientRpc();
        }
        [ClientRpc]
        public void ResetRoundVarsClientRpc()
        {
            RespawnPlayerClientRpc();
            distanceTravelledPerRound = 0;
            shotsFiredPerRound = 0;
            shotsHitPerRound = 0;
            headshotsHitPerRound = 0;
            roundKills = 0;
            roundDeaths = 0;
            score = 0;
            durationWithADTOn = 0;
            durationWithADTOff = 0;
            this.gameObject.GetComponent<LatencyManager>().ResetCurrentRoundTimerClientRpc();
        }
        public static long ElapsedNanoseconds(long startTimestamp)
        {
            long now = ProfilerUnsafeUtility.Timestamp;
            var conversionRatio = ProfilerUnsafeUtility.TimestampToNanosecondsConversionRatio;
            return (now - startTimestamp) * conversionRatio.Numerator / conversionRatio.Denominator;
        }
    }
}