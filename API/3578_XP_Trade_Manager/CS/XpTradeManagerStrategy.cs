using System;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Protective trade manager converted from the MetaTrader "XP Trade Manager" expert advisor.
/// The strategy does not open new positions. Instead, it manages stops and targets for manual trades.
/// </summary>
public class XpTradeManagerStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<int> _breakEvenActivationPips;
	private readonly StrategyParam<int> _breakEvenLockPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<int> _trailingStartPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _trailingDistancePips;
	private readonly StrategyParam<bool> _trailingEndsAtBreakEven;
	private readonly StrategyParam<bool> _stealthMode;

	private Order? _stopOrder;
	private Order? _takeProfitOrder;
	private Sides? _currentSide;
	private decimal? _currentBid;
	private decimal? _currentAsk;
	private decimal? _manualStopPrice;
	private decimal? _manualTakeProfitPrice;
	private decimal _pipSize;
	private bool _breakEvenApplied;

	/// <summary>
	/// Initializes a new instance of the <see cref="XpTradeManagerStrategy"/> class.
	/// </summary>
	public XpTradeManagerStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 20)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Initial stop-loss distance in pips", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 40)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Initial take-profit distance in pips", "Targets")
			.SetCanOptimize(true);

		_useBreakEven = Param(nameof(UseBreakEven), true)
			.SetDisplay("Use Break-Even", "Move the stop-loss to break-even after configurable profit", "Break-Even");

		_breakEvenActivationPips = Param(nameof(BreakEvenActivationPips), 50)
			.SetNotNegative()
			.SetDisplay("Break-Even Activation (pips)", "Profit required before the break-even stop is applied", "Break-Even")
			.SetCanOptimize(true);

		_breakEvenLockPips = Param(nameof(BreakEvenLockPips), 10)
			.SetNotNegative()
			.SetDisplay("Break-Even Lock (pips)", "Offset kept as profit when moving to break-even", "Break-Even")
			.SetCanOptimize(true);

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing Stop", "Enable the progressive trailing stop controller", "Trailing");

		_trailingStartPips = Param(nameof(TrailingStartPips), 10)
			.SetNotNegative()
			.SetDisplay("Trailing Start (pips)", "Profit required before trailing becomes active", "Trailing")
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 10)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Profit increment that advances the trailing stop", "Trailing")
			.SetCanOptimize(true);

		_trailingDistancePips = Param(nameof(TrailingDistancePips), 15)
			.SetNotNegative()
			.SetDisplay("Trailing Distance (pips)", "Distance maintained behind price when trailing", "Trailing")
			.SetCanOptimize(true);

		_trailingEndsAtBreakEven = Param(nameof(TrailingEndsAtBreakEven), false)
			.SetDisplay("Trailing Ends At Break-Even", "Restrict trailing to never move beyond the break-even lock", "Trailing");

		_stealthMode = Param(nameof(StealthMode), false)
			.SetDisplay("Stealth Mode", "Do not place protective orders and close positions internally", "Execution");
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enable the break-even controller.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit in pips required before the break-even stop activates.
	/// </summary>
	public int BreakEvenActivationPips
	{
		get => _breakEvenActivationPips.Value;
		set => _breakEvenActivationPips.Value = value;
	}

	/// <summary>
	/// Offset in pips kept as locked-in profit when the stop moves to break-even.
	/// </summary>
	public int BreakEvenLockPips
	{
		get => _breakEvenLockPips.Value;
		set => _breakEvenLockPips.Value = value;
	}

	/// <summary>
	/// Enable the trailing stop logic.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Minimum profit in pips required before trailing logic activates.
	/// </summary>
	public int TrailingStartPips
	{
		get => _trailingStartPips.Value;
		set => _trailingStartPips.Value = value;
	}

	/// <summary>
	/// Profit increment in pips that advances the trailing stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Distance in pips maintained between price and the trailing stop.
	/// </summary>
	public int TrailingDistancePips
	{
		get => _trailingDistancePips.Value;
		set => _trailingDistancePips.Value = value;
	}

	/// <summary>
	/// Limit trailing so that the stop never goes beyond the break-even lock.
	/// </summary>
	public bool TrailingEndsAtBreakEven
	{
		get => _trailingEndsAtBreakEven.Value;
		set => _trailingEndsAtBreakEven.Value = value;
	}

	/// <summary>
	/// Operate in stealth mode without placing protective orders on the exchange.
	/// </summary>
	public bool StealthMode
	{
		get => _stealthMode.Value;
		set => _stealthMode.Value = value;
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetProtection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();
		if (_pipSize <= 0m)
			_pipSize = Security?.PriceStep ?? 1m;

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetProtection();
			_currentSide = null;
			return;
		}

		var side = Position > 0m ? Sides.Buy : Sides.Sell;
		if (_currentSide != side)
		{
			ResetProtection();
			_currentSide = side;
		}

		_breakEvenApplied = false;
		EnsureInitialProtection();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_currentBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_currentAsk = (decimal)ask;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0m)
			return;

		EnsureInitialProtection();

		if (_currentSide == Sides.Buy)
			ManageLongPosition();
		else if (_currentSide == Sides.Sell)
			ManageShortPosition();
	}

	private void EnsureInitialProtection()
	{
		if (Position == 0m)
			return;

		if (PositionPrice is not decimal entry || entry <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var stopDistance = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
		var takeDistance = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;

		if (_currentSide == Sides.Buy)
		{
			if (stopDistance > 0m)
			{
				var stopPrice = NormalizePrice(entry - stopDistance);
				SetStopPrice(true, stopPrice, volume, allowWeaker: false);
			}
			else
			{
				ResetStop();
			}

			if (takeDistance > 0m)
			{
				var takePrice = NormalizePrice(entry + takeDistance);
				SetTakeProfitPrice(true, takePrice, volume);
			}
			else
			{
				ResetTakeProfit();
			}
		}
		else if (_currentSide == Sides.Sell)
		{
			if (stopDistance > 0m)
			{
				var stopPrice = NormalizePrice(entry + stopDistance);
				SetStopPrice(false, stopPrice, volume, allowWeaker: false);
			}
			else
			{
				ResetStop();
			}

			if (takeDistance > 0m)
			{
				var takePrice = NormalizePrice(entry - takeDistance);
				SetTakeProfitPrice(false, takePrice, volume);
			}
			else
			{
				ResetTakeProfit();
			}
		}
	}

	private void ManageLongPosition()
	{
		if (_currentBid is not decimal bid || bid <= 0m)
			return;

		if (PositionPrice is not decimal entry || entry <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (UseTrailingStop)
		{
			ApplyTrailingForLong(entry, bid, volume);
		}
		else if (UseBreakEven)
		{
			ApplyBreakEvenForLong(entry, bid, volume);
		}

		if (StealthMode)
			CheckStealthExitForLong(bid);
	}

	private void ManageShortPosition()
	{
		if (_currentAsk is not decimal ask || ask <= 0m)
			return;

		if (PositionPrice is not decimal entry || entry <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (UseTrailingStop)
		{
			ApplyTrailingForShort(entry, ask, volume);
		}
		else if (UseBreakEven)
		{
			ApplyBreakEvenForShort(entry, ask, volume);
		}

		if (StealthMode)
			CheckStealthExitForShort(ask);
	}

	private void ApplyTrailingForLong(decimal entry, decimal bid, decimal volume)
	{
		if (_pipSize <= 0m)
			return;

		if (TrailingStepPips <= 0 || TrailingDistancePips <= 0)
			return;

		var distancePips = (bid - entry) / _pipSize;
		if (distancePips <= TrailingStartPips)
			return;

		var multi = (int)Math.Floor(distancePips / Math.Max(1, TrailingStepPips));
		if (multi <= 0)
			return;

		var trailingStep = TrailingStepPips * _pipSize;
		var trailingDistance = TrailingDistancePips * _pipSize;

		var stopCandidate = entry + multi * trailingStep - trailingDistance;
		if (TrailingEndsAtBreakEven && BreakEvenLockPips > 0)
		{
			var breakEven = entry + BreakEvenLockPips * _pipSize;
			if (stopCandidate > breakEven)
				stopCandidate = breakEven;
		}

		stopCandidate = NormalizePrice(stopCandidate);
		var currentStop = GetCurrentStopPrice();
		if (currentStop is decimal existing && stopCandidate <= existing)
			return;

		SetStopPrice(true, stopCandidate, volume, allowWeaker: false);
	}

	private void ApplyTrailingForShort(decimal entry, decimal ask, decimal volume)
	{
		if (_pipSize <= 0m)
			return;

		if (TrailingStepPips <= 0 || TrailingDistancePips <= 0)
			return;

		var distancePips = (entry - ask) / _pipSize;
		if (distancePips <= TrailingStartPips)
			return;

		var multi = (int)Math.Floor(distancePips / Math.Max(1, TrailingStepPips));
		if (multi <= 0)
			return;

		var trailingStep = TrailingStepPips * _pipSize;
		var trailingDistance = TrailingDistancePips * _pipSize;

		var stopCandidate = entry - multi * trailingStep + trailingDistance;
		if (TrailingEndsAtBreakEven && BreakEvenLockPips > 0)
		{
			var breakEven = entry - BreakEvenLockPips * _pipSize;
			if (stopCandidate < breakEven)
				stopCandidate = breakEven;
		}

		stopCandidate = NormalizePrice(stopCandidate);
		var currentStop = GetCurrentStopPrice();
		if (currentStop is decimal existing && stopCandidate >= existing)
			return;

		SetStopPrice(false, stopCandidate, volume, allowWeaker: false);
	}

	private void ApplyBreakEvenForLong(decimal entry, decimal bid, decimal volume)
	{
		if (_pipSize <= 0m)
			return;

		if (BreakEvenActivationPips <= 0 || BreakEvenLockPips <= 0)
			return;

		var stopTarget = NormalizePrice(entry + BreakEvenLockPips * _pipSize);
		var currentStop = GetCurrentStopPrice();
		if (_breakEvenApplied && currentStop is decimal existing && existing >= stopTarget)
			return;

		var profitPips = (bid - entry) / _pipSize;
		if (profitPips < BreakEvenActivationPips)
			return;

		SetStopPrice(true, stopTarget, volume, allowWeaker: false);
		_breakEvenApplied = true;
	}

	private void ApplyBreakEvenForShort(decimal entry, decimal ask, decimal volume)
	{
		if (_pipSize <= 0m)
			return;

		if (BreakEvenActivationPips <= 0 || BreakEvenLockPips <= 0)
			return;

		var stopTarget = NormalizePrice(entry - BreakEvenLockPips * _pipSize);
		var currentStop = GetCurrentStopPrice();
		if (_breakEvenApplied && currentStop is decimal existing && existing <= stopTarget)
			return;

		var profitPips = (entry - ask) / _pipSize;
		if (profitPips < BreakEvenActivationPips)
			return;

		SetStopPrice(false, stopTarget, volume, allowWeaker: false);
		_breakEvenApplied = true;
	}

	private void CheckStealthExitForLong(decimal bid)
	{
		if (_manualTakeProfitPrice is decimal takePrice && bid >= takePrice)
		{
			ClosePosition();
			ResetProtection();
			return;
		}

		if (_manualStopPrice is decimal stopPrice && bid <= stopPrice)
		{
			ClosePosition();
			ResetProtection();
		}
	}

	private void CheckStealthExitForShort(decimal ask)
	{
		if (_manualTakeProfitPrice is decimal takePrice && ask <= takePrice)
		{
			ClosePosition();
			ResetProtection();
			return;
		}

		if (_manualStopPrice is decimal stopPrice && ask >= stopPrice)
		{
			ClosePosition();
			ResetProtection();
		}
	}

	private void SetStopPrice(bool isLong, decimal stopPrice, decimal volume, bool allowWeaker)
	{
		if (stopPrice <= 0m || volume <= 0m)
			return;

		if (StealthMode)
		{
			if (isLong)
			{
				if (!_manualStopPrice.HasValue || stopPrice > _manualStopPrice.Value || allowWeaker)
					_manualStopPrice = stopPrice;
			}
			else
			{
				if (!_manualStopPrice.HasValue || stopPrice < _manualStopPrice.Value || allowWeaker)
					_manualStopPrice = stopPrice;
			}

			return;
		}

		if (!allowWeaker && _stopOrder?.Price is decimal existing)
		{
			if (isLong && stopPrice <= existing)
				return;

			if (!isLong && stopPrice >= existing)
				return;
		}

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active && _stopOrder.Price == stopPrice && _stopOrder.Volume == volume)
			return;

		if (_stopOrder != null)
		{
			if (_stopOrder.State == OrderStates.Active)
				CancelOrder(_stopOrder);

			_stopOrder = null;
		}

		_stopOrder = isLong ? SellStop(volume, stopPrice) : BuyStop(volume, stopPrice);
	}

	private void SetTakeProfitPrice(bool isLong, decimal takeProfitPrice, decimal volume)
	{
		if (takeProfitPrice <= 0m || volume <= 0m)
			return;

		if (StealthMode)
		{
			_manualTakeProfitPrice = takeProfitPrice;
			return;
		}

		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active && _takeProfitOrder.Price == takeProfitPrice && _takeProfitOrder.Volume == volume)
			return;

		if (_takeProfitOrder != null)
		{
			if (_takeProfitOrder.State == OrderStates.Active)
				CancelOrder(_takeProfitOrder);

			_takeProfitOrder = null;
		}

		_takeProfitOrder = isLong ? SellLimit(volume, takeProfitPrice) : BuyLimit(volume, takeProfitPrice);
	}

private decimal? GetCurrentStopPrice()
	{
		if (StealthMode)
			return _manualStopPrice;

		return _stopOrder?.Price;
	}

	private void ResetStop()
	{
		if (StealthMode)
		{
			_manualStopPrice = null;
			return;
		}

		if (_stopOrder != null)
		{
			if (_stopOrder.State == OrderStates.Active)
				CancelOrder(_stopOrder);

			_stopOrder = null;
		}
	}

	private void ResetTakeProfit()
	{
		if (StealthMode)
		{
			_manualTakeProfitPrice = null;
			return;
		}

		if (_takeProfitOrder != null)
		{
			if (_takeProfitOrder.State == OrderStates.Active)
				CancelOrder(_takeProfitOrder);

			_takeProfitOrder = null;
		}
	}

	private void ResetProtection()
	{
		ResetStop();
		ResetTakeProfit();
		_manualStopPrice = null;
		_manualTakeProfitPrice = null;
		_breakEvenApplied = false;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (price <= 0m)
			return 0m;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return price;

		return Math.Round(price / step, MidpointRounding.AwayFromZero) * step;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = GetDecimalPlaces(step);
		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var text = Math.Abs(value).ToString(CultureInfo.InvariantCulture);
		var index = text.IndexOf('.') >= 0 ? text.IndexOf('.') : text.IndexOf(',');
		return index >= 0 ? text.Length - index - 1 : 0;
	}
}
