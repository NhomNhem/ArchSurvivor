// Retains the largest component of the source and destination pixel.
float4 Blend_Lighten(float4 src, float4 dst)
{
	return max(src, dst);
	//float alpha = src.a + dst.a - src.a * dst.a;
	//float3 color = (1.0 - dst.a) * src.rgb + (1.0 + dst.a) * dst.rgb + max(src.rgb, dst.rgb);
	//return float4(color, alpha);
}	

// Retains the smallest component of the source and destination pixels.
float4 Blend_Darken(float4 src, float4 dst)
{
	return min(src, dst);
	float alpha = src.a + dst.a - src.a * dst.a;
	float3 color = (1.0 - dst.a) * src.rgb + (1.0 - src.a) * dst.rgb + min(src.rgb, dst.rgb);
	return float4(color, alpha);
}

// Multiplies the source and destination pixels.
float4 Blend_Multiply(float4 src, float4 dst)
{
	return src * dst;
}

// Multiplies or screens the source and destination depending on the destination color.
float4 Blend_Overlay(float4 src, float4 dst)
{
	float alpha = src.a + dst.a - src.a * dst.a;
	float3 colorA = 2.0 * src.rgb * dst.rgb;
	float3 colorB = src.aaa * dst.aaa - (2.0 * (dst.aaa - src.rgb) * (src.aaa - dst.rgb));
	float3 color = ((2.0 * dst.rgb) < dst.aaa) ? colorA : colorB;
	return float4(color, alpha);
}	

// Adds the source and destination pixels, then subtracts the source pixels multiplied by the destination.
// Useful for additive glow effects
float4 Blend_Screen(float4 src, float4 dst)
{
	float alpha = src.a + dst.a - src.a * dst.a;
	float3 color = src.rgb + dst.rgb - src.rgb * dst.rgb;
	return float4(color, alpha);
}	

// Adds the source pixels to the destination pixels and saturates the result.
// Useful for additive glow effects
float4 Blend_Add(float4 src, float4 dst)
{
	float alpha = saturate(src.a + dst.a);
	float3 color = saturate(src.rgb + dst.rgb);
	return float4(color, alpha);
}

float4 blend(float4 src, float4 dst)
{
	float4 result;
	result.a = src.a + dst.a * (1.0 - src.a);
	result.rgb = src.rgb * src.a + dst.rgb * dst.a * (1.0 - src.a);
	return result;
}

float4 GammaToLinearSpace_Premul(float4 color)
{
	if (color.a > 0) color.rgb /= color.a;
	color.rgb = GammaToLinearSpace(color.rgb);
	color.rgb *= color.a;
	return color;
}

float4 LinearToGammaSpace_Premul(float4 color)
{
	if (color.a > 0.0) color.rgb /= color.a;
	color.rgb = LinearToGammaSpace(color.rgb);
	color.rgb *= color.a;
	return color;
}

float4 ToStraight(float4 color)
{
	if (color.a > 0.0)
	{
		color.rgb /= color.a;
	}
	return color;
}

float4 ToPremultiplied(float4 color)
{
	color.rgb *= color.a;
	return color;
}

float4 StraightGammaToPremultipliedLinear(float4 color)
{
	color.rgb = GammaToLinearSpace(color.rgb);
	color.rgb *= color.a;
	return color;
}

float3 OkLabToLinear(float3 c)
{
	float l_ = c.x + 0.3963377774 * c.y + 0.2158037573 * c.z;
	float m_ = c.x - 0.1055613458 * c.y - 0.0638541728 * c.z;
	float s_ = c.x - 0.0894841775 * c.y - 1.2914855480 * c.z;

	float l = l_ * l_ * l_;
	float m = m_ * m_ * m_;
	float s = s_ * s_ * s_;

	return float3(
		+4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
		-1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
		-0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s
	);
}

half4 CornerGradient(half4 colorTL, half4 colorTR, half4 colorBL, half4 colorBR, half2 uv, float scale)
{
	// Expand/shrink the uv edges
	uv = saturate((uv - 0.5) * scale + 0.5);
	return lerp(lerp(colorBL, colorBR, uv.x), lerp(colorTL, colorTR, uv.x), uv.y);
}

half4 EdgeGradient(half4 colorA, half4 colorB, half uv, float scale, float bias)
{
	// Expand/shrink the uv edges
	uv = saturate((uv -  0.5) * scale + bias);
	return lerp(colorA, colorB, uv);
}