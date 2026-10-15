using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA trend + candle direction strategy (converted from grid).
/// </summary>
public class VrSetkaGridStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private decimal _prevClose;
	private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VrSetkaGridStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period for trend", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Base candle series", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma = 0;
		_prevClose = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		SubscribeCandles(CandleType)
			.Bind(ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevEma = emaValue;
			_prevClose = close;
			_hasPrev = true;
			return;
		}

		var crossUp = _prevClose <= _prevEma && close > emaValue;
		var crossDown = _prevClose >= _prevEma && close < emaValue;

		if (crossUp && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (crossDown && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevEma = emaValue;
		_prevClose = close;
	}
}
