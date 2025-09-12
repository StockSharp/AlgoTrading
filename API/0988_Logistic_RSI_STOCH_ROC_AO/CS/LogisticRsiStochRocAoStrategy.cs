using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy applying logistic map to selected indicator.
/// Trades on zero crossovers of signed standard deviation.
/// </summary>
public class LogisticRsiStochRocAoStrategy : Strategy
{
	/// <summary>
	/// Indicator source options.
	/// </summary>
	public enum IndicatorSource
	{
		AwesomeOscillator,
		LogisticDominance,
		RateOfChange,
		RelativeStrengthIndex,
		Stochastic
	}

	private readonly StrategyParam<IndicatorSource> _indicator;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _lenLd;
	private readonly StrategyParam<int> _lenRoc;
	private readonly StrategyParam<int> _lenRsi;
	private readonly StrategyParam<int> _lenSto;
	private readonly StrategyParam<DataType> _candleType;

	private readonly AwesomeOscillator _ao = new();
	private readonly RateOfChange _roc = new();
	private readonly RateOfChange _rocLd = new();
	private readonly RelativeStrengthIndex _rsi = new();
	private readonly StochasticOscillator _stoch = new();
	private readonly Highest _highest = new();

	private decimal _mean;
	private decimal _m2;
	private int _count;
	private decimal? _prevStd;

	/// <summary>
	/// Selected indicator source.
	/// </summary>
	public IndicatorSource Indicator
	{
		get => _indicator.Value;
		set => _indicator.Value = value;
	}

	/// <summary>
	/// Logistic map length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Length for logistic dominance calculation.
	/// </summary>
	public int LenLd
	{
		get => _lenLd.Value;
		set => _lenLd.Value = value;
	}

	/// <summary>
	/// Rate of change period.
	/// </summary>
	public int LenRoc
	{
		get => _lenRoc.Value;
		set => _lenRoc.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int LenRsi
	{
		get => _lenRsi.Value;
		set => _lenRsi.Value = value;
	}

	/// <summary>
	/// Stochastic period.
	/// </summary>
	public int LenSto
	{
		get => _lenSto.Value;
		set => _lenSto.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="LogisticRsiStochRocAoStrategy"/>.
	/// </summary>
	public LogisticRsiStochRocAoStrategy()
	{
		_indicator = Param(nameof(Indicator), IndicatorSource.LogisticDominance)
						 .SetDisplay("Indicator", "Source indicator", "General");

		_length = Param(nameof(Length), 13).SetDisplay("Length", "Logistic map length", "General").SetCanOptimize(true);

		_lenLd = Param(nameof(LenLd), 5)
					 .SetDisplay("Len LD", "Length for logistic dominance", "Sources")
					 .SetCanOptimize(true);

		_lenRoc = Param(nameof(LenRoc), 9).SetDisplay("Len ROC", "ROC length", "Sources").SetCanOptimize(true);

		_lenRsi = Param(nameof(LenRsi), 14).SetDisplay("Len RSI", "RSI length", "Sources").SetCanOptimize(true);

		_lenSto = Param(nameof(LenSto), 14).SetDisplay("Len STO", "Stochastic length", "Sources").SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ao.ShortPeriod = 5;
		_ao.LongPeriod = 34;
		_roc.Length = LenRoc;
		_rocLd.Length = LenLd;
		_rsi.Length = LenRsi;
		_stoch.K.Length = LenSto;
		_stoch.D.Length = 3;
		_highest.Length = Length;

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_ao, _roc, _rsi, _stoch, _rocLd, _highest, ProcessCandle).Start();

		StartProtection();
	}

	private static decimal LogisticMap(decimal s, decimal r, decimal highest)
	{
		if (highest == 0)
			return 0m;
		var norm = s / highest;
		return r * norm * (1m - norm);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue aoValue, IIndicatorValue rocValue,
							   IIndicatorValue rsiValue, IIndicatorValue stochValue, IIndicatorValue rocLdValue,
							   IIndicatorValue highestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var highest = highestValue.ToDecimal();
		decimal r;
		switch (Indicator)
		{
		case IndicatorSource.AwesomeOscillator:
			if (!aoValue.IsFinal)
				return;
			r = aoValue.ToDecimal();
			break;
		case IndicatorSource.LogisticDominance:
			if (!rocLdValue.IsFinal)
				return;
			var ld = rocLdValue.ToDecimal();
			var neg = LogisticMap(-candle.ClosePrice, ld, highest);
			var pos = LogisticMap(candle.ClosePrice, ld, highest);
			r = -neg - pos;
			break;
		case IndicatorSource.RateOfChange:
			if (!rocValue.IsFinal)
				return;
			r = rocValue.ToDecimal();
			break;
		case IndicatorSource.RelativeStrengthIndex:
			if (!rsiValue.IsFinal)
				return;
			r = rsiValue.ToDecimal() / 100m - 0.5m;
			break;
		case IndicatorSource.Stochastic:
			if (!stochValue.IsFinal)
				return;
			var st = (StochasticOscillatorValue)stochValue;
			if (st.K is not decimal k)
				return;
			r = k / 100m - 0.5m;
			break;
		default:
			return;
		}

		var logistic = LogisticMap(candle.ClosePrice, r, highest);

		_count++;
		var delta = logistic - _mean;
		_mean += delta / _count;
		var delta2 = logistic - _mean;
		_m2 += delta * delta2;
		var std = _count > 1 ? (decimal)Math.Sqrt((double)(_m2 / _count)) : 0m;
		var aStd = (_mean >= 0 ? 1m : -1m) * std;

		if (_prevStd.HasValue)
		{
			var prev = _prevStd.Value;
			var crossUp = prev <= 0m && aStd > 0m;
			var crossDown = prev >= 0m && aStd < 0m;

			if (crossUp && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (crossDown && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevStd = aStd;
	}
}
