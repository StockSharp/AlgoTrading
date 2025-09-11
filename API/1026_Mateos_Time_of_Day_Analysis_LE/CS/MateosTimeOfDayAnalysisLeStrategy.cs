namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Mateo's Time of Day Analysis LE strategy.
/// Enters long during a specified intraday window and closes positions later.
/// </summary>
public class MateosTimeOfDayAnalysisLeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<DateTimeOffset> _from;
	private readonly StrategyParam<DateTimeOffset> _thru;

	public MateosTimeOfDayAnalysisLeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_startTime = Param(nameof(StartTime), new TimeSpan(9, 30, 0))
			.SetDisplay("Start Time", "Entry window start", "Time");

		_endTime = Param(nameof(EndTime), new TimeSpan(16, 0, 0))
			.SetDisplay("End Time", "Entry window end", "Time");

		_from = Param(nameof(From), new DateTimeOffset(2017, 4, 21, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("From", "Start date for signals", "Time");

		_thru = Param(nameof(Thru), new DateTimeOffset(2099, 12, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Thru", "End date for signals", "Time");
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Entry window start.
	/// </summary>
	public TimeSpan StartTime { get => _startTime.Value; set => _startTime.Value = value; }

	/// <summary>
	/// Entry window end.
	/// </summary>
	public TimeSpan EndTime { get => _endTime.Value; set => _endTime.Value = value; }

	/// <summary>
	/// Start date for signals.
	/// </summary>
	public DateTimeOffset From { get => _from.Value; set => _from.Value = value; }

	/// <summary>
	/// End date for signals.
	/// </summary>
	public DateTimeOffset Thru { get => _thru.Value; set => _thru.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

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

		var time = candle.OpenTime;

		if (time < From || time > Thru)
			return;

		var tod = time.TimeOfDay;

		if (tod >= StartTime && tod < EndTime)
		{
			if (Position <= 0)
				BuyMarket();
		}
		else if (tod >= EndTime && tod < new TimeSpan(20, 0, 0))
		{
			if (Position != 0)
				CloseAll();
		}
	}
}
