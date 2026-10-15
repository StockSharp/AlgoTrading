using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Break-even management strategy that enters on EMA crossover and moves
/// the exit level to break-even once price moves a configurable distance in favor.
/// </summary>
public class BreakevenV3Strategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _activationPoints;
	private readonly StrategyParam<int> _deltaPoints;

	private ExponentialMovingAverage _fast;
	private ExponentialMovingAverage _slow;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;
	private decimal _breakEvenPrice;
	private bool _breakEvenActivated;
	private int _cooldown;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int ActivationPoints { get => _activationPoints.Value; set => _activationPoints.Value = value; }
	public int DeltaPoints { get => _deltaPoints.Value; set => _deltaPoints.Value = value; }

	public BreakevenV3Strategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 14).SetGreaterThanZero().SetDisplay("Fast Period", "Fast EMA period", "Indicator");
		_slowPeriod = Param(nameof(SlowPeriod), 50).SetGreaterThanZero().SetDisplay("Slow Period", "Slow EMA period", "Indicator");
		_activationPoints = Param(nameof(ActivationPoints), 200).SetNotNegative().SetDisplay("Activation", "Distance price must move before break-even activates", "Risk");
		_deltaPoints = Param(nameof(DeltaPoints), 100).SetNotNegative().SetDisplay("Delta", "Offset from entry for break-even stop", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_fast = null; _slow = null;
		_prevFast = 0; _prevSlow = 0; _entryPrice = 0; _breakEvenPrice = 0;
		_breakEvenActivated = false; _cooldown = 0;
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

		// Manage break-even for open position
		if (Position != 0 && _entryPrice > 0)
		{
			var activationDistance = ActivationPoints * step;
			var deltaOffset = DeltaPoints * step;

			if (Position > 0)
			{
				if (!_breakEvenActivated && activationDistance > 0 && close >= _entryPrice + activationDistance)
				{
					_breakEvenActivated = true;
					_breakEvenPrice = _entryPrice + deltaOffset;
				}
				if (_breakEvenActivated && close <= _breakEvenPrice)
				{
					SellMarket();
					_entryPrice = 0; _breakEvenPrice = 0; _breakEvenActivated = false;
					_cooldown = 100; _prevFast = fastValue; _prevSlow = slowValue;
					return;
				}
			}
			else if (Position < 0)
			{
				if (!_breakEvenActivated && activationDistance > 0 && close <= _entryPrice - activationDistance)
				{
					_breakEvenActivated = true;
					_breakEvenPrice = _entryPrice - deltaOffset;
				}
				if (_breakEvenActivated && close >= _breakEvenPrice)
				{
					BuyMarket();
					_entryPrice = 0; _breakEvenPrice = 0; _breakEvenActivated = false;
					_cooldown = 100; _prevFast = fastValue; _prevSlow = slowValue;
					return;
				}
			}
		}

		// Entry: EMA crossover
		if (_prevFast <= _prevSlow && fastValue > slowValue && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_entryPrice = close; _breakEvenActivated = false; _cooldown = 100;
		}
		else if (_prevFast >= _prevSlow && fastValue < slowValue && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_entryPrice = close; _breakEvenActivated = false; _cooldown = 100;
		}

		_prevFast = fastValue; _prevSlow = slowValue;
	}
}
