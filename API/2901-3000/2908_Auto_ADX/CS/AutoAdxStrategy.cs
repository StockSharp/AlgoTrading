using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Auto ADX strategy (simplified). Uses RSI for trend strength detection
/// combined with EMA for direction, mimicking ADX directional movement logic.
/// </summary>
public class AutoAdxStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _rsiHigh;
	private readonly StrategyParam<decimal> _rsiLow;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public decimal RsiHigh
	{
		get => _rsiHigh.Value;
		set => _rsiHigh.Value = value;
	}

	public decimal RsiLow
	{
		get => _rsiLow.Value;
		set => _rsiLow.Value = value;
	}

	public AutoAdxStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period for direction", "Indicators");

		_rsiHigh = Param(nameof(RsiHigh), 60m)
			.SetDisplay("RSI High", "RSI threshold for bullish strength", "Logic");

		_rsiLow = Param(nameof(RsiLow), 40m)
			.SetDisplay("RSI Low", "RSI threshold for bearish weakness", "Logic");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ema, (ICandleMessage candle, decimal rsiVal, decimal emaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var close = candle.ClosePrice;

				// Strong trend up: RSI above threshold and price above EMA
				if (rsiVal > RsiHigh && close > emaVal && Position <= 0)
					BuyMarket();
				// Weak trend / bearish: RSI below threshold and price below EMA
				else if (rsiVal < RsiLow && close < emaVal && Position >= 0)
					SellMarket();
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);

			var rsiArea = CreateChartArea();
			if (rsiArea != null)
				DrawIndicator(rsiArea, rsi);
		}
	}
}
