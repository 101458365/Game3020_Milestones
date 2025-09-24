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

    private Rigidbody rb;
    private bool isGrounded;
    private Vector2 moveInput;
    private float lastAttackTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void FixedUpdate()
    {
        Move();
    }

    void Move()
    {
        if (moveInput.magnitude >= 0.1f)
        {
            // lets get camera's forward and right directions (ignore Y component for ground movement);
            Vector3 cameraForwards = freeLookCamera.transform.forward;
            Vector3 cameraRight = freeLookCamera.transform.right;

            // this is how we project camera directions onto the horizontal plane;
            cameraForwards.y = 0f;
            cameraRight.y = 0f;
            cameraForwards.Normalize();
            cameraRight.Normalize();

            // we calculate movement direction relative to camera (aka we move in the direction of camera);
            Vector3 moveDirection = (cameraRight * moveInput.x + cameraForwards * moveInput.y).normalized;

            // apply the actual movement using physics;
            Vector3 movement = moveDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(transform.position + movement);
        }

        // with this i always face the camera direction (independent of movement);
        Vector3 cameraForward = freeLookCamera.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        // we get smooth rotation towards camera direction;
        if (cameraForward.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(cameraForward.x, cameraForward.z) * Mathf.Rad2Deg;
            float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.fixedDeltaTime);
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
        }
    }

    void PerformMeleeAttack()
    {
        // this is a check if we can attack (cooldown system);
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        if (attackEffect != null)
        {
            Vector3 effectPosition = transform.position + transform.forward * attackRange;
            GameObject effect = Instantiate(attackEffect, effectPosition, transform.rotation);
            // clean up the effect after 2 seconds;
            Destroy(effect, 2f);
        }

        Debug.Log("Attack performed!");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
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
        // i needed a visual debug for attack range in scene view;
        Gizmos.color = Color.red;
        Vector3 attackPosition = transform.position + transform.forward * (attackRange * 0.5f);
        Gizmos.DrawWireSphere(attackPosition, attackRange);
    }
}