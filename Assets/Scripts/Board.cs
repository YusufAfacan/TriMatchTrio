using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Add this namespace for TextMeshPro
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;

public class Board : MonoBehaviour
{
    // Singleton instance of the board
    public static Board Instance { get; private set; }

    // Audio clip for popping sound
    [SerializeField] private AudioClip popUpSfx;
    [SerializeField] private AudioSource audioSource;

    // Rows of tiles on the board
    public Row[] rows;
    public Tile[,] Tiles { get; private set; }

    // Width and height of the board
    public int Width => Tiles.GetLength(dimension: 0);
    public int Height => Tiles.GetLength(dimension: 1);

    // List to store selected tiles
    private readonly List<Tile> _selection = new List<Tile>();

    // Duration of tween animations
    private const float TweenDuration = 0.25f;

    // Timer for the level
    public float levelTimer;
    private bool isGameRunning = true;

    // Score threshold for the level
    public int scoreThreshold;

    // Reference to the TextMeshPro object for displaying the timer
    [SerializeField] private TextMeshProUGUI timerText;

    // Serialized fields for WIN and LOSE panels
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    public int PopCount;

    // Called when the board object is initialized
    private void Awake() => Instance = this;

    // Called when the game starts
    private async void Start()
    {
        PopCount = 0;

        await InitializeBoardAsync(); // Await the asynchronous board initialization
        StartCoroutine(UpdateTimer());
    }

    // Method to initialize the board asynchronously
    private async Task InitializeBoardAsync()
    {
        Tiles = new Tile[rows.Max(row => row.tiles.Length), rows.Length];

        // Populate the board with tiles
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var tile = rows[y].tiles[x];

                tile.x = x;
                tile.y = y;

                // Randomly select an item from the ItemDatabase
                if (SceneManager.GetActiveScene().name == "Level1" || SceneManager.GetActiveScene().name == "Level2")
                {
                    tile.Item = ItemDatabase.Items.Where(item => !item.isSandblock).ElementAt(Random.Range(0, ItemDatabase.Items.Length - 1));
                }
                else
                {
                    tile.Item = ItemDatabase.Items.ElementAt(Random.Range(0, ItemDatabase.Items.Length));
                }

                Tiles[x, y] = tile;
            }
        }

        await Pop(); // Await the popping process // Initial popping of connected tiles
    }

    // Update is called once per frame
    private void Update()
    {
        if (!isGameRunning) return; // Check if the game is running

        if (!Input.GetKeyDown(KeyCode.A)) return;

        // Debugging: Scale up the first connected tile on the board
        foreach (var connectedTile in Tiles[0, 0].GetConnectedTiles())
        {
            connectedTile.icon.transform.DOScale(1.25f, TweenDuration).Play();
        }
    }

    // Method to handle tile selection
    public async void Select(Tile tile)
    {
        if (!isGameRunning) return; // Check if the game is running

        if (!_selection.Contains(tile))
            _selection.Add(tile);

        if (_selection.Count > 0)
        {
            if (System.Array.IndexOf(_selection[0].Neighbours, tile) != -1)
            {
                _selection.Add(tile);
            }
        }
        else
        {
            _selection.Add(tile);
        }

        if (_selection.Count < 2)
            return;

        // Check if both tiles are movable before swapping
        if (_selection[0].Item.isSandblock || _selection[1].Item.isSandblock)
        {
            _selection.Clear(); // Clear selection if either tile is a sandblock
            return;
        }

        Debug.Log($"Selected tiles at ({_selection[0].x}, {_selection[0].y}) and ({_selection[1].x}, {_selection[1].y})");

        await Swap(_selection[0], _selection[1]);

        if (CanPop())
        {
            await Pop(); // Await the popping process
        }
        else
        {
            await Swap(_selection[0], _selection[1]);
        }

        _selection.Clear();
    }

    // Method to swap two tiles
    public async Task Swap(Tile tile1, Tile tile2)
    {
        Image icon1 = tile1.icon;
        Image icon2 = tile2.icon;

        Transform icon1Transform = icon1.transform;
        Transform icon2Transform = icon2.transform;

        Sequence sequence = DOTween.Sequence();

        sequence.Join(icon1Transform.DOMove(icon2Transform.position, TweenDuration));
        sequence.Join(icon2Transform.DOMove(icon1Transform.position, TweenDuration));

        await sequence.Play().AsyncWaitForCompletion();

        icon1Transform.SetParent(tile2.transform);
        icon2Transform.SetParent(tile1.transform);

        tile1.icon = icon2;
        tile2.icon = icon1;

        var tile1Item = tile1.Item;
        tile1.Item = tile2.Item;
        tile2.Item = tile1Item;
    }

    // Method to check if popping is possible
    private bool CanPop()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                if (Tiles[x, y].GetConnectedTiles().Skip(1).Count() >= 2)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Method to pop connected tiles
    private async Task Pop()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var tile = Tiles[x, y];
                var connectedTiles = tile.GetConnectedTiles();

                if (connectedTiles.Skip(1).Count() < 2) continue;

                var deflateSequence = DOTween.Sequence();

                foreach (var connectedTile in connectedTiles)
                {
                    deflateSequence.Join(connectedTile.icon.transform.DOScale(Vector3.zero, TweenDuration));
                }

                audioSource.PlayOneShot(popUpSfx);

                await deflateSequence.Play().AsyncWaitForCompletion();

                ScoreCounter.Instance.Score += tile.Item.value * connectedTiles.Count;

                var inflateSequence = DOTween.Sequence();

                foreach (var connectedTile in connectedTiles)
                {
                    if (SceneManager.GetActiveScene().name == "Level1" || SceneManager.GetActiveScene().name == "Level2")
                    {
                        if (!connectedTile.Item.isSandblock)
                        {
                            connectedTile.Item = ItemDatabase.Items.Where(item => !item.isSandblock).ElementAt(Random.Range(0, ItemDatabase.Items.Length - 1));
                            inflateSequence.Join(connectedTile.icon.transform.DOScale(Vector3.one, TweenDuration));
                        }
                    }
                    else
                    {
                        connectedTile.Item = ItemDatabase.Items.ElementAt(Random.Range(0, ItemDatabase.Items.Length));
                        inflateSequence.Join(connectedTile.icon.transform.DOScale(Vector3.one, TweenDuration));
                    }
                }

                await inflateSequence.Play().AsyncWaitForCompletion();
            }
        }

        PopCount++;
    }

    // Method to update the timer
    private IEnumerator UpdateTimer()
    {
        while (levelTimer > 0)
        {
            // Update the timer text
            if (timerText != null)
            {
                timerText.text = $"Time: {(int)levelTimer}";
            }

            // Decrease the timer value
            levelTimer -= Time.deltaTime;

            yield return null;
        }

        // When the timer hits zero
        isGameRunning = false;

        // Check if the score is above the threshold to determine which panel to activate
        if (ScoreCounter.Instance.Score >= scoreThreshold)
        {
            if (winPanel != null)
            {
                winPanel.SetActive(true);
            }
            else
            {
                Debug.LogError("Win Panel is not assigned!");
            }
        }
        else
        {
            if (losePanel != null)
            {
                losePanel.SetActive(true);
            }
            else
            {
                Debug.LogError("Lose Panel is not assigned!");
            }
        }
    }
}
