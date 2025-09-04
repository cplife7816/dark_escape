using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using LowPolyWater;

public class FirstPersonController : MonoBehaviour
{
    public bool CanMove { get; set; } = true;

    private bool IsSprinting => canSprint && Input.GetKey(sprintKey);
    private bool ShouldJump => Input.GetKeyDown(jumpKey) && characterController.isGrounded;
    private bool ShouldCrouch => Input.GetKeyDown(crouchKey) && !duringCrouchAnimation && characterController.isGrounded;

    [Header("Functional Options")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool useFootsteps = true;

    [Header("Controls")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private float crouchSpeed = 1.5f;


    [Header("Look Parameters")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 180)] private float upperLooklimit = 80.0f;
    [SerializeField, Range(1, 180)] private float lowerLooklimit = 80.0f;

    [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float gravity = 30.0f;

    [Header("Crouch Parameters")]
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standingHeight = 1.5f;
    [SerializeField] private float timeToCrouch = 0.25f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 standingCenter = new Vector3(0, 0, 0);
    private bool IsCrouching;
    private bool duringCrouchAnimation;

    private float defaultYpos = 0;
    private float timer;

    [Header("Footstep Parameters")]
    [SerializeField] private float baseStepSpeed = 0.5f;
    [SerializeField] private float crouchStepMultipler = 1.5f;
    [SerializeField] private float sprintStepMultipler = 0.6f;
    [SerializeField] private AudioSource footstepAudioSource = default;
    [SerializeField] private AudioClip[] rockClips = default;
    [SerializeField] private AudioClip[] metalClips = default;
    [SerializeField] private AudioClip[] grassClips = default;
    [SerializeField] private AudioClip[] waterClips = default;
    [SerializeField] private AudioClip[] rockLandClips = default;
    [SerializeField] private AudioClip[] metalLandClips = default;
    [SerializeField] private AudioClip[] grassLandClips = default;
    [SerializeField] private AudioClip[] waterLandClips = default;
    [SerializeField] private AudioClip[] rockJumpClips = default;
    [SerializeField] private AudioClip[] metalJumpClips = default;
    [SerializeField] private AudioClip[] grassJumpClips = default;
    [SerializeField] private AudioClip[] waterJumpClips = default;
    [SerializeField] private GameObject leftFootprintPrefab;
    [SerializeField] private GameObject rightFootprintPrefab;

    [Header("Footprint Position Offset")]
    [SerializeField] private Vector3 leftFootprintOffset = new Vector3(-0.1f, 0f, 0f);
    [SerializeField] private Vector3 rightFootprintOffset = new Vector3(0.1f, 0f, 0f);


    public bool lefted = false;

    [Header("Light Settings")]
    [SerializeField] private Light pointLight;
    [SerializeField] private float maxLightRange = 8f;
    [SerializeField] private float pointLightIntensity = 7f;
    [SerializeField] private float run = 2f;
    [SerializeField] private float crouch = -2f;

    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionDistance = 3f;

    [Header("Glass Effects Settings")]
    [SerializeField] private float redDuration = 5f;

    [SerializeField] private float blueDuration = 5f;
    [SerializeField] private float blueRangeBonus = 3f;

    [SerializeField] private float greenDuration = 5f;
    [SerializeField] private float speedPenalty = 3f;

    [SerializeField] private float yellowDuration = 5f;
    [SerializeField] private float yellowRangePenalty = 6f;

    [SerializeField] private float pinkDuration = 5f;

    [Header("Glass Effect Audio")]
    [SerializeField] private AudioSource glassEffectAudioSource;

    [SerializeField] private AudioClip yellowEffectClip;
    [SerializeField] private AudioClip pinkEffectClip;

    private bool isLightLocked = false; // 💡 분홍 병 효과 중인지 여부

    public float extraRange = 0f;

    private Coroutine lightCoroutine; 

    private float footstepTimer = 0;
    private float GetCurrentOffset => IsCrouching ? baseStepSpeed * crouchStepMultipler : IsSprinting ? baseStepSpeed * sprintStepMultipler : baseStepSpeed;

    private Camera playerCamera;
    private CharacterController characterController;

    private Vector3 moveDirection;
    private Vector2 currentInput;

    private bool checkLanding = false;

    private float rotationX = 0;

    [SerializeField] private Transform holdPosition;
    private GameObject heldObject;
    private bool isHoldingItem = false;

    [SerializeField] private Image interactIcon;
    [SerializeField] private Sprite doorIcon;
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private Sprite playIcon;
    [SerializeField] private Sprite lockIcon;
    [SerializeField] private Sprite defaultIcon;



    [SerializeField] private TextMeshProUGUI subtitleTextUI;

    private float defaultHoldZ = 1.2f;      // 손의 기본 거리
    private float minHoldZ = 0.45f;          // 벽과 너무 가까울 때 최소 거리
    private float holdAdjustSpeed = 15f;    // 보간 속도
    private float heldObjectHoldX = 0f;
    private float heldObjectHoldY = 0f;
    private float heldObjectHoldZ = 1.2f; // 기본 거리


    private Coroutine redCoroutine;
    private Coroutine blueCoroutine;
    private Coroutine greenCoroutine;
    private Coroutine yellowCoroutine;
    private Coroutine pinkCoroutine;

    private float defaultWalkSpeed;
    private float defaultSprintSpeed;
    private float defaultCrouchSpeed;
    private float defaultMaxLightRange;

    [Header("Threat Tint (Enemy Encounter)")]
    [SerializeField] private Color threatTintColor = Color.red;   // 적대자 조우 시 목표 색
    [SerializeField] private float defaultThreatFade = 0.4f;

    private Coroutine threatTintCoroutine;                        // 적대자 틴트 코루틴
    private bool threatTintDesired = false;                       // 현재 적대자 틴트 유지 의도
    private bool IsRedGlassActive => redCoroutine != null;        // 유리병 '빨강' 효과 실행 중?

    [Header("Game Over")]
    [SerializeField] private CanvasGroup gameOverOverlay;  // 옵션(UI 페이드용, 없으면 null OK)
    [SerializeField] private float gameOverFadeSeconds = 1.0f;
    [SerializeField] private AudioSource sfxSource;        // 옵션(사운드)
    [SerializeField] private AudioClip gameOverClip;       // 옵션(사운드)
    [SerializeField] private UnityEvent onGameOver;        // 옵션(씬 전환/UI 등 외부 훅)

    public bool IsGameOver { get; private set; } = false;
    private Coroutine gameOverCo;

    [Header("Game Over Light Override")]
    [SerializeField] private float gameOverLightRange = 6f;
    [SerializeField] private float gameOverLightIntensity = 8f;
    [SerializeField] private float gameOverLightFadeIn = 0.15f;

    public bool IsPlayerCrouching => IsCrouching;

    private Quaternion heldLocalRotation = Quaternion.identity;
    private PickupOverride heldOverride = null;

 

void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        defaultYpos = playerCamera.transform.localPosition.y;

        if (pointLight != null)
        {
            pointLight.color = defaultLightColor;
            defaultMaxLightRange = maxLightRange; // ✅ 기본 라이트 범위 저장
        }

        defaultWalkSpeed = walkSpeed;
        defaultSprintSpeed = sprintSpeed;
        defaultCrouchSpeed = crouchSpeed;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }

    // Update is called once per frame
    void Update()
    {
        if (CanMove)
        {
            HandleMovementInput();
            HandleMouseLook();
            if (canJump) HandleJump();

            if (canCrouch) HandleCrouch();

            if (useFootsteps) HandleFootsteps();

            ApplyFinalMovements();

            HandleInteraction();
            HandleInteractionIcon();
            UpdateSubtitleText();
            AdjustHoldPosition(); 
        }
    }

    private void HandleMovementInput()
    {
        currentInput = new Vector2((IsCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Vertical"), (IsCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Horizontal"));

        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) + (transform.TransformDirection(Vector3.right) * currentInput.y);
        moveDirection.y = moveDirectionY;
    }

