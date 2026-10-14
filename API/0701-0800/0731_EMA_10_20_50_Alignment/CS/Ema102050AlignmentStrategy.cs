using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA 10/20/50 alignment strategy.
/// Long only: buys when EMA(10) is above EMA(20) and EMA(20) is above EMA(50) and closes the long when the three EMAs align
/// in descending order. Entries are allowed only for candles between StartTime and EndTime.
/// </summary>
public class Ema102050AlignmentStrategy : Strategy
{
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Start of the trading date range.
	/// </summary>
	public DateTimeOffset StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// End of the trading date range.
	/// </summary>
	public DateTimeOffset EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public Ema102050AlignmentStrategy()
	{
		_startTime = Param(nameof(StartTime), new DateTimeOffset(2023, 5, 17, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Time", "Start of the trading date range", "General");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(2025, 5, 17, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Time", "End of the trading date range", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		var ema10 = new ExponentialMovingAverage { Length = 10 };
		var ema20 = new ExponentialMovingAverage { Length = 20 };
		var ema50 = new ExponentialMovingAverage { Length = 50 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema10, ema20, ema50, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema10);
			DrawIndicator(area, ema20);
			DrawIndicator(area, ema50);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema10, decimal ema20, decimal ema50)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var inRange = candle.OpenTime >= StartTime.UtcDateTime && candle.OpenTime <= EndTime.UtcDateTime;

		if (Position <= 0 && inRange && ema10 > ema20 && ema20 > ema50)
			BuyMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && ema10 < ema20 && ema20 < ema50)
			SellMarket(Position);
	}
}
