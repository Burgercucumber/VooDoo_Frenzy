using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;
using System.Collections.Generic;

public class AuxiliaryCard : NetworkBehaviour, IPointerDownHandler
{
    public enum AuxiliaryType
    {
        LevelUp,
        ElementChange,
        RemoveVictory
    }

    [Header("Configuración de Carta Auxiliar")]
    public AuxiliaryType auxiliaryType;
    public string cardName;
    [TextArea]
    public string description;

    private static AuxiliaryCard selectedAuxiliary = null;
    private bool isSelected = false;

    // Implementar IPointerDownHandler en lugar de OnMouseDown
    public void OnPointerDown(PointerEventData eventData)
    {
        // Solo el dueño puede usar sus cartas auxiliares
        if (!isOwned)
        {
            Debug.Log("No eres el dueño de esta carta auxiliar");
            return;
        }

        Debug.Log($"Carta auxiliar {auxiliaryType} clickeada");

        // Si ya hay una auxiliar seleccionada, deseleccionarla
        if (selectedAuxiliary != null && selectedAuxiliary != this)
        {
            selectedAuxiliary.Deselect();
        }

        // Seleccionar esta carta auxiliar
        SelectAuxiliary();
    }

    private void SelectAuxiliary()
    {
        selectedAuxiliary = this;
        isSelected = true;

        // Efecto visual de selección - solo cambiar escala
        transform.localScale = Vector3.one * 1.1f;

        if (auxiliaryType == AuxiliaryType.RemoveVictory)
        {
            Debug.Log("Carta RemoveVictory seleccionada. Haz clic en una insignia de victoria del oponente para eliminarla.");
        }
        else
        {
            Debug.Log($"Carta auxiliar {auxiliaryType} seleccionada. Haz clic en una carta para aplicar el efecto.");
        }
    }

    private void Deselect()
    {
        isSelected = false;

        // Restaurar escala original
        transform.localScale = Vector3.one;
    }

    // Método estático para verificar si hay una auxiliar seleccionada
    public static bool HasSelectedAuxiliary()
    {
        return selectedAuxiliary != null;
    }

    // Método estático para que las cartas normales puedan llamarlo
    public static void OnCardClicked(GameObject targetCard)
    {
        if (selectedAuxiliary == null)
        {
            Debug.Log("No hay carta auxiliar seleccionada");
            return;
        }

        // Solo procesar cartas normales para LevelUp y ElementChange
        if (selectedAuxiliary.auxiliaryType == AuxiliaryType.RemoveVictory)
        {
            Debug.Log("La carta RemoveVictory requiere hacer clic en una insignia de victoria, no en una carta normal.");
            return;
        }

        Debug.Log($"Aplicando efecto {selectedAuxiliary.auxiliaryType} a la carta {targetCard.name}");

        // CORRECCIÓN IMPORTANTE: Verificar que el targetCard tenga NetworkIdentity
        NetworkIdentity targetNetId = targetCard.GetComponent<NetworkIdentity>();
        if (targetNetId == null)
        {
            Debug.LogError("La carta objetivo no tiene NetworkIdentity. No se puede procesar en red.");
            return;
        }

        selectedAuxiliary.CmdUseAuxiliaryCard(targetCard);
    }

    // NUEVO: Método específico para cuando se hace clic en una insignia de victoria
    public static void OnVictoryBadgeClicked(CardData.ElementType element, CardData.ColorType color)
    {
        if (selectedAuxiliary == null)
        {
            Debug.Log("No hay carta auxiliar seleccionada");
            return;
        }

        if (selectedAuxiliary.auxiliaryType != AuxiliaryType.RemoveVictory)
        {
            Debug.Log("Solo la carta RemoveVictory puede eliminar insignias de victoria");
            return;
        }

        Debug.Log($"Eliminando insignia de victoria específica: {element} {color}");
        selectedAuxiliary.CmdRemoveSpecificVictory(element, color);
    }

