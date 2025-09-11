namespace StockSharp.Samples.Strategies;

/// <summary>
/// Collection of mathematical constants.
/// </summary>
public static class MathConstantsStrategy
{
	/// <summary>
	/// Euler's Number. Limit of `(1 + 1/n)^n` as `n` approaches infinity; transcendental number approximately `2.718` or a angle of `156º`.
	/// </summary>
	public const double E = 2.7182818284590452353602874713526624977572470937000;

	/// <summary>
	/// Base 2 logarithm of `e`, `log[2](e)`, approximately `1.442` or a angle of `83º`.
	/// </summary>
	public const double Log2E = 1.4426950408889634073599246810018921374266459541530;

	/// <summary>
	/// Base 10 logarithm of `e`, `log[10](e)`, approximately `0.434` or a angle of `25º`.
	/// </summary>
	public const double Log10E = 0.43429448190325182765112891891660508229439700580366;

	/// <summary>
	/// Natural logarithm of 2, `log[e](2)`, approximately 0.693 or a angle of `40º`. The natural logarithm of x is the power to which e would have to be raised to equal x.
	/// </summary>
	public const double Ln2 = 0.69314718055994530941723212145817656807550013436026;

	/// <summary>
	/// Natural logarithm of 10, `log[e](10)`, approximately `2.302` or a angle of `132º`. The natural logarithm of x is the power to which e would have to be raised to equal x.
	/// </summary>
	public const double Ln10 = 2.3025850929940456840179914546843642076011014886288;

	/// <summary>
	/// Natural logarithm of `π`, `log[e](pi)`, approximately `1.144` or a angle of `66º`. The natural logarithm of x is the power to which e would have to be raised to equal x.
	/// </summary>
	public const double LnPi = 1.1447298858494001741434273513530587116472948129153;

	/// <summary>
	/// Natural logarithm of `2π/2`, `log[e](2*pi)/2`, approximately `0.918` or a angle of `53º`
	/// </summary>
	public const double Ln2PiOver2 = 0.91893853320467274178032973640561763986139747363780;

	/// <summary>
	/// Reciprocal of `e`, `1/e`, approximately `0.367` or a angle of `21º`.
	/// </summary>
	public const double InvE = 0.36787944117144232159552377016146086744581113103176;

	/// <summary>
	/// Square root of `e`, `sqrt(e)`, approximately `1.648` or a angle of `95º`.
	/// </summary>
	public const double SqrtE = 1.6487212707001281468486507878141635716537761007101;

	/// <summary>
	/// Pythagoras Constant, `sqrt(2)`, approximately `1.414` or a angle of `81º`.
	/// </summary>
	public const double Sqrt2 = 1.4142135623730950488016887242096980785696718753769;

	/// <summary>
	/// Theodorus Constant, `sqrt(3)`, approximately `1.732` or a angle of `99º`.
	/// </summary>
	public const double Sqrt3 = 1.7320508075688772935274463415058723669428052538104;

	/// <summary>
	/// Square root of `1/2`, `sqrt(1/2)` = `1/sqrt(2)` = `sqrt(2)/2`, approximately `0.707` or a angle of `40º`.
	/// </summary>
	public const double Sqrt1Over2 = 0.70710678118654752440084436210484903928483593768845;

	/// <summary>
	/// Half the square root of `3`, `sqrt(3)/2`, approximately `0.866` or a angle of `50º`.
	/// </summary>
	public const double HalfSqrt3 = 0.86602540378443864676372317075293618347140262690520;

	/// <summary>
	/// The number `pi`, mathemathical constant representing the ratio of a circle's circumference to its diameter, approximately `3.141` or a angle of `180º`.
	/// </summary>
	public const double Pi = 3.1415926535897932384626433832795028841971693993751;

	/// <summary>
	/// The number `Tau` is the ratio of a circle's circumference to its radius, `pi*2`, approximately `6.283` or a angle of `360º`.
	/// </summary>
	public const double Pi2 = 6.2831853071795864769252867665590057683943387987502;

	/// <summary>
	/// The number `Tau` is the ratio of a circle's circumference to its radius, `pi*2`, approximately `6.283` or a angle of `360º`.
	/// </summary>
	public const double Tau = 6.2831853071795864769252867665590057683943387987502;

	/// <summary>
	/// Half Pi, `pi/2`, approximately `1.570` or a angle of `90º`.
	/// </summary>
	public const double PiOver2 = 1.5707963267948966192313216916397514420985846996876;

