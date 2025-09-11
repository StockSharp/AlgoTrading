using System;
using System.Drawing;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Color helpers converted from the "HSV and HSL gradient Tools" TradingView library.
/// Provides functions for color conversion, inversion and gradient calculation.
/// </summary>
public class HsvAndHslGradientToolsStrategy : Strategy
{
	private static (decimal H, decimal S, decimal V, decimal A) Limit(decimal h, decimal s, decimal v, decimal a)
	{
		var hue = ((h % 360m) + 360m) % 360m;
		var sat = Math.Min(1m, Math.Max(0m, s));
		var val = Math.Min(1m, Math.Max(0m, v));
		var alpha = Math.Min(1m, Math.Max(0m, a));
		return (hue, sat, val, alpha);
	}

	private static decimal Hue(decimal red, decimal green, decimal blue)
	{
		var min = Math.Min(red, Math.Min(green, blue));
		var max = Math.Max(red, Math.Max(green, blue));
		var hue = 0m;
		if (min != max)
		{
			var delta = max - min;
			hue = max == red
				? (green - blue) / delta
				: max == green
					? 2m + (blue - red) / delta
					: 4m + (red - green) / delta;
			hue += 6m;
			hue *= 60m;
		}
		return hue;
	}

	public static (decimal H, decimal S, decimal V, decimal A) RgbToHsv(byte r, byte g, byte b, byte t = 0)
	{
		var red = r / 255m;
		var green = g / 255m;
		var blue = b / 255m;
		var min = Math.Min(red, Math.Min(green, blue));
		var max = Math.Max(red, Math.Max(green, blue));
		var value = max;
		var chroma = max - min;
		var sat = value == 0m ? 0m : chroma / value;
		var hue = chroma == 0m
			? 0m
			: value == red
				? 60m * ((green - blue) / chroma)
				: value == green
					? 60m * (2m + (blue - red) / chroma)
					: 60m * (4m + (red - green) / chroma);
		var alpha = 1m - t / 100m;
		return Limit(hue, sat, value, alpha);
	}

	public static (decimal H, decimal S, decimal L, decimal A) RgbToHsl(byte r, byte g, byte b, byte t = 0)
	{
		var red = r / 255m;
		var green = g / 255m;
		var blue = b / 255m;
		var min = Math.Min(red, Math.Min(green, blue));
		var max = Math.Max(red, Math.Max(green, blue));
		var lum = (max + min) / 2m;
		var chroma = max - min;
		var sat = chroma == 0m ? 0m : chroma / (1m - Math.Abs(2m * lum - 1m));
		var hue = Hue(red, green, blue);
		var alpha = 1m - t / 100m;
		return Limit(hue, sat, lum, alpha);
	}

	public static Color Hsv(decimal h, decimal s = 1m, decimal v = 1m, decimal a = 1m)
	{
		var data = Limit(h, s, v, a);
		var hue = data.H / 60m;
		var chroma = data.V * data.S;
		var sec = chroma * (1m - Math.Abs((hue % 2m) - 1m));
		var m = data.V - chroma;
		decimal r = 0m, g = 0m, b = 0m;
		var i = (int)Math.Floor(hue);
		switch (i)
		{
			case 0: r = chroma; g = sec; b = 0m; break;
			case 1: r = sec; g = chroma; b = 0m; break;
			case 2: r = 0m; g = chroma; b = sec; break;
			case 3: r = 0m; g = sec; b = chroma; break;
			case 4: r = sec; g = 0m; b = chroma; break;
			default: r = chroma; g = 0m; b = sec; break;
		}
		var alpha = (int)Math.Round((1m - data.A) * 100m);
		return Color.FromArgb(alpha, (int)Math.Round((m + r) * 255m), (int)Math.Round((m + g) * 255m), (int)Math.Round((m + b) * 255m));
	}