    [Command]
    public void CmdUseAuxiliaryCard(GameObject targetCard)
    {
        Debug.Log($"[Server] Procesando uso de carta auxiliar {auxiliaryType} en {targetCard.name}");

        // Obtener el PlayerManager del jugador que posee esta carta auxiliar
        NetworkIdentity ownerIdentity = GetComponent<NetworkIdentity>();
        PlayerManager playerManager = GetPlayerManager(ownerIdentity.connectionToClient);

        if (playerManager == null)
        {
            Debug.LogError("No se pudo encontrar el PlayerManager para esta carta auxiliar");
            return;
        }

        bool effectApplied = false;

        switch (auxiliaryType)
        {
            case AuxiliaryType.LevelUp:
                effectApplied = TryLevelUpCard(playerManager, targetCard);
                break;
            case AuxiliaryType.ElementChange:
                effectApplied = TryElementChangeCard(playerManager, targetCard);
                break;
            case AuxiliaryType.RemoveVictory:
                // Este caso ya no se usa, se maneja con CmdRemoveSpecificVictory
                Debug.LogWarning("RemoveVictory debe usar CmdRemoveSpecificVictory en lugar de CmdUseAuxiliaryCard");
                break;
        }

        // Limpiar selección
        RpcClearSelection();

        if (effectApplied)
        {
            Debug.Log($"Efecto {auxiliaryType} aplicado exitosamente");
            // Destruir la carta auxiliar después de usarla
            NetworkServer.Destroy(gameObject);
        }
        else
        {
            Debug.Log($"No se pudo aplicar el efecto {auxiliaryType}. Operación cancelada.");
        }
    }

    // NUEVO: Command específico para remover una victoria específica
    [Command]
    public void CmdRemoveSpecificVictory(CardData.ElementType element, CardData.ColorType color)
    {
        Debug.Log($"[Server] === INICIANDO CmdRemoveSpecificVictory para {element} {color} ===");

        PlayerManager opponent = GetOpponentPlayer();
        if (opponent == null)
        {
            Debug.LogError("[Server] No se pudo encontrar el jugador oponente");
            RpcClearSelection();
            return;
        }

        Debug.Log($"[Server] Oponente encontrado: {opponent.name} (NetId: {opponent.netId})");

        // Intentar remover la victoria específica del oponente
        bool victoryRemoved = PlayerVictoryTracker.RemoveSpecificVictory(opponent, element, color);

        // Limpiar selección
        RpcClearSelection();

        if (victoryRemoved)
        {
            Debug.Log($"[Server] Victoria específica removida exitosamente: {element} {color}");

            // Notificar a todos los clientes sobre la remoción de victoria específica
            RpcNotifySpecificVictoryRemoval(opponent.netId, element, color);

            // Destruir la carta auxiliar después de usarla
            NetworkServer.Destroy(gameObject);
        }
        else
        {
            Debug.LogError($"[Server] Falló la remoción de la victoria específica {element} {color}");
        }
    }

    [ClientRpc]
    private void RpcNotifySpecificVictoryRemoval(uint opponentNetId, CardData.ElementType element, CardData.ColorType color)
    {
        Debug.Log($"[Client] Victoria específica removida del jugador {opponentNetId}: {element} {color}");

        // Aquí puedes agregar efectos visuales adicionales si lo deseas
        // Por ejemplo, mostrar un mensaje en pantalla o efectos de partículas
    }

    [Server]
    private bool TryLevelUpCard(PlayerManager playerManager, GameObject targetCard)
    {
        CardData cardData = targetCard.GetComponent<CardData>();
        if (cardData == null)
        {
            Debug.LogError("La carta objetivo no tiene componente CardData");
            return false;
        }

        if (cardData.starLevel >= 3)
        {
            Debug.Log("La carta no se puede mejorar más (nivel máximo alcanzado)");
            return false;
        }

        // Buscar carta de mayor nivel
        GameObject higherLevelCard = FindCardWithHigherLevel(playerManager, cardData);
        if (higherLevelCard != null)
        {
            Debug.Log($"Reemplazando carta nivel {cardData.starLevel} por nivel {higherLevelCard.GetComponent<CardData>().starLevel}");
            ReplaceCard(targetCard, higherLevelCard);
            return true;
        }

        Debug.Log("No se encontró una carta de mayor nivel disponible");
        return false;
    }

