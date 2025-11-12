Shader "Examples/Stencil"
{
    Properties
    {
        [IntRange(0,255)]
        _StencilID("Stencil ID", Range(0,255)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            // 컬러 블렌딩 설정: Blend Zero One => 결과적으로 색상 출력 없음
            Blend Zero One

            // 깊이 버퍼에 쓰지 않음
            ZWrite Off

            Stencil
            {
                // 스텐실 버퍼에 기록할 값
                Ref [_StencilID]

                // 항상 스텐실 비교 통과
                Comp Always

                // 통과 시(=비교 성공 시) 스텐실 버퍼를 Replace
                Pass Replace

                // 실패 시(=비교 실패 시) 아무 것도 하지 않음
                Fail Keep
            }
        }
    }
}
