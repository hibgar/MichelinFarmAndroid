using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps; 
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{

    public Text starCount;
    public int stars;

    public Tilemap groundTilemap;

    public GameObject plantSeedsButton;
    public GameObject waterPlantButton;
    public GameObject harvestPlantButton;
    private GameObject activePopup;

    public Tilemap overlayTilemap;
    public Sprite seedPlantedSprite;
    public Sprite wateredSprite;
    public Sprite harvestReadySprite;

    private Dictionary<TileState, Sprite> tileStateToSprite;
    private Dictionary<Vector3Int, TileState> currentTileStates = new Dictionary<Vector3Int, TileState>();

    public enum TileState
    {
        Empty,
        Planted,
        Watered,
        HarvestReady
    }

    private bool isPlanting = false;
    private bool isWatering = false;

    private AudioSource audioSource;
    public AudioClip buttonClickSound;

    //public GameObject plusOneStarPopUp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UserData userData = FileStorage.LoadData();
        
        // to clear the farm tile data
        //userData.tileStates.Clear();
        //FileStorage.SaveData(userData);
        //Debug.Log("Tile data cleared!");

        tileStateToSprite = new Dictionary<TileState, Sprite>()
        {
            { TileState.Planted, seedPlantedSprite },
            { TileState.Watered, wateredSprite },
            { TileState.HarvestReady, harvestReadySprite }
        };

        LoadTileStates();

        stars = userData.starAmt;
        
        Debug.Log("Star count successfully converted: " + stars);
        updateStarCountInUI();

        audioSource = gameObject.AddComponent<AudioSource>();

        plantSeedsButton.GetComponentInChildren<Button>().onClick.AddListener(OnPlantButtonClick);
        waterPlantButton.GetComponentInChildren<Button>().onClick.AddListener(OnWaterButtonClick);
    }

    // Update is called once per frame
    void Update()
    {
        // Check for touch input (mobile) or mouse click (Editor)
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            DetectTileAtTouch(Input.GetTouch(0).position);
        }
        else if (Input.GetMouseButtonDown(0)) // Mouse click (for testing in Editor)
        {
            DetectTileAtTouch(Input.mousePosition);
        }
    }

    // Detects tile at touch position
    void DetectTileAtTouch(Vector2 screenPosition)
    {
        if (!isPlanting && !isWatering) return;

        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(screenPosition);
        worldPoint.z = 0; // Ensures we stay in the correct 2D plane

        Vector3Int tilePosition = groundTilemap.WorldToCell(worldPoint);
        TileBase clickedTile = groundTilemap.GetTile(tilePosition);

        // if user clicked on a tile
        if (clickedTile != null)
        {
            // if user clicked on a farm tile
            if (tilePosition.x >= -2 && tilePosition.x <= 1 && tilePosition.y >= -2 && tilePosition.y <= 1)
            {
                if (stars > 0 && isPlanting)
                {
                    ChangeTileSprite(tilePosition, TileState.Planted);
                    isPlanting = false;
                }
                else if (stars > 0 && isWatering)
                {
                    ChangeTileSprite(tilePosition, TileState.Watered);
                    isWatering = false;
                }
            }  else {
                Debug.Log("Clicked outside valid range!");
                isPlanting = false;
                isWatering = false;
                //HidePopup();
            }

            Debug.Log("Clicked on tile: " + clickedTile.name + " at position: " + tilePosition);
        }
    }

    void ChangeTileSprite(Vector3Int tilePosition, TileState state)
    {
        if (state == TileState.Empty) {
            overlayTilemap.SetTile(tilePosition, null);
        }
        else if (tileStateToSprite.ContainsKey(state)) {
            Tile newTile = ScriptableObject.CreateInstance<Tile>();
            newTile.sprite = tileStateToSprite[state];
            overlayTilemap.SetTile(tilePosition, newTile);
        }

        currentTileStates[tilePosition] = state;
        SaveTileStates();

        AddStars(-1);
    }

    void SaveTileStates()
    {
        UserData userData = FileStorage.LoadData();
        userData.tileStates.Clear();

        foreach (var kvp in currentTileStates)
        {
            userData.tileStates.Add(new TileData
            {
                x = kvp.Key.x,
                y = kvp.Key.y,
                state = kvp.Value
            });
        }

        FileStorage.SaveData(userData);
    }

    void LoadTileStates()
    {
        UserData userData = FileStorage.LoadData();
        foreach (TileData data in userData.tileStates)
        {
            Vector3Int pos = new Vector3Int(data.x, data.y, 0);
            ChangeTileSprite(pos, data.state);
        }
    }

    void ShowPopup(Vector3Int position, TileBase clickedTile)
    {
        if (activePopup != null) {
            HidePopup();
        }
        
        
        Vector3 worldPosition = groundTilemap.CellToWorld(position) + new Vector3(2f, 2f, 0); // Adjust popup position
        worldPosition.x *= 20;
        worldPosition.y *= 20;
        activePopup = Instantiate(plantSeedsButton, worldPosition + new Vector3(0, 0, 1), Quaternion.identity);
        activePopup.transform.SetParent(GameObject.Find("Canvas").transform, false);
        
        Button buttonInPopup = activePopup.GetComponentInChildren<Button>();
        if (buttonInPopup)
        {
            buttonInPopup.onClick.RemoveAllListeners();
            buttonInPopup.onClick.AddListener(OnPlantButtonClick);
        } 

    }

    void HidePopup()
    {
        if (activePopup != null)
        {
            Destroy(activePopup);
            activePopup = null;
        }
    }

    void OnPlantButtonClick()
    {
        Debug.Log("Button clicked!!!!!");
        isPlanting = true;

        if (buttonClickSound != null) {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    void OnWaterButtonClick()
    {
        Debug.Log("Button clicked!!!!!");
        isWatering = true;

        if (buttonClickSound != null) {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    public void AddStars(int amount)
    {
        //Vector3 mousePos = Input.mousePosition;
        //GameObject PlusOnePopUp = Instantiate(plusOneStarPopUp, mousePos, new Quaternion());
        if (stars + amount < 0)
        {
            Debug.Log("Not enough stars!");
            return;
        }

        stars += amount;
        FileStorage.UpdateStarsInJSON(stars);
        updateStarCountInUI();
    }

    public void updateStarCountInUI()
    {
        starCount.text = stars.ToString();
    }
}


