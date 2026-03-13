using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    // La cámara debe seguir al personaje
    [SerializeField] GameObject objetoSeguir;
    void LateUpdate()
    {
        transform.position = objetoSeguir.transform.position + new Vector3 (0, 0, -10);
    }
}