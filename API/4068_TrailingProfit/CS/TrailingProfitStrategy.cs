using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trails floating profit for the entire position and liquidates when the gain retraces by a percentage.
/// </summary>
public class TrailingProfitStrategy : Strategy
{
	private readonly StrategyParam<decimal> _trailPercent;
	private readonly StrategyParam<decimal> _activationProfit;

	private bool _trailingActive;
	private bool _closing;
	private decimal _trailFloor;
	private bool? _lastCloseWasSell;
	private decimal _lastCloseVolume;

	/// <summary>
	/// Percentage of the peak profit kept before liquidation.
	/// </summary>
	public decimal TrailPercent
	{
		get => _trailPercent.Value;
		set => _trailPercent.Value = value;
	}

	/// <summary>
	/// Unrealized profit required to arm the trailing logic.
	/// </summary>
	public decimal ActivationProfit
	{
		get => _activationProfit.Value;
		set => _activationProfit.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="TrailingProfitStrategy"/>.
	/// </summary>
	public TrailingProfitStrategy()
	{
		_trailPercent = Param(nameof(TrailPercent), 33m)
			.SetDisplay("Trail Percent", "Percentage of profit preserved before closing", "Risk")
			.SetCanOptimize(true);

		_activationProfit = Param(nameof(ActivationProfit), 1000m)
			.SetDisplay("Activation Profit", "Unrealized profit needed to enable trailing", "Risk")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Ticks)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeTicks().Bind(ProcessTrade).Start();
		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged()
	{
		base.OnPositionChanged();

		if (Position == 0m)
			ResetState();
	}

	private void ProcessTrade(ITickTradeMessage trade)
	{
		if (trade.TradePrice is not decimal price)
			return;

		if (Position == 0m || PositionPrice == 0m)
		{
			// Without an open position there is nothing to trail.
			ResetState();
			return;
		}

		var openProfit = Position * (price - PositionPrice);

		if (!_trailingActive)
		{
			// Activate trailing once the floating profit reaches the arming threshold.
			if (openProfit > ActivationProfit)
			{
				_trailingActive = true;
				_trailFloor = CalculateTrailFloor(openProfit);
				LogInfo($"Trailing activated. Profit = {openProfit:0.##}; Floor = {_trailFloor:0.##}.");
			}

			return;
		}

		// Raise the trailing floor whenever a new profit high is recorded.
		var newFloor = CalculateTrailFloor(openProfit);
		if (newFloor > _trailFloor)
		{
			_trailFloor = newFloor;
			LogInfo($"Trail floor raised to {_trailFloor:0.##}.");
		}

		if (openProfit < _trailFloor)
			StartClosing(openProfit);

		if (_closing)
			ExecuteLiquidation();
	}

	private decimal CalculateTrailFloor(decimal profit)
	{
		var percent = TrailPercent;
		return profit - (profit * percent / 100m);
	}

	private void StartClosing(decimal openProfit)
	{
		if (_closing)
			return;

		_closing = true;
		_lastCloseWasSell = null;
		_lastCloseVolume = 0m;

		LogInfo($"Profit dropped to {openProfit:0.##} below trailing floor {_trailFloor:0.##}. Liquidating position.");
	}

	private void ExecuteLiquidation()
	{
		if (Position == 0m)
		{
			ResetState();
			return;
		}

		if (Position > 0m)
		{
			var volume = Position;
			if (_lastCloseWasSell != true || _lastCloseVolume != volume)
			{
				// Close the remaining long exposure at market.
				SellMarket(volume);
				_lastCloseWasSell = true;
				_lastCloseVolume = volume;
			}
		}
		else
		{
			var volume = -Position;
			if (_lastCloseWasSell != false || _lastCloseVolume != volume)
			{
				// Close the remaining short exposure at market.
				BuyMarket(volume);
				_lastCloseWasSell = false;
				_lastCloseVolume = volume;
			}
		}
	}

	private void ResetState()
	{
		if (_trailingActive || _closing || _trailFloor != 0m || _lastCloseWasSell != null || _lastCloseVolume != 0m)
			LogInfo("Trailing state reset.");

		_trailingActive = false;
		_closing = false;
		_trailFloor = 0m;
		_lastCloseWasSell = null;
		_lastCloseVolume = 0m;
	}
}
