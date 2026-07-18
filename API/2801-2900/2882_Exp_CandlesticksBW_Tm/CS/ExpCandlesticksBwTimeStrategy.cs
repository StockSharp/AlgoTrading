using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bill Williams Candlesticks BW strategy (simplified).
/// Uses candle body direction with SMA trend filter.
/// </summary>
public class ExpCandlesticksBwTimeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaLength;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	public ExpCandlesticksBwTimeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_smaLength = Param(nameof(SmaLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Bill Williams median line proxy", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = SmaLength };

		int bullCount = 0, bearCount = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, (ICandleMessage candle, decimal smaValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var isBullish = candle.ClosePrice > candle.OpenPrice;
				var isBearish = candle.ClosePrice < candle.OpenPrice;

				if (isBullish)
				{
					bullCount++;
					bearCount = 0;
				}
				else if (isBearish)
				{
					bearCount++;
					bullCount = 0;
				}

				// 3 consecutive bullish candles above SMA
				if (bullCount >= 3 && candle.ClosePrice > smaValue && Position <= 0)
				{
					BuyMarket();
					bullCount = 0;
				}
				// 3 consecutive bearish candles below SMA
				else if (bearCount >= 3 && candle.ClosePrice < smaValue && Position >= 0)
				{
					SellMarket();
					bearCount = 0;
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
