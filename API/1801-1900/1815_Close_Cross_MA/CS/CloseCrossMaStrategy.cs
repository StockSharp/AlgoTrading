using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Price cross MA strategy.
/// </summary>
public class CloseCrossMaStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevDiff;
	private bool _hasPrev;

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CloseCrossMaStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "EMA period", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevDiff = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = MaPeriod };

		SubscribeCandles(CandleType)
			.Bind(ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished) return;

		var diff = candle.ClosePrice - emaVal;

		if (!_hasPrev)
		{
			_prevDiff = diff;
			_hasPrev = true;
			return;
		}

		// Price crosses above EMA
		if (_prevDiff <= 0 && diff > 0 && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Price crosses below EMA
		else if (_prevDiff >= 0 && diff < 0 && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevDiff = diff;
	}
}
