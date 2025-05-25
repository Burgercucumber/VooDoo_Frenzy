using UnityEngine;
using Mirror;

public class CardClickHandler : MonoBehaviour
{
    private void OnMouseDown()
    {
        NetworkIdentity netId = GetComponent<NetworkIdentity>();

        // Solo responder si el jugador es dueño de esta carta
        if (netId != null && netId.isOwned)
        {
            Debug.Log($"Carta {gameObject.name} clickeada para efecto auxiliar");

            // Si hay una carta auxiliar seleccionada, usar su efecto
            if (AuxiliaryCard.HasSelectedAuxiliary())
            {
                AuxiliaryCard.OnCardClicked(gameObject);
                // Detener la propagación del evento para que el GameManager no lo procese
                return;
            }
            else
            {
                Debug.Log("No hay carta auxiliar seleccionada");
            }
        }
        else
        {
            Debug.Log($"No tienes autoridad sobre la carta {gameObject.name}");
        }
    }

    // Método alternativo usando raycast si OnMouseDown no funciona bien
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CheckForClick();
        }
    }

    private void CheckForClick()
    {
        // Convertir posición del mouse a posición mundial
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        // Verificar si el click fue en este objeto
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null && collider.OverlapPoint(mousePos))
        {
            NetworkIdentity netId = GetComponent<NetworkIdentity>();

            if (netId != null && netId.isOwned)
            {
                Debug.Log($"Carta {gameObject.name} clickeada (raycast) para efecto auxiliar");

                if (AuxiliaryCard.HasSelectedAuxiliary())
                {
                    AuxiliaryCard.OnCardClicked(gameObject);
                }
                else
                {
                    Debug.Log("No hay carta auxiliar seleccionada");
                }
            }
        }
    }
}