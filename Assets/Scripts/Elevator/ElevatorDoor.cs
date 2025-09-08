using System.Collections;
using UnityEngine;

public enum DoorSide { Left, Right }

public class ElevatorDoor : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    [Header("Door Leafs (both optional)")]
    [SerializeField] private Transform leftLeaf;
    [SerializeField] private Transform rightLeaf;

    [Header("Local Offsets (open delta from closed)")]
    [SerializeField] private Vector3 leftOpenLocalOffset = new Vector3(-0.5f, 0f, 0f);
    [SerializeField] private Vector3 rightOpenLocalOffset = new Vector3(0.5f, 0f, 0f);

    [Header("Timing")]
    [SerializeField] private float openSeconds = 0.8f;
    [SerializeField] private float closeSeconds = 0.8f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 _leftClosedLocal;
    private Vector3 _rightClosedLocal;
    private bool _isOpenLeft = false;
    private bool _isOpenRight = false;

    private void Awake()
    {
        if (leftLeaf) _leftClosedLocal = leftLeaf.localPosition;
        if (rightLeaf) _rightClosedLocal = rightLeaf.localPosition;
        if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} ready (L={(bool)leftLeaf}, R={(bool)rightLeaf})");
    }

    public IEnumerator Co_OpenSide(DoorSide side)
    {
        if (side == DoorSide.Left)
        {
            if (!leftLeaf)
            {
                if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} OPEN-LEFT skipped (no leftLeaf)");
                yield break;
            }
            if (_isOpenLeft)
            {
                if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} OPEN-LEFT ignored (already open)");
                yield break;
            }
            if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} OPEN-LEFT start (t={openSeconds:F2})");
            _isOpenLeft = true;
            float t = 0f;
            while (t < openSeconds)
            {
                t += Time.deltaTime;
                float k = ease.Evaluate(Mathf.Clamp01(t / openSeconds));
                leftLeaf.localPosition = Vector3.Lerp(_leftClosedLocal, _leftClosedLocal + leftOpenLocalOffset, k);
                yield return null;
            }
            leftLeaf.localPosition = _leftClosedLocal + leftOpenLocalOffset;
            if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} OPEN-LEFT done");
        }
        else
        {
            if (!rightLeaf)
            {
                if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} OPEN-RIGHT skipped (no rightLeaf)");
                yield break;
            }
            if (_isOpenRight)
            {
                if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} OPEN-RIGHT ignored (already open)");
                yield break;
            }
            if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} OPEN-RIGHT start (t={openSeconds:F2})");
            _isOpenRight = true;
            float t = 0f;
            while (t < openSeconds)
            {
                t += Time.deltaTime;
                float k = ease.Evaluate(Mathf.Clamp01(t / openSeconds));
                rightLeaf.localPosition = Vector3.Lerp(_rightClosedLocal, _rightClosedLocal + rightOpenLocalOffset, k);
                yield return null;
            }
            rightLeaf.localPosition = _rightClosedLocal + rightOpenLocalOffset;
            if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} OPEN-RIGHT done");
        }
    }

    public IEnumerator Co_CloseSide(DoorSide side)
    {
        if (side == DoorSide.Left)
        {
            if (!leftLeaf)
            {
                if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} CLOSE-LEFT skipped (no leftLeaf)");
                yield break;
            }
            if (!_isOpenLeft)
            {
                if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} CLOSE-LEFT ignored (already closed)");
                yield break;
            }
            if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} CLOSE-LEFT start (t={closeSeconds:F2})");
            _isOpenLeft = false;
            float t = 0f;
            while (t < closeSeconds)
            {
                t += Time.deltaTime;
                float k = ease.Evaluate(Mathf.Clamp01(t / closeSeconds));
                leftLeaf.localPosition = Vector3.Lerp(_leftClosedLocal + leftOpenLocalOffset, _leftClosedLocal, k);
                yield return null;
            }
            leftLeaf.localPosition = _leftClosedLocal;
            if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} CLOSE-LEFT done");
        }
        else
        {
            if (!rightLeaf)
            {
                if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} CLOSE-RIGHT skipped (no rightLeaf)");
                yield break;
            }
            if (!_isOpenRight)
            {
                if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} CLOSE-RIGHT ignored (already closed)");
                yield break;
            }
            if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} CLOSE-RIGHT start (t={closeSeconds:F2})");
            _isOpenRight = false;
            float t = 0f;
            while (t < closeSeconds)
            {
                t += Time.deltaTime;
                float k = ease.Evaluate(Mathf.Clamp01(t / closeSeconds));
                rightLeaf.localPosition = Vector3.Lerp(_rightClosedLocal + rightOpenLocalOffset, _rightClosedLocal, k);
                yield return null;
            }
            rightLeaf.localPosition = _rightClosedLocal;
            if (debugLogs) Debug.Log($"[ELEV/DOOR] {name} CLOSE-RIGHT done");
        }
    }

    // 필요 시 양쪽 동시 API도 유지
    public IEnumerator Co_Open()
    {
        var l = Co_OpenSide(DoorSide.Left);
        var r = Co_OpenSide(DoorSide.Right);
        if (leftLeaf) StartCoroutine(l);
        if (rightLeaf) yield return StartCoroutine(r);
        else if (leftLeaf) yield return StartCoroutine(l);
    }

    public IEnumerator Co_Close()
    {
        var l = Co_CloseSide(DoorSide.Left);
        var r = Co_CloseSide(DoorSide.Right);
        if (leftLeaf) StartCoroutine(l);
        if (rightLeaf) yield return StartCoroutine(r);
        else if (leftLeaf) yield return StartCoroutine(l);
    }
}
