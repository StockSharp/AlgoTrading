using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ErrorFunctionsStrategy : Strategy
{
	private const double Infinity = double.PositiveInfinity;

	private static double HastingsErf(double x)
	{
		var t = 1.0 / (1.0 + 0.3275911 * Math.Abs(x));
		var y = 1.0 - (((((1.061405429 * t - 1.453152027) * t + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * Math.Exp(-x * x));
		return x < 0 ? -y : y;
	}

	private static double GilesErfInv(double x)
	{
		var t = -Math.Log((1 - x) * (1 + x));
		if (t < 5)
		{
			t -= 2.5;
			return (((((((((2.81022636e-08 * t + 3.43273939e-07) * t - 3.5233877e-06) * t - 4.39150654e-06) * t + 0.00021858087) * t - 0.00125372503) * t - 0.00417768164) * t + 0.246640727) * t + 1.50140941) * x;
		}
		t = Math.Sqrt(t) - 3;
		return ((((((((-0.000200214257 * t + 0.000100950558) * t + 0.00134934322) * t - 0.00367342844) * t + 0.00573950773) * t - 0.0076224613) * t + 0.00943887047) * t + 1.00167406) * t + 2.83297682) * x;
	}

	private static double SunErfPolynomial(int i, double t)
	{
		double p, q;
		switch (i)
		{
			case 0:
				p = (((-2.37630166566501626084e-05 * t - 5.77027029648944159157e-03) * t - 2.84817495755985104766e-02) * t - 3.25042107247001499370e-01) * t + 1.28379167095512558561e-01;
				q = ((((-3.96022827877536812320e-06 * t + 1.32494738004321644526e-04) * t + 5.08130628187576562776e-03) * t + 6.50222499887672944485e-02) * t + 3.97917223959155352819e-01) * t + 1;
				break;
			case 1:
				p = (((((-2.16637559486879084300e-03 * t + 3.54783043256182359371e-02) * t - 1.10894694282396677476e-01) * t + 3.18346619901161753674e-01) * t - 3.72207876035701323847e-01) * t + 4.14856118683748331666e-01) * t - 2.36211856075265944077e-03;
				q = (((((1.19844998467991074170e-02 * t + 1.36370839120290507362e-02) * t + 1.26171219808761642112e-01) * t + 7.18286544141962662868e-02) * t + 5.40397917702171048937e-01) * t + 1.06420880400844228286e-01) * t + 1;
				break;
			case 2:
				p = (((((( -9.81432934416914548592e+00 * t - 8.12874355063065934246e+01) * t - 1.84605092906711035994e+02) * t - 1.62396669462573470355e+02) * t - 6.23753324503260060396e+01) * t - 1.05586262253232909814e+01) * t - 6.93858572707181764372e-01) * t - 9.86494403484714822705e-03;
				q = (((((((-6.04244152148580987438e-02 * t + 6.57024977031928170135e+00) * t + 1.08635005541779435134e+02) * t + 4.29008140027567833386e+02) * t + 6.45387271733267880336e+02) * t + 4.34565877475229228821e+02) * t + 1.37657754143519042600e+02) * t + 1.96512716674392571292e+01) * t + 1;
				break;
			default:
				p = ((((( -4.83519191608651397019e+02 * t - 1.02509513161107724954e+03) * t - 6.37566443368389627722e+02) * t - 1.60636384855821916062e+02) * t - 1.77579549177547519889e+01) * t - 7.99283237680523006574e-01) * t - 9.86494292470009928597e-03;
				q = (((((( -2.24409524465858183362e+01 * t + 4.74528541206955367215e+02) * t + 2.55305040643316442583e+03) * t + 3.19985821950859553908e+03) * t + 1.53672958608443695994e+03) * t + 3.25792512996573918826e+02) * t + 3.03380607434824582924e+01) * t + 1;
				break;
		}
		return p / q;
	}

	private static double SunErfXp(double z, double p)
	{
		const int c = 100000000;
		var f = Math.Floor(z * c) / c;
		return Math.Exp(-f * f - 0.5625) * Math.Exp((f - z) * (f + z) + p);
	}

	private static double SunErf(double x)
	{
		var X = Math.Abs(x);
		if (X < 0.84375)
		{
			if (X < 3.725290298e-9)
			return 1.28379167095512586316e-01 * x + x;
			return SunErfPolynomial(0, x * x) * x + x;
		}
		if (X < 1.25)
		{
			const double c = 8.45062911510467529297e-01;
			var p = SunErfPolynomial(1, X - 1);
			return x < 0 ? -c - p : c + p;
		}
		var pp = SunErfPolynomial(X < 1 / 0.35 ? 2 : 3, 1 / (x * x));
		var r = SunErfXp(x, pp);
		return x < 0 ? -r / x - 1 : 1 - r / x;
	}

	private static double SunErfc(double x)
	{
		var X = Math.Abs(x);
		if (X < 0.84375)
		{
			var p = SunErfPolynomial(0, x * x) * x;
			return X < 0.25 ? 1 - (x + p) : 0.5 - (p + (x - 0.5));
		}
		if (X < 1.25)
		{
			const double c = 8.45062911510467529297e-01;
			var p = SunErfPolynomial(1, X - 1);
			return x < 0 ? 1 + c + p : 1 - c - p;
		}
		var pp = SunErfPolynomial(X < 1 / 0.35 ? 2 : 3, 1 / (x * x));
		var r = SunErfXp(x, pp);
		return x > 0 ? r / x : 2 + r / x;
	}

	private static double BoostErfInvPolynomial(int i, double t, double y)
	{
		double p, q;
		switch (i)
		{
			case 0:
				p = (((((( -0.005387729650712429329650 * t + 0.008226878746769157431550 ) * t + 0.021987868111116889916500 ) * t - 0.036563797141176266400600) * t - 0.012692614766297402903400) * t + 0.033480662540974461503300) * t - 0.00836874819741736770379 ) * t - 0.000508781949658280665617;
				q = (((((((( 0.000886216390456424707504 * t - 0.002333937593741900167760 ) * t + 0.079528368734157168001800 ) * t - 0.052739638234009971395400) * t - 0.712289023415428475530000) * t + 0.662328840472002992063000) * t + 1.56221558398423026363000 ) * t - 1.565745582341758468090000) * t - 0.970005043303290640362) * t + 1;
				break;
			case 1:
				p = ((((((( -3.671922547077293485460000 * t + 21.129465544834052625800000 ) * t + 17.445385985570866523000000 ) * t - 44.638232444178696081800000) * t - 18.851064805871425189500000) * t + 17.644729840837401548600000) * t + 8.37050328343119927838000 ) * t + 0.105264680699391713268000) * t -0.202433508355938759655;
				q = ((((((( 1.721147657612002827240000 * t -22.643693341313972173600000 ) * t + 10.826866735546015900800000 ) * t + 48.560921310873993546800000) * t -20.143263468048518880100000) * t -28.660818049980002997400000) * t + 3.97134379533438690950000 ) * t + 6.242641248542475377120000) * t + 1;
				break;
			case 2:
				p = (((((((((-0.681149956853776992068e-9 * t + 0.285225331782217055858e-7 ) * t -0.679465575181126350155e-6 ) * t + 0.002145589953888052771690) * t + 0.029015791000532906043200) * t + 0.142869534408157156766000) * t + 0.33778553891203589892400 ) * t + 0.387079738972604337464000) * t + 0.117030156341995252019) * t -0.163794047193317060787) * t -0.131102781679951906451;
				q = (((((( 0.011059242293464891210000 * t + 0.152264338295331783612000 ) * t + 0.848854343457902036425000 ) * t + 2.593019216236202713740000) * t + 4.778465929458437783820000) * t + 5.381683457070068554250000) * t + 3.46625407242567245975000 ) * t + 1;
				break;
			case 3:
				p = ((((((( 0.266339227425782031962e-11 * t -0.230404776911882601748e-9 ) * t + 0.460469890584317994083e-5 ) * t + 0.000157544617424960554631) * t + 0.001871234928195592233450) * t + 0.009508047013259196036190) * t + 0.01855733065142310723240 ) * t -0.002224265292134479272810) * t -0.0350353787183177984712;
				q = ((((( 0.764675292302794483503e-4 * t + 0.002638616766570159929590 ) * t + 0.034158914367094772793400 ) * t + 0.220091105764131249824000) * t + 0.762059164553623404043000) * t + 1.365334981755406309700000) * t + 1;
				break;
			case 4:
				p = ((((((( 0.99055709973310326855e-16 * t -0.281128735628831791805e-13) * t + 0.462596163522878599135e-8 ) * t + 0.449696789927706453732e-6) * t + 0.149624783758342370182e-4) * t + 0.000209386317487588078668) * t + 0.00105628862152492910091 ) * t -0.001129514387455802788630) * t -0.0167431005076633737133;
				q = ((((( 0.282243172016108031869e-6 * t + 0.275335474764726041141e-4 ) * t + 0.000964011807005165528527 ) * t + 0.016074608709367650469500) * t + 0.138151865749083321638000) * t + 0.591429344886417493481000) * t + 1;
				break;
			case 5:
				p = (((((( -0.116765012397184275695e-17 * t + 0.145596286718675035587e-11) * t + 0.411632831190944208473e-9 ) * t + 0.396341011304801168516e-7) * t + 0.162397777342510920873e-5) * t + 0.254723037413027451751e-4) * t -0.779190719229053954292e-5) * t -0.0024978212791898131227;
				q = ((((( 0.509761276599778486139e-9 * t + 0.144437756628144157666e-6 ) * t + 0.145007359818232637924e-4 ) * t + 0.000690538265622684595676) * t + 0.016941083812097590647800) * t + 0.207123112214422517181000) * t + 1;
				break;
			default:
				p = (((((( -0.348890393399948882918e-21 * t + 0.135880130108924861008e-14) * t + 0.947846627503022684216e-12) * t + 0.225561444863500149219e-9) * t + 0.229345859265920864296e-7) * t + 0.899465114892291446442e-6) * t -0.28398759004727721098e-6 ) * t -0.000539042911019078575891;
				q = ((((( 0.231558608310259605225e-11 * t + 0.161809290887904476097e-8 ) * t + 0.399968812193862100054e-6 ) * t + 0.468292921940894236786e-4) * t + 0.002820929847262646819810) * t + 0.084574623400189943691400) * t + 1;
				break;
		}
		return p / q + y;
	}

	private static double BoostErfInvImp(double p, double q)
	{
		if (p <= 0.5)
		return BoostErfInvPolynomial(0, p, 0.0891314744949340820313) * (p * (p + 10));
		if (0.25 <= q)
		return Math.Sqrt(-2 * Math.Log(q)) / BoostErfInvPolynomial(1, q - 0.25, 2.249481201171875);
		var x = Math.Sqrt(-Math.Log(q));
		if (x < 3)
		return x * BoostErfInvPolynomial(2, x - 1.125, 0.807220458984375);
		if (x < 6)
		return x * BoostErfInvPolynomial(3, x - 3.0, 0.93995571136474609375);
		if (x < 18)
		return x * BoostErfInvPolynomial(4, x - 6.0, 0.98362827301025390625);
		if (x < 44)
		return x * BoostErfInvPolynomial(5, x - 18.0, 0.99714565277099609375);
		return x * BoostErfInvPolynomial(6, x - 44.0, 0.99941349029541015625);
	}

	private static double BoostErfInv(double x)
	{
		var p = Math.Abs(x);
		var q = 1 - p;
		var r = BoostErfInvImp(p, q);
		return x < 0 ? -r : r;
	}

	private static double BoostErfcInv(double x)
	{
		var q = x > 1 ? 2 - x : x;
		var p = 1 - q;
		var r = BoostErfInvImp(p, q);
		return x < 1 ? r : -r;
	}

	public static double Erf(double x, bool precise = true)
	{
		if (double.IsNaN(x))
		return double.NaN;
		var sign = Math.Sign(x);
		if (x == 0)
		return sign;
		if (Math.Abs(x) > 5.829395261518418)
		return sign * 0.9999999999999999;
		return precise ? SunErf(x) : HastingsErf(x);
	}

	public static double Erfc(double x, bool precise = true)
	{
		if (double.IsNaN(x))
		return double.NaN;
		if (x == 0)
		return Math.Sign(-x) + 1;
		if (Math.Abs(x) > 5.838230645058284)
		return x > 0 ? 0.0000000000000001 : 1.999999999999999;
		return precise ? SunErfc(x) : 1 - HastingsErf(x);
	}

	public static double ErfInv(double x, bool precise = true)
	{
		if (double.IsNaN(x) || x < -1 || x > 1)
		return double.NaN;
		if (x == 0)
		return 0;
		if (Math.Abs(x) > 0.9999999999)
		return Math.Sign(x) * Infinity;
		return precise ? BoostErfInv(x) : GilesErfInv(x);
	}

	public static double ErfcInv(double x, bool precise = true)
	{
		if (double.IsNaN(x) || x < 0 || x > 2)
		return double.NaN;
		var x1 = 1 - x;
		if (x1 == 0)
		return 0;
		if (Math.Abs(x1) > 0.9999999999)
		return Math.Sign(x1) * Infinity;
		return precise ? BoostErfcInv(x) : GilesErfInv(x1);
	}

	public static double Pdf(double x, double m, double s)
	{
		var d = x - m;
		return 1 / (s * Math.Sqrt(2 * Math.PI)) * Math.Exp(-(d * d / (2 * s * s)));
	}

	public static double Cdf(double z, bool precise = true)
	{
		return (1 + Erf(z / Math.Sqrt(2), precise)) / 2;
	}

	public static double CdfInv(double a, bool precise = true)
	{
		return ErfInv(2 * a - 1, precise) * Math.Sqrt(2);
	}

	public static double CdfAb(double z1, double z2, bool precise = true)
	{
		return Cdf(Math.Max(z1, z2), precise) - Cdf(Math.Min(z1, z2), precise);
	}

	public static double Ttt(double z, bool precise = true)
	{
		return Erfc(Math.Abs(z) / Math.Sqrt(2), precise);
	}

	public static double TttInv(double a, bool precise = true)
	{
		return Math.Abs(ErfcInv(a, precise) * Math.Sqrt(2));
	}

	public static double Ott(double z, bool precise = true)
	{
		return Ttt(z, precise) / 2;
	}

	public static double OttInv(double a, bool precise = true)
	{
		return Math.Abs(TttInv(a * 2, precise));
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield break;
	}
}
