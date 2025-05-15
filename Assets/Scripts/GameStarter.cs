using UnityEngine;
using Mirror;

public class GameStarter : NetworkBehaviour
{
    [SyncVar]
    private bool gameHasStarted = false;

    // Este m�todo debe ser llamado despu�s de que un jugador reciba sus cartas
    [Command(requiresAuthority = false)]
    public void CmdRequestGameStart(NetworkConnectionToClient conn = null)
    {
        if (!isServer) return;

        if (gameHasStarted)
        {
            Debug.Log("[Server] Intento de iniciar el juego, pero ya est� iniciado.");
            return;
        }

        // Verificar si todos los jugadores tienen cartas
        CheckAllPlayersReady();
    }

    [Server]
    private void CheckAllPlayersReady()
    {
        if (!isServer) return;

        Debug.Log("[Server] Verificando si todos los jugadores est�n listos...");

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
                Debug.LogError("[Server] Jugador o �rea de jugador es nula");
                allReady = false;
                break;
            }

            bool hasCards = false;

            // Contar directamente las cartas bajo el �rea del jugador
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
                Debug.Log($"[Server] Jugador {player.netId} a�n no est� listo (no tiene cartas).");
                allReady = false;
                break;
            }
        }

        // Si todos est�n listos, iniciar el juego
        if (allReady)
        {
            Debug.Log("[Server] Todos los jugadores est�n listos. Iniciando juego...");
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
                Debug.LogError("[Server] No se encontr� el RoundManager en la escena.");
            }
        }
        else
        {
            Debug.Log("[Server] A�n no est�n todos los jugadores listos.");
        }
    }

    [ClientRpc]
    void RpcNotifyGameStarted()
    {
        Debug.Log("[Client] El juego ha iniciado oficialmente.");
    }

    // M�todo para resetear el estado del juego (�til para partidas nuevas)
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

