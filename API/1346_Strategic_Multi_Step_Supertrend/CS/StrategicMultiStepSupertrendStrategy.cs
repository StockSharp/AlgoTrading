using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using double Supertrend with multi-step take profits.
/// </summary>
public class StrategicMultiStepSupertrendStrategy : Strategy
{
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPercent1;
	private readonly StrategyParam<decimal> _takeProfitPercent2;
	private readonly StrategyParam<decimal> _takeProfitPercent3;
	private readonly StrategyParam<decimal> _takeProfitPercent4;
	private readonly StrategyParam<decimal> _takeProfitAmount1;
	private readonly StrategyParam<decimal> _takeProfitAmount2;
	private readonly StrategyParam<decimal> _takeProfitAmount3;
	private readonly StrategyParam<decimal> _takeProfitAmount4;
	private readonly StrategyParam<int> _numberOfSteps;
private readonly StrategyParam<Sides?> _direction;
	private readonly StrategyParam<int> _atrPeriod1;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<int> _atrPeriod2;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevSupertrend1;
	private bool _prevAbove1;
	private decimal _prevSupertrend2;
	private bool _prevAbove2;
	private decimal _entryPrice;
	
	public bool UseTakeProfit { get => _useTakeProfit.Value; set => _useTakeProfit.Value = value; }
	public decimal TakeProfitPercent1 { get => _takeProfitPercent1.Value; set => _takeProfitPercent1.Value = value; }
	public decimal TakeProfitPercent2 { get => _takeProfitPercent2.Value; set => _takeProfitPercent2.Value = value; }
	public decimal TakeProfitPercent3 { get => _takeProfitPercent3.Value; set => _takeProfitPercent3.Value = value; }
	public decimal TakeProfitPercent4 { get => _takeProfitPercent4.Value; set => _takeProfitPercent4.Value = value; }
	public decimal TakeProfitAmount1 { get => _takeProfitAmount1.Value; set => _takeProfitAmount1.Value = value; }
	public decimal TakeProfitAmount2 { get => _takeProfitAmount2.Value; set => _takeProfitAmount2.Value = value; }
	public decimal TakeProfitAmount3 { get => _takeProfitAmount3.Value; set => _takeProfitAmount3.Value = value; }
	public decimal TakeProfitAmount4 { get => _takeProfitAmount4.Value; set => _takeProfitAmount4.Value = value; }
	public int NumberOfSteps { get => _numberOfSteps.Value; set => _numberOfSteps.Value = value; }
public Sides? Direction { get => _direction.Value; set => _direction.Value = value; }
	public int AtrPeriod1 { get => _atrPeriod1.Value; set => _atrPeriod1.Value = value; }
	public decimal Factor1 { get => _factor1.Value; set => _factor1.Value = value; }
	public int AtrPeriod2 { get => _atrPeriod2.Value; set => _atrPeriod2.Value = value; }
	public decimal Factor2 { get => _factor2.Value; set => _factor2.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public StrategicMultiStepSupertrendStrategy()
	{
		_useTakeProfit = Param(nameof(UseTakeProfit), true)
		.SetDisplay("Use Take Profit", "Enable partial take profit", "Take Profit Settings");
		
		_takeProfitPercent1 = Param(nameof(TakeProfitPercent1), 6.0m)
		.SetDisplay("TP % Step 1", "Take profit percentage step 1", "Take Profit Settings")
		.SetCanOptimize(true);
		_takeProfitPercent2 = Param(nameof(TakeProfitPercent2), 12.0m)
		.SetDisplay("TP % Step 2", "Take profit percentage step 2", "Take Profit Settings")
		.SetCanOptimize(true);
		_takeProfitPercent3 = Param(nameof(TakeProfitPercent3), 18.0m)
		.SetDisplay("TP % Step 3", "Take profit percentage step 3", "Take Profit Settings")
		.SetCanOptimize(true);
		_takeProfitPercent4 = Param(nameof(TakeProfitPercent4), 50.0m)
		.SetDisplay("TP % Step 4", "Take profit percentage step 4", "Take Profit Settings")
		.SetCanOptimize(true);
		
		_takeProfitAmount1 = Param(nameof(TakeProfitAmount1), 12m)
		.SetDisplay("TP Amount % Step 1", "Quantity percent step 1", "Take Profit Settings")
		.SetCanOptimize(true);
		_takeProfitAmount2 = Param(nameof(TakeProfitAmount2), 8m)
		.SetDisplay("TP Amount % Step 2", "Quantity percent step 2", "Take Profit Settings")
		.SetCanOptimize(true);
		_takeProfitAmount3 = Param(nameof(TakeProfitAmount3), 4m)
		.SetDisplay("TP Amount % Step 3", "Quantity percent step 3", "Take Profit Settings")
		.SetCanOptimize(true);
		_takeProfitAmount4 = Param(nameof(TakeProfitAmount4), 0m)
		.SetDisplay("TP Amount % Step 4", "Quantity percent step 4", "Take Profit Settings")
		.SetCanOptimize(true);
		
		_numberOfSteps = Param(nameof(NumberOfSteps), 3)
		.SetDisplay("Number of Steps", "Number of take profit steps", "Take Profit Settings")
		.SetCanOptimize(true);
		
_direction = Param(nameof(Direction), (Sides?)null)
.SetDisplay("Trade Direction", "Trade direction (Long/Short/Both)", "Trade Direction");
		
		_atrPeriod1 = Param(nameof(AtrPeriod1), 10)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period 1", "ATR length for first Supertrend", "Supertrend Settings")
		.SetCanOptimize(true);
		_factor1 = Param(nameof(Factor1), 3.0m)
		.SetDisplay("Factor 1", "Multiplier for first Supertrend", "Supertrend Settings")
		.SetCanOptimize(true);
		
		_atrPeriod2 = Param(nameof(AtrPeriod2), 5)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period 2", "ATR length for second Supertrend", "Supertrend Settings")
		.SetCanOptimize(true);
		_factor2 = Param(nameof(Factor2), 4.0m)
		.SetDisplay("Factor 2", "Multiplier for second Supertrend", "Supertrend Settings")
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
		_prevSupertrend1 = 0;
		_prevSupertrend2 = 0;
		_prevAbove1 = false;
		_prevAbove2 = false;
		_entryPrice = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var atr1 = new AverageTrueRange { Length = AtrPeriod1 };
		var atr2 = new AverageTrueRange { Length = AtrPeriod2 };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr1, atr2, ProcessCandle).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal atr1Value, decimal atr2Value)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var direction1 = GetDirection(candle, atr1Value, Factor1, ref _prevSupertrend1, ref _prevAbove1);
		var direction2 = GetDirection(candle, atr2Value, Factor2, ref _prevSupertrend2, ref _prevAbove2);
		
var allowLong = Direction is null or Sides.Buy;
var allowShort = Direction is null or Sides.Sell;
var longCondition = direction1 < 0 && direction2 < 0 && allowLong;
var shortCondition = direction1 > 0 && direction2 > 0 && allowShort;

var longExitCondition = direction1 > 0 && direction2 > 0 && allowLong;
var shortExitCondition = direction1 < 0 && direction2 < 0 && allowShort;
		
