using UnityEngine;
using System.Collections;
using System.IO;

/// <summary>
/// Handles taking a screenshot of the game window.
/// </summary>
public class Screenshotter : MonoBehaviour
{
    // Static reference so it can be accessed from other scripts
    public static Screenshotter screenshotter;

    #region Public Variables
    public string m_ScreenshotKey = "s";
    public int m_ScaleFactor = 3;
    #endregion

    #region Private Variables
    private int m_ImageCount = 0;
    #endregion

    #region Constants
    private const string ImageCntKey = "IMAGE_CNT";
    #endregion

    private string picturesPath;

    void Awake () 
    {
        if (screenshotter != null) {
            Destroy(this.gameObject);
        } else {
            screenshotter = this.GetComponent<Screenshotter>();
            DontDestroyOnLoad(gameObject);
            m_ImageCount = PlayerPrefs.GetInt(ImageCntKey);
        }

        // Path: ~/Pictures/UnityScreenshots
        picturesPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Pictures", "UnityScreenshots");

        if (!Directory.Exists(picturesPath)) {
            Directory.CreateDirectory(picturesPath);
        }
    }

    void Update ()
    {
        if (Input.GetKeyDown(m_ScreenshotKey.ToLower()))
        {
            PlayerPrefs.SetInt(ImageCntKey, ++m_ImageCount);

            int width = Screen.width * m_ScaleFactor;
            int height = Screen.height * m_ScaleFactor;

            string fileName = $"Screenshot_{width}x{height}_{m_ImageCount}.png";
            string fullPath = Path.Combine(picturesPath, fileName);

            ScreenCapture.CaptureScreenshot(fullPath, m_ScaleFactor);
            Debug.Log("Screenshot saved to: " + fullPath);
        }
    }
}
