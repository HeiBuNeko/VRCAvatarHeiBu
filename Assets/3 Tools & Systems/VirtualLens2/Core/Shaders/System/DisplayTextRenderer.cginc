#ifndef VIRTUALLENS2_SYSTEM_DISPLAY_TEXT_RENDERER_CGINC
#define VIRTUALLENS2_SYSTEM_DISPLAY_TEXT_RENDERER_CGINC

#include "UnityCG.cginc"
#include "../Common/Constants.cginc"
#include "../Common/TargetDetector.cginc"
#include "../Common/FieldOfView.cginc"


//---------------------------------------------------------------------------
// Parameter declarations
//---------------------------------------------------------------------------

Texture2D<float4> _ComponentTex;  // Components texture

float _ShowInfo;     // Display parameters
float _Resolution;   // Internal resolution
float _AFMode;       // Auto focus mode
float _FarOverride;  // Far plane override mode
float _DepthEnabler; // Depth enabler status

float _FieldOfView;      // Raw field of view [deg]
float _LogFNumber;       // log(F)
float _LogFocusDistance; // log(Manual focus distance)
float _BlurringThresh;   // Maximum F-number (disable DoF simulation when _LogFNumber == _BlurringThresh)
float _FocusingThresh;   // Minimum focus distance for MF
float _Exposure;         // Exposure value

SamplerState point_clamp_sampler;
SamplerState linear_clamp_sampler;


//---------------------------------------------------------------------------
// Enumerates and constants
//---------------------------------------------------------------------------

static const int DISPLAY_WIDTH  = 1280;
static const int DISPLAY_HEIGHT =  720;


//---------------------------------------------------------------------------
// Structure definitions
//---------------------------------------------------------------------------

struct appdata {
	float4 vertex : POSITION;
	float2 uv     : TEXCOORD0;
};

struct v2f {
	float4 vertex : SV_POSITION;
	float2 uv     : TEXCOORD0;
};


//---------------------------------------------------------------------------
// Vertex shader
//---------------------------------------------------------------------------

v2f vertex(appdata v){
	v2f o;
	o.vertex = 0;
	o.uv     = v.uv;
	return o;
}


//---------------------------------------------------------------------------
// Geometry shader
//---------------------------------------------------------------------------

// Utility: draw a rectangle
void drawRectangle(
	inout TriangleStream<v2f> output,
	float2 p0, float2 p1,
	float2 uv0, float2 uv1)
{
	const float2 scale  = 2.0 / float2(DISPLAY_WIDTH, DISPLAY_HEIGHT);
	const float2 offset = float2(1.0, 1.0);
	p0 = (p0 * scale - offset) * float2(1.0, _ProjectionParams.x);
	p1 = (p1 * scale - offset) * float2(1.0, _ProjectionParams.x);
	v2f v0, v1, v2, v3;
	v0.vertex = float4(p0.x, p0.y, 0, 1);
	v1.vertex = float4(p0.x, p1.y, 0, 1);
	v2.vertex = float4(p1.x, p0.y, 0, 1);
	v3.vertex = float4(p1.x, p1.y, 0, 1);
	v0.uv = float2(uv0.x, uv0.y);
	v1.uv = float2(uv0.x, uv1.y);
	v2.uv = float2(uv1.x, uv0.y);
	v3.uv = float2(uv1.x, uv1.y);
	output.Append(v0);
	output.Append(v1);
	output.Append(v2);
	output.Append(v3);
	output.RestartStrip();
}

// Utility: draw a character
void drawCharacter(
	inout TriangleStream<v2f> output,
	float2 p0, float2 p1, uint c)
{
	const uint row = c >> 4;
	const uint col = c & 15;
	const float2 uv0 = float2((col + 0) * 32.0 / 512.0, (row + 0) * 64.0 / 512.0);
	const float2 uv1 = float2((col + 1) * 32.0 / 512.0, (row + 1) * 64.0 / 512.0);
	drawRectangle(output, p0, p1, uv0, uv1);
}

void drawIcon(
	inout TriangleStream<v2f> output,
	float2 p0, float2 p1, uint c)
{
	const uint row = c >> 3;
	const uint col = c &  7;
	const float2 uv0 = float2((col + 0) * 64.0 / 512.0, (row + 0) * 64.0 / 512.0);
	const float2 uv1 = float2((col + 1) * 64.0 / 512.0, (row + 1) * 64.0 / 512.0);
	drawRectangle(output, p0, p1, uv0, uv1);
}

// Draw focal length
void drawFocalLength(inout TriangleStream<v2f> output, float fov){
	const uint focal = (uint)round(computeFocalLength(fov) * 1e3);
	const uint c0 = (focal >= 100 ? focal / 100 % 10 : 15);
	const uint c1 = (focal >=  10 ? focal /  10 % 10 : 15);
	const uint c2 = (focal % 10);
	const uint c3 = 14;  // 'm'
	const uint c4 = 14;  // 'm'
	drawIcon     (output, float2(32 + 32 * 0, 8), float2(32 + 32 * 2, 72), 12);
	drawCharacter(output, float2(32 + 32 * 2, 8), float2(32 + 32 * 3, 72), c0);
	drawCharacter(output, float2(32 + 32 * 3, 8), float2(32 + 32 * 4, 72), c1);
	drawCharacter(output, float2(32 + 32 * 4, 8), float2(32 + 32 * 5, 72), c2);
	drawCharacter(output, float2(32 + 32 * 5, 8), float2(32 + 32 * 6, 72), c3);
	drawCharacter(output, float2(32 + 32 * 6, 8), float2(32 + 32 * 7, 72), c4);
}

