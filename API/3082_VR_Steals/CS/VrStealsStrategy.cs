using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Risk management strategy converted from the original VR---STEALS MQL expert.
/// Closes existing positions based on fixed stop-loss and take-profit distances and
/// applies a step-based trailing stop while monitoring trade prices.
/// </summary>
public class VrStealsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossDistance;
	private readonly StrategyParam<decimal> _takeProfitDistance;
	private readonly StrategyParam<decimal> _trailingStopDistance;
	private readonly StrategyParam<decimal> _trailingStepDistance;

	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private bool _longExitRequested;
	private bool _shortExitRequested;

	/// <summary>
	/// Absolute price distance used to detect a loss for both long and short trades.
	/// </summary>
	public decimal StopLossDistance
	{
		get => _stopLossDistance.Value;
		set => _stopLossDistance.Value = value;
	}

	/// <summary>
	/// Absolute price distance used to detect a profit target for both directions.
	/// </summary>
	public decimal TakeProfitDistance
	{
		get => _takeProfitDistance.Value;
		set => _takeProfitDistance.Value = value;
	}

	/// <summary>
	/// Distance between the current price and the trailing stop that should be preserved.
	/// </summary>
	public decimal TrailingStopDistance
	{
		get => _trailingStopDistance.Value;
		set => _trailingStopDistance.Value = value;
	}

	/// <summary>
	/// Minimal increment required before the trailing stop is moved closer to price.
	/// </summary>
	public decimal TrailingStepDistance
	{
		get => _trailingStepDistance.Value;
		set => _trailingStepDistance.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public VrStealsStrategy()
	{
		_stopLossDistance = Param(nameof(StopLossDistance), 0m)
			.SetDisplay("Stop Loss Distance", "Loss distance in price units", "Risk")
			.SetCanOptimize(true);

		_takeProfitDistance = Param(nameof(TakeProfitDistance), 0m)
			.SetDisplay("Take Profit Distance", "Profit distance in price units", "Risk")
			.SetCanOptimize(true);

		_trailingStopDistance = Param(nameof(TrailingStopDistance), 0m)
			.SetDisplay("Trailing Stop Distance", "Base distance maintained from price", "Risk")
			.SetCanOptimize(true);

		_trailingStepDistance = Param(nameof(TrailingStepDistance), 0m)
			.SetDisplay("Trailing Step Distance", "Minimal advance required to move the trail", "Risk")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Ticks)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopDistance > 0m && TrailingStepDistance <= 0m)
			throw new InvalidOperationException("Trailing step must be positive when the trailing stop is enabled.");

		ResetState();

		SubscribeTrades().Bind(ProcessTrade).Start();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetState();
			return;
		}

		if (Position > 0m && delta > 0m)
		{
			_longTrailingStop = null;
			_shortTrailingStop = null;
			_longExitRequested = false;
			_shortExitRequested = false;
		}
		else if (Position < 0m && delta < 0m)
		{
			_shortTrailingStop = null;
			_longTrailingStop = null;
			_longExitRequested = false;
			_shortExitRequested = false;
		}
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		if (trade.TradePrice is not decimal price)
			return;

		if (Position > 0m)
		{
			ManageLong(price);
		}
		else if (Position < 0m)
		{
			ManageShort(price);
		}
	}

	private void ManageLong(decimal price)
	{
		if (_longExitRequested)
			return;

		var entryPrice = PositionPrice;

		var stopDistance = StopLossDistance;
		if (stopDistance > 0m && entryPrice - price >= stopDistance)
		{
			RequestCloseLong();
			return;
		}

		var takeDistance = TakeProfitDistance;
		if (takeDistance > 0m && price - entryPrice >= takeDistance)
		{
			RequestCloseLong();
			return;
		}

		var trailingDistance = TrailingStopDistance;
		if (trailingDistance <= 0m)
			return;

		var stepDistance = TrailingStepDistance;
		if (price - entryPrice > trailingDistance + stepDistance)
		{
			var desiredStop = price - trailingDistance;

			if (_longTrailingStop is not decimal currentStop || currentStop + stepDistance < desiredStop)
				_longTrailingStop = desiredStop;
		}

		if (_longTrailingStop is decimal trailingStop && price <= trailingStop)
		{
			RequestCloseLong();
		}
	}

	private void ManageShort(decimal price)
	{
		if (_shortExitRequested)
			return;

		var entryPrice = PositionPrice;

		var stopDistance = StopLossDistance;
		if (stopDistance > 0m && price - entryPrice >= stopDistance)
		{
			RequestCloseShort();
			return;
		}

		var takeDistance = TakeProfitDistance;
		if (takeDistance > 0m && entryPrice - price >= takeDistance)
		{
			RequestCloseShort();
			return;
		}

		var trailingDistance = TrailingStopDistance;
		if (trailingDistance <= 0m)
			return;

		var stepDistance = TrailingStepDistance;
		if (entryPrice - price > trailingDistance + stepDistance)
		{
			var desiredStop = price + trailingDistance;

			if (_shortTrailingStop is not decimal currentStop || currentStop - stepDistance > desiredStop)
				_shortTrailingStop = desiredStop;
		}

		if (_shortTrailingStop is decimal trailingStop && price >= trailingStop)
		{
			RequestCloseShort();
		}
	}

	private void RequestCloseLong()
	{
		if (_longExitRequested)
			return;

		_longExitRequested = true;

		var volume = Position;
		if (volume > 0m)
			SellMarket(volume);
	}

	private void RequestCloseShort()
	{
		if (_shortExitRequested)
			return;

		_shortExitRequested = true;

		var volume = -Position;
		if (volume > 0m)
			BuyMarket(volume);
	}

	private void ResetState()
	{
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_longExitRequested = false;
		_shortExitRequested = false;
	}
}
