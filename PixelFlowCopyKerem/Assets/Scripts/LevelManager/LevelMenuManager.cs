using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelMenuManager : MonoBehaviour
{
    [System.Serializable]
    public struct LevelButtonItem
    {
        public GameObject parentObject; // Butonun en dýþtaki kapsayýcýsý (Level objesi)
        public Image background;
        public TextMeshProUGUI text;
        public GameObject hardUI;
        public GameObject glowOutline;
        public GameObject bottomLine; // Altýndaki level'a giden yol
        public GameObject topLine;    // Üstündeki level'a giden yol
    }

    [Header("Panel Yönetimi")]
    public GameObject mainMenuPanel; // Menüdeki ana panel (Pattern, Butonlar vs.)
    public GameObject gameHUDPanel;   // Oyun içindeki UI (Skor, Kalan Mermi vs.)

    public LevelButtonItem[] menuItems;
    public List<LevelDataSO> allLevels;

    private int currentLevel;

    void Start ()
    {
        currentLevel = PlayerPrefs.GetInt("SavedLevel", 0);
        SetupMenu();
    }

    void SetupMenu ()
    {
        for (int i = 0; i < menuItems.Length; i++)
        {
            int targetLevelIndex = currentLevel + i;

            // Resetleme iþlemleri
            if (menuItems[i].parentObject != null)
            {
                menuItems[i].parentObject.transform.DOKill();
                menuItems[i].parentObject.transform.localScale = Vector3.one;
            }

            if (targetLevelIndex < allLevels.Count)
            {
                LevelDataSO data = allLevels[targetLevelIndex];

                // DATA ATAMALARI
                if (menuItems[i].text != null) menuItems[i].text.text = (targetLevelIndex + 1).ToString();
                if (menuItems[i].background != null && data.ButtonSprite != null)
                    menuItems[i].background.sprite = data.ButtonSprite;

                if (menuItems[i].hardUI != null) menuItems[i].hardUI.SetActive(data.IsHardLevel);

                bool isCurrentLevel = (i == 0); // Ekrandaki en alt buton (Aktif olan)
                bool isLastLevelInList = (targetLevelIndex == allLevels.Count - 1); // Oyunun son level'ý

                // YENÝ MANTIK: Ekrandaki her buton, bir üstteki kutucuk (menuItems[i+1]) 
                // aktif olmasa bile, o boþluða doðru topLine'ýný uzatmalý.
                // Sadece oyunun GERÇEKTEN son level'ý ise (targetLevelIndex listenin sonuysa) ucu kapanmalý.

                // 1. ÜST ÇUBUK (TopLine): 
                if (menuItems[i].topLine != null)
                {
                    // Eðer bu, oyunun gerçekten bittiði son level ise ucu kapanmalý.
                    // Deðilse, her zaman açýk kalsýn (gökyüzüne uzasýn).
                    menuItems[i].topLine.SetActive(!isLastLevelInList);
                }

                // 2. ALT ÇUBUK (BottomLine):
                if (menuItems[i].bottomLine != null)
                {
                    // KURAL: Current Level (index 0) ise alt çubuk her zaman KAPALI.
                    // Diðer tüm durumlarda açýk kalsýn (son level olsa bile öncekine baðlansýn).
                    menuItems[i].bottomLine.SetActive(!isCurrentLevel);
                }

                // EFEKTLER (Senin kodun devamý)
                if (i == 0) // Current Level Efektleri
                {
                    if (menuItems[i].glowOutline != null)
                    {
                        menuItems[i].glowOutline.SetActive(true);
                        Image gImg = menuItems[i].glowOutline.GetComponent<Image>();
                        gImg.DOKill();
                        gImg.DOFade(0.3f, 0.8f).SetLoops(-1, LoopType.Yoyo);
                    }

                    menuItems[i].parentObject.transform.DOScale(1.15f, 0.8f)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine);
                }
                else
                {
                    if (menuItems[i].glowOutline != null) menuItems[i].glowOutline.SetActive(false);
                }

                menuItems[i].parentObject.SetActive(true);
            }
            else
            {
                // Eðer o indexte level yoksa (liste dýþýysa) objeyi komple kapat
                if (menuItems[i].parentObject != null) menuItems[i].parentObject.SetActive(false);
            }
        }
    }

    // --- TEST ÝÇÝN LEVEL ARTTIRMA TUÞU ---
    [ContextMenu("Debug: Level Atla")] // Unity'de scripte sað týklayýp da basabilirsin
    public void Debug_LevelAtla ()
    {
        int nextLevel = PlayerPrefs.GetInt("SavedLevel", 0) + 1;
        PlayerPrefs.SetInt("SavedLevel", nextLevel);
        PlayerPrefs.Save();

        // Deðiþikliði anýnda görmek için menüyü tazele
        currentLevel = nextLevel;
        SetupMenu();

        Debug.Log("Test: Level " + (nextLevel + 1) + " yapýldý.");
    }
}