using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerHUDManager : MonoBehaviour
{
    public GameObject playerHUDPrefab;
    public float activePlayerScale = 1f;
    public float inactivePlayerScale = 0.75f;
    public float spacing = 10f;
    public float transitionDuration = 0.3f;

    private VerticalLayoutGroup layoutGroup;
    private int currentActivePlayerIndex = 0;
    private GameObject[] playerHUDs;
    
    void Start()
    {
        EnsureLayoutGroupInitialized();
    }

    public void UpdatePlayerOrder(List<string> playerOrder)
    {
        if (playerHUDs == null || playerHUDs.Length != playerOrder.Count)
        {
            InitializePlayerHUDs(playerOrder.Count);
        }

        UpdateHUDLayout();
    }

    public void UpdateActivePlayer(int index)
    {
        // Check if HUDs are initialized
        if (playerHUDs == null)
        {
            Debug.LogError("Player HUDs not initialized. Call InitializePlayerHUDs first.");
            return;
        }

        if (index < 0 || index >= playerHUDs.Length)
        {
            Debug.LogError($"Invalid player index: {index}. Must be between 0 and {playerHUDs.Length - 1}");
            return;
        }

        SetActivePlayerAnimated(index);
    }

    public void InitializePlayerHUDs(int playerCount)
    {
        EnsureLayoutGroupInitialized();

        // Clear existing HUDs
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        playerHUDs = new GameObject[playerCount];
        float yOffset = 0;

        for (int i = 0; i < playerCount; i++)
        {
            GameObject hud = Instantiate(playerHUDPrefab, transform);
            playerHUDs[i] = hud;
            
            RectTransform hudRT = hud.GetComponent<RectTransform>();
            if (hudRT != null)
            {
                // Configure anchors for vertical layout
                hudRT.anchorMin = new Vector2(0, 1);
                hudRT.anchorMax = new Vector2(1, 1);
                hudRT.pivot = new Vector2(0.5f, 1);
                hudRT.sizeDelta = new Vector2(0, 60);
                
                // Position each HUD vertically with spacing
                yOffset -= (i > 0 ? layoutGroup.spacing + hudRT.sizeDelta.y : 0);
                hudRT.anchoredPosition = new Vector2(0, yOffset);
            }
            
            hud.transform.localScale = i == currentActivePlayerIndex ? 
                Vector3.one * activePlayerScale : 
                Vector3.one * inactivePlayerScale;
        }

        // Force immediate layout update
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
    }

    private void EnsureLayoutGroupInitialized()
    {
        // First check if we have a parent
        if (transform.parent == null)
        {
            Debug.LogWarning("PlayerHUDManager needs a parent object with VerticalLayoutGroup. Creating one.");
            
            // Create parent container
            GameObject container = new GameObject("HUD_Container");
            container.transform.SetParent(transform.parent, false);
            
            // Move this object to be a child of the new container
            transform.SetParent(container.transform, false);
        }

        // Now we can safely look for or add the VerticalLayoutGroup
        if (layoutGroup == null)
        {
            layoutGroup = transform.parent.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = transform.parent.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            // Configure VerticalLayoutGroup
            layoutGroup.spacing = 20f;
            layoutGroup.padding = new RectOffset(10, 10, 20, 20);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlHeight = false; // Changed to false
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;

            RectTransform containerRT = layoutGroup.GetComponent<RectTransform>();
            if (containerRT != null)
            {
                containerRT.anchorMin = new Vector2(0, 1);
                containerRT.anchorMax = new Vector2(0, 1);
                containerRT.pivot = new Vector2(0, 1);
                containerRT.sizeDelta = new Vector2(300, 500);
                containerRT.anchoredPosition = new Vector2(50, -50);
            }
        }
    }

    private void UpdateHUDLayout()
    {
        EnsureLayoutGroupInitialized();

        // Desactivar temporalmente el layout group para hacer cambios manuales
        layoutGroup.enabled = false;

        // Mover el HUD activo a su posición correcta
        playerHUDs[currentActivePlayerIndex].transform.SetSiblingIndex(currentActivePlayerIndex);

        // Volver a activar el layout group
        layoutGroup.enabled = true;

        // Forzar la actualización del layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
    }

    private IEnumerator SmoothTransition(int newActiveIndex)
    {
        // Guardar referencias a las escalas originales
        Vector3[] startScales = new Vector3[playerHUDs.Length];
        for (int i = 0; i < playerHUDs.Length; i++)
        {
            startScales[i] = playerHUDs[i].transform.localScale;
        }

        float elapsed = 0f;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            
            for (int i = 0; i < playerHUDs.Length; i++)
            {
                Vector3 targetScale = i == newActiveIndex ? 
                    Vector3.one * activePlayerScale : 
                    Vector3.one * inactivePlayerScale;
                    
                playerHUDs[i].transform.localScale = 
                    Vector3.Lerp(startScales[i], targetScale, t);
            }
            
            yield return null;
        }

        currentActivePlayerIndex = newActiveIndex;
        UpdateHUDLayout();
    }

    // Llamar esto en lugar de SetActivePlayer para la versión animada
    public void SetActivePlayerAnimated(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerHUDs.Length) return;
        StartCoroutine(SmoothTransition(playerIndex));
    }

}