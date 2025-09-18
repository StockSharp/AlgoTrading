using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing stop and position management utility converted from the
/// "Trailing Only with Close All Button" MQL expert.
/// Tracks floating profit, applies pip-based trailing logic and provides
/// manual buttons (parameters) for closing positions.
/// </summary>
public class TrailingCloseManagerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _closeProfitThreshold;
	private readonly StrategyParam<decimal> _closeLossThreshold;
	private readonly StrategyParam<int> _retryCount;
	private readonly StrategyParam<TimeSpan> _retryDelay;
	private readonly StrategyParam<bool> _closeAllButton;
	private readonly StrategyParam<bool> _closeProfitableButton;
	private readonly StrategyParam<bool> _closeLosingButton;

	private decimal _pipSize;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;
	private bool _longTrailingActive;
	private bool _shortTrailingActive;
	private bool _longExitRequested;
	private bool _shortExitRequested;
	private decimal? _lastTradePrice;

	private readonly List<CloseRequest> _closeRequests = new();

	private sealed class CloseRequest
	{
		public CloseMode Mode { get; init; }
		public int RemainingAttempts { get; set; }
		public DateTimeOffset NextAttempt { get; set; }
		public string Reason { get; set; } = string.Empty;
	}

	private enum CloseMode
	{
		All,
		Profitable,
		Losing
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimal pip advance before the trailing stop moves again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Floating profit required to close every open position.
	/// </summary>
	public decimal CloseProfitThreshold
	{
		get => _closeProfitThreshold.Value;
		set => _closeProfitThreshold.Value = value;
	}

	/// <summary>
	/// Floating loss that forces the strategy to flatten the portfolio.
	/// </summary>
	public decimal CloseLossThreshold
	{
		get => _closeLossThreshold.Value;
		set => _closeLossThreshold.Value = value;
	}

	/// <summary>
	/// Maximum number of close attempts when a manual button is pressed.
	/// </summary>
	public int RetryCount
	{
		get => _retryCount.Value;
		set => _retryCount.Value = value;
	}

	/// <summary>
	/// Delay between consecutive close attempts.
	/// </summary>
	public TimeSpan RetryDelay
	{
		get => _retryDelay.Value;
		set => _retryDelay.Value = value;
	}

	/// <summary>
	/// Requests closing of every open position (emulates "Close All" button).
	/// </summary>
	public bool CloseAllButton
	{
		get => _closeAllButton.Value;
		set => _closeAllButton.Value = value;
	}

	/// <summary>
	/// Requests closing of profitable positions only.
	/// </summary>
	public bool CloseProfitableButton
	{
		get => _closeProfitableButton.Value;
		set => _closeProfitableButton.Value = value;
	}

	/// <summary>
	/// Requests closing of losing positions only.
	/// </summary>
	public bool CloseLosingButton
	{
		get => _closeLosingButton.Value;
		set => _closeLosingButton.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public TrailingCloseManagerStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 500m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss (pips)", "Initial stop loss distance expressed in pips", "Risk Management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 1000m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit (pips)", "Initial take profit distance expressed in pips", "Risk Management");

		_trailingStopPips = Param(nameof(TrailingStopPips), 200m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Stop (pips)", "Distance used by the trailing stop", "Risk Management");

		_trailingStepPips = Param(nameof(TrailingStepPips), 50m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Step (pips)", "Minimum additional profit before the trail moves", "Risk Management");

		_closeProfitThreshold = Param(nameof(CloseProfitThreshold), 500m)
		.SetDisplay("Close Profit Threshold", "Close everything once floating profit reaches this value", "Close Buttons");

		_closeLossThreshold = Param(nameof(CloseLossThreshold), 0m)
		.SetDisplay("Close Loss Threshold", "Close everything once floating loss reaches this negative value", "Close Buttons");

		_retryCount = Param(nameof(RetryCount), 5)
		.SetGreaterOrEqualZero()
		.SetDisplay("Retry Count", "How many times the close routines retry", "Close Buttons");

		_retryDelay = Param(nameof(RetryDelay), TimeSpan.FromMilliseconds(500))
		.SetDisplay("Retry Delay", "Delay between close attempts", "Close Buttons");

		_closeAllButton = Param(nameof(CloseAllButton), false)
		.SetDisplay("Close All", "Manual trigger that closes every open position", "Close Buttons");

		_closeProfitableButton = Param(nameof(CloseProfitableButton), false)
		.SetDisplay("Close Profit", "Manual trigger that closes profitable positions", "Close Buttons");

		_closeLosingButton = Param(nameof(CloseLosingButton), false)
		.SetDisplay("Close Losing", "Manual trigger that closes losing positions", "Close Buttons");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetPositionState();
		_closeRequests.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		SubscribeTrades()
		.Bind(ProcessTrade)
		.Start();

		Timer.Start(TimeSpan.FromMilliseconds(250), ProcessPendingCloseRequests);
	}

	/// <inheritdoc />
	protected override void OnStop()
	{
		Timer.Stop();

		base.OnStop();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		UpdatePositionTargets();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetPositionState();
			return;
		}

		UpdatePositionTargets();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice;
		if (price == null || price.Value <= 0m)
			return;

		_lastTradePrice = price.Value;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var time = trade.ServerTime != default ? trade.ServerTime : trade.LocalTime;
		if (time == default)
			time = CurrentTime;

		ProcessManualButtons(time);
		ProcessFloatingThresholds(time);
		ProcessCloseRequests(time);

		ManageTrailing(price.Value);
	}

	private void ProcessManualButtons(DateTimeOffset time)
	{
		if (CloseAllButton)
		{
			CloseAllButton = false;
			EnqueueCloseRequest(CloseMode.All, "Manual close all requested", time);
		}

		if (CloseProfitableButton)
		{
			CloseProfitableButton = false;
			EnqueueCloseRequest(CloseMode.Profitable, "Manual close of profitable positions requested", time);
		}

		if (CloseLosingButton)
		{
			CloseLosingButton = false;
			EnqueueCloseRequest(CloseMode.Losing, "Manual close of losing positions requested", time);
		}
	}

	private void ProcessFloatingThresholds(DateTimeOffset time)
	{
		var totalPnL = GetFloatingPnL();

		if (CloseProfitThreshold > 0m && totalPnL >= CloseProfitThreshold)
		{
			EnqueueCloseRequest(CloseMode.All, $"Floating profit {totalPnL:0.##} reached the configured threshold {CloseProfitThreshold:0.##}", time);
		}

		if (CloseLossThreshold < 0m && totalPnL <= CloseLossThreshold)
		{
			EnqueueCloseRequest(CloseMode.All, $"Floating loss {totalPnL:0.##} reached the configured threshold {CloseLossThreshold:0.##}", time);
		}
	}

	private void ProcessPendingCloseRequests()
	{
		ProcessCloseRequests(CurrentTime);
	}

	private void ProcessCloseRequests(DateTimeOffset time)
	{
		if (_closeRequests.Count == 0)
			return;

		for (var i = _closeRequests.Count - 1; i >= 0; i--)
		{
			var request = _closeRequests[i];

			if (time < request.NextAttempt)
			continue;

			var allClosed = ExecuteClose(request.Mode);

			if (allClosed)
			{
			LogInfo($"Close routine for {request.Mode} finished: {request.Reason}.");
			_closeRequests.RemoveAt(i);
			continue;
			}

			request.RemainingAttempts--;

			if (request.RemainingAttempts <= 0)
			{
			LogWarning($"Close routine for {request.Mode} exhausted retries: {request.Reason}.");
			_closeRequests.RemoveAt(i);
			continue;
			}

			var delay = RetryDelay;
			if (delay <= TimeSpan.Zero)
			delay = TimeSpan.FromMilliseconds(500);

			request.NextAttempt = time + delay;
		}
	}

	private bool ExecuteClose(CloseMode mode)
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return true;

		var exposures = new Dictionary<Security, (decimal volume, decimal pnl)>();

		foreach (var position in portfolio.Positions)
		{
			var security = position.Security ?? Security;
			if (security == null)
			continue;

			var volume = position.CurrentValue ?? 0m;
			if (volume == 0m)
			continue;

			var pnl = position.PnL ?? 0m;

			if (exposures.TryGetValue(security, out var existing))
			{
			exposures[security] = (existing.volume + volume, existing.pnl + pnl);
			}
			else
			{
			exposures.Add(security, (volume, pnl));
			}
		}

		var mainSecurity = Security;
		if (mainSecurity != null && !exposures.ContainsKey(mainSecurity))
		{
			var volume = GetPositionValue(mainSecurity, portfolio) ?? 0m;
			if (volume != 0m)
			exposures.Add(mainSecurity, (volume, 0m));
		}

		var anySent = false;

		foreach (var pair in exposures)
		{
			var security = pair.Key;
			var volume = pair.Value.volume;
			if (volume == 0m)
			continue;

			var pnl = pair.Value.pnl;

			var shouldClose = mode switch
			{
			CloseMode.All => true,
			CloseMode.Profitable => pnl > 0m,
			CloseMode.Losing => pnl < 0m,
			_ => false
			};

			if (!shouldClose)
			continue;

			anySent = true;

			var absVolume = AdjustVolume(Math.Abs(volume), security);
			if (absVolume <= 0m)
			continue;

			if (volume > 0m)
			{
			SellMarket(absVolume, security);
			}
			else
			{
			BuyMarket(absVolume, security);
			}
		}

		return !anySent;
	}

	private void ManageTrailing(decimal price)
	{
		if (Position > 0m && _longEntryPrice is decimal entry)
		{
			HandleTakeProfit(price, _longTakePrice, Sides.Sell);

			var stopDistance = StopLossPips * _pipSize;
			if (stopDistance > 0m)
			{
			var baseStop = entry - stopDistance;
			if (_longStopPrice is null || _longStopPrice < baseStop)
			_longStopPrice = baseStop;

			if (_longStopPrice is decimal stop && price <= stop && !_longExitRequested)
			{
			_longExitRequested = true;
			SellMarket(AdjustVolume(Position));
			return;
			}
			}

			ApplyTrailingForLong(price, entry);
		}
		else if (Position < 0m && _shortEntryPrice is decimal shortEntry)
		{
			HandleTakeProfit(price, _shortTakePrice, Sides.Buy);

			var stopDistance = StopLossPips * _pipSize;
			if (stopDistance > 0m)
			{
			var baseStop = shortEntry + stopDistance;
			if (_shortStopPrice is null || _shortStopPrice > baseStop)
			_shortStopPrice = baseStop;

			if (_shortStopPrice is decimal stop && price >= stop && !_shortExitRequested)
			{
			_shortExitRequested = true;
			BuyMarket(AdjustVolume(-Position));
			return;
			}
			}

			ApplyTrailingForShort(price, shortEntry);
		}
	}

	private void ApplyTrailingForLong(decimal price, decimal entry)
	{
		var trailingDistance = TrailingStopPips * _pipSize;
		if (trailingDistance <= 0m)
			return;

		var trailingStep = TrailingStepPips * _pipSize;
		var desiredStop = price - trailingDistance;

		if (!_longTrailingActive)
		{
			if (price - entry >= trailingDistance)
			{
			_longTrailingActive = true;
			_longStopPrice = desiredStop;
			LogInfo($"Long trailing activated at {desiredStop:0.#####} (price {price:0.#####}).");
			}
		}
		else if (_longStopPrice is decimal currentStop)
		{
			if (desiredStop - currentStop >= trailingStep)
			{
			_longStopPrice = desiredStop;
			LogInfo($"Long trailing stop moved to {desiredStop:0.#####} (price {price:0.#####}).");
			}

			if (!_longExitRequested && price <= _longStopPrice)
			{
			_longExitRequested = true;
			SellMarket(AdjustVolume(Position));
			}
		}
	}

	private void ApplyTrailingForShort(decimal price, decimal entry)
	{
		var trailingDistance = TrailingStopPips * _pipSize;
		if (trailingDistance <= 0m)
			return;

		var trailingStep = TrailingStepPips * _pipSize;
		var desiredStop = price + trailingDistance;

		if (!_shortTrailingActive)
		{
			if (entry - price >= trailingDistance)
			{
			_shortTrailingActive = true;
			_shortStopPrice = desiredStop;
			LogInfo($"Short trailing activated at {desiredStop:0.#####} (price {price:0.#####}).");
			}
		}
		else if (_shortStopPrice is decimal currentStop)
		{
			if (currentStop - desiredStop >= trailingStep)
			{
			_shortStopPrice = desiredStop;
			LogInfo($"Short trailing stop moved to {desiredStop:0.#####} (price {price:0.#####}).");
			}

			if (!_shortExitRequested && price >= _shortStopPrice)
			{
			_shortExitRequested = true;
			BuyMarket(AdjustVolume(-Position));
			}
		}
	}

	private void HandleTakeProfit(decimal price, decimal? takePrice, Sides exitSide)
	{
		if (takePrice is not decimal target)
			return;

		var shouldExit = exitSide == Sides.Sell ? price >= target : price <= target;
		if (!shouldExit)
			return;

		if (exitSide == Sides.Sell)
		{
			if (_longExitRequested)
			return;

			_longExitRequested = true;
			SellMarket(AdjustVolume(Position));
		}
		else
		{
			if (_shortExitRequested)
			return;

			_shortExitRequested = true;
			BuyMarket(AdjustVolume(-Position));
		}
	}

	private void EnqueueCloseRequest(CloseMode mode, string reason, DateTimeOffset time)
	{
		var existing = _closeRequests.FirstOrDefault(r => r.Mode == mode);
		if (existing != null)
		{
			existing.RemainingAttempts = RetryCount <= 0 ? 1 : RetryCount;
			existing.NextAttempt = time;
			existing.Reason = reason;
			return;
		}

		_closeRequests.Add(new CloseRequest
		{
			Mode = mode,
			RemainingAttempts = RetryCount <= 0 ? 1 : RetryCount,
			NextAttempt = time,
			Reason = reason
		});
	}

	private decimal GetFloatingPnL()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return 0m;

		if (portfolio.CurrentProfit is decimal aggregated)
			return aggregated;

		decimal total = 0m;
		foreach (var position in portfolio.Positions)
			total += position.PnL ?? 0m;

		return total;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 1m;

		var step = security.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var multiplier = (security.Decimals == 3 || security.Decimals == 5) ? 10m : 1m;
		return step * multiplier;
	}

	private void UpdatePositionTargets()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		var position = portfolio.Positions.FirstOrDefault(p => p.Security == Security);
		if (position == null)
		{
			ResetPositionState();
			return;
		}

		var volume = position.CurrentValue ?? 0m;
		var entry = position.AveragePrice ?? 0m;

		if (volume > 0m)
		{
			_longEntryPrice = entry;
			_longTrailingActive = false;
			_longExitRequested = false;
			_shortEntryPrice = null;
			_shortTrailingActive = false;
			_shortExitRequested = false;

			_longStopPrice = StopLossPips > 0m ? entry - StopLossPips * _pipSize : null;
			_longTakePrice = TakeProfitPips > 0m ? entry + TakeProfitPips * _pipSize : null;
			_shortStopPrice = null;
			_shortTakePrice = null;
		}
		else if (volume < 0m)
		{
			_shortEntryPrice = entry;
			_shortTrailingActive = false;
			_shortExitRequested = false;
			_longEntryPrice = null;
			_longTrailingActive = false;
			_longExitRequested = false;

			_shortStopPrice = StopLossPips > 0m ? entry + StopLossPips * _pipSize : null;
			_shortTakePrice = TakeProfitPips > 0m ? entry - TakeProfitPips * _pipSize : null;
			_longStopPrice = null;
			_longTakePrice = null;
		}
		else
		{
			ResetPositionState();
		}
	}

	private void ResetPositionState()
	{
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakePrice = null;
		_shortTakePrice = null;
		_longTrailingActive = false;
		_shortTrailingActive = false;
		_longExitRequested = false;
		_shortExitRequested = false;
	}

	private decimal AdjustVolume(decimal volume)
	{
		return AdjustVolume(volume, Security);
	}

	private decimal AdjustVolume(decimal volume, Security? security)
	{
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
			volume = Math.Round(volume / step, MidpointRounding.AwayFromZero) * step;

		var minVolume = security.VolumeMin ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = security.VolumeMax ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}
}
