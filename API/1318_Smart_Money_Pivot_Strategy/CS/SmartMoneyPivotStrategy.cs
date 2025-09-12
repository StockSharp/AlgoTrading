using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Smart Money Pivot breakout strategy.
/// Opens long when price breaks above the latest pivot high and short when breaking below the pivot low.
/// Each trade uses separate stop-loss and take-profit percentages.
/// </summary>
public class SmartMoneyPivotStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableLongStrategy;
	private readonly StrategyParam<decimal> _longStopLossPercent;
	private readonly StrategyParam<decimal> _longTakeProfitPercent;
	private readonly StrategyParam<bool> _enableShortStrategy;
	private readonly StrategyParam<decimal> _shortStopLossPercent;
	private readonly StrategyParam<decimal> _shortTakeProfitPercent;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal? _updatedHigh;
	private decimal? _updatedLow;
	private bool _phActive;
	private bool _plActive;
	private decimal[] _highs;
	private decimal[] _lows;
	private int _bufferCount;
	
	private decimal _longStopPrice;
	private decimal _longTakeProfitPrice;
	private decimal _shortStopPrice;
	private decimal _shortTakeProfitPrice;
	
	/// <summary>
	/// Enable or disable long trades.
	/// </summary>
	public bool EnableLongStrategy { get => _enableLongStrategy.Value; set => _enableLongStrategy.Value = value; }
	
	/// <summary>
	/// Stop-loss percent for long trades.
	/// </summary>
	public decimal LongStopLossPercent { get => _longStopLossPercent.Value; set => _longStopLossPercent.Value = value; }
	
	/// <summary>
	/// Take-profit percent for long trades.
	/// </summary>
	public decimal LongTakeProfitPercent { get => _longTakeProfitPercent.Value; set => _longTakeProfitPercent.Value = value; }
	
	/// <summary>
	/// Enable or disable short trades.
	/// </summary>
	public bool EnableShortStrategy { get => _enableShortStrategy.Value; set => _enableShortStrategy.Value = value; }
	
	/// <summary>
	/// Stop-loss percent for short trades.
	/// </summary>
	public decimal ShortStopLossPercent { get => _shortStopLossPercent.Value; set => _shortStopLossPercent.Value = value; }
	
	/// <summary>
	/// Take-profit percent for short trades.
	/// </summary>
	public decimal ShortTakeProfitPercent { get => _shortTakeProfitPercent.Value; set => _shortTakeProfitPercent.Value = value; }
	
	/// <summary>
	/// Pivot length.
	/// </summary>
	public int Period { get => _period.Value; set => _period.Value = value; }
	
	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes <see cref="SmartMoneyPivotStrategy"/>.
	/// </summary>
	public SmartMoneyPivotStrategy()
	{
	_enableLongStrategy = Param(nameof(EnableLongStrategy), true)
	.SetDisplay("Enable Long", "Enable long trades", "General");
	
	_longStopLossPercent = Param(nameof(LongStopLossPercent), 1m)
	.SetDisplay("Long SL %", "Stop-loss percent for longs", "Risk")
	.SetGreaterThanZero();
	
	_longTakeProfitPercent = Param(nameof(LongTakeProfitPercent), 1.5m)
	.SetDisplay("Long TP %", "Take-profit percent for longs", "Risk")
	.SetGreaterThanZero();
	
	_enableShortStrategy = Param(nameof(EnableShortStrategy), true)
	.SetDisplay("Enable Short", "Enable short trades", "General");
	
	_shortStopLossPercent = Param(nameof(ShortStopLossPercent), 1m)
	.SetDisplay("Short SL %", "Stop-loss percent for shorts", "Risk")
	.SetGreaterThanZero();
	
	_shortTakeProfitPercent = Param(nameof(ShortTakeProfitPercent), 1.5m)
	.SetDisplay("Short TP %", "Take-profit percent for shorts", "Risk")
	.SetGreaterThanZero();
	
	_period = Param(nameof(Period), 20)
	.SetDisplay("Length", "Pivot length", "Smart Money Pivot")
	.SetGreaterThanZero();
	
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
	.SetDisplay("Candle Type", "Type of candles", "General");
	
	Volume = 1;
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
	
	_updatedHigh = null;
	_updatedLow = null;
	_phActive = false;
	_plActive = false;
	_bufferCount = 0;
	var len = Period * 2 + 1;
	_highs = new decimal[len];
	_lows = new decimal[len];
	_longStopPrice = 0m;
	_longTakeProfitPrice = 0m;
	_shortStopPrice = 0m;
	_shortTakeProfitPrice = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);
	
	StartProtection();
	
	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(ProcessCandle).Start();
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
	return;
	
	for (var i = _highs.Length - 1; i > 0; i--)
	{
	_highs[i] = _highs[i - 1];
	_lows[i] = _lows[i - 1];
	}
	
	_highs[0] = candle.HighPrice;
	_lows[0] = candle.LowPrice;
	if (_bufferCount < _highs.Length)
	_bufferCount++;
	
	if (_bufferCount == _highs.Length)
	{
	var mid = Period;
	var candidateHigh = _highs[mid];
	var candidateLow = _lows[mid];
	
	var isPivotHigh = true;
	var isPivotLow = true;
	
	for (var i = 0; i < _highs.Length; i++)
	{
	if (i == mid)
	continue;
	
	if (_highs[i] >= candidateHigh)
	isPivotHigh = false;
	
	if (_lows[i] <= candidateLow)
	isPivotLow = false;
	
	if (!isPivotHigh && !isPivotLow)
	break;
	}
	
	if (isPivotHigh)
	{
	_updatedHigh = candidateHigh;
	_phActive = true;
	}
	
	if (isPivotLow)
	{
	_updatedLow = candidateLow;
	_plActive = true;
	}
	}
	
	if (Position > 0)
	{
	if (candle.LowPrice <= _longStopPrice || candle.HighPrice >= _longTakeProfitPrice)
	SellMarket(Position);
	}
	else if (Position < 0)
	{
	if (candle.HighPrice >= _shortStopPrice || candle.LowPrice <= _shortTakeProfitPrice)
	BuyMarket(Math.Abs(Position));
	}
	
	if (!IsFormedAndOnlineAndAllowTrading())
	return;
	
	if (EnableLongStrategy && _phActive && _updatedHigh is decimal up && candle.HighPrice > up && Position <= 0)
	{
	var volume = Volume + Math.Abs(Position);
	BuyMarket(volume);
	var price = candle.ClosePrice;
	_longStopPrice = price * (1m - LongStopLossPercent / 100m);
	_longTakeProfitPrice = price * (1m + LongTakeProfitPercent / 100m);
	_phActive = false;
	}
	else if (EnableShortStrategy && _plActive && _updatedLow is decimal down && candle.LowPrice < down && Position >= 0)
	{
	var volume = Volume + Math.Abs(Position);
	SellMarket(volume);
	var price = candle.ClosePrice;
	_shortStopPrice = price * (1m + ShortStopLossPercent / 100m);
	_shortTakeProfitPrice = price * (1m - ShortTakeProfitPercent / 100m);
	_plActive = false;
	}
	}
}
