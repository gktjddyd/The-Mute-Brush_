using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class XRGrabInteractableWithPalette : XRGrabInteractable
{
    [Header("XR Node Settings")]
    [SerializeField] private XRNode inputSource1;           
    [SerializeField] private InputHelpers.Button inputButton1;
    [SerializeField] private XRNode inputSource2;           
    [SerializeField] private InputHelpers.Button inputButton2;
    [SerializeField] private XRNode inputSource3;           
    [SerializeField] private InputHelpers.Button inputButton3;
    [SerializeField] private XRNode inputSource4;           
    [SerializeField] private InputHelpers.Button inputButton4;
    [SerializeField] private float inputThreshold = 0.1f;

    [Header("Palette Objects [반대로 넣어주세요.]")]
    [SerializeField] private GameObject leftPalette;
    [SerializeField] private GameObject rightPalette;

    [Header("Toggle Object")]
    [SerializeField] private GameObject togglePenObject;
    [SerializeField] private GameObject toggleEraserObject; 

    [Header("Draw || Erase")]
    [SerializeField] private Mesh3DPen mesh3DPen;
    [SerializeField] private MeshPenPixelEraser meshPenPixelEraser; 
    [SerializeField] public bool noEraserChange = false;

    [Header("하이라이트 설정")]
    [SerializeField] GameObject penHighlight;
    [SerializeField] GameObject eraserHighlight;

    // 이 오브젝트가 현재 "잡혀" 있는지 여부
    private bool isGrabbed = false;

    // 왼손/오른손 1번 버튼의 ‘이전 프레임’ 상태
    private bool wasPressed3 = false; // 왼손 1번 버튼
    private bool wasPressed4 = false; // 오른손 1번 버튼

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        isGrabbed = true;
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        isGrabbed = false;

        // 놓는 순간 팔레트를 모두 꺼준다
        if (leftPalette != null)
            leftPalette.SetActive(false);

        if (rightPalette != null)
            rightPalette.SetActive(false);

        if (togglePenObject.activeSelf){
            penHighlight.SetActive(true);
        }

        if (toggleEraserObject.activeSelf){
            eraserHighlight.SetActive(true);
        }
    }

    private void Update()
    {
        // 잡고 있지 않다면 버튼 검사할 필요 없음
        if (!isGrabbed)
            return;

        // 버튼 상태를 폴링
        bool isPressed1 = false;
        bool isPressed2 = false;
        bool isPressed3 = false; // 왼손 1번 버튼(토글용)
        bool isPressed4 = false; // 오른손 1번 버튼(토글용)

        // 왼손 (Palette On/Off)
        InputHelpers.IsPressed(
            InputDevices.GetDeviceAtXRNode(inputSource1),
            inputButton1,
            out isPressed1,
            inputThreshold
        );

        // 오른손 (Palette On/Off)
        InputHelpers.IsPressed(
            InputDevices.GetDeviceAtXRNode(inputSource2),
            inputButton2,
            out isPressed2,
            inputThreshold
        );

        // 왼손 1번 버튼 (Toggle용)
        InputHelpers.IsPressed(
            InputDevices.GetDeviceAtXRNode(inputSource3),
            inputButton3,
            out isPressed3,
            inputThreshold
        );

        // 오른손 1번 버튼 (Toggle용)
        InputHelpers.IsPressed(
            InputDevices.GetDeviceAtXRNode(inputSource4),
            inputButton4,
            out isPressed4,
            inputThreshold
        );
        
        //하이라이트
        penHighlight.SetActive(!(isPressed1 || isPressed2));
        eraserHighlight.SetActive(!(isPressed1 || isPressed2));
        
        
        // 왼손/오른손 버튼을 누르는 동안 팔레트 On/Off
        if (leftPalette != null){
            leftPalette.SetActive(isPressed1);
        }

        if (rightPalette != null){
            rightPalette.SetActive(isPressed2);
        }

        // 왼손 1번 버튼 "이번 프레임에 새로 눌린" 상태
        bool justPressedLeft = (isPressed3 && !wasPressed3);
        // 오른손 1번 버튼 "이번 프레임에 새로 눌린" 상태
        bool justPressedRight = (isPressed4 && !wasPressed4);

        if (noEraserChange) {
                togglePenObject.SetActive(true);
                toggleEraserObject.SetActive(false);
                mesh3DPen.drawing = true;
                meshPenPixelEraser.eraserOn = false;
                return;
            }

        // 왼손 1번 혹은 오른손 1번 버튼 "둘 중 하나라도" 새롭게 눌렸으면 toggle
        /*
        if ((justPressedLeft || justPressedRight)){
            if (togglePenObject.activeSelf) togglePenObject.SetActive(false);
            else if (!togglePenObject.activeSelf) togglePenObject.SetActive(true);

            if (toggleEraserObject.activeSelf) toggleEraserObject.SetActive(false);
            else if (!toggleEraserObject.activeSelf) toggleEraserObject.SetActive(true);

            if (mesh3DPen.drawing) mesh3DPen.drawing = false;
            else if (!mesh3DPen.drawing) mesh3DPen.drawing = true;

            if (meshPenPixelEraser.eraserOn) meshPenPixelEraser.eraserOn = false;
            else if (!meshPenPixelEraser.eraserOn) meshPenPixelEraser.eraserOn = true;
        }
        */
        // 현재 프레임의 버튼 상태 저장(다음 프레임 비교용)
        wasPressed3 = isPressed3;
        wasPressed4 = isPressed4;
    }
}
