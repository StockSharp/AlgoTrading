namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Candlestick reversal strategy: detects hammer/shooting star patterns.
/// Buys on bullish hammer candle, sells on bearish shooting star.
/// </summary>
public class ExpertCandlesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _shadowRatio;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal ShadowRatio
	{
		get => _shadowRatio.Value;
		set => _shadowRatio.Value = value;
	}

	public ExpertCandlesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_shadowRatio = Param(nameof(ShadowRatio), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Shadow Ratio", "Min shadow to body ratio for pattern", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, (candle, smaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var open = candle.OpenPrice;
				var high = candle.HighPrice;
				var low = candle.LowPrice;
				var close = candle.ClosePrice;
				var range = high - low;

				if (range <= 0)
					return;

				var body = Math.Abs(close - open);
				var upperShadow = high - Math.Max(open, close);
				var lowerShadow = Math.Min(open, close) - low;

				var isHammer = lowerShadow > range * ShadowRatio && upperShadow < body;
				var isShootingStar = upperShadow > range * ShadowRatio && lowerShadow < body;

				if (isHammer && close > smaVal && Position <= 0)
					BuyMarket();
				else if (isShootingStar && close < smaVal && Position >= 0)
					SellMarket();
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
