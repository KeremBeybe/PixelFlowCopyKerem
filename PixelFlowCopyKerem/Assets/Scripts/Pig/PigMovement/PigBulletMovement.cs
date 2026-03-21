using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PigBulletMovement : MonoBehaviour
{
    public float speed = 15f;
    [HideInInspector] public CubeData targetCube; // Domuzun mermiye verdiði KESÝN hedef!

    void Update ()
    {
        // Eðer hedefimiz yoksa veya biz yoldayken baþka bir mucizeyle yok olduysa mermiyi iptal et.
        if (targetCube == null || targetCube.CubeRef == null)
        {
            LevelManager.Instance.ReturnBulletToPool(gameObject);
            return;
        }

        // Mermiyi DÜMDÜZ hedefin tam merkezine (adrese teslim) uçur
        Vector3 targetPos = targetCube.CubeRef.transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // Mermi hedefin tam kalbine ulaþtý mý?
        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySeries(0);
            GameObject hedefKup = targetCube.CubeRef;
            // 1. OYUNUN BEYNÝNDEN ANINDA SÝL (Arkadan gelen domuzlarýn önü hemen açýlsýn)
            LevelManager.Instance.GridMap[targetCube.GridPosition.x, targetCube.GridPosition.y] = null;
            LevelManager.Instance.destroyedCubes++;
            // YENÝ EKLENEN: Skoru artýrdýn, peki oyun bitti mi diye sor!
            LevelManager.Instance.CheckWinCondition();

            // 2. GÖRSEL ÞÖLEN (DOTween Pop Animasyonu)
            if (hedefKup != null)
            {
                // Önce %130 boyutuna þiþir (0.1 saniyede)
                hedefKup.transform.DOScale(Vector3.one * 1.3f, 0.1f).OnComplete(() =>
                {
                    hedefKup.transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).OnComplete(() =>
                    {
                        // KÜPÜ TAMAMEN SÝLME! Obje havuzu (Pool) kullandýðýmýz için uykuya al.
                        // Boyutunu da tekrar normale (1,1,1) döndür ki havuzdan bir daha çýkarken görünmez olmasýn!
                        hedefKup.SetActive(false);
                        hedefKup.transform.localScale = Vector3.one;
                    });
                });
            }

            LevelManager.Instance.ReturnBulletToPool(gameObject);
        }
    }
}