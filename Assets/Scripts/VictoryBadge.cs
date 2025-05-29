using UnityEngine;

public class VictoryBadge : MonoBehaviour
{
    [Header("Configuración de la Insignia")]
    public CardData.ElementType element;
    public CardData.ColorType color;

    [Header("Información de la Insignia")]
    public string badgeName = "Insignia de Victoria";

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