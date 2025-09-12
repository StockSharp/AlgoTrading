using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple dollar cost averaging strategy.
/// </summary>
public class SimpleDcaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _baseOrderSize;
	private readonly StrategyParam<decimal> _priceDeviation;
	private readonly StrategyParam<int> _maxSafetyOrders;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _orderSizeMultiplier;

	private decimal _lastEntryPrice;
	private int _safetyOrderCount;
	private decimal _totalQuantity;
	private decimal _totalCost;
	private decimal _averageEntryPrice;

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base order size in quote currency.
	/// </summary>
	public decimal BaseOrderSize
	{
		get => _baseOrderSize.Value;
		set => _baseOrderSize.Value = value;
	}

	/// <summary>
	/// Price deviation for safety orders in percent.
	/// </summary>
	public decimal PriceDeviation
	{
		get => _priceDeviation.Value;
		set => _priceDeviation.Value = value;
	}

	/// <summary>
	/// Maximum number of safety orders.
	/// </summary>
	public int MaxSafetyOrders
	{
		get => _maxSafetyOrders.Value;
		set => _maxSafetyOrders.Value = value;
	}

	/// <summary>
	/// Take profit in percent.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Multiplier for safety order size.
	/// </summary>
	public decimal OrderSizeMultiplier
	{
		get => _orderSizeMultiplier.Value;
		set => _orderSizeMultiplier.Value = value;
	}

	public SimpleDcaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetCanOptimize(false);
		_baseOrderSize = Param(nameof(BaseOrderSize), 50m);
		_priceDeviation = Param(nameof(PriceDeviation), 1m);
		_maxSafetyOrders = Param(nameof(MaxSafetyOrders), 10);
		_takeProfit = Param(nameof(TakeProfit), 1m);
		_orderSizeMultiplier = Param(nameof(OrderSizeMultiplier), 1.3m);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position == 0)
		{
			_lastEntryPrice = 0m;
			_safetyOrderCount = 0;
			_totalQuantity = 0m;
			_totalCost = 0m;
			_averageEntryPrice = 0m;

			var qty = BaseOrderSize / candle.ClosePrice;
			BuyMarket(qty);

			_lastEntryPrice = candle.ClosePrice;
			_totalQuantity = qty;
			_totalCost = BaseOrderSize;
			_averageEntryPrice = candle.ClosePrice;
		}
		else
		{
			var deviationPrice = _lastEntryPrice * (1 - PriceDeviation / 100m);

			if (_safetyOrderCount < MaxSafetyOrders && candle.LowPrice < deviationPrice)
			{
				var orderSize = BaseOrderSize * (decimal)Math.Pow((double)OrderSizeMultiplier, _safetyOrderCount + 1);
				var qty = orderSize / candle.ClosePrice;
				BuyMarket(qty);

				_lastEntryPrice = candle.ClosePrice;
				_totalQuantity += qty;
				_totalCost += orderSize;
				_averageEntryPrice = _totalCost / _totalQuantity;
				_safetyOrderCount++;
			}

			var targetPrice = _averageEntryPrice * (1 + TakeProfit / 100m);

			if (candle.HighPrice >= targetPrice)
				SellMarket(Position);
		}
	}
}
