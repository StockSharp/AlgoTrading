using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Calculates theoretical option price using a two-step binomial tree.
/// </summary>
public class BinomialOptionPricingModelStrategy : Strategy
{
	private const int _daysInYear = 252;
	private const int _stepNum = 2;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _strikePrice;
	private readonly StrategyParam<decimal> _riskFreeRate;
	private readonly StrategyParam<decimal> _dividendYield;
	private readonly StrategyParam<string> _assetClass;
	private readonly StrategyParam<string> _optionStyle;
	private readonly StrategyParam<string> _optionType;
	private readonly StrategyParam<int> _expiryMinutes;
	private readonly StrategyParam<int> _expiryHours;
	private readonly StrategyParam<int> _expiryDays;
	private readonly StrategyParam<int> _timeframeMinutes;

	private StandardDeviation _stdDev;

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Option strike price.
	/// </summary>
	public decimal StrikePrice { get => _strikePrice.Value; set => _strikePrice.Value = value; }

	/// <summary>
	/// Risk free interest rate.
	/// </summary>
	public decimal RiskFreeRate { get => _riskFreeRate.Value; set => _riskFreeRate.Value = value; }

	/// <summary>
	/// Dividend yield or foreign risk free rate.
	/// </summary>
	public decimal DividendYield { get => _dividendYield.Value; set => _dividendYield.Value = value; }

	/// <summary>
	/// Underlying asset class (Stock, FX, Futures).
	/// </summary>
	public string AssetClass { get => _assetClass.Value; set => _assetClass.Value = value; }

	/// <summary>
	/// Option style (American Vanilla, European Vanilla).
	/// </summary>
	public string OptionStyle { get => _optionStyle.Value; set => _optionStyle.Value = value; }

	/// <summary>
	/// Option type (Long Call, Long Put).
	/// </summary>
	public string OptionType { get => _optionType.Value; set => _optionType.Value = value; }

	/// <summary>
	/// Minutes until expiry.
	/// </summary>
	public int MinutesToExpiry { get => _expiryMinutes.Value; set => _expiryMinutes.Value = value; }

	/// <summary>
	/// Hours until expiry.
	/// </summary>
	public int HoursToExpiry { get => _expiryHours.Value; set => _expiryHours.Value = value; }

	/// <summary>
	/// Days until expiry.
	/// </summary>
	public int DaysToExpiry { get => _expiryDays.Value; set => _expiryDays.Value = value; }

	/// <summary>
	/// Timeframe in minutes used for the calculation.
	/// </summary>
	public int TimeframeMinutes { get => _timeframeMinutes.Value; set => _timeframeMinutes.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public BinomialOptionPricingModelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_strikePrice = Param(nameof(StrikePrice), 50m)
			.SetDisplay("Strike Price", "Option strike price", "Option Parameters");

		_riskFreeRate = Param(nameof(RiskFreeRate), 0.00117m)
			.SetDisplay("Risk Free Rate", "Risk free interest rate", "Option Parameters");

		_dividendYield = Param(nameof(DividendYield), 0m)
			.SetDisplay("Dividend Yield", "Dividend yield or foreign risk free rate", "Option Parameters");

		_assetClass = Param(nameof(AssetClass), "Stock")
			.SetDisplay("Asset Class", "Underlying asset class", "Option Parameters");

		_optionStyle = Param(nameof(OptionStyle), "American Vanilla")
			.SetDisplay("Option Style", "Option style", "Option Parameters");

		_optionType = Param(nameof(OptionType), "Long Call")
			.SetDisplay("Option Type", "Option type", "Option Parameters");

		_expiryMinutes = Param(nameof(MinutesToExpiry), 0)
			.SetDisplay("Minutes", "Minutes until expiry", "Expiry");

		_expiryHours = Param(nameof(HoursToExpiry), 0)
			.SetDisplay("Hours", "Hours until expiry", "Expiry");

		_expiryDays = Param(nameof(DaysToExpiry), 23)
			.SetDisplay("Days", "Days until expiry", "Expiry");

		_timeframeMinutes = Param(nameof(TimeframeMinutes), 1440)
			.SetDisplay("Timeframe Minutes", "Timeframe in minutes", "Expiry");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stdDev = new StandardDeviation { Length = _daysInYear };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_stdDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sigma)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_stdDev.IsFormed)
		return;

		var S = candle.ClosePrice;
		var strike = StrikePrice;
		var r = RiskFreeRate;
		var q = DividendYield;
		var asset = AssetClass;
		var style = OptionStyle;
		var type = OptionType;

		var expiry = (HoursToExpiry + MinutesToExpiry / 60m) / 24m + DaysToExpiry;
		var time = 60m * 24m * expiry / TimeframeMinutes;
		var T = time / _daysInYear;

		var deltaT = T / _stepNum;
		var up = (decimal)Math.Exp((double)(sigma * (decimal)Math.Sqrt((double)deltaT)));
		var down = 1m / up;

		var aStock = (decimal)Math.Exp((double)((r - q) * deltaT));
		var a = asset == "Futures" ? 1m : aStock;
		var pup = (a - down) / (up - down);
		var pdown = 1m - pup;

		var Su1 = S * up;
		var Sd1 = S * down;
		var Su2a = Su1 * up;
		var Su2b = Su1 * down;
		var Sd2a = Sd1 * up;
		var Sd2b = Sd1 * down;

		var Cu2a = Math.Max(Su2a - strike, 0m);
		var Cu2b = Math.Max(Su2b - strike, 0m);
		var Cd2a = Math.Max(Sd2a - strike, 0m);
		var Cd2b = Math.Max(Sd2b - strike, 0m);

		var Pu2a = Math.Max(strike - Su2a, 0m);
		var Pu2b = Math.Max(strike - Su2b, 0m);
		var Pd2a = Math.Max(strike - Sd2a, 0m);
		var Pd2b = Math.Max(strike - Sd2b, 0m);

		decimal Nu2a, Nu2b, Nd2a, Nd2b;
		if (type == "Long Put")
		{
			Nu2a = Pu2a;
			Nu2b = Pu2b;
			Nd2a = Pd2a;
			Nd2b = Pd2b;
		}
		else
		{
			Nu2a = Cu2a;
			Nu2b = Cu2b;
			Nd2a = Cd2a;
			Nd2b = Cd2b;
		}

		var Nu1 = (pup * Nu2a + pdown * Nu2b) * (decimal)Math.Exp((double)(-r * deltaT));
		var Nd1 = (pup * Nu2b + pdown * Nd2b) * (decimal)Math.Exp((double)(-r * deltaT));

		var D = type == "Long Put" ? -1m : 1m;
		var intrinsicU1 = (Su1 - strike) * D;
		var intrinsicD1 = (Sd1 - strike) * D;
		var ANu1 = Math.Max(intrinsicU1, Nu1);
		var ANd1 = Math.Max(intrinsicD1, Nd1);

		var european = (pup * Nu1 + pdown * Nd1) * (decimal)Math.Exp((double)(-r * deltaT));

		var intrinsic0 = (S - strike) * D;
		var edv = (pup * ANu1 + pdown * ANd1) * (decimal)Math.Exp((double)(-r * deltaT));
		var american = Math.Max(intrinsic0, edv);

		var optionPrice = style == "European Vanilla" ? european : american;

		LogInfo($"Option price: {optionPrice:F5}");
	}
}
