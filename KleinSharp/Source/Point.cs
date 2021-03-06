﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using __m128 = System.Runtime.Intrinsics.Vector128<float>;
using static KleinSharp.Simd;
// ReSharper disable InconsistentNaming

namespace KleinSharp
{
	/// <summary>
	/// A homogeneous point (x,y,z,w) is represented as the multivector <c>x e₀₃₂ + y e₀₁₃ + z e₀₂₁ + w e₁₂₃</c>.
	/// <br/>
	/// For convenience, the point basis is often defined as:
	/// <c>E₁ = e₀₃₂, E₂ = y e₀₁₃, E₃ = e₀₂₁, E₀ = e₁₂₃</c>
	/// <br/>
	/// Then a homogeneous point (x,y,z,w) is easier represented as <c>x E₁ + y E₂ + z E₃ + w E₀</c>
	/// <br/>
	/// The Point has a trivector representation because it is the
	/// fixed point of 3 planar reflections (each of which is a grade-1 multivector).
	/// <br/>
	/// In practice, the coordinate mapping can be thought of as an implementation detail.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct Point : IEquatable<Point>
	{
		public static readonly Origin Origin;

		public readonly __m128 P3;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Point(__m128 p3)
		{
			P3 = p3;
		}

		/// <summary>
		/// Component-wise constructor (homogeneous coordinate is automatically initialized to 1)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Point(float x, float y, float z)
		{
			P3 = _mm_set_ps(z, y, x, 1f);
		}

		/// <summary>
		/// Component-wise constructor, with explicit homogeneous coordinate w
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Point(float x, float y, float z, float w)
		{
			P3 = _mm_set_ps(z, y, x, w);
		}

		/// <summary>
		/// Fast load from a pointer to an array of four floats with layout
		/// `(w, x, y, z)` where `w` occupies the lowest address in memory.
		///
		/// !!! tip
		///
		///     This load operation is more efficient that modifying individual
		///     components back-to-back.
		///
		/// !!! danger
		///
		///     Unlike the component-wise constructor, the load here requires the
		///     homogeneous coordinate `w` to be supplied as well in the lowest
		///     address Pointed to by `data`.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe Point(float* data)
		{
			P3 = _mm_loadu_ps(data);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Point(Span<float> data)
		{
			P3 = _mm_loadu_ps(data);
		}

		/// <summary>
		/// Deconstruct the components of the point <c>e₁₂₃ + x e₀₃₂ + y e₀₁₃ + z e₀₂₁</c>
		/// <br/>
		/// The point is assumed to be normalized, as the w component is not returned here.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Deconstruct(out float x, out float y, out float z)
		{
			x = e032;
			y = e013;
			z = e021;
		}

		/// <summary>
		/// Deconstruct the components of the point <c>w e₁₂₃ + x e₀₃₂ + y e₀₁₃ + z e₀₂₁</c>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Deconstruct(out float x, out float y, out float z, out float w)
		{
			x = e032;
			y = e013;
			z = e021;
			w = e123;
		}

		/// <summary>
		/// Store m128 contents into an array of 4 floats
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe void Store(float* data) => _mm_storeu_ps(data, P3);

		/// <summary>
		/// Store m128 contents into a span of at least 4 floats.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Store(Span<float> buffer)
		{
			_mm_storeu_ps(buffer, P3);
		}

		/// <summary>
		/// Return a normalized copy of this Point, so that the component of e₁₂₃ becomes 1.
		/// </summary>
		/// <remarks>
		/// Division is done via rcpps with an additional Newton-Raphson refinement.
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Point Normalized()
		{
			__m128 tmp = Detail.rcp_nr1(_mm_swizzle_ps(P3, 0 /* 0, 0, 0, 0 */));
			return new Point(_mm_mul_ps(P3, tmp));
		}

		/// <summary>
		/// Returns the inverse of this point. A point multiplied with its inverse is 1.
		/// </summary>
		/// <remarks>
		/// Division is done via rcpps with an additional Newton-Raphson refinement.
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Point Inverse()
		{
			__m128 p3 = P3;
			__m128 invNorm = Detail.rcp_nr1(_mm_swizzle_ps(p3, 0 /* 0, 0, 0, 0 */));
			p3 = _mm_mul_ps(invNorm, p3);
			p3 = _mm_mul_ps(invNorm, p3);
			return new Point(p3);
		}

		public float e032 => P3.GetElement(1);
		public float e023 => -e032;
		public float E1 => e032;
		public float X => e032;

		public float e013 => P3.GetElement(2);
		public float e031 => -e013;
		public float E2 => e013;
		public float Y => e013;

		public float e021 => P3.GetElement(3);
		public float e012 => -e021;
		public float E3 => e021;
		public float Z => e021;

		/// The homogeneous coordinate `w` is exactly $1$ when normalized.
		public float e123 => P3.GetElement(0);
		public float E0 => e123;
		public float W => e123;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator +(Point a, Point b)
		{
			return new Point(_mm_add_ps(a.P3, b.P3));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator -(Point a, Point b)
		{
			return new Point(_mm_sub_ps(a.P3, b.P3));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator *(Point p, float s)
		{
			return new Point(_mm_mul_ps(p.P3, _mm_set1_ps(s)));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator *(float s, Point p)
		{
			return p * s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator /(Point p, float s)
		{
			return new Point(_mm_mul_ps(p.P3, Detail.rcp_nr1(_mm_set1_ps(s))));
		}

		/// <remarks>
		/// Unary minus (leaves homogeneous component w untouched)
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator -(Point p)
		{
			return new Point(_mm_xor_ps(p.P3, _mm_set_ps(-0f, -0f, -0f, 0f)));
		}

		/// <summary>
		/// Reversion operator
		/// </summary>
		/// <remarks>
		/// Reversion negates all components except the scalar. E.g. ~(1 + e₀₃) = 1 - e₀₃
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator ~(Point p)
		{
			__m128 flip = _mm_set1_ps(-0f);
			return new Point(_mm_xor_ps(p.P3, flip));
		}

		/// <summary>
		/// Generates a translator $t$ that produces a displacement along the line
		/// between points $a$ and $b$. The translator given by $\sqrt{t}$ takes $b$ to $a$.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Translator operator *(Point a, Point b)
		{
			return new Translator(Detail.gp33(a.P3, b.P3));
		}

		/// <summary>
		/// Geometric product between a point and a plane
		/// </summary>
		/// <returns>
		/// TODO: Explain what the motor represents
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Motor operator *(Point b, Plane a)
		{
			Detail.gp03(true, a.P0, b.P3, out var p1, out var p2);
			return new Motor(p1, p2);
		}

		/// <summary>
		/// TODO: Document!
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Translator operator /(Point a, Point b)
		{
			return a * b.Inverse();
		}

		/// <summary>
		/// Inner product between point and plane
		/// </summary>
		/// <returns>
		/// The line ⊥ to the plane through the point
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Line operator |(Point a, Plane b)
		{
			return b | a;
		}

		/// <summary>
		/// Inner product between a point and a line
		/// </summary>
		/// <returns>
		/// The plane ⊥ to the line through the point
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Plane operator |(Point a, Line b)
		{
			return new Plane(Detail.dotPTL(a.P3, b.P1));
		}

		/// <summary>
		/// TODO: Document!
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float operator |(Point a, Point b)
		{
			return Detail.dot33(a.P3, b.P3).GetElement(0);
		}

		/// <summary>
		/// The dual of a point (a,b,c,d) is the plane ax + by + cz + d = 0
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Plane operator !(Point a)
		{
			return new Plane(a.P3);
		}

		/// <summary>
		/// Regressive product, aka join operator in PGA
		/// TODO: Document!
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Line operator &(Point a, Point b)
		{
			return !(!a ^ !b);
		}

		/// <summary>
		/// Regressive product, aka join operator in PGA
		/// TODO: Document!
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Plane operator &(Point a, Line b)
		{
			return !(!a ^ !b);
		}

		/// <summary>
		/// Regressive product, aka join operator in PGA
		/// TODO: Document!
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Plane operator &(Point a, Branch b)
		{
			return !(!a ^ !b);
		}

		/// <summary>
		/// Regressive product, aka join operator in PGA
		/// TODO: Document!
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Plane operator &(Point a, IdealLine b)
		{
			return !(!a ^ !b);
		}

		/// <summary>
		/// The regressive product is the <b>join</b> operator in PGA
		/// TODO: Document!
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Dual operator &(Point a, Plane b)
		{
			return !(!a ^ !b);
		}

		/// <summary>
		/// The exterior product is the <b>meet</b> operator in PGA
		/// TODO: Document!
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Dual operator ^(Point b, Plane a)
		{
			__m128 tmp = Detail.ext03(true, a.P0, b.P3);
			return new Dual(0, _mm_store_ss(tmp));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Point(Origin _)
		{
			return new Point(_mm_set_ss(1f));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Point other)
		{
			return P3.Equals(other.P3);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return obj is Point other && Equals(other);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return P3.GetHashCode();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(Point left, Point right)
		{
			return left.Equals(right);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(Point left, Point right)
		{
			return !left.Equals(right);
		}

		public override string ToString()
		{
			return new StringBuilder("e₁₂₃", 64)
				.AppendElement(X, "e₀₃₂")
				.AppendElement(Y, "e₀₁₃")
				.AppendElement(Z, "e₀₂₁")
				.ToString();
		}
	}
}

/// <summary>
/// The origin is a convenience type that occupies no memory but is castable to
/// a Point entity. Several operations like conjugation of the origin by a motor
/// is optimized.
/// </summary>
/// <remarks>
/// On its own, the origin occupies no memory, but it can be casted as an
/// entity at any Point, at which Point it is represented as e₁₂₃
/// </remarks>
public readonly struct Origin
{
}