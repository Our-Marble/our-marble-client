using TMPro;
using UnityEngine;

public class BoardTile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TMP_Text tileText;

    public void SetSpriteColor(Color color)
    {
        spriteRenderer.color = color;
    }

    public void SetText(Color color, string text="юс╫ц")
    {
        tileText.color = color;
        tileText.text = text;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
