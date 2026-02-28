using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Arttrader strategy - EMA slope trend follower.
/// Buys when EMA is rising and price crosses above it.
/// Sells when EMA is falling and price crosses below it.
/// </summary>
public class ArttraderStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private decimal _prevPrevEma;
	private decimal _prevClose;
	private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ArttraderStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 14)
			.SetDisplay("EMA Period", "EMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevPrevEma = emaValue;
			_prevEma = emaValue;
			_prevClose = close;
			_hasPrev = true;
			return;
		}

		var emaRising = emaValue > _prevEma;
		var emaFalling = emaValue < _prevEma;

		// Price crosses above EMA when it is rising - buy
		if (emaRising && _prevClose <= _prevEma && close > emaValue && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Price crosses below EMA when it is falling - sell
		else if (emaFalling && _prevClose >= _prevEma && close < emaValue && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevPrevEma = _prevEma;
		_prevEma = emaValue;
		_prevClose = close;
	}
}
