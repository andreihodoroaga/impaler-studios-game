using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CoreBooter : MonoBehaviour
{
    public static CoreBooter instance;

    public UnityEngine.UI.Image sceneTransitioner;

    private bool _sceneIsLoaded;

    private void Awake()
    {
        // Singleton pattern: Ensure only one instance of CoreBooter exists.
        if (instance == null)
            instance = this;
    }

    private void OnEnable()
    {
        // Subscribe to the "LoadedScene" event.
        EventManager.AddListener("LoadedScene", _OnLoadedScene);
    }

    private void OnDisable()
    {
        // Unsubscribe from the "LoadedScene" event.
        EventManager.AddListener("LoadedScene", _OnLoadedScene);
    }

    private void Start()
    {
        // Load the menu scene.
        LoadMenu();
    }

    private void _OnLoadedScene()
    {
        // Callback function for the "LoadedScene" event, indicating that a scene has finished loading.
        _sceneIsLoaded = true;
    }

    public void LoadMenu() => StartCoroutine(_SwitchingScene("menu"));

    public void LoadMap(string mapReference)
    {
        // Load the specified map scene.
        MapData d = Resources.Load<MapData>($"ScriptableObjects/Maps/{mapReference}");
        CoreDataHandler.instance.SetMapData(d);
        string s = d.sceneName;
        StartCoroutine(_SwitchingScene("game", s));
    }

    private IEnumerator _SwitchingScene(string to, string map = "")
    {
        // Coroutine function for switching scenes with a fade transition.

        _sceneIsLoaded = false;
        sceneTransitioner.color = Color.clear;

        float t = 0;
        while (t < 1f)
        {
            // Transition from clear to black by changing the color of the scene transition image.
            sceneTransitioner.color = Color.Lerp(Color.clear, Color.black, t);
            t += Time.deltaTime;
            yield return null;
        }

        AsyncOperation op;
        if (to == "menu")
            op = _LoadMenu();
        else
            op = _LoadMap(map);

        yield return new WaitUntil(() => _sceneIsLoaded);

        t = 0;
        while (t < 1f)
        {
            // Transition from black to clear by changing the color of the scene transition image.
            sceneTransitioner.color = Color.Lerp(Color.black, Color.clear, t);
            t += Time.deltaTime;
            yield return null;
        }

        sceneTransitioner.color = Color.clear;
    }

    private AsyncOperation _LoadMap(string map)
    {
        // Load the specified map scene asynchronously.

        AsyncOperation op = SceneManager.LoadSceneAsync(map, LoadSceneMode.Additive);
        AudioListener prevListener = Object.FindObjectOfType<AudioListener>();

        op.completed += (_) =>
        {
            // After the map scene has finished loading:
            // Disable the previous audio listener.
            if (prevListener != null) prevListener.enabled = false;
            // Set the loaded map scene as the active scene.
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(map));

            Scene s = SceneManager.GetSceneByName("MainMenu");
            if (s != null && s.IsValid())
            {
                // If the "MainMenu" scene is loaded, unload it and load the "GameScene".
                SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Additive).completed += (_) =>
                {
                    SceneManager.UnloadSceneAsync(s);
                };
            }
            else
            {
                // If the "MainMenu" scene is not loaded, directly load the "GameScene".
                SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Additive);
            }
        };

        return op;
    }

    private AsyncOperation _LoadMenu()
    {
        // Load the menu scene asynchronously.

        AudioListener prevListener = Object.FindObjectOfType<AudioListener>();
        if (prevListener != null) prevListener.enabled = false;

        AsyncOperation op = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Additive);

        op.completed += (_) =>
        {
            Scene s = SceneManager.GetSceneByName("GameScene");
            if (s != null && s.IsValid())
            {
                // If the "GameScene" is loaded, unload it.
                SceneManager.UnloadSceneAsync(s);
            }

            if (CoreDataHandler.instance.Scene != null)
            {
                // Unload the previously loaded scene (if any) based on the CoreDataHandler's scene reference.
                s = SceneManager.GetSceneByName(CoreDataHandler.instance.Scene);
                if (s != null && s.IsValid())
                    SceneManager.UnloadSceneAsync(s);
            }

            // Set the "MainMenu" scene as the active scene.
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("MainMenu"));
        };

        return op;
    }
}
