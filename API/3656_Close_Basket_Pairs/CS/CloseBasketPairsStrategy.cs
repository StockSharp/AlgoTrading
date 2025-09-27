using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes positions belonging to a predefined currency basket once profit or loss thresholds are met.
/// This strategy mirrors the MetaTrader "Close Basket Pairs" script using the StockSharp high level API.
/// </summary>
public class CloseBasketPairsStrategy : Strategy
{
	private readonly StrategyParam<string> _basketDefinition;
	private readonly StrategyParam<decimal> _profitThreshold;
	private readonly StrategyParam<decimal> _lossThreshold;

	private readonly Dictionary<string, Sides> _basketPairs = new(StringComparer.InvariantCultureIgnoreCase);

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CloseBasketPairsStrategy()
	{
		_basketDefinition = Param(nameof(BasketPairs), "EURUSD|BUY,GBPUSD|SELL,USDJPY|BUY")
			.SetDisplay("Basket Pairs", "Comma separated list of SYMBOL|SIDE entries, e.g. EURUSD|BUY", "General");

		_profitThreshold = Param(nameof(ProfitThreshold), 0m)
			.SetDisplay("Profit Threshold", "Close matching positions when their floating profit is above this value", "General")
			.SetCanOptimize(true)
			.SetOptimize(10m, 500m, 10m);

		_lossThreshold = Param(nameof(LossThreshold), 0m)
			.SetDisplay("Loss Threshold", "Close matching positions when their floating loss is below this (negative) value", "General")
			.SetCanOptimize(true)
			.SetOptimize(-500m, -10m, 10m);
	}

	/// <summary>
	/// Comma separated list describing the managed basket. Each entry must follow SYMBOL|BUY or SYMBOL|SELL format.
	/// </summary>
	public string BasketPairs
	{
		get => _basketDefinition.Value;
		set => _basketDefinition.Value = value;
	}

	/// <summary>
	/// Positive profit (in portfolio currency) required to close profitable positions.
	/// </summary>
	public decimal ProfitThreshold
	{
		get => _profitThreshold.Value;
		set => _profitThreshold.Value = value;
	}

	/// <summary>
	/// Negative loss (in portfolio currency) that forces closing losing positions.
	/// </summary>
	public decimal LossThreshold
	{
		get => _lossThreshold.Value;
		set => _lossThreshold.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var (symbol, _) in ParseBasketPairs(BasketPairs, false))
		{
			var security = this.GetSecurity(symbol);
			if (security != null)
				yield return (security, DataType.Ticks);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_basketPairs.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_basketPairs.Clear();

		foreach (var (symbol, side) in ParseBasketPairs(BasketPairs, true))
			_basketPairs[symbol] = side;

		if (_basketPairs.Count == 0)
		{
			LogError("No valid basket pairs were parsed. The strategy will stop.");
			Stop();
			return;
		}

		if (ProfitThreshold <= 0m && LossThreshold >= 0m)
		{
			LogInfo("Both profit and loss thresholds are disabled. The strategy will stop.");
			Stop();
			return;
		}

		// Evaluate positions immediately and then every second.
		TimerInterval = TimeSpan.FromSeconds(1);

		TryClosePositions();
	}

	/// <inheritdoc />
	protected override void OnTimer()
	{
		base.OnTimer();

		TryClosePositions();
	}

	private IEnumerable<(string symbol, Sides side)> ParseBasketPairs(string definition, bool logErrors)
	{
		if (definition.IsEmptyOrWhiteSpace())
		{
			if (logErrors)
				LogError("Basket definition is empty.");
			yield break;
		}

		foreach (var rawEntry in definition.Split(',', StringSplitOptions.RemoveEmptyEntries))
		{
			var entry = rawEntry.Trim();
			if (entry.Length == 0)
				continue;

			var parts = entry.Split('|', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length != 2)
			{
				if (logErrors)
					LogError($"Basket entry '{entry}' must follow SYMBOL|SIDE format.");
				continue;
			}

			var symbol = parts[0].Trim();
			var sideText = parts[1].Trim();

			if (symbol.Length == 0 || sideText.Length == 0)
			{
				if (logErrors)
					LogError($"Basket entry '{entry}' cannot contain empty parts.");
				continue;
			}

			Sides side;
			if (sideText.Equals("BUY", StringComparison.InvariantCultureIgnoreCase))
			{
				side = Sides.Buy;
			}
			else if (sideText.Equals("SELL", StringComparison.InvariantCultureIgnoreCase))
			{
				side = Sides.Sell;
			}
			else
			{
				if (logErrors)
					LogError($"Basket entry '{entry}' uses unsupported side '{sideText}'.");
				continue;
			}

			yield return (symbol, side);
		}
	}

	private bool TryGetBasketSide(Security security, out Sides side)
	{
		// Try matching by the full security identifier first.
		if (_basketPairs.TryGetValue(security.Id, out side))
			return true;

		// Fallback to the instrument code, matching typical MetaTrader symbols.
		return _basketPairs.TryGetValue(security.Code, out side);
	}

	private void TryClosePositions()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		foreach (var position in portfolio.Positions)
		{
			var security = position.Security ?? Security;
			if (security == null)
				continue;

			if (!TryGetBasketSide(security, out var expectedSide))
				continue;

			var volume = position.CurrentValue ?? 0m;
			if (volume == 0m)
				continue;

			var positionSide = volume > 0m ? Sides.Buy : Sides.Sell;
			if (positionSide != expectedSide)
				continue;

			var profit = position.PnL ?? 0m;

			if (ProfitThreshold > 0m && profit > 0m && profit >= ProfitThreshold)
			{
				ClosePosition(security, volume, $"profit {profit:0.##} above threshold {ProfitThreshold:0.##}");
				continue;
			}

			if (LossThreshold < 0m && profit < 0m && profit <= -LossThreshold)
			{
				ClosePosition(security, volume, $"loss {profit:0.##} below threshold {LossThreshold:0.##}");
			}
		}
	}

	private void ClosePosition(Security security, decimal volume, string reason)
	{
		if (volume == 0m)
			return;

		var exitSide = volume > 0m ? Sides.Sell : Sides.Buy;
		if (HasActiveExitOrder(security, exitSide))
			return;

		// Send the opposite side market order to flatten the position.
		if (volume > 0m)
		{
			SellMarket(volume, security);
		}
		else
		{
			BuyMarket(-volume, security);
		}

		LogInfo($"Closing {security.Id} position ({volume:0.####}) because {reason}.");
	}

	private bool HasActiveExitOrder(Security security, Sides side)
	{
		foreach (var order in Orders)
		{
			if (order.Security != security)
				continue;

			if (!order.State.IsActive())
				continue;

			if (order.Side == side)
				return true;
		}

		return false;
	}
}