    private void HandleMouseLook()
    {
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;
        rotationX = Mathf.Clamp(rotationX, -upperLooklimit, lowerLooklimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

        float mouseX = Input.GetAxis("Mouse X") * lookSpeedX;
        transform.Rotate(0f, mouseX, 0f);
    }

    private void HandleJump()
    {
        if (ShouldJump)
        {
            moveDirection.y = jumpForce;
            checkLanding = true;
            if (Physics.Raycast(playerCamera.transform.position, Vector3.down, out RaycastHit hit, 3))
            {
                switch (hit.collider.tag)
                {
                    case "FOOTSTEPS/GRASS":
                        footstepAudioSource.PlayOneShot(grassJumpClips[Random.Range(0, grassJumpClips.Length - 1)]);
                        break;
                    case "FOOTSTEPS/ROCK":
                        footstepAudioSource.PlayOneShot(rockJumpClips[Random.Range(0, rockJumpClips.Length - 1)]);
                        break;
                    case "FOOTSTEPS/METAL":
                        footstepAudioSource.PlayOneShot(metalJumpClips[Random.Range(0, metalJumpClips.Length - 1)]);
                        break;
                    default:
                        footstepAudioSource.PlayOneShot(rockJumpClips[Random.Range(0, rockJumpClips.Length - 1)]);
                        break;
                }
            }
        }
    }

    private void HandleCrouch()
    {
        if (IsCrouching)
        {
            footstepAudioSource.volume = 0.3f;
            extraRange = crouch;
        }
        else
        {
            footstepAudioSource.volume = 1.0f;
            extraRange = 0f;
        }
        if (ShouldCrouch)
            StartCoroutine(CrouchStand());
            
    }


    private void HandleFootsteps()
    {
        if(!characterController.isGrounded) return;
        if (currentInput == Vector2.zero) return;

        footstepTimer -= Time.deltaTime;
        
        if(footstepTimer < 0)
        {
            if(Physics.Raycast(playerCamera.transform.position, Vector3.down, out RaycastHit hit, 4))
            {
                // lefted = footPrints(lefted);
                if (lightCoroutine != null)
                {
                    StopCoroutine(lightCoroutine); 
                }
                if (IsSprinting) extraRange = run;
                lightCoroutine = StartCoroutine(PulseLightEffect(3.0f , extraRange, pointLightIntensity, pointLight));

                switch (hit.collider.tag)
                {
                    case "FOOTSTEPS/GRASS":
                        footstepAudioSource.PlayOneShot(grassClips[Random.Range(0, grassClips.Length - 1)]);
                        break;
                    case "FOOTSTEPS/ROCK":
                        footstepAudioSource.PlayOneShot(rockClips[Random.Range(0, rockClips.Length - 1)]);
                        break;
                    case "FOOTSTEPS/METAL":
                        footstepAudioSource.PlayOneShot(metalClips[Random.Range(0, metalClips.Length - 1)]);
                        break;
                    case "FOOTSTEPS/WATER":
                        footstepAudioSource.PlayOneShot(waterClips[Random.Range(0, waterClips.Length - 1)]);
                        break;
                    default:
                        footstepAudioSource.PlayOneShot(rockClips[Random.Range(0, rockClips.Length - 1)]);
                        break;
                }
            }

            footstepTimer = GetCurrentOffset;
        }
    }

    private void ApplyFinalMovements()
    {
        if (!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private IEnumerator CrouchStand()
    {
        if(IsCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1f)) 
            yield break;
        duringCrouchAnimation = true;

        float timeElapsed = 0;
        float targetHeight = IsCrouching ? standingHeight : crouchHeight;
        float currentHeight = characterController.height;
        Vector3 targetCenter = IsCrouching ? standingCenter : crouchingCenter;
        Vector3 currentCenter = characterController.center;

        while(timeElapsed < timeToCrouch)
        {
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed / timeToCrouch);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed / timeToCrouch);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter;

        IsCrouching = !IsCrouching;

        duringCrouchAnimation = false;
    }

    private IEnumerator PulseLightEffect(float duration, float extraRange, float intensity, Light point)
    {
        if (point == null) yield break;
        if (isLightLocked) yield break;

        float halfDuration = duration / 2f;
        float timer = 0f;
        float startRange = point.range;
        float startIntensity = point.intensity;
        float targetRange = maxLightRange + extraRange;

        // Light 증가
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            point.range = Mathf.Lerp(startRange, targetRange, t);
            point.intensity = Mathf.Lerp(startIntensity, intensity, t);
            yield return null;
        }

        // Light 감소
        timer = 0f;
        startRange = point.range;
        startIntensity = point.intensity;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            point.range = Mathf.Lerp(startRange, 0f, t);
            point.intensity = Mathf.Lerp(startIntensity, 0f, t);
            yield return null;
        }

