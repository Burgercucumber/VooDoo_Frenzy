using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public class VictoryDisplayManager : NetworkBehaviour
{
    [Header("Referencias de Áreas")]
    [SerializeField] private GameObject playerVictoryArea;
    [SerializeField] private GameObject enemyVictoryArea;

    [Header("Insignias Disponibles")]
    public List<GameObject> victoryBadgePrefabs = new List<GameObject>();

    // Propiedades para acceder a las áreas, con búsqueda automática si es necesario
    public GameObject PlayerVictoryArea
    {
        get
        {
            if (playerVictoryArea == null)
            {
                playerVictoryArea = GameObject.Find("PlayerVictoryArea");
            }
            return playerVictoryArea;
        }
    }

    public GameObject EnemyVictoryArea
    {
        get
        {
            if (enemyVictoryArea == null)
            {
                enemyVictoryArea = GameObject.Find("EnemyVictoryArea");
            }
            return enemyVictoryArea;
        }
    }

    // Diccionario para rastrear las insignias spawneadas por jugador
    private Dictionary<PlayerManager, List<GameObject>> spawnedBadges = new Dictionary<PlayerManager, List<GameObject>>();

    private void Start()
    {
        // Las propiedades ya se encargan de la búsqueda automática
        // Solo hacemos debug de configuración
        Debug.Log($"VictoryDisplayManager configurado:");
        Debug.Log($"- PlayerVictoryArea: {(PlayerVictoryArea != null ? PlayerVictoryArea.name : "NULL")}");
        Debug.Log($"- EnemyVictoryArea: {(EnemyVictoryArea != null ? EnemyVictoryArea.name : "NULL")}");
        Debug.Log($"- Prefabs de insignias: {victoryBadgePrefabs.Count}");

        // Listar los prefabs configurados
        for (int i = 0; i < victoryBadgePrefabs.Count; i++)
        {
            if (victoryBadgePrefabs[i] != null)
            {
                VictoryBadge badge = victoryBadgePrefabs[i].GetComponent<VictoryBadge>();
                if (badge != null)
                {
                    Debug.Log($"  - Prefab {i}: {badge.element} {badge.color}");
                }
                else
                {
                    Debug.LogWarning($"  - Prefab {i}: No tiene componente VictoryBadge");
                }
            }
        }
    }

    [Server]
    public void ShowVictoryBadge(PlayerManager player, CardData.ElementType element, CardData.ColorType color)
    {
        Debug.Log($"[Server] ===== INICIANDO ShowVictoryBadge =====");
        Debug.Log($"[Server] Player: {(player != null ? player.netId.ToString() : "NULL")}");
        Debug.Log($"[Server] Element: {element}, Color: {color}");
        Debug.Log($"[Server] Prefabs disponibles: {victoryBadgePrefabs.Count}");

        // Verificar que el player no sea null
        if (player == null)
        {
            Debug.LogError("[Server] Player es null en ShowVictoryBadge");
            return;
        }

        // Verificar que tengamos prefabs
        if (victoryBadgePrefabs.Count == 0)
        {
            Debug.LogError("[Server] No hay prefabs de insignias configurados");
            return;
        }

        // Buscar la insignia correspondiente
        GameObject badgePrefab = FindBadgePrefab(element, color);

        if (badgePrefab == null)
        {
            Debug.LogError($"[Server] No se encontró insignia para {element} {color}");
            Debug.Log("[Server] Prefabs disponibles:");
            for (int i = 0; i < victoryBadgePrefabs.Count; i++)
            {
                if (victoryBadgePrefabs[i] != null)
                {
                    VictoryBadge badge = victoryBadgePrefabs[i].GetComponent<VictoryBadge>();
                    if (badge != null)
                    {
                        Debug.Log($"  - Prefab {i}: {badge.element} {badge.color}");
                    }
                    else
                    {
                        Debug.Log($"  - Prefab {i}: SIN VictoryBadge component");
                    }
                }
                else
                {
                    Debug.Log($"  - Prefab {i}: NULL");
                }
            }
            return;
        }

        Debug.Log($"[Server] Prefab encontrado: {badgePrefab.name}");
        Debug.Log($"[Server] Spawneando insignia {element} {color} para jugador {player.netId}");

        // Instanciar la insignia
        Debug.Log($"[Server] Intentando instanciar prefab: {badgePrefab.name}");
        GameObject badgeInstance = null;

        try
        {
            badgeInstance = Instantiate(badgePrefab, Vector3.zero, Quaternion.identity);
            Debug.Log($"[Server] Instanciación exitosa: {(badgeInstance != null ? badgeInstance.name : "NULL")}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Server] Error al instanciar prefab: {e.Message}");
            return;
        }

        if (badgeInstance == null)
        {
            Debug.LogError("[Server] badgeInstance es null después de Instantiate");
            return;
        }

        // Asegurar que tiene NetworkIdentity
        Debug.Log("[Server] Verificando NetworkIdentity...");
        NetworkIdentity netIdentity = badgeInstance.GetComponent<NetworkIdentity>();
        if (netIdentity == null)
        {
            Debug.Log("[Server] Agregando NetworkIdentity...");
            netIdentity = badgeInstance.AddComponent<NetworkIdentity>();
        }
        Debug.Log($"[Server] NetworkIdentity OK: {netIdentity != null}");

        // Spawnear en la red
        Debug.Log("[Server] Intentando NetworkServer.Spawn...");
        try
        {
            NetworkServer.Spawn(badgeInstance);
            Debug.Log($"[Server] Spawn exitoso con netId: {netIdentity.netId}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Server] Error en NetworkServer.Spawn: {e.Message}");
            if (badgeInstance != null)
            {
                Destroy(badgeInstance);
            }
            return;
        }

        // Agregar a la lista de insignias del jugador
        if (!spawnedBadges.ContainsKey(player))
            spawnedBadges[player] = new List<GameObject>();

        spawnedBadges[player].Add(badgeInstance);

        // Asignar al área correspondiente usando RPC - PASAR EL NETID EN LUGAR DEL GAMEOBJECT
        RpcSetBadgeParent(badgeInstance.GetComponent<NetworkIdentity>().netId, player.netId);

        Debug.Log($"[Server] ===== FIN ShowVictoryBadge EXITOSO =====");
    }

    [ClientRpc]
    private void RpcSetBadgeParent(uint badgeNetId, uint playerNetId)
    {
        // BUSCAR EL OBJETO POR SU NETID EN LUGAR DE USAR LA REFERENCIA DIRECTA
        NetworkIdentity badgeNetIdentity = NetworkClient.spawned.ContainsKey(badgeNetId) ? NetworkClient.spawned[badgeNetId] : null;

        if (badgeNetIdentity == null)
        {
            Debug.LogWarning($"[Client] RpcSetBadgeParent: No se encontró objeto con netId {badgeNetId}");
            return;
        }

        GameObject badge = badgeNetIdentity.gameObject;

        if (badge == null)
        {
            Debug.LogWarning("[Client] RpcSetBadgeParent: badge es null");
            return;
        }

        Debug.Log($"[Client] Asignando insignia a área para jugador {playerNetId}");

        // Determinar si el jugador es local o enemigo
        PlayerManager localPlayer = NetworkClient.localPlayer?.GetComponent<PlayerManager>();
        bool isLocalPlayer = localPlayer != null && localPlayer.netId == playerNetId;

        GameObject targetArea = isLocalPlayer ? PlayerVictoryArea : EnemyVictoryArea;

        Debug.Log($"[Client] Área objetivo: {(targetArea != null ? targetArea.name : "NULL")}, Es jugador local: {isLocalPlayer}");

        if (targetArea != null)
        {
            try
            {
                // VERIFICAR QUE EL BADGE NO SEA UN PREFAB ANTES DE MODIFICAR
                if (badge.scene.IsValid()) // Solo los objetos instanciados tienen una escena válida
                {
                    // Asignar como hijo del área
                    badge.transform.SetParent(targetArea.transform, false);

                    // Configurar posición y escala local
                    badge.transform.localPosition = Vector3.zero;
                    badge.transform.localScale = Vector3.one;

                    // Asegurar que esté activo
                    badge.SetActive(true);

                    // Organizar las insignias en el área con el nuevo sistema
                    OrganizeBadgesInAreaByElement(targetArea.transform);

                    Debug.Log($"[Client] Insignia asignada correctamente a {targetArea.name}");
                }
                else
                {
                    Debug.LogError("[Client] Intentando modificar un prefab en lugar de una instancia");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Client] Error al asignar parent: {e.Message}");

                // Solución alternativa: posicionar manualmente
                if (badge.scene.IsValid())
                {
                    badge.transform.position = targetArea.transform.position;
                    badge.transform.rotation = targetArea.transform.rotation;
                    badge.SetActive(true);
                }
            }
        }
        else
        {
            Debug.LogError($"[Client] No se encontró el área de victoria correspondiente");
        }
    }

    // Nuevo método para organizar las insignias por elemento y color
    private void OrganizeBadgesInAreaByElement(Transform area)
    {
        // Obtener todas las insignias en el área
        List<VictoryBadge> badges = new List<VictoryBadge>();

        for (int i = 0; i < area.childCount; i++)
        {
            Transform child = area.GetChild(i);
            VictoryBadge badge = child.GetComponent<VictoryBadge>();
            if (badge != null)
            {
                badges.Add(badge);
            }
        }

        if (badges.Count == 0) return;

        // Agrupar por elemento, manteniendo el orden de aparición del primer color de cada elemento
        var elementGroups = new Dictionary<CardData.ElementType, List<VictoryBadge>>();
        var elementOrder = new List<CardData.ElementType>();

        foreach (var badge in badges)
        {
            if (!elementGroups.ContainsKey(badge.element))
            {
                elementGroups[badge.element] = new List<VictoryBadge>();
                elementOrder.Add(badge.element);
            }
            elementGroups[badge.element].Add(badge);
        }

        // Dentro de cada elemento, ordenar por color manteniendo el orden de aparición
        foreach (var elementType in elementOrder)
        {
            var elementBadges = elementGroups[elementType];
            var colorOrder = new List<CardData.ColorType>();
            var colorGroups = new Dictionary<CardData.ColorType, List<VictoryBadge>>();

            foreach (var badge in elementBadges)
            {
                if (!colorGroups.ContainsKey(badge.color))
                {
                    colorGroups[badge.color] = new List<VictoryBadge>();
                    colorOrder.Add(badge.color);
                }
                colorGroups[badge.color].Add(badge);
            }

            // Reorganizar la lista de badges del elemento según el orden de colores
            elementGroups[elementType].Clear();
            foreach (var color in colorOrder)
            {
                elementGroups[elementType].AddRange(colorGroups[color]);
            }
        }

        // Configurar espaciado
        float horizontalSpacing = 60f; // Espaciado horizontal entre insignias
        float verticalSpacing = 80f;   // Espaciado vertical entre filas (elementos)

        int currentRow = 0;

        // Posicionar las insignias
        foreach (var elementType in elementOrder)
        {
            var elementBadges = elementGroups[elementType];

            // Agrupar por color para esta fila
            var colorGroups = new Dictionary<CardData.ColorType, List<VictoryBadge>>();
            var colorOrder = new List<CardData.ColorType>();

            foreach (var badge in elementBadges)
            {
                if (!colorGroups.ContainsKey(badge.color))
                {
                    colorGroups[badge.color] = new List<VictoryBadge>();
                    colorOrder.Add(badge.color);
                }
                colorGroups[badge.color].Add(badge);
            }

            int currentCol = 0;

            // Posicionar por colores horizontalmente
            foreach (var color in colorOrder)
            {
                var colorBadges = colorGroups[color];

                // Posicionar múltiples insignias del mismo color horizontalmente
                for (int i = 0; i < colorBadges.Count; i++)
                {
                    VictoryBadge badge = colorBadges[i];

                    Vector3 position = new Vector3(
                        (currentCol + i) * horizontalSpacing, // Posición horizontal para insignias del mismo color
                        -currentRow * verticalSpacing,        // Misma fila para insignias del mismo color
                        0
                    );

                    badge.transform.localPosition = position;
                }

                // Avanzar las columnas según la cantidad de insignias de este color
                currentCol += colorBadges.Count;
            }

            // Avanzar a la siguiente fila
            currentRow++;
        }

        // Centrar toda la disposición
        CenterBadgeLayout(area);
    }

    // Método para centrar el layout de insignias
    private void CenterBadgeLayout(Transform area)
    {
        if (area.childCount == 0) return;

        // Calcular los límites del layout
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        for (int i = 0; i < area.childCount; i++)
        {
            Transform child = area.GetChild(i);
            Vector3 pos = child.localPosition;

            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }

        // Calcular el offset para centrar
        Vector3 centerOffset = new Vector3(
            -(minX + maxX) * 0.5f,
            -(minY + maxY) * 0.5f,
            0
        );

        // Aplicar el offset a todas las insignias
        for (int i = 0; i < area.childCount; i++)
        {
            Transform child = area.GetChild(i);
            child.localPosition += centerOffset;
        }
    }

    // Método mantenido para compatibilidad, pero ahora llama al nuevo método
    private void OrganizeBadgesInArea(Transform area)
    {
        OrganizeBadgesInAreaByElement(area);
    }

    // Buscar el prefab de insignia correspondiente
    private GameObject FindBadgePrefab(CardData.ElementType element, CardData.ColorType color)
    {
        Debug.Log($"[Server] Buscando prefab para: {element} {color}");
        Debug.Log($"[Server] Prefabs a revisar: {victoryBadgePrefabs.Count}");

        for (int i = 0; i < victoryBadgePrefabs.Count; i++)
        {
            GameObject prefab = victoryBadgePrefabs[i];
            Debug.Log($"[Server] Revisando prefab {i}: {(prefab != null ? prefab.name : "NULL")}");

            if (prefab == null)
            {
                Debug.LogWarning($"[Server] Prefab {i} es null");
                continue;
            }

            VictoryBadge badge = prefab.GetComponent<VictoryBadge>();
            if (badge == null)
            {
                Debug.LogWarning($"[Server] Prefab {i} ({prefab.name}) no tiene componente VictoryBadge");
                continue;
            }

            Debug.Log($"[Server] Prefab {i}: {badge.element} {badge.color} vs buscado {element} {color}");

            if (badge.MatchesVictory(element, color))
            {
                Debug.Log($"[Server] ¡MATCH encontrado! Prefab: {prefab.name}");
                return prefab;
            }
        }

        Debug.LogError($"[Server] No se encontró ningún prefab que coincida con {element} {color}");
        return null;
    }

    [Server]
    public void RemovePlayerBadge(PlayerManager player, CardData.ElementType element, CardData.ColorType color)
    {
        if (!spawnedBadges.ContainsKey(player)) return;

        List<GameObject> playerBadges = spawnedBadges[player];

        for (int i = playerBadges.Count - 1; i >= 0; i--)
        {
            GameObject badge = playerBadges[i];
            if (badge != null)
            {
                VictoryBadge badgeComponent = badge.GetComponent<VictoryBadge>();
                if (badgeComponent != null && badgeComponent.MatchesVictory(element, color))
                {
                    playerBadges.RemoveAt(i);

                    // Notificar a los clientes antes de destruir
                    RpcNotifyBadgeRemoval(badge.GetComponent<NetworkIdentity>().netId);

                    NetworkServer.Destroy(badge);
                    Debug.Log($"Insignia {element} {color} removida del jugador {player.name}");

                    // Reorganizar las insignias restantes
                    StartCoroutine(ReorganizeBadgesAfterRemoval(player));
                    return;
                }
            }
        }
    }

    [ClientRpc]
    private void RpcNotifyBadgeRemoval(uint badgeNetId)
    {
        NetworkIdentity badgeNetIdentity = NetworkClient.spawned.ContainsKey(badgeNetId) ? NetworkClient.spawned[badgeNetId] : null;
        if (badgeNetIdentity != null)
        {
            Debug.Log($"[Client] La insignia será eliminada");
        }
    }

    private IEnumerator ReorganizeBadgesAfterRemoval(PlayerManager player)
    {
        yield return new WaitForEndOfFrame();

        // Determinar si es jugador local para reorganizar en el área correcta
        PlayerManager localPlayer = NetworkClient.localPlayer?.GetComponent<PlayerManager>();
        bool isLocalPlayer = localPlayer != null && localPlayer.netId == player.netId;

        RpcReorganizePlayerBadges(player.netId, isLocalPlayer);
    }

    [ClientRpc]
    private void RpcReorganizePlayerBadges(uint playerNetId, bool isLocalPlayer)
    {
        GameObject targetArea = isLocalPlayer ? PlayerVictoryArea : EnemyVictoryArea;
        if (targetArea != null)
        {
            OrganizeBadgesInAreaByElement(targetArea.transform);
        }
    }

    [Server]
    public void ClearPlayerBadges(PlayerManager player)
    {
        if (!spawnedBadges.ContainsKey(player)) return;

        List<GameObject> playerBadges = spawnedBadges[player];

        // Notificar antes de limpiar
        RpcNotifyPlayerBadgesClear(player.netId);

        foreach (GameObject badge in playerBadges)
        {
            if (badge != null)
                NetworkServer.Destroy(badge);
        }

        playerBadges.Clear();
        Debug.Log($"Todas las insignias del jugador {player.name} han sido eliminadas");
    }

    [ClientRpc]
    private void RpcNotifyPlayerBadgesClear(uint playerNetId)
    {
        Debug.Log($"[Client] Todas las insignias del jugador {playerNetId} serán eliminadas");
    }

    [Server]
    public void ResetAllBadges()
    {
        // Notificar antes del reset
        RpcNotifyAllBadgesReset();

        foreach (var kvp in spawnedBadges)
        {
            foreach (GameObject badge in kvp.Value)
            {
                if (badge != null)
                    NetworkServer.Destroy(badge);
            }
        }

        spawnedBadges.Clear();
        Debug.Log("Todas las insignias han sido reseteadas");
    }

    [ClientRpc]
    private void RpcNotifyAllBadgesReset()
    {
        Debug.Log("[Client] Todas las insignias serán reseteadas");
    }

    // Método de utilidad para obtener el área de un jugador específico
    public GameObject GetPlayerVictoryArea(PlayerManager player)
    {
        PlayerManager localPlayer = NetworkClient.localPlayer?.GetComponent<PlayerManager>();
        bool isLocalPlayer = localPlayer != null && localPlayer.netId == player.netId;

        return isLocalPlayer ? PlayerVictoryArea : EnemyVictoryArea;
    }

    // Método para verificar si un área está disponible
    public bool AreAreasConfigured()
    {
        return PlayerVictoryArea != null && EnemyVictoryArea != null;
    }

    // Método para obtener el número de insignias de un jugador
    public int GetPlayerBadgeCount(PlayerManager player)
    {
        if (!spawnedBadges.ContainsKey(player))
            return 0;

        return spawnedBadges[player].Count;
    }
}