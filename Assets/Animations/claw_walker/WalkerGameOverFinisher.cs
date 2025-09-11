using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI; // ★ UI 오버레이용

public class WalkerGameOverFinisher : MonoBehaviour, IGameOverFinisher
{
    [Header("Lock & Focus")]
    [SerializeField] private Transform playerLockPoint;
    [SerializeField] private Transform cameraFocus;

    [Header("Killzone")]
    [SerializeField] private Transform killzoneAnchor;

    [Header("Camera Path")]
    [SerializeField] private Transform airCameraAnchor;
    [SerializeField] private Transform followMount;
    [SerializeField, Range(0f, 1f)] private float attachAtNormalized = 0.55f;
    [SerializeField] private Vector3 followLocalOffset = Vector3.zero;

    [Header("Timing / Zoom")]
    [SerializeField] private float preDelaySeconds = 2.0f; // 딜레이 동안 whoosh만 재생
    [SerializeField] private float jumpTime = 0.8f;        // 점프 전체 길이
    [SerializeField] private float zoomFov = 35f;
    [SerializeField] private float zoomTime = 0.25f;

    [Header("Animation/FX")]
    [SerializeField] private Animator walkerAnimator;
    [SerializeField] private string jumpTrigger = "Jump";

    [Header("SFX Sources & Clips")]
    [SerializeField] private AudioSource sfx;              // 연출 전용 소스 (whoosh/임팩트)
    [SerializeField] private AudioClip whoosh;             // 딜레이 동안 루프(플레이어 불안 호흡)
    [SerializeField, Range(0f, 1f)] private float whooshVolume = 1f;
    [SerializeField] private float whooshFadeOutSeconds = 0.2f; // 딜레이 끝 페이드아웃
    [SerializeField] private AudioClip postJumpClip;       // 점프 중간(절반) 시점에 재생
    [SerializeField, Range(0f, 1f)] private float postJumpVolume = 1f;

    [Header("PostJump Fade")]
    [SerializeField] private float postJumpFadeSeconds = 0.6f;
    [SerializeField] private bool forceFadeEvenIfShared = false;

    [Header("Post SFX Timing")]
    [SerializeField, Range(0f, 1f)] private float postClipNormalizedTime = 0.5f; // jumpTime * 이 값에 재생(기본 50%)

    [Header("Whisper (AI/Audio)")]
    [SerializeField] private WalkerAI walker;                 // 없으면 GetComponent
    [SerializeField] private bool fadeOutRageScreamOnEntry = true;
    [SerializeField] private float screamFadeSeconds = 0.6f;

    [Header("Breathing Mute During Killcam")]
    [Tooltip("킬캠 동안 음소거할 오디오소스들(예: whisper의 기본 breathing). 비우면 loop된 소스를 자동 탐색/음소거.")]
    [SerializeField] private AudioSource[] killcamMuteSources;
    [SerializeField] private bool autoMuteLoopedSourcesIfEmpty = true;
    [SerializeField] private float breathingFadeSeconds = 0.3f;

    [Header("Killcam Light Control")]
    [Tooltip("킬캠 동안 비활성화할 whisper의 불빛들(예: 두 개의 Light).")]
    [SerializeField] private Light[] killcamDisableLights;
    [SerializeField] private bool restoreLightsAfter = false; // 필요 시 끝에 원복
    private bool[] _lightEnabledBackup;

    // ★ 최종 게임오버 화면/사운드
    [Header("Final Game Over (after jump)")]
    [Tooltip("jumpTime이 끝난 뒤 추가로 기다릴 시간(초)")]
    [SerializeField] private float gameOverTime = 1.5f;
    [SerializeField] private CanvasGroup gameOverOverlayGroup;   // 전체 화면을 덮는 CanvasGroup
    [SerializeField] private Image gameOverOverlayImage;         // 색상을 줄 Image(풀스크린)
    [SerializeField] private Color gameOverColor = new Color(1f, 0.2f, 0.2f, 1f); // 하양+빨강 느낌
    [SerializeField] private float gameOverOverlayFade = 0.2f;   // 오버레이 페이드 시간
    [SerializeField] private AudioSource gameOverSfx;            // (옵션) 전용 오디오소스
    [SerializeField] private AudioClip gameOverClip;             // 최종 SFX
    [SerializeField, Range(0f, 1f)] private float gameOverVolume = 1f;
    [SerializeField] private float gameOverClipEndSeconds = -1f; // -1 이면 강제 종료 안 함
[SerializeField] private float gameOverClipFadeOutSeconds = 0.6f;

// 같은 AudioSource를 공유(sfx) 중이어도 강제로 끌지 여부(권장: 별도 gameOverSfx 사용)
[SerializeField] private bool forceEndGameOverEvenIfShared = false;


