using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Mono.Cecil.Cil;

public class PlayerController : MonoBehaviour, IDamageable
{
    #region Variables

    [Header("Movement")]
    public float baseMoveSpeed = 6f;
    public float runMultiplier = 1.5f;
    public float jumpForce = 7f;
    public float airControlMultiplier = 0.5f;
    [SerializeField] GameObject mesh;

    [Header("Mouse Look")]
    public Transform orientation;
    public float mouseSensitivity = 10f;
    Camera cam;

    [Header("Shooting")]
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform gun;
    [SerializeField] Transform bulletSpawnLoc;
    [SerializeField] float bulletSpeed;
    [SerializeField] LayerMask shootableLayers;
    [SerializeField] float shootCooldown;
    
    float shootTimer = 0;
    bool shootOnCD => shootTimer > 0;


    [Header("Force")]
    public float pickupForce = 15f;
    public float forceMultiplier = 1f;
    public LayerMask pickupLayerMask;

    [Header("Hold Distance")]
    public float scrollSpeed = 2f;
    public float minHoldDistance = 1.5f;
    public float maxHoldDistance = 6f;
    public float pickupRange = 6f;
    public float maxHoldDistanceFromCamera = 8f;
    public GameObject lineStart;
    public GameObject lineEnd;
    public LineRenderer Beam1;
    public LineRenderer Beam2;

    [Header("Weight System")]
    public float minMoveSpeed = 2f;
    public float massSlowdownMultiplier = 0.5f;
    public float heavyThreshold = 1f;
    
    [Header("Health")]
    public int maxHealth = 3;
    public int Health { 
        get { return currentHealth; }
        set { currentHealth = value; }
    }
    public float damageThreshold = 15f; 
    public float invincibilityTime = 1f; 

    [Header("Pickup & Throw")]
    public float throwForce = 15f;
    [SerializeField] LayerMask blocksHolding;
    
    [Header("Visual Feedback")]
    public AudioClip pickupSound;
    public AudioClip throwSound;
    private AudioSource audioSource;

    [Header("Events")]
    [SerializeField] VoidEventSO freezeEvent;
    [SerializeField] VoidEventSO unfreezeEvent;
    [SerializeField] IntEventSO healthSetEvent;
    [SerializeField] IntEventSO updateHealthEvent;

    [Header("Input")]
    PlayerInputMap inputControls;
    InputAction fire;
    
    // Private vars
    private float currentHoldDistance;
    private bool isGrounded;
    private Rigidbody rb;
    private Rigidbody heldObjectRB;
    private DraggableItem heldObject;
    private float xRotation;
    float yRotation;
    private float currentMoveSpeed;
    
    // Health
    private int currentHealth;
    private float lastDamageTime;
    private bool isInvincible = false;
    
    // Mode switching
    private bool isCombatPhase = false;
    
    // Pickup
    private LevelManager levelManager;
    private float heldObjectMass = 0f;

    #endregion

    #region Unity Methods

    private void OnEnable()
    {
        freezeEvent.onEventRaised += Freeze;
        unfreezeEvent.onEventRaised += Unfreeze;

        fire.performed += Shoot;
        fire.Enable();
    }

    private void OnDisable()
    {
        freezeEvent.onEventRaised -= Freeze;
        unfreezeEvent.onEventRaised -= Unfreeze;

        fire.performed -= Shoot;
        fire.Disable();
    }

    void Awake()
    {
        inputControls = new PlayerInputMap();
        fire = inputControls.Player.Fire;

        cam = Camera.main;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        currentHealth = maxHealth;
        healthSetEvent.onEventRaised.Invoke(currentHealth);
        currentMoveSpeed = baseMoveSpeed;
        
        levelManager = FindAnyObjectByType<LevelManager>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleJump();
        
        HandlePickupInput();
        HandleGunRotate();

        if (shootOnCD)
        {
            shootTimer -= Time.deltaTime;
        }
        
        if (heldObjectRB != null)
        {
            HandleHoldPointScroll();
            CheckAutoDrop();
        }
        
        if (isInvincible && Time.time - lastDamageTime > invincibilityTime)
        {
            isInvincible = false;
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
        if (heldObjectRB != null)
        {
            HandleHeldObject();
        }
    }

    #endregion

    #region Movement

    void HandleMovement()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current.aKey.isPressed) input.x -= 1;
        if (Keyboard.current.dKey.isPressed) input.x += 1;
        if (Keyboard.current.sKey.isPressed) input.y -= 1;
        if (Keyboard.current.wKey.isPressed) input.y += 1;

        Vector3 moveDir = (mesh.transform.right * input.x + mesh.transform.forward * input.y).normalized;

        CalculateCurrentSpeed();
        float speed = currentMoveSpeed;

        if (Keyboard.current.leftShiftKey.isPressed && input.y > 0)
            speed *= runMultiplier;

