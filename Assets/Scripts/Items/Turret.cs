using UnityEngine;

public class Turret : DraggableItem
{
    [Header("Aiming")]
    [SerializeField] Transform turretHead;
    [SerializeField] float turnSpeed = 2;

    [Header("Shooting")]
    [SerializeField] float shootCooldown;
    [SerializeField] float shootRange;
    [SerializeField] Transform shootLocLeft;
    [SerializeField] Transform shootLocRight;
    [SerializeField] GameObject bulletPrefab;
    
    PlayerController player;

    float shootTimer;

    protected override void Awake()
    {
        player = FindAnyObjectByType<PlayerController>();

        shootTimer = shootCooldown;

        base.Awake();
    }

    private void Update()
    {
        bool knockedOver = transform.rotation.eulerAngles.z > 30 || transform.rotation.eulerAngles.x > 30;
        bool playerInRange = Vector3.Distance(transform.position, player.transform.position) <= shootRange;
        
        if (!frozen && playerInRange)
        {
            if (!knockedOver)
            {
                Vector3 direction = (player.transform.position - turretHead.position);
                direction.y = 0;
                direction = direction.normalized;

                turretHead.rotation = Quaternion.LookRotation(Vector3.Slerp(turretHead.forward, direction, Time.deltaTime * turnSpeed));
            }

            shootTimer -= Time.deltaTime;

            if (shootTimer <= 0)
            {
                Shoot();
            }
        }
    }

    void Shoot()
    {
        shootTimer = shootCooldown;

        GameObject left = Instantiate(bulletPrefab, shootLocLeft.position, shootLocLeft.rotation);
        GameObject right = Instantiate(bulletPrefab, shootLocRight.position, shootLocRight.rotation);


    }
}
