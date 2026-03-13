using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform objetoSeguir;

    [Header("Offset")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Zona Muerta")]
    [SerializeField] private bool usarZonaMuerta = true;
    [SerializeField] private float margenX = 2f;
    [SerializeField] private float margenY = 1f;

    [Header("Limites de Camara")]
    [SerializeField] private bool usarLimites = true;
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 10f;
    [SerializeField] private float minY = -5f;
    [SerializeField] private float maxY = 5f;

    void LateUpdate()
    {
        if (objetoSeguir == null)
        {
            return;
        }

        Vector3 posicionObjetivo = objetoSeguir.position + offset;
        Vector3 posicionCamara = transform.position;

        if (usarZonaMuerta)
        {
            float margenHorizontal = Mathf.Max(0f, margenX);
            float margenVertical = Mathf.Max(0f, margenY);

            float diferenciaX = posicionObjetivo.x - posicionCamara.x;
            if (Mathf.Abs(diferenciaX) > margenHorizontal)
            {
                posicionCamara.x = posicionObjetivo.x - Mathf.Sign(diferenciaX) * margenHorizontal;
            }

            float diferenciaY = posicionObjetivo.y - posicionCamara.y;
            if (Mathf.Abs(diferenciaY) > margenVertical)
            {
                posicionCamara.y = posicionObjetivo.y - Mathf.Sign(diferenciaY) * margenVertical;
            }
        }
        else
        {
            posicionCamara.x = posicionObjetivo.x;
            posicionCamara.y = posicionObjetivo.y;
        }

        if (usarLimites)
        {
            posicionCamara.x = Mathf.Clamp(posicionCamara.x, minX, maxX);
            posicionCamara.y = Mathf.Clamp(posicionCamara.y, minY, maxY);
        }

        posicionCamara.z = posicionObjetivo.z;
        transform.position = posicionCamara;
    }
}