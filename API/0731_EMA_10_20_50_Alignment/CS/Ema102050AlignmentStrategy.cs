using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only strategy entering when EMA(10) > EMA(20) > EMA(50).
/// Closes the position when EMAs align downward.
/// </summary>
public class Ema102050AlignmentStrategy : Strategy
{
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Trading start time.
	/// </summary>
	public DateTimeOffset StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Trading end time.
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
	/// Initializes a new instance of the EMA alignment strategy.
	/// </summary>
	public Ema102050AlignmentStrategy()
	{
		_startTime = Param(nameof(StartTime), new DateTimeOffset(2023, 5, 17, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Time", "Trading start time", "General");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(2025, 5, 17, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Time", "Trading end time", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

	private void ProcessCandle(ICandleMessage candle, decimal ema10Value, decimal ema20Value, decimal ema50Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.OpenTime < StartTime || candle.OpenTime > EndTime)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bullish = ema10Value > ema20Value && ema20Value > ema50Value;
		var bearish = ema10Value < ema20Value && ema20Value < ema50Value;

		if (bullish && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (bearish && Position > 0)
		{
			SellMarket(Position);
		}
	}
}
