using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy demonstrating pending stop orders based on ADX and DMI cross.
/// Places buy/sell stop orders when ADX is above threshold and DMI crosses.
/// Positions are closed when opposite DMI cross occurs.
/// </summary>
public class AdxStopOrderTemplateStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxSignal;
	private readonly StrategyParam<int> _pips;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<decimal> _maxSpread;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevPlus;
	private decimal _prevMinus;
	private decimal _prevPrevPlus;
	private decimal _prevPrevMinus;
	private int _lastOrder; // 1 buy, 0 sell, 2 none
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public AdxStopOrderTemplateStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ADX Period", "Calculation period for ADX and DMI.", "Indicators");
		
		_adxSignal = Param(nameof(AdxSignal), 5m)
		.SetGreaterThanZero()
		.SetDisplay("ADX Threshold", "Minimum ADX value to allow entries.", "Indicators");
		
		_pips = Param(nameof(Pips), 10)
		.SetGreaterThanZero()
		.SetDisplay("Pending Offset", "Distance in price steps for stop orders.", "Orders");
		
		_takeProfit = Param(nameof(TakeProfit), 1000)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Take profit size in price steps.", "Risk");
		
		_stopLoss = Param(nameof(StopLoss), 500)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Stop loss size in price steps.", "Risk");
		
		_maxSpread = Param(nameof(MaxSpread), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Max Spread", "Maximum allowed spread in price steps.", "Orders");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for analysis.", "General");
	}
	
	#region Parameters
	
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}
	
	public decimal AdxSignal
	{
		get => _adxSignal.Value;
		set => _adxSignal.Value = value;
	}
	
	public int Pips
	{
		get => _pips.Value;
		set => _pips.Value = value;
	}
	
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}
	
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}
	
	public decimal MaxSpread
	{
		get => _maxSpread.Value;
		set => _maxSpread.Value = value;
	}
	
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	#endregion
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_prevPlus = _prevMinus = _prevPrevPlus = _prevPrevMinus = 0m;
		_lastOrder = 2;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var dmi = new DirectionalIndex { Length = AdxPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod, Smoothing = AdxPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(dmi, adx, ProcessCandle).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawIndicator(area, dmi);
			DrawOwnTrades(area);
		}
		
		var step = Security.PriceStep ?? 1m;
		StartProtection(
		new Unit(TakeProfit * step, UnitTypes.Absolute),
		new Unit(StopLoss * step, UnitTypes.Absolute)
		);
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue dmiValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var dmiTyped = (DirectionalIndexValue)dmiValue;
		if (dmiTyped.Plus is not decimal diPlus ||
		dmiTyped.Minus is not decimal diMinus)
		return;
		
		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adx)
		return;
		
		var step = Security.PriceStep ?? 1m;
		var bid = Security.BestBidPrice ?? candle.ClosePrice;
		var ask = Security.BestAskPrice ?? candle.ClosePrice;
		var spread = (ask - bid) / step;
		
		if (Position == 0 && spread < MaxSpread)
		{
			if (adx > AdxSignal && diPlus > diMinus && _prevPrevPlus < _prevPrevMinus && _lastOrder != 1)
			{
				CancelActiveOrders();
				var price = ask + Pips * step;
				BuyStop(Volume, price);
				_lastOrder = 1;
			}
			else if (adx > AdxSignal && diPlus < diMinus && _prevPrevPlus > _prevPrevMinus && _lastOrder != 0)
			{
				CancelActiveOrders();
				var price = bid - Pips * step;
				SellStop(Volume, price);
				_lastOrder = 0;
			}
		}
		else if (Position > 0)
		{
			if (diPlus < diMinus)
			{
				CancelActiveOrders();
				SellMarket(Position);
				_lastOrder = 2;
			}
		}
		else if (Position < 0)
		{
			if (diPlus > diMinus)
			{
				CancelActiveOrders();
				BuyMarket(Math.Abs(Position));
				_lastOrder = 2;
			}
		}
		
		_prevPrevPlus = _prevPlus;
		_prevPrevMinus = _prevMinus;
		_prevPlus = diPlus;
		_prevMinus = diMinus;
	}
}
