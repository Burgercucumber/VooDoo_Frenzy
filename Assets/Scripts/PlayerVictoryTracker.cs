using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public static class PlayerVictoryTracker
{
    private struct Victory
    {
        public CardData.ElementType element;
        public CardData.ColorType color;

        public Victory(CardData.ElementType element, CardData.ColorType color)
        {
            this.element = element;
            this.color = color;
        }
    }

    private static Dictionary<PlayerManager, List<Victory>> victories = new Dictionary<PlayerManager, List<Victory>>();

    public static void AddVictory(PlayerManager player)
    {
        CardData cardData = GetCardDataFromPlayedCard(player);
        if (cardData == null) return;

        if (!victories.ContainsKey(player))
            victories[player] = new List<Victory>();

        Victory newVictory = new Victory(cardData.element, cardData.color);

        if (IsVictoryValid(player, newVictory))
        {
            victories[player].Add(newVictory);
            ShowVictoryUI(cardData);
        }
    }

    private static CardData GetCardDataFromPlayedCard(PlayerManager player)
    {
        uint netId = player.GetCurrentPlayedCardNetId();
        if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity identity))
        {
            return identity.GetComponent<CardData>();
        }
        return null;
    }

    private static bool IsVictoryValid(PlayerManager player, Victory newVictory)
    {
        if (!victories.ContainsKey(player))
            return true;

        foreach (var v in victories[player])
        {
            if (v.element == newVictory.element && v.color == newVictory.color)
                return false;
        }

        return true;
    }

    public static bool HasPlayerWon(PlayerManager player)
    {
        if (!victories.ContainsKey(player)) return false;

        var winList = victories[player];
        return CheckFourDifferentElements(winList) || CheckFourSameElementWithColorLimit(winList);
    }

    private static bool CheckFourDifferentElements(List<Victory> wins)
    {
        HashSet<CardData.ElementType> elements = new HashSet<CardData.ElementType>();
        foreach (var v in wins)
            elements.Add(v.element);

        return elements.Count >= 4;
    }

    private static bool CheckFourSameElementWithColorLimit(List<Victory> wins)
    {
        var grouped = new Dictionary<CardData.ElementType, List<CardData.ColorType>>();

        foreach (var v in wins)
        {
            if (!grouped.ContainsKey(v.element))
                grouped[v.element] = new List<CardData.ColorType>();

            grouped[v.element].Add(v.color);
        }

        foreach (var pair in grouped)
        {
            if (pair.Value.Count >= 4)
            {
                var colorCount = new Dictionary<CardData.ColorType, int>();
                foreach (var c in pair.Value)
                {
                    if (!colorCount.ContainsKey(c))
                        colorCount[c] = 0;
                    colorCount[c]++;
                }

                if (colorCount.Values.All(count => count <= 2))
                    return true;
            }
        }

        return false;
    }

    public static void ResetVictories()
    {
        victories.Clear();
    }

    private static void ShowVictoryUI(CardData card)
    {
        // Llama a la clase encargada de UI
        // Ejemplo: VictoryUIManager.Instance.CreateVictorySquare(card.element, card.color);
    }
}
