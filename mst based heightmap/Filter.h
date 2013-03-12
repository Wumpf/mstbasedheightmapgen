#pragma once

/// \brief Transforms the range of values in the float image to the range [0,1].
/// \details
/// \warning For performance reasons height has to be a muliple of 4!
void Normalize( float* dataDestination, int width, int height );