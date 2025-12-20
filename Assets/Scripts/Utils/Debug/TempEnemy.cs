using UnityEngine;

public class TempEnemy : MonoBehaviour
{
    [SerializeField] VoidEventSO enemyDiedEvent;

    public bool Init(Transform player, int mult)
    {
        return true;
    }

    [ContextMenu("Die")]
    public void Die()
    {
        enemyDiedEvent.onEventRaised.Invoke();
        Destroy(gameObject);
    }
}
