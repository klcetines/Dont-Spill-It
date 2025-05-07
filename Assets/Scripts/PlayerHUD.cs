using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Slider liquidSlider;
    [SerializeField] private Slider healthSlider;

    private Character linkedCharacter;

    public void Initialize(string playerName, Character character)
    {
        if (playerNameText != null)
            playerNameText.text = playerName;
            
        linkedCharacter = character;
    }

    public void UpdateLiquid(float amount)
    {
        if (liquidSlider != null)
            liquidSlider.value = amount;
    }

    public void UpdateHealth(float amount)
    {
        if (healthSlider != null)
            healthSlider.value = amount;
    }
}