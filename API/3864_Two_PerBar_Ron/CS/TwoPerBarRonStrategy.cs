using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Two Per Bar Ron strategy - momentum-based direction with EMA confirmation.
/// Buys when momentum crosses above zero and close is above EMA.
/// Sells when momentum crosses below zero and close is below EMA.
/// </summary>
public class TwoPerBarRonStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMom;
	private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TwoPerBarRonStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetDisplay("EMA Period", "EMA trend filter", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 10)
			.SetDisplay("Momentum Period", "Momentum lookback", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var mom = new Momentum { Length = MomentumPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, mom, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal mom)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevMom = mom;
			_hasPrev = true;
			return;
		}

		if (_prevMom <= 0 && mom > 0 && close > ema && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (_prevMom >= 0 && mom < 0 && close < ema && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevMom = mom;
	}
}
