using System;
using UnityEditor;
using UnityEngine;

using UnityEngine.Rendering;

namespace ShaderEditorGUI
{
    internal abstract class BaseShaderGUI : ShaderGUI
    {
        #region EnumsAndClasses
        public enum SurfaceType
        {
            Opaque,
            Transparent
        }

        public enum BlendMode
        {
            Alpha,
            Additive
        }

        public enum RenderFace
        {
            Front = 2,
            Back = 1,
            Both = 0
        }

        public enum DissolveType
        {
            Vertical,
            Vertex,
        }
        // public enum CompareFunction
        // {
        //     Disabled = 0,
        //     Never = 1,
        //     Less = 2,
        //     Equal = 3,
        //     LessEqual = 4,
        //     Greater = 5,
        //     NotEqual = 6,
        //     GreaterEqual = 7,
        //     Always = 8
        // }

        // public enum StencilOp
        // {
        //     Keep = 0,
        //     Zero = 1,
        //     Replace = 2,
        //     IncrementSaturate = 3,
        //     DecrementSaturate = 4,
        //     Invert = 5,
        //     IncrementWrap = 6,
        //     DecrementWrap = 7
        // }

        public static class Styles
        {
            // Catergories
            public static readonly GUIContent SurfaceOptions =
                new GUIContent("Surface Options", "Controls how Universal RP renders the Material on a screen.");

            public static readonly GUIContent SurfaceInputs = new GUIContent("Surface Inputs",
                "These settings describe the look and feel of the surface itself.");

            public static readonly GUIContent AdvancedLabel = new GUIContent("Advanced",
                "These settings affect behind-the-scenes rendering and underlying calculations.");

            public static readonly GUIContent surfaceType = new GUIContent("Surface Type",
                "Select a surface type for your texture. Choose between Opaque or Transparent.");

            public static readonly GUIContent dissolveType = new GUIContent("Dissolve Type",
                "Select a dissolve type");

            public static readonly GUIContent blendingMode = new GUIContent("Blending Mode",
                "Controls how the color of the Transparent surface blends with the Material color in the background.");

            public static readonly GUIContent cullingText = new GUIContent("Render Face",
                "Specifies which faces to cull from your geometry. Front culls front faces. Back culls backfaces. None means that both sides are rendered.");
            
            public static readonly GUIContent alphaClipText = new GUIContent("Alpha Clipping",
                "Makes your Material act like a Cutout shader. Use this to create a transparent effect with hard edges between opaque and transparent areas.");

            public static readonly GUIContent alphaClipThresholdText = new GUIContent("Threshold",
                "Sets where the Alpha Clipping starts. The higher the value is, the brighter the  effect is when clipping starts.");
            public static readonly GUIContent stencilRefText = new GUIContent("Stencil Ref",
                "Sets the stencil reference value for the stencil test. The stencil test passes if (ref & mask) " +
                "(compareFunction & (stencil & readMask)) is true. This value is stored in the stencil buffer.");
            public static readonly GUIContent stencilCompText = new GUIContent("Stencil Comp",
                "Sets the stencil comparison function for the stencil test. The stencil test passes if (ref & mask) " +
                "(compareFunction & (stencil & readMask)) is true. This value is stored in the stencil buffer.");
            public static readonly GUIContent stencilPassText = new GUIContent("Stencil Pass",
                "Sets the stencil operation to perform when the stencil test passes. This value is stored in the stencil buffer.");

            public static readonly GUIContent receiveShadowText = new GUIContent("Receive Shadows",
                "When enabled, other GameObjects can cast shadows onto this GameObject.");

            public static readonly GUIContent baseMap = new GUIContent("Base Map",
                "Specifies the base Material and/or Color of the surface. If you’ve selected Transparent or Alpha Clipping under Surface Options, your Material uses the Texture’s alpha channel or color.");

            public static readonly GUIContent emissionMap = new GUIContent("Emission Map",
                "Sets a Texture map to use for emission. You can also select a color with the color picker. Colors are multiplied over the Texture.");

            public static readonly GUIContent normalMapText =
                new GUIContent("Normal Map", "Assigns a tangent-space normal map.");

            public static readonly GUIContent bumpScaleNotSupported =
                new GUIContent("Bump scale is not supported on mobile platforms");

            public static readonly GUIContent fixNormalNow = new GUIContent("Fix now",
                "Converts the assigned texture to be a normal map format.");

            public static readonly GUIContent queueSlider = new GUIContent("Priority",
                "Determines the chronological rendering order for a Material. High values are rendered first.");

            public static readonly GUIContent maskMap = new GUIContent("Mask Map", "");

            public static readonly GUIContent reflectionMap = new GUIContent("Reflection Map", "");

