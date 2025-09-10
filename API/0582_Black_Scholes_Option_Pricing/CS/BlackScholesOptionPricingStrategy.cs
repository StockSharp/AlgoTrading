using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Calculates theoretical option price using the Black-Scholes model.
/// </summary>
public class BlackScholesOptionPricingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _strikePrice;
	private readonly StrategyParam<decimal> _riskFreeRate;
	private readonly StrategyParam<decimal> _volatility;
	private readonly StrategyParam<int> _daysToExpiry;
	private readonly StrategyParam<bool> _isCall;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Option strike price.
	/// </summary>
	public decimal StrikePrice
	{
		get => _strikePrice.Value;
		set => _strikePrice.Value = value;
	}

	/// <summary>
	/// Annual risk free interest rate.
	/// </summary>
	public decimal RiskFreeRate
	{
		get => _riskFreeRate.Value;
		set => _riskFreeRate.Value = value;
	}

	/// <summary>
	/// Implied volatility (as decimal).
	/// </summary>
	public decimal Volatility
	{
		get => _volatility.Value;
		set => _volatility.Value = value;
	}

	/// <summary>
	/// Days until option expiration.
	/// </summary>
	public int DaysToExpiry
	{
		get => _daysToExpiry.Value;
		set => _daysToExpiry.Value = value;
	}

	/// <summary>
	/// Calculate call price when true, put price when false.
	/// </summary>
	public bool IsCall
	{
		get => _isCall.Value;
		set => _isCall.Value = value;
	}

	/// <summary>
	/// The type of candles to use for spot price.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Last calculated option price.
	/// </summary>
	public decimal OptionPrice { get; private set; }

	/// <summary>
	/// Initializes a new instance of <see cref="BlackScholesOptionPricingStrategy"/>.
	/// </summary>
	public BlackScholesOptionPricingStrategy()
	{
		_strikePrice = Param(nameof(StrikePrice), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Strike", "Option strike price", "Black-Scholes");

		_riskFreeRate = Param(nameof(RiskFreeRate), 0.01m)
			.SetDisplay("Risk Free Rate", "Annual risk free interest rate", "Black-Scholes");

		_volatility = Param(nameof(Volatility), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Volatility", "Implied volatility", "Black-Scholes");

		_daysToExpiry = Param(nameof(DaysToExpiry), 30)
			.SetGreaterThanZero()
			.SetDisplay("Days to Expiry", "Number of days until expiration", "Black-Scholes");

		_isCall = Param(nameof(IsCall), true)
			.SetDisplay("Is Call", "True for call option, false for put", "Black-Scholes");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
			return;

		var s = candle.ClosePrice;
		var x = StrikePrice;
		var r = RiskFreeRate;
		var sigma = Volatility;
		var t = DaysToExpiry / 365m;

		if (sigma <= 0 || t <= 0 || x <= 0)
			return;

		var sqrtT = (decimal)Math.Sqrt((double)t);
		var d1 = ((decimal)Math.Log((double)(s / x)) + (r + sigma * sigma / 2m) * t) / (sigma * sqrtT);
		var d2 = d1 - sigma * sqrtT;

		var price = IsCall
			? s * NormCdf(d1) - x * (decimal)Math.Exp((double)(-r * t)) * NormCdf(d2)
			: x * (decimal)Math.Exp((double)(-r * t)) * NormCdf(-d2) - s * NormCdf(-d1);

		OptionPrice = price;

		LogInfo($"Option price: {price}");
	}

	private static decimal NormCdf(decimal x)
	{
		var l = Math.Abs((double)x);
		var k = 1.0 / (1.0 + 0.2316419 * l);
		var w = 1.0 - 0.3989422804014327 * Math.Exp(-l * l / 2.0) *
			((((1.330274429 * k - 1.821255978) * k + 1.781477937) * k - 0.356563782) * k + 0.319381530) * k;

		return x < 0 ? (decimal)(1.0 - w) : (decimal)w;
	}
}
