using UnityEngine;
using Mirror;

public class GameStarter : NetworkBehaviour
{
    // Variable estática para tracking del estado del juego
    private static bool gameHasStarted = false;

    // Este método debe ser llamado después de que un jugador reciba sus cartas
    // por ejemplo, al final del método CmdDealCards() o desde el evento del botón
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
        Debug.Log("Verificando si todos los jugadores están listos...");
        // Buscar todos los jugadores en la escena
        PlayerManager[] allPlayers = GameObject.FindObjectsOfType<PlayerManager>();
        // Verificar si todos los jugadores tienen al menos una carta
        bool allReady = true;
        foreach (PlayerManager player in allPlayers)
        {
            if (!PlayerHasCards(player))
            {
                Debug.Log($"Jugador {player.netId} aún no está listo.");
                allReady = false;
                break;
            }
        }
        // Si todos están listos, iniciar el juego
        if (allReady)
        {
            Debug.Log("Todos los jugadores están listos. Iniciando juego...");
            gameHasStarted = true;
            // Buscar el RoundManager y comenzar el juego
            RoundManager roundManager = GameObject.FindObjectOfType<RoundManager>();
            if (roundManager != null)
            {
                roundManager.StartGame();
            }
            else
            {
                Debug.LogError("No se encontró el RoundManager en la escena.");
            }
        }
        else
        {
            Debug.Log("Aún no están todos los jugadores listos.");
        }
    }

    // Método para verificar si un jugador ya tiene cartas repartidas
    private bool PlayerHasCards(PlayerManager player)
    {
        // Verificar si el jugador tiene cartas en su área
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

    // Método para resetear el estado del juego (útil para partidas nuevas)
    public static void ResetGameState()
    {
        gameHasStarted = false;
    }
}