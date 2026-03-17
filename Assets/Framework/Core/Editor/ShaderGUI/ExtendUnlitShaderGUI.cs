using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine.Rendering;

namespace ShaderEditorGUI
{
    public static partial class Styles
    {
        public static readonly GUIContent SampleCount = new GUIContent("Sample Count", "Sample Count");
        public static readonly GUIContent Power = new GUIContent("Power", "Power");
    }

    internal class ExtendUnlitShaderGUI : UnityEditor.BaseShaderGUI
    {
        // properties
        protected MaterialProperty sampleCountProp { get; set; }
        protected MaterialProperty powerProp { get; set; }
        protected MaterialProperty stencilRefProp { get; set; }
        protected MaterialProperty stencilCompProp { get; set; }
        protected MaterialProperty stencilPassProp { get; set; }
        protected MaterialProperty hairMapProp { get; set; }
        protected MaterialProperty hairColorProp { get; set; }
        protected MaterialProperty husksMapProp { get; set; }
        protected MaterialProperty husksColorProp { get; set; }
        protected MaterialProperty bakeOriVertsMapProp { get; set; }
        protected MaterialProperty husksStartProp { get; set; }
        protected MaterialProperty husksEndProp { get; set; }
        protected MaterialProperty maxDeformationProp { get; set; }
        protected MaterialProperty maskMapProp { get; set; }
        protected MaterialProperty renderHairProp { get; set; }
        private MaterialProperty mainColorBlendModeProp;
        private MaterialProperty maskBlendModeProp;
        protected MaterialProperty maskBlendMapProp { get; set; }
        protected MaterialProperty maskBlendColorProp { get; set; }
        protected MaterialProperty enableNdotVEffectProp { get; set; }

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            sampleCountProp = FindProperty("_SampleCount", properties);
            powerProp = FindProperty("_Power", properties);

            hairMapProp = FindProperty("_HairMap", properties, false);
            hairColorProp = FindProperty("_HairColor", properties, false);
            husksMapProp = FindProperty("_HusksMap", properties, false);
            husksColorProp = FindProperty("_HusksColor", properties, false);
            bakeOriVertsMapProp = FindProperty("_OriginBakeMap", properties, false);
            husksStartProp = FindProperty("_HusksStart", properties, false);
            husksEndProp = FindProperty("_HusksEnd", properties, false);
            maxDeformationProp = FindProperty("_MaxDeformation", properties, false);
            maskMapProp = FindProperty("_MaskMap", properties, false);
            renderHairProp = FindProperty("_RenderHair", properties, false);

            stencilRefProp = FindProperty("_Stencil", properties, false);
            stencilCompProp = FindProperty("_StencilComp", properties, false);
            stencilPassProp = FindProperty("_StencilOp", properties, false);
            mainColorBlendModeProp = FindProperty("_MainColorBlendMode", properties);
            maskBlendModeProp = FindProperty("_MaskBlendMode", properties);
            maskBlendMapProp = FindProperty("_MaskBlendMap", properties);
            maskBlendColorProp = FindProperty("_MaskBlendColor", properties, false);
            enableNdotVEffectProp = FindProperty("_EnableNdotVEffect", properties, false);
        }

        bool renderHairFoldout;
        private const string k_KeyPrefix = "ExtendUnlitShader:Material:UI_State:";
        private string m_HeaderStateKey = null;

