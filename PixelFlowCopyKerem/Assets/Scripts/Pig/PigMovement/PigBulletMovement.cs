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
            Destroy(gameObject);
            return;
        }

        // Mermiyi DÜMDÜZ hedefin tam merkezine (adrese teslim) uçur
        Vector3 targetPos = targetCube.CubeRef.transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        // Mermi hedefin tam kalbine ulaþtý mý?
        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            GameObject hedefKup = targetCube.CubeRef;
            // 1. OYUNUN BEYNÝNDEN ANINDA SÝL (Arkadan gelen domuzlarýn önü hemen açýlsýn)
            LevelManager.Instance.GridMap[targetCube.GridPosition.x, targetCube.GridPosition.y] = null;
            LevelManager.Instance.destroyedCubes++;

            // 2. GÖRSEL ÞÖLEN (DOTween Pop Animasyonu)
            if (hedefKup != null)
            {
                // Önce %130 boyutuna þiþir (0.1 saniyede)
                hedefKup.transform.DOScale(Vector3.one * 1.3f, 0.1f).OnComplete(() =>
                {
                    // Sonra içine çökerek (InBack yaylanmasýyla) sýfýra küçül ve YUT! (0.15 saniyede)
                    hedefKup.transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).OnComplete(() =>
                    {
                        // Animasyon bitince objeyi sahneden tamamen temizle
                        Destroy(hedefKup);
                    });
                });
            }

            // 3. Görev tamam, mermiyi yok et
            Destroy(gameObject);
        }
    }
}