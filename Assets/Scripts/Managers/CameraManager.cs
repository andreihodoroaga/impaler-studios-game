using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraManager : MonoBehaviour
{
    public float translationSpeed = 60f;
    public float zoomSpeed = 30f;
    public float altitude = 40f;

    public Transform groundTarget;

    public bool autoAdaptAltitude;

    private Camera _camera;
    private RaycastHit _hit;
    private Ray _ray;

    private float _distance = 500f;
    private Vector3 _forwardDir;
    private Coroutine _mouseOnScreenCoroutine;
    private int _mouseOnScreenBorder;
    private bool _placingBuilding;

    private float _minX;
    private float _maxX;
    private float _minZ;
    private float _maxZ;
    private Vector3 _camOffset;
    private Vector3 _camHalfViewZone;
    private float _camMinimapBuffer = 5f;

    private void Awake()
    {
        _camera = GetComponent<Camera>(); // Get the Camera component attached to the same GameObject

        _forwardDir = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized; // Project the camera's forward vector onto the ground plane
        _mouseOnScreenCoroutine = null;
        _mouseOnScreenBorder = -1;
        _placingBuilding = false;
    }

    void Update()
    {
        if (GameManager.instance.gameIsPaused) return; // If the game is paused, do not update the camera

        if (_mouseOnScreenBorder >= 0)
        {
            _TranslateCamera(_mouseOnScreenBorder); // Translate the camera based on the mouse position on the screen
        }
        else
        {
            if (Input.GetKey(KeyCode.UpArrow))
                _TranslateCamera(0); // Translate the camera up
            if (Input.GetKey(KeyCode.RightArrow))
                _TranslateCamera(1); // Translate the camera right
            if (Input.GetKey(KeyCode.DownArrow))
                _TranslateCamera(2); // Translate the camera down
            if (Input.GetKey(KeyCode.LeftArrow))
                _TranslateCamera(3); // Translate the camera left
        }

        // only use scroll for zoom if not currently placing a building
        if (!_placingBuilding && Math.Abs(Input.mouseScrollDelta.y) > 0f)
            _Zoom(Input.mouseScrollDelta.y > 0f ? 1 : -1); // Zoom the camera based on the mouse scroll input
    }

    private void OnEnable()
    {
        EventManager.AddListener("PlaceBuildingOn", _OnPlaceBuildingOn); // Subscribe to the "PlaceBuildingOn" event
        EventManager.AddListener("PlaceBuildingOff", _OnPlaceBuildingOff); // Subscribe to the "PlaceBuildingOff" event
        EventManager.AddListener("ClickedMinimap", _OnClickedMinimap); // Subscribe to the "ClickedMinimap" event
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("PlaceBuildingOn", _OnPlaceBuildingOn); // Unsubscribe from the "PlaceBuildingOn" event
        EventManager.RemoveListener("PlaceBuildingOff", _OnPlaceBuildingOff); // Unsubscribe from the "PlaceBuildingOff" event
        EventManager.RemoveListener("ClickedMinimap", _OnClickedMinimap); // Unsubscribe from the "ClickedMinimap" event
    }

    private void _OnPlaceBuildingOn()
    {
        _placingBuilding = true; // Set the flag indicating that a building is being placed
    }

    private void _OnPlaceBuildingOff()
    {
        _placingBuilding = false; // Set the flag indicating that a building is not being placed
    }

    public void OnMouseEnterScreenBorder(int borderIndex)
    {
        _mouseOnScreenCoroutine = StartCoroutine(_SetMouseOnScreenBorder(borderIndex)); // Start the coroutine to handle mouse on screen behavior
    }

    public void OnMouseExitScreenBorder()
    {
        StopCoroutine(_mouseOnScreenCoroutine); // Stop the mouse on screen coroutine
        _mouseOnScreenBorder = -1; // Reset the mouse on screen border index
    }

    private IEnumerator _SetMouseOnScreenBorder(int borderIndex)
    {
        yield return new WaitForSeconds(0.3f); // Wait for a short delay before setting the mouse on screen border
        _mouseOnScreenBorder = borderIndex; // Set the mouse on screen border index
    }

    private void _OnClickedMinimap(object data)
    {
        Vector3 pos = _FixBounds((Vector3) data); // Get the position from the minimap click event and fix it within the camera movement bounds
        SetPosition(pos); // Set the camera position to the clicked position

        if (autoAdaptAltitude)
            _FixAltitude(); // Fix the camera altitude if autoAdaptAltitude is enabled
    }

    private void _TranslateCamera(int dir)
    {
        if (dir == 0 && transform.position.z - _camOffset.z + _camHalfViewZone.z <= _maxZ)          // top
            transform.Translate(_forwardDir * Time.deltaTime * translationSpeed, Space.World); // Translate the camera up
        else if (dir == 1 && transform.position.x + _camHalfViewZone.x <= _maxX)                    // right
            transform.Translate(transform.right * Time.deltaTime * translationSpeed); // Translate the camera right
        else if (dir == 2 && transform.position.z - _camOffset.z - _camHalfViewZone.z >= _minZ)     // bottom
            transform.Translate(-_forwardDir * Time.deltaTime * translationSpeed, Space.World); // Translate the camera down
        else if (dir == 3 && transform.position.x - _camHalfViewZone.x >= _minX)                    // left
            transform.Translate(-transform.right * Time.deltaTime * translationSpeed); // Translate the camera left

        _FixGroundTarget(); // Fix the position of the ground target to the middle of the screen

        if (autoAdaptAltitude)
            _FixAltitude(); // Fix the camera altitude if autoAdaptAltitude is enabled
    }

    private void _Zoom(int zoomDir)
    {
        _camera.orthographicSize += zoomDir * Time.deltaTime * zoomSpeed; // Adjust the orthographic size of the camera for zooming
        _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, 8f, 26f); // Clamp the orthographic size within the specified range

        (Vector3 minWorldPoint, Vector3 maxWorldPoint) = Utils.GetCameraWorldBounds(); // Get the world bounds of the camera view
        _camOffset = transform.position - (maxWorldPoint + minWorldPoint) / 2f; // Update the camera offset from the target position
        _camHalfViewZone = (maxWorldPoint - minWorldPoint) / 2f + Vector3.one * _camMinimapBuffer; // Update the camera half view zone

        // fix bounds
        Vector3 pos = Utils.MiddleOfScreenPointToWorld(); // Get the world position at the middle of the screen
        pos = _FixBounds(pos); // Fix the position within the camera movement bounds
        SetPosition(pos); // Set the camera position to the fixed position
    }

    private Vector3 _FixBounds(Vector3 pos)
    {
        if (pos.x - _camHalfViewZone.x < _minX) pos.x = _minX + _camHalfViewZone.x; // Fix the X coordinate if it goes beyond the minimum bound
        if (pos.x + _camHalfViewZone.x > _maxX) pos.x = _maxX - _camHalfViewZone.x; // Fix the X coordinate if it goes beyond the maximum bound
        if (pos.z - _camHalfViewZone.z < _minZ) pos.z = _minZ + _camHalfViewZone.z; // Fix the Z coordinate if it goes beyond the minimum bound
        if (pos.z + _camHalfViewZone.z > _maxZ) pos.z = _maxZ - _camHalfViewZone.z; // Fix the Z coordinate if it goes beyond the maximum bound
        return pos; // Return the fixed position
    }

    private void _FixAltitude()
    {
        // translate camera at proper altitude: cast a ray to the ground
        // and move up the hit point
        _ray = new Ray(transform.position, Vector3.up * -1000f); // Create a ray from the camera position pointing downwards
        if (Physics.Raycast(
                _ray,
                out _hit,
                1000f,
                Globals.TERRAIN_LAYER_MASK
            )) transform.position = _hit.point + Vector3.up * altitude; // Move the camera to the hit point on the ground plus the desired altitude
    }

    private void _FixGroundTarget()
    {
        groundTarget.position = Utils.MiddleOfScreenPointToWorld(_camera); // Set the position of the ground target to the middle of the screen
    }

    public void InitializeBounds()
    {
        _minX = 0;
        _maxX = GameManager.instance.terrainSize;
        _minZ = 0;
        _maxZ = GameManager.instance.terrainSize;

        (Vector3 minWorldPoint, Vector3 maxWorldPoint) = Utils.GetCameraWorldBounds(); // Get the world bounds of the camera view
        _camOffset = transform.position - (maxWorldPoint + minWorldPoint) / 2f; // Calculate the camera offset from the target position
        _camHalfViewZone = (maxWorldPoint - minWorldPoint) / 2f + Vector3.one * _camMinimapBuffer; // Calculate the camera half view zone

        _FixGroundTarget(); // Fix the position of the ground target to the middle of the screen
    }

    public void SetPosition(Vector3 pos)
    {
        transform.position = pos - _distance * transform.forward; // Set the camera position based on the desired position and the distance from the target
        _FixGroundTarget(); // Fix the position of the ground target to the middle of the screen
    }
}
