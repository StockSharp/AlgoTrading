using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// DeMark trendline breakout strategy converted from MetaTrader indicator.
/// </summary>
public class DeMarkLinesStrategy : Strategy
{
	private readonly StrategyParam<int> _pivotDepth;
	private readonly StrategyParam<int> _minBarsBetweenPivots;
	private readonly StrategyParam<decimal> _breakoutBuffer;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _highBuffer = Array.Empty<decimal>();
	private decimal[] _lowBuffer = Array.Empty<decimal>();
	private DateTimeOffset[] _timeBuffer = Array.Empty<DateTimeOffset>();
	private int _windowSize;
	private int _bufferCount;
	private long _processedBars;
	private decimal _pipSize;
	private PivotPoint _previousHigh;
	private PivotPoint _recentHigh;
	private PivotPoint _previousLow;
	private PivotPoint _recentLow;
	private long _lastLongSignalIndex;
	private long _lastShortSignalIndex;

	/// <summary>
	/// Gets or sets the number of confirmation bars on both sides of a pivot.
	/// </summary>
	public int PivotDepth
	{
		get => _pivotDepth.Value;
		set => _pivotDepth.Value = value;
	}

	/// <summary>
	/// Gets or sets the minimum number of bars between successive pivots of the same type.
	/// </summary>
	public int MinBarsBetweenPivots
	{
		get => _minBarsBetweenPivots.Value;
		set => _minBarsBetweenPivots.Value = value;
	}

	/// <summary>
	/// Gets or sets the breakout filter expressed in pips.
	/// </summary>
	public decimal BreakoutBuffer
	{
		get => _breakoutBuffer.Value;
		set => _breakoutBuffer.Value = value;
	}

	/// <summary>
	/// Gets or sets the candle type used for signal detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DeMarkLinesStrategy"/>.
	/// </summary>
	public DeMarkLinesStrategy()
	{
		_pivotDepth = Param(nameof(PivotDepth), 2)
			.SetGreaterThanZero()
			.SetDisplay("Pivot depth", "Number of bars confirming a swing high/low", "Signals")
			.SetCanOptimize(true);

		_minBarsBetweenPivots = Param(nameof(MinBarsBetweenPivots), 5)
			.SetGreaterThanZero()
			.SetDisplay("Minimum bars between pivots", "Prevents overlapping trendline anchors", "Signals")
			.SetCanOptimize(true);

		_breakoutBuffer = Param(nameof(BreakoutBuffer), 2m)
			.SetNotLessThanZero()
			.SetDisplay("Breakout buffer (pips)", "Extra distance beyond the trendline before entering", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe for the analysis", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highBuffer = Array.Empty<decimal>();
		_lowBuffer = Array.Empty<decimal>();
		_timeBuffer = Array.Empty<DateTimeOffset>();
		_windowSize = 0;
		_bufferCount = 0;
		_processedBars = 0;
		_pipSize = 0m;
		_previousHigh = CreateInvalidPivot();
		_recentHigh = CreateInvalidPivot();
		_previousLow = CreateInvalidPivot();
		_recentLow = CreateInvalidPivot();
		_lastLongSignalIndex = -1;
		_lastShortSignalIndex = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_windowSize = Math.Max(3, PivotDepth * 2 + 1);
		_highBuffer = new decimal[_windowSize];
		_lowBuffer = new decimal[_windowSize];
		_timeBuffer = new DateTimeOffset[_windowSize];
		_bufferCount = 0;
		_processedBars = 0;
		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _windowSize == 0)
			return;

		// Fill the buffers during the warm-up phase until enough bars are available.
		if (_bufferCount < _windowSize)
		{
			_highBuffer[_bufferCount] = candle.HighPrice;
			_lowBuffer[_bufferCount] = candle.LowPrice;
			_timeBuffer[_bufferCount] = candle.OpenTime;
			_bufferCount++;
			_processedBars++;
			return;
		}

		// Shift buffers to keep the rolling window aligned with the latest data.
		for (var i = 0; i < _windowSize - 1; i++)
		{
			_highBuffer[i] = _highBuffer[i + 1];
			_lowBuffer[i] = _lowBuffer[i + 1];
			_timeBuffer[i] = _timeBuffer[i + 1];
		}

		_highBuffer[_windowSize - 1] = candle.HighPrice;
		_lowBuffer[_windowSize - 1] = candle.LowPrice;
		_timeBuffer[_windowSize - 1] = candle.OpenTime;
		_processedBars++;

		var centerIndex = _windowSize - 1 - PivotDepth;
		var pivotBarIndex = _processedBars - PivotDepth - 1;
		var pivotTime = _timeBuffer[centerIndex];
		var pivotHigh = _highBuffer[centerIndex];
		var pivotLow = _lowBuffer[centerIndex];

		// Update downtrend anchors when a new swing high appears.
		if (IsPivotHigh(centerIndex))
			RegisterHighPivot(pivotBarIndex, pivotTime, pivotHigh);

		// Update uptrend anchors when a new swing low appears.
		if (IsPivotLow(centerIndex))
			RegisterLowPivot(pivotBarIndex, pivotTime, pivotLow);

		EvaluateBreakouts(candle);
	}