        // material changed check
        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material);
            if (renderHairProp != null)
            {
                bool enableRenderHair = renderHairProp.floatValue == 1;
                if (enableRenderHair)
                    material.EnableKeyword("_RENDER_HAIR");
                else
                    material.DisableKeyword("_RENDER_HAIR");
            }
            if (maskMapProp != null)
            {
                if (maskMapProp.textureValue != null)
                    material.EnableKeyword("_MASKMAP");
                else
                    material.DisableKeyword("_MASKMAP");
            }
        }

        public override void OnOpenGUI(Material material, MaterialEditor materialEditor)
        {
            base.OnOpenGUI(material, materialEditor);
            m_HeaderStateKey = k_KeyPrefix + material.shader.name; // Create key string for editor prefs
            // renderHairFoldout = new SavedBool($"{m_HeaderStateKey}.RenderHairFoldout", true);
            foreach (var obj in materialEditor.targets)
                ValidateMaterial(material);
        }

        public void DrawStencilProperties(Material material)
        {
            if (stencilRefProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = stencilRefProp.hasMixedValue;
                var stencilRef = EditorGUILayout.IntField(ShaderEditorGUI.Styles.stencilRefText, (int)stencilRefProp.floatValue);
                if (EditorGUI.EndChangeCheck())
                    stencilRefProp.floatValue = (float)stencilRef;
                EditorGUI.showMixedValue = false;
            }
            if (stencilCompProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = stencilCompProp.hasMixedValue;
                var comp = (CompareFunction)stencilCompProp.floatValue;
                comp = (CompareFunction)EditorGUILayout.EnumPopup(ShaderEditorGUI.Styles.stencilCompText, comp);
                if (EditorGUI.EndChangeCheck())
                    stencilCompProp.floatValue = (float)comp;
                EditorGUI.showMixedValue = false;
            }
            if (stencilPassProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = stencilPassProp.hasMixedValue;
                var pass = (StencilOp)stencilPassProp.floatValue;
                pass = (StencilOp)EditorGUILayout.EnumPopup(ShaderEditorGUI.Styles.stencilPassText, pass);
                if (EditorGUI.EndChangeCheck())
                    stencilPassProp.floatValue = (float)pass;
                EditorGUI.showMixedValue = false;
            }
        }

        // material main surface options
        public override void DrawSurfaceOptions(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;
            base.DrawSurfaceOptions(material);
            ShaderGUIHelper.SplitLine();
            DrawStencilProperties(material);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            BaseColorBlendMode mainColorBlendMode = (BaseColorBlendMode)mainColorBlendModeProp.floatValue;
            mainColorBlendMode = (BaseColorBlendMode)EditorGUILayout.EnumPopup("Main Color Blend Mode", mainColorBlendMode);
            mainColorBlendModeProp.floatValue = (float)mainColorBlendMode;

            BaseMaskBlendMode maskBlendMode = (BaseMaskBlendMode)maskBlendModeProp.floatValue;
            maskBlendMode = (BaseMaskBlendMode)EditorGUILayout.EnumPopup("Mask Blend Mode", maskBlendMode);
            maskBlendModeProp.floatValue = (float)maskBlendMode;
            if (maskBlendMode != BaseMaskBlendMode.Normal)
            {
                DrawMaskBlendMap(material);
            }

            materialEditor.RangeProperty(sampleCountProp, ShaderEditorGUI.Styles.SampleCount.text);
            materialEditor.RangeProperty(powerProp, ShaderEditorGUI.Styles.Power.text);
            DrawTileOffset(materialEditor, baseMapProp);
            ShaderGUIHelper.SplitLine();
            DrawRenderHairPanel(material);
        }

        public void DrawMaskBlendMap(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");
            if (maskBlendMapProp != null && maskBlendColorProp != null)
            {
                materialEditor.TexturePropertySingleLine(ShaderEditorGUI.Styles.MaskBlendMap, maskBlendMapProp, maskBlendColorProp);
            }
        }

        public void DrawRenderHairPanel(Material material)
        {
            if (renderHairProp != null)
            {
                renderHairFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(renderHairFoldout, ShaderEditorGUI.Styles.renderHairMode);
                if (renderHairFoldout)
                {
                    var oldEnableRenderHair = renderHairProp.floatValue == 1;
                    var enableRenderHair = EditorGUILayout.Toggle("Enable Render Hair", renderHairProp.floatValue == 1);
                    if (enableRenderHair)
                    {
                        ShaderGUIHelper.SplitLine();
                        DrawMaskMap(material);
                        ShaderGUIHelper.SplitLine();
                        DrawHairMap(material);
                        ShaderGUIHelper.SplitLine();
                        DrawHusksMap(material);
                        ShaderGUIHelper.SplitLine();
                        DrawBakeOriVertsMap(material);
                    }
                    if (oldEnableRenderHair != enableRenderHair)
                    {
                        renderHairProp.floatValue = enableRenderHair ? 1 : 0;
                        if (enableRenderHair)
                            material.EnableKeyword("_RENDER_HAIR");
                        else
                            material.DisableKeyword("_RENDER_HAIR");
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        public void DrawMaskMap(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");
            if (maskMapProp != null)
            {
                var oldMaskMap = maskMapProp.textureValue;
                materialEditor.TexturePropertySingleLine(ShaderEditorGUI.Styles.maskMap, maskMapProp);
                if (oldMaskMap != maskMapProp.textureValue)
                {
                    if (maskMapProp.textureValue != null)
                        material.EnableKeyword("_MASKMAP");
                    else
                        material.DisableKeyword("_MASKMAP");
                }
            }
        }

        public void DrawHairMap(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");
            if (hairMapProp != null && hairColorProp != null)
            {
                materialEditor.TexturePropertySingleLine(ShaderEditorGUI.Styles.hairMap, hairMapProp, hairColorProp);
                materialEditor.TextureScaleOffsetProperty(hairMapProp);
            }
        }

        public void DrawHusksMap(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");
            if (husksMapProp != null && husksColorProp != null)
            {
                materialEditor.TexturePropertySingleLine(ShaderEditorGUI.Styles.husksMap, husksMapProp, husksColorProp);
                materialEditor.TextureScaleOffsetProperty(husksMapProp);
            }
        }

        public void DrawBakeOriVertsMap(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");
            if (bakeOriVertsMapProp != null)
            {
                materialEditor.TexturePropertySingleLine(ShaderEditorGUI.Styles.bakeOriVertsMap, bakeOriVertsMapProp);
            }
            if (husksStartProp != null)
            {
                materialEditor.FloatProperty(husksStartProp, "Husks Start");
            }
            if (husksEndProp != null)
            {
                materialEditor.FloatProperty(husksEndProp, "Husks End");
            }
            if (maxDeformationProp != null)
            {
                materialEditor.FloatProperty(maxDeformationProp, "Max Deformation");
            }
        }

        public override void DrawAdvancedOptions(Material material)
        {
            base.DrawAdvancedOptions(material);
            if (enableNdotVEffectProp != null)
            {
                float oldEnableNdotVEffect = enableNdotVEffectProp.floatValue;
                materialEditor.ShaderProperty(enableNdotVEffectProp, "Enable NdotV Effect");
                if (oldEnableNdotVEffect != enableNdotVEffectProp.floatValue)
                {
                    if (enableNdotVEffectProp.floatValue == 1)
                        material.EnableKeyword("_ENABLE_NDOTV_EFFECT");
                    else
                        material.DisableKeyword("_ENABLE_NDOTV_EFFECT");
                }
            }
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
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
            material.SetFloat("_Blend", (float)blendMode);

            material.SetFloat("_Surface", (float)surfaceType);
            if (surfaceType == SurfaceType.Opaque)
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
        }
    }
}