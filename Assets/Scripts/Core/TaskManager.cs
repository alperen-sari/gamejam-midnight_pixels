using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Görev (task) sistemi. Her gün oyuncuya görevler verir,
/// tamamlanma durumunu takip eder.
/// </summary>
public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private List<Task> currentTasks = new List<Task>();

    // Events
    public System.Action<Task> OnTaskAdded;
    public System.Action<Task> OnTaskCompleted;
    public System.Action<Task> OnTaskRebelled;      // Görev rutine aykırı yapıldığında
    public System.Action OnAllTasksCompleted;

    public IReadOnlyList<Task> CurrentTasks => currentTasks;

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
        // Gün değiştiğinde yeni görevler yükle
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged += LoadTasksForDay;
        }

        // İlk gün görevlerini yükle
        LoadTasksForDay(1);
    }

    /// <summary>
    /// Belirli bir gün için görevleri yükler.
    /// </summary>
    public void LoadTasksForDay(int day)
    {
        currentTasks.Clear();

        switch (day)
        {
            case 1:
                AddTask("kahve_al", "Kahve Al", "Kahve makinesinden kahve al.", TaskType.Routine);
                AddTask("mail_oku", "Mailleri Kontrol Et", "Bilgisayarında mailleri oku.", TaskType.Routine);
                AddTask("evrak_teslim", "Evrakları Teslim Et", "Yazıcıdan evrakları al ve patrona götür.", TaskType.Routine);
                AddTask("rapor_yaz", "Rapor Yaz", "Masanda günlük raporu tamamla.", TaskType.Routine);
                AddTask("toplanti", "Toplantıya Katıl", "Saat 15:00 toplantısına git.", TaskType.Routine);
                break;

            case 2:
                AddTask("kahve_al", "Kahve Al", "Kahve makinesinden kahve al... ya da alma?", TaskType.Routine);
                AddTask("mail_oku", "Mailleri Kontrol Et", "Bilgisayarında mailleri oku. Garip bir mail var.", TaskType.Routine);
                AddTask("evrak_teslim", "Evrakları Teslim Et", "Yazıcıdan evrakları al. Ama nereye götüreceksin?", TaskType.Routine);
                AddTask("rapor_yaz", "Rapor Yaz", "Aynı rapor. Yine. Tekrar.", TaskType.Routine);
                AddTask("gizli_oda", "???", "Koridorun sonundaki kapı açık...", TaskType.Rebellion, true);
                break;

            case 3:
                AddTask("kahve_al", "K̷a̷h̷v̷e̷ Al", "Makine sana bakıyor.", TaskType.Routine);
                AddTask("mail_oku", "Son Mail", "Sadece bir mail var. Senden.", TaskType.Routine);
                AddTask("karar", "Karar Ver", "Kapıdan çık ya da masana otur.", TaskType.Rebellion);
                break;
        }

        Debug.Log($"[TaskManager] Gün {day} görevleri yüklendi. Toplam: {currentTasks.Count}");
    }

    private void AddTask(string id, string title, string description, TaskType type, bool isHidden = false)
    {
        Task task = new Task(id, title, description, type, isHidden);
        currentTasks.Add(task);
        
        if (!isHidden)
        {
            OnTaskAdded?.Invoke(task);
        }
    }

    /// <summary>
    /// Görevi normal şekilde tamamlar (rutine uygun).
    /// </summary>
    public void CompleteTask(string taskId)
    {
        Task task = FindTask(taskId);
        if (task == null || task.IsCompleted) return;

        task.Complete(false);
        Debug.Log($"[TaskManager] Görev tamamlandı (rutin): {task.Title}");
        OnTaskCompleted?.Invoke(task);

        CheckAllTasksCompleted();
    }

    /// <summary>
    /// Görevi isyankar şekilde tamamlar (rutini kırarak).
    /// Kırılma seviyesini artırır.
    /// </summary>
    public void RebelTask(string taskId, float fractureAmount = 10f)
    {
        Task task = FindTask(taskId);
        if (task == null || task.IsCompleted) return;

        task.Complete(true);
        Debug.Log($"[TaskManager] Görev isyanla tamamlandı: {task.Title} (+{fractureAmount} kırılma)");

        // Kırılma seviyesini artır
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddFracture(fractureAmount);
        }

        OnTaskRebelled?.Invoke(task);

        // Gizli görevleri açığa çıkar
        RevealHiddenTasks();

        CheckAllTasksCompleted();
    }

    /// <summary>
    /// İsyan yapıldığında gizli görevler görünür olur.
    /// </summary>
    private void RevealHiddenTasks()
    {
        foreach (var task in currentTasks)
        {
            if (task.IsHidden && !task.IsRevealed)
            {
                task.Reveal();
                OnTaskAdded?.Invoke(task);
                Debug.Log($"[TaskManager] Gizli görev açığa çıktı: {task.Title}");
            }
        }
    }

    private Task FindTask(string taskId)
    {
        return currentTasks.Find(t => t.Id == taskId);
    }

    private void CheckAllTasksCompleted()
    {
        bool allDone = true;
        foreach (var task in currentTasks)
        {
            if (!task.IsCompleted && !task.IsHidden)
            {
                allDone = false;
                break;
            }
        }

        if (allDone)
        {
            Debug.Log("[TaskManager] Tüm görevler tamamlandı!");
            OnAllTasksCompleted?.Invoke();
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged -= LoadTasksForDay;
        }
    }
}

/// <summary>
/// Tek bir görevi temsil eder.
/// </summary>
[System.Serializable]
public class Task
{
    public string Id;
    public string Title;
    public string Description;
    public TaskType Type;
    public bool IsCompleted;
    public bool WasRebelled;     // Rutine aykırı mı tamamlandı?
    public bool IsHidden;        // Gizli görev mi?
    public bool IsRevealed;      // Gizli görev açığa çıktı mı?

    public Task(string id, string title, string description, TaskType type, bool isHidden = false)
    {
        Id = id;
        Title = title;
        Description = description;
        Type = type;
        IsHidden = isHidden;
        IsCompleted = false;
        WasRebelled = false;
        IsRevealed = false;
    }

    public void Complete(bool rebelled)
    {
        IsCompleted = true;
        WasRebelled = rebelled;
    }

    public void Reveal()
    {
        IsRevealed = true;
    }
}

/// <summary>
/// Görev tipi.
/// </summary>
public enum TaskType
{
    Routine,     // Normal rutin görev (kahve al, mail oku)
    Rebellion    // İsyankar görev (rutini kır)
}
