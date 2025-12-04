using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [SerializeField] private bool isActivated = false;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color activeColor = Color.green;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject visualEffect;
    [SerializeField] private Renderer checkpointRenderer;

    private void Start()
    {
        UpdateVisuals();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!isActivated)
            {
                ActivateCheckpoint(other.transform.position);
            }
        }
    }

    private void ActivateCheckpoint(Vector3 playerPosition)
    {
        isActivated = true;

        // register this checkpoint with the GameManager;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCheckpoint(transform.position);
            Debug.Log($"Checkpoint activated at position: {transform.position}");
        }
        else
        {
            Debug.LogWarning("GameManager.Instance is null! Cannot save checkpoint.");
        }

        PlayActivationEffects();

        UpdateVisuals();
    }

    private void PlayActivationEffects()
    {
        if (visualEffect != null)
        {
            visualEffect.SetActive(true);
        }
    }

    private void UpdateVisuals()
    {
        if (checkpointRenderer != null)
        {
            Material mat = checkpointRenderer.material;
            mat.color = isActivated ? activeColor : inactiveColor;

            if (isActivated)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", activeColor * 0.5f);
            }
        }
    }

    public void ManualActivate()
    {
        if (!isActivated)
        {
            ActivateCheckpoint(transform.position);
        }
    }

    public void ResetCheckpoint()
    {
        isActivated = false;
        UpdateVisuals();

        if (visualEffect != null)
        {
            visualEffect.SetActive(false);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isActivated ? activeColor : inactiveColor;
        Gizmos.DrawWireSphere(transform.position, 1f);

        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
        Gizmos.DrawSphere(transform.position, 1f);
    }

    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.cyan;
            if (col is BoxCollider boxCol)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(boxCol.center, boxCol.size);
            }
            else if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawWireSphere(transform.position + sphereCol.center, sphereCol.radius);
            }
        }
    }
}