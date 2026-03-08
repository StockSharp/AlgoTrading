using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Price Channel breakout system with delayed confirmation.
/// Opens a long position when price breaks above the channel and then returns inside.
/// Opens a short position on a breakout below the channel followed by a return inside.
/// </summary>
public class PChannelSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevAbove;
	private bool _prevBelow;

	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public PChannelSystemStrategy()
	{
		_period = Param(nameof(Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Channel calculation period", "Indicator")
			.SetOptimize(10, 40, 5);

		_shift = Param(nameof(Shift), 2)
			.SetNotNegative()
			.SetDisplay("Shift", "Bars shift for channel", "Indicator")
			.SetOptimize(0, 5, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "General");
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
		_prevAbove = false;
		_prevBelow = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAbove = false;
		_prevBelow = false;

		var highest = new Highest { Length = Period };
		var lowest = new Lowest { Length = Period };

		var upperQueue = new Queue<decimal>();
		var lowerQueue = new Queue<decimal>();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, (candle, highVal, lowVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				upperQueue.Enqueue(highVal);
				lowerQueue.Enqueue(lowVal);

				if (upperQueue.Count <= Shift || lowerQueue.Count <= Shift)
					return;

				var upper = upperQueue.Dequeue();
				var lower = lowerQueue.Dequeue();

				var isAbove = candle.ClosePrice > upper;
				var isBelow = candle.ClosePrice < lower;

				// Was above, now returned inside -> buy signal
				if (_prevAbove && !isAbove && Position <= 0)
					BuyMarket();
				// Was below, now returned inside -> sell signal
				else if (_prevBelow && !isBelow && Position >= 0)
					SellMarket();

				_prevAbove = isAbove;
				_prevBelow = isBelow;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}
}
