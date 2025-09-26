namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Meeting Lines candlestick strategy with MFI confirmation.
/// Detects bullish or bearish Meeting Lines patterns, validates them by the MFI oscillator,
/// and manages positions using oversold and overbought cross signals.
/// </summary>
public class ExpertAmlMfiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<int> _bodyAveragePeriod;
	private readonly StrategyParam<decimal> _bullishEntryLevel;
	private readonly StrategyParam<decimal> _bearishEntryLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _tradeVolume;

	private readonly SimpleMovingAverage _bodySma = new();

	private decimal? _previousBodyAverage;
	private ICandleMessage _previousCandle;
	private ICandleMessage _secondPreviousCandle;
	private decimal? _previousMfi;
	private decimal? _secondPreviousMfi;

	/// <summary>
	/// Constructor.
	/// </summary>
	public ExpertAmlMfiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for pattern detection", "General");

		_mfiPeriod = Param(nameof(MfiPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MFI Period", "Number of bars used by the MFI oscillator", "Indicator")
			.SetCanOptimize(true);

		_bodyAveragePeriod = Param(nameof(BodyAveragePeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("Body Average Period", "Window for candle body size averaging", "Indicator")
			.SetCanOptimize(true);

		_bullishEntryLevel = Param<decimal>(nameof(BullishEntryLevel), 40m)
			.SetDisplay("Bullish Entry Level", "Maximum MFI value for bullish entries", "Signals")
			.SetCanOptimize(true);

		_bearishEntryLevel = Param<decimal>(nameof(BearishEntryLevel), 60m)
			.SetDisplay("Bearish Entry Level", "Minimum MFI value for bearish entries", "Signals")
			.SetCanOptimize(true);

		_oversoldLevel = Param<decimal>(nameof(OversoldLevel), 30m)
			.SetDisplay("Oversold Level", "Threshold used for oversold cross signals", "Signals")
			.SetCanOptimize(true);

		_overboughtLevel = Param<decimal>(nameof(OverboughtLevel), 70m)
			.SetDisplay("Overbought Level", "Threshold used for overbought cross signals", "Signals")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base volume for new positions", "Trading");
	}

	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// MFI calculation period.
	/// </summary>
	public int MfiPeriod { get => _mfiPeriod.Value; set => _mfiPeriod.Value = value; }

	/// <summary>
	/// Number of bodies used for the average body calculation.
	/// </summary>
	public int BodyAveragePeriod { get => _bodyAveragePeriod.Value; set => _bodyAveragePeriod.Value = value; }

	/// <summary>
	/// Upper bound for confirming bullish entries.
	/// </summary>
	public decimal BullishEntryLevel { get => _bullishEntryLevel.Value; set => _bullishEntryLevel.Value = value; }

	/// <summary>
	/// Lower bound for confirming bearish entries.
	/// </summary>
	public decimal BearishEntryLevel { get => _bearishEntryLevel.Value; set => _bearishEntryLevel.Value = value; }

	/// <summary>
	/// Oversold level used for exit signals.
	/// </summary>
	public decimal OversoldLevel { get => _oversoldLevel.Value; set => _oversoldLevel.Value = value; }

	/// <summary>
	/// Overbought level used for exit signals.
	/// </summary>
	public decimal OverboughtLevel { get => _overboughtLevel.Value; set => _overboughtLevel.Value = value; }

	/// <summary>
	/// Base trade volume.
	/// </summary>
	public decimal TradeVolume { get => _tradeVolume.Value; set => _tradeVolume.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bodySma.Reset();
		_previousBodyAverage = null;
		_previousCandle = null;
		_secondPreviousCandle = null;
		_previousMfi = null;
		_secondPreviousMfi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		_bodySma.Length = BodyAveragePeriod;
		_bodySma.Reset();
		_previousBodyAverage = null;

		var mfi = new MoneyFlowIndex
		{
			Length = MfiPeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(mfi, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, mfi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal currentMfi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (IsFormedAndOnlineAndAllowTrading() &&
			_previousCandle != null &&
			_secondPreviousCandle != null &&
			_previousBodyAverage.HasValue &&
			_previousBodyAverage.Value > 0m &&
			_previousMfi.HasValue)
		{
			// Validate the Meeting Lines pattern using cached candles and the average body length.
			var prevMfi = _previousMfi.Value;
			var avgBody = _previousBodyAverage.Value;
			var bullPattern = IsBullishMeetingLines(_previousCandle, _secondPreviousCandle, avgBody);
			var bearPattern = IsBearishMeetingLines(_previousCandle, _secondPreviousCandle, avgBody);

			if (bearPattern && prevMfi > BearishEntryLevel)
			{
				// Reverse to a short position when a bearish pattern is confirmed by a strong MFI reading.
				if (Position > 0m)
					SellMarket(Math.Abs(Position));

				if (Position >= 0m)
					SellMarket(Volume + Math.Abs(Position));
			}

			if (bullPattern && prevMfi < BullishEntryLevel)
			{
				// Reverse to a long position when a bullish pattern aligns with a low MFI value.
				if (Position < 0m)
					BuyMarket(Math.Abs(Position));

				if (Position <= 0m)
					BuyMarket(Volume + Math.Abs(Position));
			}

			if (_secondPreviousMfi.HasValue)
			{
				// Evaluate exit signals generated by MFI crossings of oversold and overbought levels.
				var olderMfi = _secondPreviousMfi.Value;
				var crossedAboveOversold = prevMfi > OversoldLevel && olderMfi < OversoldLevel;
				var crossedAboveOverbought = prevMfi > OverboughtLevel && olderMfi < OverboughtLevel;
				var crossedBelowOversold = prevMfi < OversoldLevel && olderMfi > OversoldLevel;
				var crossedBelowOverbought = prevMfi < OverboughtLevel && olderMfi > OverboughtLevel;

				if (Position < 0m && (crossedAboveOversold || crossedAboveOverbought))
					BuyMarket(Math.Abs(Position));

				if (Position > 0m && (crossedAboveOverbought || crossedBelowOversold))
					SellMarket(Math.Abs(Position));
			}
		}

		UpdateState(candle, currentMfi);
	}

	private void UpdateState(ICandleMessage candle, decimal currentMfi)
	{
		// Update cached indicators and candle references after the signal processing step.
		var body = Math.Abs(candle.OpenPrice - candle.ClosePrice);
		var bodyValue = _bodySma.Process(candle.OpenTime, body);
		_previousBodyAverage = bodyValue.IsFinal ? bodyValue.GetValue<decimal>() : null;

		_secondPreviousCandle = _previousCandle;
		_previousCandle = candle;

		_secondPreviousMfi = _previousMfi;
		_previousMfi = currentMfi;
	}

	private static bool IsBullishMeetingLines(ICandleMessage previous, ICandleMessage older, decimal avgBody)
	{
		// Bullish Meeting Lines require a long bearish candle followed by a long bullish candle with similar closes.
		var olderRange = older.OpenPrice - older.ClosePrice;
		var previousRange = previous.ClosePrice - previous.OpenPrice;
		var closeGap = Math.Abs(previous.ClosePrice - older.ClosePrice);

		return olderRange > avgBody &&
			previousRange > avgBody &&
			closeGap < 0.1m * avgBody;
	}

	private static bool IsBearishMeetingLines(ICandleMessage previous, ICandleMessage older, decimal avgBody)
	{
		// Bearish Meeting Lines require a long bullish candle followed by a long bearish candle with matching closes.
		var olderRange = older.ClosePrice - older.OpenPrice;
		var previousRange = previous.OpenPrice - previous.ClosePrice;
		var closeGap = Math.Abs(previous.ClosePrice - older.ClosePrice);

		return olderRange > avgBody &&
			previousRange > avgBody &&
			closeGap < 0.1m * avgBody;
	}
}
