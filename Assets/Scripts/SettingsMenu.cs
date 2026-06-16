using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private const string MasterVolumeKey = "Settings.MasterVolume";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string ResolutionKey = "Settings.Resolution";

    [Header("Panel")]
    public GameObject settingsPanel;

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

    void Awake()
    {
        if (settingsPanel == null)
        {
            settingsPanel = gameObject;
        }

        SetupResolutionDropdown();
        LoadSettings();
        RegisterUIEvents();
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
        settingsPanel.SetActive(true);
    }

    public void Close()
    {
        settingsPanel.SetActive(false);
    }

    public void TogglePanel()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public bool IsOpen()
    {
        return settingsPanel != null && settingsPanel.activeInHierarchy;
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
