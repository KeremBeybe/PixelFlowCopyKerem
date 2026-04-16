using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Settings Panel")]
    public GameObject settingsPanel;
    public GameObject outOfHealthPanel; // Can bitince çýkan uyarý paneli

    [Header("Health (Can) Ayarlarý")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI timerText;
    public int maxHealth = 5;
    public int restoreDuration = 1800; // 30 Dakika = 1800 Saniye
    private int currentHealth;
    private DateTime nextHealthTime;

    [Header("Vibration Switch (0-1 Mantýðý)")]
    public RectTransform vibHandle;      // Kayacak olan beyaz yuvarlak
    public Image vibBackground;          // Arka plan (Renk için)
    public TextMeshProUGUI vibText;      // Ýçindeki "açýk/kapalý" yazýsý

    [Header("Ses (Audio) Slider Ayarlarý")]
    public Slider audioSlider;
    [Header("Level UI")]
    public TextMeshProUGUI levelTxt; // Inspector'dan o pikselli text'i buraya sürükle
    [Header("Pozisyon ve Renk Ayarlarý")]
    public float offX = -46f;            // Deðer 0 (Kapalý) iken X pozisyonu
    public float onX = 46f;              // Deðer 1 (Açýk) iken X pozisyonu
    public Color colorOn = Color.green;
    public Color colorOff = Color.gray;

    private bool isVibOn;

    void Awake () { Instance = this; }

    void Start ()
    {
        // Hafýzadan deðeri çek (1: Açýk, 0: Kapalý)
        int savedVib = PlayerPrefs.GetInt("Vibration", 1);
        isVibOn = (savedVib == 1);

        // Ýlk açýlýþta animasyonsuz ayarla
        UpdateVibVisuals(false);
        LoadHealthData();
        UpdateUI();
        // --- SES BAÞLANGIÇ AYARLARI ---
        if (audioSlider != null)
        {
            // Hafýzadan ses seviyesini çek (Varsayýlan 1.0f yani %100 ses)
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);

            // Slider'ýn çubuðunu o deðere getir
            audioSlider.value = savedVolume;

            // Oyunun genel sesini ayarla
            AudioListener.volume = savedVolume;
        }
    }
    void Update ()
    {
        // Can ful deðilse timer geri saymaya devam etsin
        if (currentHealth < maxHealth)
        {
            HandleTimer();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            UseHealth();
            Debug.Log("Test: Can azaltýldý! Kalan: " + currentHealth);
        }
    }

    // --- BUTONA TIKLANDIÐINDA ÇALIÞACAK ANA FONKSÝYON ---
    public void OnVibClick ()
    {
        // 0-1 Takasý: Deðeri tersine çevir
        isVibOn = !isVibOn;

        // Hafýzaya yeni halini kazý
        PlayerPrefs.SetInt("Vibration", isVibOn ? 1 : 0);

        // Görseli kaydýr ve yazýyý deðiþtir
        UpdateVibVisuals(true);

        if (isVibOn)
        {
#if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate(); // Tokatý basýnca bir titret bakalým
#endif
        }
    }

    public void OnSettingsClosed ()
    {
        settingsPanel.SetActive(false);
    }

    private void UpdateVibVisuals (bool animate)
    {
        float targetX = isVibOn ? onX : offX;
        Color targetColor = isVibOn ? colorOn : colorOff;

        // Yazý güncelleme (Küçük harf takýntýsý bitti!)
        if (vibText != null) vibText.text = isVibOn ? "open" : "close";

        if (animate)
        {
            // .SetEase(Ease.OutBack) ile o tatlý sekme efektini veriyoruz
            vibHandle.DOAnchorPosX(targetX, 0.1f).SetEase(Ease.OutBack);
            vibBackground.DOColor(targetColor, 2f);
        }
        else
        {
            vibHandle.anchoredPosition = new Vector2(targetX, 0f);
            vibBackground.color = targetColor;
        }
    }
    public void OnAudioSliderChanged ()
    {
        if (audioSlider != null)
        {
            // 1. Oyunun tüm sesini slider'ýn deðerine (0.0 ile 1.0 arasý) eþitle
            AudioListener.volume = audioSlider.value;

            // 2. Bu ayarý hafýzaya kaydet ki oyuna tekrar girince sýfýrlanmasýn
            PlayerPrefs.SetFloat("MasterVolume", audioSlider.value);
        }
    }
    // Level numarasýný ekrana yazdýran fonksiyon
    public void UpdateLevelText (int levelIndex)
    {
        if (levelTxt != null)
        {
            // Level index genelde 0'dan baþlar, o yüzden +1 ekleyip "LEVEL 1" yazdýrýyoruz
            levelTxt.text = (levelIndex + 1) + ". SEVÝYE ".ToString();
        }
    }
    public void StartGameFromUI ()
    {
        // 1. Can kontrolü
        if (!CanPlay())
        {
            if (outOfHealthPanel != null) outOfHealthPanel.SetActive(true);
            Debug.Log("Canýn yok kanka, 30 dk bekle ya da reklam izle!");
            return;
        }

        // 2. Panelleri yönet (LevelMenuManager'daki referanslarý kullanacaðýz)
        // Bunun için LevelMenuManager'a bir referans alabiliriz veya 
        // panelleri direkt UIManager'a da sürükleyebilirsin.
        LevelMenuManager menuScript = FindObjectOfType<LevelMenuManager>();

        if (menuScript != null)
        {
            menuScript.mainMenuPanel.SetActive(false);
            menuScript.gameHUDPanel.SetActive(true);
        }

        // 3. Oyunu kur ve baþlat
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.StartActiveLevel();
        }
    }
    public void ToggleSettingsPanel () => settingsPanel.SetActive(!settingsPanel.activeSelf);

    #region Health Logic (Can Mantýðý)
    void LoadHealthData ()
    {
        currentHealth = PlayerPrefs.GetInt("CurrentHealth", maxHealth);

        // En son ne zaman can dolduðunu kontrol et (Offline ilerleme)
        string lastTimeStr = PlayerPrefs.GetString("NextHealthTime", string.Empty);

        if (!string.IsNullOrEmpty(lastTimeStr))
        {
            nextHealthTime = DateTime.Parse(lastTimeStr);

            // Eðer geçen sürede canlarýn dolma vakti geldiyse hesapla
            while (DateTime.Now > nextHealthTime && currentHealth < maxHealth)
            {
                currentHealth++;
                nextHealthTime = nextHealthTime.AddSeconds(restoreDuration);
            }
        }
        else
        {
            nextHealthTime = DateTime.Now;
        }

        SaveHealthData();
    }

    void HandleTimer ()
    {
        TimeSpan timeRemaining = nextHealthTime - DateTime.Now;

        if (timeRemaining.TotalSeconds <= 0)
        {
            currentHealth++;
            if (currentHealth < maxHealth)
                nextHealthTime = DateTime.Now.AddSeconds(restoreDuration);

            SaveHealthData();
            UpdateUI();
        }
        else
        {
            // 29:59 formatýnda yazdýr
            timerText.text = string.Format("{0:D2}:{1:D2}", timeRemaining.Minutes, timeRemaining.Seconds);
        }
    }

    public void UseHealth ()
    {
        if (currentHealth > 0)
        {
            currentHealth--;
            // Ýlk can harcandýðýnda timer baþlasýn
            if (currentHealth == maxHealth - 1)
                nextHealthTime = DateTime.Now.AddSeconds(restoreDuration);

            SaveHealthData();
            UpdateUI();
        }
    }

    public bool CanPlay () => currentHealth > 0;

    void SaveHealthData ()
    {
        PlayerPrefs.SetInt("CurrentHealth", currentHealth);
        PlayerPrefs.SetString("NextHealthTime", nextHealthTime.ToString());
        PlayerPrefs.Save();
    }

    public void UpdateUI ()
    {
        healthText.text = currentHealth.ToString();
        if (currentHealth >= maxHealth) timerText.text = "MAX";
    }
    #endregion
}