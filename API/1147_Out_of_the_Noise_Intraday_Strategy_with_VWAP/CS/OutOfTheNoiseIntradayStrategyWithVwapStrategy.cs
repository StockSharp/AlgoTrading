using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Out of the Noise intraday strategy with VWAP.
/// </summary>
public class OutOfTheNoiseIntradayStrategyWithVwapStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<bool> _exitAtEod;
	private readonly StrategyParam<decimal> _leverageLong;
	private readonly StrategyParam<decimal> _leverageShort;
	private readonly StrategyParam<bool> _useVolTarget;
	private readonly StrategyParam<decimal> _volTarget;
	private readonly StrategyParam<DataType> _candleType;
	
	private VWAP _vwap;
	private StandardDeviation _dailyStd;
	
	private DateTime _currentDay;
	private decimal _currentDayOpen;
	private decimal _prevClose;
	private decimal _upperBase;
	private decimal _lowerBase;
	private decimal _upperBound;
	private decimal _lowerBound;
	private int _barIndex;
	
	private decimal _prevDailyClose;
	private decimal _currentVolatility;
	
	private readonly Dictionary<int, Queue<decimal>> _moves = new();
	private readonly Dictionary<int, decimal> _sums = new();
	
	/// <summary>
	/// Number of days for average move.
	/// </summary>
	public int Period { get => _period.Value; set => _period.Value = value; }
	
	/// <summary>
	/// Close positions at end of day.
	/// </summary>
	public bool ExitAtEndOfSession { get => _exitAtEod.Value; set => _exitAtEod.Value = value; }
	
	/// <summary>
	/// Maximum long leverage.
	/// </summary>
	public decimal LeverageLong { get => _leverageLong.Value; set => _leverageLong.Value = value; }
	
	/// <summary>
	/// Maximum short leverage.
	/// </summary>
	public decimal LeverageShort { get => _leverageShort.Value; set => _leverageShort.Value = value; }
	
	/// <summary>
	/// Use volatility based position sizing.
	/// </summary>
	public bool UseVolatilityTarget { get => _useVolTarget.Value; set => _useVolTarget.Value = value; }
	
	/// <summary>
	/// Target volatility level.
	/// </summary>
	public decimal VolatilityTarget { get => _volTarget.Value; set => _volTarget.Value = value; }
	
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="OutOfTheNoiseIntradayStrategyWithVwapStrategy"/>.
	/// </summary>
	public OutOfTheNoiseIntradayStrategyWithVwapStrategy()
	{
	_period = Param(nameof(Period), 14)
	.SetGreaterThanZero()
	.SetDisplay("Period", "Number of days for average move", "General")
	.SetCanOptimize(true)
	.SetOptimize(10, 30, 1);
	
	_exitAtEod = Param(nameof(ExitAtEndOfSession), true)
	.SetDisplay("Exit at End", "Close positions at end of day", "General");
	
	_leverageLong = Param(nameof(LeverageLong), 4m)
	.SetGreaterThanZero()
	.SetDisplay("Leverage Long", "Maximum long leverage", "Trading");
	
	_leverageShort = Param(nameof(LeverageShort), 4m)
	.SetGreaterThanZero()
	.SetDisplay("Leverage Short", "Maximum short leverage", "Trading");
	
	_useVolTarget = Param(nameof(UseVolatilityTarget), true)
	.SetDisplay("Use Volatility Target", "Enable volatility based position sizing", "Risk");
	
	_volTarget = Param(nameof(VolatilityTarget), 0.02m)
	.SetGreaterThanZero()
	.SetDisplay("Volatility Target", "Target daily volatility", "Risk");
	
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
	.SetDisplay("Candle Type", "Intraday candle timeframe", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	yield return (Security, CandleType);
	yield return (Security, TimeSpan.FromDays(1).TimeFrame());
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();
	
	_currentDay = DateTime.MinValue;
	_currentDayOpen = 0m;
	_prevClose = 0m;
	_upperBase = 0m;
	_lowerBase = 0m;
	_upperBound = 0m;
	_lowerBound = 0m;
	_barIndex = 0;
	_prevDailyClose = 0m;
	_currentVolatility = 0m;
	_moves.Clear();
	_sums.Clear();
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);
	
	_vwap = new VWAP();
	_dailyStd = new StandardDeviation { Length = Period };
	
	var candleSub = SubscribeCandles(CandleType);
	candleSub.Bind(_vwap, ProcessCandle).Start();
	
	var dailySub = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
	dailySub.Bind(ProcessDailyCandle).Start();
	
	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, candleSub);
	DrawOwnTrades(area);
	}
	}
	
	private void ProcessDailyCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
	return;
	
	if (_prevDailyClose != 0m)
	{
	var ret = candle.ClosePrice / _prevDailyClose - 1m;
	var value = _dailyStd.Process(ret);
	if (value.IsFinal && value.GetValue<decimal>() is decimal vol)
	_currentVolatility = vol;
	}
	
	_prevDailyClose = candle.ClosePrice;
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal vwap)
	{
	if (candle.State != CandleStates.Finished)
	return;
	
	var day = candle.OpenTime.Date;
	var isNewDay = day != _currentDay;
	
	if (isNewDay)
	{
	if (ExitAtEndOfSession)
	CloseAll();
	
	_currentDay = day;
	_currentDayOpen = candle.OpenPrice;
	_upperBase = _prevClose == 0m ? candle.OpenPrice : Math.Max(candle.OpenPrice, _prevClose);
	_lowerBase = _prevClose == 0m ? candle.OpenPrice : Math.Min(candle.OpenPrice, _prevClose);
	_barIndex = 0;
	}
	
	var queue = _moves.TryGetValue(_barIndex, out var q) ? q : _moves[_barIndex] = new Queue<decimal>();
	var sum = _sums.TryGetValue(_barIndex, out var s) ? s : 0m;
	
	if (queue.Count == Period && IsFormedAndOnlineAndAllowTrading())
	{
	var avgMove = sum / queue.Count;
	_upperBound = _upperBase * (1 + avgMove);
	_lowerBound = _lowerBase * (1 - avgMove);
	
	var posTarget = _currentVolatility > 0m ? VolatilityTarget / _currentVolatility : 0m;
	
	if (Position <= 0)
	{
	if (candle.ClosePrice > _upperBound && (!ExitAtEndOfSession || _barIndex > 0))
	{
	var target = UseVolatilityTarget && posTarget > 0m ? Math.Min(LeverageLong, posTarget) : LeverageLong;
	var qty = (Portfolio?.CurrentValue ?? 0m) * target / candle.ClosePrice;
	if (qty > 0m)
	BuyMarket(qty + Math.Max(0m, -Position));
	}
	
	if (Position < 0 && candle.ClosePrice > Math.Min(vwap, _lowerBound))
	BuyMarket(Math.Abs(Position));
	}
	
	if (Position >= 0)
	{
	if (candle.ClosePrice < _lowerBound && (!ExitAtEndOfSession || _barIndex > 0))
	{
	var target = UseVolatilityTarget && posTarget > 0m ? Math.Min(LeverageShort, posTarget) : LeverageShort;
	var qty = (Portfolio?.CurrentValue ?? 0m) * target / candle.ClosePrice;
	if (qty > 0m)
	SellMarket(qty + Math.Max(0m, Position));
	}
	
	if (Position > 0 && candle.ClosePrice < Math.Max(vwap, _upperBound))
	SellMarket(Position);
	}
	}
	
	var move = _currentDayOpen == 0m ? 0m : Math.Abs(candle.ClosePrice / _currentDayOpen - 1m);
	queue.Enqueue(move);
	sum += move;
	if (queue.Count > Period)
	sum -= queue.Dequeue();
	
	_sums[_barIndex] = sum;
	
	_prevClose = candle.ClosePrice;
	_barIndex++;
	}
	}
