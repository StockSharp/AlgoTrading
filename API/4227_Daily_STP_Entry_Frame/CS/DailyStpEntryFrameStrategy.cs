using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily breakout strategy that enters on price crossing previous session extremes.
/// Converted from "Daily STP Entry Frame" MetaTrader expert.
/// Uses market orders when price breaks above previous high or below previous low.
/// </summary>
public class DailyStpEntryFrameStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private decimal _pipSize;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;

	private decimal? _previousDayHigh;
	private decimal? _previousDayLow;
	private decimal _currentDayHigh;
	private decimal _currentDayLow;
	private DateTime? _currentTradingDay;
	private bool _tradedToday;

	private decimal _entryPrice;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public DailyStpEntryFrameStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time-frame for monitoring", "General");

		_stopLossPoints = Param(nameof(StopLossPoints), 80m)
			.SetNotNegative()
			.SetDisplay("Stop-Loss (points)", "Stop-loss distance", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 200m)
			.SetNotNegative()
			.SetDisplay("Take-Profit (points)", "Take-profit distance", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_pipSize = 0m;
		_stopLossOffset = 0m;
		_takeProfitOffset = 0m;
		_previousDayHigh = null;
		_previousDayLow = null;
		_currentDayHigh = 0m;
		_currentDayLow = 0m;
		_currentTradingDay = null;
		_tradedToday = false;
		_entryPrice = 0m;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pipSize = Security?.PriceStep ?? 0.01m;
		if (_pipSize <= 0) _pipSize = 0.01m;

		_stopLossOffset = StopLossPoints * _pipSize;
		_takeProfitOffset = TakeProfitPoints * _pipSize;

		_previousDayHigh = null;
		_previousDayLow = null;
		_currentTradingDay = null;
		_tradedToday = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var date = candle.OpenTime.Date;

		// Track daily high/low
		if (_currentTradingDay != date)
		{
			// Save previous day's range
			if (_currentTradingDay != null)
			{
				_previousDayHigh = _currentDayHigh;
				_previousDayLow = _currentDayLow;
			}

			_currentTradingDay = date;
			_currentDayHigh = candle.HighPrice;
			_currentDayLow = candle.LowPrice;
			_tradedToday = false;
		}
		else
		{
			_currentDayHigh = Math.Max(_currentDayHigh, candle.HighPrice);
			_currentDayLow = Math.Min(_currentDayLow, candle.LowPrice);
		}

		// Manage existing position
		ManagePosition(candle);

		// Check for breakout entries
		if (_previousDayHigh is null || _previousDayLow is null)
			return;

		if (_tradedToday || Position != 0)
			return;

		var close = candle.ClosePrice;

		// Breakout above previous day high => buy
		if (close > _previousDayHigh.Value)
		{
			_entryPrice = close;
			_longStop = _stopLossOffset > 0 ? close - _stopLossOffset : null;
			_longTake = _takeProfitOffset > 0 ? close + _takeProfitOffset : null;
			_shortStop = null;
			_shortTake = null;
			BuyMarket();
			_tradedToday = true;
		}
		// Breakout below previous day low => sell
		else if (close < _previousDayLow.Value)
		{
			_entryPrice = close;
			_shortStop = _stopLossOffset > 0 ? close + _stopLossOffset : null;
			_shortTake = _takeProfitOffset > 0 ? close - _takeProfitOffset : null;
			_longStop = null;
			_longTake = null;
			SellMarket();
			_tradedToday = true;
		}
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStop is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket();
				_longStop = null;
				_longTake = null;
				return;
			}
			if (_longTake is decimal take && candle.HighPrice >= take)
			{
				SellMarket();
				_longStop = null;
				_longTake = null;
			}
		}
		else if (Position < 0)
		{
			if (_shortStop is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket();
				_shortStop = null;
				_shortTake = null;
				return;
			}
			if (_shortTake is decimal take && candle.LowPrice <= take)
			{
				BuyMarket();
				_shortStop = null;
				_shortTake = null;
			}
		}
	}
}
