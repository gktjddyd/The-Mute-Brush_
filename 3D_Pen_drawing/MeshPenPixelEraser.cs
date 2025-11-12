using UnityEngine;
using System.Collections;

public class MeshPenPixelEraser : MonoBehaviour
{
    [SerializeField] 
    private Mesh3DPenLineHolder lineHolder;

    [SerializeField] 
    private float eraseRadius = 0.1f;

    public bool eraserOn = false; 
    private bool erasing = false;
    private bool isHeld = false;

    private void Update()
    {
        // 만약 "erasing" 상태라면, 각 라인에 픽셀 단위 지우기 CheckPixelEraseLine
        if (eraserOn && erasing && lineHolder != null && lineHolder.mesh3DPenLines != null)
        {
            foreach (Mesh3DPenLine line in lineHolder.mesh3DPenLines)
            {
                line.CheckEraseLine(transform.position, eraseRadius);
            }
        }

    }

    public void OnEraserPickedUp()
    {
        isHeld = true;
    }

    public void OnEraserDropped()
    {
        isHeld = false;
        StopErasing();
    }

    public void StartErasing()
    {
        if (!erasing) erasing = true;
    }

    public void StopErasing()
    {
        if (erasing) erasing = false;
    }

}
