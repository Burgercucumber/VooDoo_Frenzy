using UnityEngine;
using UnityEngine.EventSystems;

public class VictoryBadge : MonoBehaviour, IPointerDownHandler
{
    [Header("Configuración de la Insignia")]
    public CardData.ElementType element;
    public CardData.ColorType color;

    [Header("Información de la Insignia")]
    public string badgeName = "Insignia de Victoria";

    // Implementar IPointerDownHandler para detectar clics
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"Click detectado en insignia: {element} {color}");

        // Verificar si hay una carta auxiliar RemoveVictory seleccionada
        if (AuxiliaryCard.HasSelectedAuxiliary())
        {
            Debug.Log($"Enviando eliminación de insignia específica: {element} {color}");
            AuxiliaryCard.OnVictoryBadgeClicked(element, color);
        }
        else
        {
            Debug.Log("No hay carta auxiliar seleccionada. Selecciona primero una carta RemoveVictory.");
        }
    }

    // Método para verificar si esta insignia corresponde a una victoria específica
    public bool MatchesVictory(CardData.ElementType victoryElement, CardData.ColorType victoryColor)
    {
        return element == victoryElement && color == victoryColor;
    }

    // Método para configurar la insignia programáticamente si es necesario
    public void SetBadgeType(CardData.ElementType newElement, CardData.ColorType newColor)
    {
        element = newElement;
        color = newColor;
        badgeName = $"Insignia {newElement} {newColor}";
    }
}