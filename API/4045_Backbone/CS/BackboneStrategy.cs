using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Backbone strategy that alternates between long and short baskets based on retracements from recent extremes.
/// Translates the original MetaTrader expert logic into the StockSharp high level API with risk-based sizing and trailing stops.
/// </summary>
public class BackboneStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maxRisk;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<DataType> _candleType;

	private readonly HashSet<Order> _pendingEntryOrders = new();

	private decimal? _bestBid;
	private decimal? _bestAsk;

	private decimal _priceStep;
	private decimal _point;
	private decimal _volumeStep;
	private decimal? _minVolume;
	private decimal? _maxVolume;

	private int _lastPositionDirection;
	private decimal _bidMax;
	private decimal _askMin;
	private int _tradeCount;
	private decimal? _entryPrice;

	private Order? _stopOrder;
	private Order? _takeOrder;
	private decimal? _currentStopPrice;
	private decimal? _currentTakePrice;

	public BackboneStrategy()
	{
		_maxRisk = Param(nameof(MaxRisk), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Maximum risk", "Fraction of account equity considered when sizing new entries.", "Risk");

		_maxTrades = Param(nameof(MaxTrades), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max trades", "Maximum number of concurrent trades that may be accumulated in one direction.", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 170m)
			.SetDisplay("Take profit (points)", "Distance in price steps used to place the take-profit order.", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 40m)
			.SetDisplay("Stop loss (points)", "Distance in price steps used to place the protective stop.", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 300m)
			.SetDisplay("Trailing stop (points)", "Trail distance expressed in price steps for stop updates.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Timeframe used to pace trade decisions.", "General");
	}

	/// <summary>
	/// Maximum risk fraction applied when sizing new trades.
	/// </summary>
	public decimal MaxRisk
	{
		get => _maxRisk.Value;
		set => _maxRisk.Value = value;
	}

	/// <summary>
	/// Maximum number of sequential trades allowed in the current basket.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Candle type used to pace decision making.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		// Reset runtime state so a new launch starts from a clean slate.
		_pendingEntryOrders.Clear();
		_bestBid = null;
		_bestAsk = null;
		_priceStep = 0m;
		_point = 0m;
		_volumeStep = 0m;
		_minVolume = null;
		_maxVolume = null;
		_lastPositionDirection = 0;
		_bidMax = 0m;
		_askMin = decimal.MaxValue;
		_tradeCount = 0;
		_entryPrice = null;
		_stopOrder = null;
		_takeOrder = null;
		_currentStopPrice = null;
		_currentTakePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Capture instrument specifics required for price and volume normalisation.
		var security = Security;
		if (security != null)
		{
			_priceStep = security.PriceStep ?? 0m;
			if (_priceStep <= 0m)
			{
				var decimals = security.Decimals ?? 0;
				if (decimals > 0)
					_priceStep = (decimal)Math.Pow(10, -decimals);
			}

			_point = _priceStep > 0m ? _priceStep : 0.0001m;
			_volumeStep = security.VolumeStep ?? 0m;
			_minVolume = security.MinVolume;
			_maxVolume = security.MaxVolume;
		}
		else
		{
			_priceStep = 0m;
			_point = 0.0001m;
			_volumeStep = 0m;
			_minVolume = null;
			_maxVolume = null;
		}

		_bidMax = 0m;
		_askMin = decimal.MaxValue;

		// Drive the strategy from finished candles.
		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
			.Bind(ProcessCandle)
			.Start();

		// Track best bid/ask values for trailing and initialisation logic.
		SubscribeOrderBook()
			.Bind(depth =>
			{
				var bestBid = depth.GetBestBid();
				if (bestBid != null && bestBid.Price > 0m)
					_bestBid = bestBid.Price;

				var bestAsk = depth.GetBestAsk();
				if (bestAsk != null && bestAsk.Price > 0m)
					_bestAsk = bestAsk.Price;
			})
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bid = GetBid(candle);
		var ask = GetAsk(candle, bid);

		// During the very first phase accumulate reference extremes.
		if (_lastPositionDirection == 0)
			EvaluateInitialDirection(bid, ask);

		// Update trailing stop levels based on the latest market data.
		UpdateTrailing(bid, ask);

		// Apply the original alternating entry logic on each completed candle.
		TryOpenPosition();
	}

	private void EvaluateInitialDirection(decimal bid, decimal ask)
	{
		if (bid <= 0m || ask <= 0m)
			return;

		if (bid > _bidMax)
			_bidMax = bid;

		if (ask < _askMin)
			_askMin = ask;

		var threshold = TrailingStopPoints * _point;
		if (bid < _bidMax - threshold)
			_lastPositionDirection = -1;
		else if (ask > _askMin + threshold)
			_lastPositionDirection = 1;
	}

	private void TryOpenPosition()
	{
		if (_pendingEntryOrders.Count > 0)
			return;

		var tradesLimit = MaxTrades;
		if (tradesLimit <= 0)
			return;

		var currentCount = _tradeCount;

		if ((_lastPositionDirection == -1 && currentCount == 0) ||
			(_lastPositionDirection == 1 && currentCount > 0 && currentCount < tradesLimit))
		{
			var volume = CalculateVolume(currentCount);
			if (volume <= 0m)
				return;

			var order = BuyMarket(volume);
			if (order != null)
			{
				_pendingEntryOrders.Add(order);
				_lastPositionDirection = 1;
			}

			return;
		}

		if ((_lastPositionDirection == 1 && currentCount == 0) ||
			(_lastPositionDirection == -1 && currentCount > 0 && currentCount < tradesLimit))
		{
			var volume = CalculateVolume(currentCount);
			if (volume <= 0m)
				return;

			var order = SellMarket(volume);
			if (order != null)
			{
				_pendingEntryOrders.Add(order);
				_lastPositionDirection = -1;
			}
		}
	}

	private decimal CalculateVolume(int openTrades)
	{
		var volume = Volume;
		var security = Security;

		var stopPoints = StopLossPoints;
		var risk = MaxRisk;
		var tradesLimit = MaxTrades;

		if (security != null && Portfolio != null && risk > 0m && tradesLimit > 0 && stopPoints > 0m && _point > 0m)
		{
			var priceStepCost = security.PriceStepCost ?? 0m;

			if (priceStepCost > 0m)
			{
				var denominator = tradesLimit / risk - openTrades;
				if (denominator <= 0m)
					denominator = 1m;

				var fraction = 1m / denominator;
				var accountValue = GetAccountValue();

				if (accountValue > 0m)
				{
					var riskAmount = accountValue * fraction;
					var stopDistance = stopPoints * _point;
					if (stopDistance > 0m)
					{
						var steps = stopDistance / _point;
						var riskPerUnit = steps * priceStepCost;
						if (riskPerUnit > 0m)
							volume = riskAmount / riskPerUnit;
					}
				}
			}
		}

		if (_volumeStep > 0m && volume > 0m)
			volume = Math.Floor(volume / _volumeStep) * _volumeStep;

		if (_maxVolume.HasValue && _maxVolume.Value > 0m && volume > _maxVolume.Value)
			volume = _maxVolume.Value;

		if (_minVolume.HasValue && _minVolume.Value > 0m && volume < _minVolume.Value)
			return 0m;

		return volume;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (_priceStep <= 0m)
			return price;

		var steps = Math.Round(price / _priceStep, MidpointRounding.AwayFromZero);
		return steps * _priceStep;
	}

	private void RefreshProtection(bool isLong)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		{
			ResetProtection();
			return;
		}

		var entryPrice = PositionPrice ?? _entryPrice;
		if (entryPrice is null or <= 0m)
		{
			entryPrice = _bestBid ?? _bestAsk ?? 0m;
			if (entryPrice <= 0m)
				return;
		}

		_entryPrice = entryPrice;

		var stopDistance = StopLossPoints * _point;
		if (StopLossPoints > 0m && stopDistance > 0m)
		{
			var stopPrice = isLong ? entryPrice.Value - stopDistance : entryPrice.Value + stopDistance;
			stopPrice = NormalizePrice(stopPrice);

			if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
				CancelOrder(_stopOrder);

			_stopOrder = isLong ? SellStop(volume, stopPrice) : BuyStop(volume, stopPrice);
			_currentStopPrice = stopPrice;
		}
		else
		{
			if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
				CancelOrder(_stopOrder);

			_stopOrder = null;
			_currentStopPrice = null;
		}

		var takeDistance = TakeProfitPoints * _point;
		if (TakeProfitPoints > 0m && takeDistance > 0m)
		{
			var takePrice = isLong ? entryPrice.Value + takeDistance : entryPrice.Value - takeDistance;
			takePrice = NormalizePrice(takePrice);

			if (_takeOrder != null && _takeOrder.State == OrderStates.Active)
				CancelOrder(_takeOrder);

			_takeOrder = isLong ? SellLimit(volume, takePrice) : BuyLimit(volume, takePrice);
			_currentTakePrice = takePrice;
		}
		else
		{
			if (_takeOrder != null && _takeOrder.State == OrderStates.Active)
				CancelOrder(_takeOrder);

			_takeOrder = null;
			_currentTakePrice = null;
		}
	}

	private void UpdateTrailing(decimal bid, decimal ask)
	{
		if (TrailingStopPoints <= 0m || StopLossPoints <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m || _entryPrice is null)
			return;

		var distance = TrailingStopPoints * _point;
		if (distance <= 0m)
			return;

		if (Position > 0m)
		{
			var reference = bid > 0m ? bid : ask;
			if (reference <= 0m)
				return;

			if (reference - _entryPrice.Value <= distance)
				return;

			var newStop = NormalizePrice(reference - distance);
			if (_currentStopPrice is decimal current && current >= newStop)
				return;

			UpdateStopOrder(true, newStop, volume);
		}
		else if (Position < 0m)
		{
			var reference = ask > 0m ? ask : bid;
			if (reference <= 0m)
				return;

			if (_entryPrice.Value - reference <= distance)
				return;

			var newStop = NormalizePrice(reference + distance);
			if (_currentStopPrice is decimal current && current <= newStop && current != 0m)
				return;

			UpdateStopOrder(false, newStop, volume);
		}
	}

	private void UpdateStopOrder(bool isLong, decimal newStopPrice, decimal volume)
	{
		if (newStopPrice <= 0m || volume <= 0m)
			return;

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
		{
			if (_currentStopPrice is decimal current && current == newStopPrice && _stopOrder.Volume == volume)
				return;

			CancelOrder(_stopOrder);
		}

		_stopOrder = isLong ? SellStop(volume, newStopPrice) : BuyStop(volume, newStopPrice);
		_currentStopPrice = newStopPrice;
	}

	private void ResetProtection()
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		if (_takeOrder != null && _takeOrder.State == OrderStates.Active)
			CancelOrder(_takeOrder);

		_stopOrder = null;
		_takeOrder = null;
		_currentStopPrice = null;
		_currentTakePrice = null;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order is not Order order)
			return;

		if (_pendingEntryOrders.Remove(order))
		{
			_tradeCount = _tradeCount <= 0 ? 1 : _tradeCount + 1;
			var isLong = order.Direction == Sides.Buy;
			_entryPrice = PositionPrice ?? trade.Trade.Price;
			RefreshProtection(isLong);
			return;
		}

		if (_stopOrder != null && order == _stopOrder)
		{
			_stopOrder = null;
			_currentStopPrice = null;
			_entryPrice = null;
			_tradeCount = 0;

			if (_takeOrder != null && _takeOrder.State == OrderStates.Active)
				CancelOrder(_takeOrder);

			_takeOrder = null;
			_currentTakePrice = null;
		}
		else if (_takeOrder != null && order == _takeOrder)
		{
			_takeOrder = null;
			_currentTakePrice = null;
			_entryPrice = null;
			_tradeCount = 0;

			if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
				CancelOrder(_stopOrder);

			_stopOrder = null;
			_currentStopPrice = null;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_tradeCount = 0;
			_entryPrice = null;
			ResetProtection();
		}
		else
		{
			_entryPrice = PositionPrice ?? _entryPrice;
		}
	}

	/// <inheritdoc />
	protected override void OnOrderRegisterFailed(OrderFail fail)
	{
		base.OnOrderRegisterFailed(fail);

		if (fail.Order != null)
		{
			_pendingEntryOrders.Remove(fail.Order);

			if (_stopOrder == fail.Order)
			{
				_stopOrder = null;
				_currentStopPrice = null;
			}

			if (_takeOrder == fail.Order)
			{
				_takeOrder = null;
				_currentTakePrice = null;
			}
		}
	}

	private decimal GetBid(ICandleMessage candle)
	{
		if (_bestBid is decimal bid && bid > 0m)
			return bid;

		var securityBid = Security?.BestBid?.Price;
		if (securityBid > 0m)
			return securityBid.Value;

		if (candle.ClosePrice > 0m)
			return candle.ClosePrice;

		return 0m;
	}

	private decimal GetAsk(ICandleMessage candle, decimal bid)
	{
		if (_bestAsk is decimal ask && ask > 0m)
			return ask;

		var securityAsk = Security?.BestAsk?.Price;
		if (securityAsk > 0m)
			return securityAsk.Value;

		if (bid > 0m && _priceStep > 0m)
			return bid + _priceStep;

		if (candle.ClosePrice > 0m)
			return candle.ClosePrice;

		return bid;
	}

	private decimal GetAccountValue()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return 0m;

		if (portfolio.CurrentValue > 0m)
			return portfolio.CurrentValue;

		if (portfolio.CurrentBalance > 0m)
			return portfolio.CurrentBalance;

		if (portfolio.BeginBalance > 0m)
			return portfolio.BeginBalance;

		return 0m;
	}
}
