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
/// MACD crossover strategy with EMA trend filter.
/// </summary>
public class MultiTimeframeMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<DataType> _candleType;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int TrendLength { get => _trendLength.Value; set => _trendLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MultiTimeframeMacdStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast", "Fast EMA", "MACD");
		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow", "Slow EMA", "MACD");
		_trendLength = Param(nameof(TrendLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Trend", "Trend EMA period", "Trend");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };
		var trendEma = new ExponentialMovingAverage { Length = TrendLength };

		var prevFast = 0m;
		var prevSlow = 0m;
		var initialized = false;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastEma, slowEma, trendEma, (candle, fastVal, slowVal, trendVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!fastEma.IsFormed || !slowEma.IsFormed || !trendEma.IsFormed)
					return;

				if (!initialized)
				{
					prevFast = fastVal;
					prevSlow = slowVal;
					initialized = true;
					return;
				}

				var macd = fastVal - slowVal;
				var prevMacd = prevFast - prevSlow;

				// MACD crosses zero up + price above trend => buy
				if (prevMacd <= 0 && macd > 0 && candle.ClosePrice > trendVal && Position <= 0)
					BuyMarket();
				// MACD crosses zero down + price below trend => sell
				else if (prevMacd >= 0 && macd < 0 && candle.ClosePrice < trendVal && Position > 0)
					SellMarket();

				prevFast = fastVal;
				prevSlow = slowVal;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, trendEma);
			DrawOwnTrades(area);
		}
	}
}
