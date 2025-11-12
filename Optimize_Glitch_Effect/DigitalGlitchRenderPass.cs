using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering; // GraphicsFormat 관련

namespace URPGlitch.Runtime.DigitalGlitch
{
    sealed class DigitalGlitchRenderPass : ScriptableRenderPass, IDisposable
    {
        const string RenderPassName = "DigitalGlitch RenderPass (Optimized VR Multipass)";

        // 셰이더/텍스처 프로퍼티 ID
        static readonly int MainTexID   = Shader.PropertyToID("_MainTex");
        static readonly int NoiseTexID  = Shader.PropertyToID("_NoiseTex");
        static readonly int TrashTexID  = Shader.PropertyToID("_TrashTex");
        static readonly int IntensityID = Shader.PropertyToID("_Intensity");

        readonly ProfilingSampler _profilingSampler;
        readonly System.Random _random;

        // 글리치 셰이더/머티리얼, 노이즈 텍스처, 볼륨
        readonly Material _glitchMaterial;
        readonly Texture2D _noiseTexture;
        readonly DigitalGlitchVolume _volume;

        // 임시 RT (화면, 트래시 프레임들)
        RTHandle _mainFrame;
        RTHandle _trashFrame1;
        RTHandle _trashFrame2;

        // RT 식별자
        int _mainFrameID;
        int _trashFrame1ID;
        int _trashFrame2ID;

        // 내부 상태
        int _frameCount;

        // Downsampling 비율 (0.5=절반, 1.0=원본 등)
        float _downsampleFactor;

        // N프레임마다 1회만 글리치 적용
        int _applyEveryNFrame;

        // FPS 측정(예시용)
        float _fpsTimer;
        int   _fpsFrameCounter;
        float _avgFps;

        // 활성화 여부
        bool IsActive =>
            _glitchMaterial != null &&
            _volume != null &&
            _volume.IsActive;

    
        public DigitalGlitchRenderPass(Shader shader, float downsampleFactor = 0.1f, int applyEveryNFrame = 10)
        {
            try
            {
                // 이 패스가 실행될 시점
                renderPassEvent   = RenderPassEvent.AfterRenderingPostProcessing;
                _profilingSampler = new ProfilingSampler(RenderPassName);
                _random           = new System.Random();

                // 글리치 셰이더 머티리얼 생성
                _glitchMaterial   = CoreUtils.CreateEngineMaterial(shader);

                // 노이즈 텍스처 생성
                _noiseTexture = new Texture2D(64, 32, TextureFormat.ARGB32, false)
                {
                    hideFlags  = HideFlags.DontSave,
                    wrapMode   = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };

                // 볼륨 스택에서 DigitalGlitchVolume 가져오기
                var volumeStack = VolumeManager.instance.stack;
                _volume = volumeStack.GetComponent<DigitalGlitchVolume>();

                // RT ID
                _mainFrameID   = Shader.PropertyToID("_MainFrame");
                _trashFrame1ID = Shader.PropertyToID("_TrashFrame1");
                _trashFrame2ID = Shader.PropertyToID("_TrashFrame2");

                // 임시 RTHandle
                _mainFrame = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Bilinear,
                    colorFormat: GraphicsFormat.R8G8B8A8_UNorm, useDynamicScale: true, name: "_MainFrame");
                _trashFrame1 = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Bilinear,
                    colorFormat: GraphicsFormat.R8G8B8A8_UNorm, useDynamicScale: true, name: "_TrashFrame1");
                _trashFrame2 = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Bilinear,
                    colorFormat: GraphicsFormat.R8G8B8A8_UNorm, useDynamicScale: true, name: "_TrashFrame2");

                // 한번 초기 노이즈 생성
                UpdateNoiseTexture();

