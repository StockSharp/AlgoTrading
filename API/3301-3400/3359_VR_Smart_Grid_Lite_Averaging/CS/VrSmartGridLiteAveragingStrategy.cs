namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// VR Smart Grid Lite Averaging: grid with averaging approach using Bollinger Bands.
/// Buys near lower band, sells near upper band.
/// </summary>
public class VrSmartGridLiteAveragingStrategy : Strategy
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

	public VrSmartGridLiteAveragingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands { Length = BbPeriod };

		decimal? prevClose = null;
		decimal? prevMid = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bb, (candle, bbVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var bbv = (BollingerBandsValue)bbVal;
				if (bbv.UpBand is not decimal upper || bbv.LowBand is not decimal lower)
					return;

				var close = candle.ClosePrice;
				var mid = (upper + lower) / 2m;

				if (prevClose.HasValue && prevMid.HasValue)
				{
					var crossBelow = prevClose.Value >= prevMid.Value && close < mid;
					var crossAbove = prevClose.Value <= prevMid.Value && close > mid;

					if (crossBelow && Position <= 0)
						BuyMarket();
					else if (crossAbove && Position >= 0)
						SellMarket();
				}

				prevClose = close;
				prevMid = mid;
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
