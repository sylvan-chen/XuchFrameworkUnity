using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ShaderEditorGUI
{
    // 设置材质球渲染的表面类型，会影响实体的渲染顺序，Opaque：不透明实体，Transparent：透明实体
    public enum SurfaceType
    {
        Opaque,
        Transparent
    }

    // 设置渲染透明物体的混合模式，常见的混合模式有Alpha， Premultiphy, Additive 和 Multiply四种模式：
    // Alpha模式：是一种传统的混合模式，不考虑透明度的影响。计算公式：out = src * src_alpha + dest * (1 - src_alpha)
    // Premultiphy混合模式： 这是一种物理上可行的透明度混合模式，通过预乘源颜色的透明度来实现。公式：out = src + dest * (1 - src_alpha)
    // Additive混合模式： 这是一种加法混合模式，用于实现光照、粒子效果等。计算公式为：out = src + dest
    // Multiply混合模式：这是一种乘法混合模式，用于实现阴影，叠加效果等。公式：out = src * dest
    public enum BlendMode
    {
        Alpha,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
        Premultiply, // Physically plausible transparency mode, implemented as alpha pre-multiply
        Additive,
        //Multiply
    }

    /// <summary>
    /// Enum representing the source of smoothness value.
    /// </summary>
    public enum SmoothnessSource
    {
        BaseAlpha,       // Smoothness value is derived from the base alpha channel.
        SpecularAlpha    // Smoothness value is derived from the specular alpha channel.
    }


    /// <summary>
    /// Enum representing the face(s) to render.
    /// </summary>
    public enum RenderFace
    {
        Front = 2,   // Render only the front face
        Back = 1,    // Render only the back face
        Both = 0     // Render both the front and back faces
    }

    /// <summary>
    /// Enum representing the shading mode for the shader.
    /// </summary>
    public enum ShadingMode
    {
        Standard,       // Standard shading mode
        Skin,           // Skin shading mode
        Fabric,         // Fabric shading mode
        ClearCoat,      // Clear coat shading mode
        Anisotropic,    // Anisotropic shading mode
    }
    
    /// <summary>
    /// Enum representing the mask channels for the shader.
    /// </summary>
    public enum MaskChannel
    {
        None = 0,                   // No mask channel
        ChannelR = 1,               // Mask channel for red color
        ChannelG = 2,               // Mask channel for green color
        ChannelB = 3,               // Mask channel for blue color
        ChannelA = 4,               // Mask channel for alpha (transparency)
        OneMinusChannelR = 5,       // Inverted mask channel for red color
        OneMinusChannelG = 6,       // Inverted mask channel for green color
        OneMinusChannelB = 7,       // Inverted mask channel for blue color
        OneMinusChannelA = 8,       // Inverted mask channel for alpha (transparency)
    }


    /// <summary>
    /// Enum representing the effect mode for the shader.
    /// </summary>
    public enum EffectMode
    {
        None,     // No effect
        Laser,    // Laser effect
        Glitter   // Glitter effect
    }

    
    /// <summary>
    /// Enum representing the mode for the detail effect.
    /// </summary>
    public enum DetailEffectMode
    {
        None,    // No detail effect
        Detail,  // Detail effect
        Flow,    // Flow effect
    }

    /// <summary>
    /// Enum representing the type of sparkle effect.
    /// </summary>
    public enum SparkleType
    {
        None,        // No sparkle effect
        Euclidean,   // Euclidean sparkle effect
        Minkowski    // Minkowski sparkle effect
    }

}