using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NASDAQ 100 peak hours strategy.
/// Trades only during the first two and last hours of the session using EMA trend, RSI, ATR and VWAP filters.
/// </summary>
public class Nasdaq100PeakHoursStrategy : Strategy
{
	private readonly StrategyParam<int> _longEmaLength;
	private readonly StrategyParam<int> _shortEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _trailAtrMultiplier;
	private readonly StrategyParam<decimal> _initialStopMultiplier;
	private readonly StrategyParam<decimal> _breakEvenMultiplier;
	private readonly StrategyParam<int> _timeExitBars;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Length of the long term EMA.
	/// </summary>
	public int LongEmaLength
	{
		get => _longEmaLength.Value;
		set => _longEmaLength.Value = value;
	}

	/// <summary>
	/// Length of the short term EMA.
	/// </summary>
	public int ShortEmaLength
	{
		get => _shortEmaLength.Value;
		set => _shortEmaLength.Value = value;
	}

	/// <summary>
	/// Length of the RSI.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Length of the ATR.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier for trailing stop.
	/// </summary>
	public decimal TrailAtrMultiplier
	{
		get => _trailAtrMultiplier.Value;
		set => _trailAtrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for initial stop.
	/// </summary>
	public decimal InitialStopMultiplier
	{
		get => _initialStopMultiplier.Value;
		set => _initialStopMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier to move stop to break-even.
	/// </summary>
	public decimal BreakEvenMultiplier
	{
		get => _breakEvenMultiplier.Value;
		set => _breakEvenMultiplier.Value = value;
	}

	/// <summary>
	/// Number of bars to exit position.
	/// </summary>
	public int TimeExitBars
	{
		get => _timeExitBars.Value;
		set => _timeExitBars.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Nasdaq100PeakHoursStrategy"/> class.
	/// </summary>
	public Nasdaq100PeakHoursStrategy()
	{
		_longEmaLength = Param(nameof(LongEmaLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA", "Length of the long term EMA", "Parameters");

		_shortEmaLength = Param(nameof(ShortEmaLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA", "Length of the short term EMA", "Parameters");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI", "Length of the RSI", "Parameters");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR", "Length of the ATR", "Parameters");

		_trailAtrMultiplier = Param(nameof(TrailAtrMultiplier), 1.5m)
			.SetDisplay("Trail ATR Mult", "ATR multiplier for trailing stop", "Stops");

		_initialStopMultiplier = Param(nameof(InitialStopMultiplier), 0.5m)
			.SetDisplay("Initial SL Mult", "ATR multiplier for initial stop", "Stops");

		_breakEvenMultiplier = Param(nameof(BreakEvenMultiplier), 1.5m)
			.SetDisplay("Break-even ATR Mult", "ATR multiplier to move stop to break-even", "Stops");

		_timeExitBars = Param(nameof(TimeExitBars), 20)
			.SetGreaterThanZero()
			.SetDisplay("Time Exit Bars", "Number of bars to hold position", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		var longEma = new EMA { Length = LongEmaLength };
		var shortEma = new EMA { Length = ShortEmaLength };
		var rsi = new RSI { Length = RsiLength };
		var atr = new ATR { Length = AtrLength };
		var atrSma = new SMA { Length = 20 };
		var vwap = new VWAP();

		var subscription = SubscribeCandles(CandleType);

		var startSession = new TimeSpan(9, 30, 0);
		var endSession = new TimeSpan(11, 30, 0);
		var extendedStart = new TimeSpan(8, 0, 0);
		var extendedEnd = new TimeSpan(16, 0, 0);

		decimal prevLongEma = 0m;
		decimal prevShortEma = 0m;
		var hasPrevLong = false;
		var hasPrevShort = false;
		int? longEntryBar = null;
		int? shortEntryBar = null;
		decimal longEntryPrice = 0m;
		decimal shortEntryPrice = 0m;
		decimal? longStop = null;
		decimal? shortStop = null;
		var barIndex = 0;

		subscription.Bind(longEma, shortEma, rsi, atr, vwap, (candle, longValue, shortValue, rsiValue, atrValue, vwapValue) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			var atrSmaVal = atrSma.Process(new DecimalIndicatorValue(atrSma, atrValue));
			var atrSmaDec = atrSmaVal.IsFinal ? atrSmaVal.GetValue<decimal>() : 0m;
			var volatilityFilter = atrValue > atrSmaDec;

			var longEmaDirection = hasPrevLong && longValue > prevLongEma;
			var shortEmaDirection = hasPrevShort && shortValue > prevShortEma;

			var time = candle.OpenTime.TimeOfDay;
			var inExtended = time >= extendedStart && time <= extendedEnd && volatilityFilter;
			var inSession = time >= startSession && time <= endSession;

			var longCondition = candle.ClosePrice > shortValue &&
				shortValue > longValue &&
				longEmaDirection &&
				shortEmaDirection &&
				rsiValue > 50 &&
				candle.ClosePrice > vwapValue &&
				inExtended &&
				inSession;

			var shortCondition = candle.ClosePrice < shortValue &&
				shortValue < longValue &&
				!longEmaDirection &&
				!shortEmaDirection &&
				rsiValue < 50 &&
				candle.ClosePrice < vwapValue &&
				inExtended &&
				inSession;

			if (longCondition && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				longEntryBar = barIndex;
				longEntryPrice = candle.ClosePrice;
				longStop = candle.ClosePrice - atrValue * InitialStopMultiplier;
				shortEntryBar = null;
				shortStop = null;
			}
			else if (shortCondition && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				shortEntryBar = barIndex;
				shortEntryPrice = candle.ClosePrice;
				shortStop = candle.ClosePrice + atrValue * InitialStopMultiplier;
				longEntryBar = null;
				longStop = null;
			}

			if (Position > 0)
			{
				var trail = candle.ClosePrice - atrValue * TrailAtrMultiplier;
				if (longStop is null || trail > longStop)
					longStop = trail;

				if (candle.ClosePrice <= longStop)
				{
					SellMarket(Math.Abs(Position));
					longEntryBar = null;
					longStop = null;
				}

				if (candle.ClosePrice > longEntryPrice + atrValue * BreakEvenMultiplier && longStop < longEntryPrice)
					longStop = longEntryPrice;

				if (longEntryBar.HasValue && barIndex - longEntryBar.Value >= TimeExitBars)
				{
					SellMarket(Math.Abs(Position));
					longEntryBar = null;
					longStop = null;
				}

				if (shortValue < longValue && !longEmaDirection)
				{
					SellMarket(Math.Abs(Position));
					longEntryBar = null;
					longStop = null;
				}
			}
			else if (Position < 0)
			{
				var trail = candle.ClosePrice + atrValue * TrailAtrMultiplier;
				if (shortStop is null || trail < shortStop)
					shortStop = trail;

				if (candle.ClosePrice >= shortStop)
				{
					BuyMarket(Math.Abs(Position));
					shortEntryBar = null;
					shortStop = null;
				}

				if (candle.ClosePrice < shortEntryPrice - atrValue * BreakEvenMultiplier && shortStop > shortEntryPrice)
					shortStop = shortEntryPrice;

				if (shortEntryBar.HasValue && barIndex - shortEntryBar.Value >= TimeExitBars)
				{
					BuyMarket(Math.Abs(Position));
					shortEntryBar = null;
					shortStop = null;
				}

				if (shortValue > longValue && !shortEmaDirection)
				{
					BuyMarket(Math.Abs(Position));
					shortEntryBar = null;
					shortStop = null;
				}
			}

			prevLongEma = longValue;
			prevShortEma = shortValue;
			hasPrevLong = longEma.IsFormed;
			hasPrevShort = shortEma.IsFormed;
			barIndex++;
		})
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, longEma);
			DrawIndicator(area, shortEma);
			DrawIndicator(area, vwap);
			DrawOwnTrades(area);
		}
	}
}
