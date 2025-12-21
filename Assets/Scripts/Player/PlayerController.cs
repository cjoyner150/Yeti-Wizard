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

    [Header("Mouse Look")]
    public Transform cameraHolder;
    public float mouseSensitivity = 10f;

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
    public LineRenderer Beam;

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
    
    [Header("Visual Feedback")]
    public AudioClip pickupSound;
    public AudioClip throwSound;
    private AudioSource audioSource;

    [Header("Events")]
    [SerializeField] VoidEventSO freezeEvent;
    [SerializeField] VoidEventSO unfreezeEvent;
    
    // Private vars
    private float currentHoldDistance;
    private bool isGrounded;
    private Rigidbody rb;
    private Rigidbody heldObjectRB;
    private DraggableItem heldObject;
    private float xRotation;
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
    }

    private void OnDisable()
    {
        freezeEvent.onEventRaised -= Freeze;
        unfreezeEvent.onEventRaised -= Unfreeze;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        currentHealth = maxHealth;
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

        Vector3 moveDir = transform.right * input.x + transform.forward * input.y;

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

        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    #endregion

    #region Pickup

    void HandlePickupInput()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (heldObjectRB == null)
                TryPickup();
            else
                DropObject();
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
        Ray ray = new Ray(cameraHolder.position, cameraHolder.forward);

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
                currentHoldDistance = Vector3.Distance(cameraHolder.position, heldObjectRB.position);

                Beam.enabled = true;
                heldObject.SetPickupVFX(true);

                if (pickupSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(pickupSound);
                }
                
                if (heldObjectMass >= heavyThreshold)
                {
                    Debug.Log($"Picked up HEAVY object: {heldObjectRB.gameObject.name} (Mass: {heldObjectMass})");
                }
                else
                {
                    Debug.Log($"Picked up: {heldObjectRB.gameObject.name} (Mass: {heldObjectMass})");
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

        Beam.enabled = false;
        

        Debug.Log("Dropped object");
    }

    void CheckAutoDrop()
    {
        if (heldObjectRB == null) return;
        
        float distanceToObject = Vector3.Distance(cameraHolder.position, heldObjectRB.position);
        
        if (distanceToObject > maxHoldDistanceFromCamera)
        {
            Debug.Log($"Auto-dropping object: Too far away ({distanceToObject:F1} > {maxHoldDistanceFromCamera})");
            DropObject();
            return;
        }
        
        Ray ray = new Ray(cameraHolder.position, (heldObjectRB.position - cameraHolder.position).normalized);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, distanceToObject))
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

        Vector3 throwDirection = cameraHolder.forward;
        
        float massAdjustedForce = throwForce * (1f + heldObjectMass * 0.2f);
        
        heldObjectRB.AddForce(throwDirection * massAdjustedForce, ForceMode.Impulse);
        
        if (throwSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(throwSound);
        }
        
        heldObjectRB.useGravity = true;
        heldObjectRB.linearDamping = 0f;
        heldObjectRB.angularDamping = 0.05f;
        heldObjectMass = 0f;
        heldObjectRB = null;
        heldObject = null;
        
        Debug.Log($"Threw object with force: {massAdjustedForce:F1}");
    }

    void HandleHeldObject()
    {
        if (heldObjectRB == null) return;

        Vector3 targetPos = cameraHolder.position + cameraHolder.forward * currentHoldDistance;
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

    #region Mode Switching

    public void SwitchToCombatMode()
    {
        if (heldObjectRB != null)
        {
            DropObject();
        }
        
        isCombatPhase = true;
        Debug.Log("Player: Switched to Shooting Mode");
    }

    public void SwitchToPrepMode()
    {
        isCombatPhase = false;
        Debug.Log("Player: Switched to Pickup Mode");
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

        float impactForce = collision.relativeVelocity.magnitude;
        
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