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
/// Implements the "Trailing Star" trailing stop that starts from a fixed activation price.
/// </summary>
public class TrailingStarPriceStrategy : Strategy
{
	private readonly StrategyParam<decimal> _entryPrice;
	private readonly StrategyParam<decimal> _trailingPointPips;

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal _pointValue;

	private Order _stopOrder;

	/// <summary>
	/// Initializes a new instance of the <see cref="TrailingStarPriceStrategy"/> class.
	/// </summary>
	public TrailingStarPriceStrategy()
	{
		_entryPrice = Param(nameof(EntryPrice), 0m)
			.SetGreaterThanZero()
			.SetDisplay("Activation price", "Price level that must be reached before the trailing stop starts.", "Risk")
			.SetCanOptimize(true);

		_trailingPointPips = Param(nameof(TrailingPointPips), 15m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing distance (pips)", "Distance between price and stop order expressed in MetaTrader points.", "Risk")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Price level that activates the trailing stop.
	/// </summary>
	public decimal EntryPrice
	{
		get => _entryPrice.Value;
		set => _entryPrice.Value = value;
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
			CancelStopOrder();
		}

		TryUpdateTrailingStop();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

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

		var triggerPrice = EntryPrice;
		if (triggerPrice <= 0m)
			return;

		var activated = isLong
			? bestPrice.Value > triggerPrice
			: bestPrice.Value < triggerPrice;

		if (!activated)
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

