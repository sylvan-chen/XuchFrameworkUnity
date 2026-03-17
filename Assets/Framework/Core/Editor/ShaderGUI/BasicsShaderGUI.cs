using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace ShaderEditorGUI
{
    public class BasicsShaderGUI : ShaderGUI
    {
        public enum BlendMode
        {
            Opaque,
            Cutout,
            Fade,
            Transparent,
            Additive,
        }
    }
}
