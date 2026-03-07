using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro; 
using UnityEngine;
using UnityEngine.UI;

public class PigShooter : MonoBehaviour
{
    [Header("Ateþ Ayarlarý")]
    public GameObject bulletPrefab;
    public Transform shootPoint;
    [HideInInspector] public bool isShooting = false;

    [Header("UI (Arayüz) Ayarlarý")]
    public TextMeshProUGUI ammoText;

    [Header("Otomatik Veriler")]
    [HideInInspector] public Color myAssignedColor;
    [HideInInspector] public int bulletCount;

    private float lastFireTime = 0f;
    private float fireCooldown = 0.05f; // Radarla yarým saniyede bir tarama yap

    public void SetPigData (Color exactColor, int count)
    {
        myAssignedColor = exactColor;
        bulletCount = count;

        if (ammoText != null) ammoText.text = bulletCount.ToString();

        // RENK SORUNU KESÝN ÇÖZÜM (URP Desteði)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            // Yazý veya arayüz objelerini boyamamak için kontrol
            if (rend.GetComponent<Text>() == null)
            {
                rend.material.color = exactColor;
                rend.material.SetColor("_BaseColor", exactColor); // URP kullanan projeler için asýl boyayan kod!
            }
        }
    }

    void Update ()
    {
        if (isShooting && LevelManager.Instance != null)
        {
            GarantiliAtesSistemi();
        }
    }

    private void GarantiliAtesSistemi ()
    {
        Vector3 forward = shootPoint.forward;
        bool isLookingStraight = (Mathf.Abs(forward.x) > 0.95f && Mathf.Abs(forward.z) < 0.05f) ||
                                 (Mathf.Abs(forward.z) > 0.95f && Mathf.Abs(forward.x) < 0.05f);

        if (!isLookingStraight) return;

        float cellSize = LevelManager.Instance.CellSize;
        float posX = transform.position.x;
        float posZ = transform.position.z;

        int currentX = Mathf.RoundToInt(posX / cellSize);
        int currentZ = Mathf.RoundToInt(posZ / cellSize);

        int dirX = Mathf.RoundToInt(forward.x);
        int dirZ = Mathf.RoundToInt(forward.z);

        bool tamHizada = false;
        if (Mathf.Abs(dirZ) > 0.5f && Mathf.Abs(posX - (currentX * cellSize)) < 0.15f) tamHizada = true;
        else if (Mathf.Abs(dirX) > 0.5f && Mathf.Abs(posZ - (currentZ * cellSize)) < 0.15f) tamHizada = true;

        if (tamHizada && Time.time - lastFireTime > fireCooldown)
        {
            // AKILLI RADAR: Kilitlenecek uygun hedef var mý?
            CubeData target = FindTargetInLane(currentX, currentZ, dirX, dirZ);
            if (target != null)
            {
                target.isTargeted = true; // HEDEFE KÝLÝTLEN! Artýk baþka hiçbir domuz buna sýkamaz!
                Shoot(target);
                lastFireTime = Time.time;
            }
        }
    }

    // YENÝ NESÝL AKILLI RADAR
    private CubeData FindTargetInLane (int startX, int startZ, int dirX, int dirZ)
    {
        if (bulletCount <= 0) return null;

        int checkX = startX;
        int checkZ = startZ;

        for (int i = 0; i < 20; i++)
        {
            checkX += dirX;
            checkZ += dirZ;

            if (checkX >= 0 && checkX < LevelManager.Instance.mapWidth && checkZ >= 0 && checkZ < LevelManager.Instance.mapHeight)
            {
                CubeData targetData = LevelManager.Instance.GridMap[checkX, checkZ];

                if (targetData != null && targetData.CubeRef != null)
                {
                    // Eðer bu küpe BAÞKA BÝR DOMUZ kilitlenmiþse, bizim için hala aþýlamaz bir duvardýr! Bekle!
                    if (targetData.isTargeted) return null;

                    // Unity'nin küsuratlý renk tuzaðýna düþmemek için Hex (Metin) kodlarýný karþýlaþtýrýyoruz!
                    string targetHex = ColorUtility.ToHtmlStringRGB(targetData.Color);
                    string myHex = ColorUtility.ToHtmlStringRGB(myAssignedColor);

                    if (targetHex == myHex)
                    {
                        return targetData;
                    }

                    // Rengi farklýysa arkasýndakini vuramayýz. Duvar.
                    return null;
                }
            }
        }
        return null;
    }

    // MERMÝYÝ ATEÞLE VE HEDEFÝ GÖSTER
    private void Shoot (CubeData target)
    {
        if (bulletPrefab != null && shootPoint != null && bulletCount > 0)
        {
            GameObject newBullet = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);

            // Mermiye gidip kilitlendiðimiz o küpü vurmasýný emrediyoruz!
            PigBulletMovement bulletScript = newBullet.GetComponent<PigBulletMovement>();
            if (bulletScript != null) bulletScript.targetCube = target;

            // Mermiyi de domuzun rengine boya
            Renderer bulletRend = newBullet.GetComponentInChildren<Renderer>();
            if (bulletRend != null)
            {
                bulletRend.material.color = myAssignedColor;
                bulletRend.material.SetColor("_BaseColor", myAssignedColor);
            }

            bulletCount--;
            if (ammoText != null) ammoText.text = bulletCount.ToString();
            if (bulletCount <= 0)
            {
                isShooting = false;
                SevinerekYokOl(); // ÞOV BAÞLASIN!
            }
        }
    }

    private void SevinerekYokOl ()
    {
        // 1. Yazýyý (UI) hemen gizle ki dönerken çirkin durmasýn
        if (ammoText != null) ammoText.gameObject.SetActive(false);

        // 2. Zýplama Efekti (DOJump: Havaya 1 birim zýpla, 1 kere yap, 0.5 saniye sürsün)
        transform.DOJump(transform.position, 1f, 1, 0.5f);

        // 3. Fýrýldak gibi kendi etrafýnda 360 derece dön (SetRelative ile olduðu yere ekstra 360 ekliyoruz)
        transform.DORotate(new Vector3(0, 360, 0), 0.5f, RotateMode.FastBeyond360).SetRelative(true).SetEase(Ease.OutQuad);

        // 4. Havada dönerken ayný anda küçülerek yok ol! (Ease.InBack ile tatlý bir esneme)
        transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
        {
            Destroy(transform.parent != null ? transform.parent.gameObject : gameObject);
            // (Domuzun ana objesini siliyoruz ki sahnede çöp kalmasýn)
        });
    }
}