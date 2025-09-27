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
/// Mirrors the MetaTrader "Virtual Profit/Loss Trail" expert advisor.
/// The strategy does not generate new entries and instead manages an existing position by
/// applying take-profit, stop-loss, and trailing-stop rules calculated in pip units.
/// </summary>
public class VirtualProfitLossTrailStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _trailingActivationPips;

	private decimal? _currentBid;
	private decimal? _currentAsk;
	private decimal? _longTrailingPrice;
	private decimal? _shortTrailingPrice;

	/// <summary>
	/// Take-profit distance expressed in pip units.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pip units.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance expressed in pip units.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum price improvement (in pips) required before the trail begins to follow the market.
	/// </summary>
	public decimal TrailingActivationPips
	{
		get => _trailingActivationPips.Value;
		set => _trailingActivationPips.Value = value;
	}

	/// <summary>
	/// Additional pip distance required before the trailing stop is adjusted again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="VirtualProfitLossTrailStrategy"/> class.
	/// </summary>
	public VirtualProfitLossTrailStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetNotNegative()
			.SetDisplay("Take-profit (pips)", "Distance for the take-profit level.", "Risk Management");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetNotNegative()
			.SetDisplay("Stop-loss (pips)", "Distance for the stop-loss level.", "Risk Management");

		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
			.SetNotNegative()
			.SetDisplay("Trailing stop (pips)", "Distance between price and the trailing stop.", "Trailing");

		_trailingStepPips = Param(nameof(TrailingStepPips), 1m)
			.SetNotNegative()
			.SetDisplay("Trailing step (pips)", "Extra profit required before the trail moves again.", "Trailing");

		_trailingActivationPips = Param(nameof(TrailingActivationPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing activation (pips)", "Profit that must be locked before trailing starts.", "Trailing");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentBid = null;
		_currentAsk = null;
		_longTrailingPrice = null;
		_shortTrailingPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_currentBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_currentAsk = (decimal)ask;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var pipSize = GetPipSize();

		UpdateLongProtection(pipSize);
		UpdateShortProtection(pipSize);
	}

	private void UpdateLongProtection(decimal pipSize)
	{
		if (Position <= 0m)
		{
			_longTrailingPrice = null;
			return;
		}

		if (_currentBid is not decimal bid || bid <= 0m)
			return;

		var entryPrice = Position.AveragePrice ?? 0m;
		if (entryPrice <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var stopLossOffset = StopLossPips * pipSize;
		var takeProfitOffset = TakeProfitPips * pipSize;
		var trailingDistance = TrailingStopPips * pipSize;
		var trailingStep = TrailingStepPips * pipSize;
		var activationDistance = TrailingActivationPips * pipSize;

		if (stopLossOffset > 0m)
		{
			var stopPrice = entryPrice - stopLossOffset;
			if (bid <= stopPrice)
			{
				SellMarket(volume);
				_longTrailingPrice = null;
				return;
			}
		}

		if (takeProfitOffset > 0m)
		{
			var targetPrice = entryPrice + takeProfitOffset;
			if (bid >= targetPrice)
			{
				SellMarket(volume);
				_longTrailingPrice = null;
				return;
			}
		}

		if (trailingDistance <= 0m)
		{
			_longTrailingPrice = null;
			return;
		}

		var candidate = bid - trailingDistance;
		var requiredPrice = entryPrice + activationDistance;

		if (activationDistance <= 0m || bid >= requiredPrice)
		{
			if (_longTrailingPrice is not decimal current)
			{
				_longTrailingPrice = candidate;
			}
			else
			{
				var shouldAdvance = candidate > current;
				if (trailingStep > 0m)
				shouldAdvance = candidate >= current + trailingStep;

				if (shouldAdvance)
				_longTrailingPrice = candidate;
			}
		}

		if (_longTrailingPrice is decimal trailingPrice && bid <= trailingPrice)
		{
			SellMarket(volume);
			_longTrailingPrice = null;
		}
	}

	private void UpdateShortProtection(decimal pipSize)
	{
		if (Position >= 0m)
		{
			_shortTrailingPrice = null;
			return;
		}

		if (_currentAsk is not decimal ask || ask <= 0m)
			return;

		var entryPrice = Position.AveragePrice ?? 0m;
		if (entryPrice <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var stopLossOffset = StopLossPips * pipSize;
		var takeProfitOffset = TakeProfitPips * pipSize;
		var trailingDistance = TrailingStopPips * pipSize;
		var trailingStep = TrailingStepPips * pipSize;
		var activationDistance = TrailingActivationPips * pipSize;

		if (stopLossOffset > 0m)
		{
			var stopPrice = entryPrice + stopLossOffset;
			if (ask >= stopPrice)
			{
				BuyMarket(volume);
				_shortTrailingPrice = null;
				return;
			}
		}

		if (takeProfitOffset > 0m)
		{
			var targetPrice = entryPrice - takeProfitOffset;
			if (ask <= targetPrice)
			{
				BuyMarket(volume);
				_shortTrailingPrice = null;
				return;
			}
		}

		if (trailingDistance <= 0m)
		{
			_shortTrailingPrice = null;
			return;
		}

		var candidate = ask + trailingDistance;
		var requiredPrice = entryPrice - activationDistance;

		if (activationDistance <= 0m || ask <= requiredPrice)
		{
			if (_shortTrailingPrice is not decimal current)
			{
				_shortTrailingPrice = candidate;
			}
			else
			{
				var shouldAdvance = candidate < current;
				if (trailingStep > 0m)
				shouldAdvance = candidate <= current - trailingStep;

				if (shouldAdvance)
				_shortTrailingPrice = candidate;
			}
		}

		if (_shortTrailingPrice is decimal trailingPrice && ask >= trailingPrice)
		{
			BuyMarket(volume);
			_shortTrailingPrice = null;
		}
	}

	private decimal GetPipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return 1m;

		var decimals = Security?.Decimals ?? 0;
		var multiplier = decimals == 3 || decimals == 5 ? 10m : 1m;
		return priceStep * multiplier;
	}
}

