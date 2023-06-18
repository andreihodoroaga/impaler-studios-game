using System.Collections;
using UnityEngine;

public class FogRendererToggler : MonoBehaviour
{
    public GameObject mesh; // reference to the render you want toggled based on the position of this transform
    [Range(0f, 1f)] public float threshold = 0.1f; //the threshold for when this script considers myRenderer should render

    private const float UpdateRate = 0.5f;

    private Camera camera; // the camera using the masked render texture
    private Coroutine texUpdateCoroutine;

    private static FogRendererToggler mainInstance;
    // made so all instances share the same texture, reducing texture reads
    private static Texture2D shadowTexture;
    private static Rect rect;
    private static bool validTexture = true;

    private void Start()
    {
        // disable if:
        // - FOV game parameter is inactive
        // - or no mesh is defined
        if (!GameManager.instance.gameGlobalParameters.enableFOV || !mesh)
        {
            Destroy(this);
            return;
        }

        // also disable if the unit is mine
        UnitManager unitManager = GetComponent<UnitManager>();
        if (unitManager != null && unitManager.Unit.Owner == GameManager.instance.gamePlayersParameters.myPlayerId)
        {
            Destroy(this);
            return;
        }

        // mark the "first" (arbitrary) instance as main
        if (mainInstance == null)
        {
            mainInstance = this;
        }

        // only run the texture updates on the main instance
        if (mainInstance == this)
        {
            camera = GameObject.Find("UnexploredAreasCam").GetComponent<Camera>();
            texUpdateCoroutine = StartCoroutine(UpdatingShadowTexture());
        }
        else
        {
            texUpdateCoroutine = null;
        }
    }

    private void OnDisable()
    {
        if (texUpdateCoroutine != null)
        {
            StopCoroutine(texUpdateCoroutine);
            texUpdateCoroutine = null;
        }
    }

    private void LateUpdate()
    {
        if (!camera) return;
        bool active = GetColorAtPosition().grayscale >= threshold;
        if (mesh.activeSelf != active)
        {
            mesh.SetActive(active);
        }
    }

    private void UpdateShadowTexture()
    {
        if (!camera)
        {
            validTexture = false;
            return;
        }

        RenderTexture renderTexture = camera.targetTexture;
        if (!renderTexture)
        {
            validTexture = false;
            return;
        }

        if (shadowTexture == null || renderTexture.width != rect.width || renderTexture.height != rect.height)
        {
            rect = new Rect(0, 0, renderTexture.width, renderTexture.height);
            shadowTexture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
        }

        RenderTexture.active = renderTexture;
        shadowTexture.ReadPixels(rect, 0, 0);
        RenderTexture.active = null;
    }

    private Color GetColorAtPosition()
    {
        if (!validTexture) return Color.white;
        Vector3 pixel = camera.WorldToScreenPoint(transform.position);
        return shadowTexture.GetPixel((int)pixel.x, (int)pixel.y);
    }

    private IEnumerator UpdatingShadowTexture()
    {
        while (true)
        {
            UpdateShadowTexture();
            yield return new WaitForSeconds(UpdateRate);
        }
    }
}