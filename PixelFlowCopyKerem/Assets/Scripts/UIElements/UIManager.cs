using DG.Tweening;
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

    [Header("Vibration Switch (0-1 Mantýðý)")]
    public RectTransform vibHandle;      // Kayacak olan beyaz yuvarlak
    public Image vibBackground;          // Arka plan (Renk için)
    public TextMeshProUGUI vibText;      // Ýçindeki "açýk/kapalý" yazýsý

    [Header("Ses (Audio) Slider Ayarlarý")]
    public Slider audioSlider;
    [Header("Level UI")]
    public TextMeshProUGUI levelTxt; // Inspector'dan o pikselli text'i buraya sürükle
    [Header("Pozisyon ve Renk Ayarlarý")]
    public float offX = -50f;            // Deðer 0 (Kapalý) iken X pozisyonu
    public float onX = 50f;              // Deðer 1 (Açýk) iken X pozisyonu
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

    private void UpdateVibVisuals (bool animate)
    {
        float targetX = isVibOn ? onX : offX;
        Color targetColor = isVibOn ? colorOn : colorOff;

        // Yazý güncelleme (Küçük harf takýntýsý bitti!)
        if (vibText != null) vibText.text = isVibOn ? "open" : "close";

        if (animate)
        {
            // .SetEase(Ease.OutBack) ile o tatlý sekme efektini veriyoruz
            vibHandle.DOAnchorPosX(targetX, 0.25f).SetEase(Ease.OutBack);
            vibBackground.DOColor(targetColor, 0.25f);
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
            levelTxt.text = (levelIndex + 1) + ". SEVÝYE " .ToString();
        }
    }
    public void ToggleSettingsPanel () => settingsPanel.SetActive(!settingsPanel.activeSelf);
}