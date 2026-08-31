using System.Collections.Generic;
using UnityEngine;

public class PuckVisuals : MonoBehaviour
{
    [Header("Visual Effects")]
    [SerializeField] private GameObject _impactVFX;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;

    // Cache the original colors to restore them after ghosting
    private readonly Dictionary<Renderer, Color> _originalColors = new Dictionary<Renderer, Color>();

    // Hash the shader properties for faster lookup
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new MaterialPropertyBlock();

        // Read from sharedMaterial to avoid generating material instances
        foreach (Renderer ren in _renderers)
        {
            if (ren != null && ren.sharedMaterial != null)
            {
                Color defaultColor = Color.white; // Fallback

                if (ren.sharedMaterial.HasProperty(BaseColorID))
                {
                    defaultColor = ren.sharedMaterial.GetColor(BaseColorID);
                }
                else if (ren.sharedMaterial.HasProperty(ColorID))
                {
                    defaultColor = ren.sharedMaterial.GetColor(ColorID);
                }

                _originalColors[ren] = defaultColor;
            }
        }
    }

    public void SetGhostVisuals(bool isGhost)
    {
        float targetAlpha = isGhost ? 0.5f : 1.0f;

        foreach (Renderer ren in _renderers)
        {
            if (ren != null && _originalColors.TryGetValue(ren, out Color origColor))
            {
                ren.GetPropertyBlock(_propBlock);

                // Construct the new color using the original RGB values and the new alpha
                Color newColor = new(origColor.r, origColor.g, origColor.b, targetAlpha);

                // Apply to the property block
                if (ren.sharedMaterial.HasProperty(BaseColorID))
                {
                    _propBlock.SetColor(BaseColorID, newColor);
                }
                if (ren.sharedMaterial.HasProperty(ColorID))
                {
                    _propBlock.SetColor(ColorID, newColor);
                }

                ren.SetPropertyBlock(_propBlock);
            }
        }
    }

    public void PlayImpact(Vector3 position, Vector3 normal)
    {
        if (_impactVFX != null)
        {
            // Object pooling may eventually replace this
            Instantiate(_impactVFX, position, Quaternion.LookRotation(normal));
        }
    }
}