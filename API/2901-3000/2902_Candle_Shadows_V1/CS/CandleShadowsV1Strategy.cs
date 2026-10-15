using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Candle Shadows V1 strategy (simplified). Detects candles with long shadows
/// (wicks) relative to body as reversal signals, filtered by EMA trend.
/// </summary>
public class CandleShadowsV1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _shadowRatio;

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

	public decimal ShadowRatio
	{
		get => _shadowRatio.Value;
		set => _shadowRatio.Value = value;
	}

	public CandleShadowsV1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_emaLength = Param(nameof(EmaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Trend EMA period", "Indicators");

		_shadowRatio = Param(nameof(ShadowRatio), 3m)
			.SetDisplay("Shadow Ratio", "Min shadow/body ratio", "Logic");
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
				var open = candle.OpenPrice;
				var high = candle.HighPrice;
				var low = candle.LowPrice;

				var body = Math.Abs(close - open);
				if (body <= 0) return;

				var upperShadow = high - Math.Max(close, open);
				var lowerShadow = Math.Min(close, open) - low;

				// Long lower shadow (hammer) near EMA => buy signal
				if (lowerShadow > body * ShadowRatio && close > emaValue && Position <= 0)
					BuyMarket();
				// Long upper shadow (shooting star) near EMA => sell signal
				else if (upperShadow > body * ShadowRatio && close < emaValue && Position >= 0)
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