        if (!isGrounded)
            speed *= airControlMultiplier;

        Vector3 targetVelocity = moveDir * speed;
        Vector3 velocityChange =
            targetVelocity - new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    void CalculateCurrentSpeed()
    {
        if (heldObjectRB == null)
        {
            currentMoveSpeed = baseMoveSpeed;
        }
        else
        {
            float speedReduction = heldObjectMass * massSlowdownMultiplier;
            currentMoveSpeed = Mathf.Max(baseMoveSpeed - speedReduction, minMoveSpeed);
            currentMoveSpeed = Mathf.Clamp(currentMoveSpeed, minMoveSpeed, baseMoveSpeed);
        }
    }

    void HandleJump()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            float jumpModifier = heldObjectRB != null ? 
                Mathf.Clamp(1f - (heldObjectMass * 0.1f), 0.5f, 1f) : 1f;
            
            rb.AddForce(Vector3.up * jumpForce * jumpModifier, ForceMode.Impulse);
        }
    }

    #endregion

    #region Mouse Look

    void HandleMouseLook()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        yRotation += mouseX;

        orientation.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        mesh.transform.localRotation = Quaternion.Euler(0, orientation.localRotation.eulerAngles.y, 0);
    }

    #endregion

    #region Pickup

    void HandlePickupInput()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (heldObjectRB == null)
            {
                TryPickup();
                //Debug.Log("Input pickup item");
            }
            else
            {
                DropObject();
                //Debug.Log("Input drop item");
            }
        }
        
        if (heldObjectRB != null && Mouse.current.leftButton.wasPressedThisFrame && isCombatPhase)
        {
            ThrowObject();
        }
        if (heldObject)
        {
            SetLazerPoints();
        }
    }

    Rigidbody GetRigidbodyFromHit(Collider collider)
    {
        Rigidbody rb = collider.attachedRigidbody;
        if (rb != null) return rb;

        Transform parent = collider.transform.parent;
        while (parent != null)
        {
            rb = parent.GetComponent<Rigidbody>();
            if (rb != null) return rb;
            parent = parent.parent;
        }
        return null;
    }

    void TryPickup()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayerMask))
        {
            Rigidbody rbHit = GetRigidbodyFromHit(hit.collider);
            if (rbHit != null && rbHit != rb)
            {
                heldObjectRB = rbHit;
                heldObject = heldObjectRB.GetComponent<DraggableItem>();

                heldObject.PickUpItem();

                heldObjectMass = heldObjectRB.mass;
                heldObjectRB.linearDamping = 10f;
                heldObjectRB.angularDamping = 5f;
                currentHoldDistance = Vector3.Distance(cam.transform.position, heldObjectRB.position);

                Beam1.enabled = true;
                Beam2.enabled = true;
                heldObject.SetPickupVFX(true);

                if (pickupSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(pickupSound);
                }
                
                if (heldObjectMass >= heavyThreshold)
                {
                    //Debug.Log($"Picked up HEAVY object: {heldObjectRB.gameObject.name} (Mass: {heldObjectMass})");
                }
                else
                {
                   // Debug.Log($"Picked up: {heldObjectRB.gameObject.name} (Mass: {heldObjectMass})");
                }
            }
        }
    }

    void DropObject()
    {
        if (heldObjectRB == null) return;

        heldObject.SetPickupVFX(false);

        heldObject.DropItem();

        heldObjectRB.linearDamping = 0f;
        heldObjectRB.angularDamping = 0.05f;
        heldObjectMass = 0f;
        heldObjectRB = null;
        heldObject = null;

        Beam1.enabled = false;
        Beam2.enabled = false;


        //Debug.Log("Dropped object");
    }

    void CheckAutoDrop()
    {
        if (heldObjectRB == null) return;
        
        float distanceToObject = Vector3.Distance(orientation.position, heldObjectRB.position);
        
        if (distanceToObject > maxHoldDistanceFromCamera)
        {
            Debug.Log($"Auto-dropping object: Too far away ({distanceToObject:F1} > {maxHoldDistanceFromCamera})");
            DropObject();
            return;
        }
        
        Ray ray = new Ray(orientation.position, (heldObjectRB.position - orientation.position).normalized);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, distanceToObject, blocksHolding))
        {
            if (hit.rigidbody != heldObjectRB && !hit.collider.isTrigger)
            {
                Debug.Log($"Auto-dropping object: Line of sight blocked by {hit.collider.name}");
                DropObject();
            }
        }
    }

    void ThrowObject()
    {
        if (heldObjectRB == null) return;

        Vector3 throwDirection = orientation.forward;
        
        float massAdjustedForce = throwForce * (1f + heldObjectMass * 0.2f);
        
        heldObjectRB.AddForce(throwDirection * massAdjustedForce, ForceMode.Impulse);
        
        if (throwSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(throwSound);
        }

        DropObject();
        
        //Debug.Log($"Threw object with force: {massAdjustedForce:F1}");
    }

    void HandleHeldObject()
    {
        if (heldObjectRB == null) return;

        Vector3 targetPos = orientation.position + orientation.forward * currentHoldDistance;
        Vector3 direction = targetPos - heldObjectRB.position;
        float distance = direction.magnitude;
        
        float massAdjustedForce = pickupForce * (1f + heldObjectMass * 0.1f);
        float adjustedForce = massAdjustedForce * Mathf.Clamp(distance, 0.5f, 3f);
        
        heldObjectRB.AddForce(direction.normalized * adjustedForce, ForceMode.Force);
        heldObjectRB.AddForce(-heldObjectRB.linearVelocity * 2f, ForceMode.Acceleration);
        
        if (heldObjectMass > heavyThreshold)
        {
            heldObjectRB.AddTorque(-heldObjectRB.angularVelocity * 5f, ForceMode.Acceleration);
        }
    }

    void HandleHoldPointScroll()
    {
        if (heldObjectRB == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float massAdjustedScrollSpeed = scrollSpeed / (1f + heldObjectMass * 0.2f);
            currentHoldDistance += scroll * massAdjustedScrollSpeed * Time.deltaTime * 10f;
            currentHoldDistance = Mathf.Clamp(currentHoldDistance, minHoldDistance, maxHoldDistance);
        }
    }

    void SetLazerPoints()
    {
        if (heldObject)
        {
            lineEnd.transform.position = heldObject.transform.position;
        }
        
         
    }

    #endregion

    #region Shooting

    void Shoot(InputAction.CallbackContext ctx)
    {
        if (heldObjectRB != null) return;
        if (shootOnCD || !IsShootingMode()) return;

        GameObject bulletGO = Instantiate(bulletPrefab, bulletSpawnLoc.position, bulletSpawnLoc.rotation);

        Bullet bullet = bulletGO.GetComponent<Bullet>();
        bullet.Init(1, bulletSpeed);
        bullet.Launch();

        shootTimer = shootCooldown;
    }

    void HandleGunRotate()
    {
        Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 50, shootableLayers);

        if (hit.collider != null)
        {
            gun.forward = Vector3.Slerp(gun.forward, (hit.point - gun.position).normalized, Time.deltaTime * 5);
        }
        else
        {
            gun.forward = Vector3.Slerp(gun.forward, (cam.transform.position + (cam.transform.forward * 50)) - gun.position, Time.deltaTime * 5);
        }
    }

    #endregion

    #region Mode Switching

    public void SwitchToCombatMode()
    {
        if (heldObjectRB != null)
        {
            DropObject();
        }

        gun.gameObject.SetActive(true);

        isCombatPhase = true;
        //Debug.Log("Player: Switched to Shooting Mode");
    }

    public void SwitchToPrepMode()
    {
        gun.gameObject.SetActive(false);

        isCombatPhase = false;
        //Debug.Log("Player: Switched to Pickup Mode");
    }

    #endregion

    #region Health

    void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                return;
            }
        }

        CheckForDamage(collision);
    }
    
    void CheckForDamage(Collision collision)
    {
        if (isInvincible || currentHealth <= 0) return;

        float impactForce = collision.impulse.magnitude;
        
        if (impactForce >= damageThreshold)
        {
            Hit(1);
        }
    }

    public void Hit(int damage)
    {
        if (isInvincible) return;

        currentHealth--;
        lastDamageTime = Time.time;
        isInvincible = true;

        updateHealthEvent.onEventRaised.Invoke(currentHealth);

        Debug.Log($"Player took damage! Health: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(DamageFlash());
        }
    }

    public void Die()
    {
        Debug.Log("Player died!");
        
        if (heldObjectRB != null)
        {
            DropObject();
        }
        
        enabled = false;
        rb.isKinematic = true;
    }

    IEnumerator DamageFlash()
    {
        yield return null;
        //Add damage flash
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"Player healed! Health: {currentHealth}/{maxHealth}");
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isInvincible = false;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    #endregion

    #region Ground Check

    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                return;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }

    #endregion

    #region Public Methods

    public bool IsShootingMode()
    {
        return isCombatPhase;
    }

    public bool IsHoldingObject()
    {
        return heldObjectRB != null;
    }

    public float GetHeldObjectMass()
    {
        return heldObjectMass;
    }

    public bool IsCarryingHeavyObject()
    {
        return heldObjectRB != null && heldObjectMass >= heavyThreshold;
    }

    public float GetCurrentSpeed()
    {
        return currentMoveSpeed;
    }

    public void Freeze()
    {
        SwitchToPrepMode();
    }

    public void Unfreeze()
    {
        SwitchToCombatMode();
    }

    #endregion
    
}