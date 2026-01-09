// The main Porter-Duff alpha compositing modes

// The source pixels are drawn over the destination pixels.
float4 AlphaComp_Over(float4 src, float4 dst)
{
	float alpha = src.a + (1.0 - src.a) * dst.a;
	float3 color = src.rgb + (1.0 - src.a) * dst.rgb;
	return float4(color, alpha);
}

// Keeps the source pixels that cover the destination pixels, discards the remaining source and destination pixels.
float4 AlphaComp_In(float4 src, float4 dst)
{
	float alpha = src.a * dst.a;
	float3 color = src.rgb * dst.a;
	return float4(color, alpha);
}

// Keeps the source pixels that do not cover destination pixels.
float4 AlphaComp_Out(float4 src, float4 dst)
{
	float alpha = (1.0 - dst.a) * src.a;
	float3 color = (1.0 - dst.a) * src.rgb;
	return float4(color, alpha);
}

// Discards the source pixels that do not cover destination pixels.
float4 AlphaComp_ATop(float4 src, float4 dst)
{
	float alpha = dst.a;
	float3 color = dst.a * src.rgb + (1.0 - src.a) * dst.rgb;
	return float4(color, alpha);
}

// Discards the source and destination pixels where source pixels cover destination pixels.
// Useful for outlines
float4 AlphaComp_Xor(float4 src, float4 dst)
{
	float alpha = (1.0 - dst.a) * src.a + (1.0 - src.a) * dst.a;
	float3 color = (1.0 - dst.a) * src.rgb + (1.0 - src.a) * dst.rgb;
	return float4(color, alpha);
}