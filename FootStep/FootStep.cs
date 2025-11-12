using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class XRButtonFootstep : MonoBehaviour
{
    [Header("XR Input")]
    public XRNode inputSource = XRNode.LeftHand;
    public InputHelpers.Button inputButton = InputHelpers.Button.PrimaryAxis2DUp;
    public InputHelpers.Button inputButton2 = InputHelpers.Button.PrimaryAxis2DDown;
    public InputHelpers.Button inputButton3 = InputHelpers.Button.PrimaryAxis2DRight;
    public InputHelpers.Button inputButton4 = InputHelpers.Button.PrimaryAxis2DLeft;
    public float inputThreshold = 0.1f;

    [Header("Movement Check")]
    public Transform xrOrigin;                   // XR Transform
    public float movementThreshold = 0.01f;     // 최소 이동 거리

    [Header("Footstep Settings")]
    public AudioSource audioSource;
    public AudioClip[] footstepClipsFloor;
    public AudioClip[] footstepClipsWater;

    public AudioClip[] footstepClipsStone;
    public float stepInterval = 0.6f;

    private float stepTimer = 0f;
    private Vector3 lastPosition;

    [Header("Footstep Different Collection")]
    RaycastHit hit;
    public Transform footRay;
    public float range;
    public LayerMask layerMask;
    void Start()
    {
        lastPosition = xrOrigin.position;
    }

    void Update()
    {
        //Debug.DrawRay(footRay.position, -footRay.up , Color.green);
        bool isPressed1 = false, isPressed2 = false, isPressed3 = false, isPressed4 = false;
        var device = InputDevices.GetDeviceAtXRNode(inputSource);

        InputHelpers.IsPressed(device, inputButton,  out isPressed1, inputThreshold);
        InputHelpers.IsPressed(device, inputButton2, out isPressed2, inputThreshold);
        InputHelpers.IsPressed(device, inputButton3, out isPressed3, inputThreshold);
        InputHelpers.IsPressed(device, inputButton4, out isPressed4, inputThreshold);

        bool anyPressed = isPressed1 || isPressed2 || isPressed3 || isPressed4;

        // 실제 이동량 계산
        float moveDelta = Vector3.Distance(xrOrigin.position, lastPosition);
        bool isMoving = moveDelta > movementThreshold;
        lastPosition = xrOrigin.position;

        if (anyPressed && isMoving)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                PlayFootstep();
                stepTimer -= stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    private void PlayFootstep(){
        if (audioSource == null) return;
        Vector3 dir = -footRay.up;
        if (Physics.Raycast(footRay.position, dir, out hit, range, layerMask)){
            if (!audioSource.isPlaying){
                if (hit.collider.CompareTag("floor") && footstepClipsFloor.Length > 0){
                    audioSource.clip = footstepClipsFloor[Random.Range(0, footstepClipsFloor.Length)];
                    audioSource.Play();
                }
                else if (hit.collider.CompareTag("water") && footstepClipsWater.Length > 0){
                    audioSource.clip = footstepClipsWater[Random.Range(0, footstepClipsWater.Length)];
                    audioSource.Play();
                }
                else if (hit.collider.CompareTag("stone") && footstepClipsStone.Length > 0){
                    audioSource.clip = footstepClipsStone[Random.Range(0, footstepClipsStone.Length)];
                    audioSource.Play();
                }
            }
        }
    }
}
