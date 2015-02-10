#pragma once

#include <cstdint>

/// \brief Create an integer hash and transform it to [0,1]
double Sample1D(int64_t _i);

/// \brief Create an integer hash from a 2D coordinate
double Sample2D(int64_t _x, int64_t _y);

/// \brief Samples a 2D Value Noise function.
/// \param _iLowOctave [in] "Frequence" of large scale noise - at least 0.
/// \param _iHeightOctave [in] "Frequence" of smaller scale noise - at least _iLowOctave.
/// \param _fPersistence [in] Recusive influence of higher frequent noise.
/// \param _fX [in] Sampling position X
/// \param _fX [in] Sampling position Y
/// \param _uiSeed [in] Which noise should be sampled.
/// \param _fOutGradX [out] The analytical computed gradient of the perlin noise function.
/// \param _fOutGradY [out] The analytical computed gradient of the perlin noise function.
/// \return A value in [-1,1]
float Rand2D( int _iLowOctave,
			 int _iHeightOctave,
			 float _fPersistence,
			 float _fX,
			 float _fY,
			 float& _fOutGradX,
			 float& _fOutGradY );

/// \brief Sets a global value for the current noise function.
///
void SetSeed( unsigned int _uiSeed );

/// \brief Create a random sample in a 2D pink noise.
/// \param [in] _fX Sampling position X
/// \param [in] _fY Sampling position Y
/// \param [in] _fFrequence The highest frequence of the created noise.
/// \param [out] _fOutGradX The analytically computed gradient of the noise.
/// \param [out] _fOutGradY The analytically computed gradient of the noise.
/// \return A value in [0,1]
float Rand2D(float _fX, float _fY,
			 float _fFrequence,
			 float& _fOutGradX,
			 float& _fOutGradY);