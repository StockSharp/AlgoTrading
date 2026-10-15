namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Buys below the previous N-bar low and closes the long above the previous bar's high.
/// </summary>
public class BuyOn5DayLowStrategy : Strategy
{
	private readonly StrategyParam<int> _lowestPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;
	private readonly Queue<decimal> _lows = new();
	private decimal? _previousHigh;

	public int LowestPeriod { get => _lowestPeriod.Value; set => _lowestPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public DateTimeOffset StartTime { get => _startTime.Value; set => _startTime.Value = value; }
	public DateTimeOffset EndTime { get => _endTime.Value; set => _endTime.Value = value; }

	public BuyOn5DayLowStrategy()
	{
		_lowestPeriod = Param(nameof(LowestPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lowest Period", "Number of previous candles in the low window", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_startTime = Param(nameof(StartTime), new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Time", "Beginning of the trading window", "General");
		_endTime = Param(nameof(EndTime), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Time", "End of the trading window", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_lows.Clear();
		_previousHigh = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
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

		// Evaluate before adding this candle: its own low cannot be the entry threshold.
		if (_lows.Count == LowestPeriod && IsFormedAndOnlineAndAllowTrading()
			&& candle.OpenTime >= StartTime.UtcDateTime && candle.OpenTime <= EndTime.UtcDateTime)
		{
			if (Position == 0 && candle.ClosePrice < _lows.Min())
				BuyMarket();
			else if (Position > 0 && candle.ClosePrice > _previousHigh)
				SellMarket(Position);
		}

		_lows.Enqueue(candle.LowPrice);
		while (_lows.Count > LowestPeriod)
			_lows.Dequeue();
		_previousHigh = candle.HighPrice;
	}
}