	/// <summary>
	/// Represents a angle of `270º` in radeans or `3/4` of a circle's circumference, `pi*3/2`, approximately `4.712`.
	/// </summary>
	public const double Pi3Over2 = 4.71238898038468985769396507491925432629575409906266;

	/// <summary>
	/// Represents a angle of `45º` in radeans or `1/4` of a circles's circumference, `pi/4`, approximately `0.785`.
	/// </summary>
	public const double PiOver4 = 0.78539816339744830961566084581987572104929234984378;

	/// <summary>
	/// Square root of Pi, `sqrt(pi)`, approximately `1.772` or a angle of`101º`.
	/// </summary>
	public const double SqrtPi = 1.7724538509055160272981674833411451827975494561224;

	/// <summary>
	/// Square root of `Pi2`, `sqrt(2pi)`, approximately `2.506` or a angle of `144º`.
	/// </summary>
	public const double Sqrt2Pi = 2.5066282746310005024157652848110452530069867406099;

	/// <summary>
	/// Square root of half Pi, `sqrt(pi/2)`, approximately `1.253` or a angle of `72º`.
	/// </summary>
	public const double SqrtPiOver2 = 1.2533141373155002512078826424055226265034933703050;

	/// <summary>
	/// The number `sqrt(2*pi*e)`, approximately `4.132 or a angle of `237º`.
	/// </summary>
	public const double Sqrt2PiE = 4.1327313541224929384693918842998526494455219169913;

	/// <summary>
	/// The number `log(sqrt(2*pi))`, approximately `0.918` or a angle of `52º`.
	/// </summary>
	public const double LogSqrt2Pi = 0.91893853320467274178032973640561763986139747363778;

	/// <summary>
	/// The number `log(sqrt(2*pi*e))`, approximately `1.418` or a angle of `81º`.
	/// </summary>
	public const double LogSqrt2PiE = 1.4189385332046727417803297364056176398613974736378;

	/// <summary>
	/// The number `log(2 * sqrt(e / pi))`, approximately `0.62` or a angle of `35º`.
	/// </summary>
	public const double LogTwoSqrtEOverPi = 0.6207822376352452223455184457816472122518527279025978;

	/// <summary>
	/// Inverse of Pi, `1/pi`, approximately `0.318` or a angle of `18º`.
	/// </summary>
	public const double InvPi = 0.31830988618379067153776752674502872406891929148091;

	/// <summary>
	/// Double the Inverse of Pi, `2/pi`, approximately `0.636` or a angle of `36º`.
	/// </summary>
	public const double TwoInvPi = 0.63661977236758134307553505349005744813783858296182;

	/// <summary>
	/// Inverse of the Square of Pi, `1/sqrt(pi)`, approximately `0.564` or a angle of `32º`.
	/// </summary>
	public const double InvSqrtPi = 0.56418958354775628694807945156077258584405062932899;

	/// <summary>
	/// The number `1/sqrt(2pi)`, approximately `0.398` or a angle of `23º`.
	/// </summary>
	public const double InvSqrt2Pi = 0.39894228040143267793994605993438186847585863116492;

	/// <summary>
	/// The number `2/sqrt(pi)`, approximately `1.128` or a angle of `65º`.
	/// </summary>
	public const double TwoInvSqrtPi = 1.1283791670955125738961589031215451716881012586580;

	/// <summary>
	/// The number `2 * sqrt(e / pi)`, approximately `1.86` or a angle of `107º`.
	/// </summary>
	public const double TwoSqrtEOverPi = 1.8603827342052657173362492472666631120594218414085755;

	/// <summary>
	/// The cubic root of 2, approximately `1.26` or a angle of `72º`.
	/// </summary>
	public const double Curt2 = 1.25992104989487316476;

	/// <summary>
	/// The cubic root of 3, approximately `1.44` or a angle of `83º`.
	/// </summary>
	public const double Curt3 = 1.44224957030740838232;

	/// <summary>
	/// The number `(pi)/180`, factor to convert from Degree (deg) to Radians (rad), approximately `0.017` or a angle of `1º`.
	/// </summary>
	public const double Degree = 0.017453292519943295769236907684886127134428718885417;

	/// <summary>
	/// The number `(pi)/200`, factor to convert from NewGrad (grad) to Radians (rad), approximately `0.015`.
	/// </summary>
	public const double Grad = 0.015707963267948966192313216916397514420985846996876;

