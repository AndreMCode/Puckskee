using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float velocityWeight = 0.6f; // 0 = avg only, 1 = peak only
    [SerializeField] private float velocityThreshold = 0.5f; // Minimum change to count as "moving"
    private static WaitForSeconds _waitForSeconds0_1 = new(0.1f);
    private static WaitForSeconds _waitForSeconds1 = new(1f);

    private GameObject cameraObject;
    private float orbitYaw = 0f;
    private float orbitPitch = 20f;
    // Camera variables
    [Header("Camera Settings")]
    public float minPitch = 4f;
    public float maxPitch = 80f;
    public float orbitDistance = 40f;
    public float minDistance = 20f;
    public float maxDistance = 80f;
    public float orbitSpeed = 256f;
    public float zoomSpeed = 20f;

    // Player state variables
    [Header("Player State")]
    public bool aiming = false;
    public bool shooting = false;
    public bool sliding = false;
    public bool waiting = false;

    private GameObject striker;
    [Header("Striker Settings")]
    public float strikerMinDistance = 2f;
    public float strikerMaxDistance = 8f;
    public float strikerMoveSpeed = 64f;
    public float maxStrikerVelocity = 4f;

    // Stroke tracking variables
    private float strokeStartTime;
    private float strokeStartDistance;
    private float strokeMaxVelocity;
    private bool strokeInProgress = false;
    private float lastFrameVelocity;
    private int highVelocityFrames;

    [Header("Cue Settings")]
    public float mouseSensitivity = 1f; // Implement as range from 0.25 to 4 for HUD later

    private Vector3 cameraAngles;

    private Rigidbody rb;
    // Physics variables
    [Header("Physics Settings")]
    public float impulseForce = 10f;
    public float maxGoalVelocity = 10f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false; // Hide the cursor

        rb = GetComponent<Rigidbody>();
        cameraObject = GameObject.Find("Main Camera");
        striker = GameObject.Find("Cue Tip");

        striker.SetActive(false);

        cameraObject.transform.LookAt(transform.position);
        UpdateCameraAngles();
        StartCoroutine(WaitOneMoment());
    }

    void Update()
    {
        if (aiming)
        {
            AllowCameraControl();
        }

        if (Input.GetKeyDown(KeyCode.Space) && aiming)
        {
            shooting = true;
            aiming = false;
            PrepareStriker();
        }

        if (shooting)
        {
            AllowCueControl();
        }

        if (Input.GetKeyDown(KeyCode.B) && shooting)
        {
            shooting = false;
            aiming = true;
            striker.SetActive(false);
        }

        if (sliding)
        {
            TrackPlayer();
        }
    }

    private void UpdateCameraAngles()
    {
        cameraAngles = cameraObject.transform.rotation.eulerAngles;
        orbitYaw = cameraAngles.y;
        orbitPitch = cameraAngles.x;
    }

    void AllowCameraControl()
    {
        // Orbit
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        orbitYaw += mouseX * orbitSpeed * Time.deltaTime;
        orbitPitch -= mouseY * orbitSpeed * Time.deltaTime;
        orbitPitch = Mathf.Clamp(orbitPitch, minPitch, maxPitch);

        // Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        orbitDistance -= scroll * zoomSpeed;
        orbitDistance = Mathf.Clamp(orbitDistance, minDistance, maxDistance);

        // Calculate camera position
        Vector3 offset = Quaternion.Euler(orbitPitch, orbitYaw, 0) * new Vector3(0, 0, -orbitDistance);
        cameraObject.transform.position = transform.position + offset;
        cameraObject.transform.LookAt(transform.position);
    }

    void AllowCueControl()
    {
        float moveInput = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        // Direction from player to striker, ignoring Y
        Vector3 direction = striker.transform.position - transform.position;
        direction.y = 0;
        float currentDistance = direction.magnitude;
        direction.Normalize();

        // Apply movement with clamping
        float moveAmount = strikerMoveSpeed * moveInput * Time.deltaTime;
        float targetDistance = Mathf.Clamp(currentDistance - moveAmount, strikerMinDistance, strikerMaxDistance);
        striker.transform.position = transform.position + direction * targetDistance;

        // Track the stroke
        if (moveInput > 0 && !strokeInProgress)
        {
            // Start of stroke
            strokeInProgress = true;
            strokeStartTime = Time.time;
            strokeStartDistance = currentDistance;
            strokeMaxVelocity = 0f;
            lastFrameVelocity = 0f;
            highVelocityFrames = 0;
        }
        else if (moveInput > 0 && strokeInProgress)
        {
            // Only consider velocities above threshold
            if (moveInput > velocityThreshold)
            {
                if (lastFrameVelocity > velocityThreshold)
                    highVelocityFrames++;
                else
                    highVelocityFrames = 1; // Reset if last frame was low

                if (highVelocityFrames >= 2) // Require 2 consecutive frames
                    strokeMaxVelocity = Mathf.Max(strokeMaxVelocity, moveInput);
            }

            lastFrameVelocity = moveInput;
        }
        else if (moveInput < 0 && strokeInProgress)
        {
            // Cancel stroke if pulled back
            strokeInProgress = false;
        }

        // Handle stroke completion
        if (strokeInProgress && currentDistance <= strikerMinDistance + 0.05f)
        {
            float deltaTime = Time.time - strokeStartTime;
            float deltaDistance = strokeStartDistance - currentDistance;

            // Average forward velocity
            float avgVelocity = (deltaTime > 0f) ? deltaDistance / deltaTime : 0f;

            // Blend average + peak
            float blendedVelocity = Mathf.Lerp(avgVelocity, strokeMaxVelocity, velocityWeight);

            // Clamp to max striker velocity
            float finalVelocity = Mathf.Clamp(blendedVelocity, 0f, maxStrikerVelocity);

            // Camera forward direction, ignoring Y
            Vector3 cameraForward = cameraObject.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            Debug.Log($"Avg: {avgVelocity:F2}, Peak: {strokeMaxVelocity:F2}, Final: {finalVelocity:F2}");

            // Apply impulse
            rb.AddForce(finalVelocity * impulseForce * cameraForward, ForceMode.Impulse);

            // Reset, swap states
            strokeInProgress = false;
            shooting = false;
            sliding = true;
            StartCoroutine(WaitForMotionStop());
        }
    }

    void PrepareStriker()
    {
        // Calculate direction from player to camera, ignoring Y
        Vector3 directionToCamera = cameraObject.transform.position - transform.position;
        directionToCamera.y = 0;
        directionToCamera = directionToCamera.normalized;

        // Position striker 4 units from player towards camera
        Vector3 strikerPos = transform.position + directionToCamera * 4f;
        striker.transform.position = strikerPos;

        // Align striker's rotation to camera's Y-axis only
        Vector3 cameraEuler = cameraObject.transform.rotation.eulerAngles;
        striker.transform.rotation = Quaternion.Euler(0, cameraEuler.y, 0);

        striker.SetActive(true);
    }

    void TrackPlayer()
    {
        // Keep camera looking at player while shooting
        cameraObject.transform.LookAt(transform.position);

        // Update orbit angles for the next camera step
        UpdateCameraAngles();
    }

    private IEnumerator WaitForMotionStop()
    {
        // Wait for physics to apply the impulse force
        yield return _waitForSeconds0_1;

        // Wait until the player stops moving
        while (rb.linearVelocity.magnitude > 0.1f)
        {
            yield return null;
        }

        sliding = false;
        waiting = true;
        StartCoroutine(WaitOneMoment());
    }

    private IEnumerator WaitOneMoment()
    {
        Vector3 targetPos = transform.position;

        // for later use with a second player
        //Quaternion targetRot = Quaternion.LookRotation(targetPos - cameraObject.transform.position);
        //StartCoroutine(LerpCameraRotation(cameraObject.transform.rotation, targetRot, 1f));

        yield return _waitForSeconds1;

        striker.SetActive(false);
        StartCoroutine(LerpCameraPosition(cameraObject.transform.position, targetPos, 1f, orbitDistance));
    }

    private IEnumerator LerpCameraPosition(Vector3 startPos, Vector3 endPos, float duration, float minDistance)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            Vector3 lerpedPos = Vector3.Lerp(startPos, endPos, elapsed / duration);
            Vector3 direction = (lerpedPos - transform.position).normalized;
            float distance = Vector3.Distance(lerpedPos, transform.position);

            // Clamp the distance
            if (distance < minDistance)
            {
                elapsed = duration; // Force end of lerp
                lerpedPos = transform.position + direction * minDistance;
            }

            cameraObject.transform.position = lerpedPos;
            cameraObject.transform.LookAt(transform.position);

            elapsed += Time.deltaTime;
            yield return null;
        }
        waiting = false;
        aiming = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal") && rb.linearVelocity.magnitude <= maxGoalVelocity)
        {
            Debug.Log("Goal Reached!" + rb.linearVelocity.magnitude);
            // Implement goal reached logic ...
        }
    }

    // for later use with a second player
    // private IEnumerator LerpCameraRotation(Quaternion startRot, Quaternion endRot, float duration)
    // {
    //     float elapsed = 0f;
    //     while (elapsed < duration)
    //     {
    //         cameraObject.transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
    //         elapsed += Time.deltaTime;
    //         yield return null;
    //     }
    //     cameraObject.transform.rotation = endRot;

    //     UpdateCameraAngles();
    // }
}
