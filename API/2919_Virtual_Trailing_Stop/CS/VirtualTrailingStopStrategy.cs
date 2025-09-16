using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Virtual trailing stop manager that mirrors MetaTrader style trailing behavior.
/// The strategy does not open new positions and instead manages existing ones by applying stop-loss,
/// take-profit, and trailing-stop rules on top of incoming level1 data.
/// </summary>
public class VirtualTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStartPips;
	private readonly StrategyParam<decimal> _trailingStepPips;

	private decimal? _currentBid;
	private decimal? _currentAsk;
	private decimal? _longTrailingPrice;
	private decimal? _shortTrailingPrice;

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Price advance that must happen before the trailing stop activates.
	/// </summary>
	public decimal TrailingStartPips
	{
		get => _trailingStartPips.Value;
		set => _trailingStartPips.Value = value;
	}

	/// <summary>
	/// Minimum pip distance required before the trailing stop is shifted further.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="VirtualTrailingStopStrategy"/> class.
	/// </summary>
	public VirtualTrailingStopStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop-loss (pips)", "Distance for stop-loss in pip units", "Risk Management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take-profit (pips)", "Distance for take-profit in pip units", "Risk Management");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing stop (pips)", "Trailing stop distance in pip units", "Trailing");

		_trailingStartPips = Param(nameof(TrailingStartPips), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing start (pips)", "Activation distance before trailing engages", "Trailing");

		_trailingStepPips = Param(nameof(TrailingStepPips), 1m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing step (pips)", "Minimal movement required to move the trail", "Trailing");
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

		// Subscribe to level1 updates to monitor bid/ask changes in real time.
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
		if (Position <= 0)
		{
			_longTrailingPrice = null;
			return;
		}

		if (_currentBid is not decimal bid || bid <= 0m)
			return;

		var entryPrice = Position.AveragePrice;
		var stopLossOffset = StopLossPips * pipSize;
		var takeProfitOffset = TakeProfitPips * pipSize;
		var trailingStopOffset = TrailingStopPips * pipSize;
		var trailingStartOffset = TrailingStartPips * pipSize;
		var trailingStepOffset = TrailingStepPips * pipSize;

		if (stopLossOffset > 0m)
		{
			var stopPrice = entryPrice - stopLossOffset;
			if (bid <= stopPrice)
			{
				SellMarket(Math.Abs(Position));
				_longTrailingPrice = null;
				return;
			}
		}

		if (takeProfitOffset > 0m)
		{
			var takePrice = entryPrice + takeProfitOffset;
			if (bid >= takePrice)
			{
				SellMarket(Math.Abs(Position));
				_longTrailingPrice = null;
				return;
			}
		}

		if (trailingStopOffset <= 0m)
		{
			_longTrailingPrice = null;
			return;
		}

		var newTrail = bid - trailingStopOffset;
		var activationPrice = entryPrice + trailingStartOffset;

		if (newTrail >= activationPrice)
		{
			var shouldUpdate = !_longTrailingPrice.HasValue;

			if (_longTrailingPrice.HasValue)
			{
				if (trailingStepOffset <= 0m)
				shouldUpdate = newTrail > _longTrailingPrice.Value;
				else
				shouldUpdate = newTrail >= _longTrailingPrice.Value + trailingStepOffset;
			}

			if (shouldUpdate)
			_longTrailingPrice = newTrail;
		}

		if (_longTrailingPrice.HasValue && bid <= _longTrailingPrice.Value && bid > entryPrice)
		{
			SellMarket(Math.Abs(Position));
			_longTrailingPrice = null;
		}
	}

	private void UpdateShortProtection(decimal pipSize)
	{
		if (Position >= 0)
		{
			_shortTrailingPrice = null;
			return;
		}

		if (_currentAsk is not decimal ask || ask <= 0m)
			return;

		var entryPrice = Position.AveragePrice;
		var stopLossOffset = StopLossPips * pipSize;
		var takeProfitOffset = TakeProfitPips * pipSize;
		var trailingStopOffset = TrailingStopPips * pipSize;
		var trailingStartOffset = TrailingStartPips * pipSize;
		var trailingStepOffset = TrailingStepPips * pipSize;

		if (stopLossOffset > 0m)
		{
			var stopPrice = entryPrice + stopLossOffset;
			if (ask >= stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				_shortTrailingPrice = null;
				return;
			}
		}

		if (takeProfitOffset > 0m)
		{
			var takePrice = entryPrice - takeProfitOffset;
			if (ask <= takePrice)
			{
				BuyMarket(Math.Abs(Position));
				_shortTrailingPrice = null;
				return;
			}
		}

		if (trailingStopOffset <= 0m)
		{
			_shortTrailingPrice = null;
			return;
		}

		var newTrail = ask + trailingStopOffset;
		var activationPrice = entryPrice - trailingStartOffset;

		if (newTrail <= activationPrice)
		{
			var shouldUpdate = !_shortTrailingPrice.HasValue;

			if (_shortTrailingPrice.HasValue)
			{
				if (trailingStepOffset <= 0m)
				shouldUpdate = newTrail < _shortTrailingPrice.Value;
				else
				shouldUpdate = newTrail <= _shortTrailingPrice.Value - trailingStepOffset;
			}

			if (shouldUpdate)
			_shortTrailingPrice = newTrail;
		}

		if (_shortTrailingPrice.HasValue && ask >= _shortTrailingPrice.Value && entryPrice > ask)
		{
			BuyMarket(Math.Abs(Position));
			_shortTrailingPrice = null;
		}
	}

	private decimal GetPipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return 1m;

		var decimals = Security?.Decimals ?? 0;
		var multiplier = (decimals == 3 || decimals == 5) ? 10m : 1m;
		return priceStep * multiplier;
	}
}
