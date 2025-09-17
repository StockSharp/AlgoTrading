using System;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Informational panel that periodically logs account and market metrics.
/// </summary>
public class PainelStrategy : Strategy
{
	private decimal _lastPrice;
	private decimal _sessionHigh;
	private decimal _sessionLow;
	private string _positionLabel = "Flat";
	private decimal _profit;
	private decimal _balance;
	private decimal _initialBalance;
	private bool _isStarted;

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Capture the starting balance once so profit can be calculated later.
		var portfolio = Portfolio;
		if (portfolio != null)
		{
			_initialBalance = portfolio.BeginValue ?? portfolio.CurrentValue ?? 0m;
		}

		// Listen for level1 updates to keep price, high and low synchronized.
		SubscribeLevel1().Bind(ProcessLevel1).Start();

		// Listen for trade prints to capture the last traded price when available.
		SubscribeTrades().Bind(ProcessTrade).Start();

		// Periodically log the collected information to emulate the original dashboard.
		Timer.Start(TimeSpan.FromSeconds(1), OnTimer);
		_isStarted = true;
		UpdateAccountSnapshot();
	}

	/// <inheritdoc />
	protected override void OnStopped(DateTimeOffset time)
	{
		if (_isStarted)
		{
			Timer.Stop();
			_isStarted = false;
		}

		base.OnStopped(time);
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		// Extract last price, high and low from level1 messages when present.
		if (message.Changes.TryGetValue(Level1Fields.LastTradePrice, out var last))
		{
			_lastPrice = (decimal)last;
		}

		if (message.Changes.TryGetValue(Level1Fields.HighPrice, out var high))
		{
			_sessionHigh = (decimal)high;
		}

		if (message.Changes.TryGetValue(Level1Fields.LowPrice, out var low))
		{
			_sessionLow = (decimal)low;
		}
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		// Store the price from the most recent trade to keep the panel accurate.
		if (trade.TradePrice is decimal price)
		{
			_lastPrice = price;
		}
	}

	private void OnTimer(DateTimeOffset time)
	{
		// Refresh account values before sending the next dashboard update.
		UpdateAccountSnapshot();
		LogPanelState();
	}

	private void UpdateAccountSnapshot()
	{
		// Convert the current numerical position into the textual status used by the panel.
		_positionLabel = Position switch
		{
			> 0 => "Long",
			< 0 => "Short",
			_ => "Flat",
		};

		var portfolio = Portfolio;
		if (portfolio == null)
		{
			_profit = 0m;
			_balance = 0m;
			return;
		}

		var balance = portfolio.CurrentValue ?? portfolio.BeginValue ?? _initialBalance;
		if (_initialBalance == 0m && balance != 0m)
		{
			_initialBalance = balance;
		}

		_balance = balance;
		_profit = balance - _initialBalance;
	}

	private void LogPanelState()
	{
		// Compose a single log entry that mirrors the original interface fields.
		var securityId = Security?.Id ?? string.Empty;
		var accountName = Portfolio?.Name ?? string.Empty;

		AddInfoLog(
			$"Symbol: {securityId}; " +
			$"Last: {_lastPrice.ToString(\"0.#####\", CultureInfo.InvariantCulture)}; " +
			$"High: {_sessionHigh.ToString(\"0.#####\", CultureInfo.InvariantCulture)}; " +
			$"Low: {_sessionLow.ToString(\"0.#####\", CultureInfo.InvariantCulture)}; " +
			$"Position: {_positionLabel}; " +
			$"User: {accountName}; " +
			$"Profit: {_profit.ToString(\"0.##\", CultureInfo.InvariantCulture)}; " +
			$"Balance: {_balance.ToString(\"0.##\", CultureInfo.InvariantCulture)}
		);
	}
}
