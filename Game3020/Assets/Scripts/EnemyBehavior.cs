using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    [SerializeField] private int hitCount;
    private bool isDefeated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isDefeated) return;

        if (other.CompareTag("Attack"))
        {
            hitCount++;

            if (hitCount >= 5)
            {
                isDefeated = true;
                gameObject.SetActive(false);
            }
        }
    }
}