	/// <summary>
	/// The number `ln(10)/20`, factor to convert from Power Decibel (`dB`) to Neper (`Np`). Use this version when the Decibel represent a power gain but the compared values are not powers (e.g. amplitude, current, voltage), approximately `0.115` or a angle of `7º`.
	/// </summary>
	public const double PowerDecibel = 0.11512925464970228420089957273421821038005507443144;

	/// <summary>
	/// The number `ln(10)/10`, factor to convert from Neutral Decibel (`dB`) to Neper (`Np`). Use this version when either both or neither of the Decibel and the compared values represent powers, approximately `0.23` or a angle of `23º`.
	/// </summary>
	public const double NeutralDecibel = 0.23025850929940456840179914546843642076011014886288;

	/// <summary>
	/// The Catalan constant `Sum(k=0 -> inf){ (-1)^k/(2*k + 1)2 }`, approximately `0.915` or a angle of `52º`.
	/// </summary>
	public const double Catalan = 0.9159655941772190150546035149323841107741493742816721342664981196217630197762547694794;

	/// <summary>
	/// The Euler-Mascheroni constant `lim(n -> inf){ Sum(k=1 -> n) { 1/k - log(n) } }`, approximately `0.577` or a angle of `33º`.
	/// </summary>
	public const double EulerMascheroni = 0.5772156649015328606065120900824024310421593359399235988057672348849;

	/// <summary>
	/// The number `(1+sqrt(5))/2`, also known as the Golden Ratio, approximately `1.618` or a angle of `93º`.
	/// </summary>
	public const double GoldenRatio = 1.6180339887498948482045868343656381177203091798057628621354486227052604628189024497072;

	/// <summary>
	/// The number `(1+sqrt(5))/2`, also known as the Golden Ratio, approximately `1.618` or a angle of `93º`.
	/// </summary>
	public const double Phi = 1.6180339887498948482045868343656381177203091798057628621354486227052604628189024497072;

	/// <summary>
	/// Super Golden Ratio. In mathematics, the supergolden ratio is a geometrical proportion, given by the unique real solution of the equation `x3 = x2 + 1`, approximately `1.466` or a angle of `84º`.
	/// </summary>
	public const double SuperGoldenRatio = 1.46557123187676802665;

	/// <summary>
	/// The number `sqrt(2)+1`, also known as the Silver ratio, approximately `2.414` or a angle of `138º`.
	/// </summary>
	public const double SilverRatio = 2.41421356237309504880;

	/// <summary>
	/// The number Glaisher constant `e^(1/12 - Zeta(-1))`, approximately `1.282` or a angle of `73º`.
	/// </summary>
	public const double Glaisher = 1.2824271291006226368753425688697917277676889273250011920637400217404063088588264611297;

	/// <summary>
	/// The Khinchin constant `prod(k=1 -> inf){1+1/(k*(k+2))^log(k,2)}`, approximately `2.685` or a angle of `154º`.
	/// </summary>
	public const double Khinchin = 2.6854520010653064453097148354817956938203822939944629530511523455572188595371520028011;

	/// <summary>
	/// The Connective constant for the hexagonal lattice, approximately `1.85` or a angle of `105º`.
	/// </summary>
	public const double Connective = 1.84775906502257351225;

	/// <summary>
	/// Kepler–Bouwkamp constant. In plane geometry, the Kepler–Bouwkamp constant (or polygon inscribing constant) is obtained as a limit of the following sequence. Take a circle of radius 1. Inscribe a regular triangle in this circle. Inscribe a circle in this triangle. Inscribe a square in it. Inscribe a circle, regular pentagon, circle, regular hexagon and so forth. The radius of the limiting circle is called the Kepler–Bouwkamp constant.
	/// </summary>
	public const double KeplerBouwkamp = 0.11494204485329620070;

	/// <summary>
	/// Walli's constant, approximately `2.09`.
	/// </summary>
	public const double Walli = 2.09455148154232659148;

	/// <summary>
	/// Lemniscate constant, approximately `2.62`.
	/// </summary>
	public const double Lemniscate = 2.62205755429211981046;

	/// <summary>
	/// Euler constant, approximately `0.58`.
	/// </summary>
	public const double Euler = 0.57721566490153286060;

	/// <summary>
	/// Erdos-Borwein constant, approximately `1.60`.
	/// </summary>
	public const double ErdosBorwein = 1.60669515241529176378;

	/// <summary>
	/// Omega constant, approximately `0.57`.
	/// </summary>
	public const double Omega = 0.56714329040978387299;

