using UnityEngine;

public static class CardBattleLogic
{         
    public enum BattleResult
    {
        WinA,
        WinB,
        Draw
    }

    public static BattleResult CompareCards(CardData cardA, CardData cardB)
    {
        if (Beats(cardA.element, cardB.element))
            return BattleResult.WinA;
        else if (Beats(cardB.element, cardA.element))
            return BattleResult.WinB;
        else
        {
            if (cardA.starLevel > cardB.starLevel)
                return BattleResult.WinA;
            else if (cardB.starLevel > cardA.starLevel)
                return BattleResult.WinB;
            else
                return BattleResult.Draw;
        }
    }

    //Boton gana a alfiler, alfiler gana a tela, tela gana a algodon, algodon gana a alfiler

    private static bool Beats(CardData.ElementType a, CardData.ElementType b)
    {
        return (a == CardData.ElementType.Boton && b == CardData.ElementType.Alfiler) ||
               (a == CardData.ElementType.Alfiler && b == CardData.ElementType.Tela) ||
               (a == CardData.ElementType.Tela && b == CardData.ElementType.Algodon) ||
               (a == CardData.ElementType.Algodon && b == CardData.ElementType.Boton);
    }
}

