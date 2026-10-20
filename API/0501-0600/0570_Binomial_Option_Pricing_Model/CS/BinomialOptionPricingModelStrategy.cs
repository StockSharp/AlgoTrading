namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Two-step Cox-Ross-Rubinstein price calculator with explicit carry and exercise conventions.</summary>
public class BinomialOptionPricingModelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _strikePrice;
	private readonly StrategyParam<decimal> _dividendYield;
	private readonly StrategyParam<SecurityTypes> _assetClass;
	private readonly StrategyParam<OptionStyles> _optionStyle;
	private readonly StrategyParam<OptionTypes> _optionType;
	private readonly StrategyParam<TimeSpan> _timeToExpiry;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private StandardDeviation _deviation;
	private SimpleMovingAverage _average;
	private DateTime _lastTime;

	/// <summary>Positive option strike.</summary>
	public decimal StrikePrice
	{
		get => _strikePrice.Value;
		set => _strikePrice.Value = value;
	}

	/// <summary>Continuous dividend or foreign interest rate as a fraction.</summary>
	public decimal DividendYield
	{
		get => _dividendYield.Value;
		set => _dividendYield.Value = value;
	}

	/// <summary>Stock, Index, Currency or Future carry convention.</summary>
	public SecurityTypes AssetClass
	{
		get => _assetClass.Value;
		set => _assetClass.Value = value;
	}

	/// <summary>European maturity exercise or American tree-node exercise.</summary>
	public OptionStyles OptionStyle
	{
		get => _optionStyle.Value;
		set => _optionStyle.Value = value;
	}

	/// <summary>Call or put payoff.</summary>
	public OptionTypes OptionType
	{
		get => _optionType.Value;
		set => _optionType.Value = value;
	}

	/// <summary>Remaining tenor; minutes, hours and days are supported.</summary>
	public TimeSpan TimeToExpiry
	{
		get => _timeToExpiry.Value;
		set => _timeToExpiry.Value = value;
	}

	/// <summary>Population close deviation and mean window.</summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>Finished source candle interval.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Latest formed theoretical option price.</summary>
	public decimal? LastOptionPrice { get; private set; }

	/// <summary>Initializes pricing parameters and the inherited fractional RiskFreeRate.</summary>
	public BinomialOptionPricingModelStrategy()
	{
		_strikePrice = Param(nameof(StrikePrice), 100m).SetGreaterThanZero()
			.SetDisplay("StrikePrice", "Positive option strike", "Parameters");
		_dividendYield = Param(nameof(DividendYield), 0m)
			.SetDisplay("DividendYield", "Continuous dividend or foreign interest rate as a fraction", "Parameters");
		_assetClass = Param(nameof(AssetClass), SecurityTypes.Stock)
			.SetDisplay("AssetClass", "Stock, Index, Currency or Future carry convention", "Parameters");
		_optionStyle = Param(nameof(OptionStyle), OptionStyles.European)
			.SetDisplay("OptionStyle", "European maturity exercise or American tree-node exercise", "Parameters");
		_optionType = Param(nameof(OptionType), OptionTypes.Call)
			.SetDisplay("OptionType", "Call or put payoff", "Parameters");
		_timeToExpiry = Param(nameof(TimeToExpiry), TimeSpan.FromDays(30))
			.SetDisplay("TimeToExpiry", "Remaining tenor; minutes, hours and days are supported", "Parameters");
		_length = Param(nameof(Length), 20).SetGreaterThanZero()
			.SetDisplay("Length", "Population close deviation and mean window", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("CandleType", "Finished source candle interval", "Parameters");
		RiskFreeRate = 0.05m;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_deviation = null;
		_average = null;
		_lastTime = default;
		LastOptionPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		EnsureIndicators();
		base.OnStarted2(time);
		var candles = SubscribeCandles(CandleType);
		candles.Bind(candle => EvaluatePrice(candle)).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, candles);
			DrawIndicator(area, _deviation);
		}
	}

	private void EnsureIndicators()
	{
		if (_deviation is not null && _deviation.Length == Length) return;
		_deviation = new StandardDeviation { Length = Length };
		_average = new SimpleMovingAverage { Length = Length };
	}

	/// <summary>Evaluates one finished candle; returns null until the close window is formed or when the candle is ignored.</summary>
	/// <param name="candle">UTC candle to add to the pricing window.</param>
	/// <returns>The theoretical price, with no trading side effects.</returns>
	public decimal? EvaluatePrice(ICandleMessage candle)
	{
		ArgumentNullException.ThrowIfNull(candle);
		if (candle.State != CandleStates.Finished || candle.OpenTime <= _lastTime) return null;
		if (candle.ClosePrice <= 0m) throw new ArgumentOutOfRangeException(nameof(candle), "A positive underlying close is required.");
		if (CandleType.Arg is not TimeSpan frame || frame <= TimeSpan.Zero)
			throw new InvalidOperationException("A positive time-frame candle interval is required for annualization.");
		EnsureIndicators();
		_lastTime = candle.OpenTime;
		var deviation = _deviation.Process(new DecimalIndicatorValue(_deviation, candle.ClosePrice, candle.OpenTime) { IsFinal = true });
		var average = _average.Process(new DecimalIndicatorValue(_average, candle.ClosePrice, candle.OpenTime) { IsFinal = true });
		if (!_deviation.IsFormed || !_average.IsFormed) return null;
		var sigma = (decimal)((double)(deviation.GetValue<decimal>() / average.GetValue<decimal>())
			* Math.Sqrt(TimeSpan.FromDays(365).TotalSeconds / frame.TotalSeconds));
		LastOptionPrice = CalculatePrice(candle.ClosePrice, sigma, TimeToExpiry);
		this.AddInfoLog("Option price at {0:O}: {1}", candle.OpenTime, LastOptionPrice.Value);
		return LastOptionPrice;
	}

	/// <summary>Calculates a two-step binomial value, including early exercise and zero-volatility paths.</summary>
	/// <param name="spot">Positive spot or current futures price.</param>
	/// <param name="volatility">Nonnegative annual fractional volatility.</param>
	/// <param name="timeToExpiry">Nonnegative remaining tenor on an ACT/365 basis.</param>
	/// <returns>Theoretical call or put price.</returns>
	public decimal CalculatePrice(decimal spot, decimal volatility, TimeSpan timeToExpiry)
	{
		if (spot <= 0m || volatility < 0m || timeToExpiry < TimeSpan.Zero || StrikePrice <= 0m)
			throw new ArgumentOutOfRangeException(nameof(spot), "Spot and strike must be positive; volatility and tenor must be nonnegative.");
		if (OptionStyle is not (OptionStyles.European or OptionStyles.American) || !Enum.IsDefined(OptionType)
			|| AssetClass is not (SecurityTypes.Stock or SecurityTypes.Index or SecurityTypes.Currency or SecurityTypes.Future))
			throw new ArgumentException("Unsupported option style, payoff or underlying asset class.");
		var s = (double)spot; var strike = (double)StrikePrice;
		double Payoff(double value) => Math.Max(OptionType == OptionTypes.Call ? value - strike : strike - value, 0.0);
		if (timeToExpiry == TimeSpan.Zero) return (decimal)Payoff(s);
		var years = timeToExpiry.TotalDays / 365.0;
		var dt = years / 2.0;
		var rate = (double)RiskFreeRate;
		var carry = AssetClass == SecurityTypes.Future ? 0.0 : rate - (double)DividendYield;
		if (volatility == 0m)
		{
			var final = Payoff(s * Math.Exp(carry * years)) * Math.Exp(-rate * years);
			if (OptionStyle == OptionStyles.American)
				final = Math.Max(Payoff(s), Math.Max(final, Payoff(s * Math.Exp(carry * dt)) * Math.Exp(-rate * dt)));
			return (decimal)final;
		}
		var up = Math.Exp((double)volatility * Math.Sqrt(dt));
		var down = 1.0 / up;
		var growth = Math.Exp(carry * dt);
		var probability = (growth - down) / (up - down);
		var discount = Math.Exp(-rate * dt);
		if (!double.IsFinite(probability) || probability < 0.0 || probability > 1.0 || !double.IsFinite(up) || !double.IsFinite(discount))
			throw new ArgumentException("Two-step inputs do not admit a finite risk-neutral probability in [0,1].");
		var upper = discount * (probability * Payoff(s * up * up) + (1.0 - probability) * Payoff(s));
		var lower = discount * (probability * Payoff(s) + (1.0 - probability) * Payoff(s * down * down));
		if (OptionStyle == OptionStyles.American)
		{
			upper = Math.Max(upper, Payoff(s * up));
			lower = Math.Max(lower, Payoff(s * down));
		}
		var price = discount * (probability * upper + (1.0 - probability) * lower);
		if (OptionStyle == OptionStyles.American) price = Math.Max(price, Payoff(s));
		return (decimal)price;
	}
}
