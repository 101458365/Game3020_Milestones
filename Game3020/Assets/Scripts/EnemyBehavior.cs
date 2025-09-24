using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
   [SerializeField] private int hitCount;

    void Update()
    {
        if (hitCount == 5)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Attack"))
        {
            hitCount++;
        }
    }
}
