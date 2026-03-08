using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Time-based trader using EMA trend for directional entries.
/// </summary>
public class TimeTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TimeTraderStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		SubscribeCandles(CandleType).Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevEma = emaVal;
			_hasPrev = true;
			return;
		}

		var close = candle.ClosePrice;

		// EMA rising and price above EMA -> buy
		if (emaVal > _prevEma && close > emaVal && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// EMA falling and price below EMA -> sell
		else if (emaVal < _prevEma && close < emaVal && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevEma = emaVal;
	}
}
