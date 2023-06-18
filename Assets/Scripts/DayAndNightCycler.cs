using System.Collections;
using UnityEngine;

public class DayAndNightCycler : MonoBehaviour
{
    public Transform starsTransform;

    private float starsRefreshRate = 0.1f;
    private float rotationAngleStep;
    private Vector3 rotationAxis;

    private void Start()
    {
        // Apply initial rotation on stars
        starsTransform.rotation = Quaternion.Euler(
            GameManager.instance.gameGlobalParameters.dayInitialRatio * 360f,
            -30f,
            0f
        );

        // Compute relevant calculation parameters
        rotationAxis = starsTransform.right;
        rotationAngleStep = 360f * starsRefreshRate / GameManager.instance.gameGlobalParameters.dayLengthInSeconds;

        StartCoroutine(UpdateStars());
    }

    private IEnumerator UpdateStars()
    {
        float rotation = 0f;
        while (true)
        {
            rotation = (rotation + rotationAngleStep) % 360f;
            starsTransform.Rotate(rotationAxis, rotationAngleStep, Space.World);

            // Check for specific time of day to play matching sound if needed
            if (rotation <= 90f && rotation + rotationAngleStep > 90f)
                EventManager.TriggerEvent("PlaySoundByName", "onNightStartSound");
            if (rotation <= 270f && rotation + rotationAngleStep > 270f)
                EventManager.TriggerEvent("PlaySoundByName", "onDayStartSound");

            yield return new WaitForSeconds(starsRefreshRate);
        }
    }
}