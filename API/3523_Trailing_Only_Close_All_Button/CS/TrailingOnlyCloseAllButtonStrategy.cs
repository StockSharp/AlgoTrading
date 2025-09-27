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
/// Strategy that trails open positions and provides a manual "Close All" switch similar to the MQL script.
/// It does not generate entries and only manages existing positions created by other strategies or manual trades.
/// </summary>
public class TrailingOnlyCloseAllButtonStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<CloseMode> _closeMode;
	private readonly StrategyParam<CloseSymbol> _closeSymbol;
	private readonly StrategyParam<CloseProfitFilter> _closeProfitFilter;
	private readonly StrategyParam<bool> _closeAll;

	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal? _longTake;
	private decimal? _shortTake;
	private decimal? _lastAsk;
	private decimal? _lastBid;
	private decimal _previousPosition;

	/// <summary>
	/// Defines what should be closed when the manual button is triggered.
	/// </summary>
	public enum CloseMode
	{
		Positions,
		All,
		Orders,
	}

	/// <summary>
	/// Defines whether the manual closing routine targets only the current symbol or all tracked instruments.
	/// </summary>
	public enum CloseSymbol
	{
		Chart,
		All,
	}

	/// <summary>
	/// Profit filter applied before closing positions.
	/// </summary>
	public enum CloseProfitFilter
	{
		All,
		ProfitOnly,
		LossOnly,
	}

	/// <summary>
	/// Stop loss distance expressed in MetaTrader pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in MetaTrader pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in MetaTrader pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum step required before the trailing stop is moved again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Manual close mode for the button.
	/// </summary>
	public CloseMode ManualCloseMode
	{
		get => _closeMode.Value;
		set => _closeMode.Value = value;
	}

	/// <summary>
	/// Manual close symbol scope for the button.
	/// </summary>
	public CloseSymbol ManualCloseSymbol
	{
		get => _closeSymbol.Value;
		set => _closeSymbol.Value = value;
	}

	/// <summary>
	/// Manual close profit filter for the button.
	/// </summary>
	public CloseProfitFilter ManualCloseProfitFilter
	{
		get => _closeProfitFilter.Value;
		set => _closeProfitFilter.Value = value;
	}

	/// <summary>
	/// Manual boolean button that triggers closing routines when set to <c>true</c>.
	/// </summary>
	public bool CloseAll
	{
		get => _closeAll.Value;
		set
		{
			if (!value)
			{
				_closeAll.Value = false;
				return;
			}

			if (_closeAll.Value)
			{
				return;
			}

			_closeAll.Value = true;
			ExecuteManualClose();
			_closeAll.Value = false;
		}
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TrailingOnlyCloseAllButtonStrategy"/>.
	/// </summary>
	public TrailingOnlyCloseAllButtonStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 500m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss distance in MetaTrader pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 1000m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit distance in MetaTrader pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 200m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop", "Trailing stop distance in MetaTrader pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 50m)
			.SetNotNegative()
			.SetDisplay("Trailing Step", "Additional profit in MetaTrader pips before moving the stop", "Risk");

		_closeMode = Param(nameof(ManualCloseMode), CloseMode.Positions)
			.SetDisplay("Close Mode", "Objects affected by the manual close", "Manual")
			.SetCanOptimize(false);

		_closeSymbol = Param(nameof(ManualCloseSymbol), CloseSymbol.Chart)
			.SetDisplay("Symbol Scope", "Whether to close only the main symbol or all", "Manual")
			.SetCanOptimize(false);

		_closeProfitFilter = Param(nameof(ManualCloseProfitFilter), CloseProfitFilter.ProfitOnly)
			.SetDisplay("Profit Filter", "Filter positions by floating PnL before closing", "Manual")
			.SetCanOptimize(false);

		_closeAll = Param(nameof(CloseAll), false)
			.SetDisplay("Close All", "Set to true to close according to the configured filters", "Manual")
			.SetCanOptimize(false);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetTrailingState();
		_lastAsk = null;
		_lastBid = null;
		_previousPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
		{
			throw new InvalidOperationException("Security is not specified.");
		}

		if (Portfolio == null)
		{
			throw new InvalidOperationException("Portfolio is not specified.");
		}

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
		{
			throw new InvalidOperationException("Trailing step must be greater than zero when trailing is enabled.");
		}

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
			ResetTrailingState();
		}
		else if (_previousPosition <= 0m && Position > 0m)
		{
			_shortStop = null;
			_shortTake = null;
		}
		else if (_previousPosition >= 0m && Position < 0m)
		{
			_longStop = null;
			_longTake = null;
		}

		_previousPosition = Position;
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj) && askObj != null)
		{
			_lastAsk = (decimal)askObj;
		}

		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) && bidObj != null)
		{
			_lastBid = (decimal)bidObj;
		}

		UpdateProtectionLevels();
	}

	private void UpdateProtectionLevels()
	{
		if (Security?.PriceStep is not decimal step || step <= 0m)
		{
			return;
		}

		var stopDistance = GetPipDistance(StopLossPips);
		var takeDistance = GetPipDistance(TakeProfitPips);
		var trailDistance = GetPipDistance(TrailingStopPips);
		var stepDistance = GetPipDistance(TrailingStepPips);

		if (Position > 0m)
		{
			UpdateLongProtection(stopDistance, takeDistance, trailDistance, stepDistance);
		}
		else if (Position < 0m)
		{
			UpdateShortProtection(stopDistance, takeDistance, trailDistance, stepDistance);
		}
		else
		{
			ResetTrailingState();
		}
	}

	private void UpdateLongProtection(decimal stopDistance, decimal takeDistance, decimal trailDistance, decimal stepDistance)
	{
		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
		{
			return;
		}

		if (!_longStop.HasValue && stopDistance > 0m)
		{
			_longStop = entryPrice - stopDistance;
		}

		if (!_longTake.HasValue && takeDistance > 0m)
		{
			_longTake = entryPrice + takeDistance;
		}

		if (trailDistance > 0m && _lastBid is decimal bidPrice)
		{
			var profitDistance = bidPrice - entryPrice;
			var activationDistance = trailDistance + stepDistance;

			if (profitDistance > activationDistance)
			{
				var threshold = bidPrice - activationDistance;
				if (!_longStop.HasValue || _longStop.Value < threshold)
				{
					_longStop = bidPrice - trailDistance;
					LogInfo($"Long trailing stop updated to {_longStop:F5}.");
				}
			}
		}

		if (_longStop.HasValue && _lastBid is decimal currentBid && currentBid <= _longStop.Value)
		{
			var volume = Position;
			if (volume > 0m)
			{
				SellMarket(volume);
				LogInfo($"Long stop triggered at {currentBid:F5}.");
			}

			ResetTrailingState();
			return;
		}

		if (_longTake.HasValue && _lastBid is decimal bid && bid >= _longTake.Value)
		{
			var volume = Position;
			if (volume > 0m)
			{
				SellMarket(volume);
				LogInfo($"Long take profit triggered at {bid:F5}.");
			}

			ResetTrailingState();
		}
	}

	private void UpdateShortProtection(decimal stopDistance, decimal takeDistance, decimal trailDistance, decimal stepDistance)
	{
		var entryPrice = PositionPrice;
		if (entryPrice <= 0m)
		{
			return;
		}

		if (!_shortStop.HasValue && stopDistance > 0m)
		{
			_shortStop = entryPrice + stopDistance;
		}

		if (!_shortTake.HasValue && takeDistance > 0m)
		{
			_shortTake = entryPrice - takeDistance;
		}

		if (trailDistance > 0m && _lastAsk is decimal askPrice)
		{
			var profitDistance = entryPrice - askPrice;
			var activationDistance = trailDistance + stepDistance;

			if (profitDistance > activationDistance)
			{
				var threshold = askPrice + activationDistance;
				if (!_shortStop.HasValue || _shortStop.Value > threshold)
				{
					_shortStop = askPrice + trailDistance;
					LogInfo($"Short trailing stop updated to {_shortStop:F5}.");
				}
			}
		}

		if (_shortStop.HasValue && _lastAsk is decimal currentAsk && currentAsk >= _shortStop.Value)
		{
			var volume = Math.Abs(Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
				LogInfo($"Short stop triggered at {currentAsk:F5}.");
			}

			ResetTrailingState();
			return;
		}

		if (_shortTake.HasValue && _lastAsk is decimal ask && ask <= _shortTake.Value)
		{
			var volume = Math.Abs(Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
				LogInfo($"Short take profit triggered at {ask:F5}.");
			}

			ResetTrailingState();
		}
	}

	private decimal GetPipDistance(decimal pips)
	{
		if (pips <= 0m)
		{
			return 0m;
		}

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		{
			return 0m;
		}

		var multiplier = Security?.Decimals is int decimals && (decimals == 3 || decimals == 5) ? 10m : 1m;
		return pips * step * multiplier;
	}

	private void ExecuteManualClose()
	{
		if (ManualCloseMode != CloseMode.Orders)
		{
			ClosePositionsByFilter();
		}

		if (ManualCloseMode != CloseMode.Positions)
		{
			CancelOrdersByFilter();
		}
	}

	private void ClosePositionsByFilter()
	{
		if (Portfolio == null)
		{
			return;
		}

		foreach (var position in Portfolio.Positions.ToArray())
		{
			if (!ShouldClosePosition(position))
			{
				continue;
			}

			var security = position.Security;
			if (security == null)
			{
				continue;
			}

			var volume = position.CurrentValue;
			if (volume == 0m)
			{
				continue;
			}

			if (volume > 0m)
			{
				SellMarket(volume, security);
			}
			else
			{
				BuyMarket(Math.Abs(volume), security);
			}
		}
	}

	private bool ShouldClosePosition(Position position)
	{
		if (position == null)
		{
			return false;
		}

		if (ManualCloseSymbol == CloseSymbol.Chart && Security != null && !Equals(position.Security, Security))
		{
			return false;
		}

		var pnl = position.PnL ?? 0m;

		switch (ManualCloseProfitFilter)
		{
			case CloseProfitFilter.ProfitOnly when pnl <= 0m:
			{
				return false;
			}
			case CloseProfitFilter.LossOnly when pnl >= 0m:
			{
				return false;
			}
		}

		return true;
	}

	private void CancelOrdersByFilter()
	{
		foreach (var order in Orders.ToArray())
		{
			if (!ShouldCancelOrder(order))
			{
				continue;
			}

			if (order.State == OrderStates.Active || order.State == OrderStates.Pending)
			{
				CancelOrder(order);
			}
		}
	}

	private bool ShouldCancelOrder(Order order)
	{
		if (order == null)
		{
			return false;
		}

		if (ManualCloseSymbol == CloseSymbol.Chart && Security != null && !Equals(order.Security, Security))
		{
			return false;
		}

		return true;
	}

	private void ResetTrailingState()
	{
		_longStop = null;
		_shortStop = null;
		_longTake = null;
		_shortTake = null;
	}
}

