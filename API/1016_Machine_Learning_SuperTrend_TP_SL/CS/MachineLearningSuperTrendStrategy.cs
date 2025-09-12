using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

	/// <summary>
	/// SuperTrend strategy with trailing take profit and stop loss.
	/// </summary>
	public class MachineLearningSuperTrendStrategy : Strategy
	{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	
	private SuperTrend _superTrend;
	private int _prevDirection;
	private decimal _stopLoss;
	private decimal _takeProfit;
	
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}
	
	/// <summary>
	/// ATR period for SuperTrend calculation.
	/// </summary>
	public int AtrPeriod
	{
	get => _atrPeriod.Value;
	set => _atrPeriod.Value = value;
	}
	
	/// <summary>
	/// ATR multiplier for SuperTrend.
	/// </summary>
	public decimal AtrFactor
	{
	get => _atrFactor.Value;
	set => _atrFactor.Value = value;
	}
	
	/// <summary>
	/// Stop loss multiplier relative to SuperTrend value.
	/// </summary>
	public decimal StopLossMultiplier
	{
	get => _stopLossMultiplier.Value;
	set => _stopLossMultiplier.Value = value;
	}
	
	/// <summary>
	/// Take profit multiplier relative to SuperTrend value.
	/// </summary>
	public decimal TakeProfitMultiplier
	{
	get => _takeProfitMultiplier.Value;
	set => _takeProfitMultiplier.Value = value;
	}
	
	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public MachineLearningSuperTrendStrategy()
	{
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
	.SetDisplay("Candle Type", "Type of candles to use", "General");
	
	_atrPeriod = Param(nameof(AtrPeriod), 4)
	.SetGreaterThanZero()
	.SetDisplay("ATR Period", "ATR length for SuperTrend", "SuperTrend")
	.SetCanOptimize(true)
	.SetOptimize(3, 20, 1);
	
	_atrFactor = Param(nameof(AtrFactor), 2.94m)
	.SetRange(0.5m, 10m)
	.SetDisplay("Multiplier", "ATR multiplier for SuperTrend", "SuperTrend")
	.SetCanOptimize(true)
	.SetOptimize(1m, 5m, 0.5m);
	
	_stopLossMultiplier = Param(nameof(StopLossMultiplier), 0.0025m)
	.SetRange(0m, 0.05m)
	.SetDisplay("Stop Loss Mult", "Percentage from SuperTrend", "Risk Management");
	
	_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 0.022m)
	.SetRange(0m, 0.1m)
	.SetDisplay("Take Profit Mult", "Percentage from SuperTrend", "Risk Management");
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
	_stopLoss = 0;
	_takeProfit = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);
	
	_superTrend = new SuperTrend
	{
	Length = AtrPeriod,
	Multiplier = AtrFactor
	};
	
	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(_superTrend, ProcessCandle)
	.Start();
	
	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _superTrend);
	DrawOwnTrades(area);
	}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal superTrendValue)
	{
	if (candle.State != CandleStates.Finished)
	return;
	
	if (!IsFormedAndOnlineAndAllowTrading() || !_superTrend.IsFormed)
	return;
	
	var direction = candle.ClosePrice > superTrendValue ? -1 : 1;
	var directionChanged = _prevDirection != 0 && direction != _prevDirection;
	
	_stopLoss = direction == -1
	? superTrendValue - superTrendValue * StopLossMultiplier
	: superTrendValue + superTrendValue * StopLossMultiplier;
	
	_takeProfit = direction == -1
	? superTrendValue + superTrendValue * TakeProfitMultiplier
	: superTrendValue - superTrendValue * TakeProfitMultiplier;
	
	if (directionChanged)
	{
	if (direction == -1 && Position <= 0)
	BuyMarket(Volume + Math.Abs(Position));
	else if (direction == 1 && Position >= 0)
	SellMarket(Volume + Math.Abs(Position));
	}
	
	if (Position > 0)
	{
	if (candle.ClosePrice <= _stopLoss || candle.ClosePrice >= _takeProfit)
	SellMarket(Math.Abs(Position));
	}
	else if (Position < 0)
	{
	if (candle.ClosePrice >= _stopLoss || candle.ClosePrice <= _takeProfit)
	BuyMarket(Math.Abs(Position));
	}
	
	_prevDirection = direction;
	}
	}
