using System.Collections;
using UnityEngine;
using Mirror;

public class AuxiliaryCard : NetworkBehaviour
{
    public enum AuxiliaryType
    {
        LevelUp,      // Aumenta nivel de cartas
        ElementChange, // Cambia elemento de carta
        RemoveVictory  // Elimina victoria del oponente
    }

    [Header("Configuración de Carta Auxiliar")]
    public AuxiliaryType auxiliaryType;
    public string cardName;
    [TextArea]
    public string description;

    [SyncVar]
    private bool hasBeenUsed = false;

    private float lastClickTime = 0f;
    private float doubleClickThreshold = 0.3f;

    public bool HasBeenUsed => hasBeenUsed;

    private void OnMouseDown()
    {
        // Solo el dueño puede usar sus cartas auxiliares
        if (!isOwned) return;
        if (hasBeenUsed)
        {
            Debug.Log("Esta carta auxiliar ya ha sido utilizada.");
            return;
        }

        // Detectar doble click
        if (Time.time - lastClickTime < doubleClickThreshold)
        {
            OnDoubleClick();
        }
        lastClickTime = Time.time;
    }

    private void OnDoubleClick()
    {
        Debug.Log($"Doble click en carta auxiliar: {auxiliaryType}");
        CmdUseAuxiliaryCard();
    }

    [Command]
    public void CmdUseAuxiliaryCard()
    {
        if (hasBeenUsed) return;

        switch (auxiliaryType)
        {
            case AuxiliaryType.LevelUp:
                InitiateLevelUpEffect();
                break;
            case AuxiliaryType.ElementChange:
                InitiateElementChangeEffect();
                break;
            case AuxiliaryType.RemoveVictory:
                InitiateRemoveVictoryEffect();
                break;
        }

        hasBeenUsed = true;
        RpcShowUsedEffect();
    }

    [Server]
    private void InitiateLevelUpEffect()
    {
        Debug.Log("Iniciando efecto de subir nivel");
        TargetShowCardSelection(connectionToClient, "level_up");
    }

    [Server]
    private void InitiateElementChangeEffect()
    {
        Debug.Log("Iniciando efecto de cambio de elemento");
        TargetShowCardSelection(connectionToClient, "element_change");
    }

    [Server]
    private void InitiateRemoveVictoryEffect()
    {
        Debug.Log("Iniciando efecto de remover victoria");
        // Este efecto es inmediato, no necesita selección de carta
        PlayerManager opponent = GetOpponentPlayer();
        if (opponent != null)
        {
            PlayerVictoryTracker.RemoveRandomVictory(opponent);
            RpcShowRemoveVictoryEffect();
        }
    }

    [TargetRpc]
    private void TargetShowCardSelection(NetworkConnection target, string effectType)
    {
        AuxiliaryEffectUI.Instance.ShowCardSelection(effectType, this);
    }

    [ClientRpc]
    private void RpcShowUsedEffect()
    {
        // Cambiar apariencia de la carta para mostrar que ya fue usada
        GetComponent<SpriteRenderer>().color = Color.gray;

        // Opcional: Añadir efecto visual
        StartCoroutine(ShowUsedAnimation());
    }

    [ClientRpc]
    private void RpcShowRemoveVictoryEffect()
    {
        // Mostrar efecto visual cuando se remueve una victoria
        if (AuxiliaryEffectUI.Instance != null)
        {
            AuxiliaryEffectUI.Instance.ShowRemoveVictoryEffect();
        }
    }

    private IEnumerator ShowUsedAnimation()
    {
        Vector3 originalScale = transform.localScale;

        // Animación de "pulso" para indicar que la carta fue usada
        for (int i = 0; i < 3; i++)
        {
            transform.localScale = originalScale * 1.1f;
            yield return new WaitForSeconds(0.1f);
            transform.localScale = originalScale;
            yield return new WaitForSeconds(0.1f);
        }
    }

    [Command]
    public void CmdApplyLevelUpEffect(GameObject targetCard)
    {
        if (hasBeenUsed) return;

        CardData cardData = targetCard.GetComponent<CardData>();
        if (cardData != null && cardData.CanLevelUp())
        {
            // Llamar al método LevelUp del CardData
            RpcApplyLevelUp(targetCard);
            Debug.Log($"Carta {cardData.cardName} aplicando efecto de subir nivel");
        }
        else
        {
            Debug.Log("La carta no puede subir de nivel (ya está al máximo o ya fue modificada)");
        }
    }

    [Command]
    public void CmdApplyElementChangeEffect(GameObject targetCard, int newElementIndex)
    {
        if (hasBeenUsed) return;

        CardData cardData = targetCard.GetComponent<CardData>();
        if (cardData != null && cardData.CanChangeElement())
        {
            // Llamar al método ChangeElement del CardData
            RpcApplyElementChange(targetCard, newElementIndex);
            Debug.Log($"Carta {cardData.cardName} aplicando cambio de elemento");
        }
        else
        {
            Debug.Log("La carta no puede cambiar de elemento (ya fue modificada)");
        }
    }

    [ClientRpc]
    private void RpcApplyLevelUp(GameObject targetCard)
    {
        CardData cardData = targetCard.GetComponent<CardData>();
        if (cardData != null)
        {
            cardData.LevelUp();
        }
    }

    [ClientRpc]
    private void RpcApplyElementChange(GameObject targetCard, int newElementIndex)
    {
        CardData cardData = targetCard.GetComponent<CardData>();
        if (cardData != null)
        {
            cardData.ChangeElement((CardData.ElementType)newElementIndex);
        }
    }

    [ClientRpc]
    private void RpcUpdateCardVisual(GameObject card)
    {
        // Ya no necesitamos este método, las cartas se actualizan automáticamente
        // a través de sus propios métodos LevelUp() y ChangeElement()
    }

    private PlayerManager GetOpponentPlayer()
    {
        PlayerManager[] players = FindObjectsOfType<PlayerManager>();
        foreach (PlayerManager player in players)
        {
            if (player != GetComponent<NetworkIdentity>().connectionToClient.identity.GetComponent<PlayerManager>())
            {
                return player;
            }
        }
        return null;
    }

    // Método para resetear el estado de la carta (útil para nuevas partidas)
    [Server]
    public void ResetCard()
    {
        hasBeenUsed = false;
        RpcResetVisuals();
    }

    [ClientRpc]
    private void RpcResetVisuals()
    {
        GetComponent<SpriteRenderer>().color = Color.white;
    }
}