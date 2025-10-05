using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerControls : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float rotationSpeed = 180f;

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
    [SerializeField] private GameObject attackEffect;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 1.1f;
    [SerializeField] private LayerMask groundLayer = 1;

    [Header("Wall Running")]
    [SerializeField] private LayerMask whatIsWallrunnable = -1;
    [SerializeField] private float wallRunGravity = 1f;
    [SerializeField] private float jumpCooldown = 0.1f;
    [SerializeField] private float wallRunForceMultiplier = 100f;

    private Rigidbody rb;
    public bool isGrounded;
    private bool wasGrounded;
    private Vector2 moveInput;
    private float lastAttackTime;
    private float currentBhopSpeed;
    private float timeLeftGround;
    private bool jumpQueued = false;

    private bool wallRunning = false;
    private bool readyToWallrun = true;
    private bool readyToJump = true;
    private bool cancellingWall = false;
    private Vector3 wallNormalVector;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        wallNormalVector = Vector3.up;
        currentBhopSpeed = moveSpeed;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        CheckGrounded();

        // i can track time in air for bhop timing;
        if (!isGrounded)
        {
            timeLeftGround += Time.deltaTime;
        }

        // i check if landing happened;
        if (isGrounded && !wasGrounded)
        {
            timeLeftGround = 0f;
        }

        // we decay speed when grounded and not moving;
        if (isGrounded && moveInput.magnitude < 0.1f)
        {
            currentBhopSpeed = Mathf.Lerp(currentBhopSpeed, moveSpeed, Time.deltaTime * 2f);
        }

        wasGrounded = isGrounded;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            QuitApplication();
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

    private void CheckGrounded()
    {
        Vector3[] checkPositions = {
            transform.position,
            transform.position + Vector3.forward * 0.3f,
            transform.position + Vector3.back * 0.3f,
            transform.position + Vector3.left * 0.3f,
            transform.position + Vector3.right * 0.3f
        };

        isGrounded = false;
        foreach (Vector3 pos in checkPositions)
        {
            if (Physics.Raycast(pos, Vector3.down, groundCheckDistance, groundLayer))
            {
                isGrounded = true;
                readyToJump = true;
                if (wallRunning)
                {
                    wallRunning = false;
                }
                break;
            }
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

            float speedToUse = Mathf.Max(currentBhopSpeed, moveSpeed);
            Vector3 targetVelocity = moveDirection * speedToUse;

            Vector3 currentVelocity = rb.linearVelocity;
            rb.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);

            if (moveDirection.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.fixedDeltaTime);
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
            }
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

            // my airstrafe mechanics - accelerate in the direction of input;
            Vector3 currentVelocityFlat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float currentSpeed = currentVelocityFlat.magnitude;

            // we can only apply airstrafe if below max speed;
            if (currentSpeed < airStrafeMaxSpeed)
            {
                Vector3 acceleration = wishDirection * airStrafeAcceleration * Time.fixedDeltaTime;

                Vector3 newVelocity = currentVelocityFlat + acceleration;

                // i clamped to max airstrafe speed (so we dont go too fast)
                if (newVelocity.magnitude > airStrafeMaxSpeed)
                {
                    newVelocity = newVelocity.normalized * Mathf.Min(newVelocity.magnitude, airStrafeMaxSpeed);
                }

                rb.linearVelocity = new Vector3(newVelocity.x, rb.linearVelocity.y, newVelocity.z);
            }
            else
            {
                // i should allow minor directional changes even at max speed
                Vector3 redirectedVelocity = Vector3.Lerp(currentVelocityFlat, wishDirection * currentSpeed, airControl * Time.fixedDeltaTime);
                rb.linearVelocity = new Vector3(redirectedVelocity.x, rb.linearVelocity.y, redirectedVelocity.z);
            }

            if (wishDirection.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(wishDirection.x, wishDirection.z) * Mathf.Rad2Deg;
                float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.fixedDeltaTime);
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
            }
        }

        // this is how i apply slight drag when no input in air
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

    private void CancelWallrun()
    {
        Debug.Log("Wall run cancelled");
        Invoke("GetReadyToWallrun", 0.1f);
        rb.AddForce(wallNormalVector * 600f);
        readyToWallrun = false;
        wallRunning = false;
    }

    private void GetReadyToWallrun()
    {
        readyToWallrun = true;
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
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        if (attackEffect != null)
        {
            Vector3 effectPosition = transform.position + transform.forward * attackRange;
            GameObject effect = Instantiate(attackEffect, effectPosition, transform.rotation);
            Destroy(effect, 2f);
        }

        Debug.Log("Attack performed!");
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

        timeLeftGround = 0f;
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
        Vector3 attackPosition = transform.position + transform.forward * (attackRange * 0.5f);
        Gizmos.DrawWireSphere(attackPosition, attackRange);

        if (wallRunning)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, wallNormalVector * 2f);
        }

        // Draw ground check rays for debugging
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
    }
}