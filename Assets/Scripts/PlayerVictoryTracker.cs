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
            Debug.Log($"Victoria añadida para {player.name}: {cardData.element} - {cardData.color}");
        }
    }

    // Nuevo método para remover una victoria aleatoria
    public static bool RemoveRandomVictory(PlayerManager player)
    {
        if (!victories.ContainsKey(player) || victories[player].Count == 0)
        {
            Debug.Log($"No hay victorias que remover para {player.name}");
            return false;
        }

        List<Victory> playerVictories = victories[player];
        int randomIndex = Random.Range(0, playerVictories.Count);

        Victory removedVictory = playerVictories[randomIndex];
        playerVictories.RemoveAt(randomIndex);

        Debug.Log($"Victoria removida de {player.name}: {removedVictory.element} - {removedVictory.color}");

        // Actualizar UI de victorias
        UpdateVictoryUI(player);

        return true;
    }

    // Método para remover una victoria específica
    public static bool RemoveSpecificVictory(PlayerManager player, CardData.ElementType element, CardData.ColorType color)
    {
        if (!victories.ContainsKey(player))
            return false;

        Victory targetVictory = new Victory(element, color);
        List<Victory> playerVictories = victories[player];

        for (int i = 0; i < playerVictories.Count; i++)
        {
            if (playerVictories[i].element == targetVictory.element &&
                playerVictories[i].color == targetVictory.color)
            {
                playerVictories.RemoveAt(i);
                Debug.Log($"Victoria específica removida de {player.name}: {element} - {color}");
                UpdateVictoryUI(player);
                return true;
            }
        }

        return false;
    }

    // Método para obtener el número de victorias de un jugador
    public static int GetVictoryCount(PlayerManager player)
    {
        if (!victories.ContainsKey(player))
            return 0;

        return victories[player].Count;
    }

    // Método para obtener todas las victorias de un jugador
    public static List<(CardData.ElementType element, CardData.ColorType color)> GetPlayerVictories(PlayerManager player)
    {
        List<(CardData.ElementType, CardData.ColorType)> result = new List<(CardData.ElementType, CardData.ColorType)>();

        if (victories.ContainsKey(player))
        {
            foreach (Victory victory in victories[player])
            {
                result.Add((victory.element, victory.color));
            }
        }

        return result;
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
        Debug.Log("Todas las victorias han sido reseteadas");
    }

    private static void ShowVictoryUI(CardData card)
    {
        // Llama a la clase encargada de UI para mostrar nueva victoria
        // Ejemplo: VictoryUIManager.Instance.CreateVictorySquare(card.element, card.color);
    }

    private static void UpdateVictoryUI(PlayerManager player)
    {
        // Actualiza la UI de victorias después de remover una victoria
        // Ejemplo: VictoryUIManager.Instance.UpdatePlayerVictories(player, GetPlayerVictories(player));
    }
}