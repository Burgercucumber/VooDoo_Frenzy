using System.Collections.Generic;
using UnityEngine;
using Mirror;

// Clase para gestionar la UI de victorias
public class VictoryUIManager : NetworkBehaviour
{
    [Header("Prefabs de Victoria")]
    public GameObject victoryTR; // Tu gameObject VictoryTR existente
    public GameObject victoryTM; // Tela Morada (ejemplo)
    public GameObject victoryTV; // Tela Verde (ejemplo)

    [Header("Prefabs Botón")]
    public GameObject victoryBR; // Botón Rojo
    public GameObject victoryBM; // Botón Morado
    public GameObject victoryBV; // Botón Verde

    [Header("Referencias de Áreas")]
    [SerializeField] private Transform playerVictoryArea;
    [SerializeField] private Transform enemyVictoryArea;

    // Diccionario para mapear elemento+color a prefab
    private Dictionary<(CardData.ElementType, CardData.ColorType), GameObject> victoryPrefabs;

    // Lista para trackear las victorias mostradas por jugador
    private Dictionary<PlayerManager, List<GameObject>> displayedVictories = new Dictionary<PlayerManager, List<GameObject>>();

    private void Start()
    {
        InitializeVictoryPrefabs();
        FindVictoryAreas();
    }

    private void InitializeVictoryPrefabs()
    {
        victoryPrefabs = new Dictionary<(CardData.ElementType, CardData.ColorType), GameObject>();

        // Mapear tus prefabs existentes - ajusta según tus elementos y colores reales
        if (victoryTR != null)
            victoryPrefabs[(CardData.ElementType.Tela, CardData.ColorType.Rojo)] = victoryTR;

        if (victoryTM != null)
            victoryPrefabs[(CardData.ElementType.Tela, CardData.ColorType.Morado)] = victoryTM;

        if (victoryTV != null)
            victoryPrefabs[(CardData.ElementType.Tela, CardData.ColorType.Verde)] = victoryTV;

        // Prefabs para botones
        if (victoryBR != null)
            victoryPrefabs[(CardData.ElementType.Boton, CardData.ColorType.Rojo)] = victoryBR;

        if (victoryBM != null)
            victoryPrefabs[(CardData.ElementType.Boton, CardData.ColorType.Morado)] = victoryBM;

        if (victoryBV != null)
            victoryPrefabs[(CardData.ElementType.Boton, CardData.ColorType.Verde)] = victoryBV;
    }

    private void FindVictoryAreas()
    {
        if (playerVictoryArea == null)
            playerVictoryArea = GameObject.Find("VictoryJugador")?.transform;

        if (enemyVictoryArea == null)
            enemyVictoryArea = GameObject.Find("VictoryEnemigo")?.transform;

        if (playerVictoryArea == null || enemyVictoryArea == null)
        {
            Debug.LogError("No se encontraron las áreas de victoria en el canvas!");
        }
    }

    // Método principal para mostrar una nueva victoria (llamado desde el servidor)
    [Server]
    public void ShowVictory(PlayerManager player, CardData.ElementType element, CardData.ColorType color)
    {
        if (!isServer) return;

        // Llamar RPC para mostrar en todos los clientes
        RpcShowVictory(player.netId, (int)element, (int)color);
    }

    // Método para remover victoria (llamado desde el servidor)
    [Server]
    public void RemoveVictory(PlayerManager player)
    {
        if (!isServer) return;

        // Llamar RPC para remover en todos los clientes
        RpcRemoveVictory(player.netId);
    }

    // Método para limpiar todas las victorias (llamado desde el servidor)
    [Server]
    public void ClearAllVictories()
    {
        if (!isServer) return;

        RpcClearAllVictories();
    }

    // RPC para mostrar victoria en los clientes
    [ClientRpc]
    private void RpcShowVictory(uint playerNetId, int elementType, int colorType)
    {
        // Buscar el PlayerManager por su netId
        PlayerManager player = FindPlayerByNetId(playerNetId);
        if (player == null)
        {
            Debug.LogError($"No se encontró jugador con netId: {playerNetId}");
            return;
        }

        CardData.ElementType element = (CardData.ElementType)elementType;
        CardData.ColorType color = (CardData.ColorType)colorType;

        // Determinar si es el jugador local o el enemigo
        bool isLocalPlayer = player.isOwned;
        Transform targetArea = isLocalPlayer ? playerVictoryArea : enemyVictoryArea;

        if (targetArea == null)
        {
            Debug.LogError("Área de victoria no encontrada!");
            return;
        }

        // Crear la victoria visual
        CreateVictoryVisual(player, element, color, targetArea);
    }

