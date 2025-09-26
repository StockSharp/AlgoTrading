using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy inspired by TrueSort_1001 expert advisor.
/// Requires a strict moving average alignment and a rising ADX before entering trades.
/// Implements trailing stops measured in price steps and exits when the moving averages lose alignment.
/// </summary>
public class TrueSort1001Strategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _sma10Length;
	private readonly StrategyParam<int> _sma20Length;
	private readonly StrategyParam<int> _sma50Length;
	private readonly StrategyParam<int> _sma100Length;
	private readonly StrategyParam<int> _sma200Length;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _ma10History = new decimal[3];
	private readonly decimal[] _ma20History = new decimal[3];
	private readonly decimal[] _ma50History = new decimal[3];
	private readonly decimal[] _ma100History = new decimal[3];
	private readonly decimal[] _ma200History = new decimal[3];

	private int _historyCount;
	private bool _hasPreviousAdx;
	private decimal _previousAdx;
	private decimal _longStop;
	private decimal _shortStop;


	/// <summary>
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Length of the fastest SMA.
	/// </summary>
	public int Sma10Length
	{
		get => _sma10Length.Value;
		set => _sma10Length.Value = value;
	}

	/// <summary>
	/// Length of the second SMA.
	/// </summary>
	public int Sma20Length
	{
		get => _sma20Length.Value;
		set => _sma20Length.Value = value;
	}

	/// <summary>
	/// Length of the medium SMA.
	/// </summary>
	public int Sma50Length
	{
		get => _sma50Length.Value;
		set => _sma50Length.Value = value;
	}

	/// <summary>
	/// Length of the long-term SMA used for stop placement.
	/// </summary>
	public int Sma100Length
	{
		get => _sma100Length.Value;
		set => _sma100Length.Value = value;
	}

	/// <summary>
	/// Length of the slowest SMA used for trend confirmation.
	/// </summary>
	public int Sma200Length
	{
		get => _sma200Length.Value;
		set => _sma200Length.Value = value;
	}

	/// <summary>
	/// ADX calculation period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Minimum ADX value required for entries.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public TrueSort1001Strategy()
	{

		_stopLossPoints = Param(nameof(StopLossPoints), 100)
		.SetNotNegative()
		.SetDisplay("Stop Loss Points", "Trailing stop distance in price steps", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(20, 200, 20);

		_sma10Length = Param(nameof(Sma10Length), 10)
		.SetGreaterThanZero()
		.SetDisplay("SMA 10", "Fastest moving average length", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_sma20Length = Param(nameof(Sma20Length), 20)
		.SetGreaterThanZero()
		.SetDisplay("SMA 20", "Second moving average length", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 2);

		_sma50Length = Param(nameof(Sma50Length), 50)
		.SetGreaterThanZero()
		.SetDisplay("SMA 50", "Medium moving average length", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(30, 80, 5);

		_sma100Length = Param(nameof(Sma100Length), 100)
		.SetGreaterThanZero()
		.SetDisplay("SMA 100", "Long moving average length", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(80, 150, 5);

		_sma200Length = Param(nameof(Sma200Length), 200)
		.SetGreaterThanZero()
		.SetDisplay("SMA 200", "Slow moving average length", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(150, 250, 10);

		_adxPeriod = Param(nameof(AdxPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ADX Period", "ADX calculation length", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 2);

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
		.SetGreaterThanZero()
		.SetDisplay("ADX Threshold", "Minimum ADX value for trades", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(15m, 35m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Candles used for calculations", "General");
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

		Array.Clear(_ma10History, 0, _ma10History.Length);
		Array.Clear(_ma20History, 0, _ma20History.Length);
		Array.Clear(_ma50History, 0, _ma50History.Length);
		Array.Clear(_ma100History, 0, _ma100History.Length);
		Array.Clear(_ma200History, 0, _ma200History.Length);

		_historyCount = 0;
		_hasPreviousAdx = false;
		_previousAdx = 0m;
		_longStop = 0m;
		_shortStop = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma10 = new SimpleMovingAverage { Length = Sma10Length };
		var sma20 = new SimpleMovingAverage { Length = Sma20Length };
		var sma50 = new SimpleMovingAverage { Length = Sma50Length };
		var sma100 = new SimpleMovingAverage { Length = Sma100Length };
		var sma200 = new SimpleMovingAverage { Length = Sma200Length };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(sma10, sma20, sma50, sma100, sma200, adx, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma10);
			DrawIndicator(area, sma20);
			DrawIndicator(area, sma50);
			DrawIndicator(area, sma100);
			DrawIndicator(area, sma200);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(
	ICandleMessage candle,
	IIndicatorValue sma10Value,
	IIndicatorValue sma20Value,
	IIndicatorValue sma50Value,
	IIndicatorValue sma100Value,
	IIndicatorValue sma200Value,
	IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!sma10Value.IsFinal || !sma20Value.IsFinal || !sma50Value.IsFinal || !sma100Value.IsFinal || !sma200Value.IsFinal || !adxValue.IsFinal)
		return;

		var sma10 = sma10Value.ToDecimal();
		var sma20 = sma20Value.ToDecimal();
		var sma50 = sma50Value.ToDecimal();
		var sma100 = sma100Value.ToDecimal();
		var sma200 = sma200Value.ToDecimal();

		var adxData = (AverageDirectionalIndexValue)adxValue;
		if (adxData.MovingAverage is not decimal currentAdx)
		return;

		var previousAdx = _previousAdx;
		var hadPreviousAdx = _hasPreviousAdx;
		_previousAdx = currentAdx;
		_hasPreviousAdx = true;

		var step = Security?.Step ?? 1m;
		var stopDistance = StopLossPoints > 0 ? StopLossPoints * step : 0m;
		var hasHistory = _historyCount >= 3;
		var canTrade = IsFormedAndOnlineAndAllowTrading();

		if (Position == 0 && canTrade && hasHistory && hadPreviousAdx)
		{
			var adxRising = currentAdx > AdxThreshold && currentAdx > previousAdx;
			if (adxRising)
			{
				var bullishAligned = IsStrictlyDescending(_ma10History, _ma20History, _ma50History, _ma100History, _ma200History);
				var bearishAligned = IsStrictlyAscending(_ma10History, _ma20History, _ma50History, _ma100History, _ma200History);

				if (bullishAligned)
				{
					BuyMarket(Volume);
					_longStop = CalculateInitialLongStop(candle.ClosePrice, sma100, stopDistance);
					_shortStop = 0m;
				}
				else if (bearishAligned)
				{
					SellMarket(Volume);
					_shortStop = CalculateInitialShortStop(candle.ClosePrice, sma100, stopDistance);
					_longStop = 0m;
				}
			}
		}

		if (Position > 0)
		{
			var alignmentLost = sma10 <= sma20 || sma20 <= sma50 || sma50 <= sma100 || sma100 <= sma200;
			if (alignmentLost)
			{
				SellMarket(Math.Abs(Position));
				_longStop = 0m;
			}
			else if (stopDistance > 0m)
			{
				var candidate = candle.ClosePrice - stopDistance;
				if (candidate > _longStop)
				_longStop = candidate;

				if (_longStop != 0m && candle.ClosePrice <= _longStop)
				{
					SellMarket(Math.Abs(Position));
					_longStop = 0m;
				}
			}
		}
		else if (Position < 0)
		{
			var alignmentLost = sma10 >= sma20 || sma20 >= sma50 || sma50 >= sma100 || sma100 >= sma200;
			if (alignmentLost)
			{
				BuyMarket(Math.Abs(Position));
				_shortStop = 0m;
			}
			else if (stopDistance > 0m)
			{
				var candidate = candle.ClosePrice + stopDistance;
				if (_shortStop == 0m || candidate < _shortStop)
				_shortStop = candidate;

				if (_shortStop != 0m && candle.ClosePrice >= _shortStop)
				{
					BuyMarket(Math.Abs(Position));
					_shortStop = 0m;
				}
			}
		}

		UpdateHistory(sma10, sma20, sma50, sma100, sma200);
	}

	private static decimal CalculateInitialLongStop(decimal entryPrice, decimal sma100, decimal stopDistance)
	{
		var stop = sma100;
		if (stopDistance > 0m)
		{
			var distanceStop = entryPrice - stopDistance;
			if (stop == 0m || distanceStop > stop)
			stop = distanceStop;
		}

		return stop;
	}

	private static decimal CalculateInitialShortStop(decimal entryPrice, decimal sma100, decimal stopDistance)
	{
		var stop = sma100;
		if (stopDistance > 0m)
		{
			var distanceStop = entryPrice + stopDistance;
			if (stop == 0m || distanceStop < stop)
			stop = distanceStop;
		}

		return stop;
	}

	private void UpdateHistory(decimal sma10, decimal sma20, decimal sma50, decimal sma100, decimal sma200)
	{
		ShiftHistory(_ma10History, sma10);
		ShiftHistory(_ma20History, sma20);
		ShiftHistory(_ma50History, sma50);
		ShiftHistory(_ma100History, sma100);
		ShiftHistory(_ma200History, sma200);

		if (_historyCount < 3)
		_historyCount++;
	}

	private static void ShiftHistory(decimal[] buffer, decimal value)
	{
		buffer[2] = buffer[1];
		buffer[1] = buffer[0];
		buffer[0] = value;
	}

	private static bool IsStrictlyDescending(decimal[] series1, decimal[] series2, decimal[] series3, decimal[] series4, decimal[] series5)
	{
		return series1[0] > series2[0] && series2[0] > series3[0] && series3[0] > series4[0] && series4[0] > series5[0]
		&& series1[1] > series2[1] && series2[1] > series3[1] && series3[1] > series4[1] && series4[1] > series5[1]
		&& series1[2] > series2[2] && series2[2] > series3[2] && series3[2] > series4[2] && series4[2] > series5[2];
	}

	private static bool IsStrictlyAscending(decimal[] series1, decimal[] series2, decimal[] series3, decimal[] series4, decimal[] series5)
	{
		return series1[0] < series2[0] && series2[0] < series3[0] && series3[0] < series4[0] && series4[0] < series5[0]
		&& series1[1] < series2[1] && series2[1] < series3[1] && series3[1] < series4[1] && series4[1] < series5[1]
		&& series1[2] < series2[2] && series2[2] < series3[2] && series3[2] < series4[2] && series4[2] < series5[2];
	}
}
