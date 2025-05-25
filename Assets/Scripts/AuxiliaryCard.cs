using UnityEngine;
using Mirror;

public class AuxiliaryCard : NetworkBehaviour
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

    private void OnMouseDown()
    {
        // Solo el dueño puede usar sus cartas auxiliares
        // Verificar si este cliente tiene autoridad sobre esta carta auxiliar
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

        // Si quieres cambiar color y usas otro tipo de renderer, descomenta y adapta:
        // Renderer renderer = GetComponent<Renderer>();
        // if (renderer != null)
        // {
        //     renderer.material.color = Color.yellow;
        // }

        Debug.Log($"Carta auxiliar {auxiliaryType} seleccionada. Haz clic en una carta para aplicar el efecto.");
    }

    private void Deselect()
    {
        isSelected = false;

        // Restaurar escala original
        transform.localScale = Vector3.one;

        // Si quieres restaurar color y usas otro tipo de renderer, descomenta y adapta:
        // Renderer renderer = GetComponent<Renderer>();
        // if (renderer != null)
        // {
        //     renderer.material.color = Color.white;
        // }
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
        selectedAuxiliary.CmdUseAuxiliaryCard(targetCard);
    }

    [Command]
    public void CmdUseAuxiliaryCard(GameObject targetCard)
    {
        // Obtener el PlayerManager del jugador que posee esta carta auxiliar
        NetworkIdentity ownerIdentity = GetComponent<NetworkIdentity>();
        PlayerManager playerManager = null;

        // Buscar el PlayerManager asociado con la conexión del cliente
        PlayerManager[] allPlayers = FindObjectsOfType<PlayerManager>();
        foreach (PlayerManager pm in allPlayers)
        {
            if (pm.connectionToClient == ownerIdentity.connectionToClient)
            {
                playerManager = pm;
                break;
            }
        }

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
        if (cardData == null || cardData.starLevel >= 3)
        {
            Debug.Log("La carta no se puede mejorar más (nivel máximo alcanzado)");
            return false;
        }

        // Buscar carta de mayor nivel
        GameObject higherLevelCard = FindCardWithHigherLevel(playerManager, cardData);
        if (higherLevelCard != null)
        {
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
        if (originalCardData == null) return false;

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
            // Asumiendo que tienes un sistema de victorias implementado
            // return PlayerVictoryTracker.RemoveRandomVictory(opponent);
            Debug.Log("Efecto RemoveVictory aplicado (placeholder)");
            return true; // Placeholder - implementar según tu sistema de victorias
        }
        return false;
    }

    [Server]
    private GameObject FindCardWithHigherLevel(PlayerManager playerManager, CardData originalCard)
    {
        foreach (GameObject cardPrefab in playerManager.GetAvailableCards())
        {
            CardData prefabData = cardPrefab.GetComponent<CardData>();
            if (prefabData != null &&
                prefabData.element == originalCard.element &&
                prefabData.color == originalCard.color &&
                prefabData.starLevel > originalCard.starLevel &&
                prefabData.starLevel <= 3)
            {
                return cardPrefab;
            }
        }
        return null;
    }

    [Server]
    private GameObject FindCardWithElement(PlayerManager playerManager, CardData originalCard, CardData.ElementType newElement)
    {
        foreach (GameObject cardPrefab in playerManager.GetAvailableCards())
        {
            CardData prefabData = cardPrefab.GetComponent<CardData>();
            if (prefabData != null &&
                prefabData.element == newElement &&
                prefabData.color == originalCard.color &&
                prefabData.starLevel == originalCard.starLevel)
            {
                return cardPrefab;
            }
        }
        return null;
    }

    [Server]
    private void ReplaceCard(GameObject oldCard, GameObject newCardPrefab)
    {
        Transform originalParent = oldCard.transform.parent;
        Vector3 originalLocalPosition = oldCard.transform.localPosition;

        // Obtener la conexión del jugador propietario de la carta original
        NetworkIdentity oldCardNetId = oldCard.GetComponent<NetworkIdentity>();
        NetworkConnectionToClient ownerConnection = oldCardNetId.connectionToClient;

        // Crear la nueva carta
        GameObject newCard = Instantiate(newCardPrefab, oldCard.transform.position, oldCard.transform.rotation);
        NetworkServer.Spawn(newCard, ownerConnection);

        // Configurar posición
        RpcSetCardPosition(newCard, originalParent, originalLocalPosition);

        // Destruir la carta original
        NetworkServer.Destroy(oldCard);

        Debug.Log("Carta reemplazada exitosamente");
    }

    [ClientRpc]
    private void RpcSetCardPosition(GameObject newCard, Transform parent, Vector3 localPosition)
    {
        if (newCard != null && parent != null)
        {
            newCard.transform.SetParent(parent, false);
            newCard.transform.localPosition = localPosition;
        }
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

    private PlayerManager GetOpponentPlayer()
    {
        PlayerManager[] players = FindObjectsOfType<PlayerManager>();
        NetworkIdentity ownerIdentity = GetComponent<NetworkIdentity>();

        // Buscar el PlayerManager del jugador actual
        PlayerManager ownerPlayer = null;
        foreach (PlayerManager player in players)
        {
            if (player.connectionToClient == ownerIdentity.connectionToClient)
            {
                ownerPlayer = player;
                break;
            }
        }

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