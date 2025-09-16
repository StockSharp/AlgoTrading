using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Equity trailing strategy that locks in profits after a configurable retracement.
/// </summary>
public class TrailingProfitStrategy : Strategy
{
	private readonly StrategyParam<decimal> _percentOfProfit;
	private readonly StrategyParam<decimal> _minimumProfit;
	private readonly StrategyParam<TimeSpan> _checkInterval;

	private DateTimeOffset _lastCheckTime;
	private decimal _baselineValue;
	private bool _trailingEnabled;
	private decimal _trailThreshold;
	private decimal _maxProfit;
	private bool _closing;

	/// <summary>
	/// Allowed drawdown from the peak profit in percent.
	/// </summary>
	public decimal PercentOfProfit
	{
		get => _percentOfProfit.Value;
		set => _percentOfProfit.Value = value;
	}

	/// <summary>
	/// Minimum floating profit required to arm the trailing logic.
	/// </summary>
	public decimal MinimumProfit
	{
		get => _minimumProfit.Value;
		set => _minimumProfit.Value = value;
	}

	/// <summary>
	/// Minimum interval between profit evaluations.
	/// </summary>
	public TimeSpan CheckInterval
	{
		get => _checkInterval.Value;
		set => _checkInterval.Value = value;
	}

	public TrailingProfitStrategy()
	{
		_percentOfProfit = Param(nameof(PercentOfProfit), 33m)
			.SetDisplay("Trail Percent", "Allowed drawdown from profit peak (%)", "Risk");

		_minimumProfit = Param(nameof(MinimumProfit), 1000m)
			.SetDisplay("Activation Profit", "Profit needed to enable trailing", "Risk")
			.SetGreaterThanZero();

		_checkInterval = Param(nameof(CheckInterval), TimeSpan.FromSeconds(3))
			.SetDisplay("Check Interval", "Minimum interval between profit evaluations", "General");

		ResetForValue(0m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Ticks)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetForValue(Portfolio?.CurrentValue ?? 0m);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		ResetForValue(Portfolio?.CurrentValue ?? 0m);

		SubscribeTrades()
			.Bind(ProcessTrade)
			.Start();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var now = trade.ServerTime != default ? trade.ServerTime : CurrentTime;

		if (_lastCheckTime != default && now - _lastCheckTime < CheckInterval)
			return;

		_lastCheckTime = now;

		EvaluateTrailing();
	}

	private void EvaluateTrailing()
	{
		var currentValue = Portfolio?.CurrentValue;

		if (currentValue is null)
			return;

		var profit = currentValue.Value - _baselineValue;
		var hasPosition = Math.Abs(Position) > 0m || Positions.Any();

		if (!hasPosition)
		{
			if (_closing)
				LogInfo($"All positions closed after trailing stop. Locked profit: {profit:0.##}.");

			ResetForValue(currentValue.Value);
			return;
		}

		if (!_trailingEnabled)
		{
			if (profit >= MinimumProfit)
			{
				_trailingEnabled = true;
				_maxProfit = profit;
				_trailThreshold = CalculateTrailThreshold(profit);
				LogInfo($"Trailing enabled. Peak profit {_maxProfit:0.##}, stop at {_trailThreshold:0.##}.");
			}

			return;
		}

		if (profit > _maxProfit)
		{
			_maxProfit = profit;
			var newThreshold = CalculateTrailThreshold(profit);

			if (newThreshold > _trailThreshold)
			{
				_trailThreshold = newThreshold;
				LogInfo($"New profit peak {_maxProfit:0.##}. Updated trail stop {_trailThreshold:0.##}.");
			}
		}

		if (profit <= _trailThreshold)
		{
			if (!_closing)
			{
				_closing = true;
				LogInfo($"Profit dropped to {profit:0.##}. Triggering trailing stop at {_trailThreshold:0.##}.");
			}

			CloseAllPositions();
		}
	}

	private void CloseAllPositions()
	{
		if (Position != 0m)
			ClosePosition();

		foreach (var position in Positions.ToArray())
		{
			if (Equals(position.Security, Security))
				continue;

			ClosePosition(position.Security);
		}
	}

	private decimal CalculateTrailThreshold(decimal profit)
	{
		var percent = Math.Max(0m, Math.Min(PercentOfProfit, 100m));
		return profit - profit * (percent / 100m);
	}

	private void ResetForValue(decimal currentValue)
	{
		_baselineValue = currentValue;
		_trailingEnabled = false;
		_trailThreshold = 0m;
		_maxProfit = 0m;
		_closing = false;
		_lastCheckTime = default;
	}
}