	/// <summary>
	/// Aperys constant, approximately `1.20`.
	/// </summary>
	public const double Aperys = 1.20205690315959428539;

	/// <summary>
	/// Laplace Limit, approximately `0.66`.
	/// </summary>
	public const double LaplaceLimit = 0.66274341934918158097;

	/// <summary>
	/// Soldner constant, approximately `1.45`.
	/// </summary>
	public const double Soldner = 1.45136923488338105028;

	/// <summary>
	/// Gauss constant, approximately `0.83`.
	/// </summary>
	public const double Gauss = 0.83462684167407318628;

	/// <summary>
	/// SecondHermite constant, approximately `1.15`.
	/// </summary>
	public const double SecondHermite = 1.15470053837925152901;

	/// <summary>
	/// Liouville's constant, approximately `0.11`.
	/// </summary>
	public const double Liouville = 0.110001000000000000000001;

	/// <summary>
	/// First Continued Fraction constant, approximately `0.69`.
	/// </summary>
	public const double FirstContinuedFraction = 0.69777465796400798201;

	/// <summary>
	/// Ramanujan's constant.
	/// </summary>
	public const double Ramanujan = 262537412640768743.999999999999250073;

	/// <summary>
	/// Glaisher Kinkelin constant, approximately `1.28`.
	/// </summary>
	public const double GlaisherKinkelin = 1.28242712910062263687;

	/// <summary>
	/// Dottie number, approximately `0.74`.
	/// </summary>
	public const double Dottie = 0.73908513321516064165;

	/// <summary>
	/// Meissel Mertens constant, approximately `0.26`.
	/// </summary>
	public const double MeisselMertens = 0.26149721284764278375;

	/// <summary>
	/// Universal Parabolic constant, approximately `2.29`.
	/// </summary>
	public const double UniversalParabolic = 2.29558714939263807403;

	/// <summary>
	/// Cahen's constant, approximately `0.64`.
	/// </summary>
	public const double Cahen = 0.64341054628833802618;

	/// <summary>
	/// Gelfond's constant, approximately `23.14`.
	/// </summary>
	public const double Gelfond = 23.1406926327792690057;

	/// <summary>
	/// Gelfond-Schneider constant, approximately `2.66`.
	/// </summary>
	public const double GelfondSchneider = 2.66514414269022518865;

	/// <summary>
	/// SecondFavard constant, approximately `1.23`.
	/// </summary>
	public const double SecondFavard = 1.23370055013616982735;

	/// <summary>
	/// Golden Angle constant, approximately `2.39`.
	/// </summary>
	public const double GoldenAngle = 2.39996322972865332223;

	/// <summary>
	/// Sierpiński's constant, approximately `2.58`.
	/// </summary>
	public const double Sierpinski = 2.58498175957925321706;

	/// <summary>
	/// Landau-Ramanujan constant, approximately `0.76`.
	/// </summary>
	public const double LandauRamanujan = 0.76422365358922066299;

	/// <summary>
	/// Nielsen-Ramanujan constant, approximately `0.82`.
	/// </summary>
	public const double NielsenRamanujan = 0.82246703342411321823;

	/// <summary>
	/// Gieseking constant, approximately `1.01`.
	/// </summary>
	public const double Gieseking = 1.01494160640965362502;

	/// <summary>
	/// Bernstein's constant, approximately `0.28`.
	/// </summary>
	public const double Bernstein = 0.28016949902386913303;

	/// <summary>
	/// Tribonacci constant, approximately `1.83`.
	/// </summary>
	public const double Tribonacci = 1.83928675521416113255;

	/// <summary>
	/// Brun's constant, approximately `0.57`.
	/// </summary>
	public const double Brun = 1.902160583104;

	/// <summary>
	/// Plastic Ratio, approximately `1.32`.
	/// </summary>
	public const double PlasticRatio = 1.32471795724474602596;

	/// <summary>
	/// Bloch's constant, approximately `0.43`.
	/// </summary>
	public const double BlochMin = 0.4332;

	/// <summary>
	/// Bloch's constant, approximately `0.47`.
	/// </summary>
	public const double BlochMax = 0.4719;

	/// <summary>
	/// Z Score for the 97.5 percentile point, approximately `1.96`.
	/// </summary>
	public const double ZScore975 = 1.95996398454005423552;

	/// <summary>
	/// Landau's constant, approximately `0.5`.
	/// </summary>
	public const double LandauMin = 0.5;

