using UnityEngine;



/// (지우개) 로직.
public class Mesh3DPenEraser : MonoBehaviour
{
    #region Variables

    [SerializeField] 
    private Mesh3DPenLineHolder penLineHolder;

    [SerializeField] 
    private float eraseRadius = 0.2f;

    [SerializeField] 
    private float checkEraseFrequency = 0.5f;

   
    [SerializeField] 
    private bool clearAll = false;

    private float lastCheckEraseTime = -Mathf.Infinity; 
    private bool isHeld = false; 
    private bool clearedMarks = false;

    #endregion

    #region Unity Methods

    private void Update()
    {
        if (!penLineHolder)
            return;

        // 에러서가 잡혀있다면, 일정 간격으로 라인에 mark 체크
        if (isHeld)
        {
            if (Time.time - lastCheckEraseTime > checkEraseFrequency)
            {
                Mark();
                lastCheckEraseTime = Time.time;
                clearedMarks = false;
            }
        }
        else if (!clearedMarks)
        {
            // 잡고 있지 않으면, 이전에 마킹된 라인을 해제
            clearedMarks = true;
            ClearMarks();
        }
    }

    #endregion

    #region Public Methods 

    // XR Grab Interactable에서 Select Entered 등으로 이 에러서를 집었을 때.
    public void OnEraserPickedUp()
    {
        isHeld = true;
    }

    // XR Grab Interactable에서 Select Exited 등으로 이 에러서를 놓았을 때.

    public void OnEraserDropped()
    {
        isHeld = false;
        ClearMarks();
    }


    // XR Grab Interactable에서 Activate를 눌렀을 때.

    public void OnEraserUseDown()
    {
        Erase();
    }

    #endregion

    #region Private Methods


    /// 현재 지우기로 지정된 선을 표시
    private void Mark()
    {
        if (penLineHolder == null || penLineHolder.mesh3DPenLines == null)
            return;

        foreach (Mesh3DPenLine penLine in penLineHolder.mesh3DPenLines)
        {
            penLine?.CheckMarkLine(transform.position, eraseRadius);
        }
    }



    private void ClearMarks()
    {
        if (penLineHolder == null || penLineHolder.mesh3DPenLines == null)
            return;

        foreach (Mesh3DPenLine penLine in penLineHolder.mesh3DPenLines)
        {
            penLine?.ClearMark();
        }
    }

   
    

    private void Erase()
    {
        if (penLineHolder == null || penLineHolder.mesh3DPenLines == null)
            return;

        foreach (Mesh3DPenLine penLine in penLineHolder.mesh3DPenLines)
        {
            // clearAll이 true면 penLine.CheckClearLines, 아니면 penLine.CheckEraseLine
            if (clearAll)
                penLine.CheckClearLines(transform.position, eraseRadius);
            else
                penLine.CheckEraseLine(transform.position, eraseRadius);
        }
    }

    #endregion
}
