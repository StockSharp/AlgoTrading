using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Automatically closes every open position when the floating profit or loss reaches configured thresholds.
/// Replicates the AutoCloseOnProfitLoss MetaTrader expert behaviour.
/// </summary>
public class AutoCloseOnProfitLossStrategy : Strategy
{
	private readonly StrategyParam<decimal> _targetProfit;
	private readonly StrategyParam<decimal> _maxLoss;
	private readonly StrategyParam<bool> _enableProfitClose;
	private readonly StrategyParam<bool> _enableLossClose;
	private readonly StrategyParam<bool> _showAlerts;
	private readonly StrategyParam<DataType> _candleType;

	private bool _closeAllRequested;
	private string? _pendingReason;

	/// <summary>
	/// Floating profit required to trigger a full exit.
	/// </summary>
	public decimal TargetProfit
	{
		get => _targetProfit.Value;
		set => _targetProfit.Value = value;
	}

	/// <summary>
	/// Floating loss (negative value) that triggers a full exit.
	/// </summary>
	public decimal MaxLoss
	{
		get => _maxLoss.Value;
		set => _maxLoss.Value = value;
	}

	/// <summary>
	/// Enables the profit-based exit rule.
	/// </summary>
	public bool EnableProfitClose
	{
		get => _enableProfitClose.Value;
		set => _enableProfitClose.Value = value;
	}

	/// <summary>
	/// Enables the loss-based exit rule.
	/// </summary>
	public bool EnableLossClose
	{
		get => _enableLossClose.Value;
		set => _enableLossClose.Value = value;
	}

	/// <summary>
	/// Emits informational messages when the exit routine runs.
	/// </summary>
	public bool ShowAlerts
	{
		get => _showAlerts.Value;
		set => _showAlerts.Value = value;
	}

	/// <summary>
	/// Candle series used to periodically evaluate floating profit and loss.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Configures default parameters.
	/// </summary>
	public AutoCloseOnProfitLossStrategy()
	{
		_targetProfit = Param(nameof(TargetProfit), 100m)
			.SetDisplay("Target Profit", "Floating profit (in portfolio currency) that triggers the exit", "General")
			.SetCanOptimize(true)
			.SetOptimize(50m, 500m, 50m);

		_maxLoss = Param(nameof(MaxLoss), -50m)
			.SetDisplay("Max Loss", "Floating loss (negative value) that triggers the exit", "General")
			.SetCanOptimize(true)
			.SetOptimize(-300m, -50m, 50m);

		_enableProfitClose = Param(nameof(EnableProfitClose), true)
			.SetDisplay("Enable Profit Close", "Activate the profit-based exit rule", "General");

		_enableLossClose = Param(nameof(EnableLossClose), true)
			.SetDisplay("Enable Loss Close", "Activate the loss-based exit rule", "General");

		_showAlerts = Param(nameof(ShowAlerts), true)
			.SetDisplay("Show Alerts", "Log detailed messages when positions are closed", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used to trigger periodic checks", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_closeAllRequested = false;
		_pendingReason = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (EnableProfitClose && TargetProfit <= 0m)
			throw new InvalidOperationException("TargetProfit must be greater than zero when profit closing is enabled.");

		if (EnableLossClose && MaxLoss >= 0m)
			throw new InvalidOperationException("MaxLoss must be negative when loss closing is enabled.");

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with completed candles to emulate the OnTick checks at candle close.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_closeAllRequested)
		{
			// Keep sending orders until every position is flattened.
			CloseAllPositions();

			if (!HasAnyOpenPosition())
			{
				_closeAllRequested = false;
				_pendingReason = null;
			}

			return;
		}

		var totalProfit = CalculateTotalProfit();

		if (EnableProfitClose && totalProfit >= TargetProfit)
		{
			_pendingReason = $"Target profit reached: {totalProfit:0.##}";
			TriggerCloseAll();
			return;
		}

		if (EnableLossClose && totalProfit <= MaxLoss)
		{
			_pendingReason = $"Max loss reached: {totalProfit:0.##}";
			TriggerCloseAll();
		}
	}

	private void TriggerCloseAll()
	{
		if (ShowAlerts && !string.IsNullOrEmpty(_pendingReason))
			LogInfo($"Closing all positions. Reason: {_pendingReason}.");

		_closeAllRequested = true;
		CloseAllPositions();
	}

	private decimal CalculateTotalProfit()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return 0m;

		// Prefer the aggregated floating profit provided by the portfolio when available.
		if (portfolio.CurrentProfit is decimal currentProfit)
			return currentProfit;

		var total = 0m;

		// Fallback: accumulate the reported PnL of each open position.
		foreach (var position in portfolio.Positions)
			total += position.PnL ?? 0m;

		return total;
	}

	private void CloseAllPositions()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		var securities = new HashSet<Security>();

		if (Security != null)
			securities.Add(Security);

		// Include securities from child strategies that might manage independent positions.
		foreach (var position in Positions)
		{
			if (position.Security != null)
				securities.Add(position.Security);
		}

		// Include all securities that have positions inside the portfolio.
		foreach (var position in portfolio.Positions)
		{
			if (position.Security != null)
				securities.Add(position.Security);
		}

		foreach (var security in securities)
		{
			var volume = GetPositionValue(security, portfolio) ?? 0m;
			if (volume > 0m)
			{
				// Send a sell market order to flatten long exposure.
				SellMarket(volume, security);
			}
			else if (volume < 0m)
			{
				// Send a buy market order to offset short exposure.
				BuyMarket(-volume, security);
			}
		}
	}

	private bool HasAnyOpenPosition()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return false;

		if (Position != 0m)
			return true;

		foreach (var position in portfolio.Positions)
		{
			var volume = position.CurrentValue ?? 0m;
			if (volume != 0m)
				return true;
		}

		return false;
	}
}
