using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy using Parabolic SAR with EMA confirmation.
/// </summary>
public class TrendCaptureStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevEma;
	private decimal _prevSar;
	private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendCaptureStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period for trend", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_prevEma = 0;
		_prevSar = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sar = new ParabolicSar();
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		SubscribeCandles(CandleType).Bind(sar, ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevClose = close;
			_prevEma = emaValue;
			_prevSar = sarValue;
			_hasPrev = true;
			return;
		}

		var aboveSar = close > sarValue;
		var belowSar = close < sarValue;
		var prevAboveSar = _prevClose > _prevSar;
		var prevBelowSar = _prevClose < _prevSar;

		if (!prevAboveSar && aboveSar && close > emaValue && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (!prevBelowSar && belowSar && close < emaValue && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevClose = close;
		_prevEma = emaValue;
		_prevSar = sarValue;
	}
}
