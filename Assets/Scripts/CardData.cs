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

}