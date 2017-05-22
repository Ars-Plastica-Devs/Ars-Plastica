Shader "Ars Plastica/APShaderTwoSided" {
Properties {
	_Color("Color", Color) = (1,1,1,1)
	_MainTex("Albedo (RGBA)", 2D) = "white" {}
	_Cutoff("Alpha cutoff", Range(0,1)) = 0.5
	_IllumMap("Illum Map (RGBA)", 2D) = "white" {}
	_NormalMap("Normal Map", 2D) = "bump" {}
}
SubShader{
	Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
	LOD 200
	Cull off
	CGPROGRAM
	#pragma surface surf Lambert alpha:fade
	//#pragma target 3.0 // Use shader model 3.0 target, to get nicer looking lighting

	sampler2D _MainTex;	
	sampler2D _IllumMap;
	sampler2D _NormalMap;

	fixed4 _Color;
	float _Cutoff;

	struct Input {
		float2 uv_MainTex;
	};

	void surf(Input IN, inout SurfaceOutput o) {
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
		o.Albedo = c.rgb;

		if (c.a > _Cutoff)
			o.Alpha = c.a;
		else
			o.Alpha = 0;

		o.Emission = c * tex2D(_IllumMap, IN.uv_MainTex);
		o.Normal = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));
	}
	ENDCG
}
FallBack "Diffuse"
}