	/// <summary>
	/// Landau's constant, approximately `0.54`.
	/// </summary>
	public const double LandauMax = 0.54326;

	/// <summary>
	/// Landau's Third constant, approximately `0.5`.
	/// </summary>
	public const double LandauThirdMin = 0.5;

	/// <summary>
	/// Landau's Third constant, approximately `0.78`.
	/// </summary>
	public const double LandauThirdMax = 0.7853;

	/// <summary>
	/// Prouhet-Thue-Morse constant, approximately `0.41`.
	/// </summary>
	public const double ProuhetThueMorse = 0.41245403364010759778;

	/// <summary>
	/// Golomb-Dickman constant, approximately `0.62`.
	/// </summary>
	public const double GolombDickman = 0.62432998854355087099;

	/// <summary>
	/// Feller-Tornier constant, approximately `0.66`.
	/// </summary>
	public const double FellerTornier = 0.66131704946962233528;

	/// <summary>
	/// Salem constant, approximately `1.17`.
	/// </summary>
	public const double Salem = 1.17628081825991750654;

	/// <summary>
	/// Levy constant, approximately `1.18`.
	/// </summary>
	public const double Levy1 = 1.18656911041562545282;

	/// <summary>
	/// Levy constant, approximately `3.27`.
	/// </summary>
	public const double Levy2 = 3.27582291872181115978;

	/// <summary>
	/// Copeland-Erdos constant, approximately `0.23`.
	/// </summary>
	public const double CopelandErdos = 0.23571113171923293137;

	/// <summary>
	/// Mills constant, approximately `1.30`.
	/// </summary>
	public const double Mills = 1.30637788386308069046;

	/// <summary>
	/// Gompertz constant, approximately `0.59`.
	/// </summary>
	public const double Gompertz = 0.59634736232319407434;

	/// <summary>
	/// DeBruijn-Newman constant, approximately `0.0`.
	/// </summary>
	public const double DeBruijnNewmanMin = 0.0;

	/// <summary>
	/// DeBruijn-Newman constant, approximately `0.2`.
	/// </summary>
	public const double DeBruijnNewmanMax = 0.2;

	/// <summary>
	/// VanDerPauw constant, approximately `4.53`.
	/// </summary>
	public const double VanDerPauw = 4.53236014182719380962;

	/// <summary>
	/// Magic Angle constant, approximately `0.95`.
	/// </summary>
	public const double MagicAngle = 0.955316618124509278163;

	/// <summary>
	/// Artin's constant, approximately `0.37`.
	/// </summary>
	public const double Artin = 0.37395581361920228805;

	/// <summary>
	/// Porter's constant, approximately `1.46`.
	/// </summary>
	public const double Porter = 1.46707807943397547289;

	/// <summary>
	/// Lochs constant, approximately `0.97`.
	/// </summary>
	public const double Lochs = 0.97027011439203392574;

	/// <summary>
	/// DeVicci's Tesseract constant, approximately `1.00`.
	/// </summary>
	public const double DeVicciTesseract = 1.00743475688427937609;

	/// <summary>
	/// Lieb's Square Ice constant, approximately `1.53`.
	/// </summary>
	public const double LiebSquareIce = 1.53960071783900203869;

	/// <summary>
	/// Niven's constant, approximately `1.70`.
	/// </summary>
	public const double Niven = 1.70521114010536776428;

	/// <summary>
	/// Stephens constant, approximately `0.57`.
	/// </summary>
	public const double Stephens = 0.57595996889294543964;

	/// <summary>
	/// Regular Paperfolding sequence, approximately `0.85`.
	/// </summary>
	public const double RegularPaperfolding = 0.85073618820186726036;

	/// <summary>
	/// Reciprocal Fibonacci constant, approximately `3.35`.
	/// </summary>
	public const double ReciprocalFibonacci = 3.35988566624317755317;

	/// <summary>
	/// Chvatal-Sankoff constant, approximately `0.78`.
	/// </summary>
	public const double ChvatalSankoffMin = 0.788071;

	/// <summary>
	/// Chvatal-Sankoff constant, approximately `0.82`.
	/// </summary>
	public const double ChvatalSankoffMax = 0.826280;

	/// <summary>
	/// Feigenbaum constant, approximately `4.66`.
	/// </summary>
	public const double Feigenbaum = 4.66920160910299067185;

	/// <summary>
	/// Chaitin constant, approximately `0.007`.
	/// </summary>
	public const double Chaitin = 0.0078749969978123844;

