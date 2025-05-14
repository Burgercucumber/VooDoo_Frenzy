using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerManager : NetworkBehaviour
{
    public GameObject Card;
    public GameObject Card1;
    public GameObject PlayerArea;
    public GameObject EnemyArea;
    public GameObject DropZone;

    [SyncVar(hook = nameof(OnPlayedCardChanged))]
    private bool hasPlayedCard = false;

    public bool HasPlayedCard => hasPlayedCard;

    List<GameObject> cards = new List<GameObject>();

    public override void OnStartClient()
    {
        base.OnStartClient();

        PlayerArea = GameObject.Find("AreaJugador");
        EnemyArea = GameObject.Find("AreaEnemigo");
        DropZone = GameObject.Find("Limite");
    }

    [Server]
    public override void OnStartServer()
    {
        base.OnStartServer(); // ✅ Corrigido, antes llamaba a OnStartClient incorrectamente

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

            // Inyecta referencia del dueño (PlayerManager) a la carta
            DragDrop drag = card.GetComponent<DragDrop>();
            drag.OwnerPlayerManager = this;

            NetworkServer.Spawn(card, connectionToClient);
            RpcShowCard(card, "Dealt");
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
            Debug.Log("Ya jugaste una carta, espera para jugar otra.");
        }
    }

    [Command]
    void CmdPlayCard(GameObject card)
    {
        hasPlayedCard = true; // Esto se sincroniza a todos por ser SyncVar
        RpcShowCard(card, "Played");

        if (isServer)
        {
            UpdateTurnsPlayed();
        }
    }

    [Server]
    void UpdateTurnsPlayed()
    {
        GameManager gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        gm.UpdateTurnsPlayed();
        RpcLogToClients("Turns Played: " + gm.TurnsPlayed);
    }

    [ClientRpc]
    void RpcLogToClients(string message)
    {
        Debug.Log(message);
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

    [Command]
    public void CmdTargetSelfCard()
    {
        TargetSelfCard();
    }

    [Command]
    public void CmdTargetOtherCard(GameObject target)
    {
        NetworkIdentity opponentIdentity = target.GetComponent<NetworkIdentity>();
        TargetOtherCard(opponentIdentity.connectionToClient);
    }

    [TargetRpc]
    void TargetSelfCard()
    {
        Debug.Log("Targeted by self!");
    }

    [TargetRpc]
    void TargetOtherCard(NetworkConnection target)
    {
        Debug.Log("Targeted by Other!");
    }

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
    public void CmdResetCardPlay()
    {
        hasPlayedCard = false;
    }

    // Hook para actualizar lógica local si es necesario
    void OnPlayedCardChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[SyncVar] hasPlayedCard cambiado: {oldValue} → {newValue}");
    }
}
