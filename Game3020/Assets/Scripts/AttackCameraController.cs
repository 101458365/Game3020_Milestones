using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class AttackCameraController : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private CinemachineCamera mainCamera;
    [SerializeField] private Transform playerTransform;

    [Header("Cutscene Settings")]
    [SerializeField] private float cutsceneDuration = 4f;
    [SerializeField] private float cameraDistance = 4f;
    [SerializeField] private float cameraHeightOffset = 1.5f;
    [SerializeField] private float sideOffset = 2f;
    [SerializeField] private float orbitSpeed = 120f; // degrees per second;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool disablePlayerInput = true;

    private bool isInCutscene = false;
    private CinemachineOrbitalFollow orbitalFollow;
    private float originalHorizontalAxis;
    private float originalVerticalAxis;
    private bool originalCameraEnabled;

    void Start()
    {
        if (mainCamera != null)
        {
            orbitalFollow = mainCamera.GetComponent<CinemachineOrbitalFollow>();
        }
    }

    public void PlayAttackCutscene()
    {
        if (!isInCutscene && mainCamera != null && playerTransform != null && orbitalFollow != null)
        {
            StartCoroutine(AttackCutsceneCoroutine());
        }
    }

    private IEnumerator AttackCutsceneCoroutine()
    {
        isInCutscene = true;

        // store original axis values;
        originalHorizontalAxis = orbitalFollow.HorizontalAxis.Value;
        originalVerticalAxis = orbitalFollow.VerticalAxis.Value;

        float elapsedTime = 0f;
        float startAngle = orbitalFollow.HorizontalAxis.Value;
        float targetAngle = startAngle + 180f; // orbits 180 degrees around player;

        float startVertical = orbitalFollow.VerticalAxis.Value;
        float targetVertical = 0.3f;

        // phase 1: 0rbit around player with camera movement;
        while (elapsedTime < cutsceneDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / cutsceneDuration;
            float curveT = transitionCurve.Evaluate(t);

            // smoothly orbit horizontally;
            float currentAngle = Mathf.Lerp(startAngle, targetAngle, curveT);
            orbitalFollow.HorizontalAxis.Value = currentAngle;

            // lets add a slight vertical movement for dramatic effect;
            if (t < 0.5f)
            {
                float verticalT = t / 0.5f;
                orbitalFollow.VerticalAxis.Value = Mathf.Lerp(startVertical, targetVertical, transitionCurve.Evaluate(verticalT));
            }
            else
            {
                float verticalT = (t - 0.5f) / 0.5f;
                orbitalFollow.VerticalAxis.Value = Mathf.Lerp(targetVertical, startVertical, transitionCurve.Evaluate(verticalT));
            }

            yield return null;
        }

        // ok now phase 2: we shall return to original camera position;
        elapsedTime = 0f;
        float returnDuration = 0.3f;
        float currentHorizontal = orbitalFollow.HorizontalAxis.Value;
        float currentVertical = orbitalFollow.VerticalAxis.Value;

        while (elapsedTime < returnDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / returnDuration;
            float curveT = transitionCurve.Evaluate(t);

            orbitalFollow.HorizontalAxis.Value = Mathf.Lerp(currentHorizontal, originalHorizontalAxis, curveT);
            orbitalFollow.VerticalAxis.Value = Mathf.Lerp(currentVertical, originalVerticalAxis, curveT);

            yield return null;
        }

        // lets ensure exact return to original;
        orbitalFollow.HorizontalAxis.Value = originalHorizontalAxis;
        orbitalFollow.VerticalAxis.Value = originalVerticalAxis;

        isInCutscene = false;
    }

    public bool IsInCutscene()
    {
        return isInCutscene;
    }
}