// Draw F number
void drawFNumber(inout TriangleStream<v2f> output, float log_f){
	uint c0, c1, c2, c3, c4;
	if(log_f == _BlurringThresh){
		c0 = 13;  // 'F'
		c1 = 30;  // infinity.left
		c2 = 31;  // infinity.right
		c3 = 15;  // ' '
		c4 = 15;  // ' '
	}else{
		const uint F = (uint)round(exp(log_f) * 10);
		c0 = 13;  // 'F'
		c1 = (F >= 100 ? F / 100 % 10 : 15);
		c2 = (F / 10 % 10);
		c3 = 10;  // '.'
		c4 = (F % 10);
	}
	drawIcon     (output, float2(1024 + 32 * 0, 8), float2(1024 + 32 * 2, 72), 13);
	drawCharacter(output, float2(1024 + 32 * 2, 8), float2(1024 + 32 * 3, 72), c0);
	drawCharacter(output, float2(1024 + 32 * 3, 8), float2(1024 + 32 * 4, 72), c1);
	drawCharacter(output, float2(1024 + 32 * 4, 8), float2(1024 + 32 * 5, 72), c2);
	drawCharacter(output, float2(1024 + 32 * 5, 8), float2(1024 + 32 * 6, 72), c3);
	drawCharacter(output, float2(1024 + 32 * 6, 8), float2(1024 + 32 * 7, 72), c4);
}

// Draw Exposure
void drawExposure(inout TriangleStream<v2f> output, float exposure){
	// 1/log(2)
	const int ev = (int)round(exposure * 1.442695 * 1e2);
	if(ev != 0){
		const uint aev = abs(ev);
		const uint c0 = (ev >= 0 ? 11 : 12);
		const uint c1 = (aev / 100 % 10);
		const uint c2 = 10;  // '.'
		const uint c3 = (aev / 10 % 10);
		const uint c4 = (aev % 10);
		drawIcon     (output, float2(528 + 32 * 0, 8), float2(528 + 32 * 2, 72), 14);
		drawCharacter(output, float2(528 + 32 * 2, 8), float2(528 + 32 * 3, 72), c0);
		drawCharacter(output, float2(528 + 32 * 3, 8), float2(528 + 32 * 4, 72), c1);
		drawCharacter(output, float2(528 + 32 * 4, 8), float2(528 + 32 * 5, 72), c2);
		drawCharacter(output, float2(528 + 32 * 5, 8), float2(528 + 32 * 6, 72), c3);
		drawCharacter(output, float2(528 + 32 * 6, 8), float2(528 + 32 * 7, 72), c4);
	}
}

// Draw Icons
void drawResolutionIcon(inout TriangleStream<v2f> output){
	const uint mode = (uint)round(_Resolution);
	drawIcon(output, float2(1216, 656), float2(1280, 720), 8 + mode);
}

void drawAFModeIcon(inout TriangleStream<v2f> output){
	const uint raw = (uint)round(_AFMode);
	uint mode = 3;
	if(_LogFocusDistance > _FocusingThresh){
		mode = 2; // MF
	}else if(raw == 1){
		mode = 0; // Face AF
	}else if(raw == 2){
		mode = 1; // Selfie AF
	}
	if(mode < 3){
		drawIcon(output, float2(1216, 592), float2(1280, 656), 16 + mode);
	}
}

void drawFarOverrideIcon(inout TriangleStream<v2f> output){
	const uint mode = (uint)round(_FarOverride);
	if(mode > 0){
		drawIcon(output, float2(1216, 528), float2(1280, 592), 19);
	}
}

void drawDepthEnablerIcon(inout TriangleStream<v2f> output){
	const uint mode = (uint)round(_DepthEnabler);
	if(mode == 0){
		drawIcon(output, float2(1216, 464), float2(1280, 528), 20);
	}
}

// Entry point
[maxvertexcount(48)]
void geometry(
	triangle v2f input[3],
	in uint id : SV_PrimitiveID,
	inout TriangleStream<v2f> output)
{
	const int show_info = (int)round(_ShowInfo);
	if(!show_info || !isVirtualLensCustomComputeCamera(DISPLAY_WIDTH, DISPLAY_HEIGHT)){ return; }
	if(id == 0){
		drawFocalLength(output, _FieldOfView);
		drawFNumber(output, _LogFNumber);
	}else if(id == 1){
		drawExposure(output, _Exposure);
		drawResolutionIcon(output);
		drawAFModeIcon(output);
		drawFarOverrideIcon(output);
		drawDepthEnablerIcon(output);
	}
}


//---------------------------------------------------------------------------
// Fragment shader
//---------------------------------------------------------------------------

float4 fragment(v2f i) : SV_Target {
	return _ComponentTex.Sample(linear_clamp_sampler, i.uv);
}

#endif
