﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace KleinSharp
{
	/// <summary>
	/// The `Branch` is both a line through the origin and
	/// also the principal Branch of the logarithm of a rotor.
	///
	/// The rotor Branch will be most commonly constructed by taking the
	/// logarithm of a normalized rotor. The Branch may then be linearly scaled
	/// to adjust the "strength" of the rotor, and subsequently re-exponentiated
	/// to create the adjusted rotor.
	///
	/// !!! example
	///
	///     Suppose we have a rotor <b>r</b> and we wish to produce a rotor
	///     $\sqrt[4]{r}$ which performs a quarter of the rotation produced by
	///     $r$. We can construct it like so:
	///
	///     ```cs
	///         Branch b = r.Log();
	///         Rotor r_4 = (0.25f * b).Exp();
	///     ```
	///
	/// !!! note
	///
	///     The Branch of a rotor is technically a `line`, but because there are
	///     no translational components, the Branch is given its own type for
	///     efficiency.
	/// </summary>
	/// <remarks>
	/// Klein provides three line classes: <see cref="Line"/>, <see cref="Branch"/>, and <see cref="IdealLine"/>.
	///
	/// The line class represents a full six-coordinate bivector.
	///
	/// The Branch contains three non-degenerate components (aka, a line through the origin).
	///
	/// The ideal line represents the line at infinity.
	///
	/// When the line is created as a meet of two planes or join of two points
	/// (or carefully selected Plücker coordinates), it will be a Euclidean line
	/// (factorisable as the meet of two vectors).
	/// </remarks>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct Branch : IEquatable<Branch>
	{
		public readonly Vector128<float> P1;

		/// <summary>
		/// Construct the Branch as the following multivector:
		///
		/// <b>a e₂₃ + b e₃₁ + c e₁₂</b>
		///
		/// To convince yourself this is a line through the origin, remember that
		/// such a line can be generated using the geometric product of two planes
		/// through the origin.
		/// </summary>
		public Branch(float a, float b, float c)
		{
			P1 = Simd._mm_set_ps(c, b, a, 0f);
		}

		public Branch(Vector128<float> xmm)
		{
			P1 = xmm;
		}

		public void Deconstruct(out float e23, out float e31, out float e12)
		{
			e23 = E23;
			e31 = E31;
			e12 = E12;
		}

		public float E12 => P1.GetElement(3);
		public float E21 => -E12;
		public float Z => E12;

		public float E31 => P1.GetElement(2);
		public float E13 => -E31;
		public float Y => E12;

		public float E23 => P1.GetElement(1);
		public float E32 => -E23;
		public float X => E23;

		public ReadOnlySpan<float> ToSpan() => Helpers.ToFloatSpan(this);

		/// <summary>
		/// If a line is constructed as the regressive product (join) of
		/// two points, the squared norm provided here is the squared
		/// distance between the two points (provided the points are
		/// normalized). Returns $d^2 + e^2 + f^2$.
		/// </summary>
		public float SquaredNorm()
		{
			var dp = Detail.hi_dp(P1, P1);
			Simd._mm_store_ss(out var norm, dp);
			return norm;
		}

		/// <summary>
		/// Returns the square root of the quantity produced by `squared_norm`.
		/// </summary>
		public float Norm()
		{
			return MathF.Sqrt(SquaredNorm());
		}

		public static Vector128<float> Normalized(Vector128<float> p)
		{
			Vector128<float> inv = Detail.rsqrt_nr1(Detail.hi_dp_bc(p, p));
			return Simd._mm_mul_ps(p, inv);
		}

		public Branch Normalized() => new Branch(Normalized(P1));

		public static Vector128<float> Inverse(Vector128<float> p)
		{
			Vector128<float> inv = Detail.rsqrt_nr1(Detail.hi_dp_bc(p, p));
			p = Simd._mm_mul_ps(p, inv);
			p = Simd._mm_mul_ps(p, inv);
			p = Simd._mm_xor_ps(Simd._mm_set_ps(-0f, -0f, -0f, 0f), p);
			return p;
		}

		public Branch Inverse() => new Branch(Inverse(P1));

		public static Branch operator +(Branch a, Branch b)
		{
			return new Branch(Simd._mm_add_ps(a.P1, b.P1));
		}

		public static Branch operator -(Branch a, Branch b)
		{
			return new Branch(Simd._mm_sub_ps(a.P1, b.P1));
		}

		public static Branch operator *(Branch b, float s)
		{
			return new Branch(Simd._mm_mul_ps(b.P1, Simd._mm_set1_ps(s)));
		}

		public static Branch operator *(float s, Branch b)
		{
			return new Branch(Simd._mm_mul_ps(b.P1, Simd._mm_set1_ps(s)));
		}

		public static Branch operator /(Branch b, float s)
		{
			return new Branch(Simd._mm_mul_ps(b.P1, Detail.rcp_nr1(Simd._mm_set1_ps(s))));
		}

		public static Branch operator -(Branch b)
		{
			return new Branch(Simd._mm_xor_ps(b.P1, Simd._mm_set1_ps(-0f)));
		}

		/// <summary>
		/// Reversion operator
		/// </summary>
		public static Branch operator ~(Branch b)
		{
			Vector128<float> flip = Simd._mm_set_ps(-0f, -0f, -0f, 0f);
			return new Branch(Simd._mm_xor_ps(b.P1, flip));
		}

		public bool Equals(Branch other)
		{
			return P1.Equals(other.P1);
		}

		public override bool Equals(object obj)
		{
			return obj is Branch other && Equals(other);
		}

		public override int GetHashCode()
		{
			return P1.GetHashCode();
		}

		public static bool operator ==(Branch left, Branch right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Branch left, Branch right)
		{
			return !left.Equals(right);
		}

		public override string ToString()
		{
			return $"Branch({E12} e12 + {E31} e31 + {E23} e23)";
		}
	}
}