using UnityEngine;

public class VictoryBadge : MonoBehaviour
{
    [Header("Configuraci�n de la Insignia")]
    public CardData.ElementType element;
    public CardData.ColorType color;

    [Header("Informaci�n de la Insignia")]
    public string badgeName = "Insignia de Victoria";

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