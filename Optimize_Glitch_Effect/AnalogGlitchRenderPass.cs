using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering; // GraphicsFormat 등

namespace URPGlitch.Runtime.AnalogGlitch
{
    
    sealed class AnalogGlitchRenderPass : ScriptableRenderPass, IDisposable
    {
        const string RenderPassName = "AnalogGlitch RenderPass (Optimized for VR MultiPass)";

        // 셰이더 프로퍼티 ID
        static readonly int MainTexID        = Shader.PropertyToID("_MainTex");
        static readonly int ScanLineJitterID = Shader.PropertyToID("_ScanLineJitter");
        static readonly int VerticalJumpID   = Shader.PropertyToID("_VerticalJump");
        static readonly int HorizontalShakeID= Shader.PropertyToID("_HorizontalShake");
        static readonly int ColorDriftID     = Shader.PropertyToID("_ColorDrift");

        // 프로파일링
        readonly ProfilingSampler _profilingSampler;

        // 실제 글리치 재질(머티리얼)과 볼륨(파라미터)
        readonly Material _glitchMaterial;
        readonly AnalogGlitchVolume _volume;

        // 임시 RT
        RTHandle _mainFrame;
        int      _mainFrameID;

        // 내부 상태
        float _verticalJumpTime;
        int   _frameCount;

        // 다운샘플 비율
        float _downsampleFactor;

        // N프레임마다 한 번씩 적용
        int _applyEveryNFrame;

        // FPS 측정 (동적 조절용)
        float _fpsTimer;
        int   _fpsFrameCount;
        float _avgFps;

        // 활성화 여부
        bool IsActive =>
            _glitchMaterial != null &&
            _volume != null &&
            _volume.IsActive; // VolumeComponent.active

       
        public AnalogGlitchRenderPass(Shader shader, float downsampleFactor = 0.1f, int applyEveryNFrame = 10)
        {
            try
            {
                // 언제 실행할지
                renderPassEvent   = RenderPassEvent.AfterRenderingPostProcessing;
                _profilingSampler = new ProfilingSampler(RenderPassName);

                // 셰이더 머티리얼
                _glitchMaterial = CoreUtils.CreateEngineMaterial(shader);

            
                var volumeStack = VolumeManager.instance.stack;
                _volume = volumeStack.GetComponent<AnalogGlitchVolume>();

                // 임시 RT용 ID
                _mainFrameID = Shader.PropertyToID("_MainFrame");

                // Placeholder 실제 할당은 Execute에서
                _mainFrame = RTHandles.Alloc(
                    scaleFactor: Vector2.one,
                    filterMode: FilterMode.Bilinear,
                    colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                    useDynamicScale: true,
                    name: "_MainFrame"
                );

                // 기본값
                _downsampleFactor = Mathf.Clamp01(downsampleFactor);
                _applyEveryNFrame = Mathf.Max(1, applyEveryNFrame);
            }
            catch (NullReferenceException)
            {
                // 볼륨 또는 셰이더가 없을 때
            }
        }

        public void Dispose()
        {
            // 리소스 정리
            CoreUtils.Destroy(_glitchMaterial);
            RTHandles.Release(_mainFrame);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // 현재 프레임 카운트 증가
            _frameCount++;

            bool isPostProcessEnabled = renderingData.cameraData.postProcessEnabled;
            bool isSceneViewCamera    = renderingData.cameraData.isSceneViewCamera;
            if (!IsActive || !isPostProcessEnabled || isSceneViewCamera)
                return;

            // Base 카메라만 처리 
            if (renderingData.cameraData.renderType != CameraRenderType.Base)
                return;

            // Reflection/Preview 카메라 스킵
            Camera cam = renderingData.cameraData.camera;
            if (cam == null) return;
            if (cam.cameraType == CameraType.Reflection || cam.cameraType == CameraType.Preview)
                return;

            // VR 멀티패스로
            bool isXrRendering   = renderingData.cameraData.xrRendering;
            bool isSinglePassXR  = false; // 기본값
            if (isXrRendering && renderingData.cameraData.xr != null)
            {
                // XRInfo가 있으면 singlePassEnabled 등으로 확인
                isSinglePassXR = renderingData.cameraData.xr.singlePassEnabled;
            }
            // isXrRendering == true && isSinglePassXR == false 

        
            float scanLineJitter = _volume.scanLineJitter.value;
            float verticalJump   = _volume.verticalJump.value;
            float horizontalShake= _volume.horizontalShake.value;
            float colorDrift     = _volume.colorDrift.value;

            bool isEffectivelyZero =
                Mathf.Abs(scanLineJitter)  < 0.0001f &&
                Mathf.Abs(verticalJump)    < 0.0001f &&
                Mathf.Abs(horizontalShake) < 0.0001f &&
                Mathf.Abs(colorDrift)      < 0.0001f;

            if (isEffectivelyZero)
                return;

         
            _fpsTimer += Time.unscaledDeltaTime;
            _fpsFrameCount++;
            if (_fpsTimer >= 1f)
            {
                _avgFps = _fpsFrameCount / _fpsTimer;
                _fpsFrameCount = 0;
                _fpsTimer = 0f;

                // FPS 낮으면 다운샘플/프레임스킵 강화
                if (_avgFps < 15f)
                {
                    _downsampleFactor = 0.25f;
                    _applyEveryNFrame = 4;
                }
                else if (_avgFps < 30f)
                {
                    _downsampleFactor = 0.5f;
                    _applyEveryNFrame = 2;
                }
                else
                {
                    _downsampleFactor = 1f;
                    _applyEveryNFrame = 1;
                }
            }

          
            if (_frameCount % _applyEveryNFrame != 0)
                return;

           
            CommandBuffer cmd = CommandBufferPool.Get(RenderPassName);
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                // 카메라 타겟
                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                var desc   = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;

                //  다운샘플링
                if (_downsampleFactor < 1f)
                {
                    int w = Mathf.Max(1, (int)(desc.width  * _downsampleFactor));
                    int h = Mathf.Max(1, (int)(desc.height * _downsampleFactor));
                    desc.width  = w;
                    desc.height = h;
                }

                // 임시 RT 생성
                cmd.GetTemporaryRT(_mainFrameID, desc);

                // 셰이더 파라미터 설정
                _verticalJumpTime += Time.deltaTime * verticalJump * 11.3f;

                // ScanLineJitter
                float slThresh = Mathf.Clamp01(1f - scanLineJitter * 1.2f);
                float slDisp   = 0.002f + Mathf.Pow(scanLineJitter, 3) * 0.05f;
                _glitchMaterial.SetVector(ScanLineJitterID, new Vector2(slDisp, slThresh));

                // VerticalJump
                var vj = new Vector2(verticalJump, _verticalJumpTime);
                _glitchMaterial.SetVector(VerticalJumpID, vj);

                // HorizontalShake
                _glitchMaterial.SetFloat(HorizontalShakeID, horizontalShake * 0.2f);

                // ColorDrift
                var cd = new Vector2(colorDrift * 0.04f, Time.time * 606.11f);
                _glitchMaterial.SetVector(ColorDriftID, cd);

                cmd.Blit(source, _mainFrame, _glitchMaterial);

    
                cmd.Blit(_mainFrame, source);

                cmd.ReleaseTemporaryRT(_mainFrameID);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
