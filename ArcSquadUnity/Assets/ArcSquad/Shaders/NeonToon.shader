Shader "ArcSquad/NeonToon"
{
 Properties {
  _Color ("Base Color", Color) = (1,1,1,1)
  _EmissionColor ("Neon", Color) = (0,0,0,1)
  _RimStrength ("Rim Light", Range(0,1)) = 0.3
 }
 SubShader {
  Tags {"RenderType"="Opaque" "Queue"="Geometry"}
  LOD 200
  Pass {
   Tags {"LightMode"="ForwardBase"}
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   #include "Lighting.cginc"
   struct a2v {float4 vertex:POSITION; float3 normal:NORMAL;};
   struct v2f {float4 pos:SV_POSITION;float3 n:TEXCOORD0;float3 w:TEXCOORD1;};
   float4 _Color;
   float4 _EmissionColor;
   float _RimStrength;
   v2f vert(a2v v) {
    v2f o;
    o.pos=UnityObjectToClipPos(v.vertex);
    o.w=mul(unity_ObjectToWorld,v.vertex).xyz;
    o.n=UnityObjectToWorldNormal(v.normal);
    return o;
   }
   fixed4 frag(v2f i):SV_Target {
    float3 normal=normalize(i.n);
    float3 lightDir=normalize(_WorldSpaceLightPos0.xyz);
    float ndotl=saturate(dot(normal,lightDir));
    float band=ndotl>.72?1:ndotl>.35?.74:.43;
    float view=saturate(dot(normal,normalize(_WorldSpaceCameraPos-i.w)));
    float rim=pow(1-view,3)*_RimStrength;
    float3 lit=_Color.rgb * (0.2 + band*.85*_LightColor0.rgb);
    return fixed4(lit+_EmissionColor.rgb+rim*.35,1);
   }
   ENDCG
  }
 }
 FallBack "Diffuse"
}