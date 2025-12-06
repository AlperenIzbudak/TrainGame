using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("Name Input")]
    public TMP_InputField nameInput;
    public TMP_Text nameErrorText;

    [Header("Character Selection")]
    // 6 tane karakter butonunu inspector'dan buraya sürükle
    public Button[] characterButtons;

    [Header("Play")]
    public Button playButton;

    [Header("Scene")]
    // Oyun sahnenin ismi (Build Settings'te ne yazıyorsa aynısı)
    public string gameSceneName = "Board" ;

    int selectedCharacterId = -1;

    void Start()
    {
        // Başlangıçta
        selectedCharacterId = -1;

        if (playButton != null)
            playButton.interactable = false;

        if (nameErrorText != null)
            nameErrorText.text = "";

        // İstersen input'a listener bağlayabilirsin:
        if (nameInput != null)
            nameInput.onValueChanged.AddListener(OnNameChanged);

        // Karakter butonlarının renklerini resetle
        ResetCharacterButtonColors();
    }

    // InputField → OnValueChanged event'ine bağla
    public void OnNameChanged(string newText)
    {
        ValidateAll();
    }

    // Her karakter butonunun OnClick'ine Inspector'dan bu fonksiyonu bağla
    // ve "Argument" olarak butona özel ID (0..5) ver.
    public void OnCharacterSelected(int id)
    {
        selectedCharacterId = id;
        HighlightSelectedCharacterButton();
        ValidateAll();
    }

    // Play butonu → OnClick
    public void OnPlayClicked()
    {
        Debug.Log("[MainMenuUI] Play clicked");

        if (!IsNameValid() || selectedCharacterId < 0)
        {
            Debug.Log("[MainMenuUI] Name or character invalid, not starting game.");
            return;
        }

        string trimmedName = nameInput.text.Trim();

        GameSetupData.playerName = trimmedName;
        GameSetupData.selectedCharacterId = selectedCharacterId;

        Debug.Log("[MainMenuUI] Loading scene: " + gameSceneName);
        SceneManager.LoadScene(gameSceneName);
    }


    // ------------ Helpers ------------

    void ValidateAll()
    {
        bool validName = IsNameValid();
        bool hasCharacter = selectedCharacterId >= 0;

        if (nameErrorText != null)
        {
            if (!validName)
                nameErrorText.text = "Name must be at least 1 character";
            else
                nameErrorText.text = "";
        }

        if (playButton != null)
            playButton.interactable = validName && hasCharacter;
    }

    bool IsNameValid()
    {
        if (nameInput == null) return false;
        string t = nameInput.text;
        if (string.IsNullOrWhiteSpace(t))
            return false;

        // İstersen max uzunluk koy:
        if (t.Trim().Length > 16)
            t = t.Trim().Substring(0, 16);

        return true;
    }

    void ResetCharacterButtonColors()
    {
        if (characterButtons == null) return;

        foreach (var btn in characterButtons)
        {
            if (btn == null) continue;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            btn.colors = colors;
        }
    }

    void HighlightSelectedCharacterButton()
    {
        if (characterButtons == null) return;

        for (int i = 0; i < characterButtons.Length; i++)
        {
            var btn = characterButtons[i];
            if (btn == null) continue;

            var colors = btn.colors;
            if (i == selectedCharacterId)
                colors.normalColor = new Color(0.8f, 0.8f, 1f);  // hafif mavi ton
            else
                colors.normalColor = Color.white;

            btn.colors = colors;
        }
    }
}
