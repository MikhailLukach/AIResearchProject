using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class PopupMessageUI : MonoBehaviour
{
    public static PopupMessageUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI messageText; // or TMP_Text for TextMeshPro
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float displayDuration = 2f;

    private Coroutine currentMessageCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ShowMessage(string message)
    {
        if (currentMessageCoroutine != null)
            StopCoroutine(currentMessageCoroutine);

        currentMessageCoroutine = StartCoroutine(ShowMessageRoutine(message));
    }

    private IEnumerator ShowMessageRoutine(string message)
    {
        messageText.text = message;

        Color originalColor = messageText.color;
        originalColor.a = 1f;
        messageText.color = originalColor;

        yield return new WaitForSeconds(displayDuration);

        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            messageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        messageText.text = "";
    }
}
