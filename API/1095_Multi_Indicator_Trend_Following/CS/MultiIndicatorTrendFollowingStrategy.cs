using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover with RSI confirmation trend following strategy.
/// </summary>
public class MultiIndicatorTrendFollowingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<DataType> _candleType;

	public int FastMaLength { get => _fastMaLength.Value; set => _fastMaLength.Value = value; }
	public int SlowMaLength { get => _slowMaLength.Value; set => _slowMaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MultiIndicatorTrendFollowingStrategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast EMA period", "Indicators");
		_slowMaLength = Param(nameof(SlowMaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow EMA period", "Indicators");
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastMaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowMaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var prevFast = 0m;
		var prevSlow = 0m;
		var initialized = false;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastEma, slowEma, rsi, (candle, fastVal, slowVal, rsiVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!initialized)
				{
					prevFast = fastVal;
					prevSlow = slowVal;
					initialized = true;
					return;
				}

				var crossUp = prevFast <= prevSlow && fastVal > slowVal;
				var crossDown = prevFast >= prevSlow && fastVal < slowVal;

				if (crossUp && rsiVal > 50 && Position <= 0)
					BuyMarket();
				else if (crossDown && rsiVal < 50 && Position >= 0)
					SellMarket();

				prevFast = fastVal;
				prevSlow = slowVal;
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
