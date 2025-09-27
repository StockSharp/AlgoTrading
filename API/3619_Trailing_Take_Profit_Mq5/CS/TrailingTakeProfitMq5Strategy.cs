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
/// Converts the MetaTrader trailing take profit helper into a StockSharp strategy.
/// </summary>
public class TrailingTakeProfitMq5Strategy : Strategy
{
	private readonly StrategyParam<bool> _trailingEnabled;
	private readonly StrategyParam<decimal> _trailingStartPoints;
	private readonly StrategyParam<decimal> _trailingDistancePoints;

	private decimal? _lastBid;
	private decimal? _lastAsk;
	private decimal? _longTrailingTarget;
	private decimal? _shortTrailingTarget;

	/// <summary>
	/// Enables the trailing take profit logic.
	/// </summary>
	public bool TrailingEnabled
	{
		get => _trailingEnabled.Value;
		set => _trailingEnabled.Value = value;
	}

	/// <summary>
	/// Distance in price steps that price should move against the position before trailing activates.
	/// </summary>
	public decimal TrailingStartPoints
	{
		get => _trailingStartPoints.Value;
		set => _trailingStartPoints.Value = value;
	}

	/// <summary>
	/// Maximum distance in price steps maintained between the current price and the take profit level.
	/// </summary>
	public decimal TrailingDistancePoints
	{
		get => _trailingDistancePoints.Value;
		set => _trailingDistancePoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TrailingTakeProfitMq5Strategy"/>.
	/// </summary>
	public TrailingTakeProfitMq5Strategy()
	{
		_trailingEnabled = Param(nameof(TrailingEnabled), true)
			.SetDisplay("Use Trailing", "Enable trailing take profit logic", "General");

		_trailingStartPoints = Param(nameof(TrailingStartPoints), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Start (points)", "Price adverse move required to activate trailing", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(50m, 500m, 50m);

		_trailingDistancePoints = Param(nameof(TrailingDistancePoints), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Distance (points)", "Maximum gap between price and take profit", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(50m, 500m, 50m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			yield break;

		yield return (Security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastBid = null;
		_lastAsk = null;
		_longTrailingTarget = null;
		_shortTrailingTarget = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetTrailingTargets();
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue is decimal bid)
			_lastBid = bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue is decimal ask)
			_lastAsk = ask;

		EvaluateTrailing();
	}

	private void EvaluateTrailing()
	{
		if (!TrailingEnabled)
		{
			ResetTrailingTargets();
			return;
		}

		var priceStep = Security?.PriceStep;
		if (priceStep is null || priceStep.Value <= 0m)
		return;

		var activationDistance = TrailingStartPoints * priceStep.Value;
		var trailingDistance = TrailingDistancePoints * priceStep.Value;

		if (activationDistance <= 0m || trailingDistance <= 0m)
		return;

		var position = Position;

		if (position > 0m)
		{
		ProcessLongTrailing(activationDistance, trailingDistance);
		}
		else if (position < 0m)
		{
		ProcessShortTrailing(activationDistance, trailingDistance);
		}
		else
		{
		ResetTrailingTargets();
		}
	}

	private void ProcessLongTrailing(decimal activationDistance, decimal trailingDistance)
	{
		if (!_lastBid.HasValue && !_lastAsk.HasValue)
		return;

		var referencePrice = _lastBid ?? _lastAsk;
		if (referencePrice is null)
		return;

		var entryPrice = PositionAvgPrice;
		if (entryPrice <= 0m)
		return;

		var distanceFromEntry = entryPrice - referencePrice.Value;

		if (distanceFromEntry > activationDistance)
		{
		var candidateTake = referencePrice.Value + trailingDistance;

		if (!_longTrailingTarget.HasValue || _longTrailingTarget.Value > candidateTake)
		{
		_longTrailingTarget = candidateTake;
		LogInfo($"Long trailing take profit updated to {candidateTake:F5}.");
		}
		}

		if (_longTrailingTarget.HasValue && referencePrice.Value >= _longTrailingTarget.Value)
		{
		var volume = Position;
		if (volume > 0m)
		{
		LogInfo($"Long trailing take profit triggered at {referencePrice.Value:F5}. Closing position.");
		SellMarket(volume);
		}

		_longTrailingTarget = null;
		}
	}

	private void ProcessShortTrailing(decimal activationDistance, decimal trailingDistance)
	{
		if (!_lastAsk.HasValue && !_lastBid.HasValue)
		return;

		var referencePrice = _lastAsk ?? _lastBid;
		if (referencePrice is null)
		return;

		var entryPrice = PositionAvgPrice;
		if (entryPrice <= 0m)
		return;

		var distanceFromEntry = referencePrice.Value - entryPrice;

		if (distanceFromEntry > activationDistance)
		{
		var candidateTake = referencePrice.Value - trailingDistance;

		if (!_shortTrailingTarget.HasValue || _shortTrailingTarget.Value < candidateTake)
		{
		_shortTrailingTarget = candidateTake;
		LogInfo($"Short trailing take profit updated to {candidateTake:F5}.");
		}
		}

		if (_shortTrailingTarget.HasValue && referencePrice.Value <= _shortTrailingTarget.Value)
		{
		var volume = Math.Abs(Position);
		if (volume > 0m)
		{
		LogInfo($"Short trailing take profit triggered at {referencePrice.Value:F5}. Closing position.");
		BuyMarket(volume);
		}

		_shortTrailingTarget = null;
		}
	}

	private void ResetTrailingTargets()
	{
		_longTrailingTarget = null;
		_shortTrailingTarget = null;
	}
}

