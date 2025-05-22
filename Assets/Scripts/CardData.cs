using UnityEngine;
using UnityEngine.UI;

public class CardData : MonoBehaviour
{
    public CardData card;
    public enum ElementType
    {
        Boton,
        Alfiler,
        Tela,
        Algodon
    }

    public enum ColorType
    {
        Rojo,
        Verde,
        Morado
    }

    [Header("Datos de la Carta")]
    public string cardName;
    [Range(1, 3)]
    public int starLevel = 1;
    public ElementType element;
    public ColorType color;
    [TextArea]
    public string description;

    [Header("Componente de Imagen")]
    [SerializeField] private Image cardImageComponent; // Referencia al componente Image
    [SerializeField] private Sprite originalImage; // La imagen original de la carta

    [Header("Imágenes de Variación - Subir Nivel")]
    [SerializeField] private Sprite levelUpImage; // Imagen cuando sube a nivel 2 o 3

    [Header("Imágenes de Cambio de Elemento")]
    [SerializeField] private Sprite botonVariantImage; // Si cambia a Botón
    [SerializeField] private Sprite alfilerVariantImage; // Si cambia a Alfiler
    [SerializeField] private Sprite telaVariantImage; // Si cambia a Tela
    [SerializeField] private Sprite algodonVariantImage; // Si cambia a Algodón

    // Variables para rastrear si la carta fue modificada
    private bool hasLeveledUp = false;
    private bool hasChangedElement = false;
    private ElementType originalElement;
    private int originalLevel;

    private void Start()
    {
        // Buscar el componente Image si no está asignado
        if (cardImageComponent == null)
        {
            cardImageComponent = GetComponent<Image>();
            if (cardImageComponent == null)
            {
                cardImageComponent = GetComponentInChildren<Image>();
            }
        }

        // Guardar valores originales
        originalElement = element;
        originalLevel = starLevel;

        // Guardar imagen original si no está asignada
        if (originalImage == null && cardImageComponent != null)
        {
            originalImage = cardImageComponent.sprite;
        }
    }

    // Método para subir nivel (llamado por cartas auxiliares)
    public void LevelUp()
    {
        if (starLevel < 3 && !hasLeveledUp)
        {
            starLevel++;
            hasLeveledUp = true;
            UpdateCardImage();
            StartCoroutine(ImageChangeEffect());
            Debug.Log($"Carta {cardName} subió al nivel {starLevel}");
        }
        else if (hasLeveledUp)
        {
            Debug.Log($"La carta {cardName} ya fue mejorada de nivel");
        }
        else
        {
            Debug.Log($"La carta {cardName} ya está al nivel máximo");
        }
    }

    // Método para cambiar elemento (llamado por cartas auxiliares)
    public void ChangeElement(ElementType newElement)
    {
        if (!hasChangedElement)
        {
            element = newElement;
            hasChangedElement = true;
            UpdateCardImage();
            StartCoroutine(ImageChangeEffect());
            Debug.Log($"Carta {cardName} cambió a elemento {newElement}");
        }
        else
        {
            Debug.Log($"La carta {cardName} ya cambió de elemento");
        }
    }

    private void UpdateCardImage()
    {
        if (cardImageComponent == null) return;

        Sprite newSprite = GetCurrentCardSprite();
        if (newSprite != null)
        {
            cardImageComponent.sprite = newSprite;
        }
    }

    private Sprite GetCurrentCardSprite()
    {
        // Prioridad: Cambio de elemento > Subir nivel > Original

        // Si cambió de elemento, usar la imagen correspondiente al nuevo elemento
        if (hasChangedElement)
        {
            switch (element)
            {
                case ElementType.Boton:
                    return botonVariantImage != null ? botonVariantImage : originalImage;
                case ElementType.Alfiler:
                    return alfilerVariantImage != null ? alfilerVariantImage : originalImage;
                case ElementType.Tela:
                    return telaVariantImage != null ? telaVariantImage : originalImage;
                case ElementType.Algodon:
                    return algodonVariantImage != null ? algodonVariantImage : originalImage;
            }
        }

        // Si subió de nivel, usar la imagen de nivel mejorado
        if (hasLeveledUp && levelUpImage != null)
        {
            return levelUpImage;
        }

        // Si no hay cambios, usar imagen original
        return originalImage;
    }

    // Método para resetear la carta a su estado original
    public void ResetCard()
    {
        starLevel = originalLevel;
        element = originalElement;
        hasLeveledUp = false;
        hasChangedElement = false;
        UpdateCardImage();
        Debug.Log($"Carta {cardName} reseteada a su estado original");
    }

    // Verificar si la carta puede subir de nivel
    public bool CanLevelUp()
    {
        return starLevel <= 2 && !hasLeveledUp;
    }

    // Verificar si la carta puede cambiar de elemento
    public bool CanChangeElement()
    {
        return !hasChangedElement;
    }

    // Obtener información del estado actual
    public string GetStatusInfo()
    {
        string info = $"Carta: {cardName}\n";
        info += $"Nivel: {starLevel}";
        if (hasLeveledUp) info += " (Mejorado)";
        info += $"\nElemento: {element}";
        if (hasChangedElement) info += $" (Cambiado de {originalElement})";

        return info;
    }

    private System.Collections.IEnumerator ImageChangeEffect()
    {
        if (cardImageComponent == null) yield break;

        Vector3 originalScale = transform.localScale;
        Color originalColor = cardImageComponent.color;

        // Efecto de brillo dorado para indicar mejora
        for (int i = 0; i < 3; i++)
        {
            cardImageComponent.color = Color.yellow;
            transform.localScale = originalScale * 1.1f;
            yield return new WaitForSeconds(0.15f);

            cardImageComponent.color = originalColor;
            transform.localScale = originalScale;
            yield return new WaitForSeconds(0.1f);
        }
    }
}