using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    [Header("Board Layout")]
    [SerializeField, Min(1)] private int tilesPerSide = 7;
    [SerializeField] private Vector2 tileSpacing = new(0.8f, 0.45f);
    [SerializeField, Min(1f)] private float cornerSpacingMultiplier = 1.5f;

    [Header("Tile Prefabs")]
    [SerializeField] private GameObject cornerTilePrefab;
    [SerializeField] private GameObject leftTopTilePrefab;
    [SerializeField] private GameObject rightTopTilePrefab;
    [SerializeField] private GameObject rightBottomTilePrefab;
    [SerializeField] private GameObject leftBottomTilePrefab;

    [Header("Board Tiles")]
    [SerializeField] private List<BoardTile> boardTiles = new();

    [Header("Colors")]
    [SerializeField] private Color defaultColor = Color.beige;
    [SerializeField] private Color defaultTextColor = Color.brown;
    [SerializeField] private Color cornerColor = Color.aliceBlue;
    public IReadOnlyList<BoardTile> BoardTiles => boardTiles;

    public void SetBoardTileColor(int tileIndex, Color color, Color textColor)
    {
        boardTiles[tileIndex].SetSpriteColor(color);
        boardTiles[tileIndex].SetText(defaultTextColor);
    }

    [ContextMenu("SetDefaultColors")]
    public void SetDefaultColors()
    {
        for (int i = 0; i < boardTiles.Count; i++)
            SetBoardTileColor(i, i % 8 == 0 ? cornerColor : defaultColor, defaultTextColor);

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
