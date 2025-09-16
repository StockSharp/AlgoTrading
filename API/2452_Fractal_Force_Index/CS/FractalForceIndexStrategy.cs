using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fractal Force Index strategy.
/// Uses a smoothed force index to detect trend continuation or reversal.
/// Opens or closes positions based on indicator level crossovers.
/// </summary>
public class FractalForceIndexStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<TrendMode> _trend;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<DataType> _candleType;
	
	private ExponentialMovingAverage _ema;
	private decimal _prevValue;
	private decimal _prevClose;
	private bool _isFirst;
	
	/// <summary>
	/// EMA smoothing period for the force index.
	/// </summary>
	public int Period
	{
	get => _period.Value;
	set => _period.Value = value;
	}
	
	/// <summary>
	/// Upper threshold for the indicator.
	/// </summary>
	public decimal HighLevel
	{
	get => _highLevel.Value;
	set => _highLevel.Value = value;
	}
	
	/// <summary>
	/// Lower threshold for the indicator.
	/// </summary>
	public decimal LowLevel
	{
	get => _lowLevel.Value;
	set => _lowLevel.Value = value;
	}
	
	/// <summary>
	/// Trading mode relative to the indicator direction.
	/// </summary>
	public TrendMode Trend
	{
	get => _trend.Value;
	set => _trend.Value = value;
	}
	
	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpen
	{
	get => _buyOpen.Value;
	set => _buyOpen.Value = value;
	}
	
	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpen
	{
	get => _sellOpen.Value;
	set => _sellOpen.Value = value;
	}
	
	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyClose
	{
	get => _buyClose.Value;
	set => _buyClose.Value = value;
	}
	
	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellClose
	{
	get => _sellClose.Value;
	set => _sellClose.Value = value;
	}
	
	/// <summary>
	/// The type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="FractalForceIndexStrategy"/>.
	/// </summary>
	public FractalForceIndexStrategy()
	{
	_period = Param(nameof(Period), 30)
	.SetGreaterThanZero()
	.SetDisplay("Period", "EMA length for force index", "Indicator")
	.SetCanOptimize(true)
	.SetOptimize(10, 60, 5);
	
	_highLevel = Param(nameof(HighLevel), 0m)
	.SetDisplay("High Level", "Upper force threshold", "Indicator");
	
	_lowLevel = Param(nameof(LowLevel), 0m)
	.SetDisplay("Low Level", "Lower force threshold", "Indicator");
	
	_trend = Param(nameof(Trend), TrendMode.Direct)
	.SetDisplay("Trend", "Trading relative to indicator direction", "General");
	
	_buyOpen = Param(nameof(BuyOpen), true)
	.SetDisplay("Buy Open", "Allow opening long trades", "Trading");
	
	_sellOpen = Param(nameof(SellOpen), true)
	.SetDisplay("Sell Open", "Allow opening short trades", "Trading");
	
	_buyClose = Param(nameof(BuyClose), true)
	.SetDisplay("Buy Close", "Allow closing long trades", "Trading");
	
	_sellClose = Param(nameof(SellClose), true)
	.SetDisplay("Sell Close", "Allow closing short trades", "Trading");
	
	_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
	.SetDisplay("Candle Type", "Timeframe for indicator", "General");
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
	
	_ema = null;
	_prevValue = 0m;
	_prevClose = 0m;
	_isFirst = true;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);
	
	_ema = new ExponentialMovingAverage { Length = Period };
	
	var subscription = SubscribeCandles(CandleType);
	subscription.Bind(ProcessCandle).Start();
	
	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _ema);
	DrawOwnTrades(area);
	}
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
	return;
	
	if (_isFirst)
	{
	_prevClose = candle.ClosePrice;
	_isFirst = false;
	return;
	}
	
	var force = (candle.ClosePrice - _prevClose) * candle.TotalVolume;
	_prevClose = candle.ClosePrice;
	
	var value = _ema.Process(force).ToDecimal();
	
	if (!_ema.IsFormed || !IsFormedAndOnlineAndAllowTrading())
	{
	_prevValue = value;
	return;
	}
	
	var crossedAbove = _prevValue <= HighLevel && value > HighLevel;
	var crossedBelow = _prevValue >= LowLevel && value < LowLevel;
	
	_prevValue = value;
	
	if (crossedAbove)
	{
	if (Trend == TrendMode.Direct)
	{
	if (SellClose && Position < 0)
	BuyMarket(Math.Abs(Position));
	
	if (BuyOpen && Position <= 0)
	BuyMarket(Volume);
	}
	else
	{
	if (BuyClose && Position > 0)
	SellMarket(Math.Abs(Position));
	
	if (SellOpen && Position >= 0)
	SellMarket(Volume);
	}
	}
	else if (crossedBelow)
	{
	if (Trend == TrendMode.Direct)
	{
	if (BuyClose && Position > 0)
	SellMarket(Math.Abs(Position));
	
	if (SellOpen && Position >= 0)
	SellMarket(Volume);
	}
	else
	{
	if (SellClose && Position < 0)
	BuyMarket(Math.Abs(Position));
	
	if (BuyOpen && Position <= 0)
	BuyMarket(Volume);
	}
	}
	}
}

/// <summary>
/// Trading direction modes.
/// </summary>
public enum TrendMode
{
	/// <summary>
	/// Trade in the same direction as the indicator.
	/// </summary>
	Direct,
	
	/// <summary>
	/// Trade against the indicator direction.
	/// </summary>
	Against
}