	public static Color Hsl(decimal h, decimal s = 1m, decimal l = 0.5m, decimal a = 1m)
	{
		var data = Limit(h, s, l, a);
		if (data.S == 0m)
		{
			var gray = (int)Math.Round(data.V * 255m);
			var alpha = (int)Math.Round((1m - data.A) * 100m);
			return Color.FromArgb(alpha, gray, gray, gray);
		}
		var q = data.V < 0.5m ? data.V * (1m + data.S) : data.V + data.S - data.V * data.S;
		var p = 2m * data.V - q;
		decimal Hue2Rgb(decimal t)
		{
			if (t < 0m)
				t += 1m;
			if (t > 1m)
				t -= 1m;
			if (t < 1m / 6m)
				return p + (q - p) * 6m * t;
			if (t < 1m / 2m)
				return q;
			if (t < 2m / 3m)
				return p + (q - p) * (2m / 3m - t) * 6m;
			return p;
		}
		var r = Hue2Rgb((data.H / 360m) + 1m / 3m);
		var g = Hue2Rgb(data.H / 360m);
		var b = Hue2Rgb((data.H / 360m) - 1m / 3m);
		var alpha = (int)Math.Round((1m - data.A) * 100m);
		return Color.FromArgb(alpha, (int)Math.Round(r * 255m), (int)Math.Round(g * 255m), (int)Math.Round(b * 255m));
	}

	private static decimal EaseBoth(decimal v)
	{
		return v < 0.5m
			? 4m * v * v * v
			: 1m - (decimal)Math.Pow((double)(-2m * v + 2m), 3) / 2m;
	}

	private static decimal EaseIn(decimal v)
	{
		return v == 0m ? 0m : (decimal)Math.Pow(2d, (double)(10m * v - 10m));
	}

	private static decimal EaseOut(decimal v)
	{
		return v == 1m ? 1m : 1m - (decimal)Math.Pow(2d, (double)(-10m * v));
	}

	private static decimal PercentOfDistance(decimal value, decimal start, decimal end)
	{
		var range = end - start;
		if (range == 0m)
			return 0m;
		return (value - start) / range;
	}

	public static Color HsvInvert(Color color)
	{
		var (h, s, v, a) = RgbToHsv(color.R, color.G, color.B, color.A);
		return Hsv(h, s, 1m - EaseBoth(v), a);
	}

	public static Color HslInvert(Color color)
	{
		var (h, s, l, a) = RgbToHsl(color.R, color.G, color.B, color.A);
		return Hsl(h, s, 1m - EaseBoth(l), a);
	}

	public static Color HsvGradient(decimal signal, decimal startVal, decimal endVal, Color startCol, Color endCol)
	{
		var (h1, s1, v1, a1) = RgbToHsv(startCol.R, startCol.G, startCol.B, startCol.A);
		var (h2, s2, v2, a2) = RgbToHsv(endCol.R, endCol.G, endCol.B, endCol.A);
		var pos = PercentOfDistance(signal, startVal, endVal);
		var hue = h1 + 360m + pos * ((h2 - h1 + 540m) % 360m - 180m);
		var sat = s1 + (s2 - s1) * pos;
		var val = v1 + (v2 - v1) * pos;
		var alpha = a1 + (a2 - a1) * pos;
		return Hsv(hue, sat, val, alpha);
	}

	public static Color HslGradient(decimal signal, decimal startVal, decimal endVal, Color startCol, Color endCol)
	{
		var (h1, s1, l1, a1) = RgbToHsl(startCol.R, startCol.G, startCol.B, startCol.A);
		var (h2, s2, l2, a2) = RgbToHsl(endCol.R, endCol.G, endCol.B, endCol.A);
		var pos = PercentOfDistance(signal, startVal, endVal);
		var hue = h1 + 360m + pos * ((h2 - h1 + 540m) % 360m - 180m);
		var sat = s1 + (s2 - s1) * pos;
		var lum = l1 + (l2 - l1) * pos;
		var alpha = a1 + (a2 - a1) * pos;
		return Hsl(hue, sat, lum, alpha);
	}
}
