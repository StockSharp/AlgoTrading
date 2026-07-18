using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Area MACD strategy (simplified). Tracks cumulative positive/negative
/// areas of fast-slow EMA difference to determine trend direction.
/// </summary>
public class AreaMacdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _historyLength;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public int HistoryLength
	{
		get => _historyLength.Value;
		set => _historyLength.Value = value;
	}

	public AreaMacdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA period", "Indicators");

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA period", "Indicators");

		_historyLength = Param(nameof(HistoryLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("History Length", "Area accumulation window", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };

		var diffHistory = new Queue<decimal>();
		decimal posArea = 0, negArea = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, (ICandleMessage candle, decimal fastValue, decimal slowValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var diff = fastValue - slowValue;

				diffHistory.Enqueue(diff);
				if (diff > 0) posArea += diff;
				else negArea += Math.Abs(diff);

				if (diffHistory.Count > HistoryLength)
				{
					var old = diffHistory.Dequeue();
					if (old > 0) posArea -= old;
					else negArea -= Math.Abs(old);
				}

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (diffHistory.Count < HistoryLength)
					return;

				// Bullish area dominates
				if (posArea > negArea * 1.25m && Position <= 0)
					BuyMarket();
				// Bearish area dominates
				else if (negArea > posArea * 1.25m && Position >= 0)
					SellMarket();
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}
}
