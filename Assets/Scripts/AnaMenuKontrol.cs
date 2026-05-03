using UnityEngine;
using UnityEngine.SceneManagement;

public class AnaMenuKontrol : MonoBehaviour
{
    [Header("Menu Muzik")]
    [SerializeField] private AudioClip menuMuzik;
    [SerializeField] [Range(0f, 1f)] private float muzikSes = 0.5f;

    [Header("Paneller")]
    [SerializeField] private GameObject anaPanel;        // Basla, Ayarlar, Cikis butonlari
    [SerializeField] private GameObject ayarlarPanel;    // Ayarlar paneli

    private AudioSource muzikSource;

    void Start()
    {
        // Muzik baslat
        if (menuMuzik != null)
        {
            muzikSource = gameObject.AddComponent<AudioSource>();
            muzikSource.clip = menuMuzik;
            muzikSource.volume = muzikSes;
            muzikSource.loop = true;
            muzikSource.Play();
        }

        // Baslangic: ana panel acik, ayarlar kapali
        if (anaPanel != null) anaPanel.SetActive(true);
        if (ayarlarPanel != null) ayarlarPanel.SetActive(false);
    }

    public void OyunaBasla()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void AyarlariAc()
    {
        if (anaPanel != null) anaPanel.SetActive(false);
        if (ayarlarPanel != null) ayarlarPanel.SetActive(true);
    }

    public void AyarlarGeri()
    {
        if (ayarlarPanel != null) ayarlarPanel.SetActive(false);
        if (anaPanel != null) anaPanel.SetActive(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (ayarlarPanel != null && ayarlarPanel.activeSelf)
            {
                AyarlarGeri();
            }
        }
    }

    public void OyundanCik()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}