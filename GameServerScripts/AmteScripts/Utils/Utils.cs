using System;
using System.Collections.Generic;
using System.Linq;

static class AmteUtils
{
	public static void Foreach<T>(this IEnumerable<T> self, Action<T> function)
	{
		foreach (var e in self)
			function(e);
	}

	public static T Clamp<T>(this T input, T min, T max) where T : IComparable<T>
	{
		T val = input;

		if (input.CompareTo(min) < 0)
			val = min;
		if (input.CompareTo(max) > 0)
			val = max;
		return val;
	}
}
