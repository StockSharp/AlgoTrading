namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Text;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Demonstrates Unicode font conversion utilities.
/// </summary>
public class UnicodeFontFunctionJdStrategy : Strategy
{
	private readonly StrategyParam<string> _inputString;
	private readonly StrategyParam<bool> _upperCase;
	private readonly StrategyParam<bool> _bold;
	private readonly StrategyParam<bool> _italic;
	
	public string InputString
	{
		get => _inputString.Value;
		set => _inputString.Value = value;
	}
	
	public bool UpperCase
	{
		get => _upperCase.Value;
		set => _upperCase.Value = value;
	}
	
	public bool Bold
	{
		get => _bold.Value;
		set => _bold.Value = value;
	}
	
	public bool Italic
	{
		get => _italic.Value;
		set => _italic.Value = value;
	}
	
	public UnicodeFontFunctionJdStrategy()
	{
		_inputString = Param(nameof(InputString), "input string here")
		.SetDisplay("Input", "Text to convert", "General");
		_upperCase = Param(nameof(UpperCase), false)
		.SetDisplay("Upper Case", "Convert to upper case", "General");
		_bold = Param(nameof(Bold), false)
		.SetDisplay("Bold", "Use bold style", "General");
		_italic = Param(nameof(Italic), false)
		.SetDisplay("Italic", "Use italic style", "General");
	}
	
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [];
	}
	
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		var converted = ConvertText(InputString, UpperCase, Bold, Italic);
		LogInfo("{0} -> {1}", InputString, converted);
	}
	
	private static readonly string[] StdLc = "𝚊,𝚋,𝚌,𝚍,𝚎,𝚏,𝚐,𝚑,𝚒,𝚓,𝚔,𝚕,𝚖,𝚗,𝚘,𝚙,𝚚,𝚛,𝚜,𝚝,𝚞,𝚟,𝚠,𝚡,𝚢,𝚣".Split(',');
	private static readonly string[] StdUc = "𝙰,𝙱,𝙲,𝙳,𝙴,𝙵,𝙶,𝙷,𝙸,𝙹,𝙺,𝙻,𝙼,𝙽,𝙾,𝙿,𝚀,𝚁,𝚂,𝚃,𝚄,𝚅,𝚆,𝚇,𝚈,𝚉".Split(',');
	private static readonly string[] StdDigits = "𝟶,𝟷,𝟸,𝟹,𝟺,𝟻,𝟼,𝟽,𝟾,𝟿".Split(',');
	private static readonly string[] StdItLc = "𝘢,𝘣,𝘤,𝘥,𝘦,𝘧,𝘨,𝘩,𝘪,𝘫,𝘬,𝘭,𝘮,𝘯,𝘰,𝘱,𝘲,𝘳,𝘴,𝘵,𝘶,𝘷,𝘸,𝘹,𝘺,𝘻".Split(',');
	private static readonly string[] StdItUc = "𝘈,𝘉,𝘊,𝘋,𝘌,𝘍,𝘎,𝘏,𝘐,𝘑,𝘒,𝘓,𝘔,𝘕,𝘖,𝘗,𝘘,𝘙,𝘚,𝘛,𝘜,𝘝,𝘞,𝘟,𝘠,𝘡".Split(',');
	private static readonly string[] StdItDigits = "𝟢,𝟣,𝟤,𝟥,𝟦,𝟧,𝟨,𝟩,𝟪,𝟫".Split(',');
	private static readonly string[] BoldLc = "𝗮,𝗯,𝗰,𝗱,𝗲,𝗳,𝗴,𝗵,𝗶,𝗷,𝗸,𝗹,𝗺,𝗻,𝗼,𝗽,𝗾,𝗿,𝘀,𝘁,𝘂,𝘃,𝘄,𝘅,𝘆,𝘇".Split(',');
	private static readonly string[] BoldUc = "𝗔,𝗕,𝗖,𝗗,𝗘,𝗙,𝗚,𝗛,𝗜,𝗝,𝗞,𝗟,𝗠,𝗡,𝗢,𝗣,𝗤,𝗥,𝗦,𝗧,𝗨,𝗩,𝗪,𝗫,𝗬,𝗭".Split(',');
	private static readonly string[] BoldDigits = "𝟬,𝟭,𝟮,𝟯,𝟰,𝟱,𝟲,𝟳,𝟴,𝟵".Split(',');
	private static readonly string[] BoldItLc = "𝙖,𝙗,𝙘,𝙙,𝙚,𝙛,𝙜,𝙝,𝙞,𝙟,𝙠,𝙡,𝙢,𝙣,𝙤,𝙥,𝙦,𝙧,𝙨,𝙩,𝙪,𝙫,𝙬,𝙭,𝙮,𝙯".Split(',');
	private static readonly string[] BoldItUc = "𝘼,𝘽,𝘾,𝘿,𝙀,𝙁,𝙂,𝙃,𝙄,𝙅,𝙆,𝙇,𝙈,𝙉,𝙊,𝙋,𝙌,𝙍,𝙎,𝙏,𝙐,𝙑,𝙒,𝙓,𝙔,𝙕".Split(',');
	private static readonly string[] BoldItDigits = "𝟬,𝟭,𝟮,𝟯,𝟰,𝟱,𝟲,𝟳,𝟴,𝟵".Split(',');
	
	private static string ConvertText(string text, bool upper, bool bold, bool italic)
	{
		var lc = bold ? (italic ? BoldItLc : BoldLc) : italic ? StdItLc : StdLc;
		var uc = bold ? (italic ? BoldItUc : BoldUc) : italic ? StdItUc : StdUc;
		var dg = bold ? (italic ? BoldItDigits : BoldDigits) : italic ? StdItDigits : StdDigits;
		
		var sb = new StringBuilder(text.Length);
		foreach (var ch in text)
		{
			if (char.IsDigit(ch))
			{
				var idx = ch - '0';
				sb.Append(idx >= 0 && idx < dg.Length ? dg[idx] : ch);
			}
			else if (char.IsLetter(ch))
			{
				if (upper)
				{
					var idx = char.ToLowerInvariant(ch) - 'a';
					sb.Append(idx >= 0 && idx < uc.Length ? uc[idx] : ch);
				}
				else if (char.IsLower(ch))
				{
					var idx = ch - 'a';
					sb.Append(idx >= 0 && idx < lc.Length ? lc[idx] : ch);
				}
				else
				{
					var idx = ch - 'A';
					sb.Append(idx >= 0 && idx < uc.Length ? uc[idx] : ch);
				}
			}
			else
			{
				sb.Append(ch);
			}
		}
		return sb.ToString();
	}
}
