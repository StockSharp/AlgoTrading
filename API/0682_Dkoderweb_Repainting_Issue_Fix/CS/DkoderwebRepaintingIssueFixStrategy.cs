using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Harmonic pattern strategy with Fibonacci targets and stops.
/// Based on Dkoderweb repainting issue fix strategy from TradingView.
/// </summary>
public class DkoderwebRepaintingIssueFixStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeSize;
	private readonly StrategyParam<decimal> _entryRate;
	private readonly StrategyParam<decimal> _takeProfitRate;
	private readonly StrategyParam<decimal> _stopLossRate;
	private readonly StrategyParam<DataType> _candleType;
	
	private ICandleMessage _prevCandle;
	private int _direction;
	
	private decimal? _x;
	private decimal? _a;
	private decimal? _b;
	private decimal? _c;
	private decimal? _d;
	
	private bool _inBuyTrade;
	private bool _inSellTrade;
	private decimal _buyTp;
	private decimal _buySl;
	private decimal _sellTp;
	private decimal _sellSl;
	
	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal TradeSize
	{
	get => _tradeSize.Value;
	set => _tradeSize.Value = value;
	}
	
	/// <summary>
	/// Fibonacci rate for entry window.
	/// </summary>
	public decimal EntryRate
	{
	get => _entryRate.Value;
	set => _entryRate.Value = value;
	}
	
	/// <summary>
	/// Fibonacci rate for take profit.
	/// </summary>
	public decimal TakeProfitRate
	{
	get => _takeProfitRate.Value;
	set => _takeProfitRate.Value = value;
	}
	
	/// <summary>
	/// Fibonacci rate for stop loss.
	/// </summary>
	public decimal StopLossRate
	{
	get => _stopLossRate.Value;
	set => _stopLossRate.Value = value;
	}
	
	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public DkoderwebRepaintingIssueFixStrategy()
	{
	_tradeSize = Param(nameof(TradeSize), 1m)
	.SetGreaterThanZero()
	.SetDisplay("Trade Size", "Order volume", "General");
	
	_entryRate = Param(nameof(EntryRate), 0.382m)
	.SetDisplay("Entry Rate", "Fibonacci rate for entry window", "General");
	
	_takeProfitRate = Param(nameof(TakeProfitRate), 0.618m)
	.SetDisplay("TP Rate", "Fibonacci rate for take profit", "General");
	
	_stopLossRate = Param(nameof(StopLossRate), -0.618m)
	.SetDisplay("SL Rate", "Fibonacci rate for stop loss", "General");
	
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
	.SetDisplay("Candle Type", "Type of candles to process", "General");
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
	
	_prevCandle = null;
	_direction = 0;
	_x = _a = _b = _c = _d = null;
	_inBuyTrade = _inSellTrade = false;
	_buyTp = _buySl = _sellTp = _sellSl = 0m;
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
	
	var isUp = candle.ClosePrice >= candle.OpenPrice;
	var isDown = candle.ClosePrice <= candle.OpenPrice;
	
	var prevIsUp = _prevCandle != null && _prevCandle.ClosePrice >= _prevCandle.OpenPrice;
	var prevIsDown = _prevCandle != null && _prevCandle.ClosePrice <= _prevCandle.OpenPrice;
	
	var prevDirection = _direction;
	if (prevIsUp && isDown)
	_direction = -1;
	else if (prevIsDown && isUp)
	_direction = 1;
	
	decimal? zigzag = null;
	if (prevIsUp && isDown && prevDirection != -1 && _prevCandle != null)
	zigzag = _prevCandle.HighPrice;
	else if (prevIsDown && isUp && prevDirection != 1 && _prevCandle != null)
	zigzag = _prevCandle.LowPrice;
	
	if (zigzag != null)
	ShiftPoints(zigzag.Value);
	
	_prevCandle = candle;
	
	if (_d is null || _c is null)
	return;
	
	var fibRange = Math.Abs(_d.Value - _c.Value);
	decimal LastFib(decimal rate) => _d > _c ? _d.Value - fibRange * rate : _d.Value + fibRange * rate;
	
	var buyPattern = IsAbcd(1);
	var sellPattern = IsAbcd(-1);
	
	var buyEntry = buyPattern && candle.ClosePrice <= LastFib(EntryRate);
	var sellEntry = sellPattern && candle.ClosePrice >= LastFib(EntryRate);
	
	if (buyEntry && !_inBuyTrade && IsFormedAndOnlineAndAllowTrading())
	{
	BuyMarket(TradeSize + Math.Abs(Position));
	_buyTp = LastFib(TakeProfitRate);
	_buySl = LastFib(StopLossRate);
	_inBuyTrade = true;
	}
	
	if (sellEntry && !_inSellTrade && IsFormedAndOnlineAndAllowTrading())
	{
	SellMarket(TradeSize + Math.Abs(Position));
	_sellTp = LastFib(TakeProfitRate);
	_sellSl = LastFib(StopLossRate);
	_inSellTrade = true;
	}
	
	var buyClose = _inBuyTrade && (candle.HighPrice >= _buyTp || candle.LowPrice <= _buySl);
	if (buyClose)
	{
	SellMarket(Math.Abs(Position));
	_inBuyTrade = false;
	}
	
	var sellClose = _inSellTrade && (candle.LowPrice <= _sellTp || candle.HighPrice >= _sellSl);
	if (sellClose)
	{
	BuyMarket(Math.Abs(Position));
	_inSellTrade = false;
	}
	}
	
	private void ShiftPoints(decimal value)
	{
	_x = _a;
	_a = _b;
	_b = _c;
	_c = _d;
	_d = value;
	}
	
	private bool IsAbcd(int mode)
	{
	if (_x is null || _a is null || _b is null || _c is null || _d is null)
	return false;
	
	var xab = Math.Abs(_b.Value - _a.Value) / Math.Abs(_x.Value - _a.Value);
	var abc = Math.Abs(_b.Value - _c.Value) / Math.Abs(_a.Value - _b.Value);
	var bcd = Math.Abs(_c.Value - _d.Value) / Math.Abs(_b.Value - _c.Value);
	
	var abcCond = abc >= 0.382m && abc <= 0.886m;
	var bcdCond = bcd >= 1.13m && bcd <= 2.618m;
	var dirCond = mode == 1 ? _d < _c : _d > _c;
	
	return xab > 0 && abcCond && bcdCond && dirCond;
	}
	}
	