		if (longCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			PlaceTakeProfitOrders(true);
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			PlaceTakeProfitOrders(false);
		}
		else
		{
			if (longExitCondition && Position > 0)
			{
				SellMarket(Position);
			}
			
			if (shortExitCondition && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}
	
	private int GetDirection(ICandleMessage candle, decimal atrValue, decimal factor, ref decimal prevSupertrend, ref bool prevAbove)
	{
		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var upper = median + factor * atrValue;
		var lower = median - factor * atrValue;
		
		decimal st;
		if (prevSupertrend == 0)
		{
			st = candle.ClosePrice > median ? lower : upper;
		}
		else if (prevSupertrend <= candle.HighPrice)
		{
			st = Math.Max(lower, prevSupertrend);
		}
		else if (prevSupertrend >= candle.LowPrice)
		{
			st = Math.Min(upper, prevSupertrend);
		}
		else
		{
			st = candle.ClosePrice > prevSupertrend ? lower : upper;
		}
		
		var isAbove = candle.ClosePrice > st;
		prevSupertrend = st;
		prevAbove = isAbove;
		
		return isAbove ? 1 : -1;
	}
	
	private void PlaceTakeProfitOrders(bool isLong)
	{
		if (!UseTakeProfit || _entryPrice == 0)
		return;
		
		if (NumberOfSteps >= 1 && TakeProfitAmount1 > 0)
		PlaceTakeProfitOrder(isLong, TakeProfitAmount1, TakeProfitPercent1);
		if (NumberOfSteps >= 2 && TakeProfitAmount2 > 0)
		PlaceTakeProfitOrder(isLong, TakeProfitAmount2, TakeProfitPercent2);
		if (NumberOfSteps >= 3 && TakeProfitAmount3 > 0)
		PlaceTakeProfitOrder(isLong, TakeProfitAmount3, TakeProfitPercent3);
		if (NumberOfSteps >= 4 && TakeProfitAmount4 > 0)
		PlaceTakeProfitOrder(isLong, TakeProfitAmount4, TakeProfitPercent4);
	}
	
	private void PlaceTakeProfitOrder(bool isLong, decimal qtyPercent, decimal percent)
	{
		var qty = Volume * (qtyPercent / 100m);
		if (qty <= 0)
		return;
		
		var price = _entryPrice * (1m + (isLong ? percent : -percent) / 100m);
		
		if (isLong)
		SellLimit(qty, price);
		else
		BuyLimit(qty, price);
	}
}
