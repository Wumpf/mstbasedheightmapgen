#include <cassert>
#include "math.hpp"


// ******************************************************************************** //
static double Sample1D(__int64 _i)
{
	_i ^= (_i<<13);
	return (((_i * (_i * _i * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647.0);
}

// ******************************************************************** //
// PERLIN NOISE
// ******************************************************************** //

// Function for interpolation between two points of the noise
inline float InterpolationPolynom(float _dR)
{
	// Polynomial function to get detailed information see "Burger-GradientNoiseGerman-2008".
	return _dR*_dR*_dR*(_dR*(_dR*6.0f-15.0f)+10.0f);
}

inline float Derivative(float _dR)
{
	return _dR*_dR*(_dR*(_dR-2.0f)+1.0f)*30.0f;
}

inline void IntFrac(float _f, int& _iInt, float& _fFrac)	{_iInt = Floor(_f); _fFrac = _f-_iInt;}




// ******************************************************************** //
static float Rand2D(float _fX, float _fY, float _fFrequence, unsigned int _uiSeed, float& _fOutGradX, float& _fOutGradY)
{
	// We need 2 samples per dimension -> 4 samples total
	float fFracX, fFracY;
	int iX0, iY0;
	IntFrac(_fX*_fFrequence, iX0, fFracX);
	IntFrac(_fY*_fFrequence, iY0, fFracY);
	int iX1 = (iX0+1)*57; iX0*=57;
	int iY1 = (iY0+1)*101; iY0*=101;

	float s00 = (float)Sample1D(iX0 + iY0 + _uiSeed);
	float s10 = (float)Sample1D(iX1 + iY0 + _uiSeed);
	float s01 = (float)Sample1D(iX0 + iY1 - _uiSeed);
	float s11 = (float)Sample1D(iX1 + iY1 - _uiSeed);

	float u = InterpolationPolynom(fFracX);
	float v = InterpolationPolynom(fFracY);
	float du = Derivative(fFracX);
	float dv = Derivative(fFracY);

	float k1 = s10 - s00;
	float k2 = s01 - s00;

	_fOutGradX = (k1+(s11-s01-k1)*v)*du;
	_fOutGradY = (k2+(s11-s10-k2)*u)*dv;

	//	   lrp(lrp(s00, s10, u), lrp(s01, s11, u), v);
	return lrp(	   s00 + k1 * u, lrp(s01, s11, u), v);
}


float Rand2D(int _iLowOctave, int _iHeightOctave, float _fPersistence, float _fX, float _fY, unsigned int _uiSeed, float& _fOutGradX, float& _fOutGradY)
{
	// Octaves cannot be smaller than zero and the height octave
	// must be larger than the low one.
	assert( _iLowOctave >= 0 && _iHeightOctave >= _iLowOctave );

	_fOutGradX.x = _fOutGradY.y = 0.0f;
	float fRes = 0.0f;
	float fAmplitude = 1.0f;
	float fFrequence = float(1<<_iLowOctave);
	for(int i=_iLowOctave; i<=_iHeightOctave; ++i)
	{
		float fGradX, fGradY;
		fRes += fAmplitude*Rand2D(_fX, _fY, fFrequence, _uiSeed*37, fGradX, fGradY );
		_fOutGradX += fFrequence*fAmplitude*fGradX;
		_fOutGradY += fFrequence*fAmplitude*fGradY;

		fAmplitude *= _fPersistence;
		fFrequence *= 2.0f;
	}

	// Transform to [-1,1]
	return fRes*2.0f*(1.0f-_fPersistence)/(1.0f-fAmplitude)-1.0f;
}
