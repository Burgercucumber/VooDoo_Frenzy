using UnityEngine;

public static class CardBattleLogic
{
    public static int CompareCards(CardData cardA, CardData cardB)
    {
        // Primero se compara por tipo elemental
        if (Beats(cardA.element, cardB.element))
            return 1; // A gana
        else if (Beats(cardB.element, cardA.element))
            return -1; // B gana
        else
        {
            // Si no hay ventaja elemental, se compara por estrellas
            if (cardA.starLevel > cardB.starLevel)
                return 1;
            else if (cardB.starLevel > cardA.starLevel)
                return -1;
            else
                return 0; // Empate
        }
    }

    private static bool Beats(CardData.ElementType a, CardData.ElementType b)
    {
        return (a == CardData.ElementType.Boton && b == CardData.ElementType.Alfiler) ||
               (a == CardData.ElementType.Alfiler && b == CardData.ElementType.Tela) ||
               (a == CardData.ElementType.Tela && b == CardData.ElementType.Algodon) ||
               (a == CardData.ElementType.Algodon && b == CardData.ElementType.Boton);
    }
}

