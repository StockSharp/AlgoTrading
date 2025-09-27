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
/// Protective trailing manager converted from the MetaTrader "trade protector" expert advisor.
/// The strategy does not open positions and instead adjusts protective orders for existing trades.
/// </summary>
public class TradeProtectorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _proportionalThresholdPips;
	private readonly StrategyParam<decimal> _proportionalRatio;
	private readonly StrategyParam<bool> _useEscape;
	private readonly StrategyParam<decimal> _escapeLevelPips;
	private readonly StrategyParam<decimal> _escapeTakeProfitPips;
	private readonly StrategyParam<bool> _enableDetailedLogging;

	private Order _stopOrder;
	private Order _takeProfitOrder;
	private Sides? _currentSide;
	private decimal? _currentStopPrice;
	private decimal? _currentTakeProfitPrice;
	private decimal? _currentBid;
	private decimal? _currentAsk;
	private decimal _pipSize;
	private decimal _pointSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="TradeProtectorStrategy"/> class.
	/// </summary>
	public TradeProtectorStrategy()
	{
		_trailingStopPips = Param(nameof(TrailingStopPips), 35m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Initial trailing distance expressed in pips", "Trailing")
			.SetCanOptimize(true);

		_proportionalThresholdPips = Param(nameof(ProportionalThresholdPips), 12m)
			.SetNotNegative()
			.SetDisplay("Proportional Threshold (pips)", "Profit in pips before proportional trailing activates", "Trailing")
			.SetCanOptimize(true);

		_proportionalRatio = Param(nameof(ProportionalRatio), 0.35m)
			.SetNotNegative()
			.SetDisplay("Proportional Ratio", "Multiplier applied to floating profit for proportional stops", "Trailing")
			.SetCanOptimize(true);

		_useEscape = Param(nameof(UseEscape), false)
			.SetDisplay("Use Escape", "Enable automatic escape take-profit placement", "Escape");

		_escapeLevelPips = Param(nameof(EscapeLevelPips), 0m)
			.SetNotNegative()
			.SetDisplay("Escape Level (pips)", "Losing distance that arms the escape take-profit", "Escape")
			.SetCanOptimize(true);

		_escapeTakeProfitPips = Param(nameof(EscapeTakeProfitPips), 35m)
			.SetDisplay("Escape Take Profit (pips)", "Target distance used after the escape condition triggers", "Escape")
			.SetCanOptimize(true);

		_enableDetailedLogging = Param(nameof(EnableDetailedLogging), false)
			.SetDisplay("Enable Detailed Logging", "Write informative log entries whenever orders are adjusted", "Diagnostics");
	}

	/// <summary>
	/// Trailing stop distance in pips used for both long and short positions.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum profit in pips required before proportional trailing is calculated.
	/// </summary>
	public decimal ProportionalThresholdPips
	{
		get => _proportionalThresholdPips.Value;
		set => _proportionalThresholdPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to floating profit when building the proportional stop-loss.
	/// </summary>
	public decimal ProportionalRatio
	{
		get => _proportionalRatio.Value;
		set => _proportionalRatio.Value = value;
	}

	/// <summary>
	/// Enable the escape mode that places a take-profit after heavy drawdown.
	/// </summary>
	public bool UseEscape
	{
		get => _useEscape.Value;
		set => _useEscape.Value = value;
	}

	/// <summary>
	/// Loss in pips that must be exceeded before the escape logic arms the take-profit.
	/// </summary>
	public decimal EscapeLevelPips
	{
		get => _escapeLevelPips.Value;
		set => _escapeLevelPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips applied after the escape trigger fires.
	/// Supports negative values for a controlled loss exit.
	/// </summary>
	public decimal EscapeTakeProfitPips
	{
		get => _escapeTakeProfitPips.Value;
		set => _escapeTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Enable verbose logging that mirrors the optional file logging from the original script.
	/// </summary>
	public bool EnableDetailedLogging
	{
		get => _enableDetailedLogging.Value;
		set => _enableDetailedLogging.Value = value;
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

		ResetProtectionOrders();
		_currentSide = null;
		_currentStopPrice = null;
		_currentTakeProfitPrice = null;
		_currentBid = null;
		_currentAsk = null;
		_pipSize = 0m;
		_pointSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculateAdjustedPoint();
		_pointSize = Security?.PriceStep ?? 0m;
		if (_pointSize <= 0m)
			_pointSize = 1m;

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
			ResetProtectionOrders();
			_currentSide = null;
			return;
		}

		var side = Position > 0m ? Sides.Buy : Sides.Sell;
		if (_currentSide != side)
		{
			ResetProtectionOrders();
			_currentSide = side;
		}
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

		var side = Position > 0m ? Sides.Buy : Sides.Sell;
		if (_currentSide != side)
		{
			ResetProtectionOrders();
			_currentSide = side;
		}

		if (side == Sides.Buy)
			ManageLongPosition();
		else
			ManageShortPosition();
	}

	private void ManageLongPosition()
	{
		if (_currentBid is not decimal bid || bid <= 0m)
			return;

		if (PositionPrice is not decimal entryPrice || entryPrice <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var spread = GetSpread();

		var hasCandidate = false;
		var selectedStop = 0m;

		var baseStop = _currentStopPrice ?? 0m;
		if (baseStop <= 0m && trailingDistance > 0m)
			baseStop = bid - trailingDistance;

		if (baseStop > 0m)
		{
			selectedStop = baseStop;
			hasCandidate = true;
		}

		if (ProportionalRatio > 0m)
		{
			var thresholdPrice = entryPrice + ProportionalThresholdPips * _pipSize;
			if (bid > thresholdPrice)
			{
				var candidate = entryPrice + ProportionalRatio * (bid - entryPrice) - spread;
				if (candidate > 0m && (!hasCandidate || candidate > selectedStop))
				{
					selectedStop = candidate;
					hasCandidate = true;
				}
			}
		}

		if (trailingDistance > 0m && bid < entryPrice + 4m * spread)
		{
			var candidate = bid - trailingDistance;
			if (candidate > 0m && (!hasCandidate || candidate > selectedStop))
			{
				selectedStop = candidate;
				hasCandidate = true;
			}
		}

		if (hasCandidate)
		{
			if (selectedStop >= bid)
				selectedStop = bid - Math.Max(_pointSize, _pipSize / 10m);

			if (selectedStop > 0m)
			{
				UpdateStopOrder(true, selectedStop, volume);

				if (EnableDetailedLogging)
					LogInfo($"Long stop updated to {selectedStop}.");
			}
		}

		if (UseEscape)
			ApplyLongEscape(entryPrice, bid, volume);
		else if (_takeProfitOrder != null)
			ResetTakeProfitOrder();
	}

	private void ManageShortPosition()
	{
		if (_currentAsk is not decimal ask || ask <= 0m)
			return;

		if (PositionPrice is not decimal entryPrice || entryPrice <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var spread = GetSpread();

		var hasCandidate = false;
		var selectedStop = 0m;

		var baseStop = _currentStopPrice ?? 0m;
		if (baseStop <= 0m && trailingDistance > 0m)
			baseStop = ask + trailingDistance;

		if (baseStop > 0m)
		{
			selectedStop = baseStop;
			hasCandidate = true;
		}

		if (ProportionalRatio > 0m)
		{
			var thresholdPrice = entryPrice - ProportionalThresholdPips * _pipSize;
			if (ask < thresholdPrice)
			{
				var candidate = entryPrice - ProportionalRatio * (entryPrice - ask) + spread;
				if (candidate > 0m && (!hasCandidate || candidate < selectedStop))
				{
					selectedStop = candidate;
					hasCandidate = true;
				}
			}
		}

		if (trailingDistance > 0m && ask > entryPrice - 4m * spread)
		{
			var candidate = ask + trailingDistance + spread;
			if (candidate > 0m && (!hasCandidate || candidate < selectedStop))
			{
				selectedStop = candidate;
				hasCandidate = true;
			}
		}

		if (hasCandidate)
		{
			if (selectedStop <= ask)
				selectedStop = ask + Math.Max(_pointSize, _pipSize / 10m);

			UpdateStopOrder(false, selectedStop, volume);

			if (EnableDetailedLogging)
				LogInfo($"Short stop updated to {selectedStop}.");
		}

		if (UseEscape)
			ApplyShortEscape(entryPrice, ask, volume);
		else if (_takeProfitOrder != null)
			ResetTakeProfitOrder();
	}

	private void ApplyLongEscape(decimal entryPrice, decimal bid, decimal volume)
	{
		var triggerDistance = EscapeLevelPips * _pipSize;
		var escapeThreshold = entryPrice - triggerDistance - 5m * _pointSize;

		if (bid >= escapeThreshold)
			return;

		var takeProfitOffset = EscapeTakeProfitPips * _pipSize;
		var takeProfitPrice = entryPrice + takeProfitOffset;

		UpdateTakeProfitOrder(true, takeProfitPrice, volume);

		if (EnableDetailedLogging)
			LogInfo($"Long escape take-profit set to {takeProfitPrice}.");
	}

	private void ApplyShortEscape(decimal entryPrice, decimal ask, decimal volume)
	{
		var triggerDistance = EscapeLevelPips * _pipSize;
		var escapeThreshold = entryPrice + triggerDistance + 5m * _pointSize;

		if (ask <= escapeThreshold)
			return;

		var takeProfitOffset = EscapeTakeProfitPips * _pipSize;
		var takeProfitPrice = entryPrice - takeProfitOffset;

		UpdateTakeProfitOrder(false, takeProfitPrice, volume);

		if (EnableDetailedLogging)
			LogInfo($"Short escape take-profit set to {takeProfitPrice}.");
	}

	private void UpdateStopOrder(bool isLong, decimal stopPrice, decimal volume)
	{
		if (stopPrice <= 0m || volume <= 0m)
			return;

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
		{
			if (_stopOrder.Price == stopPrice && _stopOrder.Volume == volume)
				return;

			CancelOrder(_stopOrder);
		}

		_stopOrder = isLong
			? SellStop(price: stopPrice, volume: volume)
			: BuyStop(price: stopPrice, volume: volume);

		_currentStopPrice = stopPrice;
	}

	private void UpdateTakeProfitOrder(bool isLong, decimal takeProfitPrice, decimal volume)
	{
		if (volume <= 0m)
			return;

		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
		{
			if (_takeProfitOrder.Price == takeProfitPrice && _takeProfitOrder.Volume == volume)
				return;

			CancelOrder(_takeProfitOrder);
		}

		_takeProfitOrder = isLong
			? SellLimit(price: takeProfitPrice, volume: volume)
			: BuyLimit(price: takeProfitPrice, volume: volume);

		_currentTakeProfitPrice = takeProfitPrice;
	}

	private void ResetProtectionOrders()
	{
		ResetStopOrder();
		ResetTakeProfitOrder();
	}

	private void ResetStopOrder()
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		_stopOrder = null;
		_currentStopPrice = null;
	}

	private void ResetTakeProfitOrder()
	{
		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);

		_takeProfitOrder = null;
		_currentTakeProfitPrice = null;
	}

	private decimal GetSpread()
	{
		if (_currentBid.HasValue && _currentAsk.HasValue)
		{
			var spread = _currentAsk.Value - _currentBid.Value;
			if (spread > 0m)
				return spread;
		}

		return _pointSize > 0m ? _pointSize : 1m;
	}

	private decimal CalculateAdjustedPoint()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = CountDecimals(step);
		return decimals is 3 or 5 ? step * 10m : step;
	}

	private static int CountDecimals(decimal value)
	{
		value = Math.Abs(value);
		var decimals = 0;

		while (value != Math.Truncate(value) && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}
