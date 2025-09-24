using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControls : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float rotationSpeed = 180f;

    [Header("Combat Settings")]
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    public GameObject attackEffect;

    private Rigidbody rb;
    public bool isGrounded;
    private Vector2 moveInput;
    private float lastAttackTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {

    }

    void FixedUpdate()
    {
        Move();
    }

    void Move()
    {
        Vector3 direction = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        if (direction.magnitude >= 0.1f)
        {
            Vector3 moveDirection = direction * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(transform.position + moveDirection);
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.fixedDeltaTime);
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
        }
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
        Gizmos.color = Color.red;
        Vector3 attackPosition = transform.position + transform.forward * (attackRange * 0.5f);
        Gizmos.DrawWireSphere(attackPosition, attackRange);
    }
}