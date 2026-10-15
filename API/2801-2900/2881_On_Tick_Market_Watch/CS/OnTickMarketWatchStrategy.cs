using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// On Tick Market Watch strategy (simplified).
/// Uses simple price momentum for entries.
/// </summary>
public class OnTickMarketWatchStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public OnTickMarketWatchStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, (ICandleMessage candle, decimal emaValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var close = candle.ClosePrice;

				if (close > emaValue && Position <= 0)
					BuyMarket();
				else if (close < emaValue && Position >= 0)
					SellMarket();
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}
}
