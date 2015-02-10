#include <cassert>
#include "math.hpp"
#include "Noise.h"

static unsigned int g_uiSeed;

// ************************************************************************* //
/// \brief Sets a global value for the current noise function.
///
void SetSeed( unsigned int _uiSeed )
{
	g_uiSeed = _uiSeed;
}


// ************************************************************************* //
double Sample1D(int64_t _i)
{
	_i += g_uiSeed;
	_i ^= (_i<<13);
	return (((_i * (_i * _i * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647.0);
}

double Sample2D(int64_t _x, int64_t _y)
{
	return Sample1D((_x*57) ^ (_y*101) ^ (_x*_y*17));
}

// ************************************************************************* //
// VALUE NOISE
// ************************************************************************* //

// Function for interpolation between two points of the noise
inline float InterpolationPolynom(float _dR)
{
	return 0.5f-cos(_dR*3.14159f)*0.5f;
	//return _dR;//_dR*_dR*_dR*(_dR*(_dR*6.0f-15.0f)+10.0f);
}

inline float Derivative(float _dR)
{
	return sin(_dR*3.14159f) * 3.14159f;
	//_dR*_dR*(_dR*(_dR-2.0f)+1.0f)*30.0f;
}

inline void IntFrac(float _f, int& _iInt, float& _fFrac)	{_iInt = Floor(_f); _fFrac = _f-_iInt;}




// ************************************************************************* //
float Rand2D(float _fX, float _fY, float _fFrequence, float& _fOutGradX, float& _fOutGradY)
{
	// We need 2 samples per dimension -> 4 samples total
	float fFracX, fFracY;
	int iX0, iY0;
	IntFrac(_fX*_fFrequence, iX0, fFracX);
	IntFrac(_fY*_fFrequence, iY0, fFracY);

	float s00 = (float)Sample2D(iX0  , iY0  );
	float s10 = (float)Sample2D(iX0+1, iY0  );
	float s01 = (float)Sample2D(iX0  , iY0+1);
	float s11 = (float)Sample2D(iX0+1, iY0+1);

	float u = InterpolationPolynom(fFracX);
	float v = InterpolationPolynom(fFracY);
	float du = Derivative(fFracX);
	float dv = Derivative(fFracY);

	const float k0 = s00;
    const float k1 = s10 - s00;
    const float k2 = s01 - s00;
    const float k4 = s00 - s01 - s10 + s11;

    _fOutGradX = du * (k1 + k4*v);
    _fOutGradY = dv * (k2 + k4*u);
    return k0 + k1*u + k2*v + k4*u*v;
}

float Rand2DHermite(float _fX, float _fY, float _fFrequence, float& _fOutGradX, float& _fOutGradY)
{
	// We need 2 samples per dimension -> 4 samples total
	float u, v;
	int iX0, iY0;
	IntFrac(_fX*_fFrequence, iX0, u);
	IntFrac(_fY*_fFrequence, iY0, v);

	float s00 = (float)Sample2D(iX0  , iY0  );
	float s10 = (float)Sample2D(iX0+1, iY0  );
	float s20 = (float)Sample2D(iX0+2, iY0  );
	float s30 = (float)Sample2D(iX0+3, iY0  );

	float s01 = (float)Sample2D(iX0  , iY0+1);
	float s11 = (float)Sample2D(iX0+1, iY0+1);
	float s21 = (float)Sample2D(iX0+2, iY0+1);
	float s31 = (float)Sample2D(iX0+3, iY0+1);

	float s02 = (float)Sample2D(iX0  , iY0+2);
	float s12 = (float)Sample2D(iX0+1, iY0+2);
	float s22 = (float)Sample2D(iX0+2, iY0+2);
	float s32 = (float)Sample2D(iX0+3, iY0+2);

	float s03 = (float)Sample2D(iX0  , iY0+3);
	float s13 = (float)Sample2D(iX0+1, iY0+3);
	float s23 = (float)Sample2D(iX0+2, iY0+3);
	float s33 = (float)Sample2D(iX0+3, iY0+3);

	// the 1/2 is moved to the very out side as 1/2 * 1/2 = 0.25
	float hu0 = u*((2-u)*u-1);
	float hu1 = (u*u*(3*u-5)+2);
	float hu2 = u*((4-3*u)*u+1);
	float hu3 = (u-1)*u*u;
	float hv0 = v*((2-v)*v-1);
	float hv1 = (v*v*(3*v-5)+2);
	float hv2 = v*((4-3*v)*v+1);
	float hv3 = (v-1)*v*v;

	float px0 = s00*hu0 + s10*hu1 + s20*hu2 + s30*hu3;
	float px1 = s01*hu0 + s11*hu1 + s21*hu2 + s31*hu3;
	float px2 = s02*hu0 + s12*hu1 + s22*hu2 + s32*hu3;
	float px3 = s03*hu0 + s13*hu1 + s23*hu2 + s33*hu3;

	float py0 = s00*hv0 + s01*hv1 + s02*hv2 + s03*hv3;
	float py1 = s10*hv0 + s11*hv1 + s12*hv2 + s13*hv3;
	float py2 = s20*hv0 + s21*hv1 + s22*hv2 + s23*hv3;
	float py3 = s30*hv0 + s31*hv1 + s32*hv2 + s33*hv3;

	_fOutGradX = 0.25f * (py0*(u*(-3*u+4)-1) + py1*u*(9*u-10) + py2*((-9*u+8)*u+1) + py3*u*(3*u-2));
	_fOutGradY = 0.25f * (px0*(v*(-3*v+4)-1) + px1*v*(9*v-10) + px2*((-9*v+8)*v+1) + px3*v*(3*v-2));

	return 0.25f * (px0*hv0 + px1*hv1 + px2*hv2 + px3*hv3);
}


float Rand2D(int _iLowOctave, int _iHeightOctave, float _fPersistence, float _fX, float _fY, float& _fOutGradX, float& _fOutGradY)
{
	// Octaves cannot be smaller than zero and the height octave
	// must be larger than the low one.
	assert( _iLowOctave >= 0 && _iHeightOctave >= _iLowOctave );

	_fOutGradX = _fOutGradY = 0.0f;
	float fRes = 0.0f;
	float fAmplitude = 1.0f;
	float fFrequence = float(1<<_iLowOctave);
	for(int i=_iLowOctave; i<=_iHeightOctave; ++i)
	{
		float fGradX, fGradY;
		fRes += fAmplitude*Rand2DHermite(_fX, _fY, fFrequence, fGradX, fGradY );
		_fOutGradX += fFrequence*fAmplitude*fGradX;
		_fOutGradY += fFrequence*fAmplitude*fGradY;

		fAmplitude *= _fPersistence;
		fFrequence *= 2.0f;
	}

	// Transform to [-1,1]
	return fRes*2.0f*(1.0f-_fPersistence)/(1.0f-fAmplitude)-1.0f;
}
