using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FX-CHAOS scalp strategy adapted to the StockSharp high-level API.
/// </summary>
public class FxChaosScalpStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _tradingCandleType;
	private readonly StrategyParam<DataType> _dailyCandleType;

	private AwesomeOscillator _awesomeOscillator;
	private FractalZigZagTracker _hourlyTracker;
	private FractalZigZagTracker _dailyTracker;

	private decimal _previousHigh;
	private decimal _previousLow;
	private bool _hasPrevious;

	private decimal _entryPrice;
	private bool _hasEntry;

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
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

	public DataType TradingCandleType
	{
		get => _tradingCandleType.Value;
		set => _tradingCandleType.Value = value;
	}

	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	public FxChaosScalpStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume in lots", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetDisplay("Stop Loss (pts)", "Stop loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetDisplay("Take Profit (pts)", "Take profit distance in points", "Risk");

		_tradingCandleType = Param(nameof(TradingCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Trading Candle", "Primary trading timeframe", "General");

		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Daily Candle", "Higher timeframe for ZigZag filter", "General");

		_hourlyTracker = new FractalZigZagTracker();
		_dailyTracker = new FractalZigZagTracker();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, TradingCandleType),
			(Security, DailyCandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		Volume = OrderVolume;
		_hourlyTracker.Reset();
		_dailyTracker.Reset();
		_previousHigh = 0m;
		_previousLow = 0m;
		_hasPrevious = false;
		_entryPrice = 0m;
		_hasEntry = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_awesomeOscillator = new AwesomeOscillator
		{
			ShortPeriod = 5,
			LongPeriod = 34
		};

		var dailySubscription = SubscribeCandles(DailyCandleType);
		dailySubscription.Bind(ProcessDailyCandle).Start();

		var tradingSubscription = SubscribeCandles(TradingCandleType);
		tradingSubscription.BindEx(_awesomeOscillator, ProcessTradingCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradingSubscription);
			DrawIndicator(area, _awesomeOscillator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update higher timeframe ZigZag filter.
		_dailyTracker.Update(candle);
	}

	private void ProcessTradingCandle(ICandleMessage candle, IIndicatorValue aoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track ZigZag swings for the trading timeframe.
		_hourlyTracker.Update(candle);

		if (ManageRisk(candle))
		{
			UpdatePreviousLevels(candle);
			return;
		}

		if (!_hasPrevious)
		{
			UpdatePreviousLevels(candle);
			return;
		}

		if (!aoValue.IsFinal)
		{
			UpdatePreviousLevels(candle);
			return;
		}

		if (_hourlyTracker.LastValue is not decimal hourlyZigZag ||
			_dailyTracker.LastValue is not decimal dailyZigZag)
		{
			UpdatePreviousLevels(candle);
			return;
		}

		var ao = aoValue.GetValue<decimal>();
		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		// Evaluate breakout conditions relative to previous levels and ZigZag bands.
		var longSignal = open < _previousHigh && close > _previousHigh && close < hourlyZigZag && ao < 0m && close > dailyZigZag;
		var shortSignal = open > _previousLow && close < _previousLow && close > hourlyZigZag && ao > 0m && close < dailyZigZag;

		if (longSignal && Position <= 0)
		{
			if (Position < 0)
			{
				ClosePosition();
				_hasEntry = false;
				_entryPrice = 0m;
			}

			if (Position <= 0)
			{
				BuyMarket();
				_entryPrice = close;
				_hasEntry = true;
			}
		}
		else if (shortSignal && Position >= 0)
		{
			if (Position > 0)
			{
				ClosePosition();
				_hasEntry = false;
				_entryPrice = 0m;
			}

			if (Position >= 0)
			{
				SellMarket();
				_entryPrice = close;
				_hasEntry = true;
			}
		}

		if (Position == 0)
		{
			_hasEntry = false;
			_entryPrice = 0m;
		}

		UpdatePreviousLevels(candle);
	}

	private void UpdatePreviousLevels(ICandleMessage candle)
	{
		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
		_hasPrevious = true;
	}

	private bool ManageRisk(ICandleMessage candle)
	{
		if (Position == 0)
		{
			_hasEntry = false;
			_entryPrice = 0m;
			return false;
		}

		if (!_hasEntry)
			return false;

		var step = GetPriceStep();

		if (Position > 0)
		{
			var stop = StopLossPoints > 0m ? _entryPrice - StopLossPoints * step : (decimal?)null;
			var take = TakeProfitPoints > 0m ? _entryPrice + TakeProfitPoints * step : (decimal?)null;

			if (stop is decimal stopPrice && candle.LowPrice <= stopPrice)
			{
				ClosePosition();
				_hasEntry = false;
				_entryPrice = 0m;
				return true;
			}

			if (take is decimal takePrice && candle.HighPrice >= takePrice)
			{
				ClosePosition();
				_hasEntry = false;
				_entryPrice = 0m;
				return true;
			}
		}
		else if (Position < 0)
		{
			var stop = StopLossPoints > 0m ? _entryPrice + StopLossPoints * step : (decimal?)null;
			var take = TakeProfitPoints > 0m ? _entryPrice - TakeProfitPoints * step : (decimal?)null;

			if (stop is decimal stopPrice && candle.HighPrice >= stopPrice)
			{
				ClosePosition();
				_hasEntry = false;
				_entryPrice = 0m;
				return true;
			}

			if (take is decimal takePrice && candle.LowPrice <= takePrice)
			{
				ClosePosition();
				_hasEntry = false;
				_entryPrice = 0m;
				return true;
			}
		}

		return false;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep;
		return step is decimal value && value > 0m ? value : 1m;
	}

	private sealed class FractalZigZagTracker
	{
		private const int WindowSize = 5;
		private readonly CandleInfo[] _window = new CandleInfo[WindowSize];
		private int _count;
		private decimal? _lastValue;
		private int _direction;

		public decimal? LastValue => _lastValue;

		public void Reset()
		{
			Array.Clear(_window);
			_count = 0;
			_lastValue = null;
			_direction = 0;
		}

		public decimal? Update(ICandleMessage candle)
		{
			if (_count < WindowSize)
			{
				_window[_count++] = new CandleInfo(candle.HighPrice, candle.LowPrice);
				if (_count < WindowSize)
					return _lastValue;

				Evaluate();
				return _lastValue;
			}

			for (var i = 0; i < WindowSize - 1; i++)
				_window[i] = _window[i + 1];

			_window[WindowSize - 1] = new CandleInfo(candle.HighPrice, candle.LowPrice);

			Evaluate();
			return _lastValue;
		}

		private void Evaluate()
		{
			if (_count < WindowSize)
				return;

			var c0 = _window[0];
			var c1 = _window[1];
			var c2 = _window[2];
			var c3 = _window[3];
			var c4 = _window[4];

			var isUp = c2.High > c1.High && c2.High > c0.High && c2.High >= c3.High && c2.High >= c4.High;
			var isDown = c2.Low < c1.Low && c2.Low < c0.Low && c2.Low <= c3.Low && c2.Low <= c4.Low;

			if (isUp)
			{
				if (_direction == 1)
				{
					if (_lastValue == null || c2.High > _lastValue.Value)
						_lastValue = c2.High;
				}
				else
				{
					_lastValue = c2.High;
					_direction = 1;
				}
			}
			else if (isDown)
			{
				if (_direction == -1)
				{
					if (_lastValue == null || c2.Low < _lastValue.Value)
						_lastValue = c2.Low;
				}
				else
				{
					_lastValue = c2.Low;
					_direction = -1;
				}
			}
		}

		private readonly struct CandleInfo
		{
			public CandleInfo(decimal high, decimal low)
			{
				High = high;
				Low = low;
			}

			public decimal High { get; }

			public decimal Low { get; }
		}
	}
}
