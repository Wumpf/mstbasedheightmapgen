#pragma once

/// \brief Samples a 2D Perlin Noise function.
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
			 unsigned int _uiSeed,
			 float& _fOutGradX,
			 float& _fOutGradY );