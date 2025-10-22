#ifndef VIRTUALLENS2_COMMON_TARGET_DETECTOR_CGINC
#define VIRTUALLENS2_COMMON_TARGET_DETECTOR_CGINC

#include "UnityCG.cginc"
#include "Constants.cginc"

float _IsDesktopMode;

// VRChat Camera Mode
// - 0: Rendering normally
// - 1: Rendering in VR handheld camera
// - 2: Rendering in Desktop handheld camera
// - 3: Rendering for a screenshot
float _VRChatCameraMode;

// VRChat Mirror Mode
// - 0: Rendering normally, not in a mirror
// - 1: Rendering in a mirror viewed in VR
// - 2: Rendering in a mirror viewed in desktop mode
float _VRChatMirrorMode;

// Mirror
bool testInMirror(){
	return _VRChatMirrorMode != 0;
}

// Stereo (VR HMD)
bool testStereo(){
#if UNITY_SINGLE_PASS_STEREO
	return true;
#else
	return false;
#endif
}

// Perspective or orthogonal
bool testPerspective(){
	return unity_OrthoParams.w == 0.0;
}

// Field of view
bool testFieldOfView(float expect_deg){
	const float expect_m11 = 1.0f / tan(expect_deg * 0.5 * DEG2RAD);
	return abs(unity_CameraProjection._m11 - expect_m11) < 1e-4;
}

// VirtualLens2 capture camera
//   - Screen size = (see implementation)
//   - Perspective
//   - Monocular
//   - Not in mirror
bool isVirtualLensCaptureResolution(){
	return (_ScreenParams.x == 1024 && _ScreenParams.y ==  576)  // Avatar Depth
	    || (_ScreenParams.x == 2048 && _ScreenParams.y == 1152)  // 1080p
	    || (_ScreenParams.x == 2752 && _ScreenParams.y == 1536)  // 1440p
	    || (_ScreenParams.x == 4096 && _ScreenParams.y == 2304)  // 2160p
	    || (_ScreenParams.x == 8192 && _ScreenParams.y == 4608); // 4320p
}

bool isVirtualLensCaptureCamera(){
	const bool resolution = isVirtualLensCaptureResolution();
	return resolution                     // Screen width and height
	    && unity_OrthoParams.y == 1.3125  // Constant to distinguish from VRChat camera
	    && testPerspective()              // Perspective
	    && !testStereo()                  // Monocular
	    && !testInMirror();               // Not in mirror
}

// VirtualLens2 compute camera
//   - Screen size = (2048, 1152) or (4096, 2304)
//   - Orthogonal (size = 0.125)
//   - (near, far) = (0, 0.25)
//   - Monocular
//   - Not in mirror
bool isVirtualLensComputeCamera(){
	const bool resolution =
		   (_ScreenParams.x == 2048 && _ScreenParams.y == 1152)
		|| (_ScreenParams.x == 4096 && _ScreenParams.y == 2304);
	return resolution                    // Screen width and height
	    && !testPerspective()            // Orthogonal
	    && unity_OrthoParams.y == 0.125  // Orthographic camera's height
	    && _ProjectionParams.y == 0.0    // Near plane
	    && _ProjectionParams.z == 0.25   // Far plane
	    && !testStereo()                 // Monocular
	    && !testInMirror();              // Not in mirror
}

// VirtualLens2 compute camera (custom resolution)
//   - Orthogonal (size = 0.125)
//   - (near, far) = (0, 0.25)
//   - Monocular
//   - Not in mirror
bool isVirtualLensCustomComputeCamera(int width, int height){
	return _ScreenParams.x == width      // Screen width
	    && _ScreenParams.y == height     // Screen height
	    && !testPerspective()            // Orthogonal
	    && unity_OrthoParams.y == 0.125  // Orthographic camera's height
	    && _ProjectionParams.y == 0.0    // Near plane
	    && _ProjectionParams.z == 0.25   // Far plane
	    && !testStereo()                 // Monocular
	    && !testInMirror();              // Not in mirror
}

bool isVirtualLensCustomComputeCamera(int2 wh){
	return isVirtualLensCustomComputeCamera(wh.x, wh.y);
}

// VirtualLens2 precise touch detector camera
//   - Screen size = (1, 1)
//   - Orthogonal
//   - Monocular
//   - Not in mirror
bool isVirtualLensPreciseTouchDetectorCamera(){
	return _ScreenParams.x == 1  // Screen width
	    && _ScreenParams.y == 1  // Screen height
	    && !testPerspective()    // Orthogonal
	    && !testStereo()         // Monocular
	    && !testInMirror();      // Not in mirror
}

// VirtualLens2 state updater camera
//   - Screen size = (2, 1)
//   - Orthogonal (size = 0.125)
//   - (near, far) = (0, 0.25)
//   - Monocular
//   - Not in mirror
bool isVirtualLensStateUpdaterCamera(){
	return _ScreenParams.x == 4          // Screen width
	    && _ScreenParams.y == 1          // Screen height
	    && !testPerspective()            // Orthogonal
	    && unity_OrthoParams.y == 0.125  // Orthographic camera's height
	    && _ProjectionParams.y == 0.0    // Near plane
	    && _ProjectionParams.z == 0.25   // Far plane
	    && !testStereo()                 // Monocular
	    && !testInMirror();              // Not in mirror
}

// VRChat photo or streaming camera: field of view
bool testVRChatCameraMode(){
	return _VRChatCameraMode == 1  // Rendering in VR handheld camera
	    || _VRChatCameraMode == 2  // Rendering in Desktop handheld camera
	    || _VRChatCameraMode == 3; // Rendering for a screenshot
}

// VRChat camera (low resolution)
//   - Screen size: <= 1080p
//   - Perspective
//   - Monocular
//   - Not in mirror
bool isVRChatCamera(){
	if(isVirtualLensCaptureCamera()){ return false; }
	const bool resolution =
		(_ScreenParams.x <= 1920 && _ScreenParams.y <= 1080);
	const bool is_camera =
		   testVRChatCameraMode()
		|| (_IsDesktopMode != 0 && testFieldOfView(60.0));
	return resolution              // Screen size
		&& is_camera               // VRChat camera or desktop view
	    && testPerspective()       // Perspective
	    && !testStereo()           // Monocular
	    && !testInMirror();        // Not in mirror
}

// VRChat camera (high resolution)
//   - Screen size: > 1080p
//   - Perspective
//   - Monocular
//   - Not in mirror
bool isVRChatCameraHighResolution(){
	if(isVirtualLensCaptureCamera()){ return false; }
	const bool resolution =
		(_ScreenParams.x > 1920 || _ScreenParams.y > 1080);
	const bool is_camera =
		   testVRChatCameraMode()
		|| (_IsDesktopMode != 0 && testFieldOfView(60.0));
	return resolution               // Screen size
		&& is_camera                // VRChat camera or desktop view
	    && testPerspective()        // Perspective
	    && !testStereo()            // Monocular
	    && !testInMirror();         // Not in mirror
}

#endif
