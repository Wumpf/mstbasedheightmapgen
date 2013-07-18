
// Copied from Saga-K-Demo-Game

#pragma once

#include <string.h>
#include <cmath>

#undef min
#undef max

//namespace Saga {

	// ************************************************************************** //
	// Helper functions
	// ************************************************************************** //
	inline float sgn( float f )						{ return f<0.0f ? -1.0f : 1.0f; }
	inline int min( int a, int b )					{ return a<b ? a : b; }
	inline float min( float a, float b )			{ return a<b ? a : b; }
	inline double min( double a, double b )			{ return a<b ? a : b; }
	inline int max( int a, int b )					{ return a>b ? a : b; }
	inline float max( float a, float b )			{ return a>b ? a : b; }
	inline double max( double a, double b )			{ return a>b ? a : b; }
	inline float lrp( float a, float b, float f)	{ return a+(b-a)*f; }
	inline float lrpsmooth( float a, float b, float f)	{ return a+(b-a)*f*f*f*(f*(f*6.0f-15.0f)+10.0f); }
	inline float sqr( float _x )					{ return _x*_x; }
	inline int Floor(const float a)					{int r=(int)a; return r - (int)((a<0)&&(a-r!=0.0f));}		// Round down

	// ************************************************************************** //
	// 3D Vector class
	// ************************************************************************** //
	struct Vec3
	{
		float x,y,z;

		Vec3()	{}
		Vec3( float _x, float _y, float _z ) : x(_x), y(_y), z(_z)	{}
	};
	inline Vec3 operator - ( const Vec3& v1, const Vec3& v2 )			{return Vec3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);}
	inline Vec3 operator + ( const Vec3& v1, const Vec3& v2 )			{return Vec3(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);}
	inline Vec3 operator * ( const float f, const Vec3& v )				{return Vec3(v.x * f, v.y * f, v.z * f);}
	inline Vec3 operator * ( const Vec3& v, const float f )				{return Vec3(v.x * f, v.y * f, v.z * f);}

	// TODO: code size? inline will hopefully by ignored if good for size
	inline Vec3 cross( const Vec3& v1, const Vec3& v2 )
	{
		return Vec3( v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v2.x );
	}

	inline float lensq( const Vec3& v )
	{
		return v.x*v.x + v.y*v.y + v.z*v.z;
	}

	inline float len( const Vec3& v )
	{
		return sqrt( v.x*v.x + v.y*v.y + v.z*v.z );
	}

	inline Vec3 nrm( const Vec3& v )
	{
		return v * (1.0f/max(0.00001f,len(v)));
	}

	inline float dot( const Vec3& v1, const Vec3& v2 )
	{
		return v1.x*v2.x + v1.y*v2.y + v1.z*v2.z;
	}

	inline Vec3 lrp( const Vec3& v1, const Vec3& v2, float f)
	{
		return v1+(v2-v1)*f;
	}


	// ************************************************************************** //
	// Matrix class
	// ************************************************************************** //
	struct Mat4x4
	{
		union {
			struct {
				float m11, m12, m13, m14;
				float m21, m22, m23, m24;
				float m31, m32, m33, m34;
				float m41, m42, m43, m44;
			};
			float m[16];
		};

		Mat4x4()
		{
			// TODO: test if there is a smaller code than call memset
			memset( this, 0, sizeof(Mat4x4) );
		}

		// The following methods are "advanced constructors". They should be used
		// like 'mat4x4().Setup..(...);'

		// Translation matrix in all three axis directions
		Mat4x4& SetupTranslation( float _x, float _y, float _z )
		{
			m11 = 1.0f;
						m22 = 1.0f;
									m33 = 1.0f;
			m41 = _x;	m42 = _y;	m43 = _z;	m44 = 1.0f;
			return *this;
		}

		// Scaling matrix for all axis idependently
		Mat4x4& SetupScaling( float _sx, float _sy, float _sz )
		{
			m11 = _sx;
						m22 = _sy;
									m33 = _sz;
												m44 = 1.0f;
			return *this;
		}

		// Rotation beyond the X and Y axis (Z is currently not required)
		Mat4x4& SetupRotX( float _fAngle )
		{
			float fSin = sin( _fAngle );
		    float fCos = cos( _fAngle );
			m11 = 1.0f;
						m22 = fCos;	m23 = fSin;
						m32 =-fSin;	m33 = fCos;
												m44 = 1.0f;

			return *this;
		}

		Mat4x4& SetupRotY( float _fAngle )
		{
			float fSin = sin( _fAngle );
		    float fCos = cos( _fAngle );
			m11 = fCos;				m13 =-fSin;
						m22 = 1.0f;
			m31 = fSin;				m33 = fCos;
												m44 = 1.0f;
			return *this;
		}
	};

	inline Mat4x4 operator * (const Mat4x4& ml, const Mat4x4& mr)
	{
		Mat4x4 m;
		// faster but bigger
		/*m.m11 = ml.m11 * mr.m11 + ml.m12 * mr.m21 + ml.m13 * mr.m31 + ml.m14 * mr.m41;
		m.m12 = ml.m11 * mr.m12 + ml.m12 * mr.m22 + ml.m13 * mr.m32 + ml.m14 * mr.m42;
		m.m13 = ml.m11 * mr.m13 + ml.m12 * mr.m23 + ml.m13 * mr.m33 + ml.m14 * mr.m43;
		m.m14 = ml.m11 * mr.m14 + ml.m12 * mr.m24 + ml.m13 * mr.m34 + ml.m14 * mr.m44;

		m.m21 = ml.m21 * mr.m11 + ml.m22 * mr.m21 + ml.m23 * mr.m31 + ml.m24 * mr.m41;
		m.m22 = ml.m21 * mr.m12 + ml.m22 * mr.m22 + ml.m23 * mr.m32 + ml.m24 * mr.m42;
		m.m23 = ml.m21 * mr.m13 + ml.m22 * mr.m23 + ml.m23 * mr.m33 + ml.m24 * mr.m43;
		m.m24 = ml.m21 * mr.m14 + ml.m22 * mr.m24 + ml.m23 * mr.m34 + ml.m24 * mr.m44;

		m.m31 = ml.m31 * mr.m11 + ml.m32 * mr.m21 + ml.m33 * mr.m31 + ml.m34 * mr.m41;
		m.m32 = ml.m31 * mr.m12 + ml.m32 * mr.m22 + ml.m33 * mr.m32 + ml.m34 * mr.m42;
		m.m33 = ml.m31 * mr.m13 + ml.m32 * mr.m23 + ml.m33 * mr.m33 + ml.m34 * mr.m43;
		m.m34 = ml.m31 * mr.m14 + ml.m32 * mr.m24 + ml.m33 * mr.m34 + ml.m34 * mr.m44;

		m.m41 = ml.m41 * mr.m11 + ml.m42 * mr.m21 + ml.m43 * mr.m31 + ml.m44 * mr.m41;
		m.m42 = ml.m41 * mr.m12 + ml.m42 * mr.m22 + ml.m43 * mr.m32 + ml.m44 * mr.m42;
		m.m43 = ml.m41 * mr.m13 + ml.m42 * mr.m23 + ml.m43 * mr.m33 + ml.m44 * mr.m43;
		m.m44 = ml.m41 * mr.m14 + ml.m42 * mr.m24 + ml.m43 * mr.m34 + ml.m44 * mr.m44;//*/

		// Test how many this one is smaller:
		//	all objs-Byte:		Release controller.obj
		//	3.872.609								- without
		//	3.874.300	1619	1.571.019	0		- for loop no index precalc
		//	3.874.952	2271	1.572.659	1640	- block code
		//	3.874.632	1951	1.571.207	188		- index precomputing
		for( int i=0; i<4; ++i )
		{
			int y = i*4;
			m.m11 += ml.m[i] * mr.m[y];		m.m12 += ml.m[i] * mr.m[y+1];		m.m13 += ml.m[i] * mr.m[y+2];		m.m14 += ml.m[i] * mr.m[y+3];
			m.m21 += ml.m[i+4] * mr.m[y];	m.m22 += ml.m[i+4] * mr.m[y+1];		m.m23 += ml.m[i+4] * mr.m[y+2];		m.m24 += ml.m[i+4] * mr.m[y+3];
			m.m31 += ml.m[i+8] * mr.m[y];	m.m32 += ml.m[i+8] * mr.m[y+1];		m.m33 += ml.m[i+8] * mr.m[y+2];		m.m34 += ml.m[i+8] * mr.m[y+3];
			m.m41 += ml.m[i+12] * mr.m[y];	m.m42 += ml.m[i+12] * mr.m[y+1];	m.m43 += ml.m[i+12] * mr.m[y+2];	m.m44 += ml.m[i+12] * mr.m[y+3];
		}//*/

		return m;
	}

	// Transformation of a vector without projection part (only 4x3 matrix is used)
	inline Vec3 operator * (const Vec3& v, const Mat4x4& m)
	{
		return Vec3(
			v.x * m.m11 + v.y * m.m21 + v.z * m.m31 + m.m41,
			v.x * m.m12 + v.y * m.m22 + v.z * m.m32 + m.m42,
			v.x * m.m13 + v.y * m.m23 + v.z * m.m33 + m.m43
			);
	}


	// ************************************************************************** //
	// Analytical geometry
	// ************************************************************************** //

	/// \brief Shortest distance between a point and a line (no straight!).
	/// \details A line is defined by an start and an end point. The measured
	///		distance is computed between a point on the line
	///		(p in [lrp(start,end,t) | t in R, 0<=t<=1]) and the single point.
	inline float PointLineDistanceSq( const Vec3& lineStart, const Vec3& lineEnd, const Vec3& point )
	{
		Vec3 v(lineEnd - lineStart);
		Vec3 w(point - lineStart);

		float fR = (dot(w, v) / lensq(v));

	/*	if( fR <= 0.0f )				// Nearest point to 'point' is 'lineStart'
			return len( w );
		else if( fR < 1.0f )			// Nearest point is on the line
			return len( lineStart + v * fR - point );
		return len( lineEnd - point );	// Nearest point to 'point' is 'lineEnd'*/
		fR = min( 1.0f, max( 0.0f, fR ) );
		return lensq( lineStart + v * fR - point );
	}

	inline float PointLineDistanceSq( const Vec3& lineStart, const Vec3& lineEnd, float px, float py, float& r )
	{
		float vx = lineEnd.x - lineStart.x;
		float vy = lineEnd.y - lineStart.y;
		float wx = px - lineStart.x;
		float wy = py - lineStart.y;

		r = ((wx*vx + wy*vy) / (vx*vx + vy*vy));

		r = min( 1.0f, max( 0.0f, r ) );
		float x = vx * r - wx;
		float y = vy * r - wy;
		return x*x + y*y;
	}


//};