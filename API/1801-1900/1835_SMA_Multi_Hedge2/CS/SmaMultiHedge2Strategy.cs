using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMA trend direction strategy.
/// </summary>
public class SmaMultiHedge2Strategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma1;
	private decimal _prevEma2;
	private int _count;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SmaMultiHedge2Strategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend period", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "Parameters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma1 = 0;
		_prevEma2 = 0;
		_count = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		SubscribeCandles(CandleType)
			.Bind(ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished) return;

		_count++;
		if (_count < 3)
		{
			_prevEma2 = _prevEma1;
			_prevEma1 = emaVal;
			return;
		}

		var trend = 0;
		if (_prevEma2 < _prevEma1 && _prevEma1 < emaVal)
			trend = 1;
		else if (_prevEma2 > _prevEma1 && _prevEma1 > emaVal)
			trend = -1;

		_prevEma2 = _prevEma1;
		_prevEma1 = emaVal;

		if (trend == 1 && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (trend == -1 && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}
