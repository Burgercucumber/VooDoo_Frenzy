using UnityEngine;
using UnityEngine.EventSystems;

public class VictoryBadge : MonoBehaviour, IPointerDownHandler
{
    [Header("Configuraci�n de la Insignia")]
    public CardData.ElementType element;
    public CardData.ColorType color;

    [Header("Informaci�n de la Insignia")]
    public string badgeName = "Insignia de Victoria";

    // Implementar IPointerDownHandler para detectar clics
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"Click detectado en insignia: {element} {color}");

        // Verificar si hay una carta auxiliar RemoveVictory seleccionada
        if (AuxiliaryCard.HasSelectedAuxiliary())
        {
            Debug.Log($"Enviando eliminaci�n de insignia espec�fica: {element} {color}");
            AuxiliaryCard.OnVictoryBadgeClicked(element, color);
        }
        else
        {
            Debug.Log("No hay carta auxiliar seleccionada. Selecciona primero una carta RemoveVictory.");
        }
    }

    // M�todo para verificar si esta insignia corresponde a una victoria espec�fica
    public bool MatchesVictory(CardData.ElementType victoryElement, CardData.ColorType victoryColor)
    {
        return element == victoryElement && color == victoryColor;
    }

    // M�todo para configurar la insignia program�ticamente si es necesario
    public void SetBadgeType(CardData.ElementType newElement, CardData.ColorType newColor)
    {
        element = newElement;
        color = newColor;
        badgeName = $"Insignia {newElement} {newColor}";
    }
}