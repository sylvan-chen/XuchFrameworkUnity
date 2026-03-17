using UnityEngine;

public static class AssignNewShaderHelper
{
    public static void CopyTextureProperty(this Material material, string oldName, string newName, bool withST = true)
    {
        if (material == null) return;

        if (!material.HasProperty(oldName)) return;

        material.SetTexture(newName, material.GetTexture(oldName));
        if (withST)
        {
            material.SetTextureScale(newName, material.GetTextureScale(oldName));
            material.SetTextureOffset(newName, material.GetTextureOffset(oldName));
        }
    }

    public static UnityEngine.Vector4 GetTextureST(this Material material, string name)
    {
        UnityEngine.Vector2 vec2Scale = material.GetTextureScale(name);
        UnityEngine.Vector2 vec2Offset = material.GetTextureOffset(name);
        var vec4ST = new UnityEngine.Vector4(vec2Scale.x, vec2Scale.y,
                                                vec2Offset.x, vec2Offset.y);

        return vec4ST;
    }

    public static Color TryGetColor(this Material material, string name, Color defaultColor)
    {
        if (material.HasProperty(name))
            return material.GetColor(name);
        return defaultColor;
    }

    public static float TryGetFloat(this Material material, string name, float defaultValue)
    {
        if (material.HasProperty(name))
            return material.GetFloat(name);
        return defaultValue;
    }
}