using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implements the "Trailing Star" trailing stop logic based on MetaTrader points.
/// </summary>
public class TrailingStarPointStrategy : Strategy
{
	private readonly StrategyParam<decimal> _entryPointPips;
	private readonly StrategyParam<decimal> _trailingPointPips;

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal _pointValue;

	private Order _stopOrder;

	private decimal? _longEntryPrice;
	private decimal _longVolume;
	private decimal? _shortEntryPrice;
	private decimal _shortVolume;

	/// <summary>
	/// Initializes a new instance of the <see cref="TrailingStarPointStrategy"/> class.
	/// </summary>
	public TrailingStarPointStrategy()
	{
		_entryPointPips = Param(nameof(EntryPointPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Entry point (pips)", "Minimum profit in MetaTrader points before the trailing stop activates.", "Risk")
			.SetCanOptimize(true);

		_trailingPointPips = Param(nameof(TrailingPointPips), 15m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing distance (pips)", "Distance between price and stop order expressed in MetaTrader points.", "Risk")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Profit threshold expressed in MetaTrader points.
	/// </summary>
	public decimal EntryPointPips
	{
		get => _entryPointPips.Value;
		set => _entryPointPips.Value = value;
	}

	/// <summary>
	/// Trailing distance expressed in MetaTrader points.
	/// </summary>
	public decimal TrailingPointPips
	{
		get => _trailingPointPips.Value;
		set => _trailingPointPips.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Level1)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBid = null;
		_bestAsk = null;
		_pointValue = 0m;

		CancelStopOrder();

		ResetEntryTracking();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = CalculatePointValue();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		CancelStopOrder();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetEntryTracking();
			CancelStopOrder();
		}

		TryUpdateTrailingStop();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		RegisterTrade(trade);
		TryUpdateTrailingStop();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
			_bestBid = bid;

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
			_bestAsk = ask;

		TryUpdateTrailingStop();
	}

	private void RegisterTrade(MyTrade trade)
	{
		if (trade.Trade == null)
			return;

		var order = trade.Order;
		if (order?.Direction == null)
			return;

		var tradePrice = trade.Trade.Price ?? order.Price;
		if (tradePrice == null)
			return;

		var tradeVolume = trade.Trade.Volume;
		if (tradeVolume == null || tradeVolume.Value <= 0m)
			return;

		var volume = Math.Abs(tradeVolume.Value);

		if (order.Direction == Sides.Buy)
		{
			// Buy fills reduce any short exposure first.
			if (_shortVolume > 0m)
			{
				var reduced = Math.Min(_shortVolume, volume);
				_shortVolume -= reduced;

				if (_shortVolume <= 0m)
					_shortEntryPrice = null;

				volume -= reduced;
			}

			if (volume > 0m)
			{
				var previousVolume = _longVolume;
				var previousPrice = _longEntryPrice ?? tradePrice.Value;
				var totalVolume = previousVolume + volume;

				_longEntryPrice = ((previousPrice * previousVolume) + (tradePrice.Value * volume)) / totalVolume;
				_longVolume = totalVolume;
			}
		}
		else if (order.Direction == Sides.Sell)
		{
			// Sell fills reduce any long exposure first.
			if (_longVolume > 0m)
			{
				var reduced = Math.Min(_longVolume, volume);
				_longVolume -= reduced;

				if (_longVolume <= 0m)
					_longEntryPrice = null;

				volume -= reduced;
			}

			if (volume > 0m)
			{
				var previousVolume = _shortVolume;
				var previousPrice = _shortEntryPrice ?? tradePrice.Value;
				var totalVolume = previousVolume + volume;

				_shortEntryPrice = ((previousPrice * previousVolume) + (tradePrice.Value * volume)) / totalVolume;
				_shortVolume = totalVolume;
			}
		}
	}

	private void TryUpdateTrailingStop()
	{
		if (ProcessState != ProcessStates.Started)
			return;

		var position = Position;
		if (position == 0m)
			return;

		var isLong = position > 0m;
		var bestPrice = isLong ? _bestBid : _bestAsk;
		if (bestPrice == null)
			return;

		var entryPrice = isLong ? _longEntryPrice : _shortEntryPrice;
		if (entryPrice == null)
			return;

		var entryThreshold = EntryPointPips * _pointValue;
		if (entryThreshold <= 0m)
			return;

		var profitDistance = Math.Abs(bestPrice.Value - entryPrice.Value);
		if (profitDistance <= entryThreshold)
			return;

		var trailingDistance = TrailingPointPips * _pointValue;
		if (trailingDistance <= 0m)
			return;

		var targetPrice = isLong
			? bestPrice.Value - trailingDistance
			: bestPrice.Value + trailingDistance;

		if (isLong && _stopOrder != null && _stopOrder.Price >= targetPrice)
			return;

		if (!isLong && _stopOrder != null && _stopOrder.Price <= targetPrice)
			return;

		var normalizedVolume = NormalizeVolume(Math.Abs(position));
		if (normalizedVolume <= 0m)
			return;

		UpdateStopOrder(targetPrice, normalizedVolume, isLong);
	}

	private void UpdateStopOrder(decimal targetPrice, decimal volume, bool isLong)
	{
		var normalizedPrice = NormalizePrice(targetPrice);
		if (normalizedPrice <= 0m)
			return;

		if (_stopOrder == null)
		{
			_stopOrder = isLong
				? SellStop(volume, normalizedPrice)
				: BuyStop(volume, normalizedPrice);
			return;
		}

		if (_stopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			_stopOrder = null;
			UpdateStopOrder(targetPrice, volume, isLong);
			return;
		}

		if (_stopOrder.Price != normalizedPrice || _stopOrder.Volume != volume)
			ReRegisterOrder(_stopOrder, normalizedPrice, volume);
	}

	private void CancelStopOrder()
	{
		if (_stopOrder == null)
			return;

		CancelOrder(_stopOrder);
		_stopOrder = null;
	}

	private void ResetEntryTracking()
	{
		_longEntryPrice = null;
		_longVolume = 0m;
		_shortEntryPrice = null;
		_shortVolume = 0m;
	}

	private decimal CalculatePointValue()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value > 0)
				step = (decimal)Math.Pow(10, -decimals.Value);
		}

		if (step <= 0m)
			step = 0.0001m;

		var multiplier = 1m;
		var digits = security.Decimals;
		if (digits != null && (digits.Value == 3 || digits.Value == 5))
			multiplier = 10m;

		return step * multiplier;
	}
}
