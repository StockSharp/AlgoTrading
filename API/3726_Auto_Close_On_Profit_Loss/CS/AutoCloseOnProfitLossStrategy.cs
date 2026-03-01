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
	private string _pendingReason;

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
			
			.SetOptimize(50m, 500m, 50m);

		_maxLoss = Param(nameof(MaxLoss), -50m)
			.SetDisplay("Max Loss", "Floating loss (negative value) that triggers the exit", "General")
			
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
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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
		if (ShowAlerts && !_pendingReason.IsEmpty())
			LogInfo($"Closing all positions. Reason: {_pendingReason}.");

		_closeAllRequested = true;
		CloseAllPositions();
	}

	private decimal CalculateTotalProfit()
	{
		return PnL;
	}

	private void CloseAllPositions()
	{
		if (Position > 0m)
			SellMarket(Math.Abs(Position));
		else if (Position < 0m)
			BuyMarket(Math.Abs(Position));
	}

	private bool HasAnyOpenPosition()
	{
		return Position != 0m;
	}
}

