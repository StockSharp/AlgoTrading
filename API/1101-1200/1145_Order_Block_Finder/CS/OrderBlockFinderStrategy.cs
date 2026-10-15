using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Order block finder strategy.
/// Buys on detected bullish order blocks and sells on bearish ones.
/// </summary>
public class OrderBlockFinderStrategy : Strategy
{
	private readonly StrategyParam<int> _periods;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	public int Periods { get => _periods.Value; set => _periods.Value = value; }
	public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OrderBlockFinderStrategy()
	{
		_periods = Param(nameof(Periods), 5).SetGreaterThanZero();
		_threshold = Param(nameof(Threshold), 0.5m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 20 };
		var buffer = new Queue<ICandleMessage>();
		var lastSignal = DateTimeOffset.MinValue;
		var cooldown = TimeSpan.FromMinutes(360);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, (candle, smaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!sma.IsFormed)
					return;

				buffer.Enqueue(candle);

				var need = Periods + 1;
				while (buffer.Count > need)
					buffer.Dequeue();

				if (buffer.Count < need)
					return;

				if (candle.OpenTime - lastSignal < cooldown)
					return;

				var arr = buffer.ToArray();
				var ob = arr[0];
				var last = arr[^1];

				var move = ob.ClosePrice != 0 ? Math.Abs((last.ClosePrice - ob.ClosePrice) / ob.ClosePrice) * 100m : 0m;
				if (move < Threshold)
					return;

				var up = 0;
				var down = 0;
				for (var i = 1; i < arr.Length; i++)
				{
					if (arr[i].ClosePrice > arr[i].OpenPrice) up++;
					if (arr[i].ClosePrice < arr[i].OpenPrice) down++;
				}

				if (ob.ClosePrice < ob.OpenPrice && up == Periods && Position <= 0)
				{
					BuyMarket();
					lastSignal = candle.OpenTime;
				}
				else if (ob.ClosePrice > ob.OpenPrice && down == Periods && Position >= 0)
				{
					SellMarket();
					lastSignal = candle.OpenTime;
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}
}
