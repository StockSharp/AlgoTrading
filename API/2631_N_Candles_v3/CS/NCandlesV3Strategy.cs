using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that opens positions after detecting a sequence of candles with the same direction.
/// Adds fixed take profit, stop loss and stepped trailing stop management.
/// </summary>
public class NCandlesV3Strategy : Strategy
{
	private readonly StrategyParam<int> _identicalCandles;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private int _sequenceDirection;
	private int _sequenceCount;

	private decimal _longPositionVolume;
	private decimal _shortPositionVolume;
	private decimal _longAvgPrice;
	private decimal _shortAvgPrice;

	private bool _longTrailingActive;
	private bool _shortTrailingActive;
	private decimal _longTrailingPrice;
	private decimal _shortTrailingPrice;

	private Order? _stopOrder;
	private Order? _takeProfitOrder;

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of consecutive candles required before entering a trade.
	/// </summary>
	public int IdenticalCandles
	{
		get => _identicalCandles.Value;
		set => _identicalCandles.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimal advance in price steps before the trailing stop is moved again.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Maximum number of entries allowed in the same direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Volume used for every new entry.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public NCandlesV3Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles analysed by the strategy", "General");

		_identicalCandles = Param(nameof(IdenticalCandles), 3)
			.SetRange(1, 10)
			.SetDisplay("Identical Candles", "Required number of equal candles", "Pattern")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetRange(0m, 500m)
			.SetDisplay("Take Profit Points", "Take profit distance in price steps", "Risk Management")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetRange(0m, 500m)
			.SetDisplay("Stop Loss Points", "Stop loss distance in price steps", "Risk Management")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 10m)
			.SetRange(0m, 500m)
			.SetDisplay("Trailing Stop Points", "Trailing stop distance in price steps", "Trailing")
			.SetCanOptimize(true);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 4m)
			.SetRange(0m, 200m)
			.SetDisplay("Trailing Step Points", "Extra distance required to move the trailing stop", "Trailing")
			.SetCanOptimize(true);

		_maxPositions = Param(nameof(MaxPositions), 2)
			.SetRange(1, 10)
			.SetDisplay("Max Positions", "Maximum simultaneous entries per direction", "Trading")
			.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume for every entry", "Trading");
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

		_sequenceDirection = 0;
		_sequenceCount = 0;

		_longPositionVolume = 0m;
		_shortPositionVolume = 0m;
		_longAvgPrice = 0m;
		_shortAvgPrice = 0m;

		_longTrailingActive = false;
		_shortTrailingActive = false;
		_longTrailingPrice = 0m;
		_shortTrailingPrice = 0m;

		CancelProtectionOrders();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update trailing logic before searching for new entries.
		UpdateTrailing(candle);

		var direction = GetDirection(candle);
		UpdateSequence(direction);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_sequenceCount < IdenticalCandles)
			return;

		if (_sequenceDirection > 0)
			TryOpenLong();
		else if (_sequenceDirection < 0)
			TryOpenShort();
	}

	private static int GetDirection(ICandleMessage candle)
	{
		if (candle.ClosePrice > candle.OpenPrice)
			return 1;
		if (candle.ClosePrice < candle.OpenPrice)
			return -1;
		return 0;
	}

	private void UpdateSequence(int direction)
	{
		// Reset streak when a doji candle appears.
		if (direction == 0)
		{
			_sequenceDirection = 0;
			_sequenceCount = 0;
			return;
		}

		if (_sequenceDirection == direction)
		{
			_sequenceCount++;
		}
		else
		{
			_sequenceDirection = direction;
			_sequenceCount = 1;
		}
	}

	private void TryOpenLong()
	{
		var perEntry = Volume;
		if (perEntry <= 0m)
			return;

		var maxVolume = perEntry * MaxPositions;
		var additionalVolume = maxVolume > _longPositionVolume
			? Math.Min(perEntry, maxVolume - _longPositionVolume)
			: 0m;

		var totalVolume = _shortPositionVolume + additionalVolume;
		if (totalVolume <= 0m)
			return;

		BuyMarket(totalVolume);
	}

	private void TryOpenShort()
	{
		var perEntry = Volume;
		if (perEntry <= 0m)
			return;

		var maxVolume = perEntry * MaxPositions;
		var additionalVolume = maxVolume > _shortPositionVolume
			? Math.Min(perEntry, maxVolume - _shortPositionVolume)
			: 0m;

		var totalVolume = _longPositionVolume + additionalVolume;
		if (totalVolume <= 0m)
			return;

		SellMarket(totalVolume);
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
			return;

		var tradeVolume = trade.Trade.Volume;
		var tradePrice = trade.Trade.Price;

		if (trade.Order.Side == Sides.Buy)
		{
			var closingShort = Math.Min(tradeVolume, _shortPositionVolume);
			if (closingShort > 0m)
			{
				_shortPositionVolume -= closingShort;
				tradeVolume -= closingShort;

				if (_shortPositionVolume <= 0m)
				{
					_shortPositionVolume = 0m;
					_shortAvgPrice = 0m;
					_shortTrailingActive = false;
					_shortTrailingPrice = 0m;
				}
			}

			if (tradeVolume > 0m)
			{
				_longAvgPrice = _longPositionVolume > 0m
					? (_longAvgPrice * _longPositionVolume + tradePrice * tradeVolume) / (_longPositionVolume + tradeVolume)
					: tradePrice;

				_longPositionVolume += tradeVolume;
				_longTrailingActive = false;
				_longTrailingPrice = 0m;
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			var closingLong = Math.Min(tradeVolume, _longPositionVolume);
			if (closingLong > 0m)
			{
				_longPositionVolume -= closingLong;
				tradeVolume -= closingLong;

				if (_longPositionVolume <= 0m)
				{
					_longPositionVolume = 0m;
					_longAvgPrice = 0m;
					_longTrailingActive = false;
					_longTrailingPrice = 0m;
				}
			}

			if (tradeVolume > 0m)
			{
				_shortAvgPrice = _shortPositionVolume > 0m
					? (_shortAvgPrice * _shortPositionVolume + tradePrice * tradeVolume) / (_shortPositionVolume + tradeVolume)
					: tradePrice;

				_shortPositionVolume += tradeVolume;
				_shortTrailingActive = false;
				_shortTrailingPrice = 0m;
			}
		}

		UpdateProtectionOrders();
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0m)
			return;

		var step = GetPriceStep();
		var trailingDistance = TrailingStopPoints * step;
		var trailingStep = TrailingStepPoints * step;

		if (_longPositionVolume > 0m && Position > 0m && _longAvgPrice > 0m)
		{
			var candidate = candle.ClosePrice - trailingDistance;

			if (!_longTrailingActive)
			{
				if (candidate > _longAvgPrice)
				{
					_longTrailingActive = true;
					_longTrailingPrice = _longAvgPrice;
					UpdateStopOrder(true, _longTrailingPrice, _longPositionVolume);
				}
			}
			else if (candidate - trailingStep > _longTrailingPrice)
			{
				_longTrailingPrice = candidate;
				UpdateStopOrder(true, _longTrailingPrice, _longPositionVolume);
			}
		}

		if (_shortPositionVolume > 0m && Position < 0m && _shortAvgPrice > 0m)
		{
			var candidate = candle.ClosePrice + trailingDistance;

			if (!_shortTrailingActive)
			{
				if (candidate < _shortAvgPrice)
				{
					_shortTrailingActive = true;
					_shortTrailingPrice = _shortAvgPrice;
					UpdateStopOrder(false, _shortTrailingPrice, _shortPositionVolume);
				}
			}
			else if (candidate + trailingStep < _shortTrailingPrice)
			{
				_shortTrailingPrice = candidate;
				UpdateStopOrder(false, _shortTrailingPrice, _shortPositionVolume);
			}
		}
	}

	private void UpdateProtectionOrders()
	{
		if (_longPositionVolume > 0m && Position > 0m)
		{
			var step = GetPriceStep();
			var stopDistance = StopLossPoints * step;
			var takeDistance = TakeProfitPoints * step;

			var stopPrice = stopDistance > 0m ? _longAvgPrice - stopDistance : (decimal?)null;
			var takePrice = takeDistance > 0m ? _longAvgPrice + takeDistance : (decimal?)null;

			UpdateStopOrder(true, stopPrice, _longPositionVolume);
			UpdateTakeProfitOrder(true, takePrice, _longPositionVolume);
		}
		else if (_shortPositionVolume > 0m && Position < 0m)
		{
			var step = GetPriceStep();
			var stopDistance = StopLossPoints * step;
			var takeDistance = TakeProfitPoints * step;

			var stopPrice = stopDistance > 0m ? _shortAvgPrice + stopDistance : (decimal?)null;
			var takePrice = takeDistance > 0m ? _shortAvgPrice - takeDistance : (decimal?)null;

			UpdateStopOrder(false, stopPrice, _shortPositionVolume);
			UpdateTakeProfitOrder(false, takePrice, _shortPositionVolume);
		}
		else
		{
			CancelProtectionOrders();
		}
	}

	private void UpdateStopOrder(bool isLong, decimal? price, decimal volume)
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		_stopOrder = null;

		if (price == null || volume <= 0m)
			return;

		_stopOrder = isLong
			? SellStop(volume, price.Value)
			: BuyStop(volume, price.Value);
	}

	private void UpdateTakeProfitOrder(bool isLong, decimal? price, decimal volume)
	{
		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);

		_takeProfitOrder = null;

		if (price == null || volume <= 0m)
			return;

		_takeProfitOrder = isLong
			? SellLimit(volume, price.Value)
			: BuyLimit(volume, price.Value);
	}

	private void CancelProtectionOrders()
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);
		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);

		_stopOrder = null;
		_takeProfitOrder = null;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep;
		return step != null && step > 0m ? step.Value : 1m;
	}
}
