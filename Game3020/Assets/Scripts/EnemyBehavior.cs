using UnityEngine;
using System.Collections;

public class EnemyBehavior : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private bool hasBeenDefeated = false;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationDelay = 0.2f;
    [SerializeField] private AnimationClip attackAnimationClip;

    [Header("Visual Effects")]
    [SerializeField] private GameObject[] attackEffects;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private Material[] attackMaterials;

    [Header("Defeat Settings")]
    [SerializeField] private GameObject defeatEffect;
    [SerializeField] private float defeatDelay = 3f;

    [Header("Look At Player Settings")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private bool smoothRotation = true;

    private bool isDefeated = false;
    private bool isPerformingAttack = false;
    private bool playerInZone = false;
    private PlayerControls playerInTrigger = null;
    private Renderer enemyRenderer;
    private Material originalMaterial;

    private Transform cachedTransform;

    private void Start()
    {
        cachedTransform = transform;

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer == null)
        {
            enemyRenderer = GetComponentInChildren<Renderer>();
        }

        if (enemyRenderer != null)
        {
            originalMaterial = enemyRenderer.material;
            Debug.Log("Enemy renderer and original material stored!");
        }
        else
        {
            Debug.LogWarning("No Renderer found on enemy! Material changes won't work.");
        }

        if (animator == null)
        {
            Debug.LogError("Enemy needs an Animator component!");
        }

        if (attackAnimationClip != null)
        {
            attackAnimationClip.wrapMode = WrapMode.Loop;
            Debug.Log("Attack animation set to loop!");
        }
    }

    private void Update()
    {
        // look at player when in zone;
        if (playerInZone && playerInTrigger != null && !isDefeated && !isPerformingAttack)
        {
            LookAtPlayer();

            Animator playerAnimator = playerInTrigger.GetComponent<Animator>();
            if (playerAnimator != null)
            {
                bool isPlayerAttacking = playerAnimator.GetBool("Attack");

                if (isPlayerAttacking && !hasBeenDefeated)
                {
                    Debug.Log("Player is attacking! Enemy responding to dance battle!");

                    hasBeenDefeated = true;
                    StartCoroutine(MirrorAttackThenDefeat());
                }
            }
            else
            {
                Debug.LogWarning("Player animator not found!");
            }
        }
    }

    private void LookAtPlayer()
    {
        if (playerInTrigger == null) return;

        // get direction to player;
        Vector3 directionToPlayer = playerInTrigger.transform.position - cachedTransform.position;
        directionToPlayer.y = 0f; // keep enemy upright;

        if (directionToPlayer.magnitude < 0.1f) return;

        // calculate target rotation;
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);

        // apply rotation;
        if (smoothRotation)
        {
            cachedTransform.rotation = Quaternion.Slerp(
                cachedTransform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            cachedTransform.rotation = targetRotation;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger detected: {other.gameObject.name} with tag: {other.tag}");

        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            playerInTrigger = other.GetComponent<PlayerControls>();

            if (playerInTrigger != null)
            {
                Debug.Log("Player entered enemy trigger zone - ready for dance battle!");
            }
            else
            {
                Debug.LogWarning("Player doesn't have PlayerControls component!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            playerInTrigger = null;
            Debug.Log("Player left enemy trigger zone");
        }
    }

    private IEnumerator MirrorAttackThenDefeat()
    {
        isPerformingAttack = true;

        yield return new WaitForSeconds(animationDelay);

        Debug.Log("Enemy performing attack animation before defeat!");

        if (animator != null)
        {
            animator.SetBool("Attack", true);
        }

        if (attackEffects != null && attackEffects.Length > 0)
        {
            int randomIndex = Random.Range(0, attackEffects.Length);
            GameObject selectedEffect = attackEffects[randomIndex];

            if (enemyRenderer != null && attackMaterials != null && attackMaterials.Length > 0)
            {
                int materialIndex = Mathf.Min(randomIndex, attackMaterials.Length - 1);
                enemyRenderer.material = attackMaterials[materialIndex];
                Debug.Log($"Enemy using material index: {materialIndex}");
            }

            if (selectedEffect != null)
            {
                Vector3 effectPosition = cachedTransform.position + cachedTransform.forward * attackRange;
                GameObject effect = Instantiate(selectedEffect, effectPosition, cachedTransform.rotation);
                effect.transform.SetParent(cachedTransform);
                Destroy(effect, defeatDelay);
            }
        }

        yield return new WaitForSeconds(defeatDelay);

        if (enemyRenderer != null && originalMaterial != null)
        {
            enemyRenderer.material = originalMaterial;
            Debug.Log("Enemy material reset to original");
        }

        StartCoroutine(DefeatSequence());
    }

    private IEnumerator DefeatSequence()
    {
        isDefeated = true;

        Debug.Log("Enemy defeated! Player wins the dance battle!");

        if (defeatEffect != null)
        {
            GameObject effect = Instantiate(defeatEffect, cachedTransform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        gameObject.SetActive(false);

        yield return null;
    }

    private void OnDestroy()
    {
        if (enemyRenderer != null && originalMaterial != null)
        {
            Destroy(originalMaterial);
        }
    }
}