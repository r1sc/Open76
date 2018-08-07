Shader "Unlit/CarMirror"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_MaskTex("Mask Texture", 2D) = "black" {}
		_MaskColor("Mask Color", Color) = (0, 0, 0, 0)
	}
	SubShader
	{
		Blend SrcAlpha OneMinusSrcAlpha
		
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			uniform sampler2D _MainTex;
			uniform sampler2D _MaskTex;
			uniform float4 _MaskColor;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				fixed4 maskCol = tex2D(_MaskTex, i.uv);

				float3 delta = maskCol.rgb - _MaskColor.rgb;
				if(dot(delta,delta) < 0.5)
				{
					col.a = 0.0;
				}

				return col;
			}
			ENDCG
		}
	}
}