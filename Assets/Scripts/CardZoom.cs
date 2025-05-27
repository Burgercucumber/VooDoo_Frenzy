using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class CardZoom : NetworkBehaviour
{
    public GameObject Canvas;
    public GameObject ZoomCard;
    private GameObject zoomCard;
    private Sprite zoomSprite;

    // Lista estática para trackear todas las instancias de zoom activas
    private static List<GameObject> activeZoomCards = new List<GameObject>();

    public void Awake()
    {
        Canvas = GameObject.Find("Canvas");
        zoomSprite = gameObject.GetComponent<Image>().sprite;
    }

    public void OnHoverEnter()
    {
        if (!isOwned) return;

        zoomCard = Instantiate(ZoomCard, new Vector2(Input.mousePosition.x, Input.mousePosition.y + 250), Quaternion.identity);
        zoomCard.GetComponent<Image>().sprite = zoomSprite;
        zoomCard.transform.SetParent(Canvas.transform, true);
        RectTransform rect = zoomCard.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(240, 344);

        // Agregar a la lista de zooms activos
        activeZoomCards.Add(zoomCard);
    }

    public void OnHoverExit()
    {
        if (zoomCard != null)
        {
            // Remover de la lista antes de destruir
            activeZoomCards.Remove(zoomCard);
            Destroy(zoomCard);
            zoomCard = null;
        }
    }

    // Método para limpiar el zoom cuando la carta se destruye
    private void OnDestroy()
    {
        CleanupZoom();
    }

    private void CleanupZoom()
    {
        if (zoomCard != null)
        {
            activeZoomCards.Remove(zoomCard);
            Destroy(zoomCard);
            zoomCard = null;
        }
    }

    // Método estático para limpiar todos los zooms huérfanos
    public static void ClearAllOrphanedZooms()
    {
        // Remover zooms que ya no existen
        activeZoomCards.RemoveAll(zoom => zoom == null);

        // Destruir todos los zooms restantes
        foreach (GameObject zoom in activeZoomCards)
        {
            if (zoom != null)
            {
                Destroy(zoom);
            }
        }
        activeZoomCards.Clear();
    }
}