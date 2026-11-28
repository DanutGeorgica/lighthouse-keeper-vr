
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class LedDisplay : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private TMP_Text displayText;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // =========================================================
    // RUNTIME INFORMATION
    // =========================================================

    [Header("Runtime Information (Play Mode)")]
    [SerializeField, TextArea(1, 4)]
    private string currentText;

    public string CurrentText => currentText;

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        // Automatically find a TMP component in this object's children
        // if one hasn't been assigned manually.
        if (displayText == null)
            displayText = GetComponentInChildren<TMP_Text>(true);

        if (displayText == null)
        {
            Debug.LogError(
                "[LedDisplay] No TextMeshPro component found!",
                this
            );

            enabled = false;
            return;
        }

        // Store the initial text.
        currentText = displayText.text;
    }

    // =========================================================
    // DISPLAY CONTROL
    // =========================================================

    /// <summary>
    /// Displays arbitrary text.
    /// Supports multiple lines using \n.
    /// </summary>
    public void SetText(string message)
    {
        if (displayText == null)
            return;

        message = message ?? string.Empty;

        // Avoid unnecessary updates and debug messages.
        if (displayText.text == message)
            return;

        displayText.text = message;
        currentText = message;

        if (enableDebugLogs)
        {
            Debug.Log(
                $"[{gameObject.name}] Display updated: {message}",
                this
            );
        }
    }

    /// <summary>
    /// Displays exactly two lines of text.
    /// </summary>
    public void SetLines(string firstLine, string secondLine)
    {
        SetText($"{firstLine}\n{secondLine}");
    }

    /// <summary>
    /// Clears the display.
    /// </summary>
    public void Clear()
    {
        SetText(string.Empty);
    }

    // =========================================================
    // INSPECTOR VALIDATION
    // =========================================================

    private void OnValidate()
    {
        if (displayText == null)
            displayText = GetComponentInChildren<TMP_Text>(true);
    }
}
