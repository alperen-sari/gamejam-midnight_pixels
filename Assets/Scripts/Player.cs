using UnityEngine;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float sprintSpeed = 6.5f;

    [Header("Interaction")]
    [SerializeField] private float interactionRange = 1.5f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Sound")]
    [SerializeField] private AudioClip footstepClip;      // Yürüme sesi (loop)

    private Rigidbody2D rb;
    private Animator animator;
    private AudioSource footstepSource;

    private Vector2 moveInput;
    private Vector2 lastMoveDirection;
    private bool isSprinting;
    private bool canMove = true;

    // Basit envanter — taşınan eşyaları tutar ("kahve", "evrak" vb.)
    private List<string> inventory = new List<string>();

    // Animator parameter hashes — Blend Tree'deki parametre isimleriyle eşleşmeli
    private static readonly int AnimMoveX = Animator.StringToHash("moveX");
    private static readonly int AnimMoveY = Animator.StringToHash("moveY");
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        // Yürüme sesi için AudioSource oluştur
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.clip = footstepClip;
        footstepSource.loop = true;
        footstepSource.playOnAwake = false;
        footstepSource.volume = 0.4f;
    }

    void Update()
    {
        if (!canMove) 
        {
            moveInput = Vector2.zero;
            return;
        }

        HandleInput();
        HandleInteraction();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleInput()
    {
        // WASD / Arrow Keys
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput.Normalize(); // Çapraz hareketin daha hızlı olmasını engelle

        // Sprint
        isSprinting = Input.GetKey(KeyCode.LeftShift);

        // Son hareket yönünü kaydet (idle animasyonu için)
        if (moveInput.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = moveInput;
        }
    }

    private void HandleMovement()
    {
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
        rb.linearVelocity = moveInput * currentSpeed;
    }

    private void HandleInteraction()
    {
        // ChoiceUI veya Diyalog açıkken yeni etkileşim başlatma
        if (ChoiceUI.Instance != null && ChoiceUI.Instance.IsOpen) return;
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive) return;

        // Her frame yakındaki etkileşimli objeyi tara
        DetectInteractable();

        // E'ye basınca etkileşime gir
        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            currentInteractable.Interact(this);
        }
    }

    // Şu an yakındaki etkileşimli obje (prompt UI bunu okur)
    private IInteractable currentInteractable;
    public IInteractable CurrentInteractable => currentInteractable;

    private void DetectInteractable()
    {
        // Oyuncunun etrafında dairesel tarama yap
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            (Vector2)transform.position,
            interactionRange,
            interactableLayer
        );

        // En yakın etkileşimli objeyi bul
        IInteractable closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract())
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = interactable;
                }
            }
        }

        currentInteractable = closest;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        animator.SetFloat(AnimSpeed, isMoving ? 1f : 0f);
        animator.SetFloat(AnimMoveX, lastMoveDirection.x);
        animator.SetFloat(AnimMoveY, lastMoveDirection.y);

        // Yürüme sesi kontrolü
        if (footstepSource != null && footstepClip != null)
        {
            if (isMoving && !footstepSource.isPlaying)
            {
                footstepSource.Play();
            }
            else if (!isMoving && footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }
        }
    }

    /// <summary>
    /// Hareketi kilitler/açar. Diyalog, görev vs. sırasında kullanılır.
    /// </summary>
    public void SetCanMove(bool value)
    {
        canMove = value;
        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>
    /// Aksiyon animasyonu oynatır (drinking_coffee vb.)
    /// Hareket durdurulur, animasyon oynar, süre bitince callback çağrılır.
    /// </summary>
    /// <param name="animTriggerName">Animator'daki trigger parametresinin adı</param>
    /// <param name="duration">Animasyonun süresi (saniye)</param>
    /// <param name="onComplete">Animasyon bitince çağrılacak fonksiyon</param>
    public void PlayAction(string animTriggerName, float duration, System.Action onComplete = null)
    {
        StartCoroutine(ActionRoutine(animTriggerName, duration, onComplete));
    }

    private System.Collections.IEnumerator ActionRoutine(string triggerName, float duration, System.Action onComplete)
    {
        // Hareketi durdur
        SetCanMove(false);
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        // Blend Tree değerlerini sıfırla ki yürüme animasyonu durSun
        if (animator != null)
        {
            animator.SetFloat(AnimMoveX, 0f);
            animator.SetFloat(AnimMoveY, 0f);
            animator.SetTrigger(triggerName);
        }

        // Animasyon süresince bekle
        yield return new WaitForSeconds(duration);

        // Bitince callback
        onComplete?.Invoke();
    }

    // ==================== Envanter ====================

    /// <summary>
    /// Envantere eşya ekler.
    /// </summary>
    public void AddItem(string itemId)
    {
        if (!inventory.Contains(itemId))
        {
            inventory.Add(itemId);
            Debug.Log($"[Player] Eşya alındı: {itemId}");
        }
    }

    /// <summary>
    /// Envanterden eşya çıkarır.
    /// </summary>
    public bool RemoveItem(string itemId)
    {
        bool removed = inventory.Remove(itemId);
        if (removed)
        {
            Debug.Log($"[Player] Eşya bırakıldı: {itemId}");
        }
        return removed;
    }

    /// <summary>
    /// Envanterde belirtilen eşya var mı?
    /// </summary>
    public bool HasItem(string itemId)
    {
        return inventory.Contains(itemId);
    }

    public Vector2 GetLastMoveDirection() => lastMoveDirection;
    public bool IsMoving() => moveInput.sqrMagnitude > 0.01f;

    void OnDrawGizmosSelected()
    {
        // Etkileşim alanını editörde göster
        Gizmos.color = Color.yellow;
        Vector2 interactPos = (Vector2)transform.position + (Application.isPlaying ? lastMoveDirection : Vector2.down) * 0.5f;
        Gizmos.DrawWireSphere(interactPos, interactionRange);
    }
}