                // 초기 세팅
                _downsampleFactor  = Mathf.Clamp01(downsampleFactor);
                _applyEveryNFrame  = Mathf.Max(1, applyEveryNFrame);
            }
            catch (NullReferenceException)
            {
                // 볼륨 컴포넌트나 셰이더가 누락된 경우
            }
        }

        public void Dispose()
        {
            // 머티리얼/텍스처 해제
            CoreUtils.Destroy(_glitchMaterial);
            CoreUtils.Destroy(_noiseTexture);

            // RTHandle 해제
            RTHandles.Release(_mainFrame);
            RTHandles.Release(_trashFrame1);
            RTHandles.Release(_trashFrame2);
        }


        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (!IsActive) return;

            float intensity = Mathf.Clamp01(_volume.intensity.value);
            if (intensity < 0.0001f)
                return; // 의미 없으므로 스킵

            // intensity에 따라 노이즈 갱신 
            float threshold = Mathf.Lerp(0.9f, 0.5f, intensity);
            float r = (float)_random.NextDouble();
            if (r > threshold)
            {
                UpdateNoiseTexture();
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            _frameCount++;

            bool isPostProcessEnabled = renderingData.cameraData.postProcessEnabled;
            bool isSceneViewCamera    = renderingData.cameraData.isSceneViewCamera;
            if (!IsActive || !isPostProcessEnabled || isSceneViewCamera)
                return;

            // Reflection/Preview 카메라, Overlay 카메라도 스킵
            Camera cam = renderingData.cameraData.camera;
            if (cam == null) return;

            if (cam.cameraType == CameraType.Reflection || cam.cameraType == CameraType.Preview)
                return;

            if (renderingData.cameraData.renderType != CameraRenderType.Base)
                return;

            // VR 멀티패스인지 확인
            bool isXrRendering   = renderingData.cameraData.xrRendering;
           

            float intensity = Mathf.Clamp01(_volume.intensity.value);
            if (intensity < 0.0001f)
                return; // 효과 0


            _fpsTimer += Time.unscaledDeltaTime;
            _fpsFrameCounter++;
            if (_fpsTimer >= 1f) // 매 1초마다
            {
                _avgFps = _fpsFrameCounter / _fpsTimer;
                _fpsFrameCounter = 0;
                _fpsTimer = 0f;

                // FPS 낮으면 다운샘플+프레임스킵 강화
                if (_avgFps < 20f)
                {
                    _downsampleFactor = 0.5f;
                    _applyEveryNFrame = 2;
                }
                else
                {
                    _downsampleFactor = 1.0f;
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
                var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                cameraTargetDescriptor.depthBufferBits = 0;

                // 다운샘플링 적용
                if (_downsampleFactor < 1.0f)
                {
                    int w = Mathf.Max(1, (int)(cameraTargetDescriptor.width  * _downsampleFactor));
                    int h = Mathf.Max(1, (int)(cameraTargetDescriptor.height * _downsampleFactor));
                    cameraTargetDescriptor.width  = w;
                    cameraTargetDescriptor.height = h;
                }

                // 임시 RT 할당
                cmd.GetTemporaryRT(_mainFrameID,   cameraTargetDescriptor);
                cmd.GetTemporaryRT(_trashFrame1ID, cameraTargetDescriptor);
                cmd.GetTemporaryRT(_trashFrame2ID, cameraTargetDescriptor);

                // 1) 소스 _mainFrame 복사
                cmd.Blit(source, _mainFrame);

                // 2) Trash 프레임 갱신 랜덤/주기적으로
                int frameCountGlobal = Time.frameCount; 
                // Unity 전체 프레임 카운트 기준으로 주기 잡기
                if (intensity > 0.01f) // 너무 낮으면 굳이 갱신 안 함
                {
                    if (frameCountGlobal % 13 == 0)
                        cmd.Blit(source, _trashFrame1);
                    if (frameCountGlobal % 73 == 0)
                        cmd.Blit(source, _trashFrame2);
                }

                float r = (float)_random.NextDouble();
                var chosenTrash = (r > 0.5f) ? _trashFrame1 : _trashFrame2;

                // 글리치 셰이더 파라미터
                cmd.SetGlobalFloat(IntensityID, intensity);
                cmd.SetGlobalTexture(NoiseTexID, _noiseTexture);
                cmd.SetGlobalTexture(MainTexID,  _mainFrame);
                cmd.SetGlobalTexture(TrashTexID, chosenTrash);

                // _mainFrame → source (글리치 머티리얼 적용)
                cmd.Blit(_mainFrame, source, _glitchMaterial);

                // 임시 RT 해제
                cmd.ReleaseTemporaryRT(_mainFrameID);
                cmd.ReleaseTemporaryRT(_trashFrame1ID);
                cmd.ReleaseTemporaryRT(_trashFrame2ID);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void UpdateNoiseTexture()
        {
            int w = _noiseTexture.width;
            int h = _noiseTexture.height;
            Color c = RandomColor;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float r = (float)_random.NextDouble();
                    // 랜덤하게 색깔 튐
                    if (r > 0.89f)
                    {
                        c = RandomColor;
                    }
                    _noiseTexture.SetPixel(x, y, c);
                }
            }
            _noiseTexture.Apply();
        }


        Color RandomColor
        {
            get
            {
                float r = (float)_random.NextDouble();
                float g = (float)_random.NextDouble();
                float b = (float)_random.NextDouble();
                float a = (float)_random.NextDouble();
                return new Color(r, g, b, a);
            }
        }
    }
}
