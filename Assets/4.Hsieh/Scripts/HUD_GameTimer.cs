
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Reflection;

[RequireComponent(typeof(TextMeshProUGUI))]
public class HUD_GameTimer : MonoBehaviour
{
    [Header("Auto Detect")]
    [SerializeField] private float searchInterval = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool useGameTimer;
    [SerializeField] private float currentTime;
    [SerializeField] private string timerSource = "Local";

    private TextMeshProUGUI timerText;

    private MonoBehaviour gameTimer;
    private PropertyInfo elapsedTimeProperty;

    private float localTime;
    private float searchTimer;

    private void Awake()
    {
        timerText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        FindGameTimer();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void Update()
    {
        searchTimer += Time.unscaledDeltaTime;

        if (searchTimer >= Mathf.Max(0.1f, searchInterval))
        {
            searchTimer = 0f;
            FindGameTimer();
        }

        if (gameTimer != null && elapsedTimeProperty != null)
        {
            try
            {
                currentTime = (float)
                    elapsedTimeProperty.GetValue(gameTimer);

                localTime = currentTime;
                useGameTimer = true;
                timerSource = "Hanaue";
            }
            catch (Exception)
            {
                ClearGameTimer();
                UpdateLocalTimer();
            }
        }
        else
        {
            UpdateLocalTimer();
        }

        UpdateTimerText();
    }

    private void UpdateLocalTimer()
    {
        useGameTimer = false;
        timerSource = "Local";

        localTime += Time.deltaTime;
        currentTime = localTime;
    }

    private void FindGameTimer()
    {
        MonoBehaviour[] behaviours =
            FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        MonoBehaviour candidate = null;
        PropertyInfo candidateProperty = null;

        Scene activeScene = SceneManager.GetActiveScene();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            Type type = behaviour.GetType();

            if (type.Name != "LevelScene_Timer")
                continue;

            PropertyInfo property = type.GetProperty(
                "ElapsedTime",
                BindingFlags.Public | BindingFlags.Instance);

            if (property == null ||
                property.PropertyType != typeof(float))
                continue;

            if (!behaviour.gameObject.scene.isLoaded)
                continue;

            if (candidate == null)
            {
                candidate = behaviour;
                candidateProperty = property;
            }

            if (behaviour.gameObject.scene == activeScene)
            {
                candidate = behaviour;
                candidateProperty = property;
                break;
            }
        }

        gameTimer = candidate;
        elapsedTimeProperty = candidateProperty;
    }

    private void ClearGameTimer()
    {
        gameTimer = null;
        elapsedTimeProperty = null;
    }

    private void UpdateTimerText()
    {
        float time = Mathf.Max(0f, currentTime);

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int centiseconds =
            Mathf.FloorToInt((time % 1f) * 100f);

        timerText.text =
            $"{minutes:00}:{seconds:00}.{centiseconds:00}";
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindGameTimer();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        ClearGameTimer();
        FindGameTimer();
    }

    public void ResetLocalTimer()
    {
        localTime = 0f;

        if (gameTimer == null)
            currentTime = 0f;

        UpdateTimerText();
    }
}
