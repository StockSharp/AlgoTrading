using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manages open positions by checking elapsed time and profit distance in pips.
/// </summary>
public class NSecondsNPointsStrategy : Strategy
{
	private readonly StrategyParam<int> _waitSeconds;
	private readonly StrategyParam<int> _takeProfitPips;

	private decimal _pipSize;
	private decimal _averagePrice;
	private decimal _positionVolume;
	private DateTimeOffset? _entryTime;
	private Order _takeProfitOrder;
	private decimal _lastPrice;
	private bool _hasLastPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="NSecondsNPointsStrategy"/> class.
	/// </summary>
	public NSecondsNPointsStrategy()
	{
		_waitSeconds = Param(nameof(WaitSeconds), 40)
			.SetDisplay("Wait Seconds", "Number of seconds before managing the position", "Risk Management")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 15)
			.SetDisplay("Take Profit (pips)", "Profit distance in pips before closing or protecting", "Risk Management")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Waiting time in seconds before the strategy begins to manage a position.
	/// </summary>
	public int WaitSeconds
	{
		get => _waitSeconds.Value;
		set => _waitSeconds.Value = value;
	}

	/// <summary>
	/// Profit target expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Ticks)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_averagePrice = 0m;
		_positionVolume = 0m;
		_entryTime = null;
		_takeProfitOrder = null;
		_lastPrice = 0m;
		_hasLastPrice = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		// Subscribe to real-time trades to maintain the most recent market price.
		SubscribeTrades()
			.Bind(ProcessTrade)
			.Start();

		// Evaluate the position once per second to emulate the MetaTrader timer.
		Timer.Start(TimeSpan.FromSeconds(1), ProcessTimer);
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep;
		if (priceStep == null || priceStep == 0m)
			return 0.0001m;

		var digits = Security?.Decimals ?? 0;
		var adjust = (digits == 3 || digits == 5) ? 10m : 1m;
		return priceStep.Value * adjust;
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice;
		if (price == null)
			return;

		_lastPrice = price.Value;
		_hasLastPrice = true;
	}

	private void ProcessTimer()
	{
		if (_positionVolume == 0 || !_entryTime.HasValue || !_hasLastPrice)
			return;

		var elapsed = CurrentTime - _entryTime.Value;
		// Skip any checks until the configured waiting time has passed since entry.
		if (elapsed < TimeSpan.FromSeconds(Math.Max(1, WaitSeconds)))
			return;

		var takeOffset = TakeProfitPips * _pipSize;
		if (takeOffset <= 0m)
			return;

		var isLong = _positionVolume > 0;
		var currentPrice = _lastPrice;
		var priceDiff = isLong ? currentPrice - _averagePrice : _averagePrice - currentPrice;

		// Close the position immediately once the profit distance has been reached.
		if (priceDiff >= takeOffset)
		{
			if (isLong)
				SellMarket(Position);
			else
				BuyMarket(-Position);

			CancelTakeProfitOrder();
			return;
		}

		var targetPrice = isLong ? _averagePrice + takeOffset : _averagePrice - takeOffset;
		var buffer = _pipSize;

		// Otherwise ensure a take-profit order is resting at the calculated distance.
		if (isLong)
		{
			if (targetPrice - buffer > currentPrice)
				EnsureTakeProfitOrder(isLong, targetPrice);
			else
				CancelTakeProfitOrder();
		}
		else
		{
			if (targetPrice + buffer < currentPrice)
				EnsureTakeProfitOrder(isLong, targetPrice);
			else
				CancelTakeProfitOrder();
		}
	}

	private void EnsureTakeProfitOrder(bool isLong, decimal targetPrice)
	{
		var volume = Math.Abs(Position);
		// Match the take-profit order volume with the current position size.
		if (volume == 0m)
		{
			CancelTakeProfitOrder();
			return;
		}

		targetPrice = NormalizePrice(targetPrice);
		// Align the limit price with the security price step.

		if (_takeProfitOrder != null)
		{
			// Replace the existing order if either price or volume has changed.
			if (_takeProfitOrder.State == OrderStates.Active && _takeProfitOrder.Price == targetPrice && _takeProfitOrder.Volume == volume)
				return;

			if (_takeProfitOrder.State == OrderStates.Active)
				CancelOrder(_takeProfitOrder);

			_takeProfitOrder = null;
		}

		_takeProfitOrder = isLong
			? SellLimit(volume, targetPrice)
			: BuyLimit(volume, targetPrice);
	}

	private void CancelTakeProfitOrder()
	{
		if (_takeProfitOrder == null)
			return;

		if (_takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);

		_takeProfitOrder = null;
	}

	private decimal NormalizePrice(decimal price)
	{
		var priceStep = Security?.PriceStep;
		if (priceStep == null || priceStep == 0m)
			return price;

		return Math.Round(price / priceStep.Value, MidpointRounding.AwayFromZero) * priceStep.Value;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
			return;

		var tradePrice = trade.Trade.Price;
		var tradeVolume = trade.Trade.Volume;
		var tradeTime = trade.Trade.Time;
		var isBuy = trade.Order.Side == Sides.Buy;
		var signedVolume = isBuy ? tradeVolume : -tradeVolume;
		var prevVolume = _positionVolume;
		var newVolume = prevVolume + signedVolume;

		// Store the latest fill information to track entry price and entry time.
		if (prevVolume == 0m || Math.Sign(newVolume) != Math.Sign(prevVolume))
		{
			_averagePrice = tradePrice;
			_entryTime = tradeTime;
		}
		else if (Math.Sign(newVolume) == Math.Sign(prevVolume))
		{
			if ((isBuy && newVolume > 0m) || (!isBuy && newVolume < 0m))
			{
				var absPrev = Math.Abs(prevVolume);
				var absNew = Math.Abs(newVolume);
				_averagePrice = (_averagePrice * absPrev + tradePrice * tradeVolume) / absNew;
			}
		}

		_positionVolume = newVolume;

		if (_positionVolume == 0m)
		{
			_averagePrice = 0m;
			_entryTime = null;
			CancelTakeProfitOrder();
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0m)
			return;

		CancelTakeProfitOrder();
	}
}
