using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Görev listesindeki tek bir satır.
/// TaskListUI tarafından oluşturulur ve yönetilir.
/// </summary>
public class TaskItemUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image checkmark;
    [SerializeField] private Image strikethrough;   // Üstü çizili efekti

    private Task linkedTask;

    /// <summary>
    /// Görev bilgisiyle kurulur.
    /// </summary>
    public void Setup(Task task, Color color)
    {
        linkedTask = task;

        if (titleText != null)
        {
            titleText.text = task.Title;
            titleText.color = color;
        }

        if (checkmark != null)
        {
            checkmark.enabled = false;
        }

        if (strikethrough != null)
        {
            strikethrough.enabled = false;
        }
    }

    /// <summary>
    /// Görevi tamamlanmış olarak işaretler (rutin uyumlu).
    /// </summary>
    public void MarkCompleted(Color color)
    {
        if (titleText != null)
        {
            titleText.color = color;
            titleText.fontStyle = FontStyles.Strikethrough; // Üstü çizili
        }

        if (checkmark != null)
        {
            checkmark.enabled = true;
            checkmark.color = color;
        }
    }

    /// <summary>
    /// Görevi isyanla tamamlanmış olarak işaretler.
    /// </summary>
    public void MarkRebelled(Color color)
    {
        if (titleText != null)
        {
            titleText.color = color;
            titleText.fontStyle = FontStyles.Strikethrough | FontStyles.Bold;

            // İsyankar tamamlama: metin kırılmış gibi görünsün
            StartCoroutine(GlitchTextEffect());
        }

        if (checkmark != null)
        {
            checkmark.enabled = true;
            checkmark.color = color;
        }
    }

    private System.Collections.IEnumerator GlitchTextEffect()
    {
        if (titleText == null) yield break;

        string originalText = linkedTask.Title;
        
        // Kısa bir glitch efekti
        for (int i = 0; i < 5; i++)
        {
            titleText.text = GlitchString(originalText);
            yield return new WaitForSeconds(0.05f);
        }

        titleText.text = originalText;
    }

    private string GlitchString(string input)
    {
        char[] chars = input.ToCharArray();
        int glitchCount = Random.Range(1, Mathf.Max(2, chars.Length / 3));

        for (int i = 0; i < glitchCount; i++)
        {
            int idx = Random.Range(0, chars.Length);
            chars[idx] = (char)Random.Range(33, 126); // Rastgele ASCII
        }

        return new string(chars);
    }
}
