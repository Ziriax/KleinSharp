﻿using System.Text;

namespace KleinSharp
{
	internal static class Utils
	{
		public static string ZeroWhenEmpty(this StringBuilder sb)
		{
			return sb.Length == 0 ? "0" : sb.ToString();
		}

		// [MethodImpl(MethodImplOptions.AggressiveInlining)]
		// public static unsafe float* Aligned16(float* buffer)
		// {
		// 	return (float*)(((long)buffer + 3) & ~3);
		// }
		//
		public static StringBuilder AppendScalar(this StringBuilder sb, float scalar)
		{
			if (scalar < 0 || scalar > 0)
			{
				sb.Append(scalar);
			}
			return sb;
		}

		public static StringBuilder AppendElement(this StringBuilder sb, float component, string element)
		{
			if (component < 0)
			{
				sb.Append(" - ");
				component = -component;
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				if (component != 1) sb.Append(component);
				sb.Append(element);
				return sb;
			}

			if (component > 0)
			{
				sb.Append(" + ");
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				if (component != 1) sb.Append(component);
				sb.Append(element);
				return sb;
			}

			return sb;
		}
	}
}