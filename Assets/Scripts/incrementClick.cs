using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

//Usos: Health bars or damage, mana... En nuestro caso tal vez sea util para almacenar las victorias por ronda :3

public class incrementClick : NetworkBehaviour
{
    public PlayerManager PlayerManager;

    [SyncVar]
    public int NumberOfClicks = 0;

    public void IncrementClicks()
    {
        NetworkIdentity networkIdentity = NetworkClient.connection.identity;
        PlayerManager = networkIdentity.GetComponent<PlayerManager>();
        PlayerManager.CmdIncrementClick(gameObject);
    }
}
