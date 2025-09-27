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
/// Risk monitor that reproduces the volume calculations from the original MT4 script.
/// Calculates recommended lot sizes from the account balance, tracks realized gains and losses,
/// and publishes the results in the strategy comment so the trader can adjust positions manually.
/// </summary>
public class RiskMonitorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskPercent;

	private decimal _totalPositivePnL;
	private decimal _totalNegativePnL;
	private decimal _currentPosition;
	private decimal _averagePrice;

	/// <summary>
	/// Percentage of the account balance allocated to new positions.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set
		{
			_riskPercent.Value = value;
			UpdateRiskComment();
		}
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RiskMonitorStrategy"/>.
	/// </summary>
	public RiskMonitorStrategy()
	{
		_riskPercent = Param(nameof(RiskPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Portion of balance used to size positions", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5m, 30m, 5m);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		// Reset cached values so stale data is not reused after a reset.
		_totalPositivePnL = 0m;
		_totalNegativePnL = 0m;
		_currentPosition = 0m;
		_averagePrice = 0m;
		Comment = string.Empty;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Capture initial position snapshot when the strategy becomes active.
		_currentPosition = Position;
		_averagePrice = 0m;
		_totalPositivePnL = 0m;
		_totalNegativePnL = 0m;

		UpdateRiskComment();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		// Clear the comment when the strategy stops to avoid showing outdated data.
		Comment = string.Empty;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		// Keep the cached position synchronized with the actual position value.
		_currentPosition = Position;

		UpdateRiskComment();
	}

	/// <inheritdoc />
	protected override void OnPnLChanged(decimal diff)
	{
		base.OnPnLChanged(diff);

		// Refresh the displayed statistics whenever floating PnL changes.
		UpdateRiskComment();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Trade == null || trade.Order == null)
			return;

		var volume = trade.Trade.Volume;
		if (volume <= 0m)
			return;

		var price = trade.Trade.Price;
		var commission = trade.Trade.Commission ?? 0m;
		var direction = trade.Order.Side == Sides.Buy ? 1m : -1m;

		var previousPosition = _currentPosition;
		var previousAveragePrice = _averagePrice;
		var signedVolume = direction * volume;

		// Update cached position before calculating averages to mirror MT4 behaviour.
		_currentPosition += signedVolume;

		if (previousPosition == 0m || Math.Sign(previousPosition) == Math.Sign(direction))
		{
			// Either opening a brand-new position or adding to an existing one.
			var previousAbs = Math.Abs(previousPosition);
			var newAbs = previousAbs + volume;
			_averagePrice = newAbs > 0m
				? ((previousAbs * previousAveragePrice) + (volume * price)) / newAbs
				: 0m;
		}
		else
		{
			// Trade direction differs from the existing position, so at least part closes.
			var closingVolume = Math.Min(Math.Abs(previousPosition), volume);
			if (closingVolume > 0m)
			{
				var realizedPnL = previousPosition > 0m
					? (price - previousAveragePrice) * closingVolume
					: (previousAveragePrice - price) * closingVolume;

				// Allocate commission to the closing portion of the trade.
				if (volume > 0m)
				{
					var commissionShare = commission * (closingVolume / volume);
					realizedPnL -= commissionShare;
				}

				if (realizedPnL >= 0m)
					_totalPositivePnL += realizedPnL;
				else
					_totalNegativePnL += realizedPnL;
			}

			var residualVolume = volume - Math.Min(Math.Abs(previousPosition), volume);
			if (_currentPosition == 0m)
			{
				// Position fully closed.
				_averagePrice = 0m;
			}
			else if (residualVolume > 0m && Math.Sign(_currentPosition) == Math.Sign(direction))
			{
				// The trade flipped the position and opened in the opposite direction.
				_averagePrice = price;
			}
			else
			{
				// Partial close without reversal keeps the previous average price.
				_averagePrice = previousAveragePrice;
			}
		}

		UpdateRiskComment();
	}

	private void UpdateRiskComment()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		var balance = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		var lotStep = Security?.VolumeStep ?? 0m;

		var baseLots = NormalizeVolume(balance / 1000m, lotStep);
		var riskLots = NormalizeVolume(baseLots * (RiskPercent / 100m), lotStep);
		var openLots = Math.Abs(Position);

		var lotDifference = Math.Abs(riskLots) - openLots;
		var effectiveStep = lotStep > 0m ? lotStep : 0m;
		if (effectiveStep > 0m && lotDifference < 0m && lotDifference > -effectiveStep)
			lotDifference = 0m;

		var totalProfit = Math.Round(_totalPositivePnL + _totalNegativePnL, 2, MidpointRounding.AwayFromZero);
		var totalProfitPercent = 0m;
		var denominator = balance - totalProfit;
		if (denominator != 0m)
			totalProfitPercent = Math.Round(100m / denominator * totalProfit, 2, MidpointRounding.AwayFromZero);

		var floatingPnL = PnL;
		var floatingPercent = balance != 0m
			? Math.Round(100m / balance * floatingPnL, 2, MidpointRounding.AwayFromZero)
			: 0m;

		// Mirror the MT4 Comment() output using the StockSharp strategy comment.
		Comment = $"Base lots: {baseLots:0.###}, Risk lots: {riskLots:0.###}, Open lots: {openLots:0.###}, Lots to adjust: {lotDifference:0.###}"
			+ $"\nRisk: {RiskPercent:0.##}%  Floating PnL: {floatingPnL:0.##} ({floatingPercent:0.##}%)"
			+ $"\nRealized profit: {totalProfit:0.##} ({totalProfitPercent:0.##}%)";
	}

	private static decimal NormalizeVolume(decimal volume, decimal lotStep)
	{
		if (volume <= 0m)
			return 0m;

		var decimals = lotStep > 0m ? Math.Max(1, GetDecimalPlaces(lotStep)) : 1;
		var normalized = Math.Round(volume, decimals, MidpointRounding.AwayFromZero);

		if (lotStep <= 0m)
			return normalized;

		var steps = Math.Floor(normalized / lotStep);
		var adjusted = steps * lotStep;
		return adjusted < 0m ? 0m : adjusted;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		value = Math.Abs(value);
		if (value == 0m)
			return 0;

		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0x7F;
	}
}

