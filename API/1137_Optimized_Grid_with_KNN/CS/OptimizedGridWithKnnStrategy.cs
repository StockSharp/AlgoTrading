using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// T3 crossover strategy with KNN based adaptive thresholds.
/// </summary>
public class OptimizedGridWithKnnStrategy : Strategy
{
	private readonly StrategyParam<decimal> _protect;
	private readonly StrategyParam<decimal> _profit;
	private readonly StrategyParam<int> _k;
	private readonly StrategyParam<int> _windowSize;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private ExponentialMovingAverage _fastEma1;
	private ExponentialMovingAverage _fastEma2;
	private ExponentialMovingAverage _fastEma3;
	private ExponentialMovingAverage _fastEma4;
	private ExponentialMovingAverage _fastEma5;
	private ExponentialMovingAverage _fastEma6;

	private ExponentialMovingAverage _slowEma1;
	private ExponentialMovingAverage _slowEma2;
	private ExponentialMovingAverage _slowEma3;
	private ExponentialMovingAverage _slowEma4;
	private ExponentialMovingAverage _slowEma5;
	private ExponentialMovingAverage _slowEma6;

	private readonly List<decimal> _closes = new();
	private decimal _prevT3Fast;
	private decimal _prevT3Slow;
	private decimal _lastEntryPrice;
	private int _openTrades;

	public decimal Protect { get => _protect.Value; set => _protect.Value = value; }
	public decimal Profit { get => _profit.Value; set => _profit.Value = value; }
	public int K { get => _k.Value; set => _k.Value = value; }
	public int WindowSize { get => _windowSize.Value; set => _windowSize.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public DateTimeOffset StartTime { get => _startTime.Value; set => _startTime.Value = value; }
	public DateTimeOffset EndTime { get => _endTime.Value; set => _endTime.Value = value; }

	public OptimizedGridWithKnnStrategy()
	{
		_protect = Param(nameof(Protect), 0.03m)
			.SetDisplay("Protect", "Entry threshold", "Grid");

		_profit = Param(nameof(Profit), 0.05m)
			.SetDisplay("Profit", "Exit threshold", "Grid");

		_k = Param(nameof(K), 5)
			.SetGreaterThanZero()
			.SetDisplay("K", "Number of neighbors", "KNN");

		_windowSize = Param(nameof(WindowSize), 20)
			.SetGreaterThanZero()
			.SetDisplay("Window Size", "Lookback window", "KNN");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_startTime = Param(nameof(StartTime), new DateTimeOffset(2022, 12, 5, 3, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Time", "Backtest start", "Back Test");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(2099, 1, 1, 23, 59, 0, TimeSpan.Zero))
			.SetDisplay("End Time", "Backtest end", "Back Test");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_fastEma1 = _fastEma2 = _fastEma3 = _fastEma4 = _fastEma5 = _fastEma6 = null;
		_slowEma1 = _slowEma2 = _slowEma3 = _slowEma4 = _slowEma5 = _slowEma6 = null;
		_closes.Clear();
		_prevT3Fast = _prevT3Slow = 0m;
		_lastEntryPrice = 0m;
		_openTrades = 0;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma1 = new() { Length = 5 };
		_fastEma2 = new() { Length = 5 };
		_fastEma3 = new() { Length = 5 };
		_fastEma4 = new() { Length = 5 };
		_fastEma5 = new() { Length = 5 };
		_fastEma6 = new() { Length = 5 };

		_slowEma1 = new() { Length = 8 };
		_slowEma2 = new() { Length = 8 };
		_slowEma3 = new() { Length = 8 };
		_slowEma4 = new() { Length = 8 };
		_slowEma5 = new() { Length = 8 };
		_slowEma6 = new() { Length = 8 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;
		var close = candle.ClosePrice;

		var t3Fast = CalcT3(_fastEma1, _fastEma2, _fastEma3, _fastEma4, _fastEma5, _fastEma6, close, time);
		var t3Slow = CalcT3(_slowEma1, _slowEma2, _slowEma3, _slowEma4, _slowEma5, _slowEma6, close, time);

		_closes.Add(close);

		decimal averageChange = 0m;
		if (_closes.Count >= K + 1)
		{
			var sum = 0m;
			for (var i = 0; i < K; i++)
				sum += _closes[^1 - i] - _closes[^2 - i];
			averageChange = sum / K;
		}

		var adjustedOpenTh = Protect;
		var adjustedCloseTh = Profit;
		if (averageChange > 0)
		{
			adjustedOpenTh *= 1.1m;
			adjustedCloseTh *= 1.1m;
		}
		else
		{
			adjustedOpenTh *= 0.9m;
			adjustedCloseTh *= 0.9m;
		}

		var longCondition = _prevT3Fast <= _prevT3Slow && t3Fast > t3Slow && averageChange > 0;
		var shortCondition = _prevT3Fast >= _prevT3Slow && t3Fast < t3Slow && averageChange < 0;

		if (time > StartTime && time < EndTime && longCondition)
		{
			if (_openTrades == 0 || (_openTrades > 0 && (close - _lastEntryPrice) / _lastEntryPrice < -adjustedOpenTh * 0.5m))
			{
				BuyMarket();
				_openTrades++;
				_lastEntryPrice = close;
			}
		}
		else if (time > StartTime && time < EndTime && shortCondition && _openTrades > 0 &&
		(close - _lastEntryPrice) / _lastEntryPrice > adjustedCloseTh)
		{
			SellMarket(Position);
			_openTrades = 0;
		}

		_prevT3Fast = t3Fast;
		_prevT3Slow = t3Slow;
	}

	private static decimal CalcT3(
	ExponentialMovingAverage ema1,
	ExponentialMovingAverage ema2,
	ExponentialMovingAverage ema3,
	ExponentialMovingAverage ema4,
	ExponentialMovingAverage ema5,
	ExponentialMovingAverage ema6,
	decimal price,
	DateTimeOffset time)
	{
	var e1 = ema1.Process(price, time, true).ToDecimal();
	var e2 = ema2.Process(e1, time, true).ToDecimal();
	var e3 = ema3.Process(e2, time, true).ToDecimal();
	var e4 = ema4.Process(e3, time, true).ToDecimal();
	var e5 = ema5.Process(e4, time, true).ToDecimal();
	var e6 = ema6.Process(e5, time, true).ToDecimal();

	const decimal ab = 0.7m;
	var ac1 = -ab * ab * ab;
	var ac2 = 3m * ab * ab + 3m * ab * ab * ab;
	var ac3 = -6m * ab * ab - 3m * ab - 3m * ab * ab * ab;
	var ac4 = 1m + 3m * ab + ab * ab * ab + 3m * ab * ab;

	return ac1 * e6 + ac2 * e5 + ac3 * e4 + ac4 * e3;
	}
}
