using System.Collections;
using UnityEngine;

public enum ElevatorFloor { BF = 0, F1 = 1, F2 = 2 }

public class ElevatorController : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    // === 외부에서 "받아오기"만 함 ===
    private bool isElected = false;
    private bool isElevatorOn = false;

    // 퍼즐/스위치의 UnityEvent<bool>에 연결
    public void OnElectedChanged(bool value)
    {
        isElected = value;
        if (debugLogs) Debug.Log($"[ELEV][POWER] {name} OnElectedChanged => {value} (IsPowered={IsPowered()})");
        if (!value) EmergencyStopAndClose("ELECTED=false");
    }
    public void OnElevatorOnChanged(bool value)
    {
        isElevatorOn = value;
        if (debugLogs) Debug.Log($"[ELEV][POWER] {name} OnElevatorOnChanged => {value} (IsPowered={IsPowered()})");
        if (!value) EmergencyStopAndClose("ELEVATOR_ON=false");
    }

    [Header("Setup")]
    [SerializeField] private Transform cabinRoot;
    [SerializeField] private Transform[] floorAnchors = new Transform[3]; // BF/F1/F2
    [SerializeField] private ElevatorDoor cabinDoor;                      // 좌/우 leaf 포함
    [SerializeField] private ElevatorDoor[] floorDoors = new ElevatorDoor[3]; // 층 복도문(좌/우 leaf 포함)

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float stopThreshold = 0.01f;
    [SerializeField] private float doorOpenTimeAtArrive = 0.5f;

    [Header("Door Dwell")]
    [SerializeField] private float doorCloseDelayAfterKeypad = 0.4f;
    [SerializeField] private float doorOpenStaySeconds = 2.0f;

    [Header("Audio (Single Source)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clipDoorOpen;
    [SerializeField] private AudioClip clipDoorClose;
    [SerializeField] private AudioClip clipMoveLoop;
    [SerializeField] private AudioClip clipButton;

    // ===== 플레이어 방식 라이트(3 파라미터) =====
    [Header("Light (player-style: intensity, range, time)")]
    [SerializeField] private Light pointLight;
    [SerializeField] private float lightTargetIntensity = 2.0f; // 목표 밝기
    [SerializeField] private float lightTargetRange = 6.0f;     // 목표 범위
    [SerializeField] private float lightTime = 0.6f;            // 1사이클 시간(버스트 총시간/루프 주기)

    private float _baseIntensity, _baseRange;
    private Coroutine _lightRoutine;

    public ElevatorFloor CurrentFloor => _currentFloor;
    public bool IsBusy => _state != State.Idle;

    private enum State { Idle, DoorsOpening, DoorsClosing, Moving }
    private State _state = State.Idle;
    private ElevatorFloor _currentFloor = ElevatorFloor.BF;
    private Coroutine _moveRoutine;

    private void Awake()
    {
        if (cabinRoot && floorAnchors[0])
        {
            var p = cabinRoot.position;
            p.y = floorAnchors[0].position.y;
            cabinRoot.position = p;
        }
        _currentFloor = ElevatorFloor.BF;

        if (pointLight)
        {
            _baseIntensity = pointLight.intensity;
            _baseRange = pointLight.range;
            pointLight.intensity = _baseIntensity;
            pointLight.range = _baseRange;
        }

        if (debugLogs)
        {
            Debug.Log($"[ELEV][INIT] {name} Init at {FloorName(_currentFloor)} y={cabinRoot.position.y:F3}");
            Debug.Log($"[ELEV][INIT] Anchors => BF:{Y(0)}  F1:{Y(1)}  F2:{Y(2)}");
        }
    }

    // ===== 외부 명령 (키패드/호출 버튼) =====
    public void RequestFloor(ElevatorFloor targetFloor)
    {
        if (debugLogs) Debug.Log($"[ELEV][INPUT] Keypad.RequestFloor({FloorName(targetFloor)}) "
                               + $"state={_state} curr={FloorName(_currentFloor)} powered={IsPowered()}");

        if (!IsPowered()) { PlayOneShot(clipButton); return; }
        if (_state == State.Moving || targetFloor == _currentFloor) return;

        PlayOneShot(clipButton);

        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        _moveRoutine = StartCoroutine(Co_CloseDoorsAndMove(targetFloor));
    }

    public void CallElevator(ElevatorFloor floor)
    {
        if (debugLogs) Debug.Log($"[ELEV][INPUT] CallElevator({FloorName(floor)}) "
                               + $"state={_state} curr={FloorName(_currentFloor)} powered={IsPowered()}");

        if (!IsPowered()) { PlayOneShot(clipButton); return; }
        if (_state == State.Moving) return;

        if (floor == _currentFloor)
        {
            if (_state == State.Idle)
            {
                if (debugLogs) Debug.Log("[ELEV][DOOR] Same floor call → Open doors (pairwise)");
                if (_moveRoutine != null) StopCoroutine(_moveRoutine);
                _moveRoutine = StartCoroutine(Co_OpenBothDoorsPairwise(_currentFloor));
            }
            return;
        }

        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        _moveRoutine = StartCoroutine(Co_MoveToFloorAndOpen(floor));
    }

    // ===== Core =====
    private IEnumerator Co_CloseDoorsAndMove(ElevatorFloor targetFloor)
    {
        if (!IsPowered()) yield break;
        _state = State.DoorsClosing;

        if (debugLogs) Debug.Log($"[ELEV][DOOR] Close before move (delay={doorCloseDelayAfterKeypad:F2}s)");
        yield return new WaitForSeconds(doorCloseDelayAfterKeypad);

        yield return StartCoroutine(Co_CloseBothDoorsPairwise(_currentFloor));

        _state = State.Moving;
        if (debugLogs) Debug.Log($"[ELEV][MOVE] Start moving {FloorName(_currentFloor)} → {FloorName(targetFloor)}");

        StartMoveLoop();   // 오디오 루프 + 라이트 루프(3파라미터)

        var dst = floorAnchors[(int)targetFloor].position;
        yield return StartCoroutine(Co_MoveCabinTo(dst));

        StopMoveLoop();    // 루프 종료(라이트 복원)
        _currentFloor = targetFloor;
        if (debugLogs) Debug.Log($"[ELEV][MOVE] Arrived {FloorName(_currentFloor)} y={dst.y:F3}");

        yield return new WaitForSeconds(doorOpenTimeAtArrive);
        yield return StartCoroutine(Co_OpenBothDoorsPairwise(_currentFloor));

        if (debugLogs) Debug.Log($"[ELEV][DOOR] Doors stay open for {doorOpenStaySeconds:F2}s");
        yield return new WaitForSeconds(doorOpenStaySeconds);

        _state = State.Idle;
        if (debugLogs) Debug.Log("[ELEV][STATE] Idle");
    }

    private IEnumerator Co_MoveToFloorAndOpen(ElevatorFloor targetFloor)
    {
        if (!IsPowered()) yield break;

        if (_state != State.DoorsClosing && _state != State.Moving)
        {
            if (debugLogs) Debug.Log("[ELEV][DOOR] Close current floor doors before travel (call)");
            yield return StartCoroutine(Co_CloseBothDoorsPairwise(_currentFloor));
        }

        _state = State.Moving;
        if (debugLogs) Debug.Log($"[ELEV][MOVE] Start moving (call) {FloorName(_currentFloor)} → {FloorName(targetFloor)}");

        StartMoveLoop();

        var dst = floorAnchors[(int)targetFloor].position;
        yield return StartCoroutine(Co_MoveCabinTo(dst));

        StopMoveLoop();
        _currentFloor = targetFloor;
        if (debugLogs) Debug.Log($"[ELEV][MOVE] Arrived (call) {FloorName(_currentFloor)} y={dst.y:F3}");

        yield return new WaitForSeconds(doorOpenTimeAtArrive);
        yield return StartCoroutine(Co_OpenBothDoorsPairwise(_currentFloor));

        if (debugLogs) Debug.Log($"[ELEV][DOOR] Doors stay open for {doorOpenStaySeconds:F2}s");
        yield return new WaitForSeconds(doorOpenStaySeconds);

        _state = State.Idle;
        if (debugLogs) Debug.Log("[ELEV][STATE] Idle");
    }

    private IEnumerator Co_MoveCabinTo(Vector3 targetPos)
    {
        if (!cabinRoot) yield break;

        while (Vector3.Distance(cabinRoot.position, targetPos) > stopThreshold)
        {
            if (!IsPowered()) { if (debugLogs) Debug.Log("[ELEV][ABORT] Power lost during movement"); yield break; }
            float step = moveSpeed * Time.deltaTime;
            cabinRoot.position = Vector3.MoveTowards(cabinRoot.position, targetPos, step);
            yield return null;
        }
        cabinRoot.position = targetPos;
    }

    // ===== Pairwise door sync =====
    private IEnumerator Co_OpenBothDoorsPairwise(ElevatorFloor floor)
    {
        _state = State.DoorsOpening;
        if (debugLogs) Debug.Log($"[ELEV][DOOR] OPEN (pairwise) @ {FloorName(floor)}");
        PlayOneShot(clipDoorOpen);

        var hall = floorDoors[(int)floor];

        Coroutine r1 = null, r2 = null;
        if (cabinDoor) r1 = StartCoroutine(cabinDoor.Co_OpenSide(DoorSide.Right));
        if (hall) r2 = StartCoroutine(hall.Co_OpenSide(DoorSide.Right));
        if (r2 != null) yield return r2; else if (r1 != null) yield return r1;

        Coroutine l1 = null, l2 = null;
        if (cabinDoor) l1 = StartCoroutine(cabinDoor.Co_OpenSide(DoorSide.Left));
        if (hall) l2 = StartCoroutine(hall.Co_OpenSide(DoorSide.Left));
        if (l2 != null) yield return l2; else if (l1 != null) yield return l1;

        _state = State.Idle;
    }

    private IEnumerator Co_CloseBothDoorsPairwise(ElevatorFloor floor)
    {
        _state = State.DoorsClosing;
        if (debugLogs) Debug.Log($"[ELEV][DOOR] CLOSE (pairwise) @ {FloorName(floor)}");
        PlayOneShot(clipDoorClose);

        var hall = floorDoors[(int)floor];

        Coroutine r1 = null, r2 = null;
        if (cabinDoor) r1 = StartCoroutine(cabinDoor.Co_CloseSide(DoorSide.Right));
        if (hall) r2 = StartCoroutine(hall.Co_CloseSide(DoorSide.Right));
        if (r2 != null) yield return r2; else if (r1 != null) yield return r1;

        Coroutine l1 = null, l2 = null;
        if (cabinDoor) l1 = StartCoroutine(cabinDoor.Co_CloseSide(DoorSide.Left));
        if (hall) l2 = StartCoroutine(hall.Co_CloseSide(DoorSide.Left));
        if (l2 != null) yield return l2; else if (l1 != null) yield return l1;

        _state = State.Idle;
    }

    // ===== Audio + Light (3파라미터) =====
    private void StartMoveLoop()
    {
        if (audioSource && clipMoveLoop)
        {
            if (debugLogs) Debug.Log("[ELEV/AUDIO] Start move loop");
            audioSource.loop = true;
            audioSource.clip = clipMoveLoop;
            audioSource.Play();
        }
        StartLightLoop(); // 이동 중 라이트 루프 시작
    }

    private void StopMoveLoop()
    {
        if (audioSource)
        {
            if (debugLogs) Debug.Log("[ELEV/AUDIO] Stop move loop");
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = null;
        }
        StopLightLoop(); // 라이트 복원
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (audioSource && clip)
        {
            if (debugLogs) Debug.Log($"[ELEV/AUDIO] OneShot {clip.name}");
            audioSource.PlayOneShot(clip);
        }
        // 이동 중(라이트 루프 가동 상태)이면 버스트는 생략해 충돌 방지
        if (_lightRoutine == null) StartCoroutine(Co_LightBurst());
        else if (debugLogs) Debug.Log("[ELEV/LIGHT] Burst skipped (loop active)");
    }

    // === Player-style Light: intensity, range, time ===
    private void StartLightLoop()
    {
        if (!pointLight) return;
        if (_lightRoutine != null) StopCoroutine(_lightRoutine);
        if (debugLogs) Debug.Log($"[ELEV/LIGHT] Loop start (I={lightTargetIntensity}, R={lightTargetRange}, T={lightTime})");
        _lightRoutine = StartCoroutine(Co_LightLoopWhileAudio());
    }

    private void StopLightLoop()
    {
        if (_lightRoutine != null) StopCoroutine(_lightRoutine);
        _lightRoutine = null;
        ResetLightToBase();
        if (debugLogs) Debug.Log("[ELEV/LIGHT] Loop stop → reset");
    }

    private IEnumerator Co_LightLoopWhileAudio()
    {
        // 오디오 재생 여부와 무관하게 이동 상태 동안 주기적으로 핑퐁
        float t = 0f;
        while (_state == State.Moving)
        {
            t += Time.deltaTime;
            float k = (lightTime <= 0.0001f) ? 1f : Mathf.PingPong(t, lightTime) / lightTime; // 0..1..0
            ApplyLightLerp(k);
            yield return null;
        }
        ResetLightToBase();
        _lightRoutine = null;
    }

    private IEnumerator Co_LightBurst()
    {
        if (!pointLight || lightTime <= 0f) yield break;
        if (debugLogs) Debug.Log($"[ELEV/LIGHT] Burst (I={lightTargetIntensity}, R={lightTargetRange}, T={lightTime})");

        float half = lightTime * 0.5f;
        float t = 0f;

        // Up
        while (t < half)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / half); // 0→1
            ApplyLightLerp(k);
            yield return null;
        }

        // Down
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / half); // 1→0
            ApplyLightLerp(k);
            yield return null;
        }

        ResetLightToBase();
    }

    private void ApplyLightLerp(float k01)
    {
        if (!pointLight) return;
        pointLight.intensity = Mathf.Lerp(_baseIntensity, lightTargetIntensity, k01);
        pointLight.range = Mathf.Lerp(_baseRange, lightTargetRange, k01);
    }

    private void ResetLightToBase()
    {
        if (!pointLight) return;
        pointLight.intensity = _baseIntensity;
        pointLight.range = _baseRange;
    }

    // ===== Helpers =====
    private bool IsPowered() => isElected && isElevatorOn;

    private void EmergencyStopAndClose(string reason)
    {
        if (debugLogs) Debug.Log($"[ELEV][EMERGENCY] STOP & CLOSE (reason={reason}) state={_state} curr={FloorName(_currentFloor)}");
        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        StopMoveLoop(); // 오디오/라이트 루프 모두 정리
        if (gameObject.activeInHierarchy)
            StartCoroutine(Co_CloseBothDoorsPairwise(_currentFloor));
    }

    private string FloorName(ElevatorFloor f) => f.ToString();
    private string Y(int idx) => floorAnchors != null && floorAnchors.Length > idx && floorAnchors[idx]
        ? floorAnchors[idx].position.y.ToString("F3") : "null";
}
