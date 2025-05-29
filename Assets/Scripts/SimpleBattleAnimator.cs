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

    [Header("�rea de Animaciones")]
    [SerializeField] private Transform animationPanel;

    [Header("Configuraci�n de Renderizado")]
    [SerializeField] private Canvas animationCanvas;
    [SerializeField] private int canvasSortingOrder = 1000;

    [Header("Configuraci�n de Timing")]
    [SerializeField] private float waitingToAttackDelay = 1.5f;
    [SerializeField] private float attackAnimationDuration = 3f; // Duraci�n estimada de animaciones de ataque
    [SerializeField] private float animationBuffer = 1f; // Buffer adicional

    private static SimpleBattleAnimator instance;
    public static SimpleBattleAnimator Instance => instance;

    // Control de animaci�n de espera
    private GameObject currentWaitingAnimation;
    private bool isWaitingForBattle = false;

    // Control de estado de animaciones de batalla
    [SyncVar] private bool isBattleAnimationPlaying = false;

    // Variable para controlar si el juego ha comenzado oficialmente
    [SyncVar] private bool gameHasStarted = false;

    // Propiedades p�blicas para que RoundManager pueda consultar el estado
    public bool IsBattleAnimationPlaying => isBattleAnimationPlaying;
    public float TotalBattleAnimationTime => waitingToAttackDelay + attackAnimationDuration + animationBuffer;

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
            // Buscar canvas padre del panel de animaci�n
            if (animationPanel != null)
            {
                animationCanvas = animationPanel.GetComponentInParent<Canvas>();
            }

            // Si a�n no hay canvas, crear uno
            if (animationCanvas == null)
            {
                Debug.LogWarning("[SimpleBattleAnimator] Creando Canvas de animaci�n autom�ticamente");
                GameObject canvasGO = new GameObject("AnimationCanvas");
                animationCanvas = canvasGO.AddComponent<Canvas>();
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();

                animationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                animationCanvas.sortingOrder = canvasSortingOrder;

                // Crear panel de animaci�n si no existe
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
            animationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            animationCanvas.sortingOrder = canvasSortingOrder;
            Debug.Log($"[SimpleBattleAnimator] Canvas configurado: {animationCanvas.renderMode}, SortingOrder: {animationCanvas.sortingOrder}");
        }
    }

    private void ValidatePrefabs()
    {
        Debug.Log("[SimpleBattleAnimator] Validando prefabs...");

        if (botonAttackPrefab == null) Debug.LogError("[SimpleBattleAnimator] botonAttackPrefab no est� asignado!");
        if (alfilerAttackPrefab == null) Debug.LogError("[SimpleBattleAnimator] alfilerAttackPrefab no est� asignado!");
        if (telaAttackPrefab == null) Debug.LogError("[SimpleBattleAnimator] telaAttackPrefab no est� asignado!");
        if (algodonAttackPrefab == null) Debug.LogError("[SimpleBattleAnimator] algodonAttackPrefab no est� asignado!");

        if (victoryPrefab == null) Debug.LogError("[SimpleBattleAnimator] victoryPrefab no est� asignado!");
        if (defeatPrefab == null) Debug.LogError("[SimpleBattleAnimator] defeatPrefab no est� asignado!");
        if (drawPrefab == null) Debug.LogError("[SimpleBattleAnimator] drawPrefab no est� asignado!");

        if (animationPanel == null) Debug.LogError("[SimpleBattleAnimator] animationPanel no est� asignado!");

        Debug.Log("[SimpleBattleAnimator] Validaci�n de prefabs completada.");
    }

    #region Reset System

    [Server]
    public void ResetToInitialState()
    {
        if (!isServer) return;

        Debug.Log("[Server] Reseteando SimpleBattleAnimator al estado inicial...");

        // Detener todas las corrutinas en curso
        StopAllCoroutines();

        // Resetear estado de juego
        gameHasStarted = false;
        isBattleAnimationPlaying = false;

        // Resetear variables de control de animaci�n
        isWaitingForBattle = false;

        // Limpiar cualquier animaci�n actual en el servidor
        if (currentWaitingAnimation != null)
        {
            Debug.Log("[Server] Destruyendo animaci�n de espera durante reset");
            Destroy(currentWaitingAnimation);
            currentWaitingAnimation = null;
        }

        // Notificar a todos los clientes del reset
        RpcResetAnimatorState();

        Debug.Log("[Server] SimpleBattleAnimator reseteado correctamente al estado inicial");
    }

    [ClientRpc]
    private void RpcResetAnimatorState()
    {
        Debug.Log("[Client] Reseteando estado del animator...");

        // Detener todas las corrutinas del cliente
        StopAllCoroutines();

        // Resetear variables locales del cliente
        isWaitingForBattle = false;

        // Limpiar animaci�n de espera local
        if (currentWaitingAnimation != null)
        {
            Debug.Log("[Client] Destruyendo animaci�n de espera durante reset en cliente");
            Destroy(currentWaitingAnimation);
            currentWaitingAnimation = null;
        }

        // Limpiar cualquier animaci�n residual en el panel
        CleanupAnimationPanel();

        Debug.Log("[Client] Estado del animator reseteado en cliente");
    }

    private void CleanupAnimationPanel()
    {
        if (animationPanel == null) return;

        Debug.Log("[Client] Limpiando panel de animaci�n...");

        // Destruir todos los objetos hijos que puedan ser animaciones residuales
        for (int i = animationPanel.childCount - 1; i >= 0; i--)
        {
            Transform child = animationPanel.GetChild(i);

            // Verificar si es una animaci�n (puedes ajustar esta l�gica seg�n tus prefabs)
            if (child.name.Contains("Attack") ||
                child.name.Contains("Victory") ||
                child.name.Contains("Defeat") ||
                child.name.Contains("Draw") ||
                child.name.Contains("(Clone)"))
            {
                Debug.Log($"[Client] Destruyendo animaci�n residual: {child.name}");
                Destroy(child.gameObject);
            }
        }
    }

    #endregion

    #region Game State Management

    [Server]
    public void SetGameStarted()
    {
        // Verificar que realmente estemos en un estado limpio antes de marcar como iniciado
        if (currentWaitingAnimation != null)
        {
            Debug.LogWarning("[Server] Hab�a una animaci�n de espera activa al marcar juego como iniciado - limpiando");
            Destroy(currentWaitingAnimation);
            currentWaitingAnimation = null;
        }

        isWaitingForBattle = false;
        isBattleAnimationPlaying = false;
        gameHasStarted = true;
        Debug.Log("[Server] Juego marcado como iniciado para animaciones");
    }

    #endregion

    #region Waiting Animation System

    [Server]
    public void StartWaitingAnimation()
    {
        // Solo iniciar si el juego realmente ha comenzado
        if (!gameHasStarted)
        {
            Debug.Log("[Server] Juego no ha comenzado oficialmente, saltando animaci�n de espera");
            return;
        }

        // Verificar que no haya una animaci�n de espera ya activa
        if (isWaitingForBattle)
        {
            Debug.Log("[Server] Ya hay una animaci�n de espera activa, reiniciando...");
            StopWaitingAnimation();
            // Esperar un frame antes de iniciar la nueva
            StartCoroutine(DelayedStartWaiting());
            return;
        }

        Debug.Log("[Server] Iniciando animaci�n de espera");
        RpcStartWaitingAnimation();
    }

    [Server]
    private IEnumerator DelayedStartWaiting()
    {
        yield return null; // Esperar un frame
        if (gameHasStarted && !isWaitingForBattle && !isBattleAnimationPlaying)
        {
            RpcStartWaitingAnimation();
        }
    }

    [ClientRpc]
    private void RpcStartWaitingAnimation()
    {
        Debug.Log("[Client] Recibido RPC para iniciar animaci�n de espera");
        StartCoroutine(PlayWaitingAnimation());
    }

    private IEnumerator PlayWaitingAnimation()
    {
        if (animationPanel == null || drawPrefab == null)
        {
            Debug.LogError("[Client] No se puede iniciar animaci�n de espera - componentes faltantes");
            yield break;
        }

        // Asegurar que el canvas est� configurado
        SetupAnimationCanvas();

        // Detener cualquier animaci�n de espera anterior ANTES de crear una nueva
        StopWaitingAnimationImmediate();

        // Crear la animaci�n de espera (loop)
        currentWaitingAnimation = Instantiate(drawPrefab, animationPanel);
        isWaitingForBattle = true;

        Debug.Log($"[Client] Animaci�n de espera iniciada: {currentWaitingAnimation.name}");

        // La animaci�n de espera continuar� hasta que se detenga expl�citamente
        yield return null;
    }

    private void StopWaitingAnimationImmediate()
    {
        if (currentWaitingAnimation != null)
        {
            Debug.Log($"[Client] Destruyendo animaci�n de espera: {currentWaitingAnimation.name}");
            Destroy(currentWaitingAnimation);
            currentWaitingAnimation = null;
        }
        isWaitingForBattle = false;

        // Verificaci�n adicional M�S AGRESIVA para asegurar limpieza completa
        if (animationPanel != null)
        {
            // Buscar y destruir TODAS las animaciones que puedan estar corriendo
            for (int i = animationPanel.childCount - 1; i >= 0; i--)
            {
                Transform child = animationPanel.GetChild(i);
                if (child.name.Contains("Draw") || child.name.Contains("Empate"))
                {
                    Debug.Log($"[Client] Limpiando animaci�n residual: {child.name}");
                    Destroy(child.gameObject);
                }
            }
        }
    }

    [Server]
    public void StopWaitingAnimation()
    {
        Debug.Log("[Server] Deteniendo animaci�n de espera");
        RpcStopWaitingAnimation();
    }

    [ClientRpc]
    private void RpcStopWaitingAnimation()
    {
        Debug.Log("[Client] Recibido RPC para detener animaci�n de espera");
        StopWaitingAnimationImmediate();
    }

    #endregion

    #region Battle Animation System

    [Server]
    public void PlayBattleAnimations(CardData.ElementType winnerElement, CardBattleLogic.BattleResult result)
    {
        Debug.Log($"[Server] Iniciando animaciones de batalla. Elemento ganador: {winnerElement}, Resultado: {result}");

        // Marcar que las animaciones de batalla est�n reproduci�ndose
        isBattleAnimationPlaying = true;

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
        Debug.Log("[Client] Iniciando secuencia de animaci�n de batalla...");

        if (animationPanel == null)
        {
            Debug.LogError("[Client] animationPanel es null! No se pueden reproducir animaciones.");
            yield break;
        }

        // Asegurar que el canvas est� configurado
        SetupAnimationCanvas();

        // MANTENER la animaci�n de espera por un tiempo
        Debug.Log($"[Client] Manteniendo animaci�n de espera por {waitingToAttackDelay} segundos...");
        yield return new WaitForSeconds(waitingToAttackDelay);

        // LIMPIEZA AGRESIVA: Destruir TODO lo que hay en el panel
        Debug.Log("[Client] LIMPIEZA TOTAL del panel de animaci�n");
        if (animationPanel != null)
        {
            for (int i = animationPanel.childCount - 1; i >= 0; i--)
            {
                Transform child = animationPanel.GetChild(i);
                Debug.Log($"[Client] Destruyendo objeto en panel: {child.name}");
                Destroy(child.gameObject);
            }
        }

        // Resetear variables de control
        currentWaitingAnimation = null;
        isWaitingForBattle = false;

        // Esperar varios frames para asegurar que Unity proces� las destrucciones
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Debug.Log($"[Client] Panel limpiado. Objetos restantes: {animationPanel.childCount}");

        // AHORA crear la animaci�n de ataque o resultado
        if (result != CardBattleLogic.BattleResult.Draw)
        {
            Debug.Log($"[Client] Creando animaci�n de ataque para elemento: {winnerElement}");
            GameObject attackPrefab = GetAttackPrefab(winnerElement);

            if (attackPrefab != null)
            {
                GameObject attackInstance = Instantiate(attackPrefab, animationPanel);
                Debug.Log($"[Client] Animaci�n de ataque creada: {attackInstance.name}");

                // Esperar a que termine la animaci�n usando el m�todo mejorado
                yield return StartCoroutine(WaitForAnimation(attackInstance));

                Debug.Log("[Client] Destruyendo animaci�n de ataque");
                Destroy(attackInstance);
            }
        }
        else
        {
            // Si es empate, mostrar animaci�n de empate una vez
            Debug.Log($"[Client] Reproduciendo animaci�n de empate final");
            GameObject resultPrefab = GetResultPrefab(result);

            if (resultPrefab != null)
            {
                GameObject resultInstance = Instantiate(resultPrefab, animationPanel);
                Debug.Log($"[Client] Animaci�n de empate final instanciada: {resultInstance.name}");

                yield return StartCoroutine(WaitForAnimation(resultInstance));

                Debug.Log("[Client] Animaci�n de empate final completada, destruyendo objeto...");
                Destroy(resultInstance);
            }
        }

        Debug.Log("[Client] Secuencia de animaci�n de batalla completada.");

        // Si es el servidor, marcar que las animaciones han terminado
        if (isServer)
        {
            isBattleAnimationPlaying = false;
            Debug.Log("[Server] Animaciones de batalla marcadas como completadas");
        }
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

    // M�todo p�blico para que RoundManager espere a que terminen las animaciones
    [Server]
    public IEnumerator WaitForBattleAnimationsToComplete()
    {
        Debug.Log("[Server] Esperando a que terminen las animaciones de batalla...");

        while (isBattleAnimationPlaying)
        {
            yield return new WaitForSeconds(0.1f); // Comprobar cada 0.1 segundos
        }

        // Buffer adicional para asegurar que todo se ha completado
        yield return new WaitForSeconds(animationBuffer);

        Debug.Log("[Server] Todas las animaciones de batalla han terminado");
    }

    #endregion

    #region Game End Animations

    [Server]
    public void ShowVictoryAnimation()
    {
        Debug.Log("[Server] Mostrando animaci�n de victoria del juego");
        RpcShowVictoryAnimation();
    }

    [ClientRpc]
    private void RpcShowVictoryAnimation()
    {
        Debug.Log("[Client] Recibido RPC para mostrar animaci�n de victoria");

        // Primero detener cualquier animaci�n de espera
        StopWaitingAnimationImmediate();

        if (victoryPrefab != null && animationPanel != null)
        {
            GameObject victory = Instantiate(victoryPrefab, animationPanel);
            Debug.Log($"[Client] Animaci�n de victoria del juego instanciada: {victory.name}");
            StartCoroutine(DestroyAfterAnimation(victory));
        }
        else
        {
            Debug.LogError("[Client] No se puede mostrar animaci�n de victoria - prefab o panel faltante");
        }
    }

    [Server]
    public void ShowDrawAnimation()
    {
        Debug.Log("[Server] Mostrando animaci�n de empate del juego");
        RpcShowDrawAnimation();
    }

    [ClientRpc]
    private void RpcShowDrawAnimation()
    {
        Debug.Log("[Client] Recibido RPC para mostrar animaci�n de empate");

        // Primero detener cualquier animaci�n de espera
        StopWaitingAnimationImmediate();

        if (drawPrefab != null && animationPanel != null)
        {
            GameObject draw = Instantiate(drawPrefab, animationPanel);
            Debug.Log($"[Client] Animaci�n de empate del juego instanciada: {draw.name}");
            StartCoroutine(DestroyAfterAnimation(draw));
        }
        else
        {
            Debug.LogError("[Client] No se puede mostrar animaci�n de empate - prefab o panel faltante");
        }
    }

    #endregion

    #region Animation Utilities

    private IEnumerator WaitForAnimation(GameObject animObject)
    {
        if (animObject == null)
        {
            Debug.LogWarning("[Client] Objeto de animaci�n es null en WaitForAnimation");
            yield break;
        }

        Debug.Log($"[Client] Esperando animaci�n de: {animObject.name}");

        // Buscar Animator en el objeto y sus hijos
        Animator animator = animObject.GetComponentInChildren<Animator>();

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            Debug.Log($"[Client] Animator encontrado con controller: {animator.runtimeAnimatorController.name}");

            // Esperar un frame para que se inicialice la animaci�n
            yield return null;

            // Obtener informaci�n del estado actual
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animationLength = stateInfo.length;

            Debug.Log($"[Client] Duraci�n de animaci�n: {animationLength} segundos");

            if (animationLength > 0)
            {
                // Esperar a que termine la animaci�n
                yield return new WaitForSeconds(animationLength);
            }
            else
            {
                Debug.LogWarning("[Client] Duraci�n inv�lida, usando tiempo configurado");
                yield return new WaitForSeconds(attackAnimationDuration);
            }
        }
        else
        {
            Debug.LogWarning("[Client] No se encontr� Animator v�lido, usando tiempo configurado");
            yield return new WaitForSeconds(attackAnimationDuration);
        }

        Debug.Log($"[Client] Animaci�n completada para: {animObject.name}");
    }

    private IEnumerator DestroyAfterAnimation(GameObject animObject)
    {
        if (animObject == null)
        {
            Debug.LogWarning("[Client] Objeto de animaci�n es null en DestroyAfterAnimation");
            yield break;
        }

        Debug.Log($"[Client] Iniciando DestroyAfterAnimation para: {animObject.name}");
        yield return StartCoroutine(WaitForAnimation(animObject));

        if (animObject != null)
        {
            Debug.Log($"[Client] Destruyendo objeto de animaci�n: {animObject.name}");
            Destroy(animObject);
        }
        else
        {
            Debug.LogWarning("[Client] Objeto de animaci�n ya fue destruido");
        }
    }

    #endregion

    #region Debug and Testing Methods

    [ContextMenu("Debug Animator State")]
    public void DebugAnimatorState()
    {
        if (!Application.isPlaying) return;

        Debug.Log("=== ANIMATOR STATE DEBUG ===");
        Debug.Log($"Game Started: {gameHasStarted}");
        Debug.Log($"Is Waiting for Battle: {isWaitingForBattle}");
        Debug.Log($"Is Battle Animation Playing: {isBattleAnimationPlaying}");
        Debug.Log($"Current Waiting Animation: {(currentWaitingAnimation != null ? currentWaitingAnimation.name : "null")}");
        Debug.Log($"Animation Panel Children: {(animationPanel != null ? animationPanel.childCount : 0)}");

        if (animationPanel != null && animationPanel.childCount > 0)
        {
            Debug.Log("Panel Children:");
            for (int i = 0; i < animationPanel.childCount; i++)
            {
                Debug.Log($"  - {animationPanel.GetChild(i).name}");
            }
        }
        Debug.Log("=== END DEBUG ===");
    }

    [ContextMenu("Force Complete Reset")]
    public void ForceCompleteReset()
    {
        if (!Application.isPlaying) return;

        Debug.Log("[Debug] Forzando reset completo del animator...");

        if (isServer)
        {
            ResetToInitialState();
        }
        else
        {
            // En cliente, solo limpiar estado local
            StopAllCoroutines();
            isWaitingForBattle = false;

            if (currentWaitingAnimation != null)
            {
                Destroy(currentWaitingAnimation);
                currentWaitingAnimation = null;
            }

            CleanupAnimationPanel();
        }

        Debug.Log("[Debug] Reset completo forzado completado");
    }

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

    #endregion
}