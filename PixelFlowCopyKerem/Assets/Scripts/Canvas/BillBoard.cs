using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillBoard : MonoBehaviour
{
    private Quaternion sabitAci;
    private Vector3 sabitFark;
    private Transform domuz;

    void Awake ()
    {
        domuz = transform.parent;

        // --- ÝÞTE BÜTÜN SIR BURADA ---
        // Domuz o an dünyada nereye bakýyor olursa olsun umrumuzda deðil!
        // Biz doðrudan senin Inspector'da elinle yazdýðýn o kusursuz "Local" ayarlarý alýyoruz.
        sabitAci = transform.localRotation;
        sabitFark = transform.localPosition;
    }

    void LateUpdate ()
    {
        if (domuz != null)
        {
            // Pozisyon: Domuz dünyada nerede olursa olsun, yazýyý domuzun tam merkezinden senin ayarladýðýn o "Havada durma" mesafesi kadar uzaða koy.
            transform.position = domuz.position + sabitFark;

            // Rotasyon: Domuz 50 takla da atsa, rotasyonu her zaman senin Inspector'daki o güzel açýna zorla!
            transform.rotation = sabitAci;
        }
    }
}