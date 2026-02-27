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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands { Length = BbPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(bb, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bbVal.IsFinal || bbVal.IsEmpty)
			return;

		var bb = (BollingerBandsValue)bbVal;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		var close = candle.ClosePrice;
		var mid = (upper + lower) / 2m;

		if (close < mid && Position <= 0)
			BuyMarket();
		else if (close > mid && Position >= 0)
			SellMarket();
	}
}