    [Server]
    private bool TryElementChangeCard(PlayerManager playerManager, GameObject targetCard)
    {
        CardData originalCardData = targetCard.GetComponent<CardData>();
        if (originalCardData == null)
        {
            Debug.LogError("La carta objetivo no tiene componente CardData");
            return false;
        }

        CardData.ElementType[] allElements = {
            CardData.ElementType.Boton,
            CardData.ElementType.Alfiler,
            CardData.ElementType.Tela,
            CardData.ElementType.Algodon
        };

        // Crear lista de elementos diferentes al actual
        List<CardData.ElementType> availableElements = new List<CardData.ElementType>();

        foreach (CardData.ElementType element in allElements)
        {
            if (element != originalCardData.element)
            {
                // Verificar si existe una carta con este elemento
                GameObject cardWithElement = FindCardWithElement(playerManager, originalCardData, element);
                if (cardWithElement != null)
                {
                    availableElements.Add(element);
                }
            }
        }

        // Si no hay elementos disponibles, fallar
        if (availableElements.Count == 0)
        {
            Debug.Log("No se encontró ninguna carta con elemento diferente disponible");
            return false;
        }

        // Seleccionar un elemento aleatorio de los disponibles
        int randomIndex = Random.Range(0, availableElements.Count);
        CardData.ElementType selectedElement = availableElements[randomIndex];

        // Obtener la carta con el elemento seleccionado
        GameObject cardWithSelectedElement = FindCardWithElement(playerManager, originalCardData, selectedElement);

        if (cardWithSelectedElement != null)
        {
            Debug.Log($"Cambiando elemento de {originalCardData.element} a {selectedElement}");
            ReplaceCard(targetCard, cardWithSelectedElement);
            return true;
        }

        Debug.Log("Error inesperado: no se pudo obtener la carta con el elemento seleccionado");
        return false;
    }

    [Server]
    private GameObject FindCardWithHigherLevel(PlayerManager playerManager, CardData originalCard)
    {
        var availableCards = playerManager.GetAvailableCards();
        Debug.Log($"Buscando carta de nivel superior a {originalCard.starLevel}. Cartas disponibles: {availableCards.Count}");
        Debug.Log($"Carta original: {originalCard.cardName} - Elemento: {originalCard.element}, Color: {originalCard.color}, Nivel: {originalCard.starLevel}");

        foreach (GameObject cardPrefab in availableCards)
        {
            CardData prefabData = cardPrefab.GetComponent<CardData>();
            if (prefabData != null)
            {
                Debug.Log($"Evaluando carta: {prefabData.cardName} - Elemento: {prefabData.element}, Color: {prefabData.color}, Nivel: {prefabData.starLevel}");

                // Buscar cartas del mismo elemento y color, pero de nivel superior
                if (prefabData.element == originalCard.element &&
                    prefabData.color == originalCard.color &&
                    prefabData.starLevel > originalCard.starLevel)
                {
                    Debug.Log($"¡Carta de nivel superior encontrada! {prefabData.cardName} nivel {prefabData.starLevel}");
                    return cardPrefab;
                }
            }
        }

        Debug.Log($"No se encontró carta de nivel superior para {originalCard.element} {originalCard.color} nivel {originalCard.starLevel}");
        return null;
    }

    [Server]
    private GameObject FindCardWithElement(PlayerManager playerManager, CardData originalCard, CardData.ElementType newElement)
    {
        var availableCards = playerManager.GetAvailableCards();
        Debug.Log($"Buscando carta con elemento {newElement}. Cartas disponibles: {availableCards.Count}");

        foreach (GameObject cardPrefab in availableCards)
        {
            CardData prefabData = cardPrefab.GetComponent<CardData>();
            if (prefabData != null)
            {
                Debug.Log($"Evaluando carta: {prefabData.cardName} - Elemento: {prefabData.element}, Color: {prefabData.color}, Nivel: {prefabData.starLevel}");

                if (prefabData.element == newElement &&
                    prefabData.color == originalCard.color &&
                    prefabData.starLevel == originalCard.starLevel)
                {
                    Debug.Log($"¡Carta encontrada! {prefabData.cardName} con elemento {newElement}");
                    return cardPrefab;
                }
            }
        }
        return null;
    }

