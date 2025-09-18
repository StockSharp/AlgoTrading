using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader advisor trade_protector-1_2.
/// Supervises open positions, applies layered trailing logic, and arms escape take-profits after adverse moves.
/// </summary>
public class TradeProtectorStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableLogging;
	private readonly StrategyParam<decimal> _initialStopPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _proportionalActivationPips;
	private readonly StrategyParam<decimal> _proportionalRatio;
	private readonly StrategyParam<bool> _useEscapeMode;
	private readonly StrategyParam<decimal> _escapeTriggerPips;
	private readonly StrategyParam<decimal> _escapeTakeProfitPips;

	private decimal _previousPosition;
	private decimal _pointSize;
	private decimal _pipSize;

	private decimal _bestBid;
	private decimal _bestAsk;
	private bool _hasBid;
	private bool _hasAsk;

	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;

	private decimal? _longStopPrice;
	private decimal? _longEscapeTarget;
	private decimal? _shortStopPrice;
	private decimal? _shortEscapeTarget;

	private bool _longEscapeArmed;
	private bool _shortEscapeArmed;
	private bool _exitInProgress;

	private decimal? _lastBuyPrice;
	private decimal? _lastSellPrice;

	/// <summary>
	/// Enables verbose logging of trailing and escape adjustments.
	/// </summary>
	public bool EnableLogging
	{
		get => _enableLogging.Value;
		set => _enableLogging.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance expressed in pips. Applied when a fresh position appears with no stop configured yet.
	/// </summary>
	public decimal InitialStopPips
	{
		get => _initialStopPips.Value;
		set => _initialStopPips.Value = value;
	}

	/// <summary>
	/// Static trailing distance used before the proportional stop activates.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Profit threshold that switches from the static trailing stop to the proportional rule.
	/// </summary>
	public decimal ProportionalActivationPips
	{
		get => _proportionalActivationPips.Value;
		set => _proportionalActivationPips.Value = value;
	}

	/// <summary>
	/// Portion of the open profit kept when the proportional stop is active. Values in the 0..1 range mimic the original MQL behaviour.
	/// </summary>
	public decimal ProportionalRatio
	{
		get => _proportionalRatio.Value;
		set => _proportionalRatio.Value = value;
	}

	/// <summary>
	/// Arms the escape mode after a deep drawdown to terminate the trade on a bounce.
	/// </summary>
	public bool UseEscapeMode
	{
		get => _useEscapeMode.Value;
		set => _useEscapeMode.Value = value;
	}

	/// <summary>
	/// Depth of the adverse excursion (in pips) that activates the escape take-profit.
	/// </summary>
	public decimal EscapeTriggerPips
	{
		get => _escapeTriggerPips.Value;
		set => _escapeTriggerPips.Value = value;
	}

	/// <summary>
	/// Distance (in pips) for the escape take-profit once the trigger fires. Negative values accept a controlled loss.
	/// </summary>
	public decimal EscapeTakeProfitPips
	{
		get => _escapeTakeProfitPips.Value;
		set => _escapeTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TradeProtectorStrategy"/>.
	/// </summary>
	public TradeProtectorStrategy()
	{
		_enableLogging = Param(nameof(EnableLogging), true)
		.SetDisplay("Enable Logging", "Write detailed log entries when stops change", "General");

		_initialStopPips = Param(nameof(InitialStopPips), 15m)
		.SetDisplay("Initial Stop (pips)", "Baseline stop distance assigned to fresh positions", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 35m)
		.SetDisplay("Trailing Stop (pips)", "Static trailing distance used before proportional mode", "Risk");

		_proportionalActivationPips = Param(nameof(ProportionalActivationPips), 12m)
		.SetDisplay("Proportional Threshold (pips)", "Profit required before proportional trailing activates", "Risk");

		_proportionalRatio = Param(nameof(ProportionalRatio), 0.35m)
		.SetDisplay("Proportional Ratio", "Fraction of profits kept by the proportional stop", "Risk");

		_useEscapeMode = Param(nameof(UseEscapeMode), false)
		.SetDisplay("Use Escape", "Enable escape take-profit after deep drawdown", "Escape");

		_escapeTriggerPips = Param(nameof(EscapeTriggerPips), 0m)
		.SetDisplay("Escape Trigger (pips)", "Adverse excursion that arms the escape logic", "Escape");

		_escapeTakeProfitPips = Param(nameof(EscapeTakeProfitPips), 35m)
		.SetDisplay("Escape Take-Profit (pips)", "Distance of the escape take-profit relative to the entry", "Escape");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializePriceScales();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade?.Order?.Direction == null)
		return;

		var price = trade.Trade?.Price;
		if (price == null)
		return;

		if (trade.Order.Direction == Sides.Buy)
		{
			_lastBuyPrice = price.Value;
		}
		else if (trade.Order.Direction == Sides.Sell)
		{
			_lastSellPrice = price.Value;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		var position = Position;

		if (position > 0m)
		{
			if (_previousPosition <= 0m)
			{
				InitializeLongState();
			}

			if (PositionPrice is decimal avgLong)
			_longEntryPrice = avgLong;
			else if (_longEntryPrice <= 0m && _lastBuyPrice is decimal fillPrice)
			_longEntryPrice = fillPrice;

			ResetShortState();
		}
		else if (position < 0m)
		{
			if (_previousPosition >= 0m)
			{
				InitializeShortState();
			}

			if (PositionPrice is decimal avgShort)
			_shortEntryPrice = avgShort;
			else if (_shortEntryPrice <= 0m && _lastSellPrice is decimal fillPrice)
			_shortEntryPrice = fillPrice;

			ResetLongState();
		}
		else
		{
			ResetLongState();
			ResetShortState();
			_exitInProgress = false;
		}

		_previousPosition = position;
	}

	private void InitializeLongState()
	{
		_exitInProgress = false;
		_longEscapeArmed = false;
		_longEscapeTarget = null;
		_longStopPrice = null;
		_longEntryPrice = PositionPrice ?? _lastBuyPrice ?? (_hasAsk ? _bestAsk : 0m);

		if (_longEntryPrice > 0m && InitialStopPips > 0m)
		{
			var stop = _longEntryPrice - InitialStopPips * _pipSize;
			if (stop > 0m)
			{
				_longStopPrice = NormalizePrice(stop);
				LogIfNeeded($"Initial long stop set to {_longStopPrice}.");
			}
		}
	}

	private void InitializeShortState()
	{
		_exitInProgress = false;
		_shortEscapeArmed = false;
		_shortEscapeTarget = null;
		_shortStopPrice = null;
		_shortEntryPrice = PositionPrice ?? _lastSellPrice ?? (_hasBid ? _bestBid : 0m);

		if (_shortEntryPrice > 0m && InitialStopPips > 0m)
		{
			var stop = _shortEntryPrice + InitialStopPips * _pipSize;
			if (stop > 0m)
			{
				_shortStopPrice = NormalizePrice(stop);
				LogIfNeeded($"Initial short stop set to {_shortStopPrice}.");
			}
		}
	}

	private void ResetLongState()
	{
		_longStopPrice = null;
		_longEscapeTarget = null;
		_longEscapeArmed = false;
		_longEntryPrice = 0m;
	}

	private void ResetShortState()
	{
		_shortStopPrice = null;
		_shortEscapeTarget = null;
		_shortEscapeArmed = false;
		_shortEntryPrice = 0m;
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
		{
			var bid = (decimal)bidValue;
			if (bid > 0m)
			{
				_bestBid = bid;
				_hasBid = true;
			}
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
		{
			var ask = (decimal)askValue;
			if (ask > 0m)
			{
				_bestAsk = ask;
				_hasAsk = true;
			}
		}

		ManageProtection();
	}

	private void ManageProtection()
	{
		if (_exitInProgress)
		return;

		var position = Position;
		if (position > 0m)
		{
			ManageLong(position);
		}
		else if (position < 0m)
		{
			ManageShort(position);
		}
	}

	private void ManageLong(decimal position)
	{
		if (!_hasBid)
		return;

		var entryPrice = _longEntryPrice;
		if (entryPrice <= 0m)
		return;

		var spread = _hasAsk ? Math.Max(0m, _bestAsk - _bestBid) : 0m;
		var point = GetPointSize();
		var newStop = _longStopPrice ?? 0m;

		if (TrailingStopPips > 0m)
		{
			var trailing = _bestBid - TrailingStopPips * _pipSize;
			newStop = Math.Max(newStop, trailing);
		}

		if (ProportionalRatio > 0m)
		{
			var profit = _bestBid - entryPrice;
			var activation = ProportionalActivationPips * _pipSize;
			if (profit >= activation)
			{
				var proportional = entryPrice + ProportionalRatio * profit - spread;
				newStop = Math.Max(newStop, proportional);
			}
		}

		if (point > 0m)
		{
			var maxStop = _bestBid - point;
			if (newStop > maxStop)
			newStop = maxStop;
		}

		if (newStop > 0m)
		{
			var normalized = NormalizePrice(newStop);
			if (ShouldUpdateLongStop(normalized))
			{
				_longStopPrice = normalized;
				LogIfNeeded($"Long stop moved to {normalized} (Bid={_bestBid}).");
			}
		}

		if (!UseEscapeMode)
		{
			_longEscapeTarget = null;
			_longEscapeArmed = false;
		}
		else if (!_longEscapeArmed)
		{
			var trigger = entryPrice - EscapeTriggerPips * _pipSize - 5m * point;
			if (_bestBid <= trigger)
			{
				var target = entryPrice + EscapeTakeProfitPips * _pipSize;
				var normalized = NormalizePrice(target);
				_longEscapeTarget = normalized;
				_longEscapeArmed = true;
				LogIfNeeded($"Long escape take-profit armed at {normalized} after drawdown. Bid={_bestBid}.");
			}
		}

		if (_longStopPrice is decimal stop && _bestBid <= stop)
		{
			_exitInProgress = true;
			SellMarket(Math.Abs(position));
			LogIfNeeded($"Long stop executed at {stop}.");
			return;
		}

		if (_longEscapeTarget is decimal take && _bestBid >= take)
		{
			_exitInProgress = true;
			SellMarket(Math.Abs(position));
			LogIfNeeded($"Long escape take-profit hit at {take}.");
		}
	}

	private void ManageShort(decimal position)
	{
		if (!_hasAsk)
		return;

		var entryPrice = _shortEntryPrice;
		if (entryPrice <= 0m)
		return;

		var spread = _hasBid ? Math.Max(0m, _bestAsk - _bestBid) : 0m;
		var point = GetPointSize();
		var newStop = _shortStopPrice ?? decimal.MaxValue;

		if (TrailingStopPips > 0m)
		{
			var trailing = _bestAsk + TrailingStopPips * _pipSize;
			newStop = Math.Min(newStop, trailing);
		}

		if (ProportionalRatio > 0m)
		{
			var profit = entryPrice - _bestAsk;
			var activation = ProportionalActivationPips * _pipSize;
			if (profit >= activation)
			{
				var proportional = entryPrice - ProportionalRatio * (entryPrice - _bestAsk) + spread;
				newStop = Math.Min(newStop, proportional);
			}
		}

		if (point > 0m)
		{
			var minStop = _bestAsk + point;
			if (newStop < minStop)
			newStop = minStop;
		}

		if (newStop < decimal.MaxValue)
		{
			var normalized = NormalizePrice(newStop);
			if (ShouldUpdateShortStop(normalized))
			{
				_shortStopPrice = normalized;
				LogIfNeeded($"Short stop moved to {normalized} (Ask={_bestAsk}).");
			}
		}

		if (!UseEscapeMode)
		{
			_shortEscapeTarget = null;
			_shortEscapeArmed = false;
		}
		else if (!_shortEscapeArmed)
		{
			var trigger = entryPrice + EscapeTriggerPips * _pipSize + 5m * point;
			if (_bestAsk >= trigger)
			{
				var target = entryPrice - EscapeTakeProfitPips * _pipSize;
				var normalized = NormalizePrice(target);
				_shortEscapeTarget = normalized;
				_shortEscapeArmed = true;
				LogIfNeeded($"Short escape take-profit armed at {normalized} after drawdown. Ask={_bestAsk}.");
			}
		}

		if (_shortStopPrice is decimal stop && _bestAsk >= stop)
		{
			_exitInProgress = true;
			BuyMarket(Math.Abs(position));
			LogIfNeeded($"Short stop executed at {stop}.");
			return;
		}

		if (_shortEscapeTarget is decimal take && _bestAsk <= take)
		{
			_exitInProgress = true;
			BuyMarket(Math.Abs(position));
			LogIfNeeded($"Short escape take-profit hit at {take}.");
		}
	}

	private void InitializePriceScales()
	{
		var security = Security;
		if (security == null)
		{
			_pointSize = 0m;
			_pipSize = 1m;
			return;
		}

		if (security.PriceStep is decimal step && step > 0m)
		{
			_pointSize = step;
		}
		else
		{
			_pointSize = 1m;
		}

		if (security.Decimals is int decimals && (decimals == 3 || decimals == 5))
		{
			_pipSize = _pointSize * 10m;
		}
		else
		{
			_pipSize = _pointSize;
		}

		if (_pipSize <= 0m)
			_pipSize = 1m;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		if (security == null)
		return price;

		if (security.PriceStep is decimal step && step > 0m)
		{
			var steps = Math.Round(price / step, MidpointRounding.AwayFromZero);
			return steps * step;
		}

		if (security.Decimals is int decimals)
		return Math.Round(price, decimals);

		return price;
	}

	private decimal GetPointSize()
	{
		if (_pointSize > 0m)
		return _pointSize;

		if (_pipSize > 0m)
		return _pipSize;

		return 1m;
	}

	private bool ShouldUpdateLongStop(decimal newStop)
	{
		if (newStop <= 0m)
		return false;

		if (_longStopPrice == null)
		return true;

		var diff = newStop - _longStopPrice.Value;
		if (_pointSize > 0m)
		return diff > _pointSize / 2m;

		return diff > 0m;
	}

	private bool ShouldUpdateShortStop(decimal newStop)
	{
		if (_shortStopPrice == null)
		return true;

		var diff = _shortStopPrice.Value - newStop;
		if (_pointSize > 0m)
		return diff > _pointSize / 2m;

		return diff > 0m;
	}

	private void LogIfNeeded(string message)
	{
		if (EnableLogging)
		LogInfo(message);
	}
}
