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

    public GameObject tasksPanel;

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
    private bool tutorialStep2Shown = false;
    private bool finalTutorialShown = false;


    public bool showTutorial;
    public GameObject[] tutorialPopUps;
    UserData userData;

    private AudioSource audioSource;
    public AudioClip buttonClickSound;

    //public GameObject plusOneStarPopUp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        userData = FileStorage.LoadData();
        
        // to clear the farm tile data
        userData.tileStates.Clear();
        FileStorage.SaveData(userData);
        Debug.Log("Tile data cleared!");

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

        if (showTutorial)
        {
            tutorialPopUps[0].SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // First: check if user tapped to dismiss tutorial popups
        if (showTutorial && (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began || Input.GetMouseButtonDown(0)))
        {
            bool closedAny = false;
            foreach (GameObject popup in tutorialPopUps)
            {
                if (popup.activeSelf)
                {
                    popup.SetActive(false);
                    closedAny = true;
                }
            }

            // If user just closed the final tutorial popup
            if (finalTutorialShown && closedAny)
            {
                showTutorial = false;
                Debug.Log("Tutorial fully completed!");
            }
        }

        // Then: process tile interaction
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            DetectTileAtTouch(Input.GetTouch(0).position);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            DetectTileAtTouch(Input.mousePosition);
        }

        // Step 2 tutorial
        if (!tutorialStep2Shown && showTutorial && !tasksPanel.activeSelf && stars == 2)
        {
            tutorialPopUps[2].SetActive(true);
            tutorialStep2Shown = true;
        }

        // Final tutorial step: show when watered
        if (showTutorial && tutorialStep2Shown && stars == 0)
        {
            if (!tutorialPopUps[3].activeSelf)
            {
                UserData userData = FileStorage.LoadData();
                foreach (TileData data in userData.tileStates)
                {
                    if (data.state == TileState.Watered)
                    {
                        tutorialPopUps[3].SetActive(true);
                        finalTutorialShown = true; // ← Track that final popup was shown
                        Debug.Log("Final tutorial popup shown.");
                        break;
                    }
                }
            }
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

        UserData userData = FileStorage.LoadData();

        TileData newTileData = new TileData
        {
            x = tilePosition.x,
            y = tilePosition.y,
            state = state,
            plantedAt = (state == TileState.Planted) ? System.DateTime.UtcNow.ToString("o") : null
        };

        // Add or replace the tile data
        userData.tileStates.RemoveAll(t => t.x == tilePosition.x && t.y == tilePosition.y);
        userData.tileStates.Add(newTileData);

        FileStorage.SaveData(userData);


        AddStars(-1);
    }

    void SaveTileStates()
    {
        UserData userData = FileStorage.LoadData();

        foreach (var kvp in currentTileStates)
        {
            userData.tileStates.RemoveAll(t => t.x == kvp.Key.x && t.y == kvp.Key.y);

            userData.tileStates.Add(new TileData
            {
                x = kvp.Key.x,
                y = kvp.Key.y,
                state = kvp.Value,
                plantedAt = System.DateTime.Now.ToString()
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

            if (data.state == TileState.Planted && !string.IsNullOrEmpty(data.plantedAt))
            {
                System.DateTime plantedTime;
                if (System.DateTime.TryParse(data.plantedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out plantedTime))
                {
                    if ((System.DateTime.UtcNow - plantedTime).TotalHours >= 24)
                    {
                        data.state = TileState.HarvestReady;
                    }
                }
            }
            
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

    public void OpenTasksPanelAndMaybeTutorial()
    {
        tasksPanel.SetActive(true);

        if (showTutorial && tutorialPopUps[1] != null)
        {
            tutorialPopUps[1].SetActive(true);
        }
    }
}


