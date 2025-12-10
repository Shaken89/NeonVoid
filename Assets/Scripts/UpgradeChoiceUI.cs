using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI for displaying upgrade choices (Nova Drift style)
/// </summary>
public class UpgradeChoiceUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private UpgradeCard[] upgradeCards;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollCostText;

    [Header("Settings")]
    [SerializeField] private int initialRerollCost = 50;
    [SerializeField] private int rerollCostIncrease = 25;

    private List<ModularUpgradeSystem.UpgradeDefinition> currentChoices;
    private int currentRerollCost;
    private int rerollsUsed = 0;

    [System.Serializable]
    public class UpgradeCard
    {
        public GameObject cardObject;
        public Image background;
        public Image icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI rarityText;
        public TextMeshProUGUI stackText;
        public Button selectButton;
        public GameObject synergyIndicator;
        public TextMeshProUGUI synergyText;
    }

    private void Start()
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);

        if (rerollButton != null)
            rerollButton.onClick.AddListener(OnReroll);

        currentRerollCost = initialRerollCost;
    }

    public void ShowChoices(List<ModularUpgradeSystem.UpgradeDefinition> choices)
    {
        currentChoices = choices;
        
        if (choicePanel != null)
            choicePanel.SetActive(true);

        Time.timeScale = 0f; // Pause game

        for (int i = 0; i < upgradeCards.Length; i++)
        {
            if (i < choices.Count)
            {
                SetupCard(upgradeCards[i], choices[i], i);
            }
            else
            {
                upgradeCards[i].cardObject.SetActive(false);
            }
        }

        UpdateLevelText();
        UpdateRerollButton();
    }

    private void SetupCard(UpgradeCard card, ModularUpgradeSystem.UpgradeDefinition upgrade, int index)
    {
        card.cardObject.SetActive(true);

        // Icon
        if (card.icon != null && upgrade.icon != null)
            card.icon.sprite = upgrade.icon;

        // Name
        if (card.nameText != null)
            card.nameText.text = upgrade.displayName;

        // Description
        if (card.descriptionText != null)
            card.descriptionText.text = upgrade.description;

        // Rarity
        if (card.rarityText != null)
        {
            card.rarityText.text = upgrade.rarity.ToString().ToUpper();
            card.rarityText.color = GetRarityColor(upgrade.rarity);
        }

        // Background color
        if (card.background != null)
        {
            Color rarityColor = GetRarityColor(upgrade.rarity);
            rarityColor.a = 0.2f;
            card.background.color = rarityColor;
        }

        // Stack count
        if (card.stackText != null)
        {
            int stacks = ModularUpgradeSystem.Instance.GetUpgradeStack(upgrade.id);
            if (stacks > 0)
            {
                card.stackText.gameObject.SetActive(true);
                card.stackText.text = $"x{stacks + 1}";
            }
            else
            {
                card.stackText.gameObject.SetActive(false);
            }
        }

        // Synergies
        if (card.synergyIndicator != null)
        {
            List<string> activeSynergies = CheckSynergies(upgrade);
            if (activeSynergies.Count > 0)
            {
                card.synergyIndicator.SetActive(true);
                if (card.synergyText != null)
                {
                    card.synergyText.text = "SYNERGY: " + string.Join(", ", activeSynergies);
                }
            }
            else
            {
                card.synergyIndicator.SetActive(false);
            }
        }

        // Button
        if (card.selectButton != null)
        {
            card.selectButton.onClick.RemoveAllListeners();
            card.selectButton.onClick.AddListener(() => OnUpgradeSelected(upgrade.id));
        }
    }

    private List<string> CheckSynergies(ModularUpgradeSystem.UpgradeDefinition upgrade)
    {
        List<string> synergies = new List<string>();

        foreach (var synergy in upgrade.synergies)
        {
            if (ModularUpgradeSystem.Instance.HasUpgrade(synergy.otherUpgradeId))
            {
                synergies.Add(synergy.synergyDescription);
            }
        }

        return synergies;
    }

    private Color GetRarityColor(ModularUpgradeSystem.UpgradeRarity rarity)
    {
        return rarity switch
        {
            ModularUpgradeSystem.UpgradeRarity.Common => Color.white,
            ModularUpgradeSystem.UpgradeRarity.Uncommon => Color.green,
            ModularUpgradeSystem.UpgradeRarity.Rare => Color.cyan,
            ModularUpgradeSystem.UpgradeRarity.Epic => new Color(0.7f, 0f, 1f),
            ModularUpgradeSystem.UpgradeRarity.Legendary => Color.yellow,
            _ => Color.white
        };
    }

    private void OnUpgradeSelected(string upgradeId)
    {
        ModularUpgradeSystem.Instance.ApplyUpgrade(upgradeId);
        HideChoices();
    }

    private void OnReroll()
    {
        HUDController hud = FindFirstObjectByType<HUDController>();
        if (hud != null && hud.GetScore() >= currentRerollCost)
        {
            hud.UpdateScore(-currentRerollCost);
            rerollsUsed++;
            currentRerollCost += rerollCostIncrease;

            // Request new choices
            ModularUpgradeSystem.Instance.LevelUp();
        }
    }

    private void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text = $"LEVEL {ModularUpgradeSystem.Instance.GetPlayerLevel()}";
        }
    }

    private void UpdateRerollButton()
    {
        if (rerollButton != null && rerollCostText != null)
        {
            HUDController hud = FindFirstObjectByType<HUDController>();
            int currentScore = hud != null ? hud.GetScore() : 0;

            rerollButton.interactable = currentScore >= currentRerollCost;
            rerollCostText.text = $"Reroll ({currentRerollCost})";
        }
    }

    private void HideChoices()
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);

        Time.timeScale = 1f; // Resume game
        rerollsUsed = 0;
        currentRerollCost = initialRerollCost;
    }
}