            public static readonly GUIContent reflectModeText = new GUIContent("Reflection Mode", "");

            public static readonly GUIContent reflectionTexText = new GUIContent("Internal Reflection", "");
            public static readonly GUIContent reflOffsetText = new GUIContent("Reflection Offset", "");
            public static readonly GUIContent filterTex = new GUIContent("Filter");
            public static readonly GUIContent filterStrength = new GUIContent("FilterStrength");

            public static readonly GUIContent matcapMapText = EditorGUIUtility.TrTextContent("Matcap", "Matcap Diffuse");
            public static readonly GUIContent matcapSpecMapText = EditorGUIUtility.TrTextContent("Matcap Spec", "Matcap Spec");

            // Power
            public static readonly GUIContent powerText = new GUIContent("Power", "亮度");

            // Specular
            public static readonly GUIContent specularOnText = new GUIContent("SpecularOn");
            public static readonly GUIContent specularColorText = new GUIContent("Specular Color");
            public static readonly GUIContent specularTextureText = new GUIContent("Specular Texture");
            public static readonly GUIContent specularPowerText = new GUIContent("Specular Power");


            public static GUIContent detailMaskText = EditorGUIUtility.TrTextContent("Detail Mask(G)", "Mask for Secondary Maps (A)");
            public static GUIContent detailAlbedoText = EditorGUIUtility.TrTextContent("Detail Albedo", "Albedo (RGB)");
            public static GUIContent detailNormalMapText = EditorGUIUtility.TrTextContent("Normal Map", "Normal Map");

            public static GUIContent uvSetLabel = EditorGUIUtility.TrTextContent("UV Set");

            public static readonly GUIContent perceptualRoughnessText = new GUIContent("Perceptual Roughness");            

            public static readonly GUIContent shadowText = new GUIContent("Shadow", "");
            public static readonly GUIContent shadowColorText = new GUIContent("Color", "");

            public static readonly string advancedText = "Advanced Options";
            public static string dontClipByMirrorText = "Clip By Mirror";
            public static readonly string renderingMode = "Rendering Mode";

            public static GUIContent followMaskText = new GUIContent("Follow Mask", "");
            public static GUIContent followMapText = new GUIContent("Follow Map", "");
        }
        #endregion

        #region Variables
        protected MaterialEditor materialEditor { get; set; }

        public bool m_FirstTimeApply = true;

        protected const string k_KeyPrefix = "UniversalRP:Material:UI_State:";

        private string m_HeaderStateKey = null;

        // Header foldout states

        bool m_SurfaceOptionsFoldout;

        bool m_SurfaceInputsFoldout;

        bool m_AdvancedFoldout;

        #endregion

        private const int queueOffsetRange = 50;

        #region Surface Options               
        protected MaterialProperty surfaceTypeProp { get; set; }
        protected MaterialProperty blendModeProp { get; set; }
        protected MaterialProperty cullingProp { get; set; }
        protected MaterialProperty alphaClipProp { get; set; }
        protected MaterialProperty alphaCutoffProp { get; set; }
        protected MaterialProperty zWriteModeProp { get; set; }
        protected MaterialProperty receiveShadowsProp { get; set; }
        protected MaterialProperty stencilRefProp { get; set; }
        protected MaterialProperty stencilCompProp { get; set; }
        protected MaterialProperty stencilPassProp { get; set; }
        protected virtual void FindSurfaceOptionsProperties(MaterialProperty[] properties)
        {
            surfaceTypeProp = FindProperty("_Surface", properties);
            blendModeProp = FindProperty("_Blend", properties);
            cullingProp = FindProperty("_Cull", properties, false);
            alphaClipProp = FindProperty("_AlphaClip", properties, false);
            alphaCutoffProp = FindProperty("_Cutoff", properties, false);
            zWriteModeProp = FindProperty("_ZWrite", properties, false);
            receiveShadowsProp = FindProperty("_ReceiveShadows", properties, false);
            stencilRefProp = FindProperty("_Stencil", properties, false);
            stencilCompProp = FindProperty("_StencilComp", properties, false);
            stencilPassProp = FindProperty("_StencilOp", properties, false);
        }

