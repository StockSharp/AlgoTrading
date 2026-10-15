using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class RandomHedgStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private ExponentialMovingAverage _fast;
	private ExponentialMovingAverage _slow;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;
	private int _cooldown;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public int TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	public RandomHedgStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 14).SetGreaterThanZero().SetDisplay("Fast Period", "Fast EMA period", "Indicator");
		_slowPeriod = Param(nameof(SlowPeriod), 50).SetGreaterThanZero().SetDisplay("Slow Period", "Slow EMA period", "Indicator");
		_stopLossPoints = Param(nameof(StopLossPoints), 200).SetNotNegative().SetDisplay("Stop Loss", "Stop-loss in price steps", "Risk");
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 400).SetNotNegative().SetDisplay("Take Profit", "Take-profit in price steps", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_fast = null; _slow = null;
		_prevFast = 0; _prevSlow = 0; _entryPrice = 0; _cooldown = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_fast = new ExponentialMovingAverage { Length = FastPeriod };
		_slow = new ExponentialMovingAverage { Length = SlowPeriod };
		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		subscription.Bind(_fast, _slow, ProcessCandle);
		subscription.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished) return;
		if (!_fast.IsFormed || !_slow.IsFormed) { _prevFast = fastValue; _prevSlow = slowValue; return; }
		if (_cooldown > 0) { _cooldown--; _prevFast = fastValue; _prevSlow = slowValue; return; }

		var close = candle.ClosePrice;
		var step = Security?.PriceStep ?? 1m;

		if (Position > 0 && _entryPrice > 0)
		{
			if (StopLossPoints > 0 && close <= _entryPrice - StopLossPoints * step) { SellMarket(); _entryPrice = 0; _cooldown = 100; _prevFast = fastValue; _prevSlow = slowValue; return; }
			if (TakeProfitPoints > 0 && close >= _entryPrice + TakeProfitPoints * step) { SellMarket(); _entryPrice = 0; _cooldown = 100; _prevFast = fastValue; _prevSlow = slowValue; return; }
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (StopLossPoints > 0 && close >= _entryPrice + StopLossPoints * step) { BuyMarket(); _entryPrice = 0; _cooldown = 100; _prevFast = fastValue; _prevSlow = slowValue; return; }
			if (TakeProfitPoints > 0 && close <= _entryPrice - TakeProfitPoints * step) { BuyMarket(); _entryPrice = 0; _cooldown = 100; _prevFast = fastValue; _prevSlow = slowValue; return; }
		}

		if (_prevFast <= _prevSlow && fastValue > slowValue && Position <= 0)
		{ if (Position < 0) BuyMarket(); BuyMarket(); _entryPrice = close; _cooldown = 100; }
		else if (_prevFast >= _prevSlow && fastValue < slowValue && Position >= 0)
		{ if (Position > 0) SellMarket(); SellMarket(); _entryPrice = close; _cooldown = 100; }

		_prevFast = fastValue; _prevSlow = slowValue;
	}
}
