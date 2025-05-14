using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerManager : NetworkBehaviour
{
    public GameObject Card;
    public GameObject Card1;
    // Estas son las variables para referenciar las áreas del juego
    [SerializeField] private GameObject playerArea;
    [SerializeField] private GameObject enemyArea;
    [SerializeField] private GameObject dropZone;

    // Propiedades para acceder a las áreas, con búsqueda automática si es necesario
    public GameObject PlayerArea
    {
        get
        {
            if (playerArea == null)
            {
                playerArea = GameObject.Find("AreaJugador");
            }
            return playerArea;
        }
    }

    public GameObject EnemyArea
    {
        get
        {
            if (enemyArea == null)
            {
                enemyArea = GameObject.Find("AreaEnemigo");
            }
            return enemyArea;
        }
    }

    public GameObject DropZone
    {
        get
        {
            if (dropZone == null)
            {
                dropZone = GameObject.Find("Limite");
            }
            return dropZone;
        }
    }

    [SyncVar(hook = nameof(OnPlayedCardChanged))]
    private bool hasPlayedCard = false;

    // Referencia sincronizada a la carta jugada
    [SyncVar]
    private NetworkIdentity playedCardIdentity;

    public bool HasPlayedCard => hasPlayedCard;

    // Propiedad para acceder a la carta jugada como GameObject
    public GameObject PlayedCard
    {
        get
        {
            if (playedCardIdentity != null)
            {
                return playedCardIdentity.gameObject;
            }
            return null;
        }
    }

    List<GameObject> cards = new List<GameObject>();

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Ya no necesitamos asignar las referencias aquí ya que usamos propiedades
        // que buscan los objetos cuando son necesarios
    }

    [Server]
    public override void OnStartServer()
    {
        base.OnStartServer();

        cards.Add(Card);
        cards.Add(Card1);
        Debug.Log("Cartas disponibles en el servidor: " + cards.Count);
    }

    [Command]
    public void CmdDealCards()
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject card = Instantiate(cards[Random.Range(0, cards.Count)], new Vector2(0, 0), Quaternion.identity);
            card.transform.SetParent(PlayerArea.transform, false);

            DragDrop drag = card.GetComponent<DragDrop>();
            drag.OwnerPlayerManager = this;

            NetworkServer.Spawn(card, connectionToClient);
            RpcShowCard(card, "Dealt");
        }

        // Después de repartir cartas, comprobamos si todos los jugadores están listos
        // para iniciar el juego
        GameStarter starter = GameObject.FindObjectOfType<GameStarter>();
        if (starter != null)
        {
            starter.CmdRequestGameStart(connectionToClient);
        }
        else
        {
            Debug.LogError("No se encontró GameStarter en la escena.");
        }

    }

    public void PlayCard(GameObject card)
    {
        if (!HasPlayedCard)
        {
            CmdPlayCard(card);
        }
        else
        {
            Debug.Log("Ya jugaste una carta, espera la siguiente ronda.");
        }
    }

    [Command]
    public void CmdPlayCard(GameObject card)
    {
        if (hasPlayedCard) return;

        hasPlayedCard = true;
        playedCardIdentity = card.GetComponent<NetworkIdentity>();

        // Verificación de la carta jugada
        Debug.Log("Carta jugada asignada: " + card.name);

        RpcShowCard(card, "Played");

        GameManager gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        gm.UpdateTurnsPlayed();
    }

    // Eliminar la carta jugada
    [Command]
    public void CmdRemovePlayedCard()
    {
        Debug.Log("CmdRemovePlayedCard llamada");

        if (playedCardIdentity != null)
        {
            GameObject cardToDestroy = playedCardIdentity.gameObject;
            Debug.Log("Carta jugada encontrada: " + cardToDestroy.name);

            // Aquí destruimos la carta
            NetworkServer.Destroy(cardToDestroy);
            playedCardIdentity = null;

            Debug.Log("Carta destruida correctamente.");
        }
        else
        {
            Debug.Log("No hay carta jugada para eliminar.");
        }
    }

    [Command]
    public void CmdPlayRandomCard()
    {
        if (hasPlayedCard) return;

        // Busca una carta en el área del jugador (la mano)
        GameObject[] handCards = GameObject.FindGameObjectsWithTag("Card");
        List<GameObject> playerCards = new List<GameObject>();

        foreach (GameObject card in handCards)
        {
            if (card.transform.parent == PlayerArea.transform)
            {
                playerCards.Add(card);
            }
        }

        if (playerCards.Count > 0)
        {
            // Tomar una carta aleatoria de la mano
            GameObject selectedCard = playerCards[Random.Range(0, playerCards.Count)];
            hasPlayedCard = true;
            playedCardIdentity = selectedCard.GetComponent<NetworkIdentity>();

            RpcShowCard(selectedCard, "Played");

            GameManager gm = GameObject.Find("GameManager").GetComponent<GameManager>();
            gm.UpdateTurnsPlayed();
            return;
        }

        // Si no tiene cartas, asignamos una aleatoria
        GameObject newCard = Instantiate(cards[Random.Range(0, cards.Count)], new Vector2(0, 0), Quaternion.identity);
        newCard.transform.SetParent(PlayerArea.transform, false);

        DragDrop drag = newCard.GetComponent<DragDrop>();
        drag.OwnerPlayerManager = this;

        NetworkServer.Spawn(newCard, connectionToClient);
        RpcShowCard(newCard, "Played");

        hasPlayedCard = true;
        playedCardIdentity = newCard.GetComponent<NetworkIdentity>();

        GameManager gmFallback = GameObject.Find("GameManager").GetComponent<GameManager>();
        gmFallback.UpdateTurnsPlayed();
    }

    [ClientRpc]
    public void RpcResetCardPlay()
    {
        Debug.Log("RpcResetCardPlay llamado - reseteando hasPlayedCard");
        hasPlayedCard = false;
    }

    [Command]
    public void CmdAssignNewCard()
    {
        Debug.Log("CmdAssignNewCard llamado - asignando nueva carta");
        GameObject prefab = cards[Random.Range(0, cards.Count)];
        GameObject newCard = Instantiate(prefab, new Vector2(0, 0), Quaternion.identity);
        newCard.transform.SetParent(PlayerArea.transform, false);

        DragDrop drag = newCard.GetComponent<DragDrop>();
        drag.OwnerPlayerManager = this;

        NetworkServer.Spawn(newCard, connectionToClient);
        RpcShowCard(newCard, "Dealt");
    }

    [ClientRpc]
    void RpcShowCard(GameObject card, string type)
    {
        if (type == "Dealt")
        {
            if (isOwned)
            {
                card.transform.SetParent(PlayerArea.transform, false);
            }
            else
            {
                card.transform.SetParent(EnemyArea.transform, false);
                card.GetComponent<CardFlipper>().Flip();
            }
        }
        else if (type == "Played")
        {
            card.transform.SetParent(DropZone.transform, false);
            if (!isOwned)
            {
                card.GetComponent<CardFlipper>().Flip();
            }
        }
    }

    void OnPlayedCardChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[SyncVar] hasPlayedCard cambiado: {oldValue} → {newValue}");
    }

    [Command]
    public void CmdTargetSelfCard() => TargetSelfCard();

    [Command]
    public void CmdTargetOtherCard(GameObject target)
    {
        NetworkIdentity opponentIdentity = target.GetComponent<NetworkIdentity>();
        TargetOtherCard(opponentIdentity.connectionToClient);
    }

    [TargetRpc]
    void TargetSelfCard() => Debug.Log("Targeted by self!");

    [TargetRpc]
    void TargetOtherCard(NetworkConnection target) => Debug.Log("Targeted by Other!");

    [Command]
    public void CmdIncrementClick(GameObject card)
    {
        RpcIncrementClick(card);
    }

    [ClientRpc]
    void RpcIncrementClick(GameObject card)
    {
        var click = card.GetComponent<incrementClick>();
        click.NumberOfClicks++;
        Debug.Log("This card has been clicked " + click.NumberOfClicks + " times!");
    }

    [Command]
    public void CmdResetHasPlayedCard()
    {
        hasPlayedCard = false;
        Debug.Log("hasPlayedCard ha sido reseteado a false");
    }
}