using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Disaster strategy - SMA with momentum breakout.
/// Buys when close is above SMA and momentum crosses above zero.
/// Sells when close is below SMA and momentum crosses below zero.
/// </summary>
public class DisasterStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMom;
	private bool _hasPrev;

	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DisasterStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 50)
			.SetDisplay("SMA Period", "SMA lookback", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetDisplay("Momentum Period", "Momentum lookback", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];
	protected override void OnReseted() { base.OnReseted(); _prevMom = 0m; _hasPrev = false; }

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var mom = new Momentum { Length = MomentumPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, mom, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma, decimal mom)
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

		// Momentum crosses above zero + above SMA = buy
		if (_prevMom <= 0 && mom > 0 && close > sma && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Momentum crosses below zero + below SMA = sell
		else if (_prevMom >= 0 && mom < 0 && close < sma && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevMom = mom;
	}
}
