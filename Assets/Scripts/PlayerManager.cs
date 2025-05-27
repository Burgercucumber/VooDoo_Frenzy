using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerManager : NetworkBehaviour
{

    public GameObject BotonM1;
    public GameObject BotonM2;
    public GameObject BotonM3;
    public GameObject BotonM4;

    public GameObject BotonR1;
    public GameObject BotonR2;
    public GameObject BotonR3;
    public GameObject BotonR4;

    public GameObject BotonV1;
    public GameObject BotonV2;
    public GameObject BotonV3;
    public GameObject BotonV4;


    public GameObject Auxiliar;

    // Estas son las variables para referenciar las áreas del juego
    [SerializeField] private GameObject playerArea;
    [SerializeField] private GameObject enemyArea;
    [SerializeField] private GameObject dropZone;

    [SerializeField] private GameObject playerExtraArea;
    [SerializeField] private GameObject enemyExtraArea;

    // Propiedades para acceder a las áreas, con búsqueda automática si es necesario
    public GameObject PlayerArea
    {
        get
        {
            if (playerArea == null)
            {
                playerArea = GameObject.Find("AreaJugador");
                playerExtraArea = GameObject.Find("ExtraJugador");
                //playerArea = GameObject.Find("AreaJugador");
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
                enemyExtraArea = GameObject.Find("ExtraEnemigo");
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

    // Variable sincronizada para controlar si el jugador ha jugado una carta
    [SyncVar(hook = nameof(OnPlayedCardChanged))]
    private bool hasPlayedCard = false;

    public bool HasPlayedCard => hasPlayedCard;

    // Lista de cartas disponibles para repartir
    List<GameObject> cards = new List<GameObject>();
    List<GameObject> auxiliar = new List<GameObject>();//Intento

    public override void OnStartClient()
    {
        base.OnStartClient();

        // No es necesario buscar las referencias aquí, las propiedades lo hacen automáticamente
    }

    [Server]
    public override void OnStartServer()
    {
        base.OnStartServer();

        cards.Add(BotonM1);
        cards.Add(BotonM2);
        cards.Add(BotonM3);
        cards.Add(BotonM4);

        cards.Add(BotonR1);
        cards.Add(BotonR2);
        cards.Add(BotonR3);
        cards.Add(BotonR4);

        cards.Add(BotonV1);
        cards.Add(BotonV2);
        cards.Add(BotonV3);
        cards.Add(BotonV4);

        auxiliar.Add(Auxiliar);//

        Debug.Log("Cartas disponibles en el servidor: " + cards.Count);
    }

    // Método para repartir cartas iniciales
    [Command]
    public void CmdDealCards()
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject card = Instantiate(cards[Random.Range(0, cards.Count)], new Vector2(0, 0), Quaternion.identity);
            NetworkServer.Spawn(card, connectionToClient);

            // En vez de establecer el padre directamente, usamos RPC para asignarlo
            RpcSetCardParent(card, "Player");
        }

        List<GameObject> shuffledAux = new List<GameObject>(auxiliar);
        ShuffleList(shuffledAux);

        int auxToDeal = Mathf.Min(3, shuffledAux.Count);
        for (int i = 0; i < auxToDeal; i++)
        {
            GameObject aux = Instantiate(shuffledAux[i], Vector3.zero, Quaternion.identity);
            NetworkServer.Spawn(aux, connectionToClient);
            RpcSetAuxParent(aux, "Extra");
        }

        // Después de repartir cartas, comprobamos si todos los jugadores están listos
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

    // Método para jugar una carta
    public void PlayCard(GameObject card)
    {
        if (!hasPlayedCard)
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

        // Mover la carta a la zona de juego
        RpcSetCardParent(card, "DropZone");

        // Almacenar una referencia a la carta jugada en el servidor
        NetworkIdentity cardNetId = card.GetComponent<NetworkIdentity>();
        currentPlayedCardNetId = cardNetId.netId;

        Debug.Log($"[Server] Jugador {netId} jugó la carta {card.name} con netId {currentPlayedCardNetId}");

        // Actualizar el contador de turnos en el gestor del juego
        GameManager gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        if (gm != null)
        {
            gm.UpdateTurnsPlayed();
        }
    }

    // Variable para almacenar el NetworkId de la carta jugada actualmente
    [SyncVar]
    private uint currentPlayedCardNetId = 0;

    // Método para eliminar la carta jugada (ATENCION)
    [Command(requiresAuthority = false)]
    public void CmdRemovePlayedCard(NetworkConnectionToClient conn = null)
    {
        Debug.Log($"[Server] Removiendo carta jugada para jugador {netId}");

        if (currentPlayedCardNetId != 0)
        {
            // Buscar el objeto por su NetworkId
            if (NetworkServer.spawned.TryGetValue(currentPlayedCardNetId, out NetworkIdentity cardIdentity))
            {
                Debug.Log($"[Server] Encontrada carta jugada con netId {currentPlayedCardNetId}");

                // Notificar a los clientes antes de destruir
                RpcNotifyCardRemoval();

                // Destruir la carta en el servidor
                NetworkServer.Destroy(cardIdentity.gameObject);

                // Resetear la referencia
                currentPlayedCardNetId = 0;

                Debug.Log($"[Server] Carta jugada destruida correctamente");
            }
            else
            {
                Debug.LogWarning($"[Server] No se encontró la carta con netId {currentPlayedCardNetId}");
            }
        }
        else
        {
            Debug.Log($"[Server] No hay carta jugada para eliminar");
        }
    }

    [ClientRpc]
    private void RpcNotifyCardRemoval()
    {
        Debug.Log($"[Client] La carta jugada será eliminada");
    }

    // Método para jugar una carta aleatoria (para turnos automáticos) - CORREGIDO
    [Server]
    public void PlayRandomCard()
    {
        if (hasPlayedCard) return;

        Debug.Log($"[Server] Intentando jugar carta aleatoria para jugador {netId}");

        // Lista para almacenar las cartas específicas de este jugador
        List<GameObject> playerCards = new List<GameObject>();

        // Encontrar todas las cartas que pertenecen a este jugador
        foreach (var kvp in NetworkServer.spawned)
        {
            GameObject obj = kvp.Value.gameObject;

            // Verificar si es una carta (tiene el tag "Card")
            if (obj.CompareTag("Card"))
            {
                NetworkIdentity cardNetId = obj.GetComponent<NetworkIdentity>();

                // Si la carta pertenece a este jugador y no está en la zona de juego
                if (cardNetId.connectionToClient == connectionToClient)
                {
                    // Verificamos que la carta no esté ya en la zona de juego
                    Transform parent = obj.transform.parent;
                    if (parent != null && parent.name != "Limite" && parent.name != "DropZone")
                    {
                        playerCards.Add(obj);
                        Debug.Log($"[Server] Carta {obj.name} (netId: {cardNetId.netId}) encontrada para jugador {netId}");
                    }
                }
            }
        }

        Debug.Log($"[Server] Encontradas {playerCards.Count} cartas para jugador {netId}");

        if (playerCards.Count > 0)
        {
            // Seleccionar una carta aleatoria
            GameObject selectedCard = playerCards[Random.Range(0, playerCards.Count)];
            hasPlayedCard = true;

            Debug.Log($"[Server] Seleccionada carta {selectedCard.name} para jugar automáticamente");

            // Mover la carta a la zona de juego
            RpcSetCardParent(selectedCard, "DropZone");

            // Guardar la referencia de la carta jugada
            NetworkIdentity cardNetId = selectedCard.GetComponent<NetworkIdentity>();
            currentPlayedCardNetId = cardNetId.netId;

            Debug.Log($"[Server] Jugador {netId} jugó automáticamente la carta {selectedCard.name} con netId {currentPlayedCardNetId}");

            // Actualizar el contador de turnos en el gestor del juego
            GameManager gm = GameObject.Find("GameManager").GetComponent<GameManager>();
            if (gm != null)
            {
                gm.UpdateTurnsPlayed();
            }
            return;
        }

        // Si no se encontraron cartas, crear una nueva (como fallback)
        Debug.Log($"[Server] No se encontraron cartas para el jugador {netId}, creando nueva carta");
        GameObject newCard = Instantiate(cards[Random.Range(0, cards.Count)], new Vector2(0, 0), Quaternion.identity);
        NetworkServer.Spawn(newCard, connectionToClient);

        hasPlayedCard = true;
        RpcSetCardParent(newCard, "DropZone");

        NetworkIdentity newCardNetId = newCard.GetComponent<NetworkIdentity>();
        currentPlayedCardNetId = newCardNetId.netId;

        Debug.Log($"[Server] Jugador {netId} jugó una nueva carta automáticamente con netId {currentPlayedCardNetId}");

        GameManager gmFallback = GameObject.Find("GameManager").GetComponent<GameManager>();
        if (gmFallback != null)
        {
            gmFallback.UpdateTurnsPlayed();
        }
    }

    // Método para resetear el estado de juego después de cada ronda
    [ClientRpc]
    public void RpcResetCardPlay()
    {
        Debug.Log($"[Client] Reset del estado de juego para jugador {netId}");
        // No necesitamos cambiar hasPlayedCard aquí, ya que es una SyncVar

        if (isServer || isLocalPlayer)
        {
            CmdResetHasPlayedCard();
        }
    }

    // Método para asignar una nueva carta al jugador
    [Command(requiresAuthority = false)]
    public void CmdAssignNewCard(NetworkConnectionToClient conn = null)
    {
        Debug.Log($"[Server] Asignando nueva carta al jugador {netId}");

        GameObject prefab = cards[Random.Range(0, cards.Count)];
        GameObject newCard = Instantiate(prefab, new Vector2(0, 0), Quaternion.identity);

        NetworkServer.Spawn(newCard, connectionToClient);

        // Asignar la carta al área del jugador
        RpcSetCardParent(newCard, "Player");
    }

    [ClientRpc]
    public void RpcSetCardParent(GameObject card, string areaType)
    {
        if (card == null) return;

        Debug.Log($"[Client] Estableciendo padre para carta: {areaType}");

        switch (areaType)
        {
            case "Player":
                if (isOwned) // Si soy el dueño de esta carta
                {
                    card.transform.SetParent(PlayerArea.transform, false);
                }
                else // Si es una carta del oponente
                {
                    card.transform.SetParent(EnemyArea.transform, false);
                    // Voltear la carta para ocultar su contenido al jugador
                    CardFlipper flipper = card.GetComponent<CardFlipper>();
                    if (flipper != null)
                    {
                        flipper.Flip();
                    }
                }
                break;

            case "DropZone":
                card.transform.SetParent(DropZone.transform, false);
                // Siempre mostrar la carta al entrar en el área de juego
                if (!isOwned)
                {
                    CardFlipper flipper = card.GetComponent<CardFlipper>();
                    if (flipper != null)
                    {
                        flipper.Flip(); // Mostrar la carta, no importa su estado anterior
                    }
                }
                break;

        }
        // En PlayerManager.cs, dentro de RpcSetCardParent
        CardClickHandler clickHandler = card.GetComponent<CardClickHandler>();
        if (clickHandler == null)
        {
            card.AddComponent<CardClickHandler>();
        }
    }

    [ClientRpc]
    void RpcSetAuxParent(GameObject aux, string areaType)
    {
        if (aux == null) return;

        Debug.Log($"[Client] Estableciendo padre para auxiliar: {areaType}");

        // Asegurar que tenga el componente AuxiliaryCard si no lo tiene
        AuxiliaryCard auxCard = aux.GetComponent<AuxiliaryCard>();
        if (auxCard == null)
        {
            // Si el auxiliar no tiene AuxiliaryCard, añadirlo
            auxCard = aux.AddComponent<AuxiliaryCard>();

            // Configurar el tipo basado en el nombre del objeto o algún identificador
            if (aux.name.Contains("LevelUp") || aux.name.Contains("Nivel"))
            {
                auxCard.auxiliaryType = AuxiliaryCard.AuxiliaryType.LevelUp;
                auxCard.cardName = "Subir Nivel";
                auxCard.description = "Aumenta el nivel de una carta de 2 estrellas o menos";
            }
            else if (aux.name.Contains("Element") || aux.name.Contains("Elemento"))
            {
                auxCard.auxiliaryType = AuxiliaryCard.AuxiliaryType.ElementChange;
                auxCard.cardName = "Cambiar Elemento";
                auxCard.description = "Cambia el elemento de una carta";
            }
            else if (aux.name.Contains("Remove") || aux.name.Contains("Remover"))
            {
                auxCard.auxiliaryType = AuxiliaryCard.AuxiliaryType.RemoveVictory;
                auxCard.cardName = "Remover Victoria";
                auxCard.description = "Elimina una victoria del oponente";
            }
            else
            {
                // Tipo por defecto si no se puede determinar por el nombre
                auxCard.auxiliaryType = AuxiliaryCard.AuxiliaryType.LevelUp;
                auxCard.cardName = "Carta Auxiliar";
                auxCard.description = "Carta auxiliar con efecto especial";
            }
        }

        switch (areaType)
        {
            case "Extra":
                if (isOwned)
                {
                    aux.transform.SetParent(playerExtraArea.transform, false);
                }
                else
                {
                    aux.transform.SetParent(enemyExtraArea.transform, false);
                    CardFlipper flipper = aux.GetComponent<CardFlipper>();
                    if (flipper != null)
                    {
                        flipper.Flip();
                    }
                }
                break;
            default:
                Debug.LogWarning("Tipo de área auxiliar desconocido: " + areaType);
                break;
        }
    }


    // Hook para cuando cambia la variable hasPlayedCard
    void OnPlayedCardChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[{(isServer ? "Server" : "Client")}] Jugador {netId} - hasPlayedCard: {oldValue} → {newValue}");
    }

    // Métodos para target RPCs (si se necesitan)
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

    // Método para resetear el estado de jugada
    [Command]
    public void CmdResetHasPlayedCard()
    {
        hasPlayedCard = false;
        Debug.Log($"[Server] hasPlayedCard reseteado a false para jugador {netId}");
    }

    //
    public uint GetCurrentPlayedCardNetId()
    {
        return currentPlayedCardNetId;
    }

    //
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rand = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[rand];
            list[rand] = temp;
        }
    }

    // Metodos para el auxiliari 

    [Header("Sistema de Cartas Disponibles")]
    public List<GameObject> allAvailableCards = new List<GameObject>(); // Lista completa de todas las cartas posibles

    [Server]
    private void InitializeAvailableCards()
    {
        // Limpiar la lista por si acaso
        allAvailableCards.Clear();

        // Agregar todas las cartas de la lista existente
        allAvailableCards.AddRange(cards);

        Debug.Log($"Cartas disponibles inicializadas: {allAvailableCards.Count}");
    }

    public List<GameObject> GetAvailableCards()
    {
        if (allAvailableCards.Count == 0)
        {
            InitializeAvailableCards();
        }
        return allAvailableCards;
    }

    // Método para encontrar cartas específicas por criterios
    public GameObject FindCardByCriteria(CardData.ElementType element, CardData.ColorType color, int starLevel)
    {
        foreach (GameObject cardPrefab in GetAvailableCards())
        {
            CardData cardData = cardPrefab.GetComponent<CardData>();
            if (cardData != null &&
                cardData.element == element &&
                cardData.color == color &&
                cardData.starLevel == starLevel)
            {
                return cardPrefab;
            }
        }
        return null;
    }

    // Método para obtener todas las variantes de una carta (diferentes niveles)
    public List<GameObject> GetCardVariants(CardData.ElementType element, CardData.ColorType color)
    {
        List<GameObject> variants = new List<GameObject>();

        foreach (GameObject cardPrefab in GetAvailableCards())
        {
            CardData cardData = cardPrefab.GetComponent<CardData>();
            if (cardData != null &&
                cardData.element == element &&
                cardData.color == color)
            {
                variants.Add(cardPrefab);
            }
        }

        // Ordenar por nivel de estrella
        variants.Sort((a, b) =>
        {
            CardData cardA = a.GetComponent<CardData>();
            CardData cardB = b.GetComponent<CardData>();
            return cardA.starLevel.CompareTo(cardB.starLevel);
        });

        return variants;
    }

    // Método para verificar si una carta puede ser mejorada de nivel
    public bool CanCardBeLeveledUp(CardData originalCard)
    {
        List<GameObject> variants = GetCardVariants(originalCard.element, originalCard.color);

        foreach (GameObject variant in variants)
        {
            CardData variantData = variant.GetComponent<CardData>();
            if (variantData.starLevel > originalCard.starLevel && variantData.starLevel <= 3)
            {
                return true;
            }
        }

        return false;
    }

    // Método para verificar si una carta puede cambiar de elemento
    public bool CanCardChangeElement(CardData originalCard, CardData.ElementType newElement)
    {
        return FindCardByCriteria(newElement, originalCard.color, originalCard.starLevel) != null;
    }

    // Método para incrementar clicks en una carta - CORREGIDO
    [Command]
    public void CmdIncrementClick(GameObject card)
    {
        // Validar que la carta no sea null antes de proceder
        if (card == null)
        {
            Debug.LogWarning("[Server] CmdIncrementClick: carta es null, ignorando comando");
            return;
        }

        // Verificar que la carta tenga NetworkIdentity
        NetworkIdentity cardNetId = card.GetComponent<NetworkIdentity>();
        if (cardNetId == null)
        {
            Debug.LogWarning("[Server] CmdIncrementClick: carta no tiene NetworkIdentity");
            return;
        }

        RpcIncrementClick(card);
    }

    [ClientRpc]
    void RpcIncrementClick(GameObject card)
    {
        // CORRECCIÓN PRINCIPAL: Validar que la carta no sea null
        if (card == null)
        {
            Debug.LogWarning("[Client] RpcIncrementClick: carta recibida es null, ignorando RPC");
            return;
        }

        var click = card.GetComponent<incrementClick>();
        if (click != null)
        {
            click.NumberOfClicks++;
            Debug.Log("This card has been clicked " + click.NumberOfClicks + " times!");
        }
        else
        {
            Debug.LogWarning("[Client] RpcIncrementClick: carta no tiene componente incrementClick");
        }
    }
}

