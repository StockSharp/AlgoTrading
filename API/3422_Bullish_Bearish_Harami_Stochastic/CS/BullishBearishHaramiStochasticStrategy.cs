using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy detecting Bullish and Bearish Harami patterns confirmed by a stochastic oscillator.
/// The logic follows the MQL expert from MQL/310 where candlestick patterns are filtered by stochastic oversold and overbought levels.
/// </summary>
public class BullishBearishHaramiStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<int> _movingAveragePeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _exitLowerLevel;
	private readonly StrategyParam<decimal> _exitUpperLevel;

	private StochasticOscillator _stochastic = null!;
	private SimpleMovingAverage _closeAverage = null!;
	private SimpleMovingAverage _bodyAverage = null!;

	private ICandleMessage _previousCandle;
	private ICandleMessage _twoCandlesAgo;
	private decimal? _previousCloseAverage;
	private decimal? _twoCandlesAgoCloseAverage;
	private decimal? _previousBodyAverage;
	private decimal? _previousSignal;

	/// <summary>
	/// Initializes a new instance of the <see cref="BullishBearishHaramiStochasticStrategy"/> class.
	/// </summary>
	public BullishBearishHaramiStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for pattern recognition", "General");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 47)
			.SetDisplay("%K Period", "Lookback for stochastic %K", "Stochastic");

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 9)
			.SetDisplay("%D Period", "Smoothing for stochastic %D", "Stochastic");

		_stochasticSlowing = Param(nameof(StochasticSlowing), 13)
			.SetDisplay("Slowing", "Additional smoothing for %K", "Stochastic");

		_movingAveragePeriod = Param(nameof(MovingAveragePeriod), 5)
			.SetDisplay("MA Period", "Period for candle body average", "Patterns");

		_oversoldLevel = Param(nameof(OversoldLevel), 30m)
			.SetDisplay("Oversold", "Entry threshold for long signals", "Stochastic");

		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
			.SetDisplay("Overbought", "Entry threshold for short signals", "Stochastic");

		_exitLowerLevel = Param(nameof(ExitLowerLevel), 20m)
			.SetDisplay("Exit Lower", "Lower stochastic exit threshold", "Stochastic");

		_exitUpperLevel = Param(nameof(ExitUpperLevel), 80m)
			.SetDisplay("Exit Upper", "Upper stochastic exit threshold", "Stochastic");
	}

	/// <summary>
	/// Candle type and timeframe for the strategy.
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
	/// Additional slowing applied to %K.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Period used to average candle body size.
	/// </summary>
	public int MovingAveragePeriod
	{
		get => _movingAveragePeriod.Value;
		set => _movingAveragePeriod.Value = value;
	}

	/// <summary>
	/// Oversold level used to confirm bullish signals.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought level used to confirm bearish signals.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Lower stochastic threshold used to exit short positions.
	/// </summary>
	public decimal ExitLowerLevel
	{
		get => _exitLowerLevel.Value;
		set => _exitLowerLevel.Value = value;
	}

	/// <summary>
	/// Upper stochastic threshold used to exit positions.
	/// </summary>
	public decimal ExitUpperLevel
	{
		get => _exitUpperLevel.Value;
		set => _exitUpperLevel.Value = value;
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

		_previousCandle = null;
		_twoCandlesAgo = null;
		_previousCloseAverage = null;
		_twoCandlesAgoCloseAverage = null;
		_previousBodyAverage = null;
		_previousSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stochastic = new StochasticOscillator
		{
			Length = Math.Max(StochasticKPeriod, 1),
			K = { Length = Math.Max(StochasticSlowing, 1) },
			D = { Length = Math.Max(StochasticDPeriod, 1) }
		};

		_closeAverage = new SimpleMovingAverage { Length = Math.Max(MovingAveragePeriod, 1) };
		_bodyAverage = new SimpleMovingAverage { Length = Math.Max(MovingAveragePeriod, 1) };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawOwnTrades(priceArea);

			var indicatorArea = CreateChartArea();
			if (indicatorArea != null)
			{
				DrawIndicator(indicatorArea, _stochastic);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochasticValue.IsFinal || !IsFormedAndOnlineAndAllowTrading())
			return;

		var stoch = (StochasticOscillatorValue)stochasticValue;
		if (stoch.D is not decimal signalValue)
			return;

		_stochastic.Length = Math.Max(StochasticKPeriod, 1);
		_stochastic.K.Length = Math.Max(StochasticSlowing, 1);
		_stochastic.D.Length = Math.Max(StochasticDPeriod, 1);

		_closeAverage.Length = Math.Max(MovingAveragePeriod, 1);
		_bodyAverage.Length = Math.Max(MovingAveragePeriod, 1);

		var closeAverageValue = _closeAverage.Process(new CandleIndicatorValue(candle, candle.ClosePrice));
		var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var bodyAverageValue = _bodyAverage.Process(new DecimalIndicatorValue(_bodyAverage, bodySize, candle.OpenTime));

		var currentCloseAverage = closeAverageValue.IsFinal ? closeAverageValue.ToDecimal() : (decimal?)null;
		var currentBodyAverage = bodyAverageValue.IsFinal ? bodyAverageValue.ToDecimal() : (decimal?)null;

		if (_previousCandle != null && _twoCandlesAgo != null && _previousBodyAverage.HasValue && _twoCandlesAgoCloseAverage.HasValue)
		{
			var bullishHarami = IsBullishHarami(_previousCandle, _twoCandlesAgo, _previousBodyAverage.Value, _twoCandlesAgoCloseAverage.Value);
			var bearishHarami = IsBearishHarami(_previousCandle, _twoCandlesAgo, _previousBodyAverage.Value, _twoCandlesAgoCloseAverage.Value);

			if (bullishHarami && signalValue <= OversoldLevel && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				LogInfo($"Bullish Harami with stochastic confirmation. Signal={signalValue:F2}");
			}
			else if (bearishHarami && signalValue >= OverboughtLevel && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				LogInfo($"Bearish Harami with stochastic confirmation. Signal={signalValue:F2}");
			}
		}

		if (_previousSignal.HasValue)
		{
			var previousSignal = _previousSignal.Value;
			var crossedAboveLower = previousSignal < ExitLowerLevel && signalValue >= ExitLowerLevel;
			var crossedAboveUpper = previousSignal < ExitUpperLevel && signalValue >= ExitUpperLevel;
			var crossedBelowUpper = previousSignal > ExitUpperLevel && signalValue <= ExitUpperLevel;
			var crossedBelowLower = previousSignal > ExitLowerLevel && signalValue <= ExitLowerLevel;

			if (Position < 0 && (crossedAboveLower || crossedAboveUpper))
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Covering short due to stochastic rebound {previousSignal:F2}->{signalValue:F2}");
			}
			else if (Position > 0 && (crossedBelowUpper || crossedBelowLower))
			{
				SellMarket(Position);
				LogInfo($"Closing long due to stochastic pullback {previousSignal:F2}->{signalValue:F2}");
			}
		}

		_twoCandlesAgo = _previousCandle;
		_previousCandle = candle;
		_previousSignal = signalValue;

		_twoCandlesAgoCloseAverage = _previousCloseAverage;
		if (currentCloseAverage.HasValue)
			_previousCloseAverage = currentCloseAverage;

		if (currentBodyAverage.HasValue)
			_previousBodyAverage = currentBodyAverage;
	}

	private static bool IsBullishHarami(ICandleMessage previous, ICandleMessage older, decimal avgBody, decimal olderCloseAverage)
	{
		var olderBody = Math.Abs(older.OpenPrice - older.ClosePrice);

		var isWhiteDay = previous.ClosePrice > previous.OpenPrice;
		var isLongBlack = older.OpenPrice > older.ClosePrice && olderBody > avgBody;
		var isEngulfed = previous.ClosePrice < older.OpenPrice && previous.OpenPrice > older.ClosePrice;
		var isDownTrend = (older.HighPrice + older.LowPrice) / 2m < olderCloseAverage;

		return isWhiteDay && isLongBlack && isEngulfed && isDownTrend;
	}

	private static bool IsBearishHarami(ICandleMessage previous, ICandleMessage older, decimal avgBody, decimal olderCloseAverage)
	{
		var olderBody = Math.Abs(older.ClosePrice - older.OpenPrice);

		var isBlackDay = previous.ClosePrice < previous.OpenPrice;
		var isLongWhite = older.ClosePrice > older.OpenPrice && olderBody > avgBody;
		var isEngulfed = previous.ClosePrice > older.OpenPrice && previous.OpenPrice < older.ClosePrice;
		var isUpTrend = (older.HighPrice + older.LowPrice) / 2m > olderCloseAverage;

		return isBlackDay && isLongWhite && isEngulfed && isUpTrend;
	}
}
