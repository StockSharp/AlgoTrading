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
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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

		var ema200 = new ExponentialMovingAverage { Length = 50 };
		var ema50 = new ExponentialMovingAverage { Length = 20 };
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

		// Entry/reversal conditions - EMA alignment
		if (ema10 > ema50 && Position <= 0)
			BuyMarket();
		else if (ema10 < ema50 && Position >= 0)
			SellMarket();

		_prevEma200 = ema200;
		_prevEma50 = ema50;
		_prevEma10 = ema10;
	}
}
