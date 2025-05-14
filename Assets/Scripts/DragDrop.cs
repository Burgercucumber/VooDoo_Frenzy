using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DragDrop : NetworkBehaviour
{
    public GameObject Canvas;

    // Asignado desde PlayerManager al instanciar la carta
    public PlayerManager OwnerPlayerManager;

    private PlayerManager PlayerManager;
    private bool isDragging = false;
    private bool isDraggable = true;

    private GameObject startParent;
    private Vector2 startPosition;
    private GameObject dropZone;
    private bool isOverDropZone;

    void Start()
    {
        Canvas = GameObject.Find("Canvas");

        // Solo puedes arrastrar si eres el dueño de esta carta
        if (!isOwned)
        {
            isDraggable = false;
        }

        // Asigna el PlayerManager si fue inyectado desde el server
        if (OwnerPlayerManager != null)
        {
            PlayerManager = OwnerPlayerManager;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        isOverDropZone = true;
        dropZone = collision.gameObject;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        isOverDropZone = false;
        dropZone = null;
    }

    public void StartDrag()
    {
        if (!isDraggable) return;
        isDragging = true;
        startParent = transform.parent.gameObject;
        startPosition = transform.position;
    }

    public void EndDrag()
    {
        if (!isDraggable) return;

        // Asignar PlayerManager correctamente si aún no está
        if (PlayerManager == null)
        {
            foreach (var pm in Object.FindObjectsByType<PlayerManager>(FindObjectsSortMode.None))
            {
                if (pm.isOwned)
                {
                    PlayerManager = pm;
                    break;
                }
            }

            if (PlayerManager == null)
            {
                Debug.LogWarning("Player manager no asignado");
                transform.position = startPosition;
                transform.SetParent(startParent.transform, false);
                return;
            }
        }

        isDragging = false;

        if (PlayerManager.HasPlayedCard)
        {
            Debug.Log("Ya jugaste una carta, espera para jugar otra.");
            transform.position = startPosition;
            transform.SetParent(startParent.transform, false);
            return;
        }

        if (isOverDropZone)
        {
            transform.SetParent(dropZone.transform, false);
            isDraggable = false;
            PlayerManager.PlayCard(gameObject);
        }
        else
        {
            transform.position = startPosition;
            transform.SetParent(startParent.transform, false);
        }
    }

    private void ResetCardPosition()
    {
        transform.position = startPosition;
        transform.SetParent(startParent.transform, false);
    }

    void Update()
    {
        if (isDragging)
        {
            transform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            transform.SetParent(Canvas.transform, true);
        }
    }
}
