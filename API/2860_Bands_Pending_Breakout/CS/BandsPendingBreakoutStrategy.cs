namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Bands Pending Breakout strategy (simplified).
/// Buys when price closes above the upper Bollinger Band.
/// Sells when price closes below the lower Bollinger Band.
/// </summary>
public class BandsPendingBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BbPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	public BandsPendingBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Source candles", "General");

		_bbPeriod = Param(nameof(BbPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands { Length = BbPeriod, Width = 2m };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bb, (ICandleMessage candle, IIndicatorValue bbValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var bbTyped = (BollingerBandsValue)bbValue;
				var upper = bbTyped.UpBand;
				var lower = bbTyped.LowBand;

				// Buy on breakout above upper band
				if (candle.ClosePrice > upper && Position <= 0)
				{
					BuyMarket();
				}
				// Sell on breakdown below lower band
				else if (candle.ClosePrice < lower && Position >= 0)
				{
					SellMarket();
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}
}
