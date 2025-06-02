using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PlayerHUDManager : MonoBehaviour
{
    [SerializeField] private VerticalLayoutGroup layoutGroup;
    private Dictionary<string, PlayerHUD> playerHUDs = new Dictionary<string, PlayerHUD>();

    public float activePlayerScale = 1f;
    public float inactivePlayerScale = 0.75f;
    public float spacing = 10f;
    public float transitionDuration = 0.3f;

    private int currentActivePlayerIndex = 0;
    
    void Start()
    {
        EnsureLayoutGroupInitialized();
    }

    public void UpdatePlayerOrder(List<string> playerOrder)
    {
        if (playerHUDs == null || playerHUDs.Count != playerOrder.Count)
        {
            Debug.LogWarning($"HUD count mismatch. Expected: {playerOrder.Count}, Actual: {playerHUDs?.Count}");
            return;
        }

        // Crear un nuevo diccionario con el orden actualizado
        Dictionary<string, PlayerHUD> reorderedHUDs = new Dictionary<string, PlayerHUD>();
        
        // Reordenar los HUDs según el nuevo orden
        for (int i = 0; i < playerOrder.Count; i++)
        {
            string playerName = playerOrder[i];
            if (playerHUDs.TryGetValue(playerName, out PlayerHUD hud))
            {
                hud.transform.SetSiblingIndex(i);
                reorderedHUDs.Add(playerName, hud);
            }
            else
            {
                Debug.LogError($"Missing HUD for player {playerName}");
                return;
            }
        }

        // Actualizar el diccionario con el nuevo orden
        playerHUDs = reorderedHUDs;
        
        // Resetear el índice activo y actualizar el layout
        currentActivePlayerIndex = 0;
        UpdateHUDLayout();
    }

    public void UpdateActivePlayer(int index)
    {
        Debug.Log($"Existe: {playerHUDs.Count}");
        // Check if HUDs are initialized
        if (playerHUDs == null || playerHUDs.Count == 0)
        {
            Debug.LogError("No Player HUDs initialized.");
            return;
        }

        if (index < 0 || index >= playerHUDs.Count)
        {
            Debug.LogError($"Invalid player index: {index}. Must be between 0 and {playerHUDs.Count - 1}");
            return;
        }

        SetActivePlayerAnimated(index);
    }

    public void InitializePlayerHUD(string playerName, int characterId, Character character)
    {
        if (playerHUDs.ContainsKey(playerName))
        {
            Debug.LogWarning($"HUD already exists for player {playerName}");
            return;
        }

        GameObject hudPrefab = GameManager.Instance.GetHUDPrefab(characterId);
        GameObject hudGO = Instantiate(hudPrefab, layoutGroup.transform); // Instancia como hijo del VerticalContainer
        PlayerHUD hud = hudGO.GetComponent<PlayerHUD>();

        if (hud != null)
        {
            hud.Initialize(playerName, character);
            playerHUDs.Add(playerName, hud);
            character.SetHUD(hud);

            RectTransform hudRT = hudGO.GetComponent<RectTransform>();
            if (hudRT != null)
            {
                hudRT.anchorMin = new Vector2(0, 1);
                hudRT.anchorMax = new Vector2(1, 1);
                hudRT.pivot = new Vector2(0.5f, 1);
                hudRT.sizeDelta = new Vector2(0, 100);
                hudRT.localScale = Vector3.one * inactivePlayerScale;
            }

            LayoutElement layoutElement = hudGO.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = hudGO.AddComponent<LayoutElement>();
            layoutElement.minHeight = 100;
            layoutElement.preferredHeight = 100;
            layoutElement.flexibleHeight = 0;
            layoutElement.flexibleWidth = 1;
        }
    }

    private void EnsureLayoutGroupInitialized()
    {
        if (transform.parent == null)
        {
            GameObject container = new GameObject("HUD_Container");
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                canvas = new GameObject("Canvas").AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.gameObject.AddComponent<CanvasScaler>();
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            
            container.transform.SetParent(canvas.transform, false);
            
            RectTransform containerRT = container.AddComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0, 0);
            containerRT.anchorMax = new Vector2(0.3f, 1);
            containerRT.pivot = new Vector2(0, 1);
            containerRT.anchoredPosition = new Vector2(20, -20);
            
            layoutGroup = container.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                DestroyImmediate(layoutGroup);
            }

            transform.SetParent(container.transform, false);
        }
    }

    private void UpdateHUDLayout()
    {
        layoutGroup.enabled = false;

        // Ahora usamos el diccionario reordenado para actualizar las escalas
        int index = 0;
        foreach (var kvp in playerHUDs)
        {
            var hud = kvp.Value;
            hud.transform.localScale = index == currentActivePlayerIndex ? 
                Vector3.one * activePlayerScale : 
                Vector3.one * inactivePlayerScale;
            index++;
        }

        layoutGroup.enabled = true;
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
    }

    private IEnumerator SmoothTransition(int newActiveIndex)
    {
        // Store original scales
        Dictionary<string, Vector3> startScales = new Dictionary<string, Vector3>();
        foreach (var hud in playerHUDs)
        {
            startScales[hud.Key] = hud.Value.transform.localScale;
        }

        float elapsed = 0f;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            
            int currentIndex = 0;
            foreach (var hud in playerHUDs)
            {
                Vector3 targetScale = currentIndex == newActiveIndex ? 
                    Vector3.one * activePlayerScale : 
                    Vector3.one * inactivePlayerScale;
                    
                hud.Value.transform.localScale = 
                    Vector3.Lerp(startScales[hud.Key], targetScale, t);
                
                currentIndex++;
            }
            
            yield return null;
        }

        currentActivePlayerIndex = newActiveIndex;
        UpdateHUDLayout();
    }

    // Llamar esto en lugar de SetActivePlayer para la versión animada
    public void SetActivePlayerAnimated(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerHUDs.Count) return;
        StartCoroutine(SmoothTransition(playerIndex));
    }

}