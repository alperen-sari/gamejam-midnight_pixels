using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gün geçiş ekranı: fade in/out efekti ve gün numarası gösterimi.
/// DayManager eventlerini dinler.
/// </summary>
public class DayTransitionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup fadePanel;           // Siyah panel (fade için)
    [SerializeField] private TMPro.TextMeshProUGUI dayText;  // "GÜN 2" yazısı

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float dayTextDisplayTime = 1.5f;

    void Start()
    {
        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.blocksRaycasts = false;
        }

        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayTransitionStarted += StartFadeOut;
            DayManager.Instance.OnDayTransitionEnded += StartFadeIn;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged += ShowDayText;
        }
    }

    private void StartFadeOut()
    {
        StartCoroutine(Fade(0f, 1f));
    }

    private void StartFadeIn()
    {
        StartCoroutine(Fade(1f, 0f));
    }

    private System.Collections.IEnumerator Fade(float from, float to)
    {
        if (fadePanel == null) yield break;

        fadePanel.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            fadePanel.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        fadePanel.alpha = to;
        fadePanel.blocksRaycasts = to > 0.5f; // Fade out'da blokla, fade in'de kaldır
    }

    private void ShowDayText(int day)
    {
        if (dayText != null)
        {
            // Kırılma seviyesine göre gün yazısı değişir
            if (GameManager.Instance != null)
            {
                FractureStage stage = GameManager.Instance.CurrentFractureStage;
                switch (stage)
                {
                    case FractureStage.Subtle:
                        dayText.text = $"GÜN {day}";
                        break;
                    case FractureStage.Noticeable:
                        dayText.text = $"G̈ÜN {day}...?";
                        break;
                    case FractureStage.Breaking:
                        dayText.text = $"G̷Ü̷N̷ {day}";
                        break;
                }
            }
            else
            {
                dayText.text = $"GÜN {day}";
            }

            StartCoroutine(ShowAndHideDayText());
        }
    }

    private System.Collections.IEnumerator ShowAndHideDayText()
    {
        if (dayText == null) yield break;

        dayText.gameObject.SetActive(true);
        
        // Fade in
        Color c = dayText.color;
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            c.a = Mathf.Lerp(0f, 1f, elapsed / 0.5f);
            dayText.color = c;
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(dayTextDisplayTime);

        // Fade out
        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            c.a = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
            dayText.color = c;
            elapsed += Time.deltaTime;
            yield return null;
        }

        dayText.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayTransitionStarted -= StartFadeOut;
            DayManager.Instance.OnDayTransitionEnded -= StartFadeIn;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged -= ShowDayText;
        }
    }
}
