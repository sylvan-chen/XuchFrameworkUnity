using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderEditorGUI
{
    public static partial class Styles
    {
        /// <summary>
        /// Controls the shading mode of the material.
        /// </summary>
        public static readonly GUIContent ShadingModes =
            new GUIContent("Shading Modes", "Controls the shading mode of the material.");

        /// <summary>
        /// Controls the options for the surface rendering in Universal RP. Title String
        /// </summary>
        public static readonly GUIContent SurfaceOptions =
            new GUIContent("Surface Options", "Controls how Universal RP renders the Material on a screen.");

        /// <summary>
        /// Controls the surface inputs of the material.
        /// </summary>
        public static readonly GUIContent SurfaceInputs = new GUIContent("Surface Inputs",
            "These settings describe the look and feel of the surface itself.");

        /// <summary
        /// 这是一个高级设置, Title string
        /// </summary>
        public static readonly GUIContent AdvancedLabel = new GUIContent("Advanced",
            "These settings affect behind-the-scenes rendering and underlying calculations.");

        /// <summary>
        /// 表面类型，Opaque or Transparent
        /// </summary>
        public static readonly GUIContent surfaceType = new GUIContent("Surface Type",
            "Select a surface type for your texture. Choose between Opaque or Transparent.");

        /// <summary>
        /// 混合模式
        /// </summary>
        public static readonly GUIContent blendingMode = new GUIContent("Blending Mode",
            "Controls how the color of the Transparent surface blends with the Material color in the background.");

        // Render Face
        public static readonly GUIContent cullingText = new GUIContent("Render Face",
            "Specifies which faces to cull from your geometry. Front culls front faces. Back culls backfaces. None means that both sides are rendered.");

        /// <summary>
        /// Controls the alpha clipping of the material.
        /// </summary>
        public static readonly GUIContent alphaClipText = new GUIContent("Alpha Clipping",
            "Makes your Material act like a Cutout shader. Use this to create a transparent effect with hard edges between opaque and transparent areas.");
        /// <summary>
        /// Threshold for alpha clipping.
        /// </summary>
        public static readonly GUIContent alphaClipThresholdText = new GUIContent("Threshold",
            "Sets where the Alpha Clipping starts. The higher the value is, the brighter the effect is when clipping starts.");

        // This controls whether the GameObject can receive shadows from other GameObjects
        public static readonly GUIContent receiveShadowText = new GUIContent("Receive Shadows",
            "When enabled, other GameObjects can cast shadows onto this GameObject.");

        /// <summary>
        /// Specifies the base Material and/or Color of the surface. If you've selected Transparent or Alpha Clipping under Surface Options, your Material uses the Texture's alpha channel or color.
        /// </summary>
        public static readonly GUIContent baseMap = new GUIContent("Base Map",
            "Specifies the base Material and/or Color of the surface. If you've selected Transparent or Alpha Clipping under Surface Options, your Material uses the Texture's alpha channel or color.");

        /// <summary>
        /// Sets a Texture map to use for emission. You can also select a color with the color picker. Colors are multiplied over the Texture.
        /// </summary>
        public static readonly GUIContent emissionMap = new GUIContent("Emission Map",
            "Sets a Texture map to use for emission. You can also select a color with the color picker. Colors are multiplied over the Texture.");

        // 这是一个法线贴图
        public static readonly GUIContent normalMapText =
            new GUIContent("Normal Map", "Assigns a tangent-space normal map.");

        public static readonly GUIContent bumpScaleNotSupported =
            new GUIContent("Bump scale is not supported on mobile platforms");
            
        /// <summary>
        /// Converts the assigned texture to be a normal map format.
        /// </summary>
        public static readonly GUIContent fixNormalNow = new GUIContent("Fix now",
            "Converts the assigned texture to be a normal map format.");

        // 这是一个优先级滑块
        public static readonly GUIContent queueSlider = new GUIContent("Priority",
            "Determines the chronological rendering order for a Material. High values are rendered first.");

        // 这是一个着色模式
        public static string shadingMode = "Shading Mode";

        /// <summary>
        /// Controls the power of the material.
        /// </summary>
        public static readonly GUIContent powerText = new GUIContent("Power", "亮度");

        // 这是一个法线贴图
        public static GUIContent flakesBumpMapText = EditorGUIUtility.TrTextContent("Normal Map", "Normal Map");

        public static GUIContent anisotropicDirectionText = EditorGUIUtility.TrTextContent("Direction", "Anisotropic Direction");
        // 这是一个遮罩贴图，用于控制环境光遮蔽、光滑度和金属度
        public static GUIContent maskMapText = EditorGUIUtility.TrTextContent("AO(R) Smooth(G) Metallic(B)");

        // 平滑度文本
        public static string smoothnessText = "Smoothness";

        // 金属度文本
        public static string metallicText = "Metallic";

        // 遮挡强度文本
        public static string occlusionText = "Occlusion Strength";

        // 高光强度文本
        public static string specularPowerText = "Specular Power";

        // 效果模式
        public static string effectMode = "Effect Mode";

        // 自定义环境光强度
        public static string useCustomVertexLightingText = "Use Custom Vertex Lighting";

        // 自定义环境光强度
        public static string customVertexLightingIntensityText = "Custom Vertex Light Intensity";

        //自定义环境光颜色
        public static string customVertexLightColorText = "Custom Vertex Light Color";

        // 自定义环境光强度
        public static string dontClipByMirrorText = "Clip By Mirror";

        // 消融开关
        public static string useDissolveText = "Use Dissolve";

        // 消融世界高度
        public static string dissolveHightText = "Dissolve Height";

        // 消融世界高度
        public static string dissolveIntensityText = "Dissolve Intensity";

        //消融的noise图片
        public static string dissolveTextureText = "Dissolve Texture";

        // 消融边缘yanse
        public static string dissolveColorText = "Dissolve Color";

        public static string dissolveReverceText = "Dissolve Reverce";

        public static string dissolveTypeText = "Dissolve Type";

        // 消融顶点颜色增量
        public static string dissolveVertexColorIncrementText = "Dissolve Vertex Color Increment";

        // 消融顶点强度
        public static string dissolveVertexPowerText = "Dissolve Vertex Power";

        // 消融过渡距离
        public static string dissolveLerpDistanceText = "Dissolve Lerp Distance";
        public static string maskValueOffsetText = "Mask Value Offset";

        // 效果遮罩贴图
        public static GUIContent effectMaskMapText = EditorGUIUtility.TrTextContent("Mask", "R:Sparkle,G:Detail,B:Glitter&Laser,A:Emission");

        // 激光斜坡贴图
        public static GUIContent laserRampMapText = EditorGUIUtility.TrTextContent("Ramp Map", "Laser Ramp Map");

        // 闪光遮罩贴图通道
        public static GUIContent glitterMaskMapText = EditorGUIUtility.TrTextContent("Mask Channel(B)");

        // 闪光贴图
        public static GUIContent glitterMapText = EditorGUIUtility.TrTextContent("Map", "Glitter Map");

        // 发光颜色
        public static GUIContent emissionText = EditorGUIUtility.TrTextContent("Color", "Emission (RGB)");

        //发光贴图
        public static GUIContent emissionMapText = EditorGUIUtility.TrTextContent("Map", "Emission Map");

        // 细节效果模式
        public static string detailEffectMode = "Effect Mode";
        public static GUIContent dissolveType = new GUIContent("Dissolve Type",
               "Select a dissolve type");

        // 细节遮罩贴图
        public static GUIContent detailMaskText = EditorGUIUtility.TrTextContent("Detail Mask(G)", "Mask for Secondary Maps (A)");

        // 细节反照率贴图
        public static GUIContent detailAlbedoText = EditorGUIUtility.TrTextContent("Detail Albedo", "Albedo (RGB)");

        // 细节法线贴图
        public static GUIContent detailNormalMapText = EditorGUIUtility.TrTextContent("Normal Map", "Normal Map");

        // 流动贴图
        public static GUIContent flowMapText = EditorGUIUtility.TrTextContent("Map", "Flow Map");

        // UV Set 标签
        public static GUIContent uvSetLabel = EditorGUIUtility.TrTextContent("UV Set");

        public static readonly GUIContent stencilRefText = new GUIContent("Stencil Ref",
                "Sets the stencil reference value for the stencil test. The stencil test passes if (ref & mask) " +
                "(compareFunction & (stencil & readMask)) is true. This value is stored in the stencil buffer.");
        public static readonly GUIContent stencilCompText = new GUIContent("Stencil Comp",
                "Sets the stencil comparison function for the stencil test. The stencil test passes if (ref & mask) " +
                "(compareFunction & (stencil & readMask)) is true. This value is stored in the stencil buffer.");
        public static readonly GUIContent stencilPassText = new GUIContent("Stencil Pass",
                "Sets the stencil operation to perform when the stencil test passes. This value is stored in the stencil buffer.");

        // 闪光类型
        public static string sparkleType = "Sparkle Type";

        // 主要贴图
        public static string primaryMapsText = "Main Maps";

        // 次要贴图
        public static string secondaryMapsText = "Secondary Maps";

        // 着色模式名称数组
        public static readonly string[] shadingNames = Enum.GetNames(typeof(ShadingMode));

        // 遮罩通道名称数组
        public static readonly string[] maskChannelNames = Enum.GetNames(typeof(MaskChannel));

        // 效果模式名称数组
        public static readonly string[] effectNames = Enum.GetNames(typeof(EffectMode));

        // 细节效果模式名称数组
        public static readonly string[] detailEffectNames = Enum.GetNames(typeof(DetailEffectMode));

        // 闪光类型名称数组
        public static readonly string[] sparkleTypeNames = Enum.GetNames(typeof(SparkleType));

    }
}