    [Header("Restore Camera")]
    [SerializeField] private bool restoreCameraAfter = false;

    public string ReasonId => "CaughtByWalker";

    public IEnumerator Play(FirstPersonController player)
    {

        if (gameOverOverlayGroup)
        {
            gameOverOverlayGroup.alpha = 0f;
            gameOverOverlayGroup.gameObject.SetActive(false);
        }
        if (gameOverOverlayImage)
        {
            gameOverOverlayImage.enabled = true; // ⭐ 항상 켜두기
                                                 // 필요 시 색도 선반영
            gameOverOverlayImage.color = new Color(gameOverColor.r, gameOverColor.g, gameOverColor.b, 1f);
        }

        // 0) 플레이어 이동계 잠금
        var cc = player.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        // 1) Killzone 이동(회전 포함)
        if (killzoneAnchor)
        {
            if (TryGetComponent<NavMeshAgent>(out var agent))
            {
                agent.isStopped = true; agent.ResetPath(); agent.velocity = Vector3.zero;
#if UNITY_2021_3_OR_NEWER
                agent.Warp(killzoneAnchor.position);
#else
                transform.position = killzoneAnchor.position;
#endif
            }
            else transform.position = killzoneAnchor.position;

            transform.rotation = killzoneAnchor.rotation;
        }

        // 1.1) 킬캠 동안 whisper 불빛 끄기
        DisableKillcamLights();

        // 1.5) 진입 즉시 비명 페이드아웃
        if (fadeOutRageScreamOnEntry)
        {
            if (!walker) walker = GetComponent<WalkerAI>();
            if (walker) walker.FadeOutRageScream(screamFadeSeconds);
        }

        // 1.6) 킬캠 동안 whisper의 루프/브리딩 음소거 (sfx 제외)
        StartCoroutine(MuteBreathingCo());

        // 2) 플레이어를 락포인트로 스냅(락포인트 자체는 변경 X)
        if (playerLockPoint)
        {
            player.transform.position = playerLockPoint.position;
            player.transform.rotation = playerLockPoint.rotation;
        }

        // 3) 카메라 백업 + 공중 앵커에 부착(처음엔 화면 비움)
        var cam = player.PlayerCamera;
        Transform originalParent = null;
        Vector3 originalLocalPos = Vector3.zero;
        Quaternion originalLocalRot = Quaternion.identity;
        Quaternion originalWorldRot = Quaternion.identity;
        float originalFov = 60f;

        if (cam)
        {
            originalParent = cam.transform.parent;
            originalLocalPos = cam.transform.localPosition;
            originalLocalRot = cam.transform.localRotation;
            originalWorldRot = cam.transform.rotation;
            originalFov = cam.fieldOfView;

            if (airCameraAnchor)
            {
                cam.transform.SetParent(airCameraAnchor, false);
                cam.transform.localPosition = Vector3.zero;
                cam.transform.localRotation = Quaternion.identity;
            }
        }

        // 3.5) 딜레이 시작 전에 'whoosh'(불안 호흡)만 루프 재생
        if (sfx && whoosh)
        {
            sfx.loop = true;
            sfx.clip = whoosh;
            sfx.volume = whooshVolume;
            sfx.mute = false;
            sfx.Play();
        }

        // 4) 어둠의 틈: preDelaySeconds 만큼 대기 (이 동안 whoosh만 들림)
        if (preDelaySeconds > 0f)
            yield return new WaitForSeconds(preDelaySeconds);

        // 4.5) whoosh 페이드아웃/정지
        if (sfx && whoosh && sfx.isPlaying)
            yield return StartCoroutine(FadeOutAndStop(sfx, whooshFadeOutSeconds));

        // 5) 점프 트리거
        if (walkerAnimator && !string.IsNullOrEmpty(jumpTrigger))
        {
            walkerAnimator.ResetTrigger(jumpTrigger);
            walkerAnimator.SetTrigger(jumpTrigger);
        }

        // 점프 시작과 동시에 post SFX를 "jumpTime * postClipNormalizedTime" 시점에 재생 예약
        float total = Mathf.Max(0.001f, jumpTime);
        if (sfx && postJumpClip)
            StartCoroutine(PlaySfxAfterDelay(sfx, postJumpClip, postJumpVolume,
                total * Mathf.Clamp01(postClipNormalizedTime)));

        // 6) attach 시점까지 대기 → 카메라를 whisper에 '부착'
        float attachDelay = Mathf.Clamp01(attachAtNormalized) * total;
        yield return new WaitForSeconds(attachDelay);

        if (cam)
        {
            Transform mount = followMount ? followMount : (cameraFocus ? cameraFocus : transform);
            cam.transform.SetParent(mount, false);
            cam.transform.localPosition = followLocalOffset;

            if (cameraFocus && mount != cameraFocus)
            {
                cam.transform.rotation = Quaternion.LookRotation(
                    (cameraFocus.position - cam.transform.position).normalized, Vector3.up);
            }
            else cam.transform.localRotation = Quaternion.identity;
        }

        // 7) 붙자마자 FOV 보간(선택)
        if (cam && zoomTime > 0f)
        {
            float startFov = cam.fieldOfView;
            float t = 0f; float dur = Mathf.Max(0.001f, zoomTime);
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                cam.fieldOfView = Mathf.Lerp(startFov, zoomFov, t);
                yield return null;
            }
            cam.fieldOfView = zoomFov;
        }

