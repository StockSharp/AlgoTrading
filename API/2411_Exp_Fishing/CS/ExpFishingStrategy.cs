using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy that adds to position every time price moves by configured step.
/// Based on MQL5 Exp_Fishing expert.
/// </summary>
public class ExpFishingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _priceStep;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private int _ordersCount;
	private bool _isLong;

	/// <summary>
	/// Initializes a new instance of <see cref="ExpFishingStrategy"/>.
	/// </summary>
	public ExpFishingStrategy()
	{
		_priceStep = Param(nameof(PriceStep), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Price Step", "Minimum price move to enter or add", "Parameters")
			.SetCanOptimize(true);

		_maxOrders = Param(nameof(MaxOrders), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Orders", "Maximum number of orders in one direction", "Parameters")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance in price units", "Parameters")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance in price units", "Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for analysis", "Parameters");
	}

	/// <summary>
	/// Price move required to open or add position.
	/// </summary>
	public decimal PriceStep
	{
		get => _priceStep.Value;
		set => _priceStep.Value = value;
	}

	/// <summary>
	/// Maximum number of orders in single direction.
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type used for strategy calculations.
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
		_entryPrice = 0m;
		_ordersCount = 0;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(new Unit(TakeProfit, UnitTypes.Absolute), new Unit(StopLoss, UnitTypes.Absolute));

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var move = candle.ClosePrice - candle.OpenPrice;

		if (Position == 0)
		{
			_ordersCount = 0;

			if (move >= PriceStep)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_ordersCount = 1;
				_isLong = true;
			}
			else if (move <= -PriceStep)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_ordersCount = 1;
				_isLong = false;
			}

			return;
		}

		if (_ordersCount >= MaxOrders)
			return;

		if (_isLong)
		{
			if (candle.ClosePrice - _entryPrice >= PriceStep)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_ordersCount++;
			}
		}
		else
		{
			if (_entryPrice - candle.ClosePrice >= PriceStep)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_ordersCount++;
			}
		}
	}
}
