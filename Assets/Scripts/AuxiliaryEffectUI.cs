using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AuxiliaryEffectUI : MonoBehaviour
{
    public static AuxiliaryEffectUI Instance;

    [Header("UI Referencias")]
    public GameObject cardSelectionPanel;
    public GameObject elementSelectionPanel;
    public GameObject effectNotificationPanel;

    [Header("Card Selection UI")]
    public Transform cardSelectionContainer;
    public Button cardSelectionPrefab;
    public TextMeshProUGUI cardSelectionTitle;
    public Button cancelSelectionButton;

    [Header("Element Selection UI")]
    public Button[] elementButtons; // 4 botones para los 4 elementos
    public TextMeshProUGUI elementSelectionTitle;
    public Button cancelElementButton;

    [Header("Notification UI")]
    public TextMeshProUGUI notificationText;
    public Button closeNotificationButton;

    private string currentEffectType;
    private AuxiliaryCard currentAuxiliaryCard;
    private GameObject selectedCard;
    private List<GameObject> availableCards = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Configurar botones
        cancelSelectionButton.onClick.AddListener(CancelCardSelection);
        cancelElementButton.onClick.AddListener(CancelElementSelection);
        closeNotificationButton.onClick.AddListener(CloseNotification);

        // Configurar botones de elementos
        for (int i = 0; i < elementButtons.Length; i++)
        {
            int elementIndex = i; // Capturar el índice para el closure
            elementButtons[i].onClick.AddListener(() => SelectElement(elementIndex));
        }

        // Ocultar paneles inicialmente
        HideAllPanels();
    }

    public void ShowCardSelection(string effectType, AuxiliaryCard auxiliaryCard)
    {
        currentEffectType = effectType;
        currentAuxiliaryCard = auxiliaryCard;

        // Configurar título según el tipo de efecto
        switch (effectType)
        {
            case "level_up":
                cardSelectionTitle.text = "Selecciona una carta para subir de nivel (máximo 2 estrellas)";
                break;
            case "element_change":
                cardSelectionTitle.text = "Selecciona una carta para cambiar su elemento";
                break;
        }

        // Obtener cartas disponibles
        GetAvailableCards(effectType);

        // Mostrar cartas disponibles
        DisplayAvailableCards();

        // Mostrar panel
        cardSelectionPanel.SetActive(true);
    }

    private void GetAvailableCards(string effectType)
    {
        availableCards.Clear();

        // Buscar todas las cartas del jugador
        PlayerManager localPlayer = FindLocalPlayer();
        if (localPlayer == null) return;

        // Buscar cartas en el área del jugador
        Transform playerArea = localPlayer.PlayerArea.transform;

        for (int i = 0; i < playerArea.childCount; i++)
        {
            GameObject card = playerArea.GetChild(i).gameObject;
            CardData cardData = card.GetComponent<CardData>();

            if (cardData != null)
            {
                // Filtrar según el tipo de efecto
                bool isValidCard = false;

                switch (effectType)
                {
                    case "level_up":
                        isValidCard = cardData.CanLevelUp();
                        break;
                    case "element_change":
                        isValidCard = cardData.CanChangeElement();
                        break;
                }

                if (isValidCard)
                {
                    availableCards.Add(card);
                }
            }
        }
    }

    private void DisplayAvailableCards()
    {
        // Limpiar contenedor
        foreach (Transform child in cardSelectionContainer)
        {
            Destroy(child.gameObject);
        }

        // Crear botones para cada carta disponible
        foreach (GameObject card in availableCards)
        {
            CardData cardData = card.GetComponent<CardData>();
            if (cardData == null) continue;

            Button cardButton = Instantiate(cardSelectionPrefab, cardSelectionContainer);

            // Configurar texto del botón con información de estado
            TextMeshProUGUI buttonText = cardButton.GetComponentInChildren<TextMeshProUGUI>();
            string statusText = $"{cardData.cardName}\n";
            statusText += $"Nivel: {cardData.starLevel}★\n";
            statusText += $"Elemento: {cardData.element}";

            // Añadir información sobre si puede ser modificada
            if (currentEffectType == "level_up" && !cardData.CanLevelUp())
            {
                statusText += "\n(No disponible)";
            }
            else if (currentEffectType == "element_change" && !cardData.CanChangeElement())
            {
                statusText += "\n(Ya modificada)";
            }

            buttonText.text = statusText;

            // Configurar evento del botón
            GameObject cardRef = card; // Capturar referencia para el closure
            cardButton.onClick.AddListener(() => SelectCard(cardRef));
        }
    }

    private void SelectCard(GameObject card)
    {
        selectedCard = card;

        if (currentEffectType == "element_change")
        {
            // Mostrar selección de elementos
            ShowElementSelection();
        }
        else if (currentEffectType == "level_up")
        {
            // Aplicar efecto directamente
            ApplyLevelUpEffect();
        }
    }

    private void ShowElementSelection()
    {
        cardSelectionPanel.SetActive(false);
        elementSelectionPanel.SetActive(true);

        // Configurar texto de los botones de elementos
        string[] elementNames = { "Botón", "Alfiler", "Tela", "Algodón" };
        for (int i = 0; i < elementButtons.Length && i < elementNames.Length; i++)
        {
            TextMeshProUGUI buttonText = elementButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = elementNames[i];
        }
    }

    private void SelectElement(int elementIndex)
    {
        if (currentAuxiliaryCard != null && selectedCard != null)
        {
            // Aplicar efecto de cambio de elemento
            currentAuxiliaryCard.CmdApplyElementChangeEffect(selectedCard, elementIndex);

            // Mostrar notificación
            string elementName = ((CardData.ElementType)elementIndex).ToString();
            ShowNotification($"Elemento cambiado a {elementName}");
        }

        HideAllPanels();
    }

    private void ApplyLevelUpEffect()
    {
        if (currentAuxiliaryCard != null && selectedCard != null)
        {
            // Aplicar efecto de subir nivel
            currentAuxiliaryCard.CmdApplyLevelUpEffect(selectedCard);

            // Mostrar notificación
            CardData cardData = selectedCard.GetComponent<CardData>();
            ShowNotification($"Carta {cardData.cardName} subió de nivel");
        }

        HideAllPanels();
    }

    public void ShowRemoveVictoryEffect()
    {
        ShowNotification("Se eliminó una victoria del oponente");
    }

    private void ShowNotification(string message)
    {
        notificationText.text = message;
        effectNotificationPanel.SetActive(true);

        // Auto-cerrar después de 3 segundos
        Invoke(nameof(CloseNotification), 3f);
    }

    private void CancelCardSelection()
    {
        HideAllPanels();
    }

    private void CancelElementSelection()
    {
        // Volver a la selección de cartas
        elementSelectionPanel.SetActive(false);
        cardSelectionPanel.SetActive(true);
    }

    private void CloseNotification()
    {
        effectNotificationPanel.SetActive(false);
    }

    private void HideAllPanels()
    {
        cardSelectionPanel.SetActive(false);
        elementSelectionPanel.SetActive(false);
        effectNotificationPanel.SetActive(false);

        // Limpiar referencias
        currentAuxiliaryCard = null;
        selectedCard = null;
        availableCards.Clear();
    }

    private PlayerManager FindLocalPlayer()
    {
        PlayerManager[] players = FindObjectsOfType<PlayerManager>();
        foreach (PlayerManager player in players)
        {
            if (player.isLocalPlayer)
            {
                return player;
            }
        }
        return null;
    }
}
