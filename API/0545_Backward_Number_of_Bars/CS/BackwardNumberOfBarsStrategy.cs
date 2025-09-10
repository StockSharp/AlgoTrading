using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Holds a long position only during the most recent N bars.
/// </summary>
public class BackwardNumberOfBarsStrategy : Strategy
{
	private readonly StrategyParam<int> _barCount;
	private readonly StrategyParam<DataType> _candleType;

	private DateTimeOffset _startTime;

	/// <summary>
	/// Number of bars to keep the position.
	/// </summary>
	public int BarCount
	{
		get => _barCount.Value;
		set => _barCount.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BackwardNumberOfBarsStrategy()
	{
		_barCount = Param(nameof(BarCount), 50)
			.SetDisplay("Bar Count", "Number of bars for the window", "General")
			.SetCanOptimize(true);

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
		_startTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var tf = (TimeSpan)CandleType.Arg;
		_startTime = time - TimeSpan.FromTicks(tf.Ticks * BarCount);

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

		var withinWindow = candle.OpenTime >= _startTime;

		if (withinWindow)
		{
			if (Position <= 0)
				RegisterBuy();
		}
		else
		{
			if (Position >= 0)
				RegisterSell();
		}
	}
}
