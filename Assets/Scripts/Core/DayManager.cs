using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gün geçişlerini yönetir. Tüm görevler bitince günü ilerleterek
/// geçiş efekti ve yeni görevleri tetikler.
/// </summary>
public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    [Header("Day Transition")]
    [SerializeField] private float dayTransitionDuration = 2f;

    // Events
    public System.Action OnDayTransitionStarted;
    public System.Action OnDayTransitionEnded;

    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnAllTasksCompleted += OnAllTasksDone;
        }
    }

    /// <summary>
    /// Tüm görevler bittiğinde çağrılır.
    /// NOT: Otomatik geçiş kapalı — PlayerDesk üzerinden yapılıyor.
    /// </summary>
    private void OnAllTasksDone()
    {
        Debug.Log("[DayManager] Tüm görevler bitti. Masaya dön ve günü bitir.");
        // PlayerDesk "Günü Bitir" seçeneği sunar, otomatik geçiş yok
    }

    /// <summary>
    /// Gün geçiş sekansını başlatır.
    /// </summary>
    public void StartDayTransition()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        OnDayTransitionStarted?.Invoke();

        // Oyuncuyu durdur
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.SetCanMove(false);
        }

        Debug.Log("[DayManager] Gün geçişi başladı...");

        // Fade out → gün ilerle → fade in
        StartCoroutine(DayTransitionSequence());
    }

    private System.Collections.IEnumerator DayTransitionSequence()
    {
        // Fade out (UI tarafından dinlenecek)
        yield return new WaitForSeconds(dayTransitionDuration * 0.5f);

        // Günü ilerlet
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AdvanceDay();
        }

        // Oyuncuyu başlangıç pozisyonuna taşı (opsiyonel)
        ResetPlayerPosition();

        yield return new WaitForSeconds(dayTransitionDuration * 0.5f);

        // Oyuncuyu serbest bırak
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.SetCanMove(true);
        }

        isTransitioning = false;
        OnDayTransitionEnded?.Invoke();

        Debug.Log($"[DayManager] Gün geçişi tamamlandı. Şu an: Gün {GameManager.Instance?.CurrentDay}");
    }

    [Header("Spawn")]
    [SerializeField] private Transform playerSpawnPoint;  // Inspector'dan ata

    private void ResetPlayerPosition()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player == null) return;

        if (playerSpawnPoint != null)
        {
            player.transform.position = playerSpawnPoint.position;
        }
        // SpawnPoint atanmamışsa oyuncu yerinde kalır
    }

    void OnDestroy()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnAllTasksCompleted -= OnAllTasksDone;
        }
    }
}
