using UnityEngine;


public class Mesh3DPenPalette : MonoBehaviour
{
    [Tooltip("The root gameObject of the palette (for enabling/disabling)")]
    [SerializeField] 
    private GameObject paletteRoot;
    public void PenPickedUp()
    {
        if (paletteRoot != null)
            paletteRoot.SetActive(true);
    }

    public void PenDropped()
    {
        if (paletteRoot != null)
            paletteRoot.SetActive(false);
    }
}