        point.range = 0f;
        point.intensity = 0f;
    }
/*
    private bool footPrints(bool lefted)
    {
        RaycastHit hit;
        Vector3 origin = playerCamera.transform.position;

        if (Physics.Raycast(origin, Vector3.down, out hit, 2f)) // Raycast to detect ground
        {
            GameObject footprintPrefab = lefted ? leftFootprintPrefab : rightFootprintPrefab;
            Vector3 offset = lefted ? leftFootprintOffset : rightFootprintOffset;

            // Preserve the original prefab's Y position and apply offset
            Vector3 spawnPosition = new Vector3(hit.point.x, footprintPrefab.transform.position.y, hit.point.z) + offset;

            // Instantiate with the original prefab's rotation
            GameObject footprint = Instantiate(footprintPrefab, spawnPosition, footprintPrefab.transform.rotation);

            // Ensure the clone is active
            footprint.SetActive(true);

            Destroy(footprint, 5f);
        }

        return !lefted;
    }
*/
    public float GetPointLightRange()
    {
        return pointLight.range;
    }

    private void HandleInteraction()
    {
        if (Input.GetKeyDown(interactKey))
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            int mask = ~LayerMask.GetMask("IgnorePlayerRay");

            if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance, mask))
            {
                if (isHoldingItem) DropItem();
                return;
            }

            GameObject target = hit.collider.gameObject;
            Debug.Log($"[Interaction] 대상: {target.name}, 태그: {target.tag}");

            bool interactionHandled = false;

            // ✅ 1. IItemSocket 조합 시도
            if (isHoldingItem && heldObject != null)
            {
                var socket = target.GetComponentInParent<IItemSocket>();
                if (socket != null)
                {
                    Debug.Log("[Interaction] IItemSocket 상호작용 시도");

                    bool shouldDrop = socket.TryInteract(heldObject);

                    Debug.Log($"[Interaction] TryInteract 결과: {(shouldDrop ? "드롭 필요" : "유지")}");

                    // ⛔ 무조건 DropItem() 실행하는 것이 문제 → 조건 분기
                    if (shouldDrop)
                    {
                        DropItem();
                    }

                    interactionHandled = true; // ✅ 무조건 처리로 간주
                }
            }

            // ✅ 2. 아이템이 없을 때만 TryInteractable 호출
            if (!interactionHandled && !isHoldingItem && target.CompareTag("Interact"))
            {
                if (target.TryGetComponent<ITryInteractable>(out var tryInteractable))
                {
                    tryInteractable.TryInteract();
                    interactionHandled = true;
                }
            }

            // ✅ 2. 문/창문 상호작용 (항상 허용)
            if (!interactionHandled && target.CompareTag("Door"))
            {
                if (target.TryGetComponent(out Door door))
                {
                    door.ToggleDoor();
                    interactionHandled = true;
                }
                else if (target.TryGetComponent(out WindowInteraction window))
                {
                    window.Interact();
                    interactionHandled = true;
                }
            }

            // ✅ 3. 카세트 플레이어
            if (!interactionHandled && isHoldingItem && heldObject != null &&
                heldObject.CompareTag("Cassette") && target.CompareTag("Cassette_Player"))
            {
                InsertCassette(target);
                interactionHandled = true;
            }

            // ✅ 4. 아이템 줍기
            if (!isHoldingItem && !interactionHandled)
            {
                TryPickupItem();
                interactionHandled = true;
            }

            // ✅ 5. 실패 시 드롭
            if (!interactionHandled && isHoldingItem)
            {
                Debug.Log("[Interaction] 상호작용 실패 → 드롭");
                DropItem();
            }
        }

        // ✅ 6. 들고 있는 아이템 위치 고정
        if (isHoldingItem && heldObject != null)
        {
            heldObject.transform.position = holdPosition.position;
            if (heldOverride != null)
                heldObject.transform.localRotation = heldLocalRotation;
        }
    }

    private void TryPickupItem()
    {
        RaycastHit hit;
        int mask = ~LayerMask.GetMask("IgnorePlayerRay");
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactionDistance, mask))
        {
            GameObject target = hit.collider.gameObject;

            if (target.CompareTag("Item") || target.CompareTag("Cassette"))
            {
                heldObject = target;

                if (heldObject.name == "ScrewDriver" || heldObject.CompareTag("Cassette"))
                {
                    heldObject.layer = LayerMask.NameToLayer("IgnorePlayerRay");
                }

                Rigidbody rb = heldObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;

                    // ✅ 잡을 때 Interpolate 활성화
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                }

                Collider col = heldObject.GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = false;
                }

                heldObject.transform.SetParent(holdPosition);

                // ✅ 회전 + 위치 보정 + 스케일 통합 처리
                if (heldObject.TryGetComponent(out PickupOverride overrideData))
                {
                    // 회전 처리
                    Quaternion itemLocalRot = (overrideData.customEulerRotation == Vector3.zero)
    ? Quaternion.identity
    : Quaternion.Euler(overrideData.customEulerRotation);
                    heldObject.transform.localRotation = itemLocalRot;

                    // ✅ 회전 고정 캐시
                    heldLocalRotation = itemLocalRot;
                    heldOverride = overrideData;

                    // 위치 보정값 적용
                    Vector3 offset = overrideData.holdOffset;
                    heldObjectHoldX = offset.x;
                    heldObjectHoldY = offset.y;
                    heldObjectHoldZ = offset.z != 0f ? offset.z : 1.2f;

                    holdPosition.localPosition = new Vector3(heldObjectHoldX, heldObjectHoldY, heldObjectHoldZ);

                    // 스케일 적용
                    overrideData.ApplyHeldScale();

                    Debug.Log($"[Pickup] Offset 적용: {offset}, Rotation: {(overrideData.customEulerRotation == Vector3.zero ? "원본 유지" : overrideData.customEulerRotation.ToString())}");
                }
                else
                {
                    heldObjectHoldX = 0f;
                    heldObjectHoldY = 0f;
                    heldObjectHoldZ = 1.2f;
                    holdPosition.localPosition = new Vector3(0f, 0f, heldObjectHoldZ);
                    heldObject.transform.localRotation = Quaternion.Inverse(holdPosition.rotation) * heldObject.transform.rotation;
                    Debug.Log($"[Pickup] Override 없음 → 기본 위치/회전 유지");
                }

                isHoldingItem = true;
            }
        }
    }



    private void DropItem()
    {
        if (heldObject != null)
        {
            heldObject.transform.parent = null;

            // ✅ 실제 드롭 위치를 '현재 holdPosition 위치 기준'으로 설정
            heldObject.transform.position = holdPosition.position;

            // Rigidbody 복구
            if (heldObject.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            // Collider 복구
            if (heldObject.TryGetComponent(out Collider col))
            {
                col.enabled = true;
            }

            if (heldObject.TryGetComponent(out PickupOverride overrideData))
            {
                overrideData.ApplyDroppedScale();
            }

            // ✅ 레이어 복구
            heldObject.layer = LayerMask.NameToLayer("Default");

            // ✅ 새로 추가: 드롭 이벤트 인터페이스 호출
            if (heldObject.TryGetComponent(out IDroppable droppable))
            {
                droppable.Dropped();
            }

            heldObject = null;
            isHoldingItem = false;

            // ✅ Drop 이후에 HoldPosition 복원
            Debug.Log($"[DropItem] holdPosition 복원 위치 z = {heldObjectHoldZ} (object drop)");
            holdPosition.localPosition = new Vector3(0f, 0f, heldObjectHoldZ);

        }
        heldOverride = null;
        heldLocalRotation = Quaternion.identity;
    }



    private void HandleInteractionIcon()
    {
        int mask = ~LayerMask.GetMask("IgnorePlayerRay");
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        Sprite newIcon = defaultIcon; // 기본값은 항상 흰 점

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, mask))
        {
            string tag = hit.collider.tag;

            if (heldObject != null && heldObject.CompareTag("Cassette") && tag == "Cassette_Player")
            {
                newIcon = playIcon;
            }
            else if (tag == "Item" || tag == "Cassette")
            {
                newIcon = itemIcon;
            }
            else if (tag == "Door")
            {
                if (hit.collider.TryGetComponent(out Door door))
                {
                    newIcon = door.isLocked ? lockIcon : doorIcon;
                }
                else if (hit.collider.TryGetComponent(out WindowInteraction window))
                {
                    newIcon = window.isLocked ? lockIcon : doorIcon;
                }
            }
            else if (tag == "Interact" && heldObject != null)
            {
                var socket = hit.collider.GetComponentInParent<IItemSocket>();
                if (socket != null && socket.CanInteract(heldObject))
                {
                    newIcon = itemIcon;
                }
            }
            else if (tag == "Interact")
            {
                newIcon = itemIcon;
            }
        }

        ShowInteractIcon(newIcon); // 항상 아이콘 보여줌
    }


    private void ShowInteractIcon(Sprite icon)
    {
        interactIcon.sprite = icon;
        interactIcon.gameObject.SetActive(true);
    }

    private void InsertCassette(GameObject cassettePlayer)
    {
        Transform insertPoint = cassettePlayer.transform.Find("Insert_Position");

        if (insertPoint != null && heldObject != null)
        {
            heldObject.transform.SetParent(insertPoint);
            heldObject.transform.localPosition = Vector3.zero;
            heldObject.transform.localRotation = Quaternion.identity;

            CassettePlayerController controller = cassettePlayer.GetComponent<CassettePlayerController>();
            if (controller != null)
            {
                Debug.Log("카세트 플레이 시작");
                controller.StartCassetteSequence(heldObject);
            }
            else
            {
                Debug.LogWarning("CassettePlayerController 없음");
            }

            heldObject = null;
            isHoldingItem = false;
        }
        else
        {
            Debug.LogWarning("Insert Position 또는 heldObject 없음");
        }
    }


    public void ReleaseHeldObjectIfMatch(GameObject target)
    {
        if (heldObject == target)
        {
            heldObject = null;
            isHoldingItem = false;
        }
    }

    public void ResetHoldPosition()
    {
        Debug.Log($"[ResetHoldPosition] holdPosition 복원 위치 z = {heldObjectHoldZ}");
        holdPosition.localPosition = new Vector3(0f, 0f, heldObjectHoldZ);
    }

    private void UpdateSubtitleText()
    {
        int mask = ~LayerMask.GetMask("IgnorePlayerRay");
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, mask))
        {
            SubtitleObject subtitleObject = hit.collider.GetComponentInParent<SubtitleObject>();
            if (subtitleObject != null)
            {
                subtitleTextUI.text = subtitleObject.GetSubtitleText();
                return;
            }
        }

        subtitleTextUI.text = ""; // 오브젝트를 바라보지 않으면 자막 비우기
    }

    public void PauseMovementFor(float duration)
    {
        StartCoroutine(PauseMovementCoroutine(duration));
    }

    private IEnumerator PauseMovementCoroutine(float duration)
    {
        CanMove = false;
        yield return new WaitForSeconds(duration);
        CanMove = true;
    }

    private void AdjustHoldPosition()
    {
        if (!isHoldingItem || holdPosition == null || heldObject == null) return;

        float defaultZ = heldObjectHoldZ;
        float targetZ = defaultZ;

        Vector3 boxHalfExtents;

        Renderer rend = heldObject.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            boxHalfExtents = rend.bounds.extents;
        }
        else
        {
            boxHalfExtents = new Vector3(0.15f, 0.15f, 0.25f);
            Debug.LogWarning($"[AdjustHoldPosition] Renderer 없음 → 기본 크기 사용 (object: {heldObject.name})");
        }

        Vector3 origin = playerCamera.transform.position;
        Vector3 dir = playerCamera.transform.forward;
        float checkDistance = defaultZ + 0.05f;

        if (Physics.BoxCast(origin, boxHalfExtents, dir, out RaycastHit hit, playerCamera.transform.rotation, checkDistance, ~LayerMask.GetMask("IgnorePlayerRay")))
        {
            float distance = hit.distance;
            targetZ = Mathf.Clamp(distance - 0.05f, minHoldZ, defaultZ);
        }

        float currentZ = holdPosition.localPosition.z;
        float newZ = Mathf.Lerp(currentZ, targetZ, Time.deltaTime * holdAdjustSpeed);

        holdPosition.localPosition = new Vector3(heldObjectHoldX, heldObjectHoldY, newZ);
    }

    private readonly Color defaultLightColor = Color.white; // ✅ 기본값을 흰색으로 고정

    public void ApplyGlassEffect(string materialName)
    {
        string lower = materialName.ToLower();

        if (lower.Contains("red"))
        {
            Debug.Log($"[GlassEffect] 🔴 빨강 효과 인식됨: {materialName}");
            RestartCoroutine(ref redCoroutine, RedLightEffectCoroutine());
        }
        else if (lower.Contains("green"))
        {
            Debug.Log($"[GlassEffect] 🟢 초록 효과 인식됨: {materialName}");
            RestartCoroutine(ref greenCoroutine, GreenSpeedPenaltyCoroutine());
        }
        else if (lower.Contains("blue"))
        {
            Debug.Log($"[GlassEffect] 🔵 파랑 효과 인식됨: {materialName}");
            RestartCoroutine(ref blueCoroutine, BlueRangeBoostCoroutine());
        }
        else if (lower.Contains("yellow"))
        {
            Debug.Log($"[GlassEffect] 🟡 노랑 효과 인식됨: {materialName}");
            RestartCoroutine(ref yellowCoroutine, YellowRangePenaltyCoroutine());
        }
        else if (lower.Contains("pink"))
        {
            Debug.Log($"[GlassEffect] 💖 분홍 효과 인식됨: {materialName}");
            RestartCoroutine(ref pinkCoroutine, PinkLockLightCoroutine());
        }
        else
        {
            Debug.Log($"[GlassEffect] ❓ '{materialName}' 효과 이름 인식 실패");
        }
    }

    private void RestartCoroutine(ref Coroutine coroutine, IEnumerator routine)
    {
        if (coroutine != null)
        {
            Debug.Log("[Coroutine] 기존 코루틴 중단");
            StopCoroutine(coroutine);
        }

        coroutine = StartCoroutine(routine);
    }

    private IEnumerator RedLightEffectCoroutine()
    {
        Debug.Log("[RedEffect] 시작");
        pointLight.color = Color.red;

        float elapsed = 0f;
        while (elapsed < redDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        pointLight.color = defaultLightColor; // ✅ 흰색으로 복귀
        Debug.Log("[RedEffect] 종료, 흰색 복귀");
    }

    private IEnumerator BlueRangeBoostCoroutine()
    {
        Debug.Log("[BlueEffect] 시작");
        pointLight.color = Color.blue;
        maxLightRange = defaultMaxLightRange + blueRangeBonus;
        Debug.Log($"[BlueEffect] maxLightRange 증가: {maxLightRange}");

        float elapsed = 0f;
        while (elapsed < blueDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        maxLightRange = defaultMaxLightRange;
        pointLight.color = defaultLightColor;
        Debug.Log("[BlueEffect] 종료, maxLightRange 복구 완료");
    }

    private IEnumerator GreenSpeedPenaltyCoroutine()
    {
        Debug.Log("[GreenEffect] 시작");
        pointLight.color = Color.green;
        walkSpeed = defaultWalkSpeed - speedPenalty;
        sprintSpeed = defaultSprintSpeed - speedPenalty;
        crouchSpeed = defaultCrouchSpeed - speedPenalty;

        Debug.Log($"[GreenEffect] 속도 감소: walk={walkSpeed}, sprint={sprintSpeed}, crouch={crouchSpeed}");

        float elapsed = 0f;
        while (elapsed < greenDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        walkSpeed = defaultWalkSpeed;
        sprintSpeed = defaultSprintSpeed;
        crouchSpeed = defaultCrouchSpeed;
        pointLight.color = defaultLightColor;
        Debug.Log("[GreenEffect] 종료, 속도 복구 완료");
    }

    private IEnumerator YellowRangePenaltyCoroutine()
    {
        Debug.Log("[YellowEffect] 시작");
        pointLight.color = Color.yellow;
        maxLightRange = defaultMaxLightRange - yellowRangePenalty;
        Debug.Log($"[YellowEffect] maxLightRange 감소: {maxLightRange}");

        if (glassEffectAudioSource != null && yellowEffectClip != null)
            glassEffectAudioSource.PlayOneShot(yellowEffectClip);

        float elapsed = 0f;
        while (elapsed < yellowDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        maxLightRange = defaultMaxLightRange;
        pointLight.color = defaultLightColor;
        Debug.Log("[YellowEffect] 종료, maxLightRange 복구");
    }

    private IEnumerator PinkLockLightCoroutine()
    {
        Debug.Log("[PinkEffect] 시작");

        // 빛 유지: 감쇠 금지
        isLightLocked = true;

        // 색상 분홍빛 (#FF77B3)
        pointLight.color = new Color(1.0f, 0.4667f, 0.7019f);

        // 오디오 재생
        if (glassEffectAudioSource != null && pinkEffectClip != null)
            glassEffectAudioSource.PlayOneShot(pinkEffectClip);

        // duration 유지 후 복구
        float elapsed = 0f;
        while (elapsed < pinkDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 복귀
        isLightLocked = false;
        pointLight.color = defaultLightColor;
        Debug.Log("[PinkEffect] 종료 → 흰색 복귀");
    }



    public void BeginThreatTint(float fadeSeconds = -1f)
    {
        threatTintDesired = true;
        float d = (fadeSeconds > 0f) ? fadeSeconds : defaultThreatFade;
        RestartCoroutine(ref threatTintCoroutine, ThreatTintRoutine(enable: true, duration: d));
    }

    public void EndThreatTint(float fadeSeconds = -1f)
    {
        threatTintDesired = false;
        float d = (fadeSeconds > 0f) ? fadeSeconds : defaultThreatFade;
        RestartCoroutine(ref threatTintCoroutine, ThreatTintRoutine(enable: false, duration: d));
    }

    private IEnumerator ThreatTintRoutine(bool enable, float duration)
    {
        if (pointLight == null) yield break;
        duration = Mathf.Max(0.001f, duration);

        // 1) 고우선순위 효과(분홍 락/유리병 빨강) 중엔 '기다림'
        while (isLightLocked || IsRedGlassActive)
            yield return null;

        // 2) 현재 색 → 목표 색으로 '서서히' 보간
        Color target = enable ? threatTintColor : defaultLightColor;
        Color start = pointLight.color;
        float t = 0f;

        while (t < 1f)
        {
            // 도중에 고우선순위(락/빨강) 시작되면 즉시 중단하고 다시 대기 → 끝난 후 잔여 페이드 이어감
            if (isLightLocked || IsRedGlassActive)
            {
                // 대기
                while (isLightLocked || IsRedGlassActive)
                    yield return null;

                // 재시작: 현재 색을 새 시작점으로
                start = pointLight.color;
                t = 0f;
            }

            t += Time.deltaTime / duration;
            pointLight.color = Color.Lerp(start, target, t);
            yield return null;
        }

        // 3) 최종 색 고정
        pointLight.color = target;

        // enable=false(복귀) 중 사용자가 다시 BeginThreatTint()를 호출했을 수 있으므로 코루틴 종료만
        // (상태 의도는 threatTintDesired 플래그로 유지)
    }

    public void TriggerGameOver(string reason = "Caught")
    {
        // 기존 가드
        if (IsGameOver) return;
        TriggerGameOver(null, reason);
    }

    public void TriggerGameOver(IGameOverFinisher finisher, string fallbackReason = "Caught")
    {
        if (IsGameOver) return;

        IsGameOver = true;
        CanMove = false;

        // 발소리 등 정지(필요 시)
        if (footstepAudioSource) footstepAudioSource.Stop();
        onGameOver?.Invoke();

        if (gameOverCo != null) StopCoroutine(gameOverCo);
        gameOverCo = StartCoroutine(GameOverSequenceWithFinisher(finisher, fallbackReason));
    }

    private IEnumerator GameOverSequenceWithFinisher(IGameOverFinisher finisher, string fallbackReason)
    {
        // 2-3) 캐릭터 이동을 완전히 고정(카메라 연출 방해 제거)
        HardLockCharacter();   // ★ 아래에 유틸 추가
        LockLightForGameOver(gameOverLightRange, gameOverLightIntensity, gameOverLightFadeIn);
        // 2-4) 적 전용 연출이 있다면 먼저 재생
        if (finisher != null)
        {
            yield return StartCoroutine(finisher.Play(this));
        }

        // 2-5) 공통 후처리(페이드/사운드)는 기존과 동일
        if (sfxSource && gameOverClip)
        {
            sfxSource.clip = gameOverClip;
            sfxSource.Play();
        }

        if (gameOverOverlay)
        {
            gameOverOverlay.gameObject.SetActive(true);
            float t = 0f;
            float start = gameOverOverlay.alpha;
            float dur = Mathf.Max(0.001f, gameOverFadeSeconds);
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                gameOverOverlay.alpha = Mathf.Lerp(start, 1f, t);
                yield return null;
            }
            gameOverOverlay.alpha = 1f;
        }

        // 여기서 씬 전환/메뉴 열기 등은 onGameOver에 이미 연결됐다고 가정
    }

    // 2-6) 연출 동안 이동계 완전 고정(간섭 방지)
    private void HardLockCharacter()
    {
        // 캐릭터컨트롤러 물리 이동 차단
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 마우스/키보드 입력은 CanMove=false로 이미 차단됨
        // 필요하면 커서 락/감추기 유지
    }

    // (선택) 연출이 끝난 뒤 다시 cc를 살릴 일은 일반적으로 없음(게임오버).
    // 만약 리트라이 등에서 재활성화가 필요하면 public 메서드로 되살리는 기능을 따로 두면 됨.

    // 연출에서 카메라/Transform이 필요하면 접근용 프로퍼티 하나 제공
    public Camera PlayerCamera => playerCamera;

    private IEnumerator LerpLightTo(float targetRange, float targetIntensity, float duration)
    {
        if (pointLight == null) yield break;
        duration = Mathf.Max(0.001f, duration);

        float startRange = pointLight.range;
        float startIntensity = pointLight.intensity;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            pointLight.range = Mathf.Lerp(startRange, targetRange, t);
            pointLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            yield return null;
        }
        pointLight.range = targetRange;
        pointLight.intensity = targetIntensity;
    }

    public void LockLightForGameOver(float range, float intensity, float fadeIn = 0.15f)
    {
        if (pointLight == null) return;
        isLightLocked = true;                       // ← 다른 효과가 건드리지 못하게 잠금
        if (lightCoroutine != null) StopCoroutine(lightCoroutine);
        StartCoroutine(LerpLightTo(range, intensity, fadeIn));
    }

    public void ApplyWaterRangeBonus(float bonus)
    {
        // 기본값 + 보너스로 즉시 적용
        float baseRange = defaultMaxLightRange; // ← 기존에 저장해둔 기본값
        maxLightRange = baseRange + bonus;
        Debug.Log($"[FPC] Water ON → maxLightRange = {maxLightRange} (base={baseRange}, bonus={bonus})");
    }

    public void ClearWaterRangeBonus()
    {
        maxLightRange = defaultMaxLightRange;
        Debug.Log($"[FPC] Water OFF → maxLightRange = {maxLightRange}");
    }
}
