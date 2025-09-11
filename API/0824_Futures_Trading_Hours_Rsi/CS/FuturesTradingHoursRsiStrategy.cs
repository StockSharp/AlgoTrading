using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI strategy that trades only during specified session hours.
/// Enters long when RSI crosses above the oversold level.
/// Enters short when RSI crosses below the overbought level.
/// Closes all positions after session ends.
/// </summary>
public class FuturesTradingHoursRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _overSoldLevel;
	private readonly StrategyParam<decimal> _overBoughtLevel;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<int> _timezoneOffset;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private bool _hasPrev;

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Oversold level for RSI.
	/// </summary>
	public decimal OverSoldLevel
	{
		get => _overSoldLevel.Value;
		set => _overSoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought level for RSI.
	/// </summary>
	public decimal OverBoughtLevel
	{
		get => _overBoughtLevel.Value;
		set => _overBoughtLevel.Value = value;
	}

	/// <summary>
	/// Session start time in Central Time.
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// Session end time in Central Time.
	/// </summary>
	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
	}

	/// <summary>
	/// Hours offset from UTC to Central Time.
	/// </summary>
	public int TimezoneOffset
	{
		get => _timezoneOffset.Value;
		set => _timezoneOffset.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FuturesTradingHoursRsiStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period of the RSI", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_overSoldLevel = Param(nameof(OverSoldLevel), 30m)
			.SetDisplay("RSI Oversold", "Oversold level", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 10m);

		_overBoughtLevel = Param(nameof(OverBoughtLevel), 70m)
			.SetDisplay("RSI Overbought", "Overbought level", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(60m, 90m, 10m);

		_sessionStart = Param(nameof(SessionStart), new TimeSpan(8, 30, 0))
			.SetDisplay("Session Start", "Start time in Central Time", "Trading Hours");

		_sessionEnd = Param(nameof(SessionEnd), new TimeSpan(15, 0, 0))
			.SetDisplay("Session End", "End time in Central Time", "Trading Hours");

		_timezoneOffset = Param(nameof(TimezoneOffset), 0)
			.SetDisplay("Timezone Offset", "Hours offset from UTC to Central Time", "Trading Hours");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevRsi = 0m;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adjustedTime = candle.OpenTime.UtcDateTime.AddHours(TimezoneOffset);
		var weekdayFilter = adjustedTime.DayOfWeek >= DayOfWeek.Monday && adjustedTime.DayOfWeek <= DayOfWeek.Friday;

		if (weekdayFilter && (adjustedTime.Hour > SessionEnd.Hours || (adjustedTime.Hour == SessionEnd.Hours && adjustedTime.Minute >= SessionEnd.Minutes)))
		{
			CancelActiveOrders();
			if (Position != 0)
				ClosePosition();
			return;
		}

		var inSession = (adjustedTime.Hour > SessionStart.Hours || (adjustedTime.Hour == SessionStart.Hours && adjustedTime.Minute >= SessionStart.Minutes)) &&
			(adjustedTime.Hour < SessionEnd.Hours || (adjustedTime.Hour == SessionEnd.Hours && adjustedTime.Minute < SessionEnd.Minutes));

		if (inSession && weekdayFilter && _hasPrev)
		{
			if (_prevRsi <= OverSoldLevel && rsiValue > OverSoldLevel && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (_prevRsi >= OverBoughtLevel && rsiValue < OverBoughtLevel && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevRsi = rsiValue;
		_hasPrev = true;
	}
}
