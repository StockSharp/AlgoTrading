using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the Expert_ADC_PL_Stoch MQL5 strategy.
/// Combines Dark Cloud Cover and Piercing Line candlestick patterns with a stochastic confirmation filter.
/// </summary>
public class ExpertAdcPlStochStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<int> _stochSlow;
	private readonly StrategyParam<int> _averageBodyPeriod;
	private readonly StrategyParam<decimal> _longEntryThreshold;
	private readonly StrategyParam<decimal> _shortEntryThreshold;
	private readonly StrategyParam<decimal> _exitLowerThreshold;
	private readonly StrategyParam<decimal> _exitUpperThreshold;

	private StochasticOscillator _stochastic = null!;

	private readonly List<ICandleMessage> _candles = new();
	private readonly List<decimal> _stochSignal = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpertAdcPlStochStrategy"/> class.
	/// </summary>
	public ExpertAdcPlStochStrategy()
	{
		Volume = 1;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for pattern detection", "General");

		_stochLength = Param(nameof(StochasticLength), 47)
			.SetDisplay("Stochastic Length", "Number of bars used in stochastic calculation", "Stochastic")
			.SetGreaterOrEqual(1);

		_stochKPeriod = Param(nameof(StochasticKPeriod), 9)
			.SetDisplay("%K Smoothing", "Smoothing length applied to the %K line", "Stochastic")
			.SetGreaterOrEqual(1);

		_stochDPeriod = Param(nameof(StochasticDPeriod), 13)
			.SetDisplay("%D Smoothing", "Smoothing length applied to the %D line", "Stochastic")
			.SetGreaterOrEqual(1);

		_stochSlow = Param(nameof(StochasticSlow), 3)
			.SetDisplay("Slowing", "Additional slowing factor applied to the stochastic", "Stochastic")
			.SetGreaterOrEqual(1);

		_averageBodyPeriod = Param(nameof(AverageBodyPeriod), 5)
			.SetDisplay("Body Average Period", "Number of candles used to measure typical body size", "Patterns")
			.SetGreaterOrEqual(1);

		_longEntryThreshold = Param(nameof(LongEntryThreshold), 30m)
			.SetDisplay("Long Entry Threshold", "Upper bound for stochastic signal before long entries", "Trading");

		_shortEntryThreshold = Param(nameof(ShortEntryThreshold), 70m)
			.SetDisplay("Short Entry Threshold", "Lower bound for stochastic signal before short entries", "Trading");

		_exitLowerThreshold = Param(nameof(ExitLowerThreshold), 20m)
			.SetDisplay("Exit Lower Threshold", "Level used for stochastic cross checks near oversold zone", "Trading");

		_exitUpperThreshold = Param(nameof(ExitUpperThreshold), 80m)
			.SetDisplay("Exit Upper Threshold", "Level used for stochastic cross checks near overbought zone", "Trading");
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the stochastic oscillator.
	/// </summary>
	public int StochasticLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}

	/// <summary>
	/// Smoothing applied to the %K line.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing applied to the %D line.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	/// <summary>
	/// Additional slowing factor for the stochastic oscillator.
	/// </summary>
	public int StochasticSlow
	{
		get => _stochSlow.Value;
		set => _stochSlow.Value = value;
	}

	/// <summary>
	/// Number of bodies used to measure the typical candle size.
	/// </summary>
	public int AverageBodyPeriod
	{
		get => _averageBodyPeriod.Value;
		set => _averageBodyPeriod.Value = value;
	}

	/// <summary>
	/// Maximum stochastic signal value allowed before long entries.
	/// </summary>
	public decimal LongEntryThreshold
	{
		get => _longEntryThreshold.Value;
		set => _longEntryThreshold.Value = value;
	}

	/// <summary>
	/// Minimum stochastic signal value required before short entries.
	/// </summary>
	public decimal ShortEntryThreshold
	{
		get => _shortEntryThreshold.Value;
		set => _shortEntryThreshold.Value = value;
	}

	/// <summary>
	/// Lower stochastic level used for exit cross checks.
	/// </summary>
	public decimal ExitLowerThreshold
	{
		get => _exitLowerThreshold.Value;
		set => _exitLowerThreshold.Value = value;
	}

	/// <summary>
	/// Upper stochastic level used for exit cross checks.
	/// </summary>
	public decimal ExitUpperThreshold
	{
		get => _exitUpperThreshold.Value;
		set => _exitUpperThreshold.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_candles.Clear();
		_stochSignal.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stochastic = new StochasticOscillator
		{
			Length = Math.Max(1, StochasticLength),
			Smooth = Math.Max(1, StochasticSlow),
			K = { Length = Math.Max(1, StochasticKPeriod) },
			D = { Length = Math.Max(1, StochasticDPeriod) },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stochTyped = (StochasticOscillatorValue)stochValue;

		if (stochTyped.D is not decimal signalValue)
			return;

		_candles.Insert(0, candle);
		if (_candles.Count > AverageBodyPeriod + 5)
			_candles.RemoveAt(_candles.Count - 1);

		_stochSignal.Insert(0, signalValue);
		if (_stochSignal.Count > 20)
			_stochSignal.RemoveAt(_stochSignal.Count - 1);

		TryEnterPositions();
		TryExitPositions();
	}

	private void TryEnterPositions()
	{
		if (_candles.Count < AverageBodyPeriod + 2 || _stochSignal.Count < 2)
			return;

		var recent = _candles[1];
		var previous = _candles[2];
		var stochPrev = _stochSignal[1];

		if (Position <= 0 && IsPiercingLine(recent, previous) && stochPrev < LongEntryThreshold)
		{
			BuyMarket();
		}
		else if (Position >= 0 && IsDarkCloudCover(recent, previous) && stochPrev > ShortEntryThreshold)
		{
			SellMarket();
		}
	}

	private void TryExitPositions()
	{
		if (_stochSignal.Count < 3)
			return;

		var current = _stochSignal[1];
		var previous = _stochSignal[2];

		var crossedBelowUpper = current < ExitUpperThreshold && previous >= ExitUpperThreshold;
		var crossedBelowLower = current < ExitLowerThreshold && previous >= ExitLowerThreshold;
		var crossedAboveLower = current > ExitLowerThreshold && previous <= ExitLowerThreshold;
		var crossedAboveUpper = current > ExitUpperThreshold && previous <= ExitUpperThreshold;

		if (Position > 0 && (crossedBelowUpper || crossedBelowLower))
		{
			SellMarket();
		}
		else if (Position < 0 && (crossedAboveLower || crossedAboveUpper))
		{
			BuyMarket();
		}
	}

	private bool IsPiercingLine(ICandleMessage bullish, ICandleMessage bearish)
	{
		if (!HasBodiesForAverage(1))
			return false;

		var averageBody = AverageBody(1);
		if (averageBody == 0)
			return false;

		var bullishBody = Math.Abs(bullish.ClosePrice - bullish.OpenPrice);
		var bearishBody = Math.Abs(bearish.OpenPrice - bearish.ClosePrice);
		var bearishMid = (bearish.OpenPrice + bearish.ClosePrice) / 2m;
		var averageClose = AverageClose(2);

		return bullish.ClosePrice > bullish.OpenPrice
		&& bullishBody > averageBody
		&& bearish.OpenPrice > bearish.ClosePrice
		&& bearishBody > averageBody
		&& bearish.ClosePrice > bullish.ClosePrice
		&& bullish.ClosePrice < bearish.OpenPrice
		&& bearishMid < averageClose
		&& bullish.OpenPrice < bearish.LowPrice;
	}

	private bool IsDarkCloudCover(ICandleMessage bearish, ICandleMessage bullish)
	{
		if (!HasBodiesForAverage(1))
			return false;

		var averageBody = AverageBody(1);
		if (averageBody == 0)
			return false;

		var bullishBody = Math.Abs(bullish.ClosePrice - bullish.OpenPrice);
		var bearishMid = (bearish.OpenPrice + bearish.ClosePrice) / 2m;
		var averageClose = AverageClose(1);

		return bullish.ClosePrice > bullish.OpenPrice
		&& bullishBody > averageBody
		&& bearish.OpenPrice > bearish.ClosePrice
		&& bearish.ClosePrice < bullish.ClosePrice
		&& bearish.ClosePrice > bullish.OpenPrice
		&& bearishMid > averageClose
		&& bearish.OpenPrice > bullish.HighPrice;
	}

	private bool HasBodiesForAverage(int shift)
	{
		return _candles.Count >= shift + AverageBodyPeriod;
	}

	private decimal AverageBody(int shift)
	{
		var period = AverageBodyPeriod;
		if (_candles.Count < shift + period)
			return 0m;

		decimal sum = 0m;
		for (var i = 0; i < period; i++)
		{
			sum += Math.Abs(_candles[shift + i].ClosePrice - _candles[shift + i].OpenPrice);
		}

		return sum / period;
	}

	private decimal AverageClose(int shift)
	{
		var period = AverageBodyPeriod;
		if (_candles.Count < shift + period)
			return 0m;

		decimal sum = 0m;
		for (var i = 0; i < period; i++)
		{
			sum += _candles[shift + i].ClosePrice;
		}

		return sum / period;
	}
}