	/// <summary>
	/// Robbins constant, approximately `0.66`.
	/// </summary>
	public const double Robbins = 0.66170718226717623515;

	/// <summary>
	/// Weierstrass constant, approximately `0.47`.
	/// </summary>
	public const double Weierstrass = 0.47494937998792065033;

	/// <summary>
	/// Fransen-Robinson constant, approximately `2.80`.
	/// </summary>
	public const double FransenRobinson = 2.80777024202851936522;

	/// <summary>
	/// Feigenbaum Alpha constant, approximately `2.50`.
	/// </summary>
	public const double FeigenbaumAlpha = 2.50290787509589282228;

	/// <summary>
	/// DuBois-Reymond Second constant, approximately `0.19`.
	/// </summary>
	public const double DuBoisReymondSecond = 0.19452804946532511361;

	/// <summary>
	/// Erdos-Tenenbaum-Ford constant, approximately `0.08`.
	/// </summary>
	public const double ErdosTenenbaumFord = 0.08607133205593420688;

	/// <summary>
	/// Conway's constant, approximately `1.30`.
	/// </summary>
	public const double Conway = 1.30357726903429639125;

	/// <summary>
	/// Hafner-Sarnak-McCurley constant, approximately `0.35`.
	/// </summary>
	public const double HafnerSarnakMcCurley = 0.35323637185499598454;

	/// <summary>
	/// Backhouse's constant, approximately `1.45`.
	/// </summary>
	public const double Backhouse = 1.45607494858268967139;

	/// <summary>
	/// Viswanath constant, approximately `1.13`.
	/// </summary>
	public const double Viswanath = 1.1319882487943;

	/// <summary>
	/// Komornik-Loreti constant, approximately `1.78`.
	/// </summary>
	public const double KomornikLoreti = 1.78723165018296593301;

	/// <summary>
	/// Embree-Trefethen constant, approximately `0.70`.
	/// </summary>
	public const double EmbreeTrefethen = 0.70258;

	/// <summary>
	/// Heath-Brown-Moroz constant, approximately `0.001`.
	/// </summary>
	public const double HeathBrownMoroz = 0.00131764115485317810;

	/// <summary>
	/// MRB constant, approximately `0.18`.
	/// </summary>
	public const double MRB = 0.18785964246206712024;

	/// <summary>
	/// Prime constant, approximately `0.41`.
	/// </summary>
	public const double Prime = 0.41468250985111166024;

	/// <summary>
	/// Somos Quadratic Recurrence constant, approximately `1.66`.
	/// </summary>
	public const double SomosQuadraticRecurrence = 1.66168794963359412129;

	/// <summary>
	/// Foias constant, approximately `1.18`.
	/// </summary>
	public const double Foias = 1.18745235112650105459;

	/// <summary>
	/// Taniguchi constant, approximately `0.67`.
	/// </summary>
	public const double Taniguchi = 0.67823449191739197803;

	/// <summary>
	/// Positive Infinity `+∞`, approximately `1.8e309`.
	/// </summary>
	public const double Positive_Infinity = double.PositiveInfinity;

	/// <summary>
	/// Positive finite maxima, the last value before positive infinity `+∞`, approximately `1.8e308`.
	/// </summary>
	public const double Positive_Finite_Maxima = +1.7976931348623157e+308;

	/// <summary>
	/// Positive value closest to 0, approximately `2.2e-308`.
	/// </summary>
	public const double Positive_Tiniest_Value = +2.2250738585072014e-308;

	/// <summary>
	/// Positive zero, `+0.0`.
	/// </summary>
	public const double Positive_Zero = +0.0e0;

	/// <summary>
	/// Not A Number.
	/// </summary>
	public const double Nan = double.NaN;

	/// <summary>
	/// Negative zero, `-0.0`.
	/// </summary>
	public const double Negative_Zero = -0.0;

	/// <summary>
	/// Negative value closest to 0, approximately `-2.2e-308`.
	/// </summary>
	public const double Negative_Tiniest_Value = -2.2250738585072014e-308;

	/// <summary>
	/// Negative finite maxima, the last value before negative infinity `-∞`, approximately `-1.8e+308`.
	/// </summary>
	public const double Negative_Finite_Minima = -1.7976931348623157e+308;

	/// <summary>
	/// Negative infinity `-∞`, approximately `-1.8e309`.
	/// </summary>
	public const double Negative_Infinity = double.NegativeInfinity;

}
