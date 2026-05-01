using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Görev listesi UI'ı. Sol üstte görevleri gösterir.
/// Tamamlanan görevler üstü çizili, isyanla yapılanlar kırmızı görünür.
/// </summary>
public class TaskListUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform taskContainer;       // Görev itemlerinin parent'ı
    [SerializeField] private GameObject taskItemPrefab;      // Görev satırı prefab'ı

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color completedColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
    [SerializeField] private Color rebelledColor = new Color(1f, 0.3f, 0.3f, 0.8f);
    [SerializeField] private Color hiddenRevealColor = new Color(0.8f, 0.4f, 1f);

    private Dictionary<string, TaskItemUI> taskItems = new Dictionary<string, TaskItemUI>();

    void Start()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnTaskAdded += OnTaskAdded;
            TaskManager.Instance.OnTaskCompleted += OnTaskCompleted;
            TaskManager.Instance.OnTaskRebelled += OnTaskRebelled;
        }

        // Kırılma seviyesine göre UI bozulması
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFractureStageChanged += OnFractureStageChanged;
        }
    }

    private void OnTaskAdded(Task task)
    {
        if (taskItemPrefab == null || taskContainer == null) return;
        if (taskItems.ContainsKey(task.Id)) return;

        GameObject item = Instantiate(taskItemPrefab, taskContainer);
        TaskItemUI itemUI = item.GetComponent<TaskItemUI>();

        if (itemUI != null)
        {
            itemUI.Setup(task, task.IsRevealed ? hiddenRevealColor : normalColor);
            taskItems[task.Id] = itemUI;
        }
    }

    private void OnTaskCompleted(Task task)
    {
        if (taskItems.TryGetValue(task.Id, out TaskItemUI itemUI))
        {
            itemUI.MarkCompleted(completedColor);
        }
    }

    private void OnTaskRebelled(Task task)
    {
        if (taskItems.TryGetValue(task.Id, out TaskItemUI itemUI))
        {
            itemUI.MarkRebelled(rebelledColor);
        }
    }

    private void OnFractureStageChanged(FractureStage stage)
    {
        // Kırılma arttıkça görev listesi UI'ı da bozulsun
        switch (stage)
        {
            case FractureStage.Noticeable:
                // Hafif titreme
                StartCoroutine(UIShake(0.5f, 2f));
                break;
            case FractureStage.Breaking:
                // Sürekli titreme
                StartCoroutine(UIShake(1f, -1f)); // -1 = sonsuz
                break;
        }
    }

    private System.Collections.IEnumerator UIShake(float intensity, float duration)
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null) yield break;

        Vector2 originalPos = rect.anchoredPosition;
        float elapsed = 0f;

        while (duration < 0f || elapsed < duration)
        {
            Vector2 offset = new Vector2(
                Random.Range(-intensity, intensity),
                Random.Range(-intensity, intensity)
            );
            rect.anchoredPosition = originalPos + offset;
            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.anchoredPosition = originalPos;
    }

    /// <summary>
    /// Tüm görev itemlerini temizler (gün geçişinde).
    /// </summary>
    public void ClearTasks()
    {
        foreach (var item in taskItems.Values)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        taskItems.Clear();
    }

    void OnDestroy()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.OnTaskAdded -= OnTaskAdded;
            TaskManager.Instance.OnTaskCompleted -= OnTaskCompleted;
            TaskManager.Instance.OnTaskRebelled -= OnTaskRebelled;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFractureStageChanged -= OnFractureStageChanged;
        }
    }
}
