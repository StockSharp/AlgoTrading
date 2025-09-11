using System;
using System.Collections.Generic;
	
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
	
namespace StockSharp.Samples.Strategies;
	
	/// <summary>
	/// SuperTrade ST1 strategy.
	/// </summary>
	public class SuperTradeSt1Strategy : Strategy
	{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _stopAtrMultiplier;
	private readonly StrategyParam<decimal> _takeAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	
	private int _prevDirection;
	private bool _hasPrevDirection;
	private decimal _stopPrice;
	private decimal _takePrice;
	
	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
	get => _atrPeriod.Value;
	set => _atrPeriod.Value = value;
	}
	
	/// <summary>
	/// Supertrend factor.
	/// </summary>
	public decimal Factor
	{
	get => _factor.Value;
	set => _factor.Value = value;
	}
	
	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaPeriod
	{
	get => _emaPeriod.Value;
	set => _emaPeriod.Value = value;
	}
	
	/// <summary>
	/// ATR multiplier for stop-loss.
	/// </summary>
	public decimal StopAtrMultiplier
	{
	get => _stopAtrMultiplier.Value;
	set => _stopAtrMultiplier.Value = value;
	}
	
	/// <summary>
	/// ATR multiplier for take-profit.
	/// </summary>
	public decimal TakeAtrMultiplier
	{
	get => _takeAtrMultiplier.Value;
	set => _takeAtrMultiplier.Value = value;
	}
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public SuperTradeSt1Strategy()
	{
	_atrPeriod = Param(nameof(AtrPeriod), 10)
	.SetGreaterThanZero()
	.SetDisplay("ATR Period", "ATR calculation period", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(5, 20, 1);
	
	_factor = Param(nameof(Factor), 3m)
	.SetGreaterThanZero()
	.SetDisplay("Supertrend Factor", "ATR multiplier for Supertrend", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(1m, 5m, 0.5m);
	
	_emaPeriod = Param(nameof(EmaPeriod), 200)
	.SetGreaterThanZero()
	.SetDisplay("EMA Period", "EMA filter period", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(50, 300, 50);
	
	_stopAtrMultiplier = Param(nameof(StopAtrMultiplier), 1m)
	.SetGreaterThanZero()
	.SetDisplay("Stop ATR Mult", "ATR multiplier for stop-loss", "Risk")
	.SetCanOptimize(true)
	.SetOptimize(1m, 3m, 0.5m);
	
	_takeAtrMultiplier = Param(nameof(TakeAtrMultiplier), 4m)
	.SetGreaterThanZero()
	.SetDisplay("Take ATR Mult", "ATR multiplier for take-profit", "Risk")
	.SetCanOptimize(true)
	.SetOptimize(2m, 6m, 1m);
	
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
	.SetDisplay("Candle Type", "Type of candles", "General");
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
	
	_prevDirection = 0;
	_hasPrevDirection = false;
	_stopPrice = 0m;
	_takePrice = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);
	
	var supertrend = new SuperTrend { Length = AtrPeriod, Multiplier = Factor };
	var atr = new AverageTrueRange { Length = AtrPeriod };
	var ema = new ExponentialMovingAverage { Length = EmaPeriod };
	
	var subscription = SubscribeCandles(CandleType);
	
	subscription
	.BindEx(supertrend, atr, ema, ProcessCandle)
	.Start();
	
	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, supertrend);
	DrawIndicator(area, ema);
	DrawOwnTrades(area);
	}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue, IIndicatorValue atrValue, IIndicatorValue emaValue)
	{
	if (candle.State != CandleStates.Finished)
	return;
	
	if (!IsFormedAndOnlineAndAllowTrading())
	return;
	
	var st = (SuperTrendIndicatorValue)stValue;
	var atr = atrValue.ToDecimal();
	var ema = emaValue.ToDecimal();
	
	var direction = st.IsUpTrend ? 1 : -1;
	
	if (!_hasPrevDirection)
	{
	_prevDirection = direction;
	_hasPrevDirection = true;
	return;
	}
	
	var longCondition = _prevDirection > direction && candle.ClosePrice > st.Value && candle.ClosePrice > ema;
	
	if (longCondition && Position <= 0)
	{
	BuyMarket(Volume + Math.Abs(Position));
	_stopPrice = candle.ClosePrice - StopAtrMultiplier * atr;
	_takePrice = candle.ClosePrice + TakeAtrMultiplier * atr;
	}
	else if (Position > 0)
	{
	if (candle.ClosePrice <= _stopPrice || candle.ClosePrice >= _takePrice)
	{
	SellMarket(Math.Abs(Position));
	}
	}
	
	_prevDirection = direction;
	}
	}
	
