using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Morning and Evening Star pattern strategy confirmed by the Stochastic oscillator.
/// Ported from the MetaTrader Expert Advisor "Expert_AMS_ES_Stoch" to the StockSharp high level API.
/// </summary>
public class MorningEveningStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<decimal> _stochasticOverbought;
	private readonly StrategyParam<decimal> _stochasticOversold;
	private readonly StrategyParam<int> _patternAveragePeriod;
	private readonly StrategyParam<decimal> _shortExitLevel;
	private readonly StrategyParam<decimal> _longExitLevel;

	private StochasticOscillator? _stochastic;
	private SimpleMovingAverage? _bodyAverage;

	private ICandleMessage _previousCandle;
	private ICandleMessage _previousPreviousCandle;
	private decimal? _previousStochSignal;

	/// <summary>
	/// Candle type used for pattern detection and indicator calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic slowing parameter.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Stochastic overbought level for short entries.
	/// </summary>
	public decimal StochasticOverbought
	{
		get => _stochasticOverbought.Value;
		set => _stochasticOverbought.Value = value;
	}

	/// <summary>
	/// Stochastic oversold level for long entries.
	/// </summary>
	public decimal StochasticOversold
	{
		get => _stochasticOversold.Value;
		set => _stochasticOversold.Value = value;
	}

	/// <summary>
	/// Number of candles used to average the candlestick body length.
	/// </summary>
	public int PatternAveragePeriod
	{
		get => _patternAveragePeriod.Value;
		set => _patternAveragePeriod.Value = value;
	}

	/// <summary>
	/// %D level that forces short positions to exit when crossed from below.
	/// </summary>
	public decimal ShortExitLevel
	{
		get => _shortExitLevel.Value;
		set => _shortExitLevel.Value = value;
	}

	/// <summary>
	/// %D level that forces long positions to exit when crossed from above.
	/// </summary>
	public decimal LongExitLevel
	{
		get => _longExitLevel.Value;
		set => _longExitLevel.Value = value;
	}

	/// <summary>
	/// Initializes parameters with defaults matching the original expert advisor.
	/// </summary>
	public MorningEveningStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles and indicators", "General");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Stochastic %K lookback period", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(6, 36, 2);

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Stochastic %D smoothing period", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(3, 18, 1);

		_stochasticSlowing = Param(nameof(StochasticSlowing), 29)
			.SetGreaterThanZero()
			.SetDisplay("Slowing", "Stochastic slowing value", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(1, 40, 2);

		_stochasticOverbought = Param(nameof(StochasticOverbought), 70m)
			.SetDisplay("Overbought", "Stochastic %D threshold for short entries", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(60m, 90m, 5m);

		_stochasticOversold = Param(nameof(StochasticOversold), 30m)
			.SetDisplay("Oversold", "Stochastic %D threshold for long entries", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_patternAveragePeriod = Param(nameof(PatternAveragePeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("Body Average", "Number of candles used for body average", "Candlestick Pattern")
			.SetCanOptimize(true)
			.SetOptimize(3, 12, 1);

		_shortExitLevel = Param(nameof(ShortExitLevel), 20m)
			.SetDisplay("Short Exit %D", "Level that closes shorts when crossed upward", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_longExitLevel = Param(nameof(LongExitLevel), 80m)
			.SetDisplay("Long Exit %D", "Level that closes longs when crossed downward", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(60m, 90m, 5m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_stochastic = null;
		_bodyAverage = null;
		_previousCandle = null;
		_previousPreviousCandle = null;
		_previousStochSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stochastic = new StochasticOscillator
		{
			KPeriod = StochasticKPeriod,
			DPeriod = StochasticDPeriod,
			Smooth = StochasticSlowing,
		};

		_bodyAverage = new SimpleMovingAverage
		{
			Length = PatternAveragePeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_stochastic == null || _bodyAverage == null)
			return;

		if (!indicatorValue.IsFinal)
			return;

		if (indicatorValue is not StochasticOscillatorValue stoch)
			return;

		if (stoch.D is not decimal stochSignal)
			return;

		var previousSignal = _previousStochSignal;

		var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var avgValue = _bodyAverage.Process(new DecimalIndicatorValue(_bodyAverage, bodySize, candle.OpenTime));
		if (avgValue is not DecimalIndicatorValue { IsFinal: true, Value: var averageBody })
		{
			UpdateState(candle, stochSignal);
			return;
		}

		var hasPatternHistory = _previousCandle != null && _previousPreviousCandle != null;
		var isMorningStar = hasPatternHistory && IsMorningStar(_previousPreviousCandle!, _previousCandle!, candle, averageBody);
		var isEveningStar = hasPatternHistory && IsEveningStar(_previousPreviousCandle!, _previousCandle!, candle, averageBody);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateState(candle, stochSignal);
			return;
		}

		if (previousSignal.HasValue)
		{
			var shouldCloseShort = Position < 0m && (
				(stochSignal > ShortExitLevel && previousSignal.Value < ShortExitLevel) ||
				(stochSignal > LongExitLevel && previousSignal.Value < LongExitLevel));

			if (shouldCloseShort)
			{
				var coverVolume = Math.Abs(Position);
				if (coverVolume > 0m)
				{
					BuyMarket(coverVolume);
					LogInfo($"Closing short position because %D crossed above exit levels: {previousSignal.Value:F2} -> {stochSignal:F2}.");
				}
			}

			var shouldCloseLong = Position > 0m && (
				(stochSignal < LongExitLevel && previousSignal.Value > LongExitLevel) ||
				(stochSignal < ShortExitLevel && previousSignal.Value > ShortExitLevel));

			if (shouldCloseLong)
			{
				var exitVolume = Math.Abs(Position);
				if (exitVolume > 0m)
				{
					SellMarket(exitVolume);
					LogInfo($"Closing long position because %D crossed below exit levels: {previousSignal.Value:F2} -> {stochSignal:F2}.");
				}
			}
		}

		if (isMorningStar && stochSignal < StochasticOversold && Position <= 0m)
		{
			if (Position < 0m)
			{
				var coverVolume = Math.Abs(Position);
				if (coverVolume > 0m)
				{
					BuyMarket(coverVolume);
					LogInfo("Morning Star signal detected. Closing existing short exposure before entering long.");
				}
			}

			if (Volume > 0m)
			{
				BuyMarket(Volume);
				LogInfo($"Morning Star + Stochastic confirmation. Buying {Volume} at {candle.ClosePrice}.");
			}
		}
		else if (isEveningStar && stochSignal > StochasticOverbought && Position >= 0m)
		{
			if (Position > 0m)
			{
				var exitVolume = Math.Abs(Position);
				if (exitVolume > 0m)
				{
					SellMarket(exitVolume);
					LogInfo("Evening Star signal detected. Closing existing long exposure before entering short.");
				}
			}

			if (Volume > 0m)
			{
				SellMarket(Volume);
				LogInfo($"Evening Star + Stochastic confirmation. Selling {Volume} at {candle.ClosePrice}.");
			}
		}

		UpdateState(candle, stochSignal);
	}

	private void UpdateState(ICandleMessage candle, decimal stochSignal)
	{
		_previousPreviousCandle = _previousCandle;
		_previousCandle = candle;
		_previousStochSignal = stochSignal;
	}

	private static bool IsMorningStar(ICandleMessage first, ICandleMessage second, ICandleMessage third, decimal averageBody)
	{
		if (averageBody <= 0m)
			return false;

		var firstBody = first.OpenPrice - first.ClosePrice;
		if (firstBody <= averageBody)
			return false;

		var secondBody = Math.Abs(second.ClosePrice - second.OpenPrice);
		if (secondBody >= averageBody * 0.5m)
			return false;

		if (second.ClosePrice >= first.ClosePrice || second.OpenPrice >= first.OpenPrice)
			return false;

		if (third.ClosePrice <= third.OpenPrice)
			return false;

		var midpoint = (first.OpenPrice + first.ClosePrice) / 2m;
		return third.ClosePrice > midpoint;
	}

	private static bool IsEveningStar(ICandleMessage first, ICandleMessage second, ICandleMessage third, decimal averageBody)
	{
		if (averageBody <= 0m)
			return false;

		var firstBody = first.ClosePrice - first.OpenPrice;
		if (firstBody <= averageBody)
			return false;

		var secondBody = Math.Abs(second.ClosePrice - second.OpenPrice);
		if (secondBody >= averageBody * 0.5m)
			return false;

		if (second.ClosePrice <= first.ClosePrice || second.OpenPrice <= first.OpenPrice)
			return false;

		if (third.ClosePrice >= third.OpenPrice)
			return false;

		var midpoint = (first.OpenPrice + first.ClosePrice) / 2m;
		return third.ClosePrice < midpoint;
	}
}