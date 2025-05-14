using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RoundManager : NetworkBehaviour
{
    public float roundTime = 15f;
    private float timeRemaining;
    private bool isRoundActive = false;
    private int currentRound = 0;
    private List<PlayerManager> players = new List<PlayerManager>();
    private float lastLogTime = 0f;
    private float logInterval = 1f;
    private bool gameStarted = false;

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
        RefreshPlayersList();
        // Ya no iniciamos el juego automáticamente, esperamos a que los jugadores estén listos
    }

    [Server]
    void RefreshPlayersList()
    {
        players.Clear();
        players.AddRange(FindObjectsOfType<PlayerManager>());
        Debug.Log($"Jugadores encontrados: {players.Count}");
    }

    void Update()
    {
        if (!isServer || !isRoundActive) return;

        timeRemaining -= Time.deltaTime;

        // Solo mostrar el log cada segundo
        if (Time.time - lastLogTime >= logInterval)
        {
            lastLogTime = Time.time;
            Debug.Log($"Round {currentRound} - Time remaining: {timeRemaining:F2} seconds.");
        }

        if (timeRemaining <= 0f)
        {
            EndRound();
        }
    }

    // Método público para iniciar el juego cuando todos los jugadores estén listos
    [Server]
    public void StartGame()
    {
        if (!gameStarted)
        {
            gameStarted = true;
            Debug.Log("¡El juego ha comenzado oficialmente!");
            StartRound();
        }
        else
        {
            Debug.Log("El juego ya está en curso.");
        }
    }

    [Server]
    void StartRound()
    {
        currentRound++;
        timeRemaining = roundTime;
        isRoundActive = true;
        Debug.Log($"Round {currentRound} started with {timeRemaining} seconds.");

        // Para rondas posteriores a la primera, asignar nuevas cartas
        if (currentRound > 1)
        {
            foreach (var player in players)
            {
                if (player != null)
                {
                    // Primero asignar una nueva carta
                    player.CmdAssignNewCard();

                    // Luego resetear el estado de juego
                    player.CmdResetHasPlayedCard();
                }
            }
        }

        RpcNotifyPlayersRoundStart(roundTime);
    }

    [Server]
    void EndRound()
    {
        isRoundActive = false;
        Debug.Log($"Round {currentRound} ended.");

        // Forzar jugar carta a los jugadores que no lo hicieron
        foreach (var player in players)
        {
            if (player != null && !player.HasPlayedCard)
            {
                Debug.Log($"Jugador {player.netId} no jugó. Jugando carta automática...");
                player.CmdPlayRandomCard();
            }
        }

        // Procesar las cartas jugadas y preparar nueva ronda
        StartCoroutine(ProcessPlayedCards());
    }

    [Server]
    IEnumerator ProcessPlayedCards()
    {
        Debug.Log("Procesando cartas jugadas y preparando nueva ronda...");

        // Esperar un momento para que todas las acciones de juego se completen
        yield return new WaitForSeconds(1f);

        // Eliminar las cartas jugadas
        foreach (var player in players)
        {
            if (player != null)
            {
                Debug.Log($"Eliminando carta jugada del jugador {player.netId}");
                player.CmdRemovePlayedCard();
            }
        }

        // Esperar otro momento para que se complete la eliminación
        yield return new WaitForSeconds(1f);

        // Iniciar nueva ronda
        StartRound();
    }

    [ClientRpc]
    void RpcNotifyPlayersRoundStart(float duration)
    {
        Debug.Log($"¡Nueva ronda iniciada! Tienes {duration} segundos para jugar.");
    }

    public float GetTimeRemaining()
    {
        return timeRemaining;
    }
}