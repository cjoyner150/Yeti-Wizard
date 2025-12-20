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

    private const string ANIM_PARAM_MOVING = "IsMoving";
    private const string ANIM_PARAM_ATTACKING = "IsShooting";

    #region Unity Methods
    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        TryGetComponent(out navAgent);
        ChangeState(State.Moving);
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
    #endregion

    #region Init
    public void Init(Transform playerTransform, float spawnRadiusFromOrigin)
    {
        player = playerTransform;
        NavMesh.SamplePosition(Vector2.zero, out NavMeshHit hit, spawnRadiusFromOrigin, NavMesh.AllAreas);
        navAgent.Warp(hit.position);

        ChangeState(State.Moving);
    }
    #endregion

    #region State Management
    private void ChangeState(State newState)
    {
        switch (state)
        {
            case State.Frozen:
                break;
            case State.Moving:
                navAgent.isStopped = true;
                anim.SetBool(ANIM_PARAM_MOVING, false);
                break;
            case State.Attacking:
                anim.SetBool(ANIM_PARAM_ATTACKING, false);
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
                anim.SetBool(ANIM_PARAM_MOVING, true);
                break;
            case State.Attacking:
                attackTimer = attackRate;
                anim.SetBool(ANIM_PARAM_ATTACKING, true);
                break;
            case State.Dead:
                break;
        }
    }
    #endregion

    #region Movement
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
    #endregion

    #region Attacking
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
    #endregion

    #region Utility
    public bool IsWithinStoppingDistanceFromPlayer()
    {
        return navAgent.remainingDistance <= stoppingDistanceToPlayer;
    }
    #endregion

    public enum State
    {
        Frozen,
        Moving,
        Attacking,
        Dead,
    }
}
