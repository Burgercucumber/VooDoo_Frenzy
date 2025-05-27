using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

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

        Debug.Log($"Carta auxiliar {auxiliaryType} seleccionada. Haz clic en una carta para aplicar el efecto.");
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
                effectApplied = TryRemoveVictory();
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

        CardData.ElementType[] elements = {
            CardData.ElementType.Boton,
            CardData.ElementType.Alfiler,
            CardData.ElementType.Tela,
            CardData.ElementType.Algodon
        };

        // Buscar un elemento diferente disponible
        foreach (CardData.ElementType element in elements)
        {
            if (element != originalCardData.element)
            {
                GameObject cardWithElement = FindCardWithElement(playerManager, originalCardData, element);
                if (cardWithElement != null)
                {
                    Debug.Log($"Cambiando elemento de {originalCardData.element} a {element}");
                    ReplaceCard(targetCard, cardWithElement);
                    return true;
                }
            }
        }

        Debug.Log("No se encontró una carta con elemento diferente disponible");
        return false;
    }

    [Server]
    private bool TryRemoveVictory()
    {
        PlayerManager opponent = GetOpponentPlayer();
        if (opponent != null)
        {
            Debug.Log("Efecto RemoveVictory aplicado (placeholder)");
            return true; // Placeholder - implementar según tu sistema de victorias
        }
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

        // Buscar el PlayerManager del jugador actual
        PlayerManager ownerPlayer = GetPlayerManager(ownerIdentity.connectionToClient);

        // Retornar el otro jugador
        foreach (PlayerManager player in players)
        {
            if (player != ownerPlayer)
            {
                return player;
            }
        }
        return null;
    }
}