using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple EMA-based trend following strategy.
/// Buys when the close price crosses above the EMA and sells on the opposite cross.
/// Optional fixed stop-loss and take-profit manage risk.
/// </summary>
public class EmaStickerStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _wasAbove;

	/// <summary>
	/// EMA period length.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Take-profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop-loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public EmaStickerStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("EMA Period", "Length of the EMA", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(5, 50, 5);

		_takeProfit = Param(nameof(TakeProfit), 0.001m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Take profit in price units", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.0005m, 0.005m, 0.0005m);

		_stopLoss = Param(nameof(StopLoss), 0.001m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Stop loss in price units", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.0005m, 0.005m, 0.0005m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_wasAbove = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

	var ema = new EMA { Length = MaPeriod };

	var subscription = SubscribeCandles(CandleType);

	subscription
	.Bind(ema, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, ema);
	DrawOwnTrades(area);
	}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var isAbove = candle.ClosePrice > emaValue;

	if (Position <= 0 && isAbove && !_wasAbove)
	{
	_entryPrice = candle.ClosePrice;
	BuyMarket(Volume + Math.Abs(Position));
	}
	else if (Position >= 0 && !isAbove && _wasAbove)
	{
	_entryPrice = candle.ClosePrice;
	SellMarket(Volume + Math.Abs(Position));
	}
	else
	{
	if (Position > 0)
	{
	if (candle.ClosePrice - _entryPrice >= TakeProfit ||
	_entryPrice - candle.ClosePrice >= StopLoss)
	SellMarket(Math.Abs(Position));
	}
	else if (Position < 0)
	{
	if (_entryPrice - candle.ClosePrice >= TakeProfit ||
	candle.ClosePrice - _entryPrice >= StopLoss)
	BuyMarket(Math.Abs(Position));
	}
	}

	_wasAbove = isAbove;
	}
}
