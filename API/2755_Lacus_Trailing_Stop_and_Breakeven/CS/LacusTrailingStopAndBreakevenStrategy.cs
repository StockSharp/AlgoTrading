using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Risk manager that mirrors the Lacus trailing stop and breakeven expert advisor.
/// The strategy does not generate entries on its own and instead manages existing positions
/// by maintaining stop-loss, take-profit, breakeven and trailing stop logic while also
/// monitoring global profit targets.
/// </summary>
public class LacusTrailingStopAndBreakevenStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _percentOfBalance;
	private readonly StrategyParam<decimal> _profitAmount;
	private readonly StrategyParam<decimal> _positionProfitTarget;
	private readonly StrategyParam<decimal> _trailingStartPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _breakevenGainPips;
	private readonly StrategyParam<decimal> _breakevenLockPips;
	private readonly StrategyParam<bool> _useStealthStops;
	private readonly StrategyParam<DataType> _candleType;

	private Order _stopOrder;
	private Order _takeOrder;
	private decimal? _initialCapital;
	private decimal _highestPriceSinceEntry;
	private decimal _lowestPriceSinceEntry;
	private bool _breakevenApplied;
	private decimal? _stopLevel;
	private decimal? _takeLevel;

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Percent of balance that triggers closing all positions.
	/// </summary>
	public decimal PercentOfBalance
	{
		get => _percentOfBalance.Value;
		set => _percentOfBalance.Value = value;
	}

	/// <summary>
	/// Profit amount in account currency that triggers closing all positions.
	/// </summary>
	public decimal ProfitAmount
	{
		get => _profitAmount.Value;
		set => _profitAmount.Value = value;
	}

	/// <summary>
	/// Profit target for the active position in account currency.
	/// </summary>
	public decimal PositionProfitTarget
	{
		get => _positionProfitTarget.Value;
		set => _positionProfitTarget.Value = value;
	}

	/// <summary>
	/// Distance in pips before trailing stop becomes active.
	/// </summary>
	public decimal TrailingStartPips
	{
		get => _trailingStartPips.Value;
		set => _trailingStartPips.Value = value;
	}

	/// <summary>
	/// Distance in pips maintained by the trailing stop once activated.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Profit in pips that activates the breakeven move.
	/// </summary>
	public decimal BreakevenGainPips
	{
		get => _breakevenGainPips.Value;
		set => _breakevenGainPips.Value = value;
	}

	/// <summary>
	/// Pips locked when moving stop-loss to breakeven.
	/// </summary>
	public decimal BreakevenLockPips
	{
		get => _breakevenLockPips.Value;
		set => _breakevenLockPips.Value = value;
	}

	/// <summary>
	/// When enabled, stop and take-profit are simulated without placing real orders.
	/// </summary>
	public bool UseStealthStops
	{
		get => _useStealthStops.Value;
		set => _useStealthStops.Value = value;
	}

	/// <summary>
	/// Candle type used to drive trailing logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="LacusTrailingStopAndBreakevenStrategy"/>.
	/// </summary>
	public LacusTrailingStopAndBreakevenStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 40m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Protection")
			.SetCanOptimize();

		_takeProfitPips = Param(nameof(TakeProfitPips), 200m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take-profit distance in pips", "Protection")
			.SetCanOptimize();

		_percentOfBalance = Param(nameof(PercentOfBalance), 1m)
			.SetNotNegative()
			.SetDisplay("Profit %", "Close all positions after reaching percent of balance", "Targets")
			.SetCanOptimize();

		_profitAmount = Param(nameof(ProfitAmount), 12m)
			.SetNotNegative()
			.SetDisplay("Profit Amount", "Close all positions after reaching currency profit", "Targets")
			.SetCanOptimize();

		_positionProfitTarget = Param(nameof(PositionProfitTarget), 4m)
			.SetNotNegative()
			.SetDisplay("Position Profit", "Close active position after reaching currency profit", "Targets")
			.SetCanOptimize();

		_trailingStartPips = Param(nameof(TrailingStartPips), 30m)
			.SetNotNegative()
			.SetDisplay("Trailing Start", "Activate trailing after this gain in pips", "Trailing")
			.SetCanOptimize();

		_trailingStopPips = Param(nameof(TrailingStopPips), 20m)
			.SetNotNegative()
			.SetDisplay("Trailing Distance", "Distance maintained by trailing stop", "Trailing")
			.SetCanOptimize();

		_breakevenGainPips = Param(nameof(BreakevenGainPips), 25m)
			.SetNotNegative()
			.SetDisplay("Breakeven Trigger", "Move stop to breakeven after this gain", "Breakeven")
			.SetCanOptimize();

		_breakevenLockPips = Param(nameof(BreakevenLockPips), 10m)
			.SetNotNegative()
			.SetDisplay("Breakeven Lock", "Pips locked in profit when moving stop", "Breakeven")
			.SetCanOptimize();

		_useStealthStops = Param(nameof(UseStealthStops), false)
			.SetDisplay("Stealth Mode", "Simulate stops without placing orders", "Protection");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for management", "General");
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

		_stopOrder = null;
		_takeOrder = null;
		_initialCapital = null;
		_highestPriceSinceEntry = 0m;
		_lowestPriceSinceEntry = 0m;
		_breakevenApplied = false;
		_stopLevel = null;
		_takeLevel = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_initialCapital = Portfolio?.CurrentValue;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			CancelProtectionOrders();
			_stopLevel = null;
			_takeLevel = null;
			_highestPriceSinceEntry = 0m;
			_lowestPriceSinceEntry = 0m;
			_breakevenApplied = false;
			return;
		}

		var step = Security?.PriceStep;
		if (step == null || step.Value <= 0m)
			return;

		var entry = PositionPrice;
		var stopOffset = StopLossPips * step.Value;
		var takeOffset = TakeProfitPips * step.Value;

		if (Position > 0m)
		{
			_stopLevel = stopOffset > 0m ? entry - stopOffset : null;
			_takeLevel = takeOffset > 0m ? entry + takeOffset : null;
			_highestPriceSinceEntry = entry;
			_lowestPriceSinceEntry = entry;
		}
		else
		{
			_stopLevel = stopOffset > 0m ? entry + stopOffset : null;
			_takeLevel = takeOffset > 0m ? entry - takeOffset : null;
			_highestPriceSinceEntry = entry;
			_lowestPriceSinceEntry = entry;
		}

		_breakevenApplied = false;

		if (!UseStealthStops)
			RegisterProtectionOrders();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_initialCapital == null)
			_initialCapital = Portfolio?.CurrentValue;

		UpdateTrailingLevels(candle);
		ApplyStealthStops(candle);
		CheckPositionProfitTarget(candle.ClosePrice);
		CheckGlobalTargets();
	}

	private void UpdateTrailingLevels(ICandleMessage candle)
	{
		if (Position == 0m)
			return;

		var step = Security?.PriceStep;
		if (step == null || step.Value <= 0m)
			return;

		var entry = PositionPrice;
		var trailingStart = TrailingStartPips * step.Value;
		var trailingDistance = TrailingStopPips * step.Value;
		var breakevenGain = BreakevenGainPips * step.Value;
		var breakevenLock = BreakevenLockPips * step.Value;

		if (Position > 0m)
		{
			// Track the highest price reached since entry for trailing stop logic.
			if (candle.HighPrice > _highestPriceSinceEntry)
				_highestPriceSinceEntry = candle.HighPrice;

			if (!_breakevenApplied && breakevenGain > 0m && breakevenLock < breakevenGain)
			{
				var gain = candle.ClosePrice - entry;
				if (gain >= breakevenGain)
				{
					var newStop = entry + breakevenLock;
					if (_stopLevel == null || newStop > _stopLevel)
						SetStopLevel(newStop);
					_breakevenApplied = true;
				}
			}

			if (trailingStart > 0m && trailingDistance > 0m && _highestPriceSinceEntry - entry >= trailingStart)
			{
				var desiredStop = candle.ClosePrice - trailingDistance;
				if (_stopLevel == null || desiredStop > _stopLevel)
					SetStopLevel(desiredStop);
			}
		}
		else
		{
			// Track the lowest price reached since entry for trailing stop logic.
			if (candle.LowPrice < _lowestPriceSinceEntry)
				_lowestPriceSinceEntry = candle.LowPrice;

			if (!_breakevenApplied && breakevenGain > 0m && breakevenLock < breakevenGain)
			{
				var gain = entry - candle.ClosePrice;
				if (gain >= breakevenGain)
				{
					var newStop = entry - breakevenLock;
					if (_stopLevel == null || newStop < _stopLevel)
						SetStopLevel(newStop);
					_breakevenApplied = true;
				}
			}

			if (trailingStart > 0m && trailingDistance > 0m && entry - _lowestPriceSinceEntry >= trailingStart)
			{
				var desiredStop = candle.ClosePrice + trailingDistance;
				if (_stopLevel == null || desiredStop < _stopLevel)
					SetStopLevel(desiredStop);
			}
		}
	}

	private void ApplyStealthStops(ICandleMessage candle)
	{
		if (!UseStealthStops || Position == 0m)
			return;

		if (Position > 0m)
		{
			// Close long positions once the hidden stop-loss or take-profit is touched.
			if (_stopLevel.HasValue && candle.LowPrice <= _stopLevel.Value)
			{
				ClosePosition();
				return;
			}

			if (_takeLevel.HasValue && candle.HighPrice >= _takeLevel.Value)
				ClosePosition();
		}
		else
		{
			// Close short positions once the hidden stop-loss or take-profit is touched.
			if (_stopLevel.HasValue && candle.HighPrice >= _stopLevel.Value)
			{
				ClosePosition();
				return;
			}

			if (_takeLevel.HasValue && candle.LowPrice <= _takeLevel.Value)
				ClosePosition();
		}
	}

	private void CheckPositionProfitTarget(decimal currentPrice)
	{
		if (Position == 0m || PositionProfitTarget <= 0m)
			return;

		var entry = PositionPrice;
		var profit = (currentPrice - entry) * Position;

		if (profit >= PositionProfitTarget)
			ClosePosition();
	}

	private void CheckGlobalTargets()
	{
		var totalProfit = PnL;

		if (ProfitAmount > 0m && totalProfit >= ProfitAmount)
		{
			ClosePosition();
			return;
		}

		if (_initialCapital.HasValue && _initialCapital.Value > 0m && PercentOfBalance > 0m)
		{
			var equity = Portfolio?.CurrentValue ?? 0m;
			var profit = equity - _initialCapital.Value;
			var target = _initialCapital.Value * PercentOfBalance / 100m;

			if (profit >= target)
				ClosePosition();
		}
	}

	private void SetStopLevel(decimal newLevel)
	{
		_stopLevel = newLevel;

		if (UseStealthStops)
			return;

		RegisterProtectionOrders();
	}

	private void RegisterProtectionOrders()
	{
		CancelProtectionOrders();

		if (Position == 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume == 0m)
			return;

		if (_stopLevel.HasValue)
		{
			_stopOrder = Position > 0m
				? SellStop(volume, _stopLevel.Value)
				: BuyStop(volume, _stopLevel.Value);
		}

		if (_takeLevel.HasValue)
		{
			_takeOrder = Position > 0m
				? SellLimit(volume, _takeLevel.Value)
				: BuyLimit(volume, _takeLevel.Value);
		}
	}

	private void CancelProtectionOrders()
	{
		// Cancel outstanding protective orders before registering new ones.
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		if (_takeOrder != null && _takeOrder.State == OrderStates.Active)
			CancelOrder(_takeOrder);

		_stopOrder = null;
		_takeOrder = null;
	}
}
