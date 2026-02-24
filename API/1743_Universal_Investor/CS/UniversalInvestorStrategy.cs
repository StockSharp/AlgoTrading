using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on EMA and LWMA crossover with trend confirmation.
/// Opens long when LWMA is above EMA and both are rising.
/// Opens short when LWMA is below EMA and both are falling.
/// Closes position on opposite crossover.
/// </summary>
public class UniversalInvestorStrategy : Strategy
{
	private readonly StrategyParam<int> _movingPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevEma;
	private decimal? _prevLwma;

	public int MovingPeriod { get => _movingPeriod.Value; set => _movingPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public UniversalInvestorStrategy()
	{
		_movingPeriod = Param(nameof(MovingPeriod), 23)
			.SetGreaterThanZero()
			.SetDisplay("Moving Period", "Smoothing period for EMA and LWMA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new EMA { Length = MovingPeriod };
		var lwma = new WeightedMovingAverage { Length = MovingPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, lwma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal lwmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevEma is null || _prevLwma is null)
		{
			_prevEma = emaValue;
			_prevLwma = lwmaValue;
			return;
		}

		var openBuy = lwmaValue > emaValue && lwmaValue > _prevLwma && emaValue > _prevEma;
		var openSell = lwmaValue < emaValue && lwmaValue < _prevLwma && emaValue < _prevEma;
		var closeBuy = lwmaValue < emaValue;
		var closeSell = lwmaValue > emaValue;

		if (Position > 0 && closeBuy)
		{
			SellMarket();
		}
		else if (Position < 0 && closeSell)
		{
			BuyMarket();
		}
		else if (Position == 0)
		{
			if (openBuy && !closeBuy)
				BuyMarket();
			else if (openSell && !closeSell)
				SellMarket();
		}

		_prevEma = emaValue;
		_prevLwma = lwmaValue;
	}
}
