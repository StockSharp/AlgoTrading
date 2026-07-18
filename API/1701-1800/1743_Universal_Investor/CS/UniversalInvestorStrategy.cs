using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on EMA and WMA crossover with trend confirmation.
/// </summary>
public class UniversalInvestorStrategy : Strategy
{
	private readonly StrategyParam<int> _movingPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private decimal _prevLwma;
	private bool _hasPrev;

	public int MovingPeriod { get => _movingPeriod.Value; set => _movingPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public UniversalInvestorStrategy()
	{
		_movingPeriod = Param(nameof(MovingPeriod), 23)
			.SetGreaterThanZero()
			.SetDisplay("Moving Period", "Smoothing period for EMA and WMA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma = 0;
		_prevLwma = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = MovingPeriod };
		var lwma = new WeightedMovingAverage { Length = MovingPeriod };

		SubscribeCandles(CandleType)
			.Bind(ema, lwma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal lwmaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevEma = emaValue;
			_prevLwma = lwmaValue;
			_hasPrev = true;
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
