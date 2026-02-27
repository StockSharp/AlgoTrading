using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Compass Line strategy: BB + RSI trend filter.
/// Buys when close < lower BB and RSI < 45.
/// Sells when close > upper BB and RSI > 55.
/// </summary>
public class CompassLineStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<int> _rsiPeriod;

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

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public CompassLineStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands { Length = BbPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bb, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbVal, IIndicatorValue rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bbVal.IsFinal || !rsiVal.IsFinal)
			return;

		if (bbVal.IsEmpty || rsiVal.IsEmpty)
			return;

		var bb = (BollingerBandsValue)bbVal;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		var rsi = rsiVal.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (close <= lower && rsi < 45m && Position <= 0)
			BuyMarket();
		else if (close >= upper && rsi > 55m && Position >= 0)
			SellMarket();
	}
}
