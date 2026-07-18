namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Virtual Pending Order Scalp strategy: ATR-based breakout scalper.
/// Buys when price breaks above recent high + ATR offset. Sells on break below low - ATR.
/// </summary>
public class VirtPoTestBedScalpStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public VirtPoTestBedScalpStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for breakout offset", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };

		decimal? prevHigh = null;
		decimal? prevLow = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, (candle, atrVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var close = candle.ClosePrice;

				if (prevHigh.HasValue && prevLow.HasValue)
				{
					if (close > prevHigh.Value + atrVal * 0.5m && Position <= 0)
						BuyMarket();
					else if (close < prevLow.Value - atrVal * 0.5m && Position >= 0)
						SellMarket();
				}

				prevHigh = candle.HighPrice;
				prevLow = candle.LowPrice;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
}