    [Server]
    private void ReplaceCard(GameObject oldCard, GameObject newCardPrefab)
    {
        Debug.Log($"[Server] Iniciando reemplazo de carta {oldCard.name}");

        Transform originalParent = oldCard.transform.parent;
        Vector3 originalPosition = oldCard.transform.position;
        Vector3 originalLocalPosition = oldCard.transform.localPosition;
        Quaternion originalRotation = oldCard.transform.rotation;

        // Obtener la conexión del jugador propietario de la carta original
        NetworkIdentity oldCardNetId = oldCard.GetComponent<NetworkIdentity>();
        NetworkConnectionToClient ownerConnection = oldCardNetId.connectionToClient;

        Debug.Log($"[Server] Información de la carta original:");
        Debug.Log($"  - Padre: {(originalParent != null ? originalParent.name : "null")}");
        Debug.Log($"  - Posición: {originalPosition}");
        Debug.Log($"  - Posición local: {originalLocalPosition}");
        Debug.Log($"  - Propietario: {ownerConnection}");

        // CORRECCIÓN: Crear la nueva carta ANTES de destruir la original
        // para evitar problemas de referencia
        Debug.Log($"[Server] Creando nueva carta: {newCardPrefab.name}");
        GameObject newCard = Instantiate(newCardPrefab, originalPosition, originalRotation);

        // Spawnnear en la red con el mismo propietario
        NetworkServer.Spawn(newCard, ownerConnection);

        // Configurar posición y padre usando RPC
        string parentName = originalParent != null ? originalParent.name : "";
        string areaType = DetermineAreaType(parentName);

        Debug.Log($"[Server] Configurando nueva carta en área: {areaType} (padre: {parentName})");

        // AHORA destruir la carta original
        NetworkServer.Destroy(oldCard);

        // NUEVO: Limpiar cualquier zoom huérfano después de la destrucción
        RpcClearOrphanedZooms();

        // Usar el sistema existente de PlayerManager para establecer el padre correctamente
        PlayerManager playerManager = GetPlayerManager(ownerConnection);
        if (playerManager != null)
        {
            // Usar el método RpcSetCardParent existente del PlayerManager
            playerManager.RpcSetCardParent(newCard, areaType);
        }
        else
        {
            Debug.LogError("[Server] No se pudo encontrar PlayerManager para configurar la nueva carta");
        }

        Debug.Log($"[Server] Reemplazo de carta completado. Nueva carta: {newCard.name}");
    }

    [Server]
    private string DetermineAreaType(string parentName)
    {
        if (string.IsNullOrEmpty(parentName))
            return "Player"; // Default

        // Mapear nombres de padres a tipos de área
        if (parentName.Contains("AreaJugador") || parentName.Contains("Player"))
            return "Player";
        else if (parentName.Contains("AreaEnemigo") || parentName.Contains("Enemy"))
            return "Player"; // Seguirá siendo del jugador, pero se mostrará en el área enemiga
        else if (parentName.Contains("Limite") || parentName.Contains("DropZone"))
            return "DropZone";
        else if (parentName.Contains("Extra"))
            return "Extra";

        return "Player"; // Default
    }

    [ClientRpc]
    private void RpcClearSelection()
    {
        if (selectedAuxiliary == this)
        {
            selectedAuxiliary = null;
        }
        Deselect();
    }

    // NUEVO: RPC para limpiar zooms huérfanos
    [ClientRpc]
    private void RpcClearOrphanedZooms()
    {
        // Limpiar cualquier zoom huérfano
        CardZoom.ClearAllOrphanedZooms();
    }

    // Método helper para obtener PlayerManager
    private PlayerManager GetPlayerManager(NetworkConnectionToClient connection)
    {
        PlayerManager[] allPlayers = FindObjectsOfType<PlayerManager>();
        foreach (PlayerManager pm in allPlayers)
        {
            if (pm.connectionToClient == connection)
            {
                return pm;
            }
        }
        return null;
    }

    private PlayerManager GetOpponentPlayer()
    {
        PlayerManager[] players = FindObjectsOfType<PlayerManager>();
        NetworkIdentity ownerIdentity = GetComponent<NetworkIdentity>();

        // Buscar el PlayerManager del jugador actual (dueño de esta carta auxiliar)
        PlayerManager ownerPlayer = GetPlayerManager(ownerIdentity.connectionToClient);

        Debug.Log($"[Server] Jugador propietario de la carta auxiliar: {(ownerPlayer != null ? ownerPlayer.name : "NULL")}");
        Debug.Log($"[Server] Total de jugadores encontrados: {players.Length}");

        // Retornar el otro jugador (el oponente)
        foreach (PlayerManager player in players)
        {
            Debug.Log($"[Server] Evaluando jugador: {player.name} (NetId: {player.netId})");
            if (player != ownerPlayer)
            {
                Debug.Log($"[Server] Oponente encontrado: {player.name}");
                return player;
            }
        }

        Debug.LogError("[Server] No se encontró jugador oponente");
        return null;
    }
}