	private bool IsPivotHigh(int index)
	{
		var high = _highBuffer[index];

		for (var offset = 1; offset <= PivotDepth; offset++)
		{
			if (high <= _highBuffer[index - offset])
				return false;

			if (high < _highBuffer[index + offset])
				return false;
		}

		return true;
	}

	private bool IsPivotLow(int index)
	{
		var low = _lowBuffer[index];

		for (var offset = 1; offset <= PivotDepth; offset++)
		{
			if (low >= _lowBuffer[index - offset])
				return false;

			if (low > _lowBuffer[index + offset])
				return false;
		}

		return true;
	}

	private void RegisterHighPivot(long index, DateTimeOffset time, decimal price)
	{
		if (_recentHigh.IsValid && index - _recentHigh.Index < MinBarsBetweenPivots)
			return;

		_previousHigh = _recentHigh;
		_recentHigh = CreatePivot(index, time, price);
		_lastLongSignalIndex = -1;
	}

	private void RegisterLowPivot(long index, DateTimeOffset time, decimal price)
	{
		if (_recentLow.IsValid && index - _recentLow.Index < MinBarsBetweenPivots)
			return;

		_previousLow = _recentLow;
		_recentLow = CreatePivot(index, time, price);
		_lastShortSignalIndex = -1;
	}

	private void EvaluateBreakouts(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var currentIndex = _processedBars - 1;
		var priceBuffer = BreakoutBuffer * (_pipSize > 0m ? _pipSize : 1m);

		// Look for a bullish breakout through the downtrend line.
		if (_recentHigh.IsValid && _previousHigh.IsValid && currentIndex != _lastLongSignalIndex)
		{
			var resistance = CalculateTrendValue(_previousHigh, _recentHigh, currentIndex);

			if (candle.ClosePrice > resistance + priceBuffer && Position <= 0)
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);

				if (volume > 0m)
				{
					BuyMarket(volume);
					_lastLongSignalIndex = currentIndex;
				}
			}
		}

		// Look for a bearish breakout through the uptrend line.
		if (_recentLow.IsValid && _previousLow.IsValid && currentIndex != _lastShortSignalIndex)
		{
			var support = CalculateTrendValue(_previousLow, _recentLow, currentIndex);

			if (candle.ClosePrice < support - priceBuffer && Position >= 0)
			{
				var volume = Volume + (Position > 0 ? Position : 0m);

				if (volume > 0m)
				{
					SellMarket(volume);
					_lastShortSignalIndex = currentIndex;
				}
			}
		}
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep;

		if (priceStep is not decimal step || step <= 0m)
			return 1m;

		var decimals = GetDecimalPlaces(step);

		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}

	private static PivotPoint CreatePivot(long index, DateTimeOffset time, decimal price)
		=> new()
		{
			Index = index,
			Time = time,
			Price = price
		};

	private static PivotPoint CreateInvalidPivot()
		=> new()
		{
			Index = -1,
			Time = default,
			Price = 0m
		};

	private static decimal CalculateTrendValue(PivotPoint older, PivotPoint newer, long currentIndex)
	{
		var indexDiff = newer.Index - older.Index;

		if (indexDiff == 0)
			return newer.Price;

		var slope = (newer.Price - older.Price) / (decimal)indexDiff;
		var offset = currentIndex - newer.Index;

		return newer.Price + slope * offset;
	}

	private struct PivotPoint
	{
		public long Index;
		public DateTimeOffset Time;
		public decimal Price;

		public bool IsValid => Index >= 0;
	}
}

