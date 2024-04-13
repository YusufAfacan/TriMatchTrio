using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Threading.Tasks;

public class Tile : MonoBehaviour
{
    // Coordinates of the tile in the grid
    public int x;
    public int y;

    // Item associated with the tile
    private Item _item;

    // Property to access and set the item of the tile
    public Item Item
    {
        get => _item;
        set
        {
            if (_item == value) return;
            _item = value;
            icon.sprite = _item?.sprite; // NULL CHECK ADDED HERE
        }
    }

    // Reference to the UI image & Button
    public Image icon;
    public Button button;

    // Getters for neighboring tiles
    public Tile Left => x > 0 ? Board.Instance.Tiles[x - 1, y] : null;
    public Tile Top => y > 0 ? Board.Instance.Tiles[x, y - 1] : null;
    public Tile Right => x < Board.Instance.Width - 1 ? Board.Instance.Tiles[x + 1, y] : null;
    public Tile Bottom => y < Board.Instance.Width - 1 ? Board.Instance.Tiles[x, y + 1] : null;

    // Array of neighboring tiles
    public Tile[] Neighbours => new[]
    {
        Left,
        Top,
        Right,
        Bottom,
    };

    private void Start()
    {
         // Add listener to the button click event
        button.onClick.AddListener(() => Board.Instance.Select(tile: this));

        int sandblockEnabled = LevelManager.SandblockEnabled; // LevelManager'dan sandblockEnabled özelliğini al

        // Check if the item is a sandblock and if sandblock is enabled for the current level
        if (Item != null && Item.isSandblock)
        {
            if (sandblockEnabled == 0)
            {
                // If the item is a sandblock but sandblock is not enabled for the current level, set it to null
                Item = null;
            }
        }
    }

    // Method to get connected tiles based on the same item
    public List<Tile> GetConnectedTiles(List<Tile> exclude = null)
    {
        var result = new List<Tile> { this, };

        if (exclude == null)
        {
            exclude = new List<Tile> { this, };
        }
        else
        {
            exclude.Add(this);
        }

        foreach (var neighbour in Neighbours)
        {
            if (neighbour == null || exclude.Contains(neighbour) || neighbour.Item != Item) continue;
            if (neighbour.Item.isSandblock) continue; // Skip sandblocks

            result.AddRange(neighbour.GetConnectedTiles(exclude));
        }

        return result;
    }
}
