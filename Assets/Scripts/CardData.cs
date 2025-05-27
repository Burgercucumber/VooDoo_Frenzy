using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardData : MonoBehaviour, IPointerDownHandler
{
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

    // Implementar IPointerDownHandler para detectar clicks
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"Carta {cardName} clickeada");

        // Verificar si hay una carta auxiliar seleccionada
        if (AuxiliaryCard.HasSelectedAuxiliary())
        {
            Debug.Log($"Aplicando efecto de carta auxiliar a {cardName}");
            // Llamar al método estático de AuxiliaryCard
            AuxiliaryCard.OnCardClicked(gameObject);
        }
        else
        {
            Debug.Log("No hay carta auxiliar seleccionada");
            // Aquí puedes agregar la lógica normal de click de carta
        }
    }
}

