using System;
using System.Collections.Generic;
using System.Text;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Utility strategy providing text font conversions.
/// Only a subset of fonts is implemented.
/// </summary>
public class TextStrategy : Strategy
{
	/// <summary>
	/// Supported fonts.
	/// </summary>
	public enum EFont
	{
		BoldStrong,
		Circled
	}

	/// <summary>
	/// Transform text into the selected font.
	/// </summary>
	/// <param name="fromText">Initial text.</param>
	/// <param name="font">Desired font.</param>
	/// <returns>Text transformed to the selected font.</returns>
	public static string ToFont(string fromText, EFont font)
	{
		const string pine = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		string[] map;

		switch (font)
		{
			case EFont.BoldStrong:
				map = new[]
				{
					"α","Ⴆ","ƈ","ԃ","ҽ","ϝ","ɠ","ԋ","ι","ʝ","ƙ","ʅ","ɱ","ɳ","σ","ρ","ϙ","ɾ","ʂ","ƚ","υ","ʋ","ɯ","𝐱","ყ","ȥ",
					"𝐀","𝐁","𝐂","𝐃","𝐄","𝐅","𝐆","𝐇","𝐈","𝐉","𝐊","𝐋","𝐌","𝐍","𝐎","𝐏","𝐐","𝐑","𝐒","𝐓","𝐔","𝐕","𝐖","𝐗","𝐘","𝐙",
					"𝟎","𝟏","𝟐","𝟑","𝟒","𝟓","𝟔","𝟕","𝟖","𝟗"
				};
				break;
			case EFont.Circled:
				map = new[]
				{
					"🅐","🅑","🅒","🅓","🅔","🅕","🅖","🅗","🅘","🅙","🅚","🅛","🅜","🅝","🅞","🅟","🅠","🅡","🅢","🅣","🅤","🅥","🅦","🅧","🅨","🅩",
					"🅐","🅑","🅒","🅓","🅔","🅕","🅖","🅗","🅘","🅙","🅚","🅛","🅜","🅝","🅞","🅟","🅠","🅡","🅢","🅣","🅤","🅥","🅦","🅧","🅨","🅩",
					"⓿","❶","❷","❸","❹","❺","❻","❼","❽","❾"
				};
				break;
			default:
				return fromText;
		}

		var dict = new Dictionary<char, string>();
		for (var i = 0; i < pine.Length; i++)
			dict[pine[i]] = map[i];

		var sb = new StringBuilder();
		foreach (var ch in fromText)
			sb.Append(dict.TryGetValue(ch, out var val) ? val : ch.ToString());

		return sb.ToString();
	}
}
