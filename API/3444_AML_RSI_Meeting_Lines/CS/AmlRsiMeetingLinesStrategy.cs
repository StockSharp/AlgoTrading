using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the Expert_AML_RSI MetaTrader 5 expert advisor.
/// </summary>
public class AmlRsiMeetingLinesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _bodyAveragePeriod;
	private readonly StrategyParam<decimal> _bullishRsiLevel;
	private readonly StrategyParam<decimal> _bearishRsiLevel;
	private readonly StrategyParam<decimal> _lowerExitLevel;
	private readonly StrategyParam<decimal> _upperExitLevel;

	private RelativeStrengthIndex? _rsi;
	private SimpleMovingAverage? _bodyAverage;

	private ICandleMessage _previousCandle;
	private ICandleMessage _twoBarsAgo;
	private decimal? _previousAvgBody;
	private decimal? _prevRsi;
	private decimal? _prevPrevRsi;

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI lookback period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Number of candles used to average body sizes.
	/// </summary>
	public int BodyAveragePeriod
	{
		get => _bodyAveragePeriod.Value;
		set => _bodyAveragePeriod.Value = value;
	}

	/// <summary>
	/// RSI threshold that confirms bullish Meeting Lines patterns.
	/// </summary>
	public decimal BullishRsiLevel
	{
		get => _bullishRsiLevel.Value;
		set => _bullishRsiLevel.Value = value;
	}

	/// <summary>
	/// RSI threshold that confirms bearish Meeting Lines patterns.
	/// </summary>
	public decimal BearishRsiLevel
	{
		get => _bearishRsiLevel.Value;
		set => _bearishRsiLevel.Value = value;
	}

	/// <summary>
	/// RSI level used to detect upward reversals that close short trades.
	/// </summary>
	public decimal LowerExitLevel
	{
		get => _lowerExitLevel.Value;
		set => _lowerExitLevel.Value = value;
	}

	/// <summary>
	/// RSI level used to detect downward reversals that close long trades.
	/// </summary>
	public decimal UpperExitLevel
	{
		get => _upperExitLevel.Value;
		set => _upperExitLevel.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AmlRsiMeetingLinesStrategy"/> class.
	/// </summary>
	public AmlRsiMeetingLinesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to evaluate candlestick patterns", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 11)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("RSI Period", "Number of bars used for RSI calculation", "Indicators");

		_bodyAveragePeriod = Param(nameof(BodyAveragePeriod), 3)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Body Average Period", "Number of candles used to average body sizes", "Patterns");

		_bullishRsiLevel = Param(nameof(BullishRsiLevel), 40m)
			.SetDisplay("Bullish RSI", "Maximum RSI value that confirms bullish Meeting Lines", "Signals");

		_bearishRsiLevel = Param(nameof(BearishRsiLevel), 60m)
			.SetDisplay("Bearish RSI", "Minimum RSI value that confirms bearish Meeting Lines", "Signals");

		_lowerExitLevel = Param(nameof(LowerExitLevel), 30m)
			.SetDisplay("Lower Exit Level", "RSI cross above this level closes short positions", "Risk");

		_upperExitLevel = Param(nameof(UpperExitLevel), 70m)
			.SetDisplay("Upper Exit Level", "RSI cross below this level closes long positions", "Risk");
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

		_rsi = null;
		_bodyAverage = null;
		_previousCandle = null;
		_twoBarsAgo = null;
		_previousAvgBody = null;
		_prevRsi = null;
		_prevPrevRsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		_bodyAverage = new SimpleMovingAverage
		{
			Length = BodyAveragePeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_rsi, ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_bodyAverage is null)
			return;

		// Cache data that belong to the previous candle before updating indicator states.
		var previousCandle = _previousCandle;
		var twoBarsAgo = _twoBarsAgo;
		var avgBodyForPrevious = _previousAvgBody;
		var prevRsi = _prevRsi;
		var prevPrevRsi = _prevPrevRsi;

		var canTrade = IsFormedAndOnlineAndAllowTrading();

		if (canTrade && previousCandle is not null && twoBarsAgo is not null &&
			avgBodyForPrevious is decimal avgBody && prevRsi is decimal prevRsiValue &&
			prevPrevRsi is decimal prevPrevRsiValue)
		{
			// Detect classical Meeting Lines patterns on the last two completed candles.
			var bullishPattern = IsBullishMeetingLines(twoBarsAgo, previousCandle, avgBody);
			var bearishPattern = IsBearishMeetingLines(twoBarsAgo, previousCandle, avgBody);

			// Confirm entries with RSI thresholds taken from the original expert advisor.
			var bullishSignal = bullishPattern && prevRsiValue < BullishRsiLevel;
			var bearishSignal = bearishPattern && prevRsiValue > BearishRsiLevel;

			// Exit when RSI crosses through key boundaries in the opposite direction.
			var closeShort = (prevRsiValue > LowerExitLevel && prevPrevRsiValue < LowerExitLevel) ||
				(prevRsiValue > UpperExitLevel && prevPrevRsiValue < UpperExitLevel);
			var closeLong = (prevRsiValue < UpperExitLevel && prevPrevRsiValue > UpperExitLevel) ||
				(prevRsiValue < LowerExitLevel && prevPrevRsiValue > LowerExitLevel);

			if (closeLong && Position > 0)
				ClosePosition();

			if (closeShort && Position < 0)
				ClosePosition();

			if (bullishSignal && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (bearishSignal && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		var body = Math.Abs(candle.OpenPrice - candle.ClosePrice);
		var avgValue = _bodyAverage.Process(new DecimalIndicatorValue(_bodyAverage, body, candle.OpenTime));

		if (avgValue is DecimalIndicatorValue { IsFinal: true, Value: var avgBodyValue })
			_previousAvgBody = avgBodyValue;
		else
			_previousAvgBody = null;

		_twoBarsAgo = previousCandle;
		_previousCandle = candle;

		_prevPrevRsi = prevRsi;
		_prevRsi = rsiValue;
	}

	private static bool IsBullishMeetingLines(ICandleMessage twoBarsAgo, ICandleMessage previous, decimal avgBody)
	{
		if (avgBody <= 0m)
			return false;

		// Two bars ago must be a long black candle.
		var longBlack = twoBarsAgo.OpenPrice - twoBarsAgo.ClosePrice > avgBody;
		// The most recent completed bar must be a long white candle.
		var longWhite = previous.ClosePrice - previous.OpenPrice > avgBody;
		// Closes should match within 10% of the average body size.
		var closeMatch = Math.Abs(previous.ClosePrice - twoBarsAgo.ClosePrice) < 0.1m * avgBody;

		return longBlack && longWhite && closeMatch;
	}

	private static bool IsBearishMeetingLines(ICandleMessage twoBarsAgo, ICandleMessage previous, decimal avgBody)
	{
		if (avgBody <= 0m)
			return false;

		// Two bars ago must be a long white candle.
		var longWhite = twoBarsAgo.ClosePrice - twoBarsAgo.OpenPrice > avgBody;
		// The most recent completed bar must be a long black candle.
		var longBlack = previous.OpenPrice - previous.ClosePrice > avgBody;
		// Closes should match within 10% of the average body size.
		var closeMatch = Math.Abs(previous.ClosePrice - twoBarsAgo.ClosePrice) < 0.1m * avgBody;

		return longWhite && longBlack && closeMatch;
	}
}
