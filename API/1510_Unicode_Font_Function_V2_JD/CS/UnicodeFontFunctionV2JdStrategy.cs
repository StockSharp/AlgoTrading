using System;
using System.Text;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Unicode font conversion strategy.
/// Converts input strings to selected unicode font styles.
/// </summary>
public class UnicodeFontFunctionV2JdStrategy : Strategy
{
	private readonly StrategyParam<string> _input1;
	private readonly StrategyParam<string> _input2;
	private readonly StrategyParam<string> _fontType1;
	private readonly StrategyParam<string> _fontType2;

	private static readonly string[] _stdLower = "a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z".Split(',');
	private static readonly string[] _stdUpper = "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z".Split(',');
	private static readonly string[] _stdDigits = "0,1,2,3,4,5,6,7,8,9".Split(',');

	/// <summary>
	/// First input string.
	/// </summary>
	public string Input1
	{
		get => _input1.Value;
		set => _input1.Value = value;
	}

	/// <summary>
	/// Second input string.
	/// </summary>
	public string Input2
	{
		get => _input2.Value;
		set => _input2.Value = value;
	}

	/// <summary>
	/// Font for first string.
	/// </summary>
	public string FontType1
	{
		get => _fontType1.Value;
		set => _fontType1.Value = value;
	}

