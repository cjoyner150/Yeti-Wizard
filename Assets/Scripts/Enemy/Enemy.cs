using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int startingHealth;

    [Header("Move Settings")]
    [SerializeField] private float stoppingDistanceToTarget;

    [Header("Attack Settings")]
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private float attackRate;
    [SerializeField] private float bulletSpeed;
    [SerializeField] private int bulletDamage;

    [Header("Broadcast Events")]
    [SerializeField] private VoidEventSO deathEventSO;

    [Header("Listen Events")]
    [SerializeField] private VoidEventSO freezeEventSO;
    [SerializeField] private VoidEventSO unfreezeEventSO;

    private State state;

    private Animator anim;
    private NavMeshAgent navAgent;
    [SerializeField] private Transform goalTarget; // SerializeField, just for testing
    private Transform currentTarget;

    private int health;
    private float attackTimer;
    private int bulletDamageMultiplier;

    private const string ANIM_PARAM_MOVING = "IsMoving";
    private const string ANIM_PARAM_ATTACKING = "IsShooting";

    public int Health { get => health; set => health = value; }

    #region Unity Methods
    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        TryGetComponent(out navAgent);
        ChangeState(State.Moving); // Change to Frozen
    }

    private void OnEnable()
    {
        freezeEventSO.onEventRaised += Freeze;
        unfreezeEventSO.onEventRaised += Unfreeze;
    }

    private void OnDisable()
    {
        freezeEventSO.onEventRaised -= Freeze;
        unfreezeEventSO.onEventRaised -= Unfreeze;
    }

    private void Start()
    {
        currentTarget = goalTarget; // For Testing
        health = startingHealth;
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
                FaceTarget();
                HandleAttacking();
                break;
            case State.Dead:
                break;
        }
    }
    #endregion

    #region Init
    public void Init(Transform goalTransform, int bulletDamageMult)
    {
        goalTarget = goalTransform;
        NavMesh.SamplePosition(Vector2.zero, out NavMeshHit hit, 500f, NavMesh.AllAreas);
        navAgent.Warp(hit.position);

        bulletDamageMultiplier = bulletDamageMult;

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

    #region Freezing
    private void Freeze()
    {
        ChangeState(State.Frozen);
    }

    private void Unfreeze()
    {
        ChangeState(State.Moving);
    }
    #endregion

    #region Movement
    private void HandleMovement()
    {
        if (navAgent == null) return;
        if (currentTarget == null) return;

        navAgent.SetDestination(currentTarget.position);

        if (navAgent.pathPending) return;

        if (IsWithinStoppingDistanceFromTarget())
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
        bullet.Init(bulletDamage * bulletDamageMultiplier, bulletSpeed);
        bullet.Launch();

        navAgent.SetDestination(currentTarget.position);

        if (navAgent.pathPending) return;

        if (!IsWithinStoppingDistanceFromTarget())
        {
            ChangeState(State.Moving);
            return;
        }
    }
    #endregion

    #region Damage
    public void Hit(int damage)
    {
        health -= damage;

        if (health < 0)
        {
            Die();
        }
    }
    public void Die()
    {
        Destroy(gameObject);
    }

    #endregion

    #region Player Detection
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>())
        {
            currentTarget = other.transform;
            return;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>())
        {
            currentTarget = goalTarget;
            return;
        }
    }
    #endregion

    #region Utility
    public bool IsWithinStoppingDistanceFromTarget()
    {
        return navAgent.remainingDistance <= stoppingDistanceToTarget;
    }

    public void FaceTarget()
    {
        if (currentTarget == null) return;

        Vector3 directionToTarget = currentTarget.position - transform.position;
        directionToTarget.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, navAgent.angularSpeed * Time.deltaTime);
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
