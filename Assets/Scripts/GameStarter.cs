using UnityEngine;
using Mirror;

public class GameStarter : NetworkBehaviour
{
    // Variable est�tica para tracking del estado del juego
    private static bool gameHasStarted = false;

    // Este m�todo debe ser llamado despu�s de que un jugador reciba sus cartas
    // por ejemplo, al final del m�todo CmdDealCards() o desde el evento del bot�n
    [Command(requiresAuthority = false)]
    public void CmdRequestGameStart(NetworkConnectionToClient conn = null)
    {
        if (gameHasStarted) return;
        // Verificar si todos los jugadores tienen cartas
        CheckAllPlayersReady();
    }

    [Server]
    private void CheckAllPlayersReady()
    {
        Debug.Log("Verificando si todos los jugadores est�n listos...");
        // Buscar todos los jugadores en la escena
        PlayerManager[] allPlayers = GameObject.FindObjectsOfType<PlayerManager>();
        // Verificar si todos los jugadores tienen al menos una carta
        bool allReady = true;
        foreach (PlayerManager player in allPlayers)
        {
            if (!PlayerHasCards(player))
            {
                Debug.Log($"Jugador {player.netId} a�n no est� listo.");
                allReady = false;
                break;
            }
        }
        // Si todos est�n listos, iniciar el juego
        if (allReady)
        {
            Debug.Log("Todos los jugadores est�n listos. Iniciando juego...");
            gameHasStarted = true;
            // Buscar el RoundManager y comenzar el juego
            RoundManager roundManager = GameObject.FindObjectOfType<RoundManager>();
            if (roundManager != null)
            {
                roundManager.StartGame();
            }
            else
            {
                Debug.LogError("No se encontr� el RoundManager en la escena.");
            }
        }
        else
        {
            Debug.Log("A�n no est�n todos los jugadores listos.");
        }
    }

    // M�todo para verificar si un jugador ya tiene cartas repartidas
    private bool PlayerHasCards(PlayerManager player)
    {
        // Verificar si el jugador tiene cartas en su �rea
        GameObject[] allCards = GameObject.FindGameObjectsWithTag("Card");
        foreach (GameObject card in allCards)
        {
            if (card.transform.parent == player.PlayerArea.transform)
            {
                return true;
            }
        }
        return false;
    }

    // M�todo para resetear el estado del juego (�til para partidas nuevas)
    public static void ResetGameState()
    {
        gameHasStarted = false;
    }
}