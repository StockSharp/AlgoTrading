using System;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demonstrates subscription and unsubscription to trading signals.
/// </summary>
public class SignalsDemoStrategy : Strategy
{
	private readonly StrategyParam<long> _signalId;
	private readonly StrategyParam<decimal> _equityLimit;
	private readonly StrategyParam<decimal> _slippage;
	private readonly StrategyParam<decimal> _depositPercent;

	/// <summary>
	/// Identifier of the trading signal.
	/// </summary>
	public long SignalId
	{
		get => _signalId.Value;
		set => _signalId.Value = value;
	}

	/// <summary>
	/// Maximum equity allowed for copying the signal.
	/// </summary>
	public decimal EquityLimit
	{
		get => _equityLimit.Value;
		set => _equityLimit.Value = value;
	}

	/// <summary>
	/// Allowed slippage when copying trades.
	/// </summary>
	public decimal Slippage
	{
		get => _slippage.Value;
		set => _slippage.Value = value;
	}

	/// <summary>
	/// Percentage of account equity to allocate.
	/// </summary>
	public decimal DepositPercent
	{
		get => _depositPercent.Value;
		set => _depositPercent.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public SignalsDemoStrategy()
	{
		_signalId = Param(nameof(SignalId), 0L)
			.SetDisplay("Signal Id", "Identifier of the signal", "General");

		_equityLimit = Param(nameof(EquityLimit), 0m)
			.SetDisplay("Equity Limit", "Maximum equity to use", "General");

		_slippage = Param(nameof(Slippage), 2m)
			.SetDisplay("Slippage", "Allowed slippage", "General");

		_depositPercent = Param(nameof(DepositPercent), 5m)
			.SetDisplay("Deposit %", "Percent of account equity to allocate", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeToSignal();
	}

	/// <inheritdoc />
	protected override void OnStopping()
	{
		UnsubscribeFromSignal();

		base.OnStopping();
	}

	private void SubscribeToSignal()
	{
		if (DepositPercent < 5m || DepositPercent > 95m)
		{
			this.LogInfo("Deposit percent is not specified. 5% used by default.");
			DepositPercent = 5m;
		}

		if (EquityLimit < 0m)
		{
			this.LogInfo("Error in equity limit. Value = {0}", EquityLimit);
			EquityLimit = 0m;
		}

		if (Slippage < 0m)
		{
			this.LogInfo("Error in slippage. Value = {0}", Slippage);
			Slippage = 0m;
		}

		this.LogInfo("Subscribe to signal {0}", SignalId);
	}

	private void UnsubscribeFromSignal()
	{
		this.LogInfo("Unsubscribe from current signal.");
	}
}
