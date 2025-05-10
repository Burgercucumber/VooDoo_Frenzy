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
            card.transform.SetParent(PlayerArea.transform, false);
        }
    }
}
