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
        base.OnStartClient();

        cards.Add(Card);
        cards.Add(Card1);
        Debug.Log(cards);
    }

    [Command]
    public void CmdDealCards()
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject card = Instantiate(cards[Random.Range(0,cards.Count)], new Vector2(0, 0), Quaternion.identity);
            NetworkServer.Spawn(card, connectionToClient);
            RpcShowCard(card, "Dealt");
        }
    }

    public void PlayCard(GameObject card)
    {
        CmdPlayCard(card);
    }

    [Command]
    void CmdPlayCard(GameObject card)
    {
        RpcShowCard(card, "Played");

        if(isServer)
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
        if(type == "Dealt")
        {
            if(isOwned)
            {
                card.transform.SetParent(PlayerArea.transform, false);
            }
            else
            {
                card.transform.SetParent(EnemyArea.transform, false);
                card.GetComponent<CardFlipper>().Flip();
            }
        }
        else if(type == "Played")
        {
            card.transform.SetParent(DropZone.transform, false);
            if(!isOwned)
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
        card.GetComponent<incrementClick>().NumberOfClicks++;
        Debug.Log("This card has been clicked" + card.GetComponent<incrementClick>().NumberOfClicks+"times!");
    }
}
