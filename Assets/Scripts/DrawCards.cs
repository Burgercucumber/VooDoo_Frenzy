using UnityEngine;

public class DrawCards : MonoBehaviour
{
    public GameObject Card;
    public GameObject AreaJugador;

    public void OnClick()
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject card = Instantiate(Card, new Vector2(0, 0), Quaternion.identity);
            card.transform.SetParent(AreaJugador.transform, false);
        }
    }

    void Start()
    {
        
    }

    
    void Update()
    {
        
    }
}
