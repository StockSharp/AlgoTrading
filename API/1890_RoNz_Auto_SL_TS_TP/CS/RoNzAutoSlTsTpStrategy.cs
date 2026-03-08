using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that automatically manages stop-loss and take-profit levels.
/// Opens positions using EMA crossover and protects them with
/// profit lock and trailing stop.
/// </summary>
public class RoNzAutoSlTsTpStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _lockAfter;
	private readonly StrategyParam<decimal> _profitLock;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<int> _cooldownBars;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Profit threshold to start locking gains.
	/// </summary>
	public decimal LockProfitAfter
	{
		get => _lockAfter.Value;
		set => _lockAfter.Value = value;
	}

	/// <summary>
	/// Amount of profit to lock once threshold reached.
	/// </summary>
	public decimal ProfitLock
	{
		get => _profitLock.Value;
		set => _profitLock.Value = value;
	}

	/// <summary>
	/// Trailing stop distance.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Step to move trailing stop.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Minimum number of bars between entries.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _trailAnchor;
	private decimal _prevEma10;
	private decimal _prevEma20;
	private bool _profitLocked;
	private bool _isInitialized;
	private int _barsSinceExit;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public RoNzAutoSlTsTpStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_takeProfit = Param(nameof(TakeProfit), 500m)
		.SetDisplay("Take Profit", "Take profit in points", "Risk");

		_stopLoss = Param(nameof(StopLoss), 250m)
		.SetDisplay("Stop Loss", "Stop loss in points", "Risk");

		_lockAfter = Param(nameof(LockProfitAfter), 100m)
		.SetDisplay("Lock Profit After", "Profit threshold for locking", "Risk");

		_profitLock = Param(nameof(ProfitLock), 60m)
		.SetDisplay("Profit Lock", "Profit to lock after threshold", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 50m)
		.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk");

		_trailingStep = Param(nameof(TrailingStep), 10m)
		.SetDisplay("Trailing Step", "Step for trailing stop", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 6)
		.SetDisplay("Cooldown Bars", "Minimum number of bars between entries", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetProtection();
		_prevEma10 = 0m;
		_prevEma20 = 0m;
		_isInitialized = false;
		_barsSinceExit = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema10 = new ExponentialMovingAverage { Length = 10 };
		var ema20 = new ExponentialMovingAverage { Length = 20 };
		var ema100 = new ExponentialMovingAverage { Length = 100 };

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(ema10, ema20, ema100, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema10, decimal ema20, decimal ema100)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isInitialized)
		{
			_prevEma10 = ema10;
			_prevEma20 = ema20;
			_isInitialized = true;
			return;
		}

		_barsSinceExit++;

		var bullishCross = _prevEma10 <= _prevEma20 && ema10 > ema20 && ema10 > ema100 && ema20 > ema100;
		var bearishCross = _prevEma10 >= _prevEma20 && ema10 < ema20 && ema10 < ema100 && ema20 < ema100;

		if (Position == 0)
		{
			if (_barsSinceExit >= CooldownBars && bullishCross)
			{
				BuyMarket();
				SetInitialProtection(candle.ClosePrice);
			}
			else if (_barsSinceExit >= CooldownBars && bearishCross)
			{
				SellMarket();
				SetInitialProtection(candle.ClosePrice);
			}
		}
		else
		{
			ManageProtection(candle.ClosePrice);
		}

		_prevEma10 = ema10;
		_prevEma20 = ema20;
	}

	private void SetInitialProtection(decimal price)
	{
		_entryPrice = price;
		_profitLocked = false;
		_trailAnchor = price;

		if (Position > 0)
		{
			_stopPrice = StopLoss > 0 ? price - StopLoss : 0m;
			_takePrice = TakeProfit > 0 ? price + TakeProfit : 0m;
		}
		else if (Position < 0)
		{
			_stopPrice = StopLoss > 0 ? price + StopLoss : 0m;
			_takePrice = TakeProfit > 0 ? price - TakeProfit : 0m;
		}
	}

	private void ManageProtection(decimal price)
	{
		if (Position > 0)
		{
			if ((_takePrice > 0 && price >= _takePrice) || (_stopPrice > 0 && price <= _stopPrice))
			{
				SellMarket();
				ResetProtection();
				return;
			}

			var profit = price - _entryPrice;

			if (!_profitLocked && LockProfitAfter > 0 && ProfitLock > 0 && profit >= LockProfitAfter)
			{
				_stopPrice = _entryPrice + ProfitLock;
				_profitLocked = true;
			}

			if (TrailingStop > 0 && (LockProfitAfter == 0 || profit >= LockProfitAfter))
			{
				if (price - _trailAnchor >= TrailingStep)
				{
					var newStop = price - TrailingStop;
					if (newStop > _stopPrice)
					_stopPrice = newStop;

					_trailAnchor = price;
				}
			}
		}
		else if (Position < 0)
		{
			if ((_takePrice > 0 && price <= _takePrice) || (_stopPrice > 0 && price >= _stopPrice))
			{
				BuyMarket();
				ResetProtection();
				return;
			}

			var profit = _entryPrice - price;

			if (!_profitLocked && LockProfitAfter > 0 && ProfitLock > 0 && profit >= LockProfitAfter)
			{
				_stopPrice = _entryPrice - ProfitLock;
				_profitLocked = true;
			}

			if (TrailingStop > 0 && (LockProfitAfter == 0 || profit >= LockProfitAfter))
			{
				if (_trailAnchor - price >= TrailingStep)
				{
					var newStop = price + TrailingStop;
					if (_stopPrice == 0m || newStop < _stopPrice)
					_stopPrice = newStop;

					_trailAnchor = price;
				}
			}
		}
	}

	private void ResetProtection()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_trailAnchor = 0m;
		_profitLocked = false;
		_barsSinceExit = 0;
	}
}
