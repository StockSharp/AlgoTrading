using System;
using System.Text;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dashboard strategy that reproduces the MetaTrader risk manager panel.
/// Calculates position sizing metrics, daily risk usage, and exposes
/// the formatted summary through the strategy comment for UI bindings.
/// </summary>
public class RiskManagerInfoPanelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _entryPrice;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _maxDailyRiskPercent;
	private readonly StrategyParam<int> _updateIntervalSeconds;
	private readonly StrategyParam<bool> _useSupportMessage;
	private readonly StrategyParam<string> _supportMessage;

	private decimal _dailyRealizedPnL;
	private DateTime _currentDay;
	private string _lastSnapshot = string.Empty;
	private bool _isDailyLimitBreached;

	/// <summary>
	/// Risk allocation per trade expressed in percent of account equity.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set
		{
			_riskPercent.Value = value;
			UpdateRiskSnapshot();
		}
	}

	/// <summary>
	/// Reference entry price used to compute stop-loss and take-profit targets.
	/// </summary>
	public decimal EntryPrice
	{
		get => _entryPrice.Value;
		set
		{
			_entryPrice.Value = value;
			UpdateRiskSnapshot();
		}
	}

	/// <summary>
	/// Stop-loss distance expressed as percentage of the entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set
		{
			_stopLossPercent.Value = value;
			UpdateRiskSnapshot();
		}
	}

	/// <summary>
	/// Take-profit distance expressed as percentage of the entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set
		{
			_takeProfitPercent.Value = value;
			UpdateRiskSnapshot();
		}
	}

	/// <summary>
	/// Maximum daily loss allowed before trading should be suspended.
	/// </summary>
	public decimal MaxDailyRiskPercent
	{
		get => _maxDailyRiskPercent.Value;
		set
		{
			_maxDailyRiskPercent.Value = value;
			UpdateRiskSnapshot();
		}
	}

	/// <summary>
	/// Interval in seconds for refreshing the informational snapshot.
	/// </summary>
	public int UpdateIntervalSeconds
	{
		get => _updateIntervalSeconds.Value;
		set
		{
			_updateIntervalSeconds.Value = value;
			RestartTimer();
		}
	}

	/// <summary>
	/// Enable or disable the optional support message line.
	/// </summary>
	public bool UseSupportMessage
	{
		get => _useSupportMessage.Value;
		set
		{
			_useSupportMessage.Value = value;
			UpdateRiskSnapshot();
		}
	}

	/// <summary>
	/// Custom support message displayed alongside the risk summary.
	/// </summary>
	public string SupportMessage
	{
		get => _supportMessage.Value;
		set
		{
			_supportMessage.Value = value;
			UpdateRiskSnapshot();
		}
	}

	/// <summary>
	/// Latest textual snapshot formatted like the MetaTrader info panel.
	/// </summary>
	public string RiskSnapshot => _lastSnapshot;

	/// <summary>
	/// Realized profit or loss accumulated during the current day.
	/// </summary>
	public decimal DailyRealizedPnL => _dailyRealizedPnL;

	/// <summary>
	/// Indicates whether the configured daily risk limit has been breached.
	/// </summary>
	public bool IsDailyRiskLimitBreached => _isDailyLimitBreached;

	/// <summary>
	/// Initializes the strategy parameters that mirror the MQL inputs.
	/// </summary>
	public RiskManagerInfoPanelStrategy()
	{
		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Risk per trade expressed as percent of equity", "Risk");

		_entryPrice = Param(nameof(EntryPrice), 1.1000m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Price", "Reference entry price used for calculations", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 0.2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss distance as percent of entry", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.5m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit distance as percent of entry", "Risk");

		_maxDailyRiskPercent = Param(nameof(MaxDailyRiskPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Max Daily Risk %", "Maximum drawdown allowed for the day", "Risk");

		_updateIntervalSeconds = Param(nameof(UpdateIntervalSeconds), 10)
			.SetGreaterThanZero()
			.SetDisplay("Update Interval", "Seconds between dashboard refreshes", "General");

		_useSupportMessage = Param(nameof(UseSupportMessage), true)
			.SetDisplay("Use Support Message", "Append the optional support note", "General");

		_supportMessage = Param(nameof(SupportMessage), "Contact support for assistance!")
			.SetDisplay("Support Message", "Custom message shown in the panel", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_dailyRealizedPnL = 0m;
		_currentDay = default;
		_lastSnapshot = string.Empty;
		_isDailyLimitBreached = false;
		Comment = string.Empty;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_currentDay = time.Date;
		_dailyRealizedPnL = 0m;
		_isDailyLimitBreached = false;

		RestartTimer();
		UpdateRiskSnapshot(time);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		Timer.Stop();
		Comment = string.Empty;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Trade == null)
			return;

		var tradeDay = trade.Trade.ServerTime.Date;

		if (_currentDay == default || tradeDay != _currentDay)
		{
			_currentDay = tradeDay;
			_dailyRealizedPnL = 0m;
		}

		_dailyRealizedPnL += trade.PnL ?? 0m;

		UpdateRiskSnapshot(trade.Trade.ServerTime);
	}

	private void RestartTimer()
	{
		if (ProcessState != ProcessStates.Started)
			return;

		Timer.Stop();

		var seconds = Math.Max(1, UpdateIntervalSeconds);
		Timer.Start(TimeSpan.FromSeconds(seconds), UpdateRiskSnapshot);
	}

	private void UpdateRiskSnapshot()
	{
		var time = CurrentTime != default ? CurrentTime : DateTimeOffset.UtcNow;
		UpdateRiskSnapshot(time);
	}

	private void UpdateRiskSnapshot(DateTimeOffset time)
	{
		if (_currentDay == default)
		{
			_currentDay = time.Date;
			_dailyRealizedPnL = 0m;
		}
		else if (time.Date > _currentDay)
		{
			_currentDay = time.Date;
			_dailyRealizedPnL = 0m;
		}

		var portfolio = Portfolio;
		var balance = portfolio?.CurrentBalance ?? portfolio?.BeginValue ?? 0m;
		var equity = portfolio?.CurrentValue ?? balance;
		var floatingPnL = PnL;
		var login = portfolio?.Name ?? "-";

		var computedSl = CalculateStopPrice();
		var computedTp = CalculateTakeProfit();
		var tickSize = Security?.PriceStep ?? 0m;
		var tickValue = Security?.StepPrice ?? 0m;
		var riskPips = tickSize > 0m ? Math.Abs(EntryPrice - computedSl) / tickSize : 0m;
		var riskMoney = equity * (RiskPercent / 100m);
		var recommendedLots = CalculateRecommendedVolume(riskPips, tickSize, tickValue, riskMoney);
		var rewardPips = tickSize > 0m ? Math.Abs(computedTp - EntryPrice) / tickSize : 0m;
		var rewardRisk = riskPips > 0m ? rewardPips / riskPips : (decimal?)null;

		var dailyRiskLimit = equity * (MaxDailyRiskPercent / 100m);
		_isDailyLimitBreached = dailyRiskLimit > 0m && _dailyRealizedPnL < -dailyRiskLimit;

		var decimals = Math.Max(0, Security?.Decimals ?? 5);
		var priceFormat = "F" + decimals;

		var builder = new StringBuilder();
		builder.AppendLine($"Risk Manager for {Security?.Id ?? Security?.Code ?? "Unknown"}");
		builder.AppendLine("-----------------------------");
		builder.AppendLine($"Account: {login}");
		builder.AppendLine($"Balance: {balance:0.00}");
		builder.AppendLine($"Equity: {equity:0.00}");
		builder.AppendLine($"Floating PnL: {floatingPnL:0.00}");
		builder.AppendLine($"Updated: {time:HH:mm}");
		builder.AppendLine();
		builder.AppendLine($"Risk/Trade: {RiskPercent:0.##}%");
		builder.AppendLine($"Entry Price: {EntryPrice.ToString(priceFormat)}");
		builder.AppendLine($"Stop Loss: {computedSl.ToString(priceFormat)} ({StopLossPercent:0.##}%)");
		builder.AppendLine($"Take Profit: {computedTp.ToString(priceFormat)} ({TakeProfitPercent:0.##}%)");
		builder.AppendLine();
		builder.AppendLine($"Distance (pips): {riskPips:0.##}");
		builder.AppendLine($"Risk ($): {riskMoney:0.00}");
		builder.AppendLine($"Recommended Volume: {recommendedLots:0.####}");

		if (rewardRisk.HasValue)
			builder.AppendLine($"Reward:Risk Ratio: {rewardRisk.Value:0.##}");

		builder.AppendLine();
		builder.AppendLine($"Daily P/L: {DailyRealizedPnL:0.00}");
		builder.AppendLine($"Daily Risk Limit: {dailyRiskLimit:0.00}");

		if (IsDailyRiskLimitBreached)
		{
			builder.AppendLine();
			builder.AppendLine("*** DAILY RISK LIMIT EXCEEDED! Trading suspended.");
		}

		if (UseSupportMessage && !string.IsNullOrWhiteSpace(SupportMessage))
		{
			builder.AppendLine();
			builder.AppendLine(SupportMessage);
		}

		_lastSnapshot = builder.ToString();
		Comment = _lastSnapshot;
	}

	private decimal CalculateStopPrice()
	{
		return EntryPrice - (EntryPrice * (StopLossPercent / 100m));
	}

	private decimal CalculateTakeProfit()
	{
		return EntryPrice + (EntryPrice * (TakeProfitPercent / 100m));
	}

	private decimal CalculateRecommendedVolume(decimal riskPips, decimal tickSize, decimal tickValue, decimal riskMoney)
	{
		if (riskPips <= 0m || tickSize <= 0m || tickValue <= 0m)
			return 0m;

		var pipValuePerLot = tickValue / tickSize;
		if (pipValuePerLot <= 0m)
			return 0m;

		var riskPerLot = riskPips * pipValuePerLot;
		if (riskPerLot <= 0m)
			return 0m;

		var rawVolume = riskMoney / riskPerLot;
		return NormalizeVolume(rawVolume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		var max = security.MaxVolume ?? 0m;

		if (step <= 0m)
		{
			if (max > 0m && volume > max)
				return max;

			return volume;
		}

		var steps = Math.Floor(volume / step);
		if (steps <= 0m)
			return 0m;

		var adjusted = steps * step;
		if (max > 0m && adjusted > max)
			adjusted = max;

		return adjusted;
	}
}
