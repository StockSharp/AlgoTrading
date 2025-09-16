using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that monitors account losses and closes positions when risk limits are exceeded.
/// It supports daily loss limit, per-trade loss limit and trailing profit protection.
/// </summary>
public class RiskManagerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _dailyRisk;
	private readonly StrategyParam<decimal> _tradeRisk;
	private readonly StrategyParam<decimal> _trailRisk;

	private decimal _startBalance;
	private decimal _peakProfit;
	private DateTime _day;

	/// <summary>
	/// Maximum allowed daily loss in percent.
	/// </summary>
	public decimal DailyRisk { get => _dailyRisk.Value; set => _dailyRisk.Value = value; }

	/// <summary>
	/// Maximum allowed loss for a single open position in percent of initial balance.
	/// </summary>
	public decimal TradeRisk { get => _tradeRisk.Value; set => _tradeRisk.Value = value; }

	/// <summary>
	/// Allowed profit drop from the peak in percent before all positions are closed.
	/// </summary>
	public decimal TrailingRisk { get => _trailRisk.Value; set => _trailRisk.Value = value; }

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public RiskManagerStrategy()
	{
		_dailyRisk = Param(nameof(DailyRisk), 5m)
			.SetDisplay("Daily Loss %", "Maximum allowed loss per day", "Risk");

		_tradeRisk = Param(nameof(TradeRisk), 0m)
			.SetDisplay("Trade Loss %", "Maximum loss for an open position", "Risk");

		_trailRisk = Param(nameof(TrailingRisk), 0m)
			.SetDisplay("Trail Stop %", "Profit drop to trigger exit", "Risk");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_startBalance = Portfolio.CurrentValue ?? 0m;
		_day = time.Date;
		_peakProfit = 0m;

		// Start position protection once when strategy starts.
		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnTimer(DateTimeOffset time)
	{
		base.OnTimer(time);

		// Reset tracking values at the start of a new day.
		if (time.Date > _day)
		{
			_startBalance = Portfolio.CurrentValue ?? _startBalance;
			_peakProfit = 0m;
			_day = time.Date;
		}

		CheckDailyRisk();
		CheckTrailingStop();
		CheckTradeRisk();
	}

	// Check daily loss against allowed percentage.
	private void CheckDailyRisk()
	{
		if (_startBalance <= 0m)
			return;

		var value = Portfolio.CurrentValue ?? _startBalance;
		var lossPercent = (value - _startBalance) / _startBalance * 100m;

		if (lossPercent <= -DailyRisk)
			CloseAll("Daily loss limit reached");
	}

	// Check per trade loss for the current position.
	private void CheckTradeRisk()
	{
		if (TradeRisk <= 0m || Position == 0 || _startBalance <= 0m)
			return;

		var price = Security.LastTrade?.Price ?? Security.LastPrice ?? 0m;
		var openPnL = Position * (price - PositionPrice);
		var lossPercent = openPnL / _startBalance * 100m;

		if (lossPercent <= -TradeRisk)
			CloseAll("Trade loss limit reached");
	}

	// Monitor trailing profit and close positions if profit drops too much.
	private void CheckTrailingStop()
	{
		if (TrailingRisk <= 0m || _startBalance <= 0m)
			return;

		var value = Portfolio.CurrentValue ?? _startBalance;
		var profitPercent = (value - _startBalance) / _startBalance * 100m;

		if (profitPercent > _peakProfit)
			_peakProfit = profitPercent;

		if (_peakProfit - profitPercent >= TrailingRisk)
		{
			CloseAll("Trailing profit target hit");
			_peakProfit = profitPercent;
		}
	}
}
