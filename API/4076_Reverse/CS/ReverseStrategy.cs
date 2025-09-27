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
/// Strategy that closes every open position and immediately opens an opposite one.
/// </summary>
public class ReverseStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _currentSymbolOnly;
	private readonly StrategyParam<bool> _marketWatchMode;
	private readonly StrategyParam<int> _slippagePoints;
	private readonly StrategyParam<int> _retryCount;

	private readonly Dictionary<Security, ProtectionOrders> _protectionOrders = new();

	/// <summary>
	/// Initializes a new instance of <see cref="ReverseStrategy"/>.
	/// </summary>
	public ReverseStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 30m)
			.SetDisplay("Stop Loss (pips)", "Distance of the protective stop in pips", "Orders")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetDisplay("Take Profit (pips)", "Distance of the target in pips", "Orders")
			.SetCanOptimize(true);

		_currentSymbolOnly = Param(nameof(CurrentSymbolOnly), true)
			.SetDisplay("Current Symbol Only", "Reverse only the position for the selected security", "Scope");

		_marketWatchMode = Param(nameof(MarketWatchMode), false)
			.SetDisplay("Market Watch Mode", "Skip attaching protective orders if the broker forbids them on market orders", "Orders");

		_slippagePoints = Param(nameof(SlippagePoints), 3)
			.SetDisplay("Slippage (points)", "Maximum expected slippage expressed in price points", "Execution");

		_retryCount = Param(nameof(RetryCount), 3)
			.SetDisplay("Retry Count", "Number of attempts made when a market order fails", "Execution")
			.SetGreaterThanZero();
	}

	/// <summary>
	/// Protective stop distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Profit target distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Apply the reversal only to the strategy security.
	/// </summary>
	public bool CurrentSymbolOnly
	{
		get => _currentSymbolOnly.Value;
		set => _currentSymbolOnly.Value = value;
	}

	/// <summary>
	/// When true, protective orders are not attached immediately after reversing.
	/// </summary>
	public bool MarketWatchMode
	{
		get => _marketWatchMode.Value;
		set => _marketWatchMode.Value = value;
	}

	/// <summary>
	/// Maximum tolerated slippage in points. Reserved for analytics.
	/// </summary>
	public int SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	/// <summary>
	/// Number of attempts for each market order submission.
	/// </summary>
	public int RetryCount
	{
		get => _retryCount.Value;
		set => _retryCount.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, default);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		if (Portfolio == null)
		{
			Stop();
			return;
		}

		var securities = new HashSet<Security>();

		if (CurrentSymbolOnly)
		{
			if (Security != null)
				securities.Add(Security);
		}
		else
		{
			foreach (var position in Portfolio.Positions)
			{
				if (position.Security != null)
					securities.Add(position.Security);
			}
		}

		foreach (var security in securities)
			ReverseFor(security);

		Stop();
	}

	private void ReverseFor(Security security)
	{
		if (security == null || Portfolio == null)
			return;

		var position = Portfolio.Positions.FirstOrDefault(p => p.Security == security);
		var currentVolume = position?.CurrentValue ?? 0m;

		if (currentVolume == 0m)
			return;

		var step = GetPriceStep(security);
		var stopOffset = StopLossPips > 0m && step > 0m ? StopLossPips * step : (decimal?)null;
		var takeOffset = TakeProfitPips > 0m && step > 0m ? TakeProfitPips * step : (decimal?)null;
		var volume = Math.Abs(currentVolume);

		if (currentVolume > 0m)
		{
			// Close the long position first.
			ExecuteWithRetry(() => SellMarket(volume, security));
			// Immediately open a new short position with the same volume.
			ExecuteWithRetry(() => SellMarket(volume, security));

			if (!MarketWatchMode)
			{
				var entryPrice = GetShortEntryPrice(security);
				if (entryPrice.HasValue)
					AttachProtection(security, false, volume, entryPrice.Value, stopOffset, takeOffset);
			}
		}
		else if (currentVolume < 0m)
		{
			// Close the short position first.
			ExecuteWithRetry(() => BuyMarket(volume, security));
			// Immediately open a new long position with the same volume.
			ExecuteWithRetry(() => BuyMarket(volume, security));

			if (!MarketWatchMode)
			{
				var entryPrice = GetLongEntryPrice(security);
				if (entryPrice.HasValue)
					AttachProtection(security, true, volume, entryPrice.Value, stopOffset, takeOffset);
			}
		}
	}

	private void ExecuteWithRetry(Action action)
	{
		var attempts = Math.Max(1, RetryCount);

		for (var i = 0; i < attempts; i++)
		{
			try
			{
				action();
				return;
			}
			catch (Exception ex)
			{
				LogError($"Reverse order attempt {i + 1} failed: {ex.Message}");

				if (i + 1 >= attempts)
					throw;
			}
		}
	}

	private void AttachProtection(Security security, bool isLong, decimal volume, decimal entryPrice, decimal? stopOffset, decimal? takeOffset)
	{
		if (!_protectionOrders.TryGetValue(security, out var protection))
		{
			protection = new ProtectionOrders();
			_protectionOrders.Add(security, protection);
		}

		CancelProtection(protection);

		if (stopOffset.HasValue && stopOffset.Value > 0m)
		{
			var stopPrice = isLong ? entryPrice - stopOffset.Value : entryPrice + stopOffset.Value;
			protection.StopOrder = isLong
				? SellStop(volume, stopPrice, security)
				: BuyStop(volume, stopPrice, security);
		}

		if (takeOffset.HasValue && takeOffset.Value > 0m)
		{
			var takePrice = isLong ? entryPrice + takeOffset.Value : entryPrice - takeOffset.Value;
			protection.TakeProfitOrder = isLong
				? SellLimit(volume, takePrice, security)
				: BuyLimit(volume, takePrice, security);
		}
	}

	private void CancelProtection(ProtectionOrders protection)
	{
		if (protection.StopOrder != null && protection.StopOrder.State == OrderStates.Active)
			CancelOrder(protection.StopOrder);

		if (protection.TakeProfitOrder != null && protection.TakeProfitOrder.State == OrderStates.Active)
			CancelOrder(protection.TakeProfitOrder);

		protection.StopOrder = null;
		protection.TakeProfitOrder = null;
	}

	private static decimal? GetLongEntryPrice(Security security)
	{
		// Prefer the ask price for new long entries.
		return security.BestAsk?.Price ?? security.LastTrade?.Price ?? security.BestBid?.Price;
	}

	private static decimal? GetShortEntryPrice(Security security)
	{
		// Prefer the bid price for new short entries.
		return security.BestBid?.Price ?? security.LastTrade?.Price ?? security.BestAsk?.Price;
	}

	private static decimal GetPriceStep(Security security)
	{
		if (security.PriceStep != null && security.PriceStep.Value > 0m)
			return security.PriceStep.Value;

		if (security.MinStep != null && security.MinStep.Value > 0m)
			return security.MinStep.Value;

		return 0m;
	}

	private sealed class ProtectionOrders
	{
		public Order StopOrder { get; set; }
		public Order TakeProfitOrder { get; set; }
	}
}
