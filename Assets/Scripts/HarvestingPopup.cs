using UnityEngine;

public class HarvestingPopup : MonoBehaviour
{
    public float floatSpeed = 1f; // Speed at which the prefab floats upwards
    public float fadeDuration = 1f; // Duration for the fade effect (1 second)
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 initialPosition;
    private float timer = 0f;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Save the initial position
        initialPosition = rectTransform.position;

        // Make sure the prefab is visible at the start
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Make the prefab float upwards
        rectTransform.position = Vector3.Lerp(initialPosition, initialPosition + Vector3.up, timer * floatSpeed);

        // Fade out the prefab
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
        }

        // Destroy the prefab after 1 second
        if (timer >= fadeDuration)
        {
            Destroy(gameObject);
        }
    }
}
