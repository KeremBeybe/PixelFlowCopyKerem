using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DockManager : MonoBehaviour
{
    public static DockManager Instance;

    [Header("Ayarlar")]
    public GameObject pigPrefab;

    [Tooltip("Her sütunun en önündeki (baþlangýç) noktalarý")]
    public Transform[] columnStarts;

    [Tooltip("Ayný sütundaki domuzlarýn aralarýndaki mesafe")]
    public float pigSpacing = 1.5f;

    // Her sütunun kendi domuz listesini tutan BÜYÜK LÝSTE
    public List<List<GameObject>> columns = new List<List<GameObject>>();

    [Header("Bekleme Odasý (Slot) Ayarlarý")]
    public Transform[] waitingSlots; // Sahnede alt tarafa dizeceðimiz boþ slot noktalarý
    [HideInInspector] public GameObject[] currentWaitingPigs; // Hangi slotta hangi domuz yatýyor?
    private int activeMaxSlots;

    void Awake ()
    {
        Instance = this;
    }

    void Update ()
    {
        // Tüm sütunlarý gez ve domuzlarý pürüzsüzce sýraya diz
        for (int col = 0; col < columns.Count; col++)
        {
            for (int i = 0; i < columns[col].Count; i++)
            {
                GameObject pig = columns[col][i];
                if (pig != null)
                {
                    // Domuzun Hedef Yeri = Sütun baþý pozisyonu - (Geriye doðru mesafe * sýra numarasý)
                    Vector3 targetPos = columnStarts[col].position + (Vector3.back * (i * pigSpacing));

                    pig.transform.position = Vector3.Lerp(pig.transform.position, targetPos, Time.deltaTime * 10f);
                    pig.transform.rotation = Quaternion.Lerp(pig.transform.rotation, columnStarts[col].rotation, Time.deltaTime * 10f);
                }
            }
        }
    }

    // Artýk özel listemizi (List<PigSpawnData>) parametre olarak alýyor
    public void SpawnPigsForLevel (List<PigSpawnData> customPigQueue)
    {
        // Bölüm baþlarken slot hafýzasýný sýfýrla
        if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevel != null)
        {
            activeMaxSlots = LevelManager.Instance.CurrentLevel.maxWaitingSlots;
        }
        else activeMaxSlots = 5; // Güvenlik

        currentWaitingPigs = new GameObject[waitingSlots.Length];
        // 1. Eskileri temizle ve sütun listelerini sýfýrla
        foreach (var col in columns)
            foreach (var p in col)
                if (p != null) Destroy(p);

        columns.Clear();
        for (int i = 0; i < columnStarts.Length; i++)
        {
            columns.Add(new List<GameObject>());
        }

        // 2. SO'daki özel listeni oku ve domuzlarý senin istediðin gibi yarat!
        foreach (PigSpawnData data in customPigQueue)
        {
            // Hatalý bir sütun numarasý girilirse oyun çökmesin diye güvenlik
            int safeColumn = Mathf.Clamp(data.columnIndex, 0, columnStarts.Length - 1);

            GameObject yeniDomuz = Instantiate(pigPrefab, transform.position, Quaternion.identity);

            PigShooter shooter = yeniDomuz.GetComponentInChildren<PigShooter>();
            if (shooter != null) shooter.SetPigData(data.pigColor, data.ammoCount);

            PigMovement movement = yeniDomuz.GetComponent<PigMovement>();
            if (movement != null && LevelManager.Instance != null)
                movement.pathCreator = LevelManager.Instance.pathCreator;

            // Domuzu tam olarak senin istediðin o sütuna koy!
            columns[safeColumn].Add(yeniDomuz);
        }
    }

    // Domuz týklanýp yola çýkýnca onu kuyruktan SÝL! (Arkadakiler otomatik öne kayacak)
    public void PigLeftDock (GameObject pig)
    {
        foreach (var col in columns)
        {
            if (col.Contains(pig))
            {
                col.Remove(pig);
                break;
            }
        }
    }

    // BU ÇOK ÖNEMLÝ: Domuz kendi sütununun EN BAÞINDA MI (0. indeks)?
    public bool IsPigClickable (GameObject pig)
    {
        foreach (var col in columns)
        {
            if (col.Count > 0 && col[0] == pig) return true;
        }
        return false;
    }

    // Domuz 1 turu bitirdiðinde ona boþ bir yatak (Slot) bul
    public Transform TryGetWaitingSlot (GameObject pig)
    {
        for (int i = 0; i < activeMaxSlots; i++)
        {
            if (currentWaitingPigs[i] == null) // Boþ yer bulundu!
            {
                currentWaitingPigs[i] = pig;
                return waitingSlots[i]; // Slotun pozisyonunu gönder ki domuz oraya gitsin
            }
        }
        return null; // YER YOK! (Oyuncu birazdan Game Over olacak)
    }

    // Domuza tekrar týklandýðýnda onu yataktan (slottan) çýkar
    public void RemoveFromWaitingSlot (GameObject pig)
    {
        for (int i = 0; i < currentWaitingPigs.Length; i++)
        {
            if (currentWaitingPigs[i] == pig)
            {
                currentWaitingPigs[i] = null;
                break;
            }
        }
    }
}