    private void CreateVictoryVisual(PlayerManager player, CardData.ElementType element, CardData.ColorType color, Transform targetArea)
    {
        // Buscar el prefab correspondiente
        if (!victoryPrefabs.TryGetValue((element, color), out GameObject prefab))
        {
            Debug.LogWarning($"No se encontró prefab para {element} - {color}. Usando prefab por defecto.");

            // Usar el primer prefab disponible como fallback
            if (victoryTR != null)
                prefab = victoryTR;
            else
            {
                Debug.LogError("No hay prefabs de victoria configurados!");
                return;
            }
        }

        // Instanciar el objeto de victoria
        GameObject victoryObj = Instantiate(prefab, targetArea);

        // Configurar posición y escala
        RectTransform rectTransform = victoryObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Ajustar posición basada en el número de victorias existentes
            int victoryCount = GetPlayerVictoryCount(player);
            PositionVictoryObject(rectTransform, victoryCount);
        }

        // Trackear la victoria mostrada
        if (!displayedVictories.ContainsKey(player))
            displayedVictories[player] = new List<GameObject>();

        displayedVictories[player].Add(victoryObj);

        Debug.Log($"Victoria mostrada: {element} - {color} para jugador {player.netId}");
    }

    private void PositionVictoryObject(RectTransform rectTransform, int index)
    {
        // Configurar posición en grid - ajusta según tu diseño
        float spacing = 80f; // Espacio entre victorias
        int columns = 4; // Número de columnas en el grid

        int row = index / columns;
        int col = index % columns;

        Vector2 position = new Vector2(col * spacing, -row * spacing);
        rectTransform.anchoredPosition = position;

        // Configurar escala si es necesario
        rectTransform.localScale = Vector3.one;
    }

    // RPC para remover una victoria de la UI
    [ClientRpc]
    private void RpcRemoveVictory(uint playerNetId)
    {
        PlayerManager player = FindPlayerByNetId(playerNetId);
        if (player == null || !displayedVictories.ContainsKey(player))
            return;

        List<GameObject> playerVictoryObjects = displayedVictories[player];
        if (playerVictoryObjects.Count > 0)
        {
            // Remover una victoria aleatoria (o la última)
            int indexToRemove = Random.Range(0, playerVictoryObjects.Count);
            GameObject victoryToRemove = playerVictoryObjects[indexToRemove];

            playerVictoryObjects.RemoveAt(indexToRemove);

            if (victoryToRemove != null)
                Destroy(victoryToRemove);

            // Reposicionar las victorias restantes
            RepositionVictories(player);

            Debug.Log($"Victoria removida de la UI para jugador {player.netId}");
        }
    }

    private void RepositionVictories(PlayerManager player)
    {
        if (!displayedVictories.ContainsKey(player))
            return;

        List<GameObject> victories = displayedVictories[player];
        for (int i = 0; i < victories.Count; i++)
        {
            if (victories[i] != null)
            {
                RectTransform rectTransform = victories[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    PositionVictoryObject(rectTransform, i);
                }
            }
        }
    }

    private PlayerManager FindPlayerByNetId(uint netId)
    {
        // Buscar en todos los PlayerManagers activos
        PlayerManager[] players = FindObjectsOfType<PlayerManager>();
        foreach (PlayerManager player in players)
        {
            if (player.netId == netId)
                return player;
        }
        return null;
    }

    private int GetPlayerVictoryCount(PlayerManager player)
    {
        if (!displayedVictories.ContainsKey(player))
            return 0;
        return displayedVictories[player].Count;
    }

    // RPC para limpiar todas las victorias (al reiniciar juego)
    [ClientRpc]
    private void RpcClearAllVictories()
    {
        foreach (var kvp in displayedVictories)
        {
            foreach (GameObject victoryObj in kvp.Value)
            {
                if (victoryObj != null)
                    Destroy(victoryObj);
            }
        }
        displayedVictories.Clear();
        Debug.Log("Todas las victorias limpiadas de la UI");
    }
}