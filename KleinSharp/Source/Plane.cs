﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using __m128 = System.Runtime.Intrinsics.Vector128<float>;
using static KleinSharp.Simd;
// ReSharper disable ParameterHidesMember
// ReSharper disable InconsistentNaming

namespace KleinSharp
{
	/// <summary>
	/// <p>In projective geometry, <b>planes</b> are the fundamental element through which all
	/// other entities are constructed.</p>
	/// <br/>
	/// <p>Lines are the meet of two planes, and points are the meet of three planes (equivalently, a line and a plane).</p>
	/// <br/>
	/// <p>The Plane multivector in PGA looks like <c>d e₀ + a e₁ + b e₂ + c e₃</c>
	/// </p>
	/// <br/>
	/// where <c>e₁, e₂, e₃ </c>are the basis Euclidean YZ, ZX and XY planes, and e₀ is the ideal plane (aka the plane "at infinity")
	/// <br/>
	/// <p>
	/// <br/>
	/// <br/>
	/// Points that reside on the plane satisfy the familiar equation:
	/// <br/>
	/// <br/>
	/// d + a x + b y + c z = 0
	/// <br/>
	/// </p>
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct Plane : IEquatable<Plane>
	{
		public readonly __m128 P0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Plane(__m128 p0)
		{
			P0 = p0;
		}

		/// <summary>
		/// The constructor performs the rearrangement so the Plane
		/// can be specified in the familiar form: ax + by + cz + d
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Plane(float a, float b, float c, float d)
		{
			P0 = _mm_set_ps(c, b, a, d);
		}

		/// <summary>
		/// Data should point to four floats with memory layout `(d, a, b, c)` where
		/// `d` occupies the lowest address in memory.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe Plane(float* data)
		{
			P0 = _mm_loadu_ps(data);
		}

		public Plane(ReadOnlySpan<float> data)
		{
			P0 = _mm_loadu_ps(data);
		}

		/// <summary>
		/// Store the 4 float components to memory
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe void Store(float* data)
		{
			_mm_storeu_ps(data, P0);
		}

		/// <summary>
		/// Store the 4 float components in a span
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Store(Span<float> data)
		{
			_mm_storeu_ps(data, P0);
		}

		/// <summary>
		/// Deconstructs the components <c>(a,b,c,d)</c> of the plane <c>a e₁ + b e₂ +c e₃ + d e₀</c>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Deconstruct(out float a, out float b, out float c, out float d)
		{
			a = e1;
			b = e2;
			c = e3;
			d = e0;
		}

		/// <summary>
		/// Normalize this Plane $p$ such that $p \cdot p = 1$.
		///
		/// In order to compute the cosine of the angle between Planes via the
		/// inner product operator `|`, the Planes must be normalized. Producing a
		/// normalized rotor between two Planes with the geometric product `*` also
		/// requires that the Planes are normalized.
		/// 
		/// Return a normalized copy of this Plane.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Plane Normalized()
		{
			__m128 invNorm = Detail.rsqrt_nr1(Detail.hi_dp_bc(P0, P0));
			invNorm = Sse41.IsSupported
				? _mm_blend_ps(invNorm, _mm_set_ss(1f), 1)
				: _mm_add_ps(invNorm, _mm_set_ss(1f));
			return new Plane(_mm_mul_ps(invNorm, P0));
		}

		/// <summary>
		/// Compute the Plane norm, which is often used to compute distances
		/// between points and lines.
		///
		/// Given a normalized point $P$ and normalized line $\ell$, the Plane
		/// $P\vee\ell$ containing both $\ell$ and $P$ will have a norm equivalent
		/// to the distance between $P$ and $\ell$.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Norm()
		{
			return _mm_store_ss(Detail.sqrt_nr1(Detail.hi_dp(P0, P0)));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Plane Inverse()
		{
			__m128 p0 = P0;
			__m128 invNorm = Detail.rsqrt_nr1(Detail.hi_dp_bc(p0, p0));
			p0 = _mm_mul_ps(invNorm, p0);
			p0 = _mm_mul_ps(invNorm, p0);
			return new Plane(p0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Plane other)
		{
			return _mm_movemask_ps(_mm_cmpeq_ps(P0, other.P0)) == 0b1111;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Plane other, float epsilon)
		{
			__m128 eps = _mm_set1_ps(epsilon);
			__m128 cmp = _mm_cmplt_ps(
				_mm_andnot_ps(_mm_set1_ps(-0f), _mm_sub_ps(P0, other.P0)), eps);
			return _mm_movemask_ps(cmp) != 0b1111;
		}

		/// <summary>
		/// Reflect another Plane $p_2$ through this Plane $p_1$. The operation
		/// performed via this method is an optimized routine equivalent to
		/// the expression $p_1 p_2 p_1$.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Plane Reflect(Plane p)
		{
			Detail.sw00(P0, p.P0, out var p0);
			return new Plane(p0);
		}

		/// <summary>
		/// <p>Reflect line <i>L</i> through this plane <b>p</b>.</p>
		/// <p>This an optimized routine equivalent to the expression <b>p</b> <i>L</i> <b>p</b></p>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Line Reflect(Line l)
		{
			Detail.sw10(P0, l.P1, out var p1, out var p2);
			p2 = _mm_add_ps(p2, Detail.sw20(P0, l.P2));
			return new Line(p1, p2);
		}

		/// <summary>
		/// Reflect the point $Q$ through this plane $p$.
		///
		/// The operation performed via this index operator, e.g. p[Q],
		/// is an optimized routine equivalent to the expression $p Q p$.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Point Reflect(Point p) => new Point(Detail.sw30(P0, p.P3));

		/// <summary>
		/// Same as <see cref="Reflect(Plane)"/>
		/// </summary>
		public Plane this[in Plane p] => Reflect(p);

		/// <summary>
		/// Same as <see cref="Reflect(Line)"/>
		/// </summary>
		public Line this[in Line l] => Reflect(l);

		/// <summary>
		/// Same as <see cref="Reflect(Point)"/>
		/// </summary>
		public Point this[Point p] => Reflect(p);

		public float e1 => P0.GetElement(1);
		public float X => e1;
		public float A => X;

		public float e2 => P0.GetElement(2);
		public float Y => e2;
		public float B => Y;

		public float e3 => P0.GetElement(3);
		public float Z => e3;
		public float C => Z;

		public float e0 => P0.GetElement(0);
		public float W => e0;
		public float D => W;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Plane operator +(Plane a, Plane b)
		{
			return new Plane(_mm_add_ps(a.P0, b.P0));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Plane operator -(Plane a, Plane b)
		{
			return new Plane(_mm_sub_ps(a.P0, b.P0));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Plane operator *(Plane p, float s)
		{
			return new Plane(_mm_mul_ps(p.P0, _mm_set1_ps(s)));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Plane operator *(float s, Plane p)
		{
			return p * s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Plane operator /(Plane p, float s)
		{
			return new Plane(_mm_mul_ps(p.P0, Detail.rcp_nr1(_mm_set1_ps(s))));
		}

		/// Unary minus (leaves displacement from origin untouched, changing orientation only)
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Plane operator -(Plane p)
		{
			return new Plane(_mm_xor_ps(p.P0, _mm_set_ps(-0f, -0f, -0f, 0f)));
		}

		/// <summary>
		/// Wedge (aka exterior, outer) product between two planes
		/// </summary>
		/// <returns>
		/// The intersection line between the two planes (could be an ideal line for parallel planes)
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Line operator ^(Plane a, Plane b)
		{
			Detail.ext00(a.P0, b.P0, out var p1, out var p2);
			return new Line(p1, p2);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator ^(Plane a, Branch b)
		{
			return new Point(Detail.extPB(a.P0, b.P1));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator ^(Plane a, Line b)
		{
			var p3 = Detail.extPB(a.P0, b.P1);
			var tmp = Detail.ext02(a.P0, b.P2);
			p3 = _mm_add_ps(tmp, p3);
			return new Point(p3);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Dual operator ^(Plane a, Point b)
		{
			return new Dual(0, _mm_store_ss(Detail.ext03(false, a.P0, b.P3)));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator ^(Plane a, IdealLine b)
		{
			return new Point(Detail.ext02(a.P0, b.P2));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Plane operator |(Plane a, IdealLine b)
		{
			return new Plane(Detail.dotPIL(false, a.P0, b.P2));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float operator |(Plane a, Plane b)
		{
			return _mm_store_ss(Detail.dot00(a.P0, b.P0));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Plane operator |(Plane a, Line b)
		{
			return new Plane(Detail.dotPL(false, a.P0, b.P1, b.P2));
		}

		/// <summary>
		/// The dual of a plane ax+by+cz+d = 0 is the point (a,b,c,d).
		/// </summary>
		/// <remarks>
		/// If the plane is through the origin, then the dual corresponds to the normal of the plane.
		/// TODO: Document other case
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator !(Plane p)
		{
			return new Point(p.P0);
		}

		/// <summary>
		/// Regressive product, aka join operator in PGA between plane and point.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Dual operator &(Plane a, Point b)
		{
			return !(!a ^ !b);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return obj is Plane other && Equals(other);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return P0.GetHashCode();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(Plane left, Plane right)
		{
			return left.Equals(right);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(Plane left, Plane right)
		{
			return !left.Equals(right);
		}

		/// <summary>
		/// Inner product between plane and point
		/// </summary>
		/// <returns>
		/// The line ⊥ to the plane through the point
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Line operator |(Plane a, Point b)
		{
			Detail.dot03(a.P0, b.P3, out var p1, out var p2);
			return new Line(p1, p2);
		}

		/// <summary>
		/// Construct a Motor $m$ such that $\sqrt{m}$ takes Plane $b$ to Plane $a$.
		///
		/// !!! example
		///
		///     ```cpp
		///         kln::Plane p1{x1, y1, z1, d1};
		///         kln::Plane p2{x2, y2, z2, d2};
		///         kln::Motor m = sqrt(p1 * p2);
		///         Plane p3 = m(p2);
		///         // p3 will be approximately equal to p1
		///     ```
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Motor operator *(Plane a, Plane b)
		{
			Detail.gp00(a.P0, b.P0, out var p1, out var p2);
			return new Motor(p1, p2);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Motor operator *(Plane a, Point b)
		{
			Detail.gp03(false, a.P0, b.P3, out var p1, out var p2);
			return new Motor(p1, p2);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Motor operator /(Plane a, Plane b)
		{
			return a * b.Inverse();
		}


		public override string ToString()
		{
			var (a, b, c, d) = this;

			return new StringBuilder(64)
				.AppendElement(d, "e₀")
				.AppendElement(a, "e₁")
				.AppendElement(b, "e₂")
				.AppendElement(c, "e₃")
				.ZeroWhenEmpty();
		}

	}
}
