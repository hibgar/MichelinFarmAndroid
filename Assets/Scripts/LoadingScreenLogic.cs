using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    public Image loadingImage; // Assign your Image in the Inspector
    public float displayDuration = 5f; // Time to show the image in seconds

    void Start()
    {
        if (loadingImage != null)
        {
            StartCoroutine(ShowLoadingScreen());
        }
    }

    System.Collections.IEnumerator ShowLoadingScreen()
    {
        loadingImage.gameObject.SetActive(true); // Show the image
        yield return new WaitForSeconds(displayDuration); // Wait for the specified duration
        loadingImage.gameObject.SetActive(false); // Hide the image
    }
}