        // 8) 남은 점프 시간 대기(착지까지)
        float remain = total - attachDelay;
        if (remain > 0f) yield return new WaitForSeconds(remain);

        // 9) (선택) 약간의 정적
        yield return new WaitForSeconds(0.2f);

        // ★ 10) jumpTime 이후 → gameOverTime 추가 대기 → 오버레이/사운드 실행
        if (gameOverTime > 0f) yield return new WaitForSeconds(gameOverTime);
        var (usedSrc, usedClip) = TriggerFinalGameOverOverlayAndSfx();
        StartCoroutine(FadeOutPostJumpSafely());

        if (gameOverClipEndSeconds > 0f)
            yield return new WaitForSeconds(gameOverClipEndSeconds);

        // 11) 카메라 복원(원한다면) — 오버레이가 화면을 덮으므로 복원해도 보기에 영향 없음
        if (restoreCameraAfter && cam)
        {
            cam.fieldOfView = originalFov;
            cam.transform.rotation = originalWorldRot;
            cam.transform.SetParent(originalParent, false);
            cam.transform.localPosition = originalLocalPos;
            cam.transform.localRotation = originalLocalRot;
        }

        // 11.5) 불빛 원복(옵션)
        if (restoreLightsAfter)
            RestoreKillcamLights();

        // WalkerGameOverFinisher.cs - Play(...) 코루틴 마지막 부분
        var playerFpc = player; // 이미 인자로 받음
        if (playerFpc != null)
        {
            // 연출 끝나고 0.5초 후 _Last 복귀 (원하면 조절)
            playerFpc.TriggerGameOverReturn(0.5f);
        }
        UnmuteKillcamSources();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 루프/브리딩 음소거 (sfx 제외)
    private IEnumerator MuteBreathingCo()
    {
        bool anyExplicit = false;
        if (killcamMuteSources != null && killcamMuteSources.Length > 0)
        {
            anyExplicit = true;
            foreach (var src in killcamMuteSources)
            {
                if (!src) continue;
                if ((sfx && src == sfx) || (gameOverSfx && src == gameOverSfx)) continue;
                yield return StartCoroutine(FadeOutAndMute(src, breathingFadeSeconds));
            }
        }

        if (!anyExplicit && autoMuteLoopedSourcesIfEmpty)
        {
            var all = GetComponentsInChildren<AudioSource>(true);
            var coros = new List<Coroutine>();
            foreach (var a in all)
            {
                if (!a) continue;
                if ((sfx && a == sfx) || (gameOverSfx && a == gameOverSfx)) continue;
                if (!a.loop) continue;
                coros.Add(StartCoroutine(FadeOutAndMute(a, breathingFadeSeconds)));
            }
            foreach (var co in coros) yield return co;
        }
    }

    private IEnumerator FadeOutAndMute(AudioSource src, float seconds)
    {
        if (!src) yield break;
        float dur = Mathf.Max(0.001f, seconds);
        float startVol = src.volume;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            src.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        src.mute = true;
        src.volume = startVol; // 복구 대비
    }

    private IEnumerator FadeOutAndStop(AudioSource src, float seconds)
    {
        if (!src) yield break;
        float dur = Mathf.Max(0.001f, seconds);
        float startVol = src.volume;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            src.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        if (src.isPlaying) src.Stop();
        src.loop = false;
        src.volume = startVol;
    }

