using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mateo's Time of Day Analysis LE strategy.
/// Between the From and Thru dates a long position is opened once the candle time reaches StartTime and closed once it reaches
/// EndTime. Times are the candle open time in UTC.
/// </summary>
public class MateosTimeOfDayAnalysisLeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<DateTimeOffset> _from;
	private readonly StrategyParam<DateTimeOffset> _thru;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Time of day the long position opens.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Time of day the long position closes.
	/// </summary>
	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// First date entries are allowed.
	/// </summary>
	public DateTimeOffset From
	{
		get => _from.Value;
		set => _from.Value = value;
	}

	/// <summary>
	/// Last date entries are allowed.
	/// </summary>
	public DateTimeOffset Thru
	{
		get => _thru.Value;
		set => _thru.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MateosTimeOfDayAnalysisLeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_startTime = Param(nameof(StartTime), new TimeSpan(9, 30, 0))
			.SetDisplay("Start Time", "Time of day the long position opens", "Time");

		_endTime = Param(nameof(EndTime), new TimeSpan(16, 0, 0))
			.SetDisplay("End Time", "Time of day the long position closes", "Time");

		_from = Param(nameof(From), new DateTimeOffset(2017, 4, 21, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("From", "First date entries are allowed", "Time");

		_thru = Param(nameof(Thru), new DateTimeOffset(2099, 12, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Thru", "Last date entries are allowed", "Time");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var openTime = candle.OpenTime;
		var timeOfDay = openTime.TimeOfDay;
		var inSession = timeOfDay >= StartTime && timeOfDay < EndTime;
		var inDateRange = openTime >= From.UtcDateTime && openTime <= Thru.UtcDateTime;

		if (inSession && inDateRange && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (!inSession && Position > 0)
			SellMarket(Position);
	}
}
