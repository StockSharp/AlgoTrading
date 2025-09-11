using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy based on Dow Theory pivot analysis.
/// Enters long when both higher highs and higher lows are formed.
/// Enters short when both lower highs and lower lows are formed.
/// </summary>
public class DowTheoryTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _pivotLookback;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _highBuffer = Array.Empty<decimal>();
	private decimal[] _lowBuffer = Array.Empty<decimal>();
	private int _bufferIndex;
	private int _bufferCount;
	private int _barIndex;

	private decimal? _lastPivotHigh;
	private decimal? _prevPivotHigh;
	private decimal? _lastPivotLow;
	private decimal? _prevPivotLow;
	private int _lastPivotHighBar;
	private int _prevPivotHighBar;
	private int _lastPivotLowBar;
	private int _prevPivotLowBar;

	private int _trendDirection;
	private int _prevTrendDirection;

	/// <summary>
	/// Number of bars on each side used to confirm a pivot high or low.
	/// </summary>
	public int PivotLookback { get => _pivotLookback.Value; set => _pivotLookback.Value = value; }

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="DowTheoryTrendStrategy"/> class.
	/// </summary>
	public DowTheoryTrendStrategy()
	{
		_pivotLookback = Param(nameof(PivotLookback), 10)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Lookback", "Bars on each side for pivot detection", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Parameters");
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

		_highBuffer = Array.Empty<decimal>();
		_lowBuffer = Array.Empty<decimal>();
		_bufferIndex = 0;
		_bufferCount = 0;
		_barIndex = 0;

		_lastPivotHigh = _prevPivotHigh = null;
		_lastPivotLow = _prevPivotLow = null;
		_lastPivotHighBar = _prevPivotHighBar = 0;
		_lastPivotLowBar = _prevPivotLowBar = 0;
		_trendDirection = _prevTrendDirection = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var size = PivotLookback * 2 + 1;
		_highBuffer = new decimal[size];
		_lowBuffer = new decimal[size];
		_bufferIndex = 0;
		_bufferCount = 0;
		_barIndex = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		var size = _highBuffer.Length;

		_highBuffer[_bufferIndex] = candle.HighPrice;
		_lowBuffer[_bufferIndex] = candle.LowPrice;
		_bufferIndex = (_bufferIndex + 1) % size;

		if (_bufferCount < size)
		{
			_bufferCount++;
			_barIndex++;
			return;
		}

		var centerIndex = (_bufferIndex + size - PivotLookback - 1) % size;
		var centerHigh = _highBuffer[centerIndex];
		var centerLow = _lowBuffer[centerIndex];

		var isPivotHigh = true;
		var isPivotLow = true;

		for (var i = 0; i < size; i++)
		{
			if (i == centerIndex)
				continue;

			if (isPivotHigh && _highBuffer[i] >= centerHigh)
				isPivotHigh = false;
			if (isPivotLow && _lowBuffer[i] <= centerLow)
				isPivotLow = false;

			if (!isPivotHigh && !isPivotLow)
				break;
		}

		if (isPivotHigh)
		{
			_prevPivotHigh = _lastPivotHigh;
			_prevPivotHighBar = _lastPivotHighBar;
			_lastPivotHigh = centerHigh;
			_lastPivotHighBar = _barIndex - PivotLookback - 1;
		}

		if (isPivotLow)
		{
			_prevPivotLow = _lastPivotLow;
			_prevPivotLowBar = _lastPivotLowBar;
			_lastPivotLow = centerLow;
			_lastPivotLowBar = _barIndex - PivotLookback - 1;
		}

		if (_lastPivotHigh is decimal lph && _prevPivotHigh is decimal pph &&
			_lastPivotLow is decimal lpl && _prevPivotLow is decimal ppl)
		{
			var isHigherHigh = lph > pph;
			var isHigherLow = lpl > ppl;
			var isLowerHigh = lph < pph;
			var isLowerLow = lpl < ppl;

			if (isHigherHigh && isHigherLow)
				_trendDirection = 1;
			else if (isLowerHigh && isLowerLow)
				_trendDirection = -1;
		}

		if (_trendDirection != 0 && _trendDirection != _prevTrendDirection)
		{
			if (_trendDirection == 1 && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (_trendDirection == -1 && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevTrendDirection = _trendDirection;
		_barIndex++;
	}
}
