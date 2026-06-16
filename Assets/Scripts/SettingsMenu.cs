using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private const string MasterVolumeKey = "Settings.MasterVolume";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string ResolutionKey = "Settings.Resolution";

    [Header("Panel")]
    public GameObject panelRoot;
    public GameObject settingsPanel;
    [SerializeField] private string closeAnimationStateName = "Close_UI";

    [Header("Audio")]
    public Slider masterVolumeSlider;
    public TextMeshProUGUI masterVolumeText;

    [Header("Display")]
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;

    [Header("Buttons")]
    public Button closeButton;
    public Button exitButton;

    private Resolution[] resolutions;
    private bool isInitializing;
    private static int escapeCloseFrame = -1;
    private bool savedCursorVisible;
    private CursorLockMode savedCursorLockState;
    private bool hasSavedCursorState;
    private bool isClosing;

    void Awake()
    {
        SetupPanelReferences();
        EnterSettingsInputMode();
        if (!IsOpen())
        {
            RestoreCursorState();
        }

        SetupResolutionDropdown();
        LoadSettings();
        RegisterUIEvents();
    }

    void OnDisable()
    {
        RestoreCursorState();
        isClosing = false;
    }

    private void SetupPanelReferences()
    {
        if (settingsPanel == null)
        {
            settingsPanel = gameObject;
        }

        if (panelRoot == null)
        {
            panelRoot = settingsPanel;

            if (settingsPanel.transform.parent != null)
            {
                panelRoot = settingsPanel.transform.parent.gameObject;
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && IsOpen())
        {
            Close();
            escapeCloseFrame = Time.frameCount;
        }
    }

    public void Open()
    {
        SetupPanelReferences();
        isClosing = false;
        panelRoot.SetActive(true);
        settingsPanel.SetActive(true);
        EnsureLegacyMouseInputModule();
        EnterSettingsInputMode();
    }

    public void Close()
    {
        isClosing = true;
        Animator panelAnimator = panelRoot != null ? panelRoot.GetComponent<Animator>() : null;
        if (panelAnimator != null && !string.IsNullOrEmpty(closeAnimationStateName))
        {
            panelAnimator.Play(closeAnimationStateName, 0, 0f);
        }
        else
        {
            settingsPanel.SetActive(false);
            if (panelRoot != settingsPanel)
            {
                panelRoot.SetActive(false);
            }

            isClosing = false;
        }

        RestoreCursorState();
    }

    public void TogglePanel()
    {
        if (IsOpen())
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public bool IsOpen()
    {
        return !isClosing &&
               panelRoot != null && panelRoot.activeInHierarchy &&
               settingsPanel != null && settingsPanel.activeInHierarchy;
    }

    public bool IsClosing()
    {
        return isClosing;
    }

    public static bool EscapeClosedSettingsThisFrame()
    {
        return escapeCloseFrame == Time.frameCount;
    }

    public static bool AnySettingsMenuOpen()
    {
        SettingsMenu[] menus = FindObjectsOfType<SettingsMenu>();
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i].IsOpen())
            {
                return true;
            }
        }

        return false;
    }

    public void SetMasterVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        UpdateMasterVolumeText(value);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutions == null || resolutionIndex < 0 || resolutionIndex >= resolutions.Length)
        {
            return;
        }

        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt(ResolutionKey, resolutionIndex);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void ResetDefaults()
    {
        SetMasterVolume(1f);
        SetFullscreen(true);

        int currentResolutionIndex = FindCurrentResolutionIndex();
        SetResolution(currentResolutionIndex);

        isInitializing = true;
        if (masterVolumeSlider != null) masterVolumeSlider.value = 1f;
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;
        if (resolutionDropdown != null && resolutions.Length > 0) resolutionDropdown.value = currentResolutionIndex;
        isInitializing = false;

        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        isInitializing = true;

        float masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        bool isFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        int resolutionIndex = PlayerPrefs.GetInt(ResolutionKey, FindCurrentResolutionIndex());

        AudioListener.volume = masterVolume;
        Screen.fullScreen = isFullscreen;

        if (resolutions != null && resolutionIndex >= 0 && resolutionIndex < resolutions.Length)
        {
            Resolution resolution = resolutions[resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, isFullscreen);
        }

        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVolume;
        if (fullscreenToggle != null) fullscreenToggle.isOn = isFullscreen;
        if (resolutionDropdown != null && resolutions.Length > 0) resolutionDropdown.value = Mathf.Clamp(resolutionIndex, 0, resolutions.Length - 1);

        UpdateMasterVolumeText(masterVolume);
        isInitializing = false;
    }

    private void RegisterUIEvents()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(value =>
            {
                if (!isInitializing) SetMasterVolume(value);
            });
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(value =>
            {
                if (!isInitializing) SetFullscreen(value);
            });
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.AddListener(value =>
            {
                if (!isInitializing) SetResolution(value);
            });
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitGame);
        }
    }

    private void EnterSettingsInputMode()
    {
        if (!hasSavedCursorState)
        {
            savedCursorVisible = Cursor.visible;
            savedCursorLockState = Cursor.lockState;
            hasSavedCursorState = true;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void EnsureLegacyMouseInputModule()
    {
        if (EventSystem.current == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
            return;
        }

        StandaloneInputModule standaloneInputModule = EventSystem.current.GetComponent<StandaloneInputModule>();
        if (standaloneInputModule == null)
        {
            standaloneInputModule = EventSystem.current.gameObject.AddComponent<StandaloneInputModule>();
        }

        standaloneInputModule.enabled = true;
    }

    private void RestoreCursorState()
    {
        if (!hasSavedCursorState || AnySettingsMenuOpen())
        {
            return;
        }

        Cursor.visible = savedCursorVisible;
        Cursor.lockState = savedCursorLockState;
        hasSavedCursorState = false;
    }

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null)
        {
            resolutions = Screen.resolutions;
            return;
        }

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        for (int i = 0; i < resolutions.Length; i++)
        {
            Resolution resolution = resolutions[i];
            options.Add(resolution.width + " x " + resolution.height);
        }

        if (options.Count == 0)
        {
            options.Add(Screen.width + " x " + Screen.height);
        }

        resolutionDropdown.AddOptions(options);
    }

    private int FindCurrentResolutionIndex()
    {
        if (resolutions == null || resolutions.Length == 0)
        {
            resolutions = Screen.resolutions;
        }

        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                return i;
            }
        }

        return Mathf.Max(0, resolutions.Length - 1);
    }

    private void UpdateMasterVolumeText(float value)
    {
        if (masterVolumeText != null)
        {
            masterVolumeText.text = Mathf.RoundToInt(value * 100f) + "%";
        }
    }
}
