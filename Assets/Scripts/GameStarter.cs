using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameStarter : NetworkBehaviour
{
    [SyncVar]
    private bool gameHasStarted = false;

    [SyncVar]
    private bool gameHasEnded = false;

    // Este método debe ser llamado después de que un jugador reciba sus cartas
    [Command(requiresAuthority = false)]
    public void CmdRequestGameStart(NetworkConnectionToClient conn = null)
    {
        if (!isServer) return;
        if (gameHasStarted)
        {
            Debug.Log("[Server] Intento de iniciar el juego, pero ya está iniciado.");
            return;
        }
        // Verificar si todos los jugadores tienen cartas
        CheckAllPlayersReady();
    }

    [Server]
    private void CheckAllPlayersReady()
    {
        if (!isServer) return;
        Debug.Log("[Server] Verificando si todos los jugadores están listos...");

        // Buscar todos los jugadores en la escena
        PlayerManager[] allPlayers = GameObject.FindObjectsOfType<PlayerManager>();
        if (allPlayers.Length == 0)
        {
            Debug.LogError("[Server] No se encontraron jugadores en la escena.");
            return;
        }

        // Verificar si todos los jugadores tienen al menos una carta
        bool allReady = true;
        foreach (PlayerManager player in allPlayers)
        {
            if (player == null || player.PlayerArea == null)
            {
                Debug.LogError("[Server] Jugador o área de jugador es nula");
                allReady = false;
                break;
            }

            bool hasCards = false;
            // Contar directamente las cartas bajo el área del jugador
            foreach (Transform child in player.PlayerArea.transform)
            {
                if (child.CompareTag("Card"))
                {
                    hasCards = true;
                    break;
                }
            }

            if (!hasCards)
            {
                Debug.Log($"[Server] Jugador {player.netId} aún no está listo (no tiene cartas).");
                allReady = false;
                break;
            }
        }

        // Si todos están listos, iniciar el juego
        if (allReady)
        {
            Debug.Log("[Server] Todos los jugadores están listos. Iniciando juego...");
            gameHasStarted = true;
            gameHasEnded = false; // Asegurar que el juego no esté marcado como terminado

            // Notificar a todos los clientes
            RpcNotifyGameStarted();

            // Buscar el RoundManager y comenzar el juego
            RoundManager roundManager = GameObject.FindObjectOfType<RoundManager>();
            if (roundManager != null)
            {
                roundManager.StartGame();
            }
            else
            {
                Debug.LogError("[Server] No se encontró el RoundManager en la escena.");
            }
        }
        else
        {
            Debug.Log("[Server] Aún no están todos los jugadores listos.");
        }
    }

    [ClientRpc]
    void RpcNotifyGameStarted()
    {
        Debug.Log("[Client] El juego ha iniciado oficialmente.");
    }

    // NUEVO: Método para finalizar el juego cuando alguien gana
    [Server]
    public void EndGameWithWinner(PlayerManager winner)
    {
        if (!isServer) return;
        if (gameHasEnded) return; // Evitar múltiples llamadas

        Debug.Log($"[Server] ¡El juego ha terminado! Ganador: {winner.netId}");

        gameHasEnded = true;
        gameHasStarted = false;

        // Notificar a todos los clientes sobre el ganador
        RpcNotifyGameEnded(winner.netId);

        // Iniciar el proceso de reset completo después de un breve delay
        StartCoroutine(CompleteGameReset());
    }

    [ClientRpc]
    void RpcNotifyGameEnded(uint winnerNetId)
    {
        Debug.Log($"[Client] ¡El juego ha terminado! Ganador: {winnerNetId}");

        // Aquí puedes agregar efectos visuales, sonidos, UI de victoria, etc.
        // Por ejemplo:
        // UIManager.Instance?.ShowVictoryScreen(winnerNetId);
        // AudioManager.Instance?.PlayVictorySound();
    }

    // NUEVO: Método para forzar sincronización de estados
    [ClientRpc]
    void RpcForceSyncStates()
    {
        Debug.Log("[Client] Forzando sincronización de estados después del reset...");

        // Forzar actualización de todas las referencias de área
        PlayerManager[] allPlayers = GameObject.FindObjectsOfType<PlayerManager>();
        foreach (PlayerManager player in allPlayers)
        {
            if (player != null)
            {
                // Forzar re-búsqueda de las áreas
                var _ = player.PlayerArea; // Esto triggerea la búsqueda automática
                var __ = player.EnemyArea;
                var ___ = player.DropZone;
            }
        }

        // Esperar un frame para asegurar que todo esté sincronizado
        StartCoroutine(DelayedStateReset());
    }

    IEnumerator DelayedStateReset()
    {
        yield return null; // Esperar un frame
        Debug.Log("[Client] Sincronización de estados completada");
    }

    // NUEVO: Método para resetear el RoundManager
    [Server]
    private void ResetRoundManager()
    {
        Debug.Log("[Server] Reseteando RoundManager al estado inicial...");

        RoundManager roundManager = GameObject.FindObjectOfType<RoundManager>();
        if (roundManager != null)
        {
            // Llamar al método de reset del RoundManager
            roundManager.ResetToInitialState();
            Debug.Log("[Server] RoundManager reseteado correctamente");
        }
        else
        {
            Debug.LogWarning("[Server] No se encontró RoundManager para resetear");
        }
    }

    // NUEVO: Método para resetear el animator
    [Server]
    private void ResetAnimator()
    {
        Debug.Log("[Server] Reseteando SimpleBattleAnimator...");

        if (SimpleBattleAnimator.Instance != null)
        {
            SimpleBattleAnimator.Instance.ResetToInitialState();
            Debug.Log("[Server] SimpleBattleAnimator reseteado correctamente");
        }
        else
        {
            Debug.LogWarning("[Server] SimpleBattleAnimator.Instance es null - no se puede resetear");
        }
    }

    [ClientRpc]
    void RpcPrepareForCompleteReset()
    {
        Debug.Log("[Client] Preparando para reset completo...");

        // Limpiar cualquier estado local del cliente
        CleanupClientState();
    }

    [ClientRpc]
    void RpcCompleteReset()
    {
        Debug.Log("[Client] Reset completo finalizado. Juego vuelto al estado inicial.");

        // Reinicializar completamente el estado del cliente
        ResetClientGameState();
    }

    [ClientRpc]
    void RpcDestroyLocalAuxiliaries()
    {
        Debug.Log("[Client] Destruyendo auxiliares locales...");

        try
        {
            // Buscar todos los objetos que puedan ser auxiliares
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            List<GameObject> auxiliaresToDestroy = new List<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                if (obj != null && (
                    obj.CompareTag("Auxiliary") ||
                    obj.CompareTag("Card") ||
                    obj.name.ToLower().Contains("auxiliary") ||
                    obj.name.ToLower().Contains("aux") ||
                    obj.name.ToLower().Contains("helper") ||
                    obj.name.ToLower().Contains("support") ||
                    obj.name.ToLower().Contains("card")))
                {
                    auxiliaresToDestroy.Add(obj);
                }
            }

            int destroyedCount = 0;
            foreach (GameObject aux in auxiliaresToDestroy)
            {
                if (aux != null)
                {
                    Debug.Log($"[Client] Destruyendo auxiliar local: {aux.name} (Tag: {aux.tag})");
                    Destroy(aux);
                    destroyedCount++;
                }
            }

            Debug.Log($"[Client] Destruidos {destroyedCount} auxiliares/cartas locales");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Client] Error durante destrucción de auxiliares locales: {e.Message}");
        }
    }

    void ResetClientGameState()
    {
        // Resetear cualquier manager o estado del juego en el cliente
        try
        {
            // NUEVO: Resetear específicamente el estado de las cartas en el cliente
            PlayerManager[] allPlayers = GameObject.FindObjectsOfType<PlayerManager>();
            foreach (PlayerManager player in allPlayers)
            {
                if (player != null && player.isLocalPlayer)
                {
                    // Resetear el estado hasPlayedCard específicamente para el jugador local
                    Debug.Log($"[Client] Reseteando estado local para jugador {player.netId}");

                    // Forzar refresh de las áreas
                    var _ = player.PlayerArea;
                    var __ = player.EnemyArea;
                    var ___ = player.DropZone;
                }
            }

            // Si tienes managers de UI, resetéalos aquí para volver al estado inicial
            // UIManager.Instance?.ResetToInitialState();
            // SoundManager.Instance?.StopAllSounds();
            // GameUIManager.Instance?.ShowInitialScreen();

            Debug.Log("[Client] Estado del cliente reseteado al estado inicial");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Client] Error durante reset del cliente: {e.Message}");
        }
    }

    [Server]
    private void DestroyAllGameObjects()
    {
        Debug.Log("[Server] Destruyendo TODOS los objetos de juego...");

        // Crear lista de objetos a destruir para evitar modificar colección durante iteración
        List<GameObject> objectsToDestroy = new List<GameObject>();

        // Método 1: Destruir objetos networkeados
        foreach (var kvp in NetworkServer.spawned.ToList()) // ToList() crea una copia
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
            {
                GameObject obj = kvp.Value.gameObject;

                // Destruir cartas, auxiliares y cualquier otro objeto de juego
                if (obj.CompareTag("Card") || obj.CompareTag("Auxiliary") ||
                    obj.name.Contains("Card") || obj.name.Contains("Auxiliary") ||
                    obj.name.ToLower().Contains("card") || obj.name.ToLower().Contains("auxiliary") ||
                    obj.name.ToLower().Contains("aux"))
                {
                    objectsToDestroy.Add(obj);
                }
            }
        }

        // Método 2: Buscar objetos por tag (por si algunos no están en spawned)
        GameObject[] allCards = GameObject.FindGameObjectsWithTag("Card");
        GameObject[] allAux = GameObject.FindGameObjectsWithTag("Auxiliary");

        objectsToDestroy.AddRange(allCards);
        objectsToDestroy.AddRange(allAux);

        // Método 3: Buscar por nombres alternativos comunes para auxiliares
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj != null && (
                obj.name.ToLower().Contains("auxiliary") ||
                obj.name.ToLower().Contains("aux") ||
                obj.name.ToLower().Contains("helper") ||
                obj.name.ToLower().Contains("support") ||
                // Agrega aquí otros nombres que puedan tener tus auxiliares
                obj.tag == "Auxiliary" || obj.tag == "Card"))
            {
                objectsToDestroy.Add(obj);
            }
        }

        // Eliminar duplicados
        objectsToDestroy = objectsToDestroy.Distinct().ToList();

        Debug.Log($"[Server] Encontrados {objectsToDestroy.Count} objetos para destruir");

        // Destruir todos los objetos
        int destroyedCount = 0;
        foreach (GameObject obj in objectsToDestroy)
        {
            if (obj != null)
            {
                Debug.Log($"[Server] Destruyendo objeto: {obj.name} (Tag: {obj.tag})");

                // Si es un objeto networkeado, usar NetworkServer.Destroy
                if (obj.GetComponent<NetworkIdentity>() != null)
                {
                    NetworkServer.Destroy(obj);
                    destroyedCount++;
                }
                else
                {
                    // Si no es networkeado, destruir normalmente
                    Destroy(obj);
                    destroyedCount++;
                }
            }
        }

        Debug.Log($"[Server] Destruidos {destroyedCount} objetos de juego");

        // Notificar a los clientes para que también limpien auxiliares locales
        RpcDestroyLocalAuxiliaries();
    }

    [Server]
    private void ResetAllPlayersCompletely()
    {
        Debug.Log("[Server] Reseteando completamente todos los jugadores al estado inicial...");

        PlayerManager[] allPlayers = GameObject.FindObjectsOfType<PlayerManager>();

        foreach (PlayerManager player in allPlayers)
        {
            if (player != null)
            {
                Debug.Log($"[Server] Reseteando al estado inicial al jugador {player.netId}");

                // CRÍTICO: Resetear todos los estados del jugador al estado inicial
                player.CmdResetHasPlayedCard();

                // NUEVO: Resetear también el currentPlayedCardNetId
                if (player.GetCurrentPlayedCardNetId() != 0)
                {
                    player.CmdRemovePlayedCard();
                }

                // Limpiar el área del jugador de cualquier objeto restante
                if (player.PlayerArea != null)
                {
                    // Destruir cualquier hijo que sea una carta o auxiliar
                    List<Transform> childrenToDestroy = new List<Transform>();
                    foreach (Transform child in player.PlayerArea.transform)
                    {
                        if (child.CompareTag("Card") || child.CompareTag("Auxiliary"))
                        {
                            childrenToDestroy.Add(child);
                        }
                    }

                    foreach (Transform child in childrenToDestroy)
                    {
                        if (child != null)
                        {
                            Debug.Log($"[Server] Limpiando {child.name} del área del jugador {player.netId}");
                            if (child.GetComponent<NetworkIdentity>() != null)
                            {
                                NetworkServer.Destroy(child.gameObject);
                            }
                            else
                            {
                                Destroy(child.gameObject);
                            }
                        }
                    }
                }

                // NUEVO: Notificar al cliente específico que debe resetear su estado
                player.RpcResetPlayerStateAfterGameReset();
            }
        }

        Debug.Log("[Server] Todos los jugadores han sido reseteados al estado inicial");
    }

    // Método para resetear el estado del juego manualmente (útil para testing)
    [Server]
    public void ResetGameState()
    {
        if (!isServer) return;

        Debug.Log("[Server] Reseteando estado del juego manualmente...");

        gameHasStarted = false;
        gameHasEnded = false;

        // Resetear victorias
        PlayerVictoryTracker.ResetVictories();

        // Hacer reset completo al estado inicial
        StartCoroutine(CompleteGameReset());
    }

    [ClientRpc]
    void RpcNotifyGameReset()
    {
        Debug.Log("[Client] El estado del juego ha sido reseteado al estado inicial.");
        ResetClientGameState();
    }

    public bool IsGameStarted => gameHasStarted;
    public bool IsGameEnded => gameHasEnded;

    // Método de utilidad para verificar si el juego está en progreso
    public bool IsGameInProgress => gameHasStarted && !gameHasEnded;

    void CleanupClientState()
    {
        Debug.Log("[Client] Preparando limpieza de estado local...");
        // Esta función ahora se llama desde RpcPrepareForCompleteReset
        // La limpieza real se hace en RpcDestroyLocalAuxiliaries
    }

    [Server]
    IEnumerator CompleteGameReset()
    {
        Debug.Log("[Server] Iniciando reset completo del juego...");

        // Esperar unos segundos para que los jugadores vean el resultado
        yield return new WaitForSeconds(3f);

        // 1. CRÍTICO: Resetear estados de jugadores PRIMERO
        yield return StartCoroutine(ResetAllPlayersStateCoroutine());

        // 2. Resetear todas las victorias
        PlayerVictoryTracker.ResetVictories();
        Debug.Log("[Server] Victorias reseteadas");

        // 3. Resetear el RoundManager
        ResetRoundManager();

        // 4. Notificar a todos los clientes que van a hacer reset completo
        RpcPrepareForCompleteReset();

        // 5. Esperar que los clientes se preparen
        yield return new WaitForSeconds(1f);

        // 6. Destruir TODOS los objetos de juego
        DestroyAllGameObjects();

        // 7. Esperar que las destrucciones se procesen
        yield return new WaitForSeconds(1f);

        // 8. Resetear el animator
        ResetAnimator();

        // 9. FORZAR sincronización final de estados
        yield return StartCoroutine(ForceFinalStateSync());

        // 10. Notificar que el reset está completo
        RpcCompleteReset();

        Debug.Log("[Server] Reset completo finalizado. Juego vuelto al estado inicial.");
    }

    [Server]
    IEnumerator ResetAllPlayersStateCoroutine()
    {
        Debug.Log("[Server] Iniciando reset de estado de todos los jugadores...");

        PlayerManager[] allPlayers = GameObject.FindObjectsOfType<PlayerManager>();

        foreach (PlayerManager player in allPlayers)
        {
            if (player != null)
            {
                Debug.Log($"[Server] Reseteando completamente al jugador {player.netId}");

                // CRÍTICO: Resetear los estados inmediatamente
                player.ForceResetAllStates();

                // Esperar un frame para que se procese
                yield return null;
            }
        }

        // Esperar un poco más para asegurar sincronización
        yield return new WaitForSeconds(0.5f);

        Debug.Log("[Server] Estados de todos los jugadores reseteados");
    }

    // NUEVO: Método para forzar sincronización final
    [Server]
    IEnumerator ForceFinalStateSync()
    {
        Debug.Log("[Server] Forzando sincronización final de estados...");

        // Resetear una vez más los estados críticos
        PlayerManager[] allPlayers = GameObject.FindObjectsOfType<PlayerManager>();
        foreach (PlayerManager player in allPlayers)
        {
            if (player != null)
            {
                player.ForceFinalStateReset();
            }
        }

        yield return new WaitForSeconds(0.5f);

        // Enviar RPC de sincronización final
        RpcForceFinalSync();

        yield return new WaitForSeconds(0.5f);
    }

    [ClientRpc]
    void RpcForceFinalSync()
    {
        Debug.Log("[Client] Aplicando sincronización final...");

        // Forzar que todos los jugadores actualicen su estado local
        PlayerManager[] allPlayers = GameObject.FindObjectsOfType<PlayerManager>();
        foreach (PlayerManager player in allPlayers)
        {
            if (player != null)
            {
                player.ForceClientStateUpdate();
            }
        }
    }
}