using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Session breakout scalper strategy.
/// Tracks high and low of a user defined session and trades breakouts with ATR-based stop.
/// </summary>
public class SessionBreakoutScalperTradingBotStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _useAtrStop;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _sessionHigh;
	private decimal _sessionLow;
	private DateTime _currentDay;
	private bool _rangeReady;
	private decimal? _entryPrice;
	
	/// <summary>
	/// Start of the session.
	/// </summary>
	public TimeSpan SessionStart { get => _sessionStart.Value; set => _sessionStart.Value = value; }
	
	/// <summary>
	/// End of the session.
	/// </summary>
	public TimeSpan SessionEnd { get => _sessionEnd.Value; set => _sessionEnd.Value = value; }
	
	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	
	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	
	/// <summary>
	/// Use ATR for stop calculation.
	/// </summary>
	public bool UseAtrStop { get => _useAtrStop.Value; set => _useAtrStop.Value = value; }
	
	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	
	/// <summary>
	/// ATR multiplier.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public SessionBreakoutScalperTradingBotStrategy()
	{
		_sessionStart = Param(nameof(SessionStart), TimeSpan.FromHours(1)).SetDisplay("Session Start").SetCanOptimize(true);
		_sessionEnd = Param(nameof(SessionEnd), TimeSpan.FromHours(2)).SetDisplay("Session End").SetCanOptimize(true);
		_takeProfit = Param(nameof(TakeProfit), 100m).SetDisplay("Take Profit").SetCanOptimize(true);
		_stopLoss = Param(nameof(StopLoss), 50m).SetDisplay("Stop Loss").SetCanOptimize(true);
		_useAtrStop = Param(nameof(UseAtrStop), true).SetDisplay("Use ATR Stop").SetCanOptimize(true);
		_atrLength = Param(nameof(AtrLength), 14).SetDisplay("ATR Length").SetCanOptimize(true);
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m).SetDisplay("ATR Multiplier").SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type").SetCanOptimize(true);
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		var atr = new AverageTrueRange { Length = AtrLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(atr, ProcessCandle)
		.Start();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var day = candle.OpenTime.Date;
		var time = candle.OpenTime.TimeOfDay;
		
		if (day != _currentDay)
		{
			_currentDay = day;
			_sessionHigh = decimal.MinValue;
			_sessionLow = decimal.MaxValue;
			_rangeReady = false;
			_entryPrice = null;
		}
		
		if (time >= SessionStart && time < SessionEnd)
		{
			_sessionHigh = Math.Max(_sessionHigh, candle.HighPrice);
			_sessionLow = Math.Min(_sessionLow, candle.LowPrice);
			return;
		}
		
		if (time >= SessionEnd)
		_rangeReady = true;
		
		if (_rangeReady && _entryPrice is null && Position == 0)
		{
			if (candle.ClosePrice > _sessionHigh)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (candle.ClosePrice < _sessionLow)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}
		
		if (_entryPrice is not decimal price)
		return;
		
		var stop = UseAtrStop ? atr * AtrMultiplier : StopLoss;
		var tp = TakeProfit;
		
		if (Position > 0)
		{
			if (candle.LowPrice <= price - stop || candle.HighPrice >= price + tp)
			{
				SellMarket();
				_entryPrice = null;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= price + stop || candle.LowPrice <= price - tp)
			{
				BuyMarket();
				_entryPrice = null;
			}
		}
	}
}
