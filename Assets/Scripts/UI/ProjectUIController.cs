using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProjectUIController : MonoBehaviour
{
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private Button toggleInfoButton;
    [SerializeField] private Button reloadButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        infoPanel.SetActive(false);

        toggleInfoButton.onClick.AddListener(() =>
        {
            infoPanel.SetActive(!infoPanel.activeSelf);
        });

        reloadButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });

        quitButton.onClick.AddListener(() =>
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        });
    }
}
