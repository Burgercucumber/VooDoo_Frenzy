using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class SimpleBattleAnimator : NetworkBehaviour
{
    [Header("Prefabs de Animaciones de Ataque")]
    [SerializeField] private GameObject botonAttackPrefab;
    [SerializeField] private GameObject alfilerAttackPrefab;
    [SerializeField] private GameObject telaAttackPrefab;
    [SerializeField] private GameObject algodonAttackPrefab;

    [Header("Prefabs de Resultado")]
    [SerializeField] private GameObject victoryPrefab;
    [SerializeField] private GameObject defeatPrefab;
    [SerializeField] private GameObject drawPrefab;

    [Header("Área de Animaciones")]
    [SerializeField] private Transform animationPanel;

    [Header("Configuración de Renderizado")]
    [SerializeField] private Canvas animationCanvas; // Canvas específico para animaciones
    [SerializeField] private int canvasSortingOrder = 1000; // Muy alto para estar encima
    [SerializeField] private bool useWorldSpaceCanvas = false;

    [Header("Configuración de Tamaño")]
    [SerializeField] private Vector2 animationSize = new Vector2(300, 300); // Tamaño de las animaciones
    [SerializeField] private float scaleMultiplier = 1f; // Multiplicador de escala adicional

    private static SimpleBattleAnimator instance;
    public static SimpleBattleAnimator Instance => instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ValidatePrefabs();
        SetupAnimationCanvas();
    }

    private void SetupAnimationCanvas()
    {
        // Si no hay canvas asignado, intentar encontrar o crear uno
        if (animationCanvas == null)
        {
            // Buscar canvas padre del panel de animación
            if (animationPanel != null)
            {
                animationCanvas = animationPanel.GetComponentInParent<Canvas>();
            }

            // Si aún no hay canvas, crear uno
            if (animationCanvas == null)
            {
                Debug.LogWarning("[SimpleBattleAnimator] Creando Canvas de animación automáticamente");
                GameObject canvasGO = new GameObject("AnimationCanvas");
                animationCanvas = canvasGO.AddComponent<Canvas>();
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();

                animationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                animationCanvas.sortingOrder = canvasSortingOrder;

                // Crear panel de animación si no existe
                if (animationPanel == null)
                {
                    GameObject panelGO = new GameObject("AnimationPanel");
                    panelGO.transform.SetParent(canvasGO.transform, false);
                    animationPanel = panelGO.transform;

                    // Configurar RectTransform para pantalla completa
                    RectTransform rectTransform = panelGO.AddComponent<RectTransform>();
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.sizeDelta = Vector2.zero;
                    rectTransform.anchoredPosition = Vector2.zero;
                }
            }
        }

        // Configurar canvas para animaciones
        if (animationCanvas != null)
        {
            if (useWorldSpaceCanvas)
            {
                animationCanvas.renderMode = RenderMode.WorldSpace;
                animationCanvas.worldCamera = Camera.main;
            }
            else
            {
                animationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            animationCanvas.sortingOrder = canvasSortingOrder;

            Debug.Log($"[SimpleBattleAnimator] Canvas configurado: {animationCanvas.renderMode}, SortingOrder: {animationCanvas.sortingOrder}");
        }
    }

    private void ValidatePrefabs()
    {
        Debug.Log("[SimpleBattleAnimator] Validando prefabs...");

        if (botonAttackPrefab == null) Debug.LogError("[SimpleBattleAnimator] botonAttackPrefab no está asignado!");
        if (alfilerAttackPrefab == null) Debug.LogError("[SimpleBattleAnimator] alfilerAttackPrefab no está asignado!");
        if (telaAttackPrefab == null) Debug.LogError("[SimpleBattleAnimator] telaAttackPrefab no está asignado!");
        if (algodonAttackPrefab == null) Debug.LogError("[SimpleBattleAnimator] algodonAttackPrefab no está asignado!");

        if (victoryPrefab == null) Debug.LogError("[SimpleBattleAnimator] victoryPrefab no está asignado!");
        if (defeatPrefab == null) Debug.LogError("[SimpleBattleAnimator] defeatPrefab no está asignado!");
        if (drawPrefab == null) Debug.LogError("[SimpleBattleAnimator] drawPrefab no está asignado!");

        if (animationPanel == null) Debug.LogError("[SimpleBattleAnimator] animationPanel no está asignado!");

        Debug.Log("[SimpleBattleAnimator] Validación de prefabs completada.");
    }

    [Server]
    public void PlayBattleAnimations(CardData.ElementType winnerElement, CardBattleLogic.BattleResult result)
    {
        Debug.Log($"[Server] Iniciando animaciones de batalla. Elemento ganador: {winnerElement}, Resultado: {result}");
        RpcPlayBattleAnimations(winnerElement, result);
    }

    [ClientRpc]
    private void RpcPlayBattleAnimations(CardData.ElementType winnerElement, CardBattleLogic.BattleResult result)
    {
        Debug.Log($"[Client] Recibido RPC para animaciones. Elemento: {winnerElement}, Resultado: {result}");
        StartCoroutine(PlayAnimationSequence(winnerElement, result));
    }

    private IEnumerator PlayAnimationSequence(CardData.ElementType winnerElement, CardBattleLogic.BattleResult result)
    {
        Debug.Log("[Client] Iniciando secuencia de animación...");

        if (animationPanel == null)
        {
            Debug.LogError("[Client] animationPanel es null! No se pueden reproducir animaciones.");
            yield break;
        }

        // Asegurar que el canvas esté configurado
        SetupAnimationCanvas();

        // 1. Primero mostrar animación de ataque (si hay ganador)
        if (result != CardBattleLogic.BattleResult.Draw)
        {
            Debug.Log($"[Client] Reproduciendo animación de ataque para elemento: {winnerElement}");
            GameObject attackPrefab = GetAttackPrefab(winnerElement);

            if (attackPrefab != null)
            {
                GameObject attackInstance = Instantiate(attackPrefab, animationPanel);
                ConfigureAnimationObject(attackInstance, "Attack");

                Debug.Log($"[Client] Animación de ataque instanciada: {attackInstance.name}");
                yield return StartCoroutine(WaitForAnimation(attackInstance));

                Debug.Log("[Client] Animación de ataque completada, destruyendo objeto...");
                Destroy(attackInstance);
            }
            else
            {
                Debug.LogWarning($"[Client] No se encontró prefab de ataque para elemento: {winnerElement}");
            }
        }

        // 2. Luego mostrar resultado
        Debug.Log($"[Client] Reproduciendo animación de resultado: {result}");
        GameObject resultPrefab = GetResultPrefab(result);

        if (resultPrefab != null)
        {
            GameObject resultInstance = Instantiate(resultPrefab, animationPanel);
            ConfigureAnimationObject(resultInstance, "Result");

            Debug.Log($"[Client] Animación de resultado instanciada: {resultInstance.name}");
            yield return StartCoroutine(WaitForAnimation(resultInstance));

            Debug.Log("[Client] Animación de resultado completada, destruyendo objeto...");
            Destroy(resultInstance);
        }
        else
        {
            Debug.LogWarning($"[Client] No se encontró prefab de resultado para: {result}");
        }

        Debug.Log("[Client] Secuencia de animación completada.");
    }

    private void ConfigureAnimationObject(GameObject animObject, string type)
    {
        if (animObject == null) return;

        Debug.Log($"[Client] Configurando objeto de animación: {animObject.name} (Tipo: {type})");

        // Primero determinar si necesitamos convertir a UI o mantener como world object
        bool isInUICanvas = IsInUICanvas();

        if (isInUICanvas)
        {
            ConvertToUIObject(animObject);
        }
        else
        {
            ConfigureWorldSpaceObject(animObject);
        }

        // Asegurar visibilidad
        animObject.SetActive(true);

        // Debug final
        DebugObjectConfiguration(animObject, type);
    }

    private bool IsInUICanvas()
    {
        if (animationCanvas != null)
        {
            return animationCanvas.renderMode == RenderMode.ScreenSpaceOverlay ||
                   animationCanvas.renderMode == RenderMode.ScreenSpaceCamera;
        }
        return true; // Por defecto asumir UI
    }

    private void ConvertToUIObject(GameObject animObject)
    {
        Debug.Log("[Client] Convirtiendo a objeto UI...");

        // Agregar RectTransform si no existe
        RectTransform rectTransform = animObject.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = animObject.AddComponent<RectTransform>();
        }

        // Configurar RectTransform para centrado con tamaño controlado
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = animationSize; // Usar tamaño configurable
        rectTransform.localScale = Vector3.one * scaleMultiplier;

        Debug.Log($"[Client] RectTransform configurado - Size: {animationSize}, Scale: {scaleMultiplier}");

        // Convertir SpriteRenderers a Image components
        ConvertSpriteRenderersToImages(animObject);
    }

    private void ConvertSpriteRenderersToImages(GameObject obj)
    {
        SpriteRenderer[] spriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>();

        foreach (SpriteRenderer sr in spriteRenderers)
        {
            if (sr.sprite != null)
            {
                Debug.Log($"[Client] Convirtiendo SpriteRenderer a Image: {sr.name}");

                // Crear Image component
                Image image = sr.gameObject.AddComponent<Image>();
                image.sprite = sr.sprite;
                image.color = sr.color;
                image.preserveAspect = true;
                image.type = Image.Type.Simple; // Asegurar tipo simple

                // Agregar RectTransform si no existe
                RectTransform rt = sr.GetComponent<RectTransform>();
                if (rt == null)
                {
                    rt = sr.gameObject.AddComponent<RectTransform>();
                }

                // Configurar RectTransform basado en el sprite original
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);

                // Calcular tamaño apropiado basado en el sprite
                Vector2 spriteSize = sr.sprite.bounds.size;
                Vector2 pixelsPerUnit = Vector2.one * sr.sprite.pixelsPerUnit;
                Vector2 targetSize = new Vector2(
                    spriteSize.x * pixelsPerUnit.x,
                    spriteSize.y * pixelsPerUnit.y
                );

                // Escalar al tamaño de animación manteniendo proporción
                float maxDimension = Mathf.Max(targetSize.x, targetSize.y);
                float maxAnimSize = Mathf.Max(animationSize.x, animationSize.y);
                float scaleFactor = maxAnimSize / maxDimension;

                rt.sizeDelta = targetSize * scaleFactor * scaleMultiplier;
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;

                Debug.Log($"[Client] Image configurada - OriginalSize: {targetSize}, FinalSize: {rt.sizeDelta}");

                // Remover SpriteRenderer
                DestroyImmediate(sr);
            }
        }
    }

    private void ConfigureWorldSpaceObject(GameObject animObject)
    {
        Debug.Log("[Client] Configurando como objeto World Space...");

        // Posicionar frente a la cámara
        Camera targetCamera = Camera.main;
        if (targetCamera != null)
        {
            Vector3 cameraForward = targetCamera.transform.forward;
            Vector3 position = targetCamera.transform.position + cameraForward * 5f;
            animObject.transform.position = position;
            animObject.transform.LookAt(targetCamera.transform);
        }

        // Configurar renderers para visibilidad
        ConfigureRenderersForVisibility(animObject);
    }

    private void ConfigureRenderersForVisibility(GameObject obj)
    {
        // Configurar SpriteRenderers
        SpriteRenderer[] spriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            sr.sortingOrder = canvasSortingOrder;

            // Asegurar que el material sea visible
            if (sr.material == null || sr.material.shader.name == "Hidden/InternalErrorShader")
            {
                sr.material = new Material(Shader.Find("Sprites/Default"));
            }
        }

        // Configurar MeshRenderers si los hay
        MeshRenderer[] meshRenderers = obj.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer mr in meshRenderers)
        {
            if (mr.material.shader.name != "Sprites/Default")
            {
                Material newMat = new Material(Shader.Find("Sprites/Default"));
                if (mr.material.mainTexture != null)
                    newMat.mainTexture = mr.material.mainTexture;
                mr.material = newMat;
            }
        }
    }

    private void DebugObjectConfiguration(GameObject obj, string type)
    {
        Debug.Log($"[Client] === DEBUG CONFIGURACIÓN {type.ToUpper()} ===");
        Debug.Log($"[Client] Objeto: {obj.name}");
        Debug.Log($"[Client] Activo: {obj.activeInHierarchy}");
        Debug.Log($"[Client] Parent: {(obj.transform.parent != null ? obj.transform.parent.name : "null")}");
        Debug.Log($"[Client] Posición: {obj.transform.position}");
        Debug.Log($"[Client] Posición local: {obj.transform.localPosition}");
        Debug.Log($"[Client] Escala: {obj.transform.localScale}");

        // Debug de RectTransform si existe
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt != null)
        {
            Debug.Log($"[Client] RectTransform - AnchoredPos: {rt.anchoredPosition}, SizeDelta: {rt.sizeDelta}");
        }

        // Debug de componentes de renderizado
        Image[] images = obj.GetComponentsInChildren<Image>();
        Debug.Log($"[Client] Images encontradas: {images.Length}");
        foreach (var img in images)
        {
            Debug.Log($"[Client] - Image: {img.name}, Enabled: {img.enabled}, Sprite: {(img.sprite != null ? img.sprite.name : "null")}, Color: {img.color}");
        }

        SpriteRenderer[] spriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>();
        Debug.Log($"[Client] SpriteRenderers encontrados: {spriteRenderers.Length}");
        foreach (var sr in spriteRenderers)
        {
            Debug.Log($"[Client] - SpriteRenderer: {sr.name}, Enabled: {sr.enabled}, Sprite: {(sr.sprite != null ? sr.sprite.name : "null")}, SortingOrder: {sr.sortingOrder}, Color: {sr.color}");
        }

        Debug.Log($"[Client] === FIN DEBUG CONFIGURACIÓN ===");
    }

    private GameObject GetAttackPrefab(CardData.ElementType elementType)
    {
        GameObject prefab = null;

        switch (elementType)
        {
            case CardData.ElementType.Boton:
                prefab = botonAttackPrefab;
                break;
            case CardData.ElementType.Alfiler:
                prefab = alfilerAttackPrefab;
                break;
            case CardData.ElementType.Tela:
                prefab = telaAttackPrefab;
                break;
            case CardData.ElementType.Algodon:
                prefab = algodonAttackPrefab;
                break;
            default:
                Debug.LogWarning($"[Client] Tipo de elemento no reconocido: {elementType}");
                break;
        }

        if (prefab == null)
        {
            Debug.LogError($"[Client] Prefab de ataque para {elementType} es null!");
        }

        return prefab;
    }

    private GameObject GetResultPrefab(CardBattleLogic.BattleResult result)
    {
        GameObject prefab = null;

        switch (result)
        {
            case CardBattleLogic.BattleResult.WinA:
            case CardBattleLogic.BattleResult.WinB:
                prefab = victoryPrefab;
                break;
            case CardBattleLogic.BattleResult.Draw:
                prefab = drawPrefab;
                break;
            default:
                Debug.LogWarning($"[Client] Resultado de batalla no reconocido: {result}");
                break;
        }

        if (prefab == null)
        {
            Debug.LogError($"[Client] Prefab de resultado para {result} es null!");
        }

        return prefab;
    }

    [Server]
    public void ShowVictoryAnimation()
    {
        Debug.Log("[Server] Mostrando animación de victoria del juego");
        RpcShowVictoryAnimation();
    }

    [ClientRpc]
    private void RpcShowVictoryAnimation()
    {
        Debug.Log("[Client] Recibido RPC para mostrar animación de victoria");

        if (victoryPrefab != null && animationPanel != null)
        {
            GameObject victory = Instantiate(victoryPrefab, animationPanel);
            ConfigureAnimationObject(victory, "GameVictory");
            Debug.Log($"[Client] Animación de victoria del juego instanciada: {victory.name}");
            StartCoroutine(DestroyAfterAnimation(victory));
        }
        else
        {
            Debug.LogError("[Client] No se puede mostrar animación de victoria - prefab o panel faltante");
        }
    }

    [Server]
    public void ShowDrawAnimation()
    {
        Debug.Log("[Server] Mostrando animación de empate del juego");
        RpcShowDrawAnimation();
    }

    [ClientRpc]
    private void RpcShowDrawAnimation()
    {
        Debug.Log("[Client] Recibido RPC para mostrar animación de empate");

        if (drawPrefab != null && animationPanel != null)
        {
            GameObject draw = Instantiate(drawPrefab, animationPanel);
            ConfigureAnimationObject(draw, "GameDraw");
            Debug.Log($"[Client] Animación de empate del juego instanciada: {draw.name}");
            StartCoroutine(DestroyAfterAnimation(draw));
        }
        else
        {
            Debug.LogError("[Client] No se puede mostrar animación de empate - prefab o panel faltante");
        }
    }

    private IEnumerator WaitForAnimation(GameObject animObject)
    {
        if (animObject == null)
        {
            Debug.LogWarning("[Client] Objeto de animación es null en WaitForAnimation");
            yield break;
        }

        // Debug del objeto
        Debug.Log($"[Client] Esperando animación de: {animObject.name}");

        // Buscar Animator
        Animator animator = animObject.GetComponent<Animator>();

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            Debug.Log($"[Client] Animator encontrado con controller: {animator.runtimeAnimatorController.name}");

            // Esperar un frame para que se inicialice
            yield return null;

            // Obtener información del estado actual
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animationLength = stateInfo.length;

            Debug.Log($"[Client] Duración de animación: {animationLength} segundos");

            if (animationLength > 0)
            {
                yield return new WaitForSeconds(animationLength);
            }
            else
            {
                Debug.LogWarning("[Client] Duración inválida, usando tiempo por defecto");
                yield return new WaitForSeconds(2f);
            }
        }
        else
        {
            Debug.LogWarning("[Client] No se encontró Animator válido, usando tiempo por defecto");
            yield return new WaitForSeconds(2f);
        }

        Debug.Log($"[Client] Animación completada para: {animObject.name}");
    }

    private IEnumerator DestroyAfterAnimation(GameObject animObject)
    {
        if (animObject == null)
        {
            Debug.LogWarning("[Client] Objeto de animación es null en DestroyAfterAnimation");
            yield break;
        }

        Debug.Log($"[Client] Iniciando DestroyAfterAnimation para: {animObject.name}");
        yield return StartCoroutine(WaitForAnimation(animObject));

        if (animObject != null)
        {
            Debug.Log($"[Client] Destruyendo objeto de animación: {animObject.name}");
            Destroy(animObject);
        }
        else
        {
            Debug.LogWarning("[Client] Objeto de animación ya fue destruido");
        }
    }

    // Métodos de debug para probar animaciones manualmente
    [ContextMenu("Test Victory Animation")]
    public void TestVictoryAnimation()
    {
        if (Application.isPlaying)
        {
            if (isServer)
            {
                ShowVictoryAnimation();
            }
            else
            {
                Debug.Log("Solo el servidor puede iniciar animaciones de prueba");
            }
        }
    }

    [ContextMenu("Test Attack Animation")]
    public void TestAttackAnimation()
    {
        if (Application.isPlaying)
        {
            if (isServer)
            {
                PlayBattleAnimations(CardData.ElementType.Boton, CardBattleLogic.BattleResult.WinA);
            }
            else
            {
                Debug.Log("Solo el servidor puede iniciar animaciones de prueba");
            }
        }
    }

    [ContextMenu("Force Canvas Setup")]
    public void ForceCanvasSetup()
    {
        SetupAnimationCanvas();
        Debug.Log("[SimpleBattleAnimator] Canvas setup forzado completado");
    }

    [ContextMenu("Test Small Animation")]
    public void TestSmallAnimation()
    {
        if (Application.isPlaying)
        {
            // Reducir temporalmente el tamaño para prueba
            Vector2 originalSize = animationSize;
            float originalScale = scaleMultiplier;

            animationSize = new Vector2(150, 150);
            scaleMultiplier = 0.5f;

            if (isServer)
            {
                ShowVictoryAnimation();
            }
            else
            {
                Debug.Log("Solo el servidor puede iniciar animaciones de prueba");
            }

            // Restaurar valores originales después de un tiempo
            StartCoroutine(RestoreOriginalSize(originalSize, originalScale));
        }
    }

    private IEnumerator RestoreOriginalSize(Vector2 originalSize, float originalScale)
    {
        yield return new WaitForSeconds(3f);
        animationSize = originalSize;
        scaleMultiplier = originalScale;
        Debug.Log("[SimpleBattleAnimator] Tamaños restaurados a valores originales");
    }
}