using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RoundManager : NetworkBehaviour
{
    [SyncVar]
    private bool timeIsZero = false;
    //
    public PlayerManager player1;
    public PlayerManager player2;
    //

    public float roundTime = 15f;
    [SyncVar]
    public float timeRemaining;

    [SyncVar]
    private bool isRoundActive = false;

    [SyncVar]
    private int currentRound = 0;

    [SyncVar]
    private bool gameStarted = false;

    private float lastLogTime = 0f;
    private float logInterval = 1f;

    void Start()
    {
        if (isServer)
        {
            // Esperar un momento para que los jugadores se conecten
            StartCoroutine(DelayedStart());

            PlayerManager[] players = FindObjectsOfType<PlayerManager>();

            if (players.Length >= 2)
            {
                player1 = players[0];
                player2 = players[1];
            }
        }
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1f);
        // Ya no iniciamos el juego automáticamente, esperamos a que los jugadores estén listos
        Debug.Log("[Server] RoundManager inicializado. Esperando inicio del juego...");
    }

    // NUEVO: Método para resetear el RoundManager al estado inicial
    [Server]
    public void ResetToInitialState()
    {
        if (!isServer) return;

        Debug.Log("[Server] Reseteando RoundManager al estado inicial...");

        // Resetear todas las variables de estado
        gameStarted = false;
        isRoundActive = false;
        currentRound = 0;
        timeRemaining = 0f;
        lastLogTime = 0f;

        // Parar cualquier corrutina en ejecución
        StopAllCoroutines();

        // Reinicializar referencias de jugadores
        PlayerManager[] players = FindObjectsOfType<PlayerManager>();
        if (players.Length >= 2)
        {
            player1 = players[0];
            player2 = players[1];
        }
        else
        {
            player1 = null;
            player2 = null;
        }

        Debug.Log("[Server] RoundManager reseteado correctamente al estado inicial");
    }

    void Update()
    {
        if (!isServer || !isRoundActive) return;

        timeRemaining -= Time.deltaTime;

        // Solo mostrar el log cada segundo
        if (Time.time - lastLogTime >= logInterval)
        {
            lastLogTime = Time.time;
            //Debug.Log($"[Server] Round {currentRound} - Time remaining: {timeRemaining:F2} seconds.");
        }

        if (timeRemaining <= 0f)
        {
            EndRound();
        }
    }

    // Método para que los clientes obtengan el tiempo restante
    public float GetTimeRemaining()
    {
        return timeRemaining;
    }

    // Método para iniciar el juego cuando todos los jugadores estén listos
    [Server]
    public void StartGame()
    {
        if (!isServer) return;

        if (!gameStarted)
        {
            gameStarted = true;
            Debug.Log("[Server] ¡El juego ha comenzado oficialmente!");

            // NUEVO: Marcar en el SimpleBattleAnimator que el juego ha comenzado
            if (SimpleBattleAnimator.Instance != null)
            {
                SimpleBattleAnimator.Instance.SetGameStarted();
                Debug.Log("[Server] Animator marcado como juego iniciado");
            }
            else
            {
                Debug.LogWarning("[Server] SimpleBattleAnimator.Instance es null - no se puede marcar como iniciado");
            }

            // Notificar a todos los clientes
            RpcNotifyGameStarted();

            // Iniciar la primera ronda
            StartRound();
        }
        else
        {
            Debug.Log("[Server] El juego ya está en curso.");
        }
    }

    [ClientRpc]
    void RpcNotifyGameStarted()
    {
        Debug.Log("[Client] ¡El juego ha comenzado oficialmente!");
    }

    // Modifica tu método StartRound() en RoundManager para incluir esta verificación al inicio:

    [Server]
    void StartRound()
    {
        if (!isServer) return;

        // NUEVO: Verificar si el juego ha terminado antes de iniciar una nueva ronda
        GameStarter gameStarter = GameObject.FindObjectOfType<GameStarter>();
        if (gameStarter != null && gameStarter.IsGameEnded)
        {
            Debug.Log("[Server] No se puede iniciar nueva ronda - el juego ha terminado");
            return;
        }

        currentRound++;
        timeRemaining = roundTime;
        isRoundActive = true;
        //
        timeIsZero = false; // NUEVO: Resetear el estado de tiempo cero

        Debug.Log($"[Server] Round {currentRound} started with {timeRemaining} seconds.");

        // CORREGIDO: Solo iniciar animación de espera si el juego ha comenzado oficialmente
        if (gameStarted && SimpleBattleAnimator.Instance != null)
        {
            SimpleBattleAnimator.Instance.StartWaitingAnimation();
            Debug.Log("[Server] Animación de espera iniciada para la ronda");
        }
        else if (!gameStarted)
        {
            Debug.Log("[Server] Juego no ha comenzado oficialmente, saltando animación de espera");
        }
        else
        {
            Debug.LogWarning("[Server] SimpleBattleAnimator.Instance es null - no se puede iniciar animación de espera");
        }

        // Para rondas posteriores a la primera, asignar nuevas cartas
        if (currentRound > 1)
        {
            // Buscar todos los jugadores
            PlayerManager[] players = FindObjectsOfType<PlayerManager>();

            foreach (var player in players)
            {
                if (player != null)
                {
                    // Primero resetear el estado de jugada
                    player.RpcResetCardPlay();

                    // Luego asignar nueva carta
                    player.CmdAssignNewCard();
                }
            }
        }

        // Notificar a todos los clientes del inicio de ronda
        RpcNotifyRoundStart(currentRound, roundTime);
    }

    [ClientRpc]
    void RpcNotifyRoundStart(int round, float time)
    {
        Debug.Log($"[Client] Round {round} ha comenzado con {time} segundos.");
    }

    [Server]
    void EndRound()
    {
        if (!isServer) return;

        isRoundActive = false;
        timeIsZero = true; // NUEVO: Marcar que el tiempo llegó a cero
        Debug.Log($"[Server] Round {currentRound} ended.");

        // NUEVO: Notificar que el tiempo llegó a cero para hacer flip a las cartas
        RpcNotifyTimeZero();

        RpcNotifyRoundEnded(currentRound);

        PlayerManager[] players = FindObjectsOfType<PlayerManager>();

        foreach (var player in players)
        {
            if (player != null && !player.HasPlayedCard)
            {
                Debug.Log($"[Server] Jugador {player.netId} no jugó. Jugando carta automática...");
                player.PlayRandomCard();
            }
        }

        ResolveRound();
        StartCoroutine(ProcessPlayedCards());
    }

    // NUEVO: RPC para notificar cuando el tiempo llega a cero
    // NUEVO: RPC para notificar cuando el tiempo llega a cero - CORREGIDO
    [ClientRpc]
    void RpcNotifyTimeZero()
    {
        Debug.Log("[Client] Tiempo llegó a cero - preparando flip de cartas en DropZone");

        // Buscar todas las cartas en el DropZone y hacer flip solo a las del oponente
        GameObject dropZone = GameObject.Find("Limite");
        if (dropZone != null)
        {
            foreach (Transform child in dropZone.transform)
            {
                if (child.CompareTag("Card"))
                {
                    NetworkIdentity cardNetId = child.GetComponent<NetworkIdentity>();
                    if (cardNetId != null)
                    {
                        // CORREGIDO: Usar isOwned directamente desde NetworkIdentity
                        bool isMyCard = cardNetId.isOwned;

                        // Solo hacer flip si NO es mi carta
                        if (!isMyCard)
                        {
                            CardFlipper flipper = child.GetComponent<CardFlipper>();
                            if (flipper != null)
                            {
                                flipper.Flip();
                                Debug.Log($"[Client] Flip aplicado a carta del oponente {child.name} en DropZone");
                            }
                        }
                        else
                        {
                            Debug.Log($"[Client] Carta propia {child.name} en DropZone - NO se hace flip");
                        }
                    }
                }
            }
        }
    }

    [ClientRpc]
    void RpcNotifyRoundEnded(int round)
    {
        Debug.Log($"[Client] Round {round} ha terminado.");
    }

    [Server]
    IEnumerator ProcessPlayedCards()
    {
        Debug.Log("[Server] Procesando cartas jugadas y preparando nueva ronda...");

        // Esperar un momento para que todas las acciones de juego se completen
        yield return new WaitForSeconds(1f);

        // Eliminar las cartas jugadas de todos los jugadores
        PlayerManager[] players = FindObjectsOfType<PlayerManager>();

        foreach (var player in players)
        {
            if (player != null)
            {
                Debug.Log($"[Server] Eliminando carta jugada del jugador {player.netId}");

                //Paras la conexion explicitamente¿
                NetworkConnectionToClient conn = player.GetComponent<NetworkIdentity>().connectionToClient;
                if (conn != null)
                {
                    player.CmdRemovePlayedCard(conn);
                }
                else
                {
                    Debug.LogError($"[Server] No se pudo obtener la conexión del jugador {player.netId}");
                }
            }
        }

        // Esperar otro momento para que se complete la eliminación
        yield return new WaitForSeconds(1f);

        // Iniciar nueva ronda
        StartRound();
    }

    // Añadido con animaciones simples

    [Server]
    public void ResolveRound()
    {
        PlayerManager[] players = FindObjectsOfType<PlayerManager>();
        if (players.Length < 2)
        {
            Debug.LogWarning("No hay suficientes jugadores conectados para resolver la ronda.");
            return;
        }

        PlayerManager resolvedPlayer1 = players[0];
        PlayerManager resolvedPlayer2 = players[1];

        uint netId1 = resolvedPlayer1.GetCurrentPlayedCardNetId();
        uint netId2 = resolvedPlayer2.GetCurrentPlayedCardNetId();

        Debug.Log($"Resolviendo ronda con cartas NetId: {netId1} y {netId2}");

        if (!NetworkServer.spawned.TryGetValue(netId1, out NetworkIdentity card1NetId) ||
            !NetworkServer.spawned.TryGetValue(netId2, out NetworkIdentity card2NetId))
        {
            Debug.LogWarning("No se encontraron las cartas jugadas.");
            return;
        }

        CardData cardData1 = card1NetId.GetComponent<CardData>();
        CardData cardData2 = card2NetId.GetComponent<CardData>();

        if (cardData1 == null || cardData2 == null)
        {
            Debug.LogWarning("No se pudo encontrar CardData en alguna de las cartas.");
            return;
        }

        var result = CardBattleLogic.CompareCards(cardData1, cardData2);

        // Determinar qué elemento ganó para la animación
        CardData.ElementType winnerElement = CardData.ElementType.Boton; // valor por defecto

        switch (result)
        {
            case CardBattleLogic.BattleResult.WinA:
                Debug.Log($"Jugador {resolvedPlayer1.netId} gana la ronda.");
                PlayerVictoryTracker.AddVictory(resolvedPlayer1);
                winnerElement = cardData1.element;
                break;

            case CardBattleLogic.BattleResult.WinB:
                Debug.Log($"Jugador {resolvedPlayer2.netId} gana la ronda.");
                PlayerVictoryTracker.AddVictory(resolvedPlayer2);
                winnerElement = cardData2.element;
                break;

            case CardBattleLogic.BattleResult.Draw:
                Debug.Log("Empate en la ronda.");
                break;
        }

        // CORREGIDO: Solo reproducir animaciones si el juego ha comenzado oficialmente
        if (gameStarted && SimpleBattleAnimator.Instance != null)
        {
            SimpleBattleAnimator.Instance.PlayBattleAnimations(winnerElement, result);
            Debug.Log($"[Server] Animación de batalla iniciada: Elemento={winnerElement}, Resultado={result}");
        }
        else if (!gameStarted)
        {
            Debug.Log("[Server] Juego no ha comenzado oficialmente, saltando animación de batalla");
        }
        else
        {
            Debug.LogWarning("[Server] SimpleBattleAnimator.Instance es null - no se puede reproducir animación de batalla");
        }

        CheckForGameVictory();
    }

    // Reemplaza tu método CheckForGameVictory() en RoundManager con este:

    private void CheckForGameVictory()
    {
        PlayerManager[] players = FindObjectsOfType<PlayerManager>();
        if (players.Length < 2)
        {
            Debug.LogWarning("No hay suficientes jugadores para comprobar victoria.");
            return;
        }

        PlayerManager winner = null;

        if (PlayerVictoryTracker.HasPlayerWon(players[0]))
        {
            Debug.Log($"Jugador {players[0].netId} gana el juego!");
            winner = players[0];
        }
        else if (PlayerVictoryTracker.HasPlayerWon(players[1]))
        {
            Debug.Log($"Jugador {players[1].netId} gana el juego!");
            winner = players[1];
        }

        // Si hay un ganador, finalizar el juego
        if (winner != null)
        {
            // CORREGIDO: Solo mostrar animación si el juego ha comenzado oficialmente
            if (gameStarted && SimpleBattleAnimator.Instance != null)
            {
                SimpleBattleAnimator.Instance.ShowVictoryAnimation();
                Debug.Log("[Server] Animación de victoria del juego iniciada");
            }
            else if (!gameStarted)
            {
                Debug.Log("[Server] Juego no ha comenzado oficialmente, saltando animación de victoria");
            }
            else
            {
                Debug.LogWarning("[Server] SimpleBattleAnimator.Instance es null - no se puede mostrar animación de victoria");
            }

            // NUEVO: Finalizar el juego y preparar reset
            GameStarter gameStarter = GameObject.FindObjectOfType<GameStarter>();
            if (gameStarter != null)
            {
                gameStarter.EndGameWithWinner(winner);
            }
            else
            {
                Debug.LogError("[Server] No se encontró GameStarter - no se puede finalizar el juego correctamente");
            }

            // Detener las rondas
            isRoundActive = false;
            Debug.Log("[Server] Rondas detenidas debido a victoria del juego");
        }
    }
}