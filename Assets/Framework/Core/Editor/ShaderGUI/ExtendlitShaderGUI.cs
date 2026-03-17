using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderEditorGUI
{
    internal class ExtendlitShaderGUI : ShaderGUI
    {
        public enum DissolveType
        {
            Vertical,
            Vertex,
        }
        MaterialEditor materialEditor { get; set; }
        public bool m_FirstTimeApply = true;
        private const string k_KeyPrefix = "ExtendlitShader:Material:UI_State:";
        private string m_HeaderStateKey = null;
        
        // Foldout states
        bool m_shadingModeFoldout;
        bool m_SurfaceOptionsFoldout;
        bool m_SurfaceInputsFoldout;
        bool m_AdvancedFoldout;
        
        private const int queueOffsetRange = 50;
        #region Shading Mode       
        MaterialProperty shadingModeProp = null;
        void FindShadingModeProperties(MaterialProperty[] properties)
        {
            shadingModeProp = FindProperty("_ShadingMode", properties);

            FindFabricScatterProperties(properties);

            FindSkinProperties(properties);

            FindClearCoatProperties(properties);

            FindAnisotropicProperties(properties);
        }
        void DrawShadingModeProperties(Material material)
        {
            ShadingMode mode = ShadingModePopup();
            switch (mode)
            {
                case ShadingMode.Fabric:
                    {
                        ShaderGUIHelper.SplitLine();
                        DrawFabricScatterProperties();
                    }
                    break;
                case ShadingMode.Skin:
                    {
                        ShaderGUIHelper.SplitLine();
                        DrawSkinProperties();
                    }
                    break;
                case ShadingMode.ClearCoat:
                    {
                        ShaderGUIHelper.SplitLine();
                        DrawClearCoatProperties();
                    }
                    break;
                case ShadingMode.Anisotropic:
                    {
                        ShaderGUIHelper.SplitLine();
                        DrawAnisotropicProperties();
                    }
                    break;
            }
        }
        ShadingMode ShadingModePopup()
        {
            EditorGUI.showMixedValue = shadingModeProp.hasMixedValue;
            var mode = (ShadingMode)shadingModeProp.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (ShadingMode)EditorGUILayout.Popup(Styles.shadingMode, (int)mode, Styles.shadingNames);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo("Shading Mode");
                shadingModeProp.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
            return mode;
        }

        #region Fabric Scatter
        MaterialProperty fabricScatterColor = null;
        MaterialProperty fabricScatterScale = null;

        void FindFabricScatterProperties(MaterialProperty[] props)
        {
            fabricScatterColor = FindProperty("_FabricScatterColor", props);
            fabricScatterScale = FindProperty("_FabricScatterScale", props);
        }

        void DrawFabricScatterProperties()
        {
            GUILayout.Label("Fabric Scatter", EditorStyles.boldLabel);
            materialEditor.ColorProperty(fabricScatterColor, "Color");
            materialEditor.RangeProperty(fabricScatterScale, "Scale");
        }
        #endregion

        #region Skin
        MaterialProperty brdfTex = null;
        MaterialProperty curvatureScale = null;

        MaterialProperty ambientSky;
        MaterialProperty ambientEquator;
        MaterialProperty ambientGround;

        void FindSkinProperties(MaterialProperty[] props)
        {
            brdfTex = FindProperty("_BRDFMap", props);
            curvatureScale = FindProperty("_CurvatureScale", props);

            ambientSky = FindProperty("_AmbientSky", props);
            ambientEquator = FindProperty("_AmbientEquator", props);
            ambientGround = FindProperty("_AmbientGround", props);
        }

        void DrawSkinProperties()
        {
            GUILayout.Label("Skin", EditorStyles.boldLabel);
            materialEditor.TextureProperty(brdfTex, "BRDF Lookup (RGB)");
            materialEditor.RangeProperty(curvatureScale, "Curvature Scale");

            GUILayout.Label("Ambient", EditorStyles.boldLabel);
            materialEditor.ColorProperty(ambientSky, "Sky Color");
            materialEditor.ColorProperty(ambientEquator, "Equator Color");
            materialEditor.ColorProperty(ambientGround, "Ground Color");
        }
        #endregion

        #region Clear Coat
        MaterialProperty flakesBumpMap = null;
        MaterialProperty flakesBumpMapScale = null;
        MaterialProperty flakesBumpStrength = null;

        MaterialProperty reflectionSpecular = null;
        MaterialProperty reflectionGlossiness = null;

        void FindClearCoatProperties(MaterialProperty[] props)
        {
            flakesBumpMap = FindProperty("_FlakesBumpMap", props);
            flakesBumpMapScale = FindProperty("_FlakesBumpMapScale", props);
            flakesBumpStrength = FindProperty("_FlakesBumpStrength", props);

            reflectionSpecular = FindProperty("_ReflectionSpecular", props);
            reflectionGlossiness = FindProperty("_ReflectionGlossiness", props);
        }

        void DrawClearCoatProperties()
        {
            GUILayout.Label("Clear Coat", EditorStyles.boldLabel);
            ShaderGUIHelper.SplitLine(2f);
            GUILayout.Label("Base Bump Flakes", EditorStyles.miniBoldLabel);
            materialEditor.TexturePropertySingleLine(Styles.flakesBumpMapText, flakesBumpMap, flakesBumpMapScale);

            materialEditor.RangeProperty(flakesBumpStrength, "Strength");
            ShaderGUIHelper.SplitLine(2f);
            GUILayout.Label("Reflection", EditorStyles.miniBoldLabel);
            materialEditor.ColorProperty(reflectionSpecular, "Specular");
            materialEditor.RangeProperty(reflectionGlossiness, "Glossiness");
        }
        #endregion

        #region Anisotropic
        MaterialProperty anisotrpicT;
        MaterialProperty anisotrpicB;
        //MaterialProperty anisotrpicDirection;

        void FindAnisotropicProperties(MaterialProperty[] props)
        {
            anisotrpicT = FindProperty("_AnisotropyT", props, false);
            anisotrpicB = FindProperty("_AnisotropyB", props, false);

            //anisotrpicDirection = FindProperty("_AnisotropyDirection", props, false);
        }

        void DrawAnisotropicProperties()
        {
            GUILayout.Label("Anisotropic", EditorStyles.boldLabel);
            materialEditor.RangeProperty(anisotrpicT, "Tangent");
            materialEditor.RangeProperty(anisotrpicB, "Bitangent");

            //materialEditor.TexturePropertySingleLine(Styles.anisotropicDirectionText, anisotrpicDirection);
        }

        #endregion
        #endregion

        #region Surface Options               
        private MaterialProperty surfaceTypeProp { get; set; }
        private MaterialProperty blendModeProp { get; set; }
        private MaterialProperty cullingProp { get; set; }
        private MaterialProperty alphaClipProp { get; set; }
        private MaterialProperty alphaCutoffProp { get; set; }
        private MaterialProperty zWriteModeProp { get; set; }
        private MaterialProperty receiveShadowsProp { get; set; }
        protected MaterialProperty stencilRefProp { get; set; }
        protected MaterialProperty stencilCompProp { get; set; }
        protected MaterialProperty stencilPassProp { get; set; }
        private void FindSurfaceOptionsProperties(MaterialProperty[] properties)
        {
            surfaceTypeProp = FindProperty("_Surface", properties);
            blendModeProp = FindProperty("_Blend", properties);
            cullingProp = FindProperty("_Cull", properties);
            alphaClipProp = FindProperty("_AlphaClip", properties);
            alphaCutoffProp = FindProperty("_Cutoff", properties);
            zWriteModeProp = FindProperty("_ZWrite", properties);
            receiveShadowsProp = FindProperty("_ReceiveShadows", properties, false);
            stencilRefProp = FindProperty("_Stencil", properties, false);
            stencilCompProp = FindProperty("_StencilComp", properties, false);
            stencilPassProp = FindProperty("_StencilOp", properties, false);
        }

        private void DrawSurfaceOptions(Material material)
        {
            ShaderGUIHelper.DoPopup(Styles.surfaceType, surfaceTypeProp, Enum.GetNames(typeof(SurfaceType)), materialEditor);
            if ((SurfaceType)material.GetFloat("_Surface") == SurfaceType.Transparent)
            {
                ShaderGUIHelper.DoPopup(Styles.blendingMode, blendModeProp, Enum.GetNames(typeof(BlendMode)), materialEditor);

                materialEditor.ShaderProperty(zWriteModeProp, "ZWrite OnOff");
            }
            if ((SurfaceType)material.GetFloat("_Surface") == SurfaceType.Opaque)
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

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

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = alphaClipProp.hasMixedValue;
            var alphaClipEnabled = EditorGUILayout.Toggle(Styles.alphaClipText, alphaClipProp.floatValue == 1);
            if (EditorGUI.EndChangeCheck())
                alphaClipProp.floatValue = alphaClipEnabled ? 1 : 0;
            EditorGUI.showMixedValue = false;

            if (alphaClipProp.floatValue == 1)
                materialEditor.ShaderProperty(alphaCutoffProp, Styles.alphaClipThresholdText, 1);

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
        void FindSurfaceInputs(MaterialProperty[] properties)
        {
            FindBaseProperties(properties);

            FindNormalProperties(properties);

            FindEffectMaskProperties(properties);

            FindEffectModeProperties(properties);

            FindLaserProperties(properties);

            FindGlitterProperties(properties);

            FindSparkleProperties(properties);

            FindMaskProperties(properties);

            FindEmissionProperties(properties);

            FindEnvironmentProperties(properties);

            FindDissolveProperties(properties);

            FindPowerProperties(properties);

            FindDetailEffectModeProperties(properties);

            FindDetailProperties(properties);

            FindFlowProperties(properties);
        }

        void DrawSurfaceInputs(Material material)
        {
            // Primary properties
            GUILayout.Label(Styles.primaryMapsText, EditorStyles.boldLabel);
            ShaderGUIHelper.SplitLine();
            DrawEffectMaskProperties();

            ShaderGUIHelper.SplitLine();
            DrawBaseProperties(material);

            ShaderGUIHelper.SplitLine();
            DrawNormalProperties();

            ShaderGUIHelper.SplitLine();
            DrawMaskProperties();
            ShaderGUIHelper.SplitLine();
            DrawPowerProperties();

            ShaderGUIHelper.SplitLine(4f);
            EffectMode effectMode = EffectModePopup();

            switch (effectMode)
            {
                case EffectMode.Laser:
                    {
                        DrawLaserProperties();
                    }
                    break;
                case EffectMode.Glitter:
                    {
                        DrawGlitterProperties();
                    }
                    break;
            }

            ShaderGUIHelper.SplitLine(4f);
            DrawSparkleProperties();

            ShaderGUIHelper.SplitLine();
            DrawEmissionProperties(material);

            ShaderGUIHelper.SplitLine();
            DrawEnvironmentProperties(material);

            ShaderGUIHelper.SplitLine();
            DrawDissolveProperties(material);

            ShaderGUIHelper.SplitLine();
            EditorGUI.BeginChangeCheck();
            materialEditor.TextureScaleOffsetProperty(baseMapProp);
            if (EditorGUI.EndChangeCheck())
                effectMaskMap.textureScaleAndOffset = baseMapProp.textureScaleAndOffset; // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake

            EditorGUILayout.Space();

            ShaderGUIHelper.SplitLine();
            GUILayout.Label(Styles.secondaryMapsText, EditorStyles.boldLabel);
            // Secondary properties           
            DetailEffectMode detailEffectMode = DetailEffectModePopup();

            switch (detailEffectMode)
            {
                case DetailEffectMode.None:
                    break;
                case DetailEffectMode.Detail:
                    DrawDetailProperties();
                    EditorGUILayout.Space();
                    break;
                case DetailEffectMode.Flow:
                    DrawFlowProperties();
                    EditorGUILayout.Space();
                    break;
            }
        }

        #region Base Properties
        protected MaterialProperty baseMapProp { get; set; }
        protected MaterialProperty baseColorProp { get; set; }

        protected MaterialProperty baseColorMaskProp { get; set; }

        protected void FindBaseProperties(MaterialProperty[] properties)
        {
            baseColorMaskProp = FindProperty("_BaseColorMaskChannel", properties, false);

            baseMapProp = FindProperty("_BaseMap", properties, false);
            baseColorProp = FindProperty("_BaseColor", properties, false);
        }

        protected void DrawBaseProperties(Material material)
        {
            MaskChannelPopup(baseColorMaskProp, "Mask Channel(None)");

            if (baseMapProp != null && baseColorProp != null) // Draw the baseMap, most shader will have at least a baseMap
            {
                materialEditor.TexturePropertySingleLine(Styles.baseMap, baseMapProp, baseColorProp);                
            }
        }
        #endregion

        #region Normal Properties
        MaterialProperty bumpMap = null;
        MaterialProperty bumpMapScale = null;
        protected void FindNormalProperties(MaterialProperty[] properties)
        {
            bumpMap = FindProperty("_BumpMap", properties);
            bumpMapScale = FindProperty("_BumpScale", properties, false);
        }

        protected void DrawNormalProperties()
        {
            materialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMap);
            materialEditor.FloatProperty(bumpMapScale, "Normal Scale");
            //if (bumpMapScale.floatValue != 1 && UnityEditorInternal.InternalEditorUtility.IsMobilePlatform(EditorUserBuildSettings.activeBuildTarget))
            //    if (materialEditor.HelpBoxWithButton(
            //        EditorGUIUtility.TrTextContent("Bump scale is not supported on mobile platforms"),
            //        EditorGUIUtility.TrTextContent("Fix Now")))
            //    {
            //        bumpMapScale.floatValue = 1;
            //    }
        }
        #endregion

        #region Mask
        MaterialProperty maskMap = null;

        MaterialProperty occlusionStrength = null;
        MaterialProperty smoothness = null;
        MaterialProperty metallic = null;
        MaterialProperty specularPower = null;

        void FindMaskProperties(MaterialProperty[] props)
        {
            maskMap = FindProperty("_MaskMap", props);

            occlusionStrength = FindProperty("_OcclusionStrength", props);
            smoothness = FindProperty("_Smoothness", props);
            specularPower = FindProperty("_SpecularPower", props);
            metallic = FindProperty("_Metallic", props, false);
        }
        void DrawMaskProperties()
        {
            materialEditor.TexturePropertySingleLine(Styles.maskMapText, maskMap);

            materialEditor.RangeProperty(smoothness, Styles.smoothnessText);
            materialEditor.RangeProperty(metallic, Styles.metallicText);
            materialEditor.RangeProperty(occlusionStrength, Styles.occlusionText);
            materialEditor.RangeProperty(specularPower, Styles.specularPowerText);
        }
        #endregion

        #region Effect Mask 
        MaterialProperty effectMaskMap = null;
        MaterialProperty maskValueOffset = null;
        void FindEffectMaskProperties(MaterialProperty[] props)
        {
            effectMaskMap = FindProperty("_EffectMaskMap", props);
            if(BaseShaderGUI.HasProperty(props, "_MaskValueOffset"))
                maskValueOffset = FindProperty("_MaskValueOffset", props);
        }

        void DrawEffectMaskProperties()
        {
            materialEditor.TexturePropertySingleLine(Styles.effectMaskMapText, effectMaskMap);
            if (maskValueOffset != null)
                materialEditor.RangeProperty(maskValueOffset, Styles.maskValueOffsetText);
        }

        #endregion        

        #region Effect Mode
        MaterialProperty effectMode = null;

        void FindEffectModeProperties(MaterialProperty[] props)
        {
            effectMode = FindProperty("_EffectMode", props);
        }

        EffectMode EffectModePopup()
        {
            EditorGUI.showMixedValue = effectMode.hasMixedValue;
            var mode = (EffectMode)effectMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (EffectMode)EditorGUILayout.Popup(Styles.effectMode, (int)mode, Styles.effectNames);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo("Effect Mode");
                effectMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
            return mode;
        }
        #endregion

        #region Laser
        MaterialProperty laserMask = null;

        MaterialProperty laserMap = null;
        MaterialProperty laserStrength = null;

        MaterialProperty laserColorHue = null;
        MaterialProperty laserColorSaturation = null;
        MaterialProperty laserColorBrightness = null;
        MaterialProperty laserColorContrast = null;

        void FindLaserProperties(MaterialProperty[] props)
        {
            laserMask = FindProperty("_LaserMaskChannel", props);
            laserMap = FindProperty("_LaserRampMap", props);
            laserStrength = FindProperty("_LaserStrength", props);

            laserColorHue = FindProperty("_LaserColorHue", props);
            laserColorSaturation = FindProperty("_LaserColorSaturation", props);
            laserColorBrightness = FindProperty("_LaserColorBrightness", props);
            laserColorContrast = FindProperty("_LaserColorContrast", props);
        }

        void DrawLaserProperties()
        {
            MaskChannelPopup(laserMask, "Mask Channel(B)");

            materialEditor.TexturePropertySingleLine(Styles.laserRampMapText, laserMap, laserStrength);

            materialEditor.RangeProperty(laserColorHue, "Hue");
            materialEditor.RangeProperty(laserColorSaturation, "Saturation");
            materialEditor.RangeProperty(laserColorBrightness, "Brightness");
            materialEditor.RangeProperty(laserColorContrast, "Contrast");
        }
        #endregion

        #region Glitter
        MaterialProperty glitterMask = null;
        MaterialProperty glitterMap = null;
        MaterialProperty glitterScale = null;

        MaterialProperty glitterBrightness = null;
        MaterialProperty glitterPower = null;
        MaterialProperty glitterSpeed = null;

        void FindGlitterProperties(MaterialProperty[] props)
        {
            glitterMask = FindProperty("_GlitterMaskChannel", props);
            glitterMap = FindProperty("_GlitterMap", props);
            glitterScale = FindProperty("_GlitterScale", props);

            glitterBrightness = FindProperty("_GlitterBrightness", props);
            glitterPower = FindProperty("_GlitterPower", props);
            glitterSpeed = FindProperty("_GlitterySpeed", props);
        }

        void DrawGlitterProperties()
        {
            MaskChannelPopup(glitterMask, "Mask Channel(B)");

            materialEditor.TexturePropertySingleLine(Styles.glitterMapText, glitterMap, glitterScale);

            materialEditor.RangeProperty(glitterBrightness, "Brightness");
            materialEditor.RangeProperty(glitterPower, "Power");
            materialEditor.RangeProperty(glitterSpeed, "Speed");
        }
        #endregion

        #region Sparkle
        MaterialProperty sparkle;
        MaterialProperty sparkleMask;
        MaterialProperty sparkleColor;
        MaterialProperty sparkleDepth;
        MaterialProperty sparkleNoiseScale;
        MaterialProperty sparkleAnimationSpeed;
        MaterialProperty minkowskiNumber;

        void FindSparkleProperties(MaterialProperty[] props)
        {
            sparkle = FindProperty("_Sparkle", props);
            sparkleMask = FindProperty("_SparkleMaskChannel", props);
            sparkleColor = FindProperty("_SparkleColor", props);
            sparkleDepth = FindProperty("_SparkleDepth", props);
            sparkleNoiseScale = FindProperty("_NoiseScale", props);
            sparkleAnimationSpeed = FindProperty("_AnimSpeed", props);
            minkowskiNumber = FindProperty("_MinkowskiNumber", props);
        }

        SparkleType SparkleTypePopup()
        {
            EditorGUI.showMixedValue = sparkle.hasMixedValue;
            var mode = (SparkleType)sparkle.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (SparkleType)EditorGUILayout.Popup(Styles.sparkleType, (int)mode, Styles.sparkleTypeNames);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo("Sparkle Type");
                sparkle.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
            return mode;
        }

        void DrawSparkleProperties()
        {
            var type = SparkleTypePopup();

            switch (type)
            {
                case SparkleType.None:
                    break;
                case SparkleType.Euclidean:
                case SparkleType.Minkowski:
                    MaskChannelPopup(sparkleMask, "Mask Channel(R)");
                    // Subsurface            
                    materialEditor.ColorProperty(sparkleColor, "Color");
                    materialEditor.RangeProperty(sparkleDepth, "Depth");
                    materialEditor.RangeProperty(sparkleNoiseScale, "Noise Scale");
                    if (type == SparkleType.Minkowski)
                    {
                        materialEditor.RangeProperty(minkowskiNumber, "Minkowski Number");
                    }

                    materialEditor.RangeProperty(sparkleAnimationSpeed, "Animation Speed");
                    break;
            }
        }
        #endregion

        #region Detail Effect Mode
        MaterialProperty detailEffectMode = null;

        void FindDetailEffectModeProperties(MaterialProperty[] props)
        {
            detailEffectMode = FindProperty("_DetailEffectMode", props);
        }

        DetailEffectMode DetailEffectModePopup()
        {
            EditorGUI.showMixedValue = detailEffectMode.hasMixedValue;
            var mode = (DetailEffectMode)detailEffectMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (DetailEffectMode)EditorGUILayout.Popup(Styles.detailEffectMode, (int)mode, Styles.detailEffectNames);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo("Detail Effect Mode");
                detailEffectMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
            return mode;
        }
        #endregion

        #region Detail
        MaterialProperty detailType = null;
        MaterialProperty detailMask = null;
        MaterialProperty detailAlbedoMap = null;
        MaterialProperty detailNormalMapScale = null;
        MaterialProperty detailNormalMap = null;
        MaterialProperty uvSetSecondary = null;
        void FindDetailProperties(MaterialProperty[] props)
        {
            if (BaseShaderGUI.HasProperty(props, "_DetailType"))
                detailType = FindProperty("_DetailType", props);
            detailMask = FindProperty("_DetailMaskChannel", props);
            detailAlbedoMap = FindProperty("_DetailBaseMap", props);
            detailNormalMapScale = FindProperty("_DetailNormalMapScale", props);
            detailNormalMap = FindProperty("_DetailNormalMap", props);
            if (BaseShaderGUI.HasProperty(props, "_UVSec"))
                uvSetSecondary = FindProperty("_UVSec", props);
        }

        void MaskChannelPopup(MaterialProperty mask, string label)
        {
            EditorGUI.showMixedValue = mask.hasMixedValue;
            var channel = (MaskChannel)mask.floatValue;

            EditorGUI.BeginChangeCheck();
            channel = (MaskChannel)EditorGUILayout.Popup(label, (int)channel, Styles.maskChannelNames);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(mask.displayName);
                mask.floatValue = (float)channel;
            }

            EditorGUI.showMixedValue = false;
        }

        void DrawDetailProperties()
        {
            if(detailType != null)
                materialEditor.ShaderProperty(detailType, "Detail Type");
            MaskChannelPopup(detailMask, Styles.detailMaskText.text);
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

        #region Flow 
        MaterialProperty flowMask = null;
        MaterialProperty flowMap = null;
        MaterialProperty flowSpeed = null;

        MaterialProperty flowColor = null;
        MaterialProperty flowPower = null;

        void FindFlowProperties(MaterialProperty[] props)
        {
            flowMask = FindProperty("_FlowMaskChannel", props);
            flowMap = FindProperty("_FlowMap", props);
            flowSpeed = FindProperty("_FlowSpeed", props);

            flowColor = FindProperty("_FlowColor", props);
            flowPower = FindProperty("_FlowPower", props);
        }

        void DrawFlowProperties()
        {
            MaskChannelPopup(flowMask, "Mask Channel(G)");

            materialEditor.TexturePropertySingleLine(Styles.flowMapText, flowMap, flowColor);

            materialEditor.RangeProperty(flowPower, "Power");

            materialEditor.VectorProperty(flowSpeed, "Speed(UV)");

            materialEditor.TextureScaleOffsetProperty(flowMap);

            if(uvSetSecondary != null)
                materialEditor.ShaderProperty(uvSetSecondary, Styles.uvSetLabel.text);
        }
        #endregion

        #region Emission Properties
        protected MaterialProperty emissionMaskProp { get; set; }
        protected MaterialProperty emissionColorProp { get; set; }
        protected MaterialProperty emmissionMap { get; set; }

        protected void FindEmissionProperties(MaterialProperty[] properties)
        {
            emissionMaskProp = FindProperty("_EmissionMaskChannel", properties, false);
            emissionColorProp = FindProperty("_EmissionColor", properties, false);
            emmissionMap = FindProperty("_EmissionMap", properties, false);
        }

        protected void DrawEmissionProperties(Material material)
        {
            // Emission for GI?
            if (materialEditor.EmissionEnabledProperty())
            {
                bool hadEmission = emissionMaskProp.floatValue > 0.0;

                // Texture and HDR color controls
                MaskChannelPopup(emissionMaskProp, "Emission Mask(A)");
                materialEditor.ColorProperty(emissionColorProp, "Emission (RGB)");

                // If texture was assigned and color was black set color to white
                float brightness = emissionColorProp.colorValue.maxColorComponent;
                if (emissionMaskProp.floatValue > 0.0 && !hadEmission && brightness <= 0f)
                    emissionColorProp.colorValue = Color.white;

                // change the GI flag and fix it up with emissive as black if necessary
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                if (brightness <= 0f)
                    material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                materialEditor.TexturePropertySingleLine(Styles.emissionMapText, emmissionMap);
            }
        }
        #endregion

        #region Environment Properties
        protected MaterialProperty customVertexLightingIntensityProp { get; set; }
        protected MaterialProperty customVertexLightColorProp { get; set; }

        protected MaterialProperty useCustomVertexLightingSettingProp { get; set; }

        protected void FindEnvironmentProperties(MaterialProperty[] properties)
        {
            customVertexLightingIntensityProp = FindProperty("_CustomVertexLightingIntensity", properties, false);
            customVertexLightColorProp = FindProperty("_CustomVertexLightColor", properties, false);
            useCustomVertexLightingSettingProp = FindProperty("_UseCustomVertexLightingSetting", properties, false);
        }

        protected void DrawEnvironmentProperties(Material material)
        {
            //materialEditor.
            if (material.HasFloat("_UseCustomVertexLightingSetting"))
            {
                float useCustomVertexLightingSetting = material.GetFloat("_UseCustomVertexLightingSetting");
                bool showGUI = useCustomVertexLightingSetting > 0.5 ? true : false;
                showGUI = EditorGUILayout.Toggle(Styles.useCustomVertexLightingText, showGUI);
                if (showGUI)
                {
                    materialEditor.RangeProperty(customVertexLightingIntensityProp, Styles.customVertexLightingIntensityText);
                    materialEditor.ColorProperty(customVertexLightColorProp, Styles.customVertexLightColorText);
                    material.SetFloat("_UseCustomVertexLightingSetting", 1);
                }
                else
                {
                    material.SetFloat("_UseCustomVertexLightingSetting", 0);
                }
            }
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
                float useDissolve= material.GetFloat("_UseDissolve");
                bool showGUI = useDissolve > 0.5 ? true : false;
                showGUI = EditorGUILayout.Toggle(Styles.useDissolveText, showGUI);
                float dissolveReverce = material.GetFloat("_DissolveReverce");
                if (showGUI)
                {
                    material.EnableKeyword("_USEDISSOLVE_ON");
                    material.SetFloat("_UseDissolve", 1);
                    // for _DissolveReverce
                    bool isReverce = dissolveReverce > 0.5 ? true : false;
                    isReverce = EditorGUILayout.Toggle(Styles.dissolveReverceText, isReverce);
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
                    else if (dissolveTypeValue == 1)
                    {
                        material.EnableKeyword("_DISSOLVETYPE_VERTEX");
                        material.DisableKeyword("_DISSOLVETYPE_VERTICAL");
                    }
                    material.EnableKeyword("REQUIRES_WORLD_SPACE_POS_INTERPOLATOR");
                    materialEditor.FloatProperty(dissolveHeightProp, Styles.dissolveHightText);
                    materialEditor.ColorProperty(dissolveColorProp, Styles.dissolveColorText);
                    materialEditor.TextureProperty(dissolveTextureProp, Styles.dissolveTextureText);
                    materialEditor.FloatProperty(dissolveLerpDistanceProp, Styles.dissolveLerpDistanceText);
                    materialEditor.FloatProperty(dissolveVertexColorIncrementProp, Styles.dissolveVertexColorIncrementText);
                    materialEditor.FloatProperty(dissolveVertexPowerProp, Styles.dissolveVertexPowerText);
                }
                else
                {
                    material.SetFloat("_UseDissolve", 0);
                    material.DisableKeyword("_USEDISSOLVE_ON");
                }
            }
        }

        #endregion

        #region Power Property
        protected MaterialProperty powerMaskProp { get; set; }

        MaterialProperty powerProp = null;
        protected void FindPowerProperties(MaterialProperty[] properties)
        {
            powerMaskProp = FindProperty("_PowerMaskChannel", properties, false);

            powerProp = FindProperty("_Power", properties);
        }

        protected void DrawPowerProperties()
        {
            MaskChannelPopup(powerMaskProp, "Mask Channel(None)");

            materialEditor.RangeProperty(powerProp, "Power");
        }
        #endregion
        #endregion

        #region Advanced Options
        protected MaterialProperty queueOffsetProp { get; set; }
        protected MaterialProperty clipByMirrorProp { get; set; }
        protected virtual void FindAdvancedOptionsProperties(MaterialProperty[] properties)
        {
            queueOffsetProp = FindProperty("_QueueOffset", properties, false);
            clipByMirrorProp = FindProperty("_ClipByMirror", properties, false);
        }

        public virtual void DrawAdvancedOptions(Material material)
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
            // m_shadingModeFoldout = new SavedBool($"{m_HeaderStateKey}.ShadingModeFoldout", true);
            // m_SurfaceOptionsFoldout = new SavedBool($"{m_HeaderStateKey}.SurfaceOptionsFoldout", true);
            // m_SurfaceInputsFoldout = new SavedBool($"{m_HeaderStateKey}.SurfaceInputsFoldout", true);
            // m_AdvancedFoldout = new SavedBool($"{m_HeaderStateKey}.AdvancedFoldout", false);

            foreach (var obj in materialEditor.targets)
                MaterialChanged((Material)obj);
        }

        // 材质改变，Keyword 设置
        void MaterialChanged(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            ResetMaterialKeywords(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            if (ConvertLegacyShader(material, oldShader, newShader, base.AssignNewShaderToMaterial))
            {
                return;
            }

            ConvertOtherShader(material, oldShader, newShader);
        }

        public void ConvertOtherShader(Material material, Shader oldShader, Shader newShader)
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

        public static bool IsLegacy(string shaderName)
        {
            if (shaderName.Equals("ExtendStandardFur"))
            {
                return false;
            }

            return shaderName.Contains("ExtendStandard");
        }

        public static string ShaderName()
        {
            return "URP/ExtendLit";
        }

        public static bool ConvertLegacyShader(Material material, Shader oldShader, Shader newShader
                                                    , Action<Material, Shader, Shader> SetShader)
        {
            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            if (oldShader == null)
            {
                SetShader(material, oldShader, newShader);
                return false;
            }

            bool isLegacy = IsLegacy(oldShader.name);
            BasicsShaderGUI.BlendMode legacyBlendMode = BasicsShaderGUI.BlendMode.Opaque;
            Color baseColor = Color.white;
            bool shadowPass = true;
            bool twoSides = true;
            int renderQueue = -1;
            if (isLegacy)
            {
                legacyBlendMode = (BasicsShaderGUI.BlendMode)material.GetFloat("_Mode");

                baseColor = material.TryGetColor("_Color", Color.white);

                shadowPass = material.TryGetFloat("_ShadowPass", 1.0f) > 0.5f;
                twoSides = material.TryGetFloat("_TwoSides", 0.0f) > 0.5f;

                renderQueue = material.renderQueue;
                Debug.Log("CCCCCCCCCCCCC");
            }

            SetShader(material, oldShader, newShader);

            if (!isLegacy) return false;

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;

            bool alphaClip = false;
            switch (legacyBlendMode)
            {
                case BasicsShaderGUI.BlendMode.Opaque:
                    surfaceType = SurfaceType.Opaque;
                    break;
                case BasicsShaderGUI.BlendMode.Cutout:
                    surfaceType = SurfaceType.Opaque;
                    alphaClip = true;
                    break;
                case BasicsShaderGUI.BlendMode.Fade:
                    surfaceType = SurfaceType.Transparent;
                    blendMode = BlendMode.Alpha;
                    break;
                case BasicsShaderGUI.BlendMode.Transparent:
                    surfaceType = SurfaceType.Transparent;
                    blendMode = BlendMode.Premultiply;
                    break;
                case BasicsShaderGUI.BlendMode.Additive:
                    surfaceType = SurfaceType.Transparent;
                    blendMode = BlendMode.Additive;
                    break;
            }

            material.SetFloat("_Surface", (float)surfaceType);
            material.SetFloat("_Blend", (float)blendMode);

            material.SetFloat("_AlphaClip", alphaClip ? 1.0f : 0.0f);

            material.SetColor("_BaseColor", baseColor);
            material.CopyTextureProperty("_MainTex", "_BaseMap", true);
            material.CopyTextureProperty("_DetailAlbedoMap", "_DetailBaseMap", true);

            material.CopyTextureProperty("_BRDFTex", "_BRDFMap", false);

            material.SetFloat("_ReceiveShadows", shadowPass ? 1.0f : 0.0f);
            if (twoSides)
            {
                material.SetFloat("_Cull", 0.0f);
            }

            material.renderQueue = renderQueue;
            Debug.Log("CCCCCCCCCCCCC");
            ResetMaterialKeywords(material);
            return true;
        }

        public static void ResetMaterialKeywords(Material material)
        {
            ClearMaterialKeywords(material);

            SetupMaterialWithShadingMode(material);

            SetupMaterialSurfaceOptions(material);

            SetMaterialKeywords(material);

            SetupMaterialWithEffectMode(material);

            SetupMaterialWithSparkleType(material);

            SetupMaterialWithDetailEffectMode(material);
        }

        public static void ClearMaterialKeywords(Material material)
        {
            // Clear all keywords for fresh start
            material.shaderKeywords = null;
        }

        public static void SetupMaterialWithShadingMode(Material material)
        {
            ShadingMode shadingMode = (ShadingMode)material.GetFloat("_ShadingMode");
            switch (shadingMode)
            {
                case ShadingMode.Standard:
                    material.DisableKeyword("_SKIN");
                    material.DisableKeyword("_FABRIC");
                    material.DisableKeyword("_CLEARCOAT");
                    material.DisableKeyword("_ANISOTROPIC");
                    break;
                case ShadingMode.Skin:
                    material.EnableKeyword("_SKIN");
                    material.DisableKeyword("_FABRIC");
                    material.DisableKeyword("_CLEARCOAT");
                    material.DisableKeyword("_ANISOTROPIC");
                    break;
                case ShadingMode.Fabric:
                    material.DisableKeyword("_SKIN");
                    material.EnableKeyword("_FABRIC");
                    material.DisableKeyword("_CLEARCOAT");
                    material.DisableKeyword("_ANISOTROPIC");
                    break;
                case ShadingMode.ClearCoat:
                    material.DisableKeyword("_SKIN");
                    material.DisableKeyword("_FABRIC");
                    material.EnableKeyword("_CLEARCOAT");
                    material.DisableKeyword("_ANISOTROPIC");
                    break;
                case ShadingMode.Anisotropic:
                    material.DisableKeyword("_SKIN");
                    material.DisableKeyword("_FABRIC");
                    material.DisableKeyword("_CLEARCOAT");
                    material.EnableKeyword("_ANISOTROPIC");
                    break;
            }
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
                    // Debug.Log("CCCCCCCCCCCCC");
                    //material.renderQueue += material.HasProperty("_QueueOffset") ? (int)material.GetFloat("_QueueOffset") : 0;
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
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
                            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                            break;
                        case BlendMode.Premultiply:
                            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                            break;
                        case BlendMode.Additive:
                            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                            break;
                    }

                    // General Transparent Material Settings
                    material.SetOverrideTag("RenderType", "Transparent");
                    //material.SetInt("_ZWrite", 0);
                    if (renderQueue < (int)RenderQueue.Transparent - 50)
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

        public static void SetMaterialKeywords(Material material)
        {
            // Normal Map
            // Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
            // (MaterialProperty value might come from renderer material property block)            
            var shadingMode = (ShadingMode)material.GetFloat("_ShadingMode");
            if (shadingMode == ShadingMode.ClearCoat)
            {
                CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap")
                                                || material.GetTexture("_DetailNormalMap")
                                                || material.GetTexture("_FlakesBumpMap"));
            }
            else
            {
                CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap")
                                                || material.GetTexture("_DetailNormalMap"));
            }

            CoreUtils.SetKeyword(material, "_MASKMAP", material.GetTexture("_MaskMap"));

            CoreUtils.SetKeyword(material, "_EFFECTMASKMAP", material.GetTexture("_EffectMaskMap"));

            CoreUtils.SetKeyword(material, "_DETAIL", material.GetTexture("_DetailBaseMap")
                                               || material.GetTexture("_DetailNormalMap"));

            CoreUtils.SetKeyword(material, "_FLOW_LIGHT", material.GetTexture("_FlowMap"));


            // A material's GI flag internally keeps track of whether emission is enabled at all, it's enabled but has no effect
            // or is enabled and may be modified at runtime. This state depends on the values of the current flag and emissive color.
            // The fixup routine makes sure that the material is in the correct state if/when changes are made to the mode or color.
            MaterialEditor.FixupEmissiveFlag(material);
            bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            CoreUtils.SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

            //SetKeyword(material, "_SUBSURFACE_ON", material.GetFloat("_Subsurface") > 0.0);

            //SetKeyword(material, "_SPARKLE", material.GetFloat("_Sparkle") > 0.0);
        }

        public static void SetupMaterialWithEffectMode(Material material)
        {
            EffectMode effectMode = (EffectMode)material.GetFloat("_EffectMode");
            switch (effectMode)
            {
                case EffectMode.None:
                    material.DisableKeyword("_LASER");
                    material.DisableKeyword("_GLITTER");
                    break;
                case EffectMode.Laser:
                    material.EnableKeyword("_LASER");
                    material.DisableKeyword("_GLITTER");
                    break;
                case EffectMode.Glitter:
                    material.DisableKeyword("_LASER");
                    material.EnableKeyword("_GLITTER");
                    break;
            }
        }

        public static void SetupMaterialWithSparkleType(Material material)
        {
            SparkleType sparkleType = (SparkleType)material.GetFloat("_Sparkle");
            switch (sparkleType)
            {
                case SparkleType.None:
                    material.DisableKeyword("_SPARKLE_EUCLIDEAN");
                    material.DisableKeyword("_SPARKLE_MINKOWSKI");
                    break;
                case SparkleType.Euclidean:
                    material.EnableKeyword("_SPARKLE_EUCLIDEAN");
                    material.DisableKeyword("_SPARKLE_MINKOWSKI");
                    break;
                case SparkleType.Minkowski:
                    material.DisableKeyword("_SPARKLE_EUCLIDEAN");
                    material.EnableKeyword("_SPARKLE_MINKOWSKI");
                    break;
            }
        }

        public static void SetupMaterialWithDetailEffectMode(Material material)
        {
            DetailEffectMode effectMode = (DetailEffectMode)material.GetFloat("_DetailEffectMode");
            switch (effectMode)
            {
                case DetailEffectMode.None:
                    material.SetTexture("_DetailBaseMap", null);
                    material.SetTexture("_DetailNormalMap", null);

                    material.SetTexture("_FlowMap", null);
                    break;
                case DetailEffectMode.Detail:
                    material.SetTexture("_FlowMap", null);
                    break;
                case DetailEffectMode.Flow:
                    material.SetTexture("_DetailBaseMap", null);
                    material.SetTexture("_DetailNormalMap", null);
                    break;
            }
        }
        #endregion

        // 属性
        void FindProperties(MaterialProperty[] properties)
        {
            FindShadingModeProperties(properties);

            FindSurfaceOptionsProperties(properties);

            FindSurfaceInputs(properties);

            FindAdvancedOptionsProperties(properties);
        }

        void DrawProperties(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            EditorGUI.BeginChangeCheck();

            m_shadingModeFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_shadingModeFoldout, Styles.ShadingModes);
            if (m_shadingModeFoldout)
            {
                DrawShadingModeProperties(material);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

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

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in materialEditor.targets)
                    MaterialChanged((Material)obj);
            }
        }
        ////////////////////////////////////
        // Material Data Functions        //
        ////////////////////////////////////
        #region MaterialDataFunctions


        #endregion
    }
}