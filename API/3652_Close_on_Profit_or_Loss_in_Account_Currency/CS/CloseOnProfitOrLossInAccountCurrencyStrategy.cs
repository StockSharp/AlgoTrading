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

using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes all positions when portfolio equity reaches configured profit or loss thresholds.
/// Pending orders are cancelled before liquidating the exposure.
/// </summary>
public class CloseOnProfitOrLossInAccountCurrencyStrategy : Strategy
{
	private readonly StrategyParam<decimal> _positiveClosure;
	private readonly StrategyParam<decimal> _negativeClosure;
	private readonly StrategyParam<DataType> _candleType;

	private bool _closeRequested;

	/// <summary>
	/// Equity level in account currency that triggers closing all positions when exceeded.
	/// </summary>
	public decimal PositiveClosureInAccountCurrency
	{
		get => _positiveClosure.Value;
		set => _positiveClosure.Value = value;
	}

	/// <summary>
	/// Equity level in account currency that triggers closing all positions when reached on drawdown.
	/// </summary>
	public decimal NegativeClosureInAccountCurrency
	{
		get => _negativeClosure.Value;
		set => _negativeClosure.Value = value;
	}

	/// <summary>
	/// Candle type used as a heartbeat to evaluate portfolio equity.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CloseOnProfitOrLossInAccountCurrencyStrategy"/> class.
	/// </summary>
	public CloseOnProfitOrLossInAccountCurrencyStrategy()
	{
		_positiveClosure = Param(nameof(PositiveClosureInAccountCurrency), 0m)
			.SetDisplay("Positive Closure", "Equity level that triggers full liquidation", "Risk");

		_negativeClosure = Param(nameof(NegativeClosureInAccountCurrency), 0m)
			.SetDisplay("Negative Closure", "Equity floor that forces liquidation", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Heartbeat Candle", "Candle type that triggers equity checks", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			return Array.Empty<(Security, DataType)>();

		return new (Security, DataType)[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio cannot be null.");

		if (Security == null)
			throw new InvalidOperationException("Security must be set to subscribe for candles.");

		// Start protective stop-loss processing once at launch.
		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var equity = Portfolio?.CurrentValue;

		if (equity is null)
			return;

		if (PositiveClosureInAccountCurrency > 0m && equity >= PositiveClosureInAccountCurrency)
		{
			RequestCloseAll("Positive equity target reached.");
			return;
		}

		if (NegativeClosureInAccountCurrency > 0m && equity <= NegativeClosureInAccountCurrency)
			RequestCloseAll("Negative equity threshold reached.");
	}

	private void RequestCloseAll(string reason)
	{
		if (_closeRequested)
			return;

		_closeRequested = true;

		LogInfo(reason);

		// Cancel any pending orders to avoid unexpected executions during liquidation.
		CancelActiveOrders();

		foreach (var position in Positions.ToArray())
		{
			var value = GetPositionValue(position.Security, Portfolio) ?? 0m;

			if (value == 0m)
				continue;

			// Submit a market order opposite to the current exposure.
			ClosePosition(position.Security);
		}

		// Stop the strategy after sending exit orders, mirroring ExpertRemove behavior.
		Stop();
	}
}

