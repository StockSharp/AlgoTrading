using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martin Strategy - No Loss Exit v3.
/// Adds to a long position on price drops and exits on profit target.
/// </summary>
public class MartinStrategyNoLossExitV3Strategy : Strategy
{
	private readonly StrategyParam<decimal> _initialCash;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<decimal> _priceStepPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _increaseFactor;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _totalCost;
	private decimal _totalQty;
	private decimal _lastCash;
	private int _orderCount;
	private bool _inPosition;
	private bool _waitingClose;

	/// <summary>
	/// Initial purchase amount in USD.
	/// </summary>
	public decimal InitialCash
	{
		get => _initialCash.Value;
		set => _initialCash.Value = value;
	}

	/// <summary>
	/// Maximum number of entries including the first.
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Percentage drop for each additional order.
	/// </summary>
	public decimal PriceStepPercent
	{
		get => _priceStepPercent.Value;
		set => _priceStepPercent.Value = value;
	}

	/// <summary>
	/// Take profit percentage from average price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Multiplier for each additional cash amount.
	/// </summary>
	public decimal IncreaseFactor
	{
		get => _increaseFactor.Value;
		set => _increaseFactor.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MartinStrategyNoLossExitV3Strategy"/>.
	/// </summary>
	public MartinStrategyNoLossExitV3Strategy()
	{
		_initialCash = Param(nameof(InitialCash), 100m)
			.SetDisplay("Initial Cash", "Initial purchase amount", "General")
			.SetCanOptimize(true);

		_maxOrders = Param(nameof(MaxOrders), 20)
			.SetDisplay("Max Orders", "Maximum number of entries", "General")
			.SetCanOptimize(true);

		_priceStepPercent = Param(nameof(PriceStepPercent), 1.5m)
			.SetDisplay("Price Step %", "Price drop for next order", "General")
			.SetCanOptimize(true);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1m)
			.SetDisplay("Take Profit %", "Profit target from average price", "General")
			.SetCanOptimize(true);

		_increaseFactor = Param(nameof(IncreaseFactor), 1.05m)
			.SetDisplay("Increase Factor", "Multiplier for next cash amount", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_totalCost = 0m;
		_totalQty = 0m;
		_lastCash = 0m;
		_orderCount = 0;
		_inPosition = false;
		_waitingClose = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_inPosition)
		{
			var avgPrice = _totalQty > 0m ? _totalCost / _totalQty : 0m;
			var takeProfitPrice = avgPrice * (1 + TakeProfitPercent / 100m);

			if (candle.HighPrice >= takeProfitPrice)
			{
				if (Position > 0)
					SellMarket(Position);
				_inPosition = false;
				_waitingClose = true;
				return;
			}

			var nextEntryPrice = _entryPrice * (1 - PriceStepPercent / 100m * _orderCount);
			var canAdd = _orderCount < MaxOrders && candle.ClosePrice <= nextEntryPrice;

			if (canAdd && !_waitingClose)
			{
				var newCash = _lastCash * IncreaseFactor;
				var qty = newCash / candle.ClosePrice;

				BuyMarket(qty);

				_totalCost += newCash;
				_totalQty += qty;
				_lastCash = newCash;
				_orderCount++;
			}
		}

		if (!_inPosition && _waitingClose)
		{
			_entryPrice = 0m;
			_totalCost = 0m;
			_totalQty = 0m;
			_lastCash = 0m;
			_orderCount = 0;
			_waitingClose = false;
		}

		if (!_inPosition && !_waitingClose)
		{
			var qty = InitialCash / candle.ClosePrice;
			BuyMarket(qty);

			_entryPrice = candle.ClosePrice;
			_totalCost = InitialCash;
			_totalQty = qty;
			_lastCash = InitialCash;
			_orderCount = 1;
			_inPosition = true;
		}
	}
}