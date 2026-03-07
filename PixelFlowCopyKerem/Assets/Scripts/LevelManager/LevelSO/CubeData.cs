using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] // Inspector'da görebilmemiz için gerekli
public class CubeData
{
    public Vector2Int GridPosition; // Izgaradaki konumu (Örn: 5, 2)
    public Color Color;             // Rengi
    public bool IsActive;           // Patladý mý, duruyor mu?
    public GameObject CubeRef;      // Sahnede baðlý olduðu gerçek küp objesi
    public bool isTargeted = false; 
}