        protected virtual void DrawSurfaceOptions(Material material)
        {
            ShaderGUIHelper.DoPopup(Styles.surfaceType, surfaceTypeProp, Enum.GetNames(typeof(SurfaceType)), materialEditor);
            if ((SurfaceType)material.GetFloat("_Surface") == SurfaceType.Transparent)
            {
                ShaderGUIHelper.DoPopup(Styles.blendingMode, blendModeProp, Enum.GetNames(typeof(BlendMode)), materialEditor);

                if (zWriteModeProp != null)
                {
                    materialEditor.ShaderProperty(zWriteModeProp, "ZWrite OnOff");
                }
            }


            if (cullingProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = cullingProp.hasMixedValue;
                var culling = (RenderFace)cullingProp.floatValue;
                culling = (RenderFace)EditorGUILayout.EnumPopup(Styles.cullingText, culling);
                if (EditorGUI.EndChangeCheck())
                {
                    materialEditor.RegisterPropertyChangeUndo(Styles.cullingText.text);
                    cullingProp.floatValue = (float)culling;
                    material.doubleSidedGI = (RenderFace)cullingProp.floatValue != RenderFace.Front;
                }

                EditorGUI.showMixedValue = false;
            }

            if (alphaClipProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = alphaClipProp.hasMixedValue;
                var alphaClipEnabled = EditorGUILayout.Toggle(Styles.alphaClipText, alphaClipProp.floatValue == 1);
                if (EditorGUI.EndChangeCheck())
                    alphaClipProp.floatValue = alphaClipEnabled ? 1 : 0;
                EditorGUI.showMixedValue = false;

                if (alphaClipProp.floatValue == 1)
                    materialEditor.ShaderProperty(alphaCutoffProp, Styles.alphaClipThresholdText, 1);
            }

            if (receiveShadowsProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = receiveShadowsProp.hasMixedValue;
                var receiveShadows =
                    EditorGUILayout.Toggle(Styles.receiveShadowText, receiveShadowsProp.floatValue == 1.0f);
                if (EditorGUI.EndChangeCheck())
                    receiveShadowsProp.floatValue = receiveShadows ? 1.0f : 0.0f;
                EditorGUI.showMixedValue = false;
            }

            if (stencilRefProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = stencilRefProp.hasMixedValue;
                var stencilRef = EditorGUILayout.IntField(Styles.stencilRefText, (int)stencilRefProp.floatValue);
                if (EditorGUI.EndChangeCheck())
                    stencilRefProp.floatValue = (float)stencilRef;
                EditorGUI.showMixedValue = false;
            }
            if (stencilCompProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = stencilCompProp.hasMixedValue;
                var comp = (CompareFunction)stencilCompProp.floatValue;
                comp = (CompareFunction)EditorGUILayout.EnumPopup(Styles.stencilCompText, comp);
                if (EditorGUI.EndChangeCheck())
                    stencilCompProp.floatValue = (float)comp;
                EditorGUI.showMixedValue = false;
            }
            if (stencilPassProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = stencilPassProp.hasMixedValue;
                var pass = (StencilOp)stencilPassProp.floatValue;
                pass = (StencilOp)EditorGUILayout.EnumPopup(Styles.stencilPassText, pass);
                if (EditorGUI.EndChangeCheck())
                    stencilPassProp.floatValue = (float)pass;
                EditorGUI.showMixedValue = false;
            }
        }
        #endregion

        #region Surface Inputs
        protected virtual void FindSurfaceInputs(MaterialProperty[] properties)
        {
            FindBaseProperties(properties);
        }

        protected virtual void DrawSurfaceInputs(Material material)
        {
            DrawBaseProperties(material);
        }
        #endregion

        #region Base Properties
        protected MaterialProperty baseMapProp { get; set; }
        protected MaterialProperty baseColorProp { get; set; }

        protected void FindBaseProperties(MaterialProperty[] properties)
        {
            baseMapProp = FindProperty("_BaseMap", properties, false);
            baseColorProp = FindProperty("_BaseColor", properties, false);
        }

        protected void DrawBaseProperties(Material material)
        {
            if (baseMapProp != null && baseColorProp != null) // Draw the baseMap, most shader will have at least a baseMap
            {
                materialEditor.TexturePropertySingleLine(Styles.baseMap, baseMapProp, baseColorProp);
                // TODO Temporary fix for lightmapping, to be replaced with attribute tag.
                if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", baseMapProp.textureValue);
                    var baseMapTiling = baseMapProp.textureScaleAndOffset;
                    material.SetTextureScale("_MainTex", new Vector2(baseMapTiling.x, baseMapTiling.y));
                    material.SetTextureOffset("_MainTex", new Vector2(baseMapTiling.z, baseMapTiling.w));
                }
            }
        }
        #endregion

        #region Normal Properties
        MaterialProperty bumpMap = null;
        MaterialProperty bumpMapScale = null;
        protected void FindNormalProperties(MaterialProperty[] properties, bool supportedScale = true)
        {
            bumpMap = FindProperty("_BumpMap", properties);
            if (supportedScale)
            {
                bumpMapScale = FindProperty("_BumpScale", properties, false);
            }
        }

