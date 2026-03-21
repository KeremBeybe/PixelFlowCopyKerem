using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillBoard : MonoBehaviour
{
    private Quaternion angle;
    private Vector3 distance;
    private Transform domuz;

    void Awake ()
    {
        domuz = transform.parent;
        angle = transform.localRotation;
        distance = transform.localPosition;
    }

    void LateUpdate ()
    {
        if (domuz != null)
        {
            // Pozisyon: Domuz dünyada nerede olursa olsun, yazýyý domuzun tam merkezinden senin ayarladýðýn o "Havada durma" mesafesi kadar uzaða koy.
            transform.position = domuz.position + distance;

            // Rotasyon: Domuz 50 takla da atsa, rotasyonu her zaman senin Inspector'daki o güzel açýna zorla!
            transform.rotation = angle;
        }
    }
}