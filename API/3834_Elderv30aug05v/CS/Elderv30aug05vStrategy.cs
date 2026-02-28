using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Elder strategy - EMA trend + Momentum confirmation.
/// Buys when EMA is rising and momentum crosses above zero.
/// Sells when EMA is falling and momentum crosses below zero.
/// </summary>
public class Elderv30aug05vStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private decimal _prevMomentum;
	private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Elderv30aug05vStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 13)
			.SetDisplay("EMA Period", "EMA period for trend", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 10)
			.SetDisplay("Momentum Period", "Momentum period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var momentum = new Momentum { Length = MomentumPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, momentum, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal momentum)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevEma = ema;
			_prevMomentum = momentum;
			_hasPrev = true;
			return;
		}

		var emaRising = ema > _prevEma;
		var emaFalling = ema < _prevEma;
		var momCrossUp = _prevMomentum <= 0 && momentum > 0;
		var momCrossDown = _prevMomentum >= 0 && momentum < 0;

		// Buy: EMA rising + momentum crosses above zero
		if (emaRising && momCrossUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Sell: EMA falling + momentum crosses below zero
		else if (emaFalling && momCrossDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevEma = ema;
		_prevMomentum = momentum;
	}
}
