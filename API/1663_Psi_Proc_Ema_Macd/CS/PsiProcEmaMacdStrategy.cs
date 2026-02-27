using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on multiple EMA alignment with MACD confirmation.
/// Buys when EMA10 > EMA50 > EMA200 and MACD positive.
/// Sells on opposite alignment.
/// </summary>
public class PsiProcEmaMacdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma200;
	private decimal _prevEma50;
	private decimal _prevEma10;
	private bool _initialized;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PsiProcEmaMacdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma200 = 0;
		_prevEma50 = 0;
		_prevEma10 = 0;
		_initialized = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema200 = new ExponentialMovingAverage { Length = 200 };
		var ema50 = new ExponentialMovingAverage { Length = 50 };
		var ema10 = new ExponentialMovingAverage { Length = 10 };
		var macd = new MovingAverageConvergenceDivergence();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema200, ema50, ema10, macd, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema200, decimal ema50, decimal ema10, decimal macdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_initialized)
		{
			_prevEma200 = ema200;
			_prevEma50 = ema50;
			_prevEma10 = ema10;
			_initialized = true;
			return;
		}

		// Exit conditions
		if (Position > 0 && candle.ClosePrice < ema50)
		{
			SellMarket();
		}
		else if (Position < 0 && candle.ClosePrice > ema50)
		{
			BuyMarket();
		}

		// Entry conditions
		var longCond = ema200 > _prevEma200 && ema50 > ema200 && ema10 > ema50 && macdVal > 0;
		var shortCond = ema200 < _prevEma200 && ema50 < ema200 && ema10 < ema50 && macdVal < 0;

		if (longCond && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (shortCond && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevEma200 = ema200;
		_prevEma50 = ema50;
		_prevEma10 = ema10;
	}
}
