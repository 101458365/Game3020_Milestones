using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerControls : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float rotationSpeed = 180f;

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
    [SerializeField] private float jumpCooldown = 0.1f; // reduced for better responsiveness;
    [SerializeField] private float wallRunForceMultiplier = 100f;

    private Rigidbody rb;
    public bool isGrounded;
    private Vector2 moveInput;
    private float lastAttackTime;

    public bool wallRunning = false;
    public bool readyToWallrun = true;
    public bool readyToJump = true;
    private bool cancellingWall = false;
    private Vector3 wallNormalVector;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        wallNormalVector = Vector3.up;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        CheckGrounded();

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
        Move();
        WallRunning();
    }

    // my new and improved ground detection method;
    private void CheckGrounded()
    {
        // it will cast multiple rays for better ground detection;
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

    void Move()
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

            float movementMultiplier = 1f;
            if (!isGrounded) movementMultiplier = 0.5f;
            if (wallRunning) movementMultiplier = 1.5f; // 20 × 1.5 = 30 speed for wall running;

            Vector3 targetVelocity = moveDirection * moveSpeed * movementMultiplier;

            Vector3 currentVelocity = rb.linearVelocity;
            rb.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
        }
        else
        {
            Vector3 currentVelocity = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);
        }

        Vector3 cameraForward = freeLookCamera.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        if (cameraForward.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(cameraForward.x, cameraForward.z) * Mathf.Rad2Deg;
            float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.fixedDeltaTime);
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
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

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        // i added debug logging to help diagnose jump issues;
        Debug.Log($"Jump input: {value.isPressed}, Grounded: {isGrounded}, Ready: {readyToJump}, WallRunning: {wallRunning}");

        if (value.isPressed && (isGrounded || wallRunning) && readyToJump)
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
                Debug.Log("Ground jump performed");
            }

            Invoke("ResetJump", jumpCooldown);
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