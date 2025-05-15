using UnityEngine;
using Mirror;

public class GameStarter : NetworkBehaviour
{
    [SyncVar]
    private bool gameHasStarted = false;

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

    // Método para resetear el estado del juego (útil para partidas nuevas)
    [Server]
    public void ResetGameState()
    {
        if (!isServer) return;

        gameHasStarted = false;
        Debug.Log("[Server] Estado del juego reseteado.");

        // Notificar a todos los clientes
        RpcNotifyGameReset();
    }

    [ClientRpc]
    void RpcNotifyGameReset()
    {
        Debug.Log("[Client] El estado del juego ha sido reseteado.");
    }
}

