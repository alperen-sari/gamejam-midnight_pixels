using UnityEngine;

/// <summary>
/// Bilgisayar masası. İki ana işlev:
///   1) Raporu Yaz → mini-game
///   2) Maile Bak → mail okuma ekranı
/// 
/// Her ikisi de ayrı ayrı tamamlanabilir (aynı gün içinde).
/// </summary>
public class Computer : MonoBehaviour, IInteractable
{
    [Header("Task IDs")]
    [SerializeField] private string reportTaskId = "rapor_yaz";
    [SerializeField] private string mailTaskId = "mail_oku";

    [Header("Trust Impact")]
    [SerializeField] private float trustGainOnReport = 8f;
    [SerializeField] private float trustLossOnDoodle = 10f;
    [SerializeField] private float fractureGainOnDoodle = 15f;

    [Header("Sound")]
    [SerializeField] private AudioClip typingSound;          // Rapor yazarken
    [SerializeField] private AudioClip mailOpenSound;        // Mail açınca

    private bool reportDone = false;
    private bool mailDone = false;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayChanged += (_) => { reportDone = false; mailDone = false; };
        }
    }

    public void Interact(Player player)
    {
        if (!CanInteract()) return;

        // Hangi seçenekler mevcut?
        if (!reportDone && !mailDone)
        {
            // İkisi de açık
            ChoiceUI.Instance?.ShowChoices(
                ("Raporu Yaz", () => StartReport(player)),
                ("Maile Bak", () => StartMail(player))
            );
        }
        else if (!reportDone)
        {
            ChoiceUI.Instance?.ShowChoices(
                ("Raporu Yaz", () => StartReport(player)),
                ("Vazgeç", () => { })
            );
        }
        else if (!mailDone)
        {
            ChoiceUI.Instance?.ShowChoices(
                ("Maile Bak", () => StartMail(player)),
                ("Vazgeç", () => { })
            );
        }
    }

    public string GetInteractionPrompt()
    {
        return "[E] Bilgisayarı Kullan";
    }

    public bool CanInteract()
    {
        return !reportDone || !mailDone;
    }

    // ==================== Rapor ====================

    private void StartReport(Player player)
    {
        SFXManager.Play(typingSound, transform.position);

        if (ReportMiniGame.Instance != null)
        {
            // Önce normal/doodle seçimi
            ChoiceUI.Instance?.ShowChoices(
                ("Normal Rapor Yaz", () => RunMiniGame(player, false)),
                ("Doodle Çiz", () => RunMiniGame(player, true))
            );
        }
        else
        {
            FinishReport(player, true, false);
        }
    }

    private void RunMiniGame(Player player, bool isRebellion)
    {
        ReportMiniGame.Instance.StartGame(isRebellion, (success) =>
        {
            FinishReport(player, success, isRebellion);
        });
    }

    private void FinishReport(Player player, bool success, bool isRebellion)
    {
        reportDone = true;

        if (!success)
        {
            DialogueSystem.Instance?.StartDialogue(new DialogueLine[]
            {
                new DialogueLine("", "Raporu bitiremedim... Çok fazla hata yaptım."),
                new DialogueLine("", "Müdür bunu fark edecek.")
            });
            GameManager.Instance?.ReduceBossTrust(5f);
            return;
        }

        if (isRebellion)
        {
            DialogueSystem.Instance?.StartDialogue(new DialogueLine[]
            {
                new DialogueLine("", "Rapor yerine saçma sapan şeyler yazdın."),
                new DialogueLine("", "İçinden bir şeylerin kırıldığını hissediyorsun."),
                new DialogueLine("", "...ama aynı zamanda özgürleştiğini de.")
            });
            GameManager.Instance?.ReduceBossTrust(trustLossOnDoodle);
            GameManager.Instance?.AddFracture(fractureGainOnDoodle);
        }
        else
        {
            DialogueSystem.Instance?.StartDialogue(new DialogueLine[]
            {
                new DialogueLine("", "Rapor tamamlandı. Her zamanki gibi.")
            });
            TaskManager.Instance?.CompleteTask(reportTaskId);
            GameManager.Instance?.AddBossTrust(trustGainOnReport);
        }
    }

    // ==================== Mail ====================

    private void StartMail(Player player)
    {
        SFXManager.Play(mailOpenSound, transform.position);

        if (MailSystem.Instance != null)
        {
            MailSystem.Instance.OpenMails(() =>
            {
                mailDone = true;
                TaskManager.Instance?.CompleteTask(mailTaskId);
            });
        }
        else
        {
            // Fallback
            mailDone = true;
            DialogueSystem.Instance?.StartDialogue(new DialogueLine[]
            {
                new DialogueLine("", "Mailleri kontrol ettin. Önemli bir şey yok."),
            });
            TaskManager.Instance?.CompleteTask(mailTaskId);
        }
    }
}
