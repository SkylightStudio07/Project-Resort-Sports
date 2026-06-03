using ResortSports.Jogging;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRInputDevices = UnityEngine.XR.InputDevices;
using XRInputFeatureUsageBool = UnityEngine.XR.InputFeatureUsage<bool>;

namespace ResortSports.Jogging
{
    /// <summary>
    /// 조깅 미니게임의 진행을 총괄. 트랙/플레이어/입력/UI를 와이어링하고
    /// 시작·종료·일시정지·통계(경과시간, 평균속도)를 관리한다.
    /// 씬에 하나만 배치하면 된다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class JoggingGameManager : MonoBehaviour
    {
        public enum GameState { Idle, Running, Finished }
        public enum XRButtonUsage
        {
            TriggerButton,
            GripButton,
            MenuButton,
            Primary2DAxisClick,
            PrimaryButton,
            SecondaryButton
        }

        [Header("Wiring")]
        public JogTrack track;
        public JoggingPlayerController player;
        public VRShakeJogInput shakeInput;
        public SpeedBillboardUI speedUI;

        [Header("Rules")]
        [Tooltip("이 거리(m)에 도달하면 종료. 0이면 트랙 전체 길이를 사용.")]
        public float targetDistance = 0f;

        [Tooltip("자동으로 시작할지 여부")]
        public bool autoStart = false;

        [Header("Interaction")]
        [Tooltip("조깅 시작 인터랙션 위치. 비워두면 트랙의 첫 지점을 사용합니다.")]
        public Transform startInteractionPoint;

        [Tooltip("이 거리 안에서 시작 입력을 누르면 조깅을 시작합니다.")]
        [Range(0.25f, 10f)] public float interactionDistance = 2f;

        [Tooltip("테스트용 키보드 시작 입력을 허용합니다.")]
        public bool allowKeyboardStart = true;
        public Key keyboardStartKey = Key.E;

        [Tooltip("VR 컨트롤러 입력으로 시작합니다. Vive wand는 기본적으로 Trigger Button을 사용합니다.")]
        public bool allowXRStart = true;
        public XRButtonUsage xrStartButton = XRButtonUsage.TriggerButton;

        [Tooltip("러닝 중 키보드 포기 입력을 허용합니다.")]
        public bool allowKeyboardCancel = true;
        public Key keyboardCancelKey = Key.Escape;

        [Tooltip("러닝 중 VR 컨트롤러 입력으로 포기합니다. Vive wand는 기본적으로 Grip Button을 사용합니다.")]
        public bool allowXRCancel = true;
        public XRButtonUsage xrCancelButton = XRButtonUsage.GripButton;

        [Header("Events")]
        public UnityEvent onStarted;
        public UnityEvent onFinished;

        public GameState State { get; private set; } = GameState.Idle;
        public float ElapsedTime { get; private set; }
        public float AverageSpeed =>
            ElapsedTime > 0.01f && player != null ? player.CurrentDistance / ElapsedTime : 0f;

        private float _goalDistance;
        private bool _wasXRStartPressed;
        private bool _wasXRCancelPressed;
        private static readonly List<XRInputDevice> _inputDevices = new List<XRInputDevice>();

        private void Start()
        {
            if (player != null) player.StopRun();
            if (autoStart) BeginRun();
        }

        public void BeginRun()
        {
            if (State == GameState.Running) return;
            if (track == null || player == null)
            {
                Debug.LogWarning("[JoggingGameManager] track/player가 비어 있어 시작할 수 없습니다.");
                return;
            }
            _goalDistance = targetDistance > 0f ? targetDistance : track.TotalLength;
            player.BeginRun();
            ElapsedTime = 0f;
            State = GameState.Running;
            onStarted?.Invoke();
        }

        public void EndRun()
        {
            if (State != GameState.Running) return;
            if (player != null) player.StopRun();
            State = GameState.Finished;
            onFinished?.Invoke();
        }

        private void Update()
        {
            if (State != GameState.Running)
            {
                TryBeginRunFromInteraction();
                return;
            }

            ElapsedTime += Time.deltaTime;

            if (IsCancelPressed())
            {
                EndRun();
                return;
            }

            if (player != null && _goalDistance > 0f && player.CurrentDistance >= _goalDistance)
                EndRun();
        }

        private void TryBeginRunFromInteraction()
        {
            if (!IsPlayerNearStart()) return;
            if (IsStartPressed()) BeginRun();
        }

        public bool IsPlayerNearStart()
        {
            if (player == null || player.XrOrigin == null || track == null) return false;

            Vector3 startPosition = startInteractionPoint != null
                ? startInteractionPoint.position
                : track.GetPositionAtDistance(0f);

            Vector3 playerPosition = player.XrOrigin.position;
            startPosition.y = 0f;
            playerPosition.y = 0f;
            return Vector3.Distance(playerPosition, startPosition) <= interactionDistance;
        }

        private bool IsStartPressed()
        {
            if (allowKeyboardStart && WasKeyboardKeyPressed(keyboardStartKey)) return true;
            if (!allowXRStart) return false;

            bool pressed = TryGetXRButton(ToFeatureUsage(xrStartButton));
            bool pressedThisFrame = pressed && !_wasXRStartPressed;
            _wasXRStartPressed = pressed;
            return pressedThisFrame;
        }

        private bool IsCancelPressed()
        {
            if (allowKeyboardCancel && WasKeyboardKeyPressed(keyboardCancelKey)) return true;
            if (!allowXRCancel) return false;

            bool pressed = TryGetXRButton(ToFeatureUsage(xrCancelButton));
            bool pressedThisFrame = pressed && !_wasXRCancelPressed;
            _wasXRCancelPressed = pressed;
            return pressedThisFrame;
        }

        private static bool WasKeyboardKeyPressed(Key key)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;

            KeyControl keyControl = keyboard[key];
            return keyControl != null && keyControl.wasPressedThisFrame;
        }

        private static XRInputFeatureUsageBool ToFeatureUsage(XRButtonUsage button)
        {
            switch (button)
            {
                case XRButtonUsage.TriggerButton:
                    return XRCommonUsages.triggerButton;
                case XRButtonUsage.GripButton:
                    return XRCommonUsages.gripButton;
                case XRButtonUsage.MenuButton:
                    return XRCommonUsages.menuButton;
                case XRButtonUsage.Primary2DAxisClick:
                    return XRCommonUsages.primary2DAxisClick;
                case XRButtonUsage.PrimaryButton:
                    return XRCommonUsages.primaryButton;
                case XRButtonUsage.SecondaryButton:
                    return XRCommonUsages.secondaryButton;
                default:
                    return XRCommonUsages.triggerButton;
            }
        }

        private static bool TryGetXRButton(XRInputFeatureUsageBool usage)
        {
            _inputDevices.Clear();
            XRInputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.HeldInHand | UnityEngine.XR.InputDeviceCharacteristics.Controller,
                _inputDevices);

            foreach (XRInputDevice device in _inputDevices)
            {
                if (device.isValid && device.TryGetFeatureValue(usage, out bool pressed) && pressed)
                    return true;
            }

            return false;
        }
    }
}
