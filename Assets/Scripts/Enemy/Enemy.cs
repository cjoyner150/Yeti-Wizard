using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int startingHealth;
    [SerializeField] private float damageMult;
    [SerializeField] private float damageVelocityThreshold;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnDistance;
    [SerializeField] private float invincibleTimeAfterSpawn;

    [Header("Move Settings")]
    [SerializeField] private float stoppingDistanceToTarget;
    [SerializeField] private float moveTimeMin;
    [SerializeField] private float moveTimeMax;
    [SerializeField] private float waitTimeMin;
    [SerializeField] private float waitTimeMax;

    [Header("Attack Settings")]
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private float attackRate;
    [SerializeField] private float bulletSpeed;
    [SerializeField] private int bulletDamage;
    [SerializeField] private float attackRotationMax;

    [Header("Death Settings")]
    [SerializeField] private float despawnTime;

    [Header("Components")]
    [SerializeField] private Collider baseCollider;
    [SerializeField] private Rigidbody[] rigRigidbodies;

    [Header("Audio")]
    [SerializeField] private SFXPlayer gunshotSFXPlayer;
    [SerializeField] private SFXPlayer deathSFXPlayer;

    [Header("Broadcast Events")]
    [SerializeField] private VoidEventSO deathEventSO;

    [Header("Listen Events")]
    [SerializeField] private VoidEventSO freezeEventSO;
    [SerializeField] private VoidEventSO unfreezeEventSO;

    private State state;
    private int health;
    private int bulletDamageMultiplier;
    private float moveTimer;
    private float waitTimer;
    private float attackTimer;
    private float despawnTimer;
    private float invincibleTimer;
    private Transform currentTarget;
    [SerializeField] private Transform goalTarget; // SerializeField is just for testing
    private Animator anim;
    private NavMeshAgent navAgent;

    private const string ANIM_PARAM_MOVING = "IsMoving";
    private const string ANIM_PARAM_ATTACKING = "IsShooting";

    public int Health { get => health; set => health = value; }

    #region Unity Methods
    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        TryGetComponent(out navAgent);
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

    //private void Start()
    //{
    //    Init(goalTarget, 1); // For Testing Only
    //}

    private void Update()
    {
        HandleInvincibility();

        switch (state)
        {
            case State.Frozen:
                break;
            case State.Waiting:
                HandleWaiting();
                break;
            case State.Moving:
                HandleMovement();
                break;
            case State.Attacking:
                FaceTarget();
                HandleAttacking();
                break;
            case State.Dead:
                HandleDeath();
                break;
        }
    }
    #endregion

    #region Init
    public void Init(Transform goalTransform, int bulletDamageMult, int startHealth = -1)
    {
        currentTarget = goalTarget = goalTransform;
        bulletDamageMultiplier = bulletDamageMult;

        if (startHealth > 0) health = startHealth;
        else health = startingHealth;
        invincibleTimer = invincibleTimeAfterSpawn;

        SetRagdollParts();

        ClampTime(ref waitTimeMin, ref waitTimeMax);
        ClampTime(ref moveTimeMin, ref moveTimeMax);

        Vector3 spawnPos = transform.position + Random.insideUnitSphere * spawnDistance;
        NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 500f, NavMesh.AllAreas);
        navAgent.Warp(hit.position);

        ChangeState(State.Frozen); // Change to Frozen; Moving for testing only
    }

    private void SetRagdollParts()
    {
        EnemyRagdollPart[] ragdollParts = GetComponentsInChildren<EnemyRagdollPart>();
        foreach (EnemyRagdollPart part in ragdollParts)
        {
            part.DamageMult = damageMult;
            part.DamageVelocityThreshold = damageVelocityThreshold;
        }
    }
    #endregion

    #region State Management
    private void ChangeState(State newState)
    {
        switch (state)
        {
            case State.Frozen:
                SetRagdollKinematic(false);
                anim.speed = 1;
                break;
            case State.Waiting:
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
                SetRagdollKinematic(true);
                anim.speed = 0;
                break;
            case State.Waiting:
                waitTimer = Random.Range(waitTimeMin, waitTimeMax);
                break;
            case State.Moving:
                navAgent.isStopped = false;
                anim.SetBool(ANIM_PARAM_MOVING, true);
                moveTimer = Random.Range(moveTimeMin, moveTimeMax);
                break;
            case State.Attacking:
                attackTimer = attackRate;
                anim.SetBool(ANIM_PARAM_ATTACKING, true);
                break;
            case State.Dead:
                navAgent.enabled = false;
                anim.enabled = false;
                baseCollider.enabled = false;
                despawnTimer = despawnTime;
                deathEventSO.RaiseEvent();
                deathSFXPlayer.Play();
                break;
        }
    }
    #endregion

    #region Invincibility
    private void HandleInvincibility()
    {
        if (invincibleTimer > 0)
        {
            invincibleTimer -= Time.deltaTime;
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
        if (health > 0) ChangeState(State.Moving);
        else ChangeState(State.Dead);
    }
    #endregion

    #region
    private void HandleWaiting()
    {
        if (waitTimer > 0)
        {
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0)
            {
                ChangeState(State.Moving);
                return;
            }
        }
    }
    #endregion

    #region Movement
    private void HandleMovement()
    {
        if (navAgent == null) return;
        if (currentTarget == null) return;

        if (moveTimer > 0)
        {
            moveTimer -= Time.deltaTime;

            if (moveTimer <= 0)
            {
                ChangeState(State.Waiting);
                return;
            }
        }

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

        RotateGunBarrelTowardsTarget();

        Bullet bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        bullet.Init(bulletDamage * bulletDamageMultiplier, bulletSpeed);
        bullet.Launch();
        gunshotSFXPlayer.Play();

        navAgent.SetDestination(currentTarget.position);

        if (navAgent.pathPending) return;

        if (!IsWithinStoppingDistanceFromTarget())
        {
            ChangeState(State.Moving);
            return;
        }
    }
    #endregion

    #region Collision
    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody hitRigidbody = collision.rigidbody;
        if (hitRigidbody != null)
        {
            if (hitRigidbody.transform.IsChildOf(transform)) return;
            if (invincibleTimer > 0 && hitRigidbody.GetComponentInParent<Enemy>()) return;

            int damage;
            if (!hitRigidbody.TryGetComponent(out DamageComponent dmgComponent))
            {
                if (hitRigidbody.linearVelocity.magnitude < damageVelocityThreshold) return;

                damage = Mathf.RoundToInt(hitRigidbody.linearVelocity.magnitude * damageMult);
            }
            else damage = dmgComponent.Damage;

            Hit(damage);
            return;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>())
        {
            currentTarget = other.transform;
            return;
        }

        if (other.TryGetComponent(out DamageComponent dmgComponent))
        {
            if (invincibleTimer > 0) return;

            Hit(dmgComponent.Damage);
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

    public void Hit(int damage)
    {
        if (state == State.Dead || state == State.Frozen) return;

        health -= damage;

        if (health < 0)
        {
            Die();
        }
    }

    public void HitFromPart(int damage)
    {
        if (invincibleTimer > 0) return;

        Hit(damage);
    }
    #endregion

    #region Death
    public void Die()
    {
        ChangeState(State.Dead);
    }

    private void HandleDeath()
    {
        if (despawnTimer > 0)
        {
            despawnTimer -= Time.deltaTime;
            return;
        }

        Destroy(gameObject);
    }
    #endregion

    #region Ragdoll
    private void SetRagdollKinematic(bool value)
    {
        foreach (Rigidbody rb in rigRigidbodies)
        {
            rb.isKinematic = value;
        }
    }
    #endregion

    #region Utility
    private bool IsWithinStoppingDistanceFromTarget()
    {
        return navAgent.remainingDistance <= stoppingDistanceToTarget;
    }

    private void FaceTarget()
    {
        if (currentTarget == null) return;

        Vector3 directionToTarget = currentTarget.position - transform.position;
        directionToTarget.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, navAgent.angularSpeed * Time.deltaTime);
    }

    private void RotateGunBarrelTowardsTarget()
    {
        if (currentTarget == null) return;

        Vector3 directionToTarget = currentTarget.position - bulletSpawnPoint.position;

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

        if (angleToTarget > attackRotationMax)
        {
            targetRotation = Quaternion.RotateTowards(Quaternion.LookRotation(transform.forward), Quaternion.LookRotation(directionToTarget), attackRotationMax);
        }

        bulletSpawnPoint.rotation = targetRotation;
    }

    private void ClampTime(ref float min, ref float max)
    {
        if (min > max) min = max;
        if (max < min) max = min;
    }
    #endregion

    public enum State
    {
        Frozen,
        Waiting,
        Moving,
        Attacking,
        Dead,
    }
}
