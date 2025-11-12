using UnityEngine;

public class Mesh3DPenPaletteColor : MonoBehaviour
{
    [Tooltip("The color index this pen palette color applies")]
    public int colorIndex = 0;

    private void OnTriggerEnter(Collider other)
    {
        Mesh3DPen pen = other.GetComponentInParent<Mesh3DPen>();
        if (pen != null)
        {
            pen.SetColor(colorIndex);
        }
    }
}