    private IEnumerator PlaySfxAfterDelay(AudioSource src, AudioClip clip, float volume, float delaySeconds)
    {
        if (!src || !clip) yield break;
        yield return new WaitForSeconds(Mathf.Max(0f, delaySeconds));
        if (!src.mute) src.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Killcam 동안 whisper 불빛 끄기/복원
    private void DisableKillcamLights()
    {
        if (killcamDisableLights == null || killcamDisableLights.Length == 0) return;

        if (_lightEnabledBackup == null || _lightEnabledBackup.Length != killcamDisableLights.Length)
            _lightEnabledBackup = new bool[killcamDisableLights.Length];

        for (int i = 0; i < killcamDisableLights.Length; i++)
        {
            var l = killcamDisableLights[i];
            if (!l) continue;
            _lightEnabledBackup[i] = l.enabled;
            l.enabled = false;
        }
    }

    private void RestoreKillcamLights()
    {
        if (killcamDisableLights == null || _lightEnabledBackup == null) return;

        for (int i = 0; i < killcamDisableLights.Length && i < _lightEnabledBackup.Length; i++)
        {
            var l = killcamDisableLights[i];
            if (!l) continue;
            l.enabled = _lightEnabledBackup[i];
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 최종 게임오버: 화면 대체 + 전용 SFX
    private (AudioSource usedSrc, AudioClip usedClip) TriggerFinalGameOverOverlayAndSfx()
    {
        if (gameOverOverlayGroup)
        {
            // ⭐ 혹시 모를 외부 끔상태를 재차 방지
            if (gameOverOverlayImage) gameOverOverlayImage.enabled = true;

            gameOverOverlayGroup.gameObject.SetActive(true);
            if (gameOverOverlayImage) gameOverOverlayImage.color = gameOverColor;
            StopCoroutineSafe(_overlayFadeCo);
            _overlayFadeCo = StartCoroutine(FadeOverlayTo(gameOverOverlayGroup, 1f, gameOverOverlayFade));
        }

        // 전용 사운드
        var src = gameOverSfx ? gameOverSfx : sfx; // 전용 소스 우선, 없으면 sfx
        AudioClip usedClip = null;

        if (src && gameOverClip && !src.mute)
        {
            src.PlayOneShot(gameOverClip, Mathf.Clamp01(gameOverVolume));
            usedClip = gameOverClip;

            // (옵션) 강제 종료 스케줄은 그대로 유지
            if (gameOverClipEndSeconds > 0f)
                StartCoroutine(EndGameOverClipAfterDelay(src, gameOverClipEndSeconds, gameOverClipFadeOutSeconds));
        }

        return (src, usedClip);
    }

    private Coroutine _overlayFadeCo;
    private void StopCoroutineSafe(Coroutine co) { if (co != null) StopCoroutine(co); }

    private IEnumerator FadeOverlayTo(CanvasGroup group, float targetAlpha, float seconds)
    {
        if (!group) yield break;
        float dur = Mathf.Max(0.001f, seconds);
        float start = group.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            group.alpha = Mathf.Lerp(start, targetAlpha, t);
            yield return null;
        }
        group.alpha = targetAlpha;
    }

    private IEnumerator FadeOutPostJumpSafely()
    {
        // postJumpClip을 sfx로 재생해 왔다는 전제
        if (sfx == null || postJumpClip == null) yield break;

        // 같은 AudioSource(sfx)로 gameOverClip도 재생 중이면, 페이드가 최종 사운드까지 깎을 수 있음
        bool sharedWithGameOver = (gameOverSfx == null || gameOverSfx == sfx);

        if (sharedWithGameOver && !forceFadeEvenIfShared)
            yield break; // 안전상 스킵 (gameOverSfx를 분리해 연결하면 페이드 가능)

        yield return StartCoroutine(FadeOutAndStop(sfx, postJumpFadeSeconds));
    }

    private IEnumerator EndGameOverClipAfterDelay(AudioSource src, float endDelay, float fadeSeconds)
    {
        if (!src) yield break;

        // 지정 시간 대기
        yield return new WaitForSeconds(Mathf.Max(0f, endDelay));

        // 동일 소스 공유 시(= gameOverSfx 미지정이거나 sfx와 동일) 의도치 않은 다른 소리까지 깎일 수 있음
        bool sharedWithOtherSfx = (gameOverSfx == null || gameOverSfx == sfx);
        if (sharedWithOtherSfx && !forceEndGameOverEvenIfShared)
            yield break; // 안전상 스킵(전용 gameOverSfx 사용 권장)

        // 부드럽게 0으로 페이드 후 정지
        yield return StartCoroutine(FadeOutAndStop(src, fadeSeconds));
    }

    private void UnmuteKillcamSources()
    {
        var all = GetComponentsInChildren<AudioSource>(true);
        foreach (var a in all)
        {
            if (!a) continue;
            if (gameOverSfx && a == gameOverSfx) continue; // 게임오버 전용 소스는 제외(원하면 제외 안 해도 OK)
            if (sfx && a == sfx) continue;                  // 연출 전용 sfx는 제외
            a.mute = false;                                 // 🔓 핵심: 언뮤트
        }
    }

}
