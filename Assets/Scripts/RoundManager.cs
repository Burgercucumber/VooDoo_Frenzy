using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RoundManager : NetworkBehaviour
{
    public float roundTime = 15f;

    [SyncVar]
    private float timeRemaining;

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
        }
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1f);
        // Ya no iniciamos el juego automáticamente, esperamos a que los jugadores estén listos
        Debug.Log("[Server] RoundManager inicializado. Esperando inicio del juego...");
    }

    void Update()
    {
        if (!isServer || !isRoundActive) return;

        timeRemaining -= Time.deltaTime;

        // Solo mostrar el log cada segundo
        if (Time.time - lastLogTime >= logInterval)
        {
            lastLogTime = Time.time;
            Debug.Log($"[Server] Round {currentRound} - Time remaining: {timeRemaining:F2} seconds.");
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

    [Server]
    void StartRound()
    {
        if (!isServer) return;

        currentRound++;
        timeRemaining = roundTime;
        isRoundActive = true;
        Debug.Log($"[Server] Round {currentRound} started with {timeRemaining} seconds.");

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
        Debug.Log($"[Server] Round {currentRound} ended.");

        RpcNotifyRoundEnded(currentRound);

        PlayerManager[] players = FindObjectsOfType<PlayerManager>();

        foreach (var player in players)
        {
            if (player != null && !player.HasPlayedCard)
            {
                Debug.Log($"[Server] Jugador {player.netId} no jugó. Jugando carta automática...");

                player.PlayRandomCard(); // <- llamada directa en el servidor
            }
        }

        StartCoroutine(ProcessPlayedCards());
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
}