	/// <summary>
	/// Font for second string.
	/// </summary>
	public string FontType2
	{
		get => _fontType2.Value;
		set => _fontType2.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="UnicodeFontFunctionV2JdStrategy"/>.
	/// </summary>
	public UnicodeFontFunctionV2JdStrategy()
	{
		_input1 = Param(nameof(Input1), "This function brought to you by")
			.SetDisplay("Input 1", "First string", "General");

		_input2 = Param(nameof(Input2), "Please input your custom text, then try to change the font type also.")
			.SetDisplay("Input 2", "Second string", "General");

		_fontType1 = Param(nameof(FontType1), "Sans Bold Italic")
			.SetDisplay("Font Type 1", "Font for first string", "General");

		_fontType2 = Param(nameof(FontType2), "Regional Indicator")
			.SetDisplay("Font Type 2", "Font for second string", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var first = FontTypeSelector(Input1, FontType1);
		var second = FontTypeSelector(Input2, FontType2);

		AddInfo("Converted 1: {0}", first);
		AddInfo("Converted 2: {0}", second);
	}

	private static string FontTypeSelector(string text, string fontType)
	{
		var (lower, upper, digits) = GetFontSet(fontType);
		var sb = new StringBuilder();

		foreach (var ch in text)
		{
			if (ch >= 'a' && ch <= 'z')
				sb.Append(lower[ch - 'a']);
			else if (ch >= 'A' && ch <= 'Z')
				sb.Append(upper[ch - 'A']);
			else if (ch >= '0' && ch <= '9')
				sb.Append(digits[ch - '0']);
			else
				sb.Append(ch);
		}

		return sb.ToString();
	}

	private static (string[] lower, string[] upper, string[] digits) GetFontSet(string fontType)
	{
		return fontType switch
		{
			"Sans" => (
				"𝖺,𝖻,𝖼,𝖽,𝖾,𝖿,𝗀,𝗁,𝗂,𝗃,𝗄,𝗅,𝗆,𝗇,𝗈,𝗉,𝗊,𝗋,𝗌,𝗍,𝗎,𝗏,𝗐,𝗑,𝗒,𝗓".Split(','),
				"𝖠,𝖡,𝖢,𝖣,𝖤,𝖥,𝖦,𝖧,𝖨,𝖩,𝖪,𝖫,𝖬,𝖭,𝖮,𝖯,𝖰,𝖱,𝖲,𝖳,𝖴,𝖵,𝖶,𝖷,𝖸,𝖹".Split(','),
				"𝟢,𝟣,𝟤,𝟥,𝟦,𝟧,𝟨,𝟩,𝟪,𝟫".Split(',')
			),
			"Sans Italic" => (
				"𝘢,𝘣,𝘤,𝘥,𝘦,𝘧,𝘨,𝘩,𝘪,𝘫,𝘬,𝘭,𝘮,𝘯,𝘰,𝘱,𝘲,𝘳,𝘴,𝘵,𝘶,𝘷,𝘸,𝘹,𝘺,𝘻".Split(','),
				"𝘈,𝘉,𝘊,𝘋,𝘌,𝘍,𝘎,𝘏,𝘐,𝘑,𝘒,𝘓,𝘔,𝘕,𝘖,𝘗,𝘘,𝘙,𝘚,𝘛,𝘜,𝘝,𝘞,𝘟,𝘠,𝘡".Split(','),
				"𝟢,𝟣,𝟤,𝟥,𝟦,𝟧,𝟨,𝟩,𝟪,𝟫".Split(',')
			),
			"Sans Bold" => (
				"𝗮,𝗯,𝗰,𝗱,𝗲,𝗳,𝗴,𝗵,𝗶,𝗷,𝗸,𝗹,𝗺,𝗻,𝗼,𝗽,𝗾,𝗿,𝘀,𝘁,𝘂,𝘃,𝘄,𝘅,𝘆,𝘇".Split(','),
				"𝗔,𝗕,𝗖,𝗗,𝗘,𝗙,𝗚,𝗛,𝗜,𝗝,𝗞,𝗟,𝗠,𝗡,𝗢,𝗣,𝗤,𝗥,𝗦,𝗧,𝗨,𝗩,𝗪,𝗫,𝗬,𝗭".Split(','),
				"𝟬,𝟭,𝟮,𝟯,𝟰,𝟱,𝟲,𝟳,𝟴,𝟵".Split(',')
			),
			"Sans Bold Italic" => (
				"𝙖,𝙗,𝙘,𝙙,𝙚,𝙛,𝙜,𝙝,𝙞,𝙟,𝙠,𝙡,𝙢,𝙣,𝙤,𝙥,𝙦,𝙧,𝙨,𝙩,𝙪,𝙫,𝙬,𝙭,𝙮,𝙯".Split(','),
				"𝘼,𝘽,𝘾,𝘿,𝙀,𝙁,𝙂,𝙃,𝙄,𝙅,𝙆,𝙇,𝙈,𝙉,𝙊,𝙋,𝙌,𝙍,𝙎,𝙏,𝙐,𝙑,𝙒,𝙓,𝙔,𝙕".Split(','),
				"𝟬,𝟭,𝟮,𝟯,𝟰,𝟱,𝟲,𝟳,𝟴,𝟵".Split(',')
			),
			"Sans-Serif" => (
				"𝚊,𝚋,𝚌,𝚍,𝚎,𝚏,𝚐,𝚑,𝚒,𝚓,𝚔,𝚕,𝚖,𝚗,𝚘,𝚙,𝚚,𝚛,𝚜,𝚝,𝚞,𝚟,𝚠,𝚡,𝚢,𝚣".Split(','),
				"𝙰,𝙱,𝙲,𝙳,𝙴,𝙵,𝙶,𝙷,𝙸,𝙹,𝙺,𝙻,𝙼,𝙽,𝙾,𝙿,𝚀,𝚁,𝚂,𝚃,𝚄,𝚅,𝚆,𝚇,𝚈,𝚉".Split(','),
				"𝟶,𝟷,𝟸,𝟹,𝟺,𝟻,𝟼,𝟽,𝟾,𝟿".Split(',')
			),
			"Sans-Serif Italic" => (
				"𝘢,𝘣,𝘤,𝘥,𝘦,𝘧,𝘨,𝘩,𝘪,𝘫,𝘬,𝘭,𝘮,𝘯,𝘰,𝘱,𝘲,𝘳,𝘴,𝘵,𝘶,𝘷,𝘸,𝘹,𝘺,𝘻".Split(','),
				"𝘈,𝘉,𝘊,𝘋,𝘌,𝘍,𝘎,𝘏,𝘐,𝘑,𝘒,𝘓,𝘔,𝘕,𝘖,𝘗,𝘘,𝘙,𝘚,𝘛,𝘜,𝘝,𝘞,𝘟,𝘠,𝘡".Split(','),
				"𝟶,𝟷,𝟸,𝟹,𝟺,𝟻,𝟼,𝟽,𝟾,𝟿".Split(',')
			),
			"Sans-Serif Bold" => (
				"𝐚,𝐛,𝐜,𝐝,𝐞,𝐟,𝐠,𝐡,𝐢,𝐣,𝐤,𝐥,𝐦,𝐧,𝐨,𝐩,𝐪,𝐫,𝐬,𝐭,𝐮,𝐯,𝐰,𝐱,𝐲,𝐳".Split(','),
				"𝐀,𝐁,𝐂,𝐃,𝐄,𝐅,𝐆,𝐇,𝐈,𝐉,𝐊,𝐋,𝐌,𝐍,𝐎,𝐏,𝐐,𝐑,𝐒,𝐓,𝐔,𝐕,𝐖,𝐗,𝐘,𝐙".Split(','),
				"𝟎,𝟏,𝟐,𝟑,𝟒,𝟓,𝟔,𝟕,𝟖,𝟗".Split(',')
			),
			"Sans-Serif Bold Italic" => (
				"𝒂,𝒃,𝒄,𝒅,𝒆,𝒇,𝒈,𝒉,𝒊,𝒋,𝒌,𝒍,𝒎,𝒏,𝒐,𝒑,𝒒,𝒓,𝒔,𝒕,𝒖,𝒗,𝒘,𝒙,𝒚,𝒛".Split(','),
				"𝑨,𝑩,𝑪,𝑫,𝑬,𝑭,𝑮,𝑯,𝑰,𝑱,𝑲,𝑳,𝑴,𝑵,𝑶,𝑷,𝑸,𝑹,𝑺,𝑻,𝑼,𝑽,𝑾,𝑿,𝒀,𝒁".Split(','),
				"𝟎,𝟏,𝟐,𝟑,𝟒,𝟓,𝟔,𝟕,𝟖,𝟗".Split(',')
			),
			"Fraktur" => (
				"𝔞,𝔟,𝔠,𝔡,𝔢,𝔣,𝔤,𝔥,𝔦,𝔧,𝔨,𝔩,𝔪,𝔫,𝔬,𝔭,𝔮,𝔯,𝔰,𝔱,𝔲,𝔳,𝔴,𝔵,𝔶,𝔷".Split(','),
				"𝔄,𝔅,ℭ,𝔇,𝔈,𝔉,𝔊,ℌ,ℑ,𝔍,𝔎,𝔏,𝔐,𝔑,𝔒,𝔓,𝔔,ℜ,𝔖,𝔗,𝔘,𝔙,𝔚,𝔛,𝔜,ℨ".Split(','),
				"𝟢,𝟣,𝟤,𝟥,𝟦,𝟧,𝟨,𝟩,𝟪,𝟫".Split(',')
			),
			"Fraktur Bold" => (
				"𝖆,𝖇,𝖈,𝖉,𝖊,𝖋,𝖌,𝖍,𝖎,𝖏,𝖐,𝖑,𝖒,𝖓,𝖔,𝖕,𝖖,𝖗,𝖘,𝖙,𝖚,𝖛,𝖜,𝖝,𝖞,𝖟".Split(','),
				"𝕬,𝕭,𝕮,𝕯,𝕰,𝕱,𝕲,𝕳,𝕴,𝕵,𝕶,𝕷,𝕸,𝕹,𝕺,𝕻,𝕼,𝕽,𝕾,𝕿,𝖀,𝖁,𝖂,𝖃,𝖄,𝖅".Split(','),
				"𝟎,𝟏,𝟐,𝟑,𝟒,𝟓,𝟔,𝟕,𝟖,𝟗".Split(',')
			),
			"Script" => (
				"𝒶,𝒷,𝒸,𝒹,ℯ,𝒻,ℊ,𝒽,𝒾,𝒿,𝓀,𝓁,𝓂,𝓃,ℴ,𝓅,𝓆,𝓇,𝓈,𝓉,𝓊,𝓋,𝓌,𝓍,𝓎,𝓏".Split(','),
				"𝒜,ℬ,𝒞,𝒟,ℰ,ℱ,𝒢,ℋ,ℐ,𝒥,𝒦,ℒ,ℳ,𝒩,𝒪,𝒫,𝒬,ℛ,𝒮,𝒯,𝒰,𝒱,𝒲,𝒳,𝒴,𝒵".Split(','),
				"𝟢,𝟣,𝟤,𝟥,𝟦,𝟧,𝟨,𝟩,𝟪,𝟫".Split(',')
			),
			"Script Bold" => (
				"𝓪,𝓫,𝓬,𝓭,𝓮,𝓯,𝓰,𝓱,𝓲,𝓳,𝓴,𝓵,𝓶,𝓷,𝓸,𝓹,𝓺,𝓻,𝓼,𝓽,𝓾,𝓿,𝔀,𝔁,𝔂,𝔃".Split(','),
				"𝓐,𝓑,𝓒,𝓓,𝓔,𝓕,𝓖,𝓗,𝓘,𝓙,𝓚,𝓛,𝓜,𝓝,𝓞,𝓟,𝓠,𝓡,𝓢,𝓣,𝓤,𝓥,𝓦,𝓧,𝓨,𝓩".Split(','),
				"𝟎,𝟏,𝟐,𝟑,𝟒,𝟓,𝟔,𝟕,𝟖,𝟗".Split(',')
			),
			"Double-Struck" => (
				"𝕒,𝕓,𝕔,𝕕,𝕖,𝕗,𝕘,𝕙,𝕚,𝕛,𝕜,𝕝,𝕞,𝕟,𝕠,𝕡,𝕢,𝕣,𝕤,𝕥,𝕦,𝕧,𝕨,𝕩,𝕪,𝕫".Split(','),
				"𝔸,𝔹,ℂ,𝔻,𝔼,𝔽,𝔾,ℍ,𝕀,𝕁,𝕂,𝕃,𝕄,ℕ,𝕆,ℙ,ℚ,ℝ,𝕊,𝕋,𝕌,𝕍,𝕎,𝕏,𝕐,ℤ".Split(','),
				"𝟘,𝟙,𝟚,𝟛,𝟜,𝟝,𝟞,𝟟,𝟠,𝟡".Split(',')
			),
			"Monospace" => (
				"𝚊,𝚋,𝚌,𝚍,𝚎,𝚏,𝚐,𝚑,𝚒,𝚓,𝚔,𝚕,𝚖,𝚗,𝚘,𝚙,𝚚,𝚛,𝚜,𝚝,𝚞,𝚟,𝚠,𝚡,𝚢,𝚣".Split(','),
				"𝙰,𝙱,𝙲,𝙳,𝙴,𝙵,𝙶,𝙷,𝙸,𝙹,𝙺,𝙻,𝙼,𝙽,𝙾,𝙿,𝚀,𝚁,𝚂,𝚃,𝚄,𝚅,𝚆,𝚇,𝚈,𝚉".Split(','),
				"𝟶,𝟷,𝟸,𝟹,𝟺,𝟻,𝟼,𝟽,𝟾,𝟿".Split(',')
			),
			"Regional Indicator" => (
				"🇦,🇧,🇨,🇩,🇪,🇫,🇬,🇭,🇮,🇯,🇰,🇱,🇲,🇳,🇴,🇵,🇶,🇷,🇸,🇹,🇺,🇻,🇼,🇽,🇾,🇿".Split(','),
				"🇦,🇧,🇨,🇩,🇪,🇫,🇬,🇭,🇮,🇯,🇰,🇱,🇲,🇳,🇴,🇵,🇶,🇷,🇸,🇹,🇺,🇻,🇼,🇽,🇾,🇿".Split(','),
				"𝟶,𝟷,𝟸,𝟹,𝟺,𝟻,𝟼,𝟽,𝟾,𝟿".Split(',')
			),
			"Full Width" => (
				"ａ,ｂ,ｃ,ｄ,ｅ,ｆ,ｇ,ｈ,ｉ,ｊ,ｋ,ｌ,ｍ,ｎ,ｏ,ｐ,ｑ,ｒ,ｓ,ｔ,ｕ,ｖ,ｗ,ｘ,ｙ,ｚ".Split(','),
				"Ａ,Ｂ,Ｃ,Ｄ,Ｅ,Ｆ,Ｇ,Ｈ,Ｉ,Ｊ,Ｋ,Ｌ,Ｍ,Ｎ,Ｏ,Ｐ,Ｑ,Ｒ,Ｓ,Ｔ,Ｕ,Ｖ,Ｗ,Ｘ,Ｙ,Ｚ".Split(','),
				"０,１,２,３,４,５,６,７,８,９".Split(',')
			),
			"Circled" => (
				"🅐,🅑,🅒,🅓,🅔,🅕,🅖,🅗,🅘,🅙,🅚,🅛,🅜,🅝,🅞,🅟,🅠,🅡,🅢,🅣,🅤,🅥,🅦,🅧,🅨,🅩".Split(','),
				"🅐,🅑,🅒,🅓,🅔,🅕,🅖,🅗,🅘,🅙,🅚,🅛,🅜,🅝,🅞,🅟,🅠,🅡,🅢,🅣,🅤,🅥,🅦,🅧,🅨,🅩".Split(','),
				"⓿,❶,❷,❸,❹,❺,❻,❼,❽,❾".Split(',')
			),
			_ => (_stdLower, _stdUpper, _stdDigits)
		};
	}
}
