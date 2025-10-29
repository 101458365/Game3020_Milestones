using UnityEngine;

public class DelayedColliderActivation : MonoBehaviour
{
    [SerializeField] private float delayTime = 1f;
    private BoxCollider boxCollider;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
            Invoke("EnableCollider", delayTime);
        }
    }

    void EnableCollider()
    {
        if (boxCollider != null)
        {
            boxCollider.enabled = true;
        }
    }
}