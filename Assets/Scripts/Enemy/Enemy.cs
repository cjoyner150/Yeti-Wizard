using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnCheckRadius;

    [Header("Move Settings")]
    [SerializeField] private float stoppingDistanceToPlayer;

    [Header("Attack Settings")]
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private float attackRate;
    [SerializeField] private float bulletSpeed;
    [SerializeField] private int bulletDamage;

    private State state;

    private Animator anim;
    private NavMeshAgent navAgent;
    [SerializeField] private Transform player;

    private float attackTimer;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        TryGetComponent(out navAgent);
        ChangeState(State.Moving);
    }

    public void Init(Transform playerTransform, float spawnRadiusFromOrigin)
    {
        player = playerTransform;
        NavMesh.SamplePosition(Vector2.zero, out NavMeshHit hit, spawnRadiusFromOrigin, NavMesh.AllAreas);
        navAgent.Warp(hit.position);

        ChangeState(State.Moving);
    }

    private void ChangeState(State newState)
    {
        switch (state)
        {
            case State.Frozen:
                break;
            case State.Moving:
                navAgent.isStopped = true;
                break;
            case State.Attacking:
                break;
            case State.Dead:
                break;
        }

        state = newState;

        switch (state)
        {
            case State.Frozen:
                break;
            case State.Moving:
                navAgent.isStopped = false;
                break;
            case State.Attacking:
                attackTimer = attackRate;
                break;
            case State.Dead:
                break;
        }
    }

    private void Update()
    {
        switch (state)
        {
            case State.Frozen:
                break;
            case State.Moving:
                HandleMovement();
                break;
            case State.Attacking:
                HandleAttacking();
                break;
            case State.Dead:
                break;
        }
    }

    private void HandleMovement()
    {
        if (navAgent == null) return;
        if (player == null) return;

        navAgent.SetDestination(player.position);

        if (navAgent.pathPending) return;

        if (IsWithinStoppingDistanceFromPlayer())
        {
            ChangeState(State.Attacking);
        }
    }

    private void HandleAttacking()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
            return;
        }
        else attackTimer = attackRate;

        if (bulletSpawnPoint == null) return;
        if (bulletPrefab == null) return;

        Bullet bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        bullet.Init(bulletDamage, bulletSpeed);
        bullet.Launch();

        navAgent.SetDestination(player.position);

        if (navAgent.pathPending) return;

        if (!IsWithinStoppingDistanceFromPlayer())
        {
            ChangeState(State.Moving);
            return;
        }
    }

    public bool IsWithinStoppingDistanceFromPlayer()
    {
        return navAgent.remainingDistance <= stoppingDistanceToPlayer;
    }

    public enum State
    {
        Frozen,
        Moving,
        Attacking,
        Dead,
    }
}
