using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.IO;
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
    public GameObject noStarsWarning;
    public GameObject tasksPanel;

    public Tilemap overlayTilemap;
    public Sprite seedPlantedSprite;
    public Sprite wateredSprite;
    public Sprite harvestReadySprite;
    public GameObject cropHarvestedPopupPrefab; // Assign in Inspector


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
    private bool isHarvesting = false;
    private bool tutorialStep2Shown = false;
    private bool finalTutorialShown = false;

    public bool showTutorial;
    public GameObject[] tutorialPopUps;
    UserData userData;

    private AudioSource audioSource;
    public AudioClip buttonClickSound;

    private float checkTimer = 0f;
    private float checkInterval = 5f; // Check every 5 seconds


    //public GameObject plusOneStarPopUp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Determine if needs first-time tutorial
        showTutorial = File.Exists(Path.Combine(Application.persistentDataPath, "userData.json")) == false;

        userData = FileStorage.LoadData();
        
        // to clear the farm tile data
        //userData.tileStates.Clear();
        //FileStorage.SaveData(userData);
        //Debug.Log("Tile data cleared!");

        tileStateToSprite = new Dictionary<TileState, Sprite>()
        {
            { TileState.Empty, null},
            { TileState.Planted, seedPlantedSprite },
            { TileState.Watered, wateredSprite },
            { TileState.HarvestReady, harvestReadySprite }
        };

        LoadTileStates();

        stars = userData.starAmt;
        
        Debug.Log("Star count successfully converted: " + stars);
        updateStarCountInUI();

        audioSource = gameObject.AddComponent<AudioSource>();

        UpdateHarvestStates();

        plantSeedsButton.GetComponentInChildren<Button>().onClick.AddListener(OnPlantButtonClick);
        waterPlantButton.GetComponentInChildren<Button>().onClick.AddListener(OnWaterButtonClick);
        harvestPlantButton.GetComponentInChildren<Button>().onClick.AddListener(OnHarvestButtonClick);

        if (showTutorial)
        {
            tutorialPopUps[0].SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // First: check if user tapped to dismiss tutorial popups
        //if (showTutorial && (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began || Input.GetMouseButtonDown(0)))
        if (showTutorial && ((Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ))
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

        if ((Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) && noStarsWarning.activeSelf)
        {
            noStarsWarning.SetActive(false);
        }

        // Then: process tile interaction
        /* if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            DetectTileAtTouch(Input.GetTouch(0).position);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            DetectTileAtTouch(Input.mousePosition);
        } */
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            DetectTileAtTouch(Touchscreen.current.primaryTouch.position.ReadValue());
        }
        else if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            DetectTileAtTouch(Mouse.current.position.ReadValue());
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

        // Check for crops ready to harvest
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            UpdateHarvestStates();
        }

    }



    // Detects tile at touch position
    void DetectTileAtTouch(Vector2 screenPosition)
    {
        if (!isPlanting && !isWatering && !isHarvesting) return;

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
                currentTileStates.TryGetValue(tilePosition, out TileState currentState);

                if (stars > 0 && isPlanting && currentState == TileState.Empty)
                {
                    ChangeTileSprite(tilePosition, TileState.Planted);
                    isPlanting = false;
                }
                else if (stars > 0 && isWatering && currentState == TileState.Planted)
                {
                    ChangeTileSprite(tilePosition, TileState.Watered);
                    isWatering = false;
                } else if (isHarvesting && currentState == TileState.HarvestReady)
                {
                    ChangeTileSprite(tilePosition, TileState.Empty);
                    isHarvesting = false;
                    Debug.Log("harvesting ");

                    // Spawn popup at tile world position
                    if (cropHarvestedPopupPrefab != null)
                    {
                        Vector3 worldPos = groundTilemap.CellToWorld(tilePosition);
                        worldPos += new Vector3(0.5f, 0.5f, 0f); // Center the popup in the tile (optional)

                        // Instantiate the popup at the correct position
                        Instantiate(cropHarvestedPopupPrefab, worldPos, Quaternion.identity);
                    }

                } else if (stars == 0 && (isPlanting || isWatering)) 
                {
                    noStarsWarning.SetActive(true);
                }
            }  else {
                Debug.Log("Clicked outside valid range!");
                isPlanting = false;
                isWatering = false;
                isHarvesting = false;
            }

            Debug.Log("Clicked on tile: " + clickedTile.name + " at position: " + tilePosition);
        }
    }

    void ChangeTileSprite(Vector3Int tilePosition, TileState state)
    {
        if (state == TileState.Empty) {
            overlayTilemap.SetTile(tilePosition, null);
            currentTileStates.Remove(tilePosition);
            AddStars(5);
        }
        else if (tileStateToSprite.ContainsKey(state)) {
            Tile newTile = ScriptableObject.CreateInstance<Tile>();
            newTile.sprite = tileStateToSprite[state];
            overlayTilemap.SetTile(tilePosition, newTile);
        }

        currentTileStates[tilePosition] = state;

        UserData userData = FileStorage.LoadData();

        // Find existing tile to keep the original plantedAt
        TileData existing = userData.tileStates.Find(t => t.x == tilePosition.x && t.y == tilePosition.y);

        TileData newTileData = new TileData
        {
            x = tilePosition.x,
            y = tilePosition.y,
            state = state,
            // If planting, use current time; otherwise keep existing value
            plantedAt = (state == TileState.Planted)
                ? System.DateTime.UtcNow.ToString("o")
                : existing?.plantedAt
        };

        userData.tileStates.RemoveAll(t => t.x == tilePosition.x && t.y == tilePosition.y);
        userData.tileStates.Add(newTileData);

        FileStorage.SaveData(userData);

        if (state == TileState.Planted || state == TileState.Watered) 
        {
            AddStars(-1);
        } 
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
        UpdateHarvestStates();

        UserData userData = FileStorage.LoadData();
        foreach (TileData data in userData.tileStates)
        {
            Vector3Int pos = new Vector3Int(data.x, data.y, 0);
            ChangeTileSprite(pos, data.state);
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

    void OnHarvestButtonClick()
    {
        Debug.Log("Button clicked!!!!!");
        isHarvesting = true;

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

    void UpdateHarvestStates()
    {
        UserData userData = FileStorage.LoadData();
        bool changesMade = false;

        foreach (TileData data in userData.tileStates)
        {
            if (data.state == TileState.Watered && !string.IsNullOrEmpty(data.plantedAt))
            {
                if (System.DateTime.TryParse(data.plantedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out System.DateTime plantedTime))
                {
                    if ((System.DateTime.UtcNow - plantedTime).TotalMinutes >= 1) // Change to TotalHours >= 24 for real use
                    {
                        data.state = TileState.HarvestReady;
                        changesMade = true;
                    }
                }
            }
        }

        if (changesMade)
        {
            FileStorage.SaveData(userData);
            LoadTileStates(); // Refresh visuals after updating states
        }
    }

}


