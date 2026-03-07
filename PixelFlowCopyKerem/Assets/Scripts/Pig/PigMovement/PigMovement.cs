using PathCreation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PigMovement : MonoBehaviour
{
    public PathCreator pathCreator;
    public float speed = 5f;

    [Header("Dinamik Dönüþ Ayarlarý (Hýza Orantýlý)")]
    [Tooltip("Viraja ne kadar erken gireceði. (Örn: 0.3 idealdir)")]
    public float lookAheadMultiplier = 0.3f;
    [Tooltip("Dönüþ keskinliði. (Örn: 3 idealdir)")]
    public float turnSpeedMultiplier = 3f;
    public float maxTurnSpeed = 12f;

    [Header("Dönüþ (Viraj) Ayarlarý")]
    public float lookAheadDistance = 1.5f; // Viraja ne kadar erken gireceði (%10'luk kýsým)
    public float turnSpeed = 15f;          // Dönüþ animasyonunun hýzý

    private float distanceTravelled;
    private bool isMoving = false;

    [Header("Týklama Ayarlarý")]
    private static float lastClickTime = 0f; // Bütün domuzlarýn ortak sayacý (Static olmasý þart!)
    public float clickCooldown = 0.4f;       // Aralarýndaki saniye farký

    [Header("Bekleme Odasý Durumu")]
    public bool isInWaitingRoom = false; // Þu an slotta mý yatýyor?
    private Transform targetSlot;        // Gideceði slotun koordinatý
    private float pathLength;            // Yolun toplam uzunluðu (1 Tur)

    void Start ()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.pathCreator != null)
        {
            pathLength = LevelManager.Instance.pathCreator.path.length;
        }
    }

    void Update ()
    {
        // --- 1. TIKLAMA (RAYCAST) SÝSTEMÝ ---
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && hit.transform == this.transform)
            {
                // A DURUMU: Ýlk defa kuyruktan yola çýkýyor!
                if (!isMoving && !isInWaitingRoom)
                {
                    if (Time.time - lastClickTime < clickCooldown) return;
                    if (DockManager.Instance != null && !DockManager.Instance.IsPigClickable(gameObject)) return;

                    lastClickTime = Time.time;
                    if (DockManager.Instance != null) DockManager.Instance.PigLeftDock(gameObject);

                    distanceTravelled = 0;
                    isMoving = true;
                    GetComponentInChildren<PigShooter>().isShooting = true;
                }
                // B DURUMU: Bekleme odasýndan (Slottan) TEKRAR yola çýkýyor!
                else if (!isMoving && isInWaitingRoom)
                {
                    // --- 1. YENÝ EKLENEN FREN ---
                    // Bekleme odasýndan art arda týklamayý da engelliyoruz!
                    if (Time.time - lastClickTime < clickCooldown) return;

                    // --- 2. YENÝ EKLENEN SAAT GÜNCELLEMESÝ ---
                    lastClickTime = Time.time;

                    DockManager.Instance.RemoveFromWaitingSlot(gameObject);
                    isInWaitingRoom = false;
                    distanceTravelled = 0; // Turu baþtan baþlat!
                    isMoving = true;
                    GetComponentInChildren<PigShooter>().isShooting = true; // Silahý tekrar aç
                }
            }
        }

        // --- 2. YÜRÜME VEYA SLOTA GÝTME SÝSTEMÝ ---
        if (isMoving && pathCreator != null)
        {
            // OYUN %95 BÝTTÝ MÝ KONTROLÜ
            bool isEndgame = false;
            if (LevelManager.Instance != null) isEndgame = LevelManager.Instance.IsEndgameActive();

            // Eðer Endgame evresindeysek hýzý 2.5 KATINA ÇIKAR!
            float activeSpeed = isEndgame ? speed * 2.5f : speed;

            distanceTravelled += activeSpeed * Time.deltaTime;

            // 1 TUR BÝTTÝ MÝ? (Yolun sonuna geldik mi?)
            if (distanceTravelled >= pathLength)
            {
                if (isEndgame)
                {
                    // ÞOV VAKTÝ: Eðer oyun bitmek üzereyse slota gitme, mesafeyi baþa sar ve ÇILGINLAR GÝBÝ DÖNMEYE DEVAM ET!
                    distanceTravelled -= pathLength; // Turu pürüzsüzce sýfýrlar
                }
                else
                {
                    // NORMAL DURUM: Tur bitti, slota git.
                    isMoving = false;
                    GetComponentInChildren<PigShooter>().isShooting = false;

                    targetSlot = DockManager.Instance.TryGetWaitingSlot(gameObject);

                    if (targetSlot != null) isInWaitingRoom = true;
                    else Debug.LogError("GAME OVER! Bekleme odasýnda yer kalmadý!");
                }
            }
            else
            {
                // Tur bitmediyse normal yürü...
                transform.position = pathCreator.path.GetPointAtDistance(distanceTravelled);

                // Domuz hýzlandýðýnda (Endgame) daha uzaða baksýn ki dönüþleri kaçýrmasýn!
                float dynamicLookAhead = activeSpeed * 0.1f; // Veya kendi lookAheadMultiplier deðerin

                // Ýlerideki hedef noktayý bul
                Vector3 pointAhead = pathCreator.path.GetPointAtDistance(distanceTravelled + dynamicLookAhead);
                Vector3 direction = pointAhead - transform.position;

                // O yöne doðru yumuþakça (Slerp) dön
                if (direction != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction);

                    // Dönüþ hýzýný da mevcut hýza (activeSpeed) baðladýk ki turbo modunda virajý alabilsinler!
                    float dynamicTurnSpeed = Mathf.Clamp(activeSpeed * 2f, 5f, 20f);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * dynamicTurnSpeed);
                }
            }
        }
        // Eðer turu bitirdiyse ve bir slota atandýysa, oraya doðru yumuþakça (Lerp) uç/kay!
        else if (isInWaitingRoom && targetSlot != null)
        {
            transform.position = Vector3.Lerp(transform.position, targetSlot.position, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetSlot.rotation, Time.deltaTime * 10f);
        }
    }
}