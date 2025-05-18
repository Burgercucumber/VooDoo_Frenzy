using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerVictoryTracker : MonoBehaviour
{
    private struct Victory
    {
        public CardData.ElementType element;
        public CardData.ColorType color;

        public Victory(CardData.ElementType element, CardData.ColorType color)
        {
            this.element = element;
            this.color = color;
        }
    }

    private List<Victory> victories = new List<Victory>();

    public void AddVictory(CardData card)
    {
        if (IsVictoryValid(card))
        {
            victories.Add(new Victory(card.element, card.color));
            ShowVictoryUI(card);
        }
    }

    public bool HasWon()
    {
        return CheckFourDifferentElements() || CheckFourSameElementWithColorLimit();
    }

    private bool IsVictoryValid(CardData card)
    {
        // No se permite repetir elemento y color exacto
        foreach (var v in victories)
        {
            if (v.element == card.element && v.color == card.color)
                return false;
        }

        return true;
    }

    private bool CheckFourDifferentElements()
    {
        HashSet<CardData.ElementType> elements = new HashSet<CardData.ElementType>();
        foreach (var v in victories)
            elements.Add(v.element);

        return elements.Count >= 4;
    }

    private bool CheckFourSameElementWithColorLimit()
    {
        var grouped = new Dictionary<CardData.ElementType, List<CardData.ColorType>>();

        foreach (var v in victories)
        {
            if (!grouped.ContainsKey(v.element))
                grouped[v.element] = new List<CardData.ColorType>();

            grouped[v.element].Add(v.color);
        }

        foreach (var pair in grouped)
        {
            if (pair.Value.Count >= 4)
            {
                var colorCount = new Dictionary<CardData.ColorType, int>();
                foreach (var c in pair.Value)
                {
                    if (!colorCount.ContainsKey(c))
                        colorCount[c] = 0;
                    colorCount[c]++;
                }

                if (colorCount.Values.All(count => count <= 2))
                    return true;
            }
        }

        return false;
    }

    private void ShowVictoryUI(CardData card)
    {
        // Aquí generas el cuadrito en UI con imagen y fondo de color
        // Por ejemplo: VictoryUIManager.Instance.CreateVictorySquare(card.element, card.color);
    }
}
