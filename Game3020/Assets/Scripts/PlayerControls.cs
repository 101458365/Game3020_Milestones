using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections;

public class PlayerControls : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float rotationSpeed = 720f;

    [Header("Airstrafe Settings")]
    [SerializeField] private float airStrafeAcceleration = 50f;
    [SerializeField] private float airStrafeMaxSpeed = 15f;
    [SerializeField] private float airControl = 0.3f;

    [Header("Bunny Hop Settings")]
    [SerializeField] private float bhopSpeedBoost = 1.2f;
    [SerializeField] private float maxBhopSpeed = 25f;
    [SerializeField] private float bhopTimingWindow = 0.15f;
    [SerializeField] private float speedDecayRate = 0.95f;

    [Header("Camera")]
    public CinemachineCamera freeLookCamera;

    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private GameObject[] attackEffects;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Material[] attackMaterials;
    [SerializeField] private Renderer playerRenderer;

    [Header("Audio - Footsteps")]
    [SerializeField] private AudioSource footstepsSound;
    [SerializeField] private AudioSource sprintSound;
    [SerializeField] private float sprintSpeedThreshold = 15f;
    [SerializeField] private AudioSource landingAudioSource;
    [SerializeField] private AudioClip landingSound;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 1.1f;
    [SerializeField] private LayerMask groundLayer = 1;
    private float lastLandTime = 0f;

    [Header("Wall Running")]
    [SerializeField] private LayerMask whatIsWallrunnable = -1;
    [SerializeField] private float wallRunGravity = 1f;
    [SerializeField] private float jumpCooldown = 0.1f;
    [SerializeField] private float wallRunForceMultiplier = 100f;

    [Header("Movement Blocking")]
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private float blockCheckDistance = 0.5f;
    [SerializeField] private int blockCheckRayCount = 8;
    [SerializeField] private float heightStart = 0.3f;
    [SerializeField] private float heightEnd = 1.5f;
    [SerializeField] private float heightStep = 0.3f;
    [SerializeField] private float blockingDotThreshold = 0.7f;

    [Header("Animation")]
    [SerializeField] private float movementThreshold = 0.1f;
    [SerializeField] private float maxForwardSpeed = 10f;
    [SerializeField] private float animationSensitivity = 2f;

    [Header("Background Audio")]
    [SerializeField] BackgroundAudio backgroundAudio;

    private Rigidbody rb;
    public bool isGrounded;
    private bool wasOnGround;
    private Vector2 moveInput;
    private float lastAttackTime;
    private float currentBhopSpeed;
    private float timeLeftGround;
    private bool jumpRequested = false;
    private float forwardSpeed = 0f;

    private bool wallRunning = false;
    private bool readyToWallrun = true;
    private bool readyToJump = true;
    private bool cancellingWall = false;
    public bool isMoving = false;
    private bool isAttacking = false;

    private Vector3 wallNormalVector;
    private Animator animator;
    private Vector3[] blockedDirections;
    private Material originalMaterial;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("Animator component not found on player! Add an Animator component to your player GameObject.");
        }

        if (playerRenderer != null)
        {
            originalMaterial = playerRenderer.material;
        }
        else
        {
            Debug.LogWarning("Player Renderer not assigned! Material changes won't work.");
        }

        wallNormalVector = Vector3.up;
        currentBhopSpeed = moveSpeed;
        blockedDirections = new Vector3[blockCheckRayCount];

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        CheckGrounded();
        CheckBlockedDirections();
        UpdateMovingState();
        UpdateAnimationParameters();
        RotateTowardsCameraDirection();
        UpdateFootstepSounds();

        if (!isGrounded)
        {
            timeLeftGround += Time.deltaTime;
        }

        if (isGrounded && moveInput.magnitude < 0.1f)
        {
            currentBhopSpeed = Mathf.Lerp(currentBhopSpeed, moveSpeed, Time.deltaTime * 2f);
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            QuitApplication();
        }
    }

    void UpdateAnimationParameters()
    {
        if (animator == null)
        {
            return;
        }

        try
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float currentSpeed = horizontalVelocity.magnitude;

            forwardSpeed = Mathf.Clamp01((currentSpeed / maxForwardSpeed) * animationSensitivity);

            animator.SetFloat("ForwardSpeed", forwardSpeed);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("JumpRequested", jumpRequested);

            if (jumpRequested)
            {
                jumpRequested = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.Log($"Error in UpdateAnimationParameters: {e.Message}");
        }
    }

    void UpdateFootstepSounds()
    {
        if (isMoving && isGrounded && moveInput.magnitude > 0.1f)
        {
            float currentSpeed = GetHorizontalSpeed();
            float speedToCheck = Mathf.Max(currentBhopSpeed, currentSpeed);

            if (speedToCheck > sprintSpeedThreshold)
            {
                if (footstepsSound != null) footstepsSound.enabled = false;
                if (sprintSound != null) sprintSound.enabled = true;
            }
            else
            {
                if (footstepsSound != null) footstepsSound.enabled = true;
                if (sprintSound != null) sprintSound.enabled = false;
            }
        }
        else
        {
            if (footstepsSound != null) footstepsSound.enabled = false;
            if (sprintSound != null) sprintSound.enabled = false;
        }
    }

    void CheckBlockedDirections()
    {
        for (int i = 0; i < blockCheckRayCount; i++)
        {
            float angle = (360f / blockCheckRayCount) * i;
            Vector3 rayDirection = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            ).normalized;

            bool hitDetected = false;
            for (float height = heightStart; height <= heightEnd; height += heightStep)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * height;
                if (Physics.Raycast(rayOrigin, rayDirection, blockCheckDistance, platformLayer))
                {
                    hitDetected = true;
                    break;
                }
            }

            if (hitDetected)
            {
                blockedDirections[i] = rayDirection;
            }
            else
            {
                blockedDirections[i] = Vector3.zero;
            }
        }
    }

    private bool IsDirectionBlocked(Vector3 direction)
    {
        if (direction.magnitude < 0.1f)
            return false;

        direction.Normalize();

        foreach (Vector3 blockedDir in blockedDirections)
        {
            if (blockedDir.magnitude > 0.1f)
            {
                float dot = Vector3.Dot(direction, blockedDir);
                if (dot > blockingDotThreshold)
                {
                    return true;
                }
            }
        }

        return false;
    }

    void RotateTowardsCameraDirection()
    {
        // i dont rotate the character if we're attacking (but camera can still move)
        if (isAttacking)
            return;

        Vector3 cameraForward = freeLookCamera.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        if (cameraForward.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(cameraForward.x, cameraForward.z) * Mathf.Rad2Deg;
            float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
        }
    }

    void UpdateMovingState()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speed = horizontalVelocity.magnitude;

        isMoving = (moveInput.magnitude > 0.1f && isGrounded) || speed > movementThreshold;
        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);
        }
    }

    void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    void FixedUpdate()
    {
        // i dont move if we're attacking
        if (!isAttacking)
        {
            if (isGrounded)
            {
                MoveGrounded();
            }
            else
            {
                MoveAirstrafe();
            }

            WallRunning();
        }
        else
        {
            // i stop horizontal movement when attacking
            Vector3 currentVelocity = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);
        }

        wasOnGround = isGrounded;
    }

    private void CheckGrounded()
    {
        bool groundedNow = false;

        Vector3[] checkPositions = {
            transform.position,
            transform.position + Vector3.forward * 0.3f,
            transform.position + Vector3.back * 0.3f,
            transform.position + Vector3.left * 0.3f,
            transform.position + Vector3.right * 0.3f
        };

        foreach (Vector3 pos in checkPositions)
        {
            if (Physics.Raycast(pos, Vector3.down, groundCheckDistance, groundLayer))
            {
                groundedNow = true;
                if (wallRunning)
                    wallRunning = false;
                break;
            }
        }

        if (groundedNow && !isGrounded)
        {
            OnLand();
        }

        isGrounded = groundedNow;
    }

    private void OnLand()
    {
        timeLeftGround = 0f;

        if (Time.time - lastLandTime > 0.2f)
        {
            if (landingAudioSource != null && landingSound != null)
            {
                landingAudioSource.PlayOneShot(landingSound);
                Debug.Log("Landed");
            }

            lastLandTime = Time.time;
        }
    }

    void MoveGrounded()
    {
        if (moveInput.magnitude >= 0.1f)
        {
            Vector3 cameraForwards = freeLookCamera.transform.forward;
            Vector3 cameraRight = freeLookCamera.transform.right;
            cameraForwards.y = 0f;
            cameraRight.y = 0f;
            cameraForwards.Normalize();
            cameraRight.Normalize();
            Vector3 moveDirection = (cameraRight * moveInput.x + cameraForwards * moveInput.y).normalized;

            if (IsDirectionBlocked(moveDirection))
            {
                Vector3 blockedVelocity = rb.linearVelocity;
                rb.linearVelocity = new Vector3(
                    blockedVelocity.x * speedDecayRate,
                    blockedVelocity.y,
                    blockedVelocity.z * speedDecayRate
                );
                return;
            }

            float speedToUse;
            if (moveInput.y < 0)
            {
                speedToUse = moveSpeed * 0.7f;
            }
            else
            {
                speedToUse = Mathf.Max(currentBhopSpeed, moveSpeed);
            }

            Vector3 targetVelocity = moveDirection * speedToUse;
            Vector3 currentVelocity = rb.linearVelocity;

            Vector3 newHorizontalVelocity = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
            if (newHorizontalVelocity.magnitude > speedToUse)
            {
                newHorizontalVelocity = newHorizontalVelocity.normalized * speedToUse;
            }

            rb.linearVelocity = new Vector3(newHorizontalVelocity.x, currentVelocity.y, newHorizontalVelocity.z);
        }
        else
        {
            Vector3 currentVelocity = rb.linearVelocity;
            rb.linearVelocity = new Vector3(
                currentVelocity.x * speedDecayRate,
                currentVelocity.y,
                currentVelocity.z * speedDecayRate
            );
        }
    }

    void MoveAirstrafe()
    {
        if (moveInput.magnitude >= 0.1f)
        {
            Vector3 cameraForwards = freeLookCamera.transform.forward;
            Vector3 cameraRight = freeLookCamera.transform.right;

            cameraForwards.y = 0f;
            cameraRight.y = 0f;
            cameraForwards.Normalize();
            cameraRight.Normalize();

            Vector3 wishDirection = (cameraRight * moveInput.x + cameraForwards * moveInput.y).normalized;

            if (moveInput.y < -0.5f)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                return;
            }

            if (IsDirectionBlocked(wishDirection))
            {
                Vector3 dragVelocity = rb.linearVelocity;
                rb.linearVelocity = new Vector3(
                    dragVelocity.x * 0.99f,
                    dragVelocity.y,
                    dragVelocity.z * 0.99f
                );
                return;
            }

            Vector3 currentVelocityFlat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float currentSpeed = currentVelocityFlat.magnitude;

            if (currentSpeed < airStrafeMaxSpeed)
            {
                Vector3 acceleration = wishDirection * airStrafeAcceleration * Time.fixedDeltaTime;

                Vector3 newVelocity = currentVelocityFlat + acceleration;

                if (newVelocity.magnitude > airStrafeMaxSpeed)
                {
                    newVelocity = newVelocity.normalized * Mathf.Min(newVelocity.magnitude, airStrafeMaxSpeed);
                }

                rb.linearVelocity = new Vector3(newVelocity.x, rb.linearVelocity.y, newVelocity.z);
            }
            else
            {
                Vector3 redirectedVelocity = Vector3.Lerp(currentVelocityFlat, wishDirection * currentSpeed, airControl * Time.fixedDeltaTime);
                rb.linearVelocity = new Vector3(redirectedVelocity.x, rb.linearVelocity.y, redirectedVelocity.z);
            }
        }

        if (moveInput.magnitude < 0.1f)
        {
            Vector3 currentVelocity = rb.linearVelocity;
            rb.linearVelocity = new Vector3(
                currentVelocity.x * 0.99f,
                currentVelocity.y,
                currentVelocity.z * 0.99f
            );
        }
    }

    void WallRunning()
    {
        if (wallRunning)
        {
            rb.AddForce(-wallNormalVector * Time.deltaTime * moveSpeed);
            rb.AddForce(Vector3.up * Time.deltaTime * rb.mass * wallRunForceMultiplier * wallRunGravity);
        }
    }

    private void StartWallRun(Vector3 normal)
    {
        if (!isGrounded && readyToWallrun)
        {
            wallNormalVector = normal;

            if (!wallRunning)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * 20f, ForceMode.Impulse);
            }

            wallRunning = true;
            Debug.Log("Started wall running");
        }
    }

    private bool IsWall(Vector3 normal)
    {
        return Mathf.Abs(90f - Vector3.Angle(Vector3.up, normal)) < 0.1f;
    }

    void PerformMeleeAttack()
    {
        if (isGrounded == true)
        {
            if (Time.time < lastAttackTime + attackCooldown)
                return;

            lastAttackTime = Time.time;
            isAttacking = true;

            if (animator != null)
            {
                animator.SetBool("Attack", true);
            }

            backgroundAudio.SetVolume(0);

            if (audioSource != null && attackSound != null)
            {
                audioSource.PlayOneShot(attackSound);
            }

            // i randomly select one of the attack effects from the array
            if (attackEffects != null && attackEffects.Length > 0 && animator != null && animator.GetBool("Attack"))
            {
                // i pick a random effect
                int randomIndex = Random.Range(0, attackEffects.Length);
                GameObject selectedEffect = attackEffects[randomIndex];

                // i change the material to match the same index as the attack effect
                if (playerRenderer != null && attackMaterials != null && attackMaterials.Length > 0)
                {
                    // i use the same index for the material (with a safety check)
                    int materialIndex = Mathf.Min(randomIndex, attackMaterials.Length - 1);
                    playerRenderer.material = attackMaterials[materialIndex];
                    Debug.Log($"Using material index: {materialIndex} for attack effect index: {randomIndex}");
                }

                if (selectedEffect != null)
                {
                    Vector3 effectLocation = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                    Vector3 effectPosition = effectLocation + transform.forward * attackRange;
                    GameObject effect = Instantiate(selectedEffect, effectPosition, transform.rotation);

                    effect.transform.SetParent(transform);
                    Destroy(effect, 4f);
                }

                StartCoroutine(DisableAttackBoolAfterDelay(0.1f));
            }

            StartCoroutine(ResetMaterialAfterDelay(4f));

            Debug.Log("Attack performed!");
        }
    }

    private IEnumerator DisableAttackBoolAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (animator != null)
        {
            animator.SetBool("Attack", false);
        }
    }

    private IEnumerator ResetMaterialAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (playerRenderer != null && originalMaterial != null)
        {
            playerRenderer.material = originalMaterial;
            Debug.Log("Material reset to original");
        }
        backgroundAudio.SetVolume(0.05f);
        // i re-enable movement after the attack is finished
        isAttacking = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Respawn"))
        {
            transform.position = new Vector3(0, 2, -5);
            currentBhopSpeed = moveSpeed;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        int layer = collision.gameObject.layer;

        if ((whatIsWallrunnable.value & (1 << layer)) == 0)
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.contacts[i].normal;

            if (IsWall(normal))
            {
                StartWallRun(normal);
                cancellingWall = false;
                CancelInvoke("StopWall");
            }
        }

        if (!cancellingWall)
        {
            cancellingWall = true;
            Invoke("StopWall", Time.deltaTime * 3f);
        }
    }

    private void StopWall()
    {
        wallRunning = false;
        cancellingWall = false;
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void PerformJump()
    {
        readyToJump = false;

        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(velocity.x, 0f, velocity.z);

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        if (wallRunning)
        {
            rb.AddForce(wallNormalVector * jumpForce * 3f, ForceMode.Impulse);
            wallRunning = false;
            Debug.Log("Wall jump performed");
        }
        else
        {
            Debug.Log("Jump performed");
        }

        Invoke("ResetJump", jumpCooldown);
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && (isGrounded || wallRunning) && readyToJump)
        {
            if (isGrounded && timeLeftGround <= bhopTimingWindow)
            {
                currentBhopSpeed = Mathf.Min(currentBhopSpeed * bhopSpeedBoost, maxBhopSpeed);
                Debug.Log($"Good bhop! Speed: {currentBhopSpeed}");
            }
            else if (isGrounded)
            {
                currentBhopSpeed = moveSpeed;
            }

            jumpRequested = true;
            PerformJump();
        }
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            PerformMeleeAttack();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 newAttackPosition = new Vector3(transform.position.x, transform.position.y + 2, transform.position.z);
        Vector3 attackPosition = newAttackPosition + transform.forward * (attackRange * 0.5f);
        Gizmos.DrawWireSphere(attackPosition, attackRange);

        if (wallRunning)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, wallNormalVector * 2f);
        }

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3[] checkPositions = {
            transform.position,
            transform.position + Vector3.forward * 0.3f,
            transform.position + Vector3.back * 0.3f,
            transform.position + Vector3.left * 0.3f,
            transform.position + Vector3.right * 0.3f
        };

        foreach (Vector3 pos in checkPositions)
        {
            Gizmos.DrawRay(pos, Vector3.down * groundCheckDistance);
        }

        Gizmos.color = Color.yellow;
        for (int i = 0; i < blockCheckRayCount; i++)
        {
            float angle = (360f / blockCheckRayCount) * i;
            Vector3 rayDirection = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            ).normalized;

            for (float height = heightStart; height <= heightEnd; height += heightStep)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * height;
                Gizmos.DrawRay(rayOrigin, rayDirection * blockCheckDistance);
            }
        }

        Gizmos.color = Color.purple;
        if (blockedDirections != null)
        {
            for (int i = 0; i < blockedDirections.Length; i++)
            {
                if (blockedDirections[i].magnitude > 0.1f)
                {
                    for (float height = heightStart; height <= heightEnd; height += heightStep)
                    {
                        Vector3 rayOrigin = transform.position + Vector3.up * height;
                        Gizmos.DrawRay(rayOrigin, blockedDirections[i] * blockCheckDistance);
                    }
                }
            }
        }
    }

    public float GetHorizontalSpeed()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        return horizontalVelocity.magnitude;
    }

    public bool IsWallRunning()
    {
        return wallRunning;
    }
}