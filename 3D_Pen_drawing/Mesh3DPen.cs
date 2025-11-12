using UnityEngine;

public class Mesh3DPen : MonoBehaviour
{
    #region Variables

    public Mesh3DPenLine line;

    [SerializeField] 
    private Mesh3DPenPalette palette;

    [SerializeField] 
    private Transform tip;

    [SerializeField] 
    private MeshRenderer meshRenderer;


    [SerializeField] 
    private string colorProperty = "_Color";


    [SerializeField] 
    private MonoBehaviour xrGrabInteractable; 
    
    
    [Header("Boolean Draw")]
    [SerializeField]
    public bool drawing = true;
    #endregion

    #region Unity Methods

    private void Start()
    {
        UpdateLineOwnership();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (line != null)
            {
                Debug.Log("[Mesh3DPen] Changing color (IncrementColor).");
                line.IncrementColor();
            }
        }
    }

    #endregion

    #region Public XR / Interaction Methods

  
    public void OnPenPickedUp()
    {
        //펜 들기
        if (palette != null)
        {
            // palette에서 PenPickedUp 호출
            palette.PenPickedUp();
        }
    }


    // 펜 놓기
    public void OnPenDropped()
    {
        // 원본 OnDrop() 이벤트 대체
        if (palette != null)
        {
            palette.PenDropped();
        }
    }


    // XR Grab Interactable에서 Activate(검지버튼 누를) 시 호출
    public void OnPenUseDown()
    {
        if (!drawing) { 
            line.StopDrawing();
        }
        // 원본 OnPickupUseDown() 이벤트 대체
        else if (drawing){
            line.StartDrawing();
        }
    }


    /// XR Grab Interactable에서 'Deactivate'(검지 버튼 땔) 시 호출
    public void OnPenUseUp()
    {
        // 원본 OnPickupUseUp() 이벤트 대체
        if (line != null)
        {
            line.StopDrawing();
        }
    }

    #endregion

    #region Public Methods

    public void SetColor(int value)
    {
        if (line != null)
        {
            line.SetColor(value);
        }
    }

    public Transform GetTip()
    {
        return tip;
    }

    public void SetPenModelColor(Color color)
    {
        if (meshRenderer != null && !string.IsNullOrEmpty(colorProperty))
        {
            meshRenderer.material.SetColor(colorProperty, color);
        }
    }

    #endregion

    #region Private Methods

   
    private void UpdateLineOwnership()
    {
        if (line != null)
        {
           //통신 시도 실패.
        }
    }

    #endregion
}
