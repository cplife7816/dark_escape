using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private AudioClip[] rockLandClips = default;
    [SerializeField] private AudioClip[] metalLandClips = default;
    [SerializeField] private AudioClip[] grassLandClips = default;
    [SerializeField] private AudioClip[] rockJumpClips = default;
    [SerializeField] private AudioClip[] metalJumpClips = default;
    [SerializeField] private AudioClip[] grassJumpClips = default;
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
    [SerializeField] private float interactionDistance = 2.5f;

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


    [SerializeField] private float interactDistance = 3f;

    [SerializeField] private TextMeshProUGUI subtitleTextUI;

    void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        defaultYpos = playerCamera.transform.localPosition.y;
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
            UpdateSubtitleText(); // ✅ 여기 추가!
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
            footstepAudioSource.volume = 0.2f;
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
        }
    }

    private void TryPickupItem()
    {
        RaycastHit hit;
        int mask = ~LayerMask.GetMask("IgnorePlayerRay"); // ScrewDriver 등을 무시
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactionDistance, mask))
        {
            GameObject target = hit.collider.gameObject;

            if (target.CompareTag("Item") || target.CompareTag("Cassette"))
            {
                heldObject = target;

                // ✅ ScrewDriver와 Cassette는 Ray 무시 레이어로 지정
                if (heldObject.name == "ScrewDriver" || heldObject.CompareTag("Cassette"))
                {
                    heldObject.layer = LayerMask.NameToLayer("IgnorePlayerRay");
                }

                // Rigidbody 설정
                Rigidbody rb = heldObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                // Collider 비활성화
                Collider col = heldObject.GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = false;
                }

                // 부모 설정
                heldObject.transform.SetParent(holdPosition);
                heldObject.transform.localPosition = Vector3.zero;

                if (heldObject.TryGetComponent(out PickupScaleOverride scaleOverride))
                {
                    scaleOverride.ApplyHeldScale(); // 원래 크기로 복원
                }

                // ✅ 회전 유지: PickupRotationOverride가 있으면 오버라이드, 없으면 현재 회전 유지
                if (heldObject.TryGetComponent(out PickupRotationOverride rotOverride))
                {
                    heldObject.transform.localRotation = Quaternion.Euler(rotOverride.customEulerRotation);

                    // 회전값 외 위치 보정
                    if (rotOverride.offsetPosition != Vector3.zero)
                    {
                        holdPosition.localPosition += rotOverride.offsetPosition;
                    }
                }
                else
                {
                    heldObject.transform.localRotation = Quaternion.Inverse(holdPosition.rotation) * heldObject.transform.rotation;
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

            // 드롭 위치
            heldObject.transform.position = playerCamera.transform.position + playerCamera.transform.forward * 1f;

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

            if (heldObject.TryGetComponent(out PickupScaleOverride scaleOverride))
            {
                scaleOverride.ApplyDroppedScale();
            }

            if (heldObject.TryGetComponent(out GlassBreakController glassBreak))
            {
                glassBreak.NotifyDroppedByPlayer();
            }

            // ✅ 레이어 복구
            heldObject.layer = LayerMask.NameToLayer("Default");

            heldObject = null;
            isHoldingItem = false;
            holdPosition.localPosition = new Vector3(0f, 0f, 1.2f);
        }
    }

    private void HandleInteractionIcon()
    {
        int mask = ~LayerMask.GetMask("IgnorePlayerRay");
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        Sprite newIcon = defaultIcon; // 기본값은 항상 흰 점

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, mask))
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
        holdPosition.localPosition = new Vector3(0f, 0f, 1f);
    }

    private void UpdateSubtitleText()
    {
        int mask = ~LayerMask.GetMask("IgnorePlayerRay");
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, mask))
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

}
