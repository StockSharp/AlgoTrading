using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

public class ImlibStrategy : Strategy
{
	public class ImgData
	{
		public int Width { get; }
		public int Height { get; }
		public string Symbols { get; }
		public string Palette { get; }
		public List<string> Data { get; }

		public ImgData(int width, int height, string symbols, string palette, List<string> data)
		{
			Width = width;
			Height = height;
			Symbols = symbols;
			Palette = palette;
			Data = data;
		}
	}

	private static List<string> Decompress(string data)
	{
		var arr = new List<string>();
		var res = string.Empty;
		var num = string.Empty;

		for (var i = 0; i < data.Length; i++)
		{
			var ch = data.Substring(i, 1);
			var isNum = int.TryParse(ch, out _);

			if (isNum)
			num += ch;

			if (!isNum)
			{
				if (num != string.Empty)
				{
					var numInt = int.Parse(num);
					for (var j = 0; j < numInt - 1; j++)
					{
						if (res.Length == 4096)
						{
							arr.Add(res);
							res = string.Empty;
						}

						res += ch;
					}

					num = string.Empty;
				}

				if (num == string.Empty)
				{
					if (res.Length == 4096)
					{
						arr.Add(res);
						res = string.Empty;
					}

					res += ch;
				}
			}
		}

		if (res != string.Empty)
		arr.Add(res);

		return arr;
	}

	public static ImgData Load(string data)
	{
		var parts = data.Split("⫝");
		var size = parts[0];
		var wChar = size.Substring(0, 1);
		var hChar = size.Substring(1, 1);
		var p2 = parts[1];
		var s = p2[..256];
		var pal = p2[256..];
		var da = Decompress(parts[2]);
		var w = s.IndexOf(wChar, StringComparison.Ordinal);
		var h = s.IndexOf(hChar, StringComparison.Ordinal);
		return new ImgData(w, h, s, pal, da);
	}

	public static void Show(ImgData imgData, Action<int, int, string> setPixel, double imageSize = 20.0, string screenRatio = "16/8.5")
	{
		var palette = new List<string>();
		var r = -1;
		var g = -1;
		var b = -1;
		var t = -1;
		var curCol = 0;
		var curRow = 0;

		for (var i = 0; i < imgData.Palette.Length; i++)
		{
			var ch = imgData.Palette.Substring(i, 1);
			var done = false;

			if (r == -1)
			{
				r = imgData.Symbols.IndexOf(ch, StringComparison.Ordinal);
				done = true;
			}

			if (g == -1 && !done)
			{
				g = imgData.Symbols.IndexOf(ch, StringComparison.Ordinal);
				done = true;
			}

			if (b == -1 && !done)
			{
				b = imgData.Symbols.IndexOf(ch, StringComparison.Ordinal);
				done = true;
			}

			if (t == -1 && !done)
			{
				t = imgData.Symbols.IndexOf(ch, StringComparison.Ordinal);
				palette.Add($"rgba({r},{g},{b},{100 - t})");
				r = g = b = t = -1;
			}
		}

		double ratio;
		var split = screenRatio.Split("/");
		if (split.Length == 2)
		ratio = double.Parse(split[0]) / double.Parse(split[1]);
		else
		ratio = double.Parse(split[0]);

		var pixelSize = imageSize / Math.Max(imgData.Height, imgData.Width);

		foreach (var block in imgData.Data)
		{
			for (var j = 0; j < block.Length; j++)
			{
				var ch = block.Substring(j, 1);
				var pos = imgData.Symbols.IndexOf(ch, StringComparison.Ordinal);

				if (curCol == imgData.Width)
				{
					curRow++;
					curCol = 0;
				}

				if (pos >= 0 && pos < palette.Count)
				setPixel(curCol, curRow, palette[pos]);

				curCol++;
			}
		}
	}

	public static void Logo(string imgData, Action<int, int, string> setPixel, double imageSize = 20.0, string screenRatio = "16/9")
	{
		var img = Load(imgData);
		Show(img, setPixel, imageSize, screenRatio);
	}
}
