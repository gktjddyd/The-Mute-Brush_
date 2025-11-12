using UnityEngine;

public class Mesh3DPenLineHolder : MonoBehaviour
{
    [Tooltip("All Mesh 3D Pen lines in the scene")]
    public Mesh3DPenLine[] mesh3DPenLines;

    public void ClearAllLines()
    {
        if (mesh3DPenLines == null) return;
        foreach (Mesh3DPenLine line in mesh3DPenLines)
        {
            if (line != null)
            {
                line.Clear();
            }
        }
    }

    public int GetLineIndex(Mesh3DPenLine line)
    {
        if (mesh3DPenLines == null) return -1;
        for (int i = 0; i < mesh3DPenLines.Length; i++)
        {
            if (mesh3DPenLines[i] == line)
                return i;
        }
        return -1;
    }
}
