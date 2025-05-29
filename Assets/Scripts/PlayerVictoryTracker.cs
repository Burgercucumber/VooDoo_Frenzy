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
    private static VictoryDisplayManager displayManager;

    // Obtener referencia al VictoryDisplayManager
    private static VictoryDisplayManager GetDisplayManager()
    {
        if (displayManager == null)
        {
            displayManager = GameObject.FindObjectOfType<VictoryDisplayManager>();
            if (displayManager == null)
            {
                Debug.LogWarning("No se encontró VictoryDisplayManager en la escena");
            }
        }
        return displayManager;
    }

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

            // Mostrar insignia de victoria
            VictoryDisplayManager manager = GetDisplayManager();
            if (manager != null)
            {
                manager.ShowVictoryBadge(player, cardData.element, cardData.color);
            }

            Debug.Log($"Victoria añadida para {player.name}: {cardData.element} - {cardData.color}");

            // Verificar si el jugador ha ganado después de añadir la victoria
            if (HasPlayerWon(player))
            {
                Debug.Log($"¡{player.name} ha ganado el juego!");
                // Aquí puedes añadir la lógica para manejar la victoria del jugador
                OnPlayerWins(player);
            }
        }
    }

    // Nuevo método para manejar cuando un jugador gana
    private static void OnPlayerWins(PlayerManager player)
    {
        // Determinar el tipo de victoria
        var playerVictories = victories[player];
        string victoryType = "";

        if (CheckFourDifferentElements(playerVictories))
        {
            victoryType = "4 Elementos Diferentes";
        }
        else if (CheckColorVictory(playerVictories))
        {
            victoryType = "Victoria por Color (4+ del mismo elemento, máx 2 por color)";
        }

        Debug.Log($"Tipo de victoria de {player.name}: {victoryType}");

        // Aquí puedes añadir más lógica para mostrar la pantalla de victoria, etc.
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

        // Remover insignia correspondiente
        VictoryDisplayManager manager = GetDisplayManager();
        if (manager != null)
        {
            manager.RemovePlayerBadge(player, removedVictory.element, removedVictory.color);
        }

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

                // Remover insignia específica
                VictoryDisplayManager manager = GetDisplayManager();
                if (manager != null)
                {
                    manager.RemovePlayerBadge(player, element, color);
                }

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

        // Contar cuántas veces ya tiene esta combinación específica (elemento + color)
        int currentCount = 0;
        foreach (var v in victories[player])
        {
            if (v.element == newVictory.element && v.color == newVictory.color)
                currentCount++;
        }

        // Permitir hasta 2 victorias de la misma combinación elemento-color
        bool isValid = currentCount < 2;

        if (!isValid)
        {
            Debug.Log($"Victoria rechazada para {player.name}: Ya tiene {currentCount} victorias de {newVictory.element}-{newVictory.color} (máximo permitido: 2)");
        }
        else
        {
            Debug.Log($"Victoria aceptada para {player.name}: {newVictory.element}-{newVictory.color} (será la #{currentCount + 1})");
        }

        return isValid;
    }

    public static bool HasPlayerWon(PlayerManager player)
    {
        if (!victories.ContainsKey(player)) return false;

        var winList = victories[player];

        // Verificar victoria por 4 elementos diferentes
        if (CheckFourDifferentElements(winList))
        {
            Debug.Log($"{player.name} ganó por 4 elementos diferentes");
            return true;
        }

        // Verificar victoria por color (4+ del mismo elemento con máximo 2 por color)
        if (CheckColorVictory(winList))
        {
            Debug.Log($"{player.name} ganó por victoria de color");
            return true;
        }

        return false;
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

    // CORREGIDO: Método para verificar victoria por colores
    // Victoria por color = 4+ victorias del MISMO ELEMENTO con diferentes colores,
    // donde cada color no puede repetirse más de 2 veces
    private static bool CheckColorVictory(List<Victory> wins)
    {
        // Agrupar victorias por elemento
        var groupedByElement = new Dictionary<CardData.ElementType, List<CardData.ColorType>>();

        foreach (var victory in wins)
        {
            if (!groupedByElement.ContainsKey(victory.element))
                groupedByElement[victory.element] = new List<CardData.ColorType>();
            groupedByElement[victory.element].Add(victory.color);
        }

        // Verificar cada elemento para ver si cumple las condiciones de victoria por color
        foreach (var elementGroup in groupedByElement)
        {
            var element = elementGroup.Key;
            var colors = elementGroup.Value;

            // Debe tener al menos 4 victorias de este elemento
            if (colors.Count >= 4)
            {
                // Contar cuántas veces aparece cada color para este elemento
                var colorCount = new Dictionary<CardData.ColorType, int>();
                foreach (var color in colors)
                {
                    if (!colorCount.ContainsKey(color))
                        colorCount[color] = 0;
                    colorCount[color]++;
                }

                // Verificar que ningún color se repita más de 2 veces
                bool validColorDistribution = colorCount.Values.All(count => count <= 2);

                if (validColorDistribution)
                {
                    Debug.Log($"Victoria por color lograda con elemento {element}:");
                    foreach (var colorKvp in colorCount)
                    {
                        Debug.Log($"  - {colorKvp.Key}: {colorKvp.Value} veces");
                    }
                    return true;
                }
                else
                {
                    Debug.Log($"Elemento {element} tiene 4+ victorias pero distribución de colores inválida:");
                    foreach (var colorKvp in colorCount)
                    {
                        Debug.Log($"  - {colorKvp.Key}: {colorKvp.Value} veces {(colorKvp.Value > 2 ? "(EXCEDE LÍMITE)" : "")}");
                    }
                }
            }
        }

        return false;
    }

    // Método adicional para obtener detalles de la victoria por colores
    public static string GetColorVictoryDetails(PlayerManager player)
    {
        if (!victories.ContainsKey(player)) return "";

        var playerVictories = victories[player];
        var groupedByElement = new Dictionary<CardData.ElementType, Dictionary<CardData.ColorType, int>>();

        // Agrupar por elemento y contar colores
        foreach (var victory in playerVictories)
        {
            if (!groupedByElement.ContainsKey(victory.element))
                groupedByElement[victory.element] = new Dictionary<CardData.ColorType, int>();

            if (!groupedByElement[victory.element].ContainsKey(victory.color))
                groupedByElement[victory.element][victory.color] = 0;

            groupedByElement[victory.element][victory.color]++;
        }

        string details = "Distribución por elemento:\n";
        foreach (var elementGroup in groupedByElement)
        {
            details += $"{elementGroup.Key}: ";
            foreach (var colorCount in elementGroup.Value)
            {
                details += $"{colorCount.Key}({colorCount.Value}) ";
            }
            details += "\n";
        }

        return details;
    }

    public static void ResetVictories()
    {
        victories.Clear();

        // Resetear todas las insignias
        VictoryDisplayManager manager = GetDisplayManager();
        if (manager != null)
        {
            manager.ResetAllBadges();
        }

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