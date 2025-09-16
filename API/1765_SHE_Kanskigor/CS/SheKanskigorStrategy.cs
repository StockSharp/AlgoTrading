using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily strategy that trades once per day based on previous candle direction.
/// Buys when the prior day's open is above the close, sells when below.
/// Applies fixed take profit and stop loss in price steps.
/// </summary>
public class SheKanskigorStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;
	
	private readonly DataType _dailyType;
	
	private bool _tradedToday;
	private DateTime _currentDate;
	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _dailyReady;
	private decimal _entryPrice;
	
	/// <summary>
	/// Time of day when the position opens.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}
	
	/// <summary>
	/// Profit target in price steps.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}
	
	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}
	
	/// <summary>
	/// Working candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes <see cref="SheKanskigorStrategy"/>.
	/// </summary>
	public SheKanskigorStrategy()
	{
		Param(nameof(Volume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "General");
		
		_takeProfit = Param(nameof(TakeProfit), 350m)
		.SetDisplay("Take Profit", "Profit target in steps", "Risk");
		
		_stopLoss = Param(nameof(StopLoss), 550m)
		.SetDisplay("Stop Loss", "Loss limit in steps", "Risk");
		
		_startTime = Param(nameof(StartTime), new TimeSpan(0, 5, 0))
		.SetDisplay("Start Time", "Daily time to open", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Intraday candle type", "General");
		
		_dailyType = TimeSpan.FromDays(1).TimeFrame();
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, _dailyType)];
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_tradedToday = false;
		_currentDate = default;
		_prevOpen = 0m;
		_prevClose = 0m;
		_dailyReady = false;
		_entryPrice = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var intraday = SubscribeCandles(CandleType);
		intraday.Bind(ProcessCandle).Start();
		
		var daily = SubscribeCandles(_dailyType);
		daily.Bind(ProcessDaily).Start();
		
		StartProtection();
	}
	
	private void ProcessDaily(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_dailyReady = true;
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var date = candle.OpenTime.Date;
		if (date != _currentDate)
		{
			_currentDate = date;
			_tradedToday = false;
		}
		
		// manage existing position
		if (Position > 0)
		{
			var tp = _entryPrice + TakeProfit * Security.PriceStep;
			var sl = _entryPrice - StopLoss * Security.PriceStep;
			
			if ((TakeProfit > 0 && candle.ClosePrice >= tp) ||
			(StopLoss > 0 && candle.ClosePrice <= sl))
			{
				SellMarket(Position);
				_entryPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			var tp = _entryPrice - TakeProfit * Security.PriceStep;
			var sl = _entryPrice + StopLoss * Security.PriceStep;
			
			if ((TakeProfit > 0 && candle.ClosePrice <= tp) ||
			(StopLoss > 0 && candle.ClosePrice >= sl))
			{
				BuyMarket(-Position);
				_entryPrice = 0m;
			}
		}
		
		var now = candle.OpenTime.TimeOfDay;
		var start = StartTime;
		var end = start + TimeSpan.FromMinutes(5);
		
		if (now < start || now > end || _tradedToday)
		return;
		
		if (Position != 0)
		{
			_tradedToday = true;
			return;
		}
		
		if (!_dailyReady)
		return;
		
		if (_prevOpen > _prevClose)
		{
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_tradedToday = true;
		}
		else if (_prevOpen < _prevClose)
		{
			SellMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_tradedToday = true;
		}
		else
		{
			_tradedToday = true;
		}
	}
}
