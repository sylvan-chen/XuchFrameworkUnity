using System;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderEditorGUI
{
    public static partial class Styles
    {
        public static readonly GUIContent hairMap = new GUIContent("Hair Map", "Hair Map");
        public static readonly GUIContent husksMap = new GUIContent("Husks Map", "Husks Map");
        public static readonly GUIContent bakeOriVertsMap = new GUIContent("Origin Bake Map", "Origin Bake Map");
        public static readonly GUIContent maskMap = new GUIContent("Mask Map", "Mask Map");
        public static readonly GUIContent renderHairMode = new GUIContent("Render Hair", "Render Hair");
        // 添加调试开关样式
        public static readonly GUIContent debugVertices = new GUIContent("Debug Vertices", "Enable debug vertices visualization");
        public static readonly GUIContent debugMode = new GUIContent("Debug Mode", "Debug Mode Settings");
        public static readonly GUIContent MaskBlendMap = new GUIContent("Mask Blend Map", "Mask Blend Map");
    }

    public enum BaseColorBlendMode
    {
        Normal = 0,
        Alpha = 1,
        AlphaInverse = 2,
    }

    public enum BaseMaskBlendMode
    {
        Normal = 0,
        RInverse = 1,
    }

    internal class ExtendSimplelitShaderGUI : UnityEditor.BaseShaderGUI
    {
        // Properties
        private SimpleLitGUI.SimpleLitProperties shadingModelProperties;

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
        protected MaterialProperty stencilRefProp { get; set; }
        protected MaterialProperty stencilCompProp { get; set; }
        protected MaterialProperty stencilPassProp { get; set; }
        protected MaterialProperty debugVerticesProp { get; set; } // 添加调试属性
        // 添加调试面板折叠状态
        bool debugFoldout;
        private MaterialProperty workflowModeProp;
        private MaterialProperty metallicMapProp;
        private MaterialProperty specularMapProp;
        private MaterialProperty metallicProp;
        private MaterialProperty specColorProp;
        private MaterialProperty smoothnessProp;
        private MaterialProperty mainColorBlendModeProp;

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            shadingModelProperties = new SimpleLitGUI.SimpleLitProperties(properties);
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

            workflowModeProp = FindProperty("_WorkflowMode", properties);
            metallicMapProp = FindProperty("_MetallicGlossMap", properties);
            specularMapProp = FindProperty("_SpecGlossMap", properties);
            metallicProp = FindProperty("_Metallic", properties);
            specColorProp = FindProperty("_SpecColor", properties);
            smoothnessProp = FindProperty("_Smoothness", properties);
            mainColorBlendModeProp = FindProperty("_MainColorBlendMode", properties);
            // 查找调试属性
            debugVerticesProp = FindProperty("_DebugVertices", properties, false);
        }

        bool renderHairFoldout;
        private const string k_KeyPrefix = "ExtendSimplelitShader:Material:UI_State:";
        private string m_HeaderStateKey = null;

        public override void OnOpenGUI(Material material, MaterialEditor materialEditor)
        {
            base.OnOpenGUI(material, materialEditor);
            m_HeaderStateKey = k_KeyPrefix + material.shader.name; // Create key string for editor prefs
            // renderHairFoldout = new SavedBool($"{m_HeaderStateKey}.RenderHairFoldout", true);
            // 添加调试面板状态
            // debugFoldout = new SavedBool($"{m_HeaderStateKey}.DebugFoldout", false);
            foreach (var obj in materialEditor.targets)
                ValidateMaterial(material);
        }

        // material changed check
        public override void ValidateMaterial(Material material)
        {
            // bool automaticRenderQueue = GetAutomaticQueueControlSetting(material);
            SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords, null);
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
            // if (bakeOriVertsMapProp != null)
            // {
            //     if (bakeOriVertsMapProp.textureValue != null)
            //         material.EnableKeyword("_ORIGINBAKEMAP");
            //     else
            //         material.DisableKeyword("_ORIGINBAKEMAP");
            // }

            // 添加调试顶点关键字验证
            if (debugVerticesProp != null)
            {
                bool enableDebugVertices = debugVerticesProp.floatValue == 1;
                if (enableDebugVertices)
                    material.EnableKeyword("DEBUG_VERTICES");
                else
                    material.DisableKeyword("DEBUG_VERTICES");
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

        public void DrawMetallicSpeularProperties(Material material)
        {
            // 工作流模式选择
            EditorGUI.BeginChangeCheck();
            workflowModeProp.floatValue = EditorGUILayout.Popup("Workflow Mode", (int)workflowModeProp.floatValue, new[] { "Metallic", "Specular" });
            if (EditorGUI.EndChangeCheck())
            {
                if (workflowModeProp.floatValue == 0) // Specular
                {
                    material.EnableKeyword("_METALLIC_SETUP");
                }
                else // Metallic
                {
                    material.DisableKeyword("_METALLIC_SETUP");
                }
            }

            // 根据工作流显示不同属性
            if (workflowModeProp.floatValue == 0) // Metallic
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Metallic Map (R:Metallic, A:Smoothness)"), metallicMapProp);

                materialEditor.ShaderProperty(metallicProp, "Metallic");
            }
            else // Specular
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Specular Map (RGB:Specular, A:Smoothness)"), specularMapProp);

                materialEditor.ShaderProperty(specColorProp, "Specular Color");
            }

            // 共用属性
            materialEditor.ShaderProperty(smoothnessProp, "Smoothness");
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            BaseColorBlendMode mainColorBlendMode = (BaseColorBlendMode)mainColorBlendModeProp.floatValue;
            mainColorBlendMode = (BaseColorBlendMode)EditorGUILayout.EnumPopup("Main Color Blend Mode", mainColorBlendMode);
            mainColorBlendModeProp.floatValue = (float)mainColorBlendMode;

            DrawMetallicSpeularProperties(material);
            SimpleLitGUI.Inputs(shadingModelProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
            ShaderGUIHelper.SplitLine();
            DrawRenderHairPanel(material);
            // 添加调试面板
            ShaderGUIHelper.SplitLine();
            DrawDebugPanel(material);
        }

        public void DrawDebugPanel(Material material)
        {
            if (debugVerticesProp != null)
            {
                debugFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(debugFoldout, ShaderEditorGUI.Styles.debugMode);
                if (debugFoldout)
                {
                    var oldEnableDebugVertices = debugVerticesProp.floatValue == 1;
                    var enableDebugVertices = EditorGUILayout.Toggle("Enable Debug Vertices", debugVerticesProp.floatValue == 1);

                    if (oldEnableDebugVertices != enableDebugVertices)
                    {
                        debugVerticesProp.floatValue = enableDebugVertices ? 1 : 0;
                        if (enableDebugVertices)
                            material.EnableKeyword("DEBUG_VERTICES");
                        else
                            material.DisableKeyword("DEBUG_VERTICES");
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
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

        public override void DrawAdvancedOptions(Material material)
        {
            SimpleLitGUI.Advanced(shadingModelProperties);
            DoPopup(Styles.queueControl, queueControlProp, Styles.queueControlNames);
            if (material.HasProperty("_QueueControl")
                && material.GetFloat("_QueueControl") == (float)QueueControl.UserOverride)
                materialEditor.RenderQueueField();
            base.DrawAdvancedOptions(material);
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
            material.SetFloat("_Surface", (float)surfaceType);
            material.SetFloat("_Blend", (float)blendMode);
        }
    }
}