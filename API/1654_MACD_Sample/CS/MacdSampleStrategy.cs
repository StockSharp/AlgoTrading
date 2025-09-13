using System;
using System.Collections.Generic;
	
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
	
namespace StockSharp.Samples.Strategies;
	
	/// <summary>
	/// MACD sample strategy with EMA trend filter and trailing stop.
	/// Based on original MACD Sample from MQL4.
	/// </summary>
	public class MacdSampleStrategy : Strategy
	{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maTrendPeriod;
	private readonly StrategyParam<decimal> _macdOpenLevel;
	private readonly StrategyParam<decimal> _macdCloseLevel;
	private readonly StrategyParam<decimal> _takeProfitLong;
	private readonly StrategyParam<decimal> _takeProfitShort;
	private readonly StrategyParam<decimal> _stopLossLong;
	private readonly StrategyParam<decimal> _stopLossShort;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	
	private decimal _entryPrice;
	private decimal _trailingStopLevel;
	private decimal _prevMacd;
	private decimal _prevSignal;
	private decimal _prevEma;
	
	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// EMA period used as trend filter.
	/// </summary>
	public int MaTrendPeriod { get => _maTrendPeriod.Value; set => _maTrendPeriod.Value = value; }
	
	/// <summary>
	/// Minimum MACD distance from zero for entries.
	/// </summary>
	public decimal MacdOpenLevel { get => _macdOpenLevel.Value; set => _macdOpenLevel.Value = value; }
	
	/// <summary>
	/// Minimum MACD distance from zero for exits.
	/// </summary>
	public decimal MacdCloseLevel { get => _macdCloseLevel.Value; set => _macdCloseLevel.Value = value; }
	
	/// <summary>
	/// Take profit distance for long positions.
	/// </summary>
	public decimal TakeProfitLong { get => _takeProfitLong.Value; set => _takeProfitLong.Value = value; }
	
	/// <summary>
	/// Take profit distance for short positions.
	/// </summary>
	public decimal TakeProfitShort { get => _takeProfitShort.Value; set => _takeProfitShort.Value = value; }
	
	/// <summary>
	/// Stop loss distance for long positions.
	/// </summary>
	public decimal StopLossLong { get => _stopLossLong.Value; set => _stopLossLong.Value = value; }
	
	/// <summary>
	/// Stop loss distance for short positions.
	/// </summary>
	public decimal StopLossShort { get => _stopLossShort.Value; set => _stopLossShort.Value = value; }
	
	/// <summary>
	/// Trailing stop distance.
	/// </summary>
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	
	/// <summary>
	/// UTC hour to start trading.
	/// </summary>
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	
	/// <summary>
	/// UTC hour to stop trading.
	/// </summary>
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of <see cref="MacdSampleStrategy"/>.
	/// </summary>
	public MacdSampleStrategy()
	{
	_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
	.SetDisplay("Candle Type", "Type of candles to use", "General");
	
	_maTrendPeriod = Param(nameof(MaTrendPeriod), 26)
	.SetGreaterThanZero()
	.SetDisplay("EMA Period", "EMA trend filter period", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(10, 50, 5);
	
	_macdOpenLevel = Param(nameof(MacdOpenLevel), 3m)
	.SetGreaterThanZero()
	.SetDisplay("MACD Open", "MACD distance from zero for entry", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(1m, 5m, 0.5m);
	
	_macdCloseLevel = Param(nameof(MacdCloseLevel), 2m)
	.SetGreaterThanZero()
	.SetDisplay("MACD Close", "MACD distance from zero for exit", "Indicators")
	.SetCanOptimize(true)
	.SetOptimize(1m, 4m, 0.5m);
	
	_takeProfitLong = Param(nameof(TakeProfitLong), 50m)
	.SetGreaterThanZero()
	.SetDisplay("Long Take Profit", "Take profit distance for longs", "Risk")
	.SetCanOptimize(true)
	.SetOptimize(20m, 100m, 10m);
	
	_takeProfitShort = Param(nameof(TakeProfitShort), 75m)
	.SetGreaterThanZero()
	.SetDisplay("Short Take Profit", "Take profit distance for shorts", "Risk")
	.SetCanOptimize(true)
	.SetOptimize(20m, 100m, 10m);
	
	_stopLossLong = Param(nameof(StopLossLong), 80m)
	.SetGreaterThanZero()
	.SetDisplay("Long Stop Loss", "Stop loss distance for longs", "Risk")
	.SetCanOptimize(true)
	.SetOptimize(20m, 100m, 10m);
	
	_stopLossShort = Param(nameof(StopLossShort), 50m)
	.SetGreaterThanZero()
	.SetDisplay("Short Stop Loss", "Stop loss distance for shorts", "Risk")
	.SetCanOptimize(true)
	.SetOptimize(20m, 100m, 10m);
	
	_trailingStop = Param(nameof(TrailingStop), 30m)
	.SetGreaterThanZero()
	.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk")
	.SetCanOptimize(true)
	.SetOptimize(10m, 60m, 10m);
	
	_startHour = Param(nameof(StartHour), 4)
	.SetDisplay("Start Hour", "Trading start hour (UTC)", "General");
	
	_endHour = Param(nameof(EndHour), 19)
	.SetDisplay("End Hour", "Trading end hour (UTC)", "General");
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
	_trailingStopLevel = 0m;
	_prevMacd = 0m;
	_prevSignal = 0m;
	_prevEma = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);
	
	var macd = new MovingAverageConvergenceDivergenceSignal();
	var ema = new EMA { Length = MaTrendPeriod };
	
	var subscription = SubscribeCandles(CandleType);
	
	subscription
	.BindEx(macd, ema, ProcessCandle)
	.Start();
	
	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, macd);
	DrawIndicator(area, ema);
	DrawOwnTrades(area);
	}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue emaValue)
	{
	if (candle.State != CandleStates.Finished)
	return;
	
	if (!IsFormedAndOnlineAndAllowTrading())
	return;
	
	var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
	var macd = macdTyped.Macd;
	var signal = macdTyped.Signal;
	var ema = emaValue.GetValue<decimal>();
	
	var hour = candle.OpenTime.UtcDateTime.Hour;
	var inHours = hour >= StartHour && hour <= EndHour;
	
	if (!inHours)
	return;
	
	if (Position == 0)
	{
	// Entry conditions
	if (macd < 0 && macd > signal && _prevMacd < _prevSignal && Math.Abs(macd) > MacdOpenLevel && ema > _prevEma)
	{
	_entryPrice = candle.ClosePrice;
	_trailingStopLevel = _entryPrice - StopLossLong;
	BuyMarket(Volume);
	LogInfo($"Enter long at {_entryPrice:F5}");
	}
	else if (macd > 0 && macd < signal && _prevMacd > _prevSignal && macd > MacdOpenLevel && ema < _prevEma)
	{
	_entryPrice = candle.ClosePrice;
	_trailingStopLevel = _entryPrice + StopLossShort;
	SellMarket(Volume);
	LogInfo($"Enter short at {_entryPrice:F5}");
	}
	}
	else if (Position > 0)
	{
	// Long position exit rules
	if (macd > 0 && macd < signal && _prevMacd > _prevSignal && Math.Abs(macd) > MacdCloseLevel)
	{
	SellMarket(Position);
	LogInfo("Exit long due to MACD cross");
	}
	else
	{
	var price = candle.ClosePrice;
	if (price - _entryPrice >= TakeProfitLong || price <= _entryPrice - StopLossLong)
	{
	SellMarket(Position);
	LogInfo("Exit long due to TP or SL");
	}
	else
	{
	if (price - _entryPrice > TrailingStop)
	_trailingStopLevel = Math.Max(_trailingStopLevel, price - TrailingStop);
	
	if (price <= _trailingStopLevel)
	{
	SellMarket(Position);
	LogInfo("Exit long due to trailing stop");
	}
	}
	}
	}
	else if (Position < 0)
	{
	// Short position exit rules
	if (macd < 0 && macd > signal && _prevMacd < _prevSignal && Math.Abs(macd) > MacdCloseLevel)
	{
	BuyMarket(Math.Abs(Position));
	LogInfo("Exit short due to MACD cross");
	}
	else
	{
	var price = candle.ClosePrice;
	if (_entryPrice - price >= TakeProfitShort || price >= _entryPrice + StopLossShort)
	{
	BuyMarket(Math.Abs(Position));
	LogInfo("Exit short due to TP or SL");
	}
	else
	{
	if (_entryPrice - price > TrailingStop)
	_trailingStopLevel = Math.Min(_trailingStopLevel, price + TrailingStop);
	
	if (price >= _trailingStopLevel)
	{
	BuyMarket(Math.Abs(Position));
	LogInfo("Exit short due to trailing stop");
	}
	}
	}
	}
	
	_prevMacd = macd;
	_prevSignal = signal;
	_prevEma = ema;
	}
	}
