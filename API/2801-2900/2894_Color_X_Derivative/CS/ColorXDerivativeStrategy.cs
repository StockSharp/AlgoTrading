using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Color X Derivative strategy (simplified). Uses Momentum to detect
/// acceleration/deceleration and generate reversal signals.
/// </summary>
public class ColorXDerivativeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _emaLength;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public ColorXDerivativeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_momentumLength = Param(nameof(MomentumLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Derivative lookback", "Indicators");

		_emaLength = Param(nameof(EmaLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Smoothing period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var momentum = new Momentum { Length = MomentumLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		decimal prevMom = 0;
		var hasPrev = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(momentum, ema, (ICandleMessage candle, decimal momValue, decimal emaValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!hasPrev)
				{
					prevMom = momValue;
					hasPrev = true;
					return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					prevMom = momValue;
					return;
				}

				var close = candle.ClosePrice;

				// Momentum turning positive with price above EMA
				if (prevMom <= 0 && momValue > 0 && close > emaValue && Position <= 0)
					BuyMarket();
				// Momentum turning negative with price below EMA
				else if (prevMom >= 0 && momValue < 0 && close < emaValue && Position >= 0)
					SellMarket();

				prevMom = momValue;
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