        protected void DrawNormalProperties(bool supportedScale = true)
        {
            materialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMap);

            if (supportedScale)
            {
                materialEditor.FloatProperty(bumpMapScale, "Normal Scale");
                //if (bumpMapScale.floatValue != 1 && UnityEditorInternal.InternalEditorUtility.IsMobilePlatform(EditorUserBuildSettings.activeBuildTarget))
                //    if (materialEditor.HelpBoxWithButton(
                //        EditorGUIUtility.TrTextContent("Bump scale is not supported on mobile platforms"),
                //        EditorGUIUtility.TrTextContent("Fix Now")))
                //    {
                //        bumpMapScale.floatValue = 1;
                //    }
            }
        }
        #endregion

        #region Reflection Properties
        MaterialProperty reflMapProp = null;
        MaterialProperty reflAmountPorp = null;

        protected void FindReflectionProperties(MaterialProperty[] properties)
        {
            reflMapProp = FindProperty("_ReflMap", properties);
            reflAmountPorp = FindProperty("_ReflAmount", properties);
        }

        protected void DrawReflectionProperties()
        {
            GUILayout.Label("Reflection", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (reflMapProp != null)
            {
                materialEditor.TexturePropertySingleLine(Styles.reflectionMap, reflMapProp, reflAmountPorp);
            }
        }
        #endregion

        #region Matcap Diffuse
        MaterialProperty matcapMapProp = null;
        MaterialProperty matcapScaleProp = null;
        protected void FindMatcapProperties(MaterialProperty[] properties)
        {
            matcapMapProp = FindProperty("_MatcapMap", properties);
            matcapScaleProp = FindProperty("_MatcapScale", properties);
        }
        protected void DrawMatcapProperties()
        {
            materialEditor.TexturePropertySingleLine(Styles.matcapMapText, matcapMapProp, matcapScaleProp);
        }
        #endregion

        #region Matcap Specular
        MaterialProperty matcapSpecMapProp = null;
        MaterialProperty matcapSpecScaleProp = null;
        protected void FindMatcapSpecProperties(MaterialProperty[] properties)
        {
            matcapSpecMapProp = FindProperty("_MatcapSpecMap", properties);
            matcapSpecScaleProp = FindProperty("_MatcapSpecScale", properties);            
        }
        protected void DrawMatcapSpecProperties()
        {
            materialEditor.TexturePropertySingleLine(Styles.matcapSpecMapText, matcapSpecMapProp, matcapSpecScaleProp);
        }
        #endregion

        #region Fresnel Properties

        MaterialProperty fresnelBiasProp = null;
        MaterialProperty fresnelPowerPorp = null;

        protected void FindFresnelProperties(MaterialProperty[] properties)
        {
            fresnelBiasProp = FindProperty("_FresnelBias", properties);
            fresnelPowerPorp = FindProperty("_FresnelPower", properties);
        }

        protected void DrawFresnelProperties()
        {
            GUILayout.Label("Fresnel", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            materialEditor.RangeProperty(fresnelBiasProp, " Bias");
            materialEditor.RangeProperty(fresnelPowerPorp, " Power");
        }
        #endregion

        #region Emission Properties
        protected MaterialProperty emissionMapProp { get; set; }
        protected MaterialProperty emissionColorProp { get; set; }

        protected void FindEmissionProperties(MaterialProperty[] properties)
        {
            emissionMapProp = FindProperty("_EmissionMap", properties, false);
            emissionColorProp = FindProperty("_EmissionColor", properties, false);
        }

        protected void DrawEmissionProperties(Material material, bool keyword)
        {
            var emissive = true;
            var hadEmissionTexture = emissionMapProp.textureValue != null;

            if (!keyword)
            {
                materialEditor.TexturePropertyWithHDRColor(Styles.emissionMap, emissionMapProp, emissionColorProp,
                    false);
            }
            else
            {
                // Emission for GI?
                emissive = materialEditor.EmissionEnabledProperty();

                EditorGUI.BeginDisabledGroup(!emissive);
                {
                    // Texture and HDR color controls
                    materialEditor.TexturePropertyWithHDRColor(Styles.emissionMap, emissionMapProp,
                        emissionColorProp,
                        false);
                }
                EditorGUI.EndDisabledGroup();
            }

            // If texture was assigned and color was black set color to white
            var brightness = emissionColorProp.colorValue.maxColorComponent;
            if (emissionMapProp.textureValue != null && !hadEmissionTexture && brightness <= 0f)
                emissionColorProp.colorValue = Color.white;

            // UniversalRP does not support RealtimeEmissive. We set it to bake emissive and handle the emissive is black right.
            if (emissive)
            {
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                if (brightness <= 0f)
                    material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }
        #endregion

        #region  Follow  Properties
        protected MaterialProperty followMaskProp { get; set; }
        protected MaterialProperty followMapProp { get; set; }
        protected MaterialProperty followColor1 { get; set; }
        protected MaterialProperty followColor2 { get; set; }
        protected MaterialProperty followColor3 { get; set; }
        protected MaterialProperty followStrong { get; set; }
        protected MaterialProperty scrollXProp { get; set; }
        protected MaterialProperty scrollYProp { get; set; }

        protected void FindFollowProperties(MaterialProperty[] properties)
        {
            followMaskProp = FindProperty("_FollowMask", properties, false);
            followMapProp = FindProperty("_FollowMap", properties, false);
            followColor1 = FindProperty("_FollowColor1", properties, false);
            followColor2 = FindProperty("_FollowColor2", properties, false);
            followColor3 = FindProperty("_FollowColor3", properties, false);
            followStrong = FindProperty("_FollowStrong", properties, false);
            scrollXProp = FindProperty("_ScrollX", properties, false);
            scrollYProp = FindProperty("_ScrollY", properties, false);
        }

        protected void DrawFollowProperties(Material material)
        {
            if (material.HasProperty("_FollowMask") && material.HasProperty("_FollowMap"))
            {
                ShaderGUIHelper.SplitLine();
                GUILayout.Label("Follow", EditorStyles.boldLabel);
                EditorGUILayout.Space();
            }else
            {
                return;
            }
            materialEditor.TexturePropertySingleLine(Styles.followMaskText, followMaskProp);
            materialEditor.TexturePropertySingleLine(Styles.followMapText, followMapProp);
            materialEditor.ColorProperty(followColor1, "LastColor");
            materialEditor.ColorProperty(followColor2, "NowColor");
            materialEditor.ColorProperty(followColor3, "NextColor");
            materialEditor.RangeProperty(followStrong, "Strong");

            materialEditor.RangeProperty(scrollXProp, "Scroll X");
            materialEditor.RangeProperty(scrollYProp, "Scroll Y");
            materialEditor.TextureScaleOffsetProperty(followMapProp);
        }

        #endregion

         #region Dissolve Properties

        protected MaterialProperty dissolveHeightProp { get; set; }
        protected MaterialProperty dissolveTextureProp { get; set; }
        protected MaterialProperty dissolveColorProp { get; set; }
        protected MaterialProperty dissolveLerpDistanceProp { get; set; }
        protected MaterialProperty useDissolveProp { get; set; }
        protected MaterialProperty dissolveReverceProp { get; set; }
        protected MaterialProperty dissolveTypeProp {get; set;}
        protected MaterialProperty dissolveVertexColorIncrementProp { get; set; }
        protected MaterialProperty dissolveVertexPowerProp { get; set; }

        public static string useDissolveText = "Use Dissolve";

        public static string dissolveReverceText = "Dissolve Reverce";

        public static string dissolveTypeText = "Dissolve Type";

        // 消融世界高度
        public static string dissolveHeightText = "Dissolve Height";

        //消融的noise图片
        public static string dissolveTextureText = "Dissolve Texture";

        // 消融边缘yanse
        public static string dissolveColorText = "Dissolve Color";

        // 消融过渡距离
        public static string dissolveLerpDistanceText = "Dissolve Lerp Distance";

        // 消融顶点颜色增量
        public static string dissolveVertexColorIncrementText = "Dissolve Vertex Color Increment";

        // 消融顶点强度
        public static string dissolveVertexPowerText = "Dissolve Vertex Power";

        protected void FindDissolveProperties(MaterialProperty[] properties)
        {
            dissolveHeightProp = FindProperty("_DissolveHeight", properties, false);
            dissolveTextureProp = FindProperty("_DissolveTexture", properties, false);
            dissolveColorProp = FindProperty("_DissolveColor", properties, false);
            dissolveLerpDistanceProp = FindProperty("_DissolveLerpDistance", properties, false);
            useDissolveProp = FindProperty("_UseDissolve", properties, false);
            dissolveReverceProp = FindProperty("_DissolveReverce", properties, false);
            dissolveTypeProp = FindProperty("_DissolveType", properties, false);
            dissolveVertexColorIncrementProp = FindProperty("_DissolveVertexColorIncrement", properties, false);
            dissolveVertexPowerProp = FindProperty("_DissolveVertexPower", properties, false);
        }

        protected void DrawDissolveProperties(Material material)
        {
            //materialEditor.
            if (material.HasFloat("_UseDissolve"))
            {
                float useDissolve = material.GetFloat("_UseDissolve");
                bool showGUI = useDissolve > 0.5 ? true : false;
                showGUI = EditorGUILayout.Toggle(useDissolveText, showGUI);
                float dissolveReverce = material.GetFloat("_DissolveReverce");
                if (showGUI)
                {
                    // for _UseDissolve
                    material.EnableKeyword("_USEDISSOLVE_ON");
                    material.SetFloat("_UseDissolve", 1);
                    // for _DissolveReverce
                    bool isReverce = dissolveReverce > 0.5 ? true : false;
                    isReverce = EditorGUILayout.Toggle(dissolveReverceText, isReverce);
                    material.SetFloat("_DissolveReverce", isReverce ? 1 : 0);
                    if (isReverce)
                    {
                        material.EnableKeyword("_DISSOLVEREVERCE_ON");
                    }
                    else
                    {
                        material.DisableKeyword("_DISSOLVEREVERCE_ON");
                    }
                    // for _DissolveType
                    ShaderGUIHelper.DoPopup(Styles.dissolveType, dissolveTypeProp, Enum.GetNames(typeof(DissolveType)), materialEditor);
                    float dissolveTypeValue = material.GetFloat("_DissolveType");
                    if (dissolveTypeValue == 0)
                    {
                        material.EnableKeyword("_DISSOLVETYPE_VERTICAL");
                        material.DisableKeyword("_DISSOLVETYPE_VERTEX");
                    }
                    else if(dissolveTypeValue == 1)
                    {
                        material.EnableKeyword("_DISSOLVETYPE_VERTEX");
                        material.DisableKeyword("_DISSOLVETYPE_VERTICAL");
                    }
                    // for _DissolveHeight
                    materialEditor.FloatProperty(dissolveHeightProp, dissolveHeightText);
                    // for _DissolveColor
                    materialEditor.ColorProperty(dissolveColorProp, dissolveColorText);
                    // for _DissolveTexture
                    materialEditor.TextureProperty(dissolveTextureProp, dissolveTextureText);
                    // for _DissolveLerpDistance
                    materialEditor.FloatProperty(dissolveLerpDistanceProp, dissolveLerpDistanceText);
                    // for _DissolveVertexColorIncrement
                    materialEditor.FloatProperty(dissolveVertexColorIncrementProp, dissolveVertexColorIncrementText);
                    // for _DissolveVertexPower
                    materialEditor.FloatProperty(dissolveVertexPowerProp, dissolveVertexPowerText);
                }
                else
                {
                    material.DisableKeyword("_USEDISSOLVE_ON");
                    material.SetFloat("_UseDissolve", 0);
                }
            }
        }

        #endregion

        #region Detail
        MaterialProperty detailAlbedoMap = null;
        MaterialProperty detailNormalMapScale = null;
        MaterialProperty detailNormalMap = null;
        MaterialProperty uvSetSecondary = null;
        protected virtual void FindDetailProperties(MaterialProperty[] props)
        {
            detailAlbedoMap = FindProperty("_DetailBaseMap", props);
            detailNormalMapScale = FindProperty("_DetailNormalMapScale", props);
            detailNormalMap = FindProperty("_DetailNormalMap", props);
            if (HasProperty(props, "_UVSec"))
                uvSetSecondary = FindProperty("_UVSec", props);
        }

        protected virtual void DrawDetailProperties()
        {
            //m_MaterialEditor.ShaderProperty(detailMask, BaseStyles.detailMaskText);
            EditorGUILayout.Space();
            materialEditor.TexturePropertySingleLine(Styles.detailAlbedoText, detailAlbedoMap);
            materialEditor.TexturePropertySingleLine(Styles.detailNormalMapText, detailNormalMap, detailNormalMap.textureValue != null ? detailNormalMapScale : null);

            // 
            materialEditor.TextureScaleOffsetProperty(detailAlbedoMap);
            if(uvSetSecondary != null)
                materialEditor.ShaderProperty(uvSetSecondary, Styles.uvSetLabel.text);
        }
        #endregion

        #region Power Property
        MaterialProperty powerProp = null;
        protected void FindPowerProperties(MaterialProperty[] properties)
        {
            powerProp = FindProperty("_Power", properties);
        }

        protected void DrawPowerProperties()
        {
            materialEditor.ShaderProperty(powerProp, Styles.powerText);
        }
        #endregion

        #region Advanced Options
        protected MaterialProperty queueOffsetProp { get; set; }
        protected MaterialProperty clipByMirrorProp { get; set; }
        protected virtual void FindAdvancedOptionsProperties(MaterialProperty[] properties)
        {
            queueOffsetProp = FindProperty("_QueueOffset", properties, false);
            clipByMirrorProp = FindProperty("_ClipByMirror", properties, false);
        }

        protected virtual void DrawAdvancedOptions(Material material)
        {
            materialEditor.EnableInstancingField();

            if (material.HasFloat("_ClipByMirror"))
            {
                float clipByMirrorSetting = material.GetFloat("_ClipByMirror");
                bool showGUI = clipByMirrorSetting > 0.5 ? true : false;
                showGUI = EditorGUILayout.Toggle(Styles.dontClipByMirrorText, showGUI);
                if (showGUI)
                {
                    material.SetFloat("_ClipByMirror", 1);
                }
                else
                {
                    material.SetFloat("_ClipByMirror", 0);
                }
            }

            if (queueOffsetProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = queueOffsetProp.hasMixedValue;
                var queue = EditorGUILayout.IntSlider(Styles.queueSlider, (int)queueOffsetProp.floatValue, -queueOffsetRange, queueOffsetRange);
                if (EditorGUI.EndChangeCheck())
                    queueOffsetProp.floatValue = queue;
                EditorGUI.showMixedValue = false;
            }
            else
            {
                materialEditor.RenderQueueField();
            }
        }
        #endregion
        
        #region Virtual Func
        public sealed override void OnGUI(MaterialEditor materialEditorIn, MaterialProperty[] properties)
        {
            if (materialEditorIn == null)
                throw new ArgumentNullException("materialEditorIn");

            FindProperties(properties); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
            materialEditor = materialEditorIn;
            Material material = materialEditor.target as Material;

            // Make sure that needed setup (ie keywords/renderqueue) are set up if we're switching some existing
            // material to a universal shader.
            if (m_FirstTimeApply)
            {
                OnOpenGUI(material, materialEditorIn);
                m_FirstTimeApply = false;
            }

            DrawProperties(material);
        }

        public virtual void OnOpenGUI(Material material, MaterialEditor materialEditor)
        {
            // Foldout states
            m_HeaderStateKey = k_KeyPrefix + material.shader.name; // Create key string for editor prefs
            // m_SurfaceOptionsFoldout = new SavedBool($"{m_HeaderStateKey}.SurfaceOptionsFoldout", true);
            // m_SurfaceInputsFoldout = new SavedBool($"{m_HeaderStateKey}.SurfaceInputsFoldout", true);
            // m_AdvancedFoldout = new SavedBool($"{m_HeaderStateKey}.AdvancedFoldout", false);

            foreach (var obj in materialEditor.targets)
                MaterialChanged((Material)obj);
        }

        public virtual void ConvertOtherShader(Material material, Shader oldShader, Shader newShader)
        {
            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }
            material.SetFloat("_Surface", (float)surfaceType);
            material.SetFloat("_Blend", (float)blendMode);

            MaterialChanged(material);
        }

        // 材质改变，Keyword 设置
        protected abstract void MaterialChanged(Material material);        

        public static void ResetBaseMaterialKeywords(Material material)
        {
            ClearMaterialKeywords(material);

            SetupMaterialSurfaceOptions(material);
        }
        #endregion

        // 属性
        public virtual void FindProperties(MaterialProperty[] properties)
        {
            FindSurfaceOptionsProperties(properties);

            FindSurfaceInputs(properties);

            FindAdvancedOptionsProperties(properties);
        }

        void DrawProperties(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            EditorGUI.BeginChangeCheck();

            m_SurfaceOptionsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_SurfaceOptionsFoldout, Styles.SurfaceOptions);
            if (m_SurfaceOptionsFoldout)
            {
                DrawSurfaceOptions(material);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            m_SurfaceInputsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_SurfaceInputsFoldout, Styles.SurfaceInputs);
            if (m_SurfaceInputsFoldout)
            {
                DrawSurfaceInputs(material);
                EditorGUILayout.Space();
            }            
            EditorGUILayout.EndFoldoutHeaderGroup();



            m_AdvancedFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_AdvancedFoldout, Styles.AdvancedLabel);
            if (m_AdvancedFoldout)
            {
                DrawAdvancedOptions(material);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            DrawAdditionalFoldouts(material);

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in materialEditor.targets)
                    MaterialChanged((Material)obj);
            }
        }

        ////////////////////////////////////
        // Drawing Functions              //
        ////////////////////////////////////
        #region DrawingFunctions

        public virtual void DrawAdditionalFoldouts(Material material) { }

        protected static void DrawTileOffset(MaterialEditor materialEditor, MaterialProperty textureProp)
        {
            materialEditor.TextureScaleOffsetProperty(textureProp);
        }
        #endregion

        ////////////////////////////////////
        // Material Data Functions        //
        ////////////////////////////////////
        #region MaterialDataFunctions
        public static void ClearMaterialKeywords(Material material)
        {
            // Clear all keywords for fresh start
            material.shaderKeywords = null;
        }

        public static void SetupMaterialSurfaceOptions(Material material)
        {
            // Setup blending - consistent across all Universal RP shaders
            SetupMaterialBlendMode(material);
            // Receive Shadows
            if (material.HasProperty("_ReceiveShadows"))
                CoreUtils.SetKeyword(material, "_RECEIVE_SHADOWS_OFF", material.GetFloat("_ReceiveShadows") == 0.0f);
        }

        public static void SetupMaterialBlendMode(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            bool alphaClip = false;
            if (material.HasProperty("_AlphaClip"))
                alphaClip = material.GetFloat("_AlphaClip") >= 0.5;

            if (alphaClip)
            {
                material.EnableKeyword("_ALPHATEST_ON");
            }
            else
            {
                material.DisableKeyword("_ALPHATEST_ON");
            }

            if (material.HasProperty("_Surface"))
            {
                int renderQueue = material.renderQueue;

                SurfaceType surfaceType = (SurfaceType)material.GetFloat("_Surface");
                if (surfaceType == SurfaceType.Opaque)
                {
                    if (alphaClip)
                    {
                        if (renderQueue < (int)RenderQueue.AlphaTest)
                        {
                            renderQueue = (int)RenderQueue.AlphaTest;
                        }
                        else if (renderQueue >= (int)RenderQueue.Transparent)
                        {
                            renderQueue = (int)RenderQueue.AlphaTest;
                        }
                        //material.renderQueue = (int)RenderQueue.AlphaTest;
                        material.SetOverrideTag("RenderType", "TransparentCutout");
                    }
                    else
                    {
                        if (renderQueue >= (int)RenderQueue.AlphaTest)
                        {
                            renderQueue = (int)RenderQueue.Geometry;
                        }
                        //material.renderQueue = (int)RenderQueue.Geometry;
                        material.SetOverrideTag("RenderType", "Opaque");
                    }
                    material.renderQueue = renderQueue;
                    //material.renderQueue += material.HasProperty("_QueueOffset") ? (int)material.GetFloat("_QueueOffset") : 0;
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.SetShaderPassEnabled("ShadowCaster", true);
                }
                else
                {
                    BlendMode blendMode = (BlendMode)material.GetFloat("_Blend");

                    // Specific Transparent Mode Settings
                    switch (blendMode)
                    {
                        case BlendMode.Alpha:
                            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            break;
                        case BlendMode.Additive:
                            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            break;
                    }

                    // General Transparent Material Settings
                    material.SetOverrideTag("RenderType", "Transparent");
                    //material.SetInt("_ZWrite", 0);
                    if (renderQueue < (int)RenderQueue.Transparent)
                    {
                        renderQueue = (int)RenderQueue.Transparent;
                    }
                    material.renderQueue = renderQueue;
                    //material.renderQueue = (int)RenderQueue.Transparent;
                    //material.renderQueue += material.HasProperty("_QueueOffset") ? (int)material.GetFloat("_QueueOffset") : 0;
                    material.SetShaderPassEnabled("ShadowCaster", false);
                }                
            }
        }

        public static void SetupMaterialNormalMap(Material material)
        {
            if (material.HasProperty("_BumpMap"))
                CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));
        }

        public static void SetupMaterialReflectMap(Material material)
        {
            if (material.HasProperty("_ReflMap"))
                CoreUtils.SetKeyword(material, "_REFLECTMAP", material.GetTexture("_ReflMap"));
        }

        public static void SetupMaterialMatcapMap(Material material)
        {
            if (material.HasProperty("_MatcapMap"))
                CoreUtils.SetKeyword(material, "_MATCAPMAP", material.GetTexture("_MatcapMap"));
        }

        public static void SetupMaterialMatcapSpecMap(Material material)
        {
            if (material.HasProperty("_MatcapSpecMap"))
                CoreUtils.SetKeyword(material, "_MATCAPSPECMAP", material.GetTexture("_MatcapSpecMap"));
        }

        public static void SetupMaterialEmission(Material material)
        {
            // Emission
            if (material.HasProperty("_EmissionColor"))
                MaterialEditor.FixupEmissiveFlag(material);
            bool shouldEmissionBeEnabled =
                (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            if (material.HasProperty("_EmissionEnabled") && !shouldEmissionBeEnabled)
                shouldEmissionBeEnabled = material.GetFloat("_EmissionEnabled") >= 0.5f;
            CoreUtils.SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);
        }

        public static bool HasProperty(MaterialProperty[] props, string name)
        {
            foreach (var prop in props)
            {
                if (prop.name == name)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
