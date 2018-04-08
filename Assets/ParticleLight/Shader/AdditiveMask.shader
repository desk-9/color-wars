Shader "Custom/AdditiveMask"
{
Properties
{
_Color ("Main Color", Color) = (1,1,1,1)
_MainTex ("Base (RGB)", 2D) = "white" {}
_Mask ("Culling Mask", 2D) = "white" {}
_Cutoff ("Alpha cutoff", Range (.01,1)) = 0.1
}
SubShader
{

//Alphatest Greater 0
Tags {"Queue"="Transparent"}
Lighting off
Blend SrcAlpha OneMinusSrcAlpha
AlphaTest GEqual [_Cutoff]
Color [_Color]
Cull off
ZWrite off


Pass
{
SetTexture [_Mask] {
constantColor [_Color]
combine texture * constant
}
SetTexture [_MainTex] {
combine texture * previous DOUBLE
}

  }
 }
 
 FallBack "VertexLit"
}