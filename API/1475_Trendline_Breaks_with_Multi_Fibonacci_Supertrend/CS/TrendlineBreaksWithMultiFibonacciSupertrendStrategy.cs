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
/// Detects breakouts of dynamic trendlines confirmed by multi-level SuperTrend average.
/// Uses pivot highs/lows to build trendlines, 3 SuperTrend levels averaged for trend direction.
/// </summary>
public class TrendlineBreaksWithMultiFibonacciSupertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _volPeriod;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<decimal> _factor3;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<int> _swingLength;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();

	private decimal? _upperTrend;
	private decimal? _lowerTrend;
	private decimal _upperSlope;
	private decimal _lowerSlope;
	private bool _prevAboveUpper;
	private bool _prevBelowLower;

	private decimal _st1, _st2, _st3;
	private bool _trend1, _trend2, _trend3;
	private decimal _smoothedTrend;
	private bool _stInitialized;

	public int VolPeriod { get => _volPeriod.Value; set => _volPeriod.Value = value; }
	public decimal Factor1 { get => _factor1.Value; set => _factor1.Value = value; }
	public decimal Factor2 { get => _factor2.Value; set => _factor2.Value = value; }
	public decimal Factor3 { get => _factor3.Value; set => _factor3.Value = value; }
	public int SmoothLength { get => _smoothLength.Value; set => _smoothLength.Value = value; }
	public int SwingLength { get => _swingLength.Value; set => _swingLength.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendlineBreaksWithMultiFibonacciSupertrendStrategy()
	{
		_volPeriod = Param(nameof(VolPeriod), 13).SetGreaterThanZero().SetDisplay("Vol Period", "StdDev length for volatility", "Indicators");
		_factor1 = Param(nameof(Factor1), 0.618m).SetGreaterThanZero().SetDisplay("Factor 1", "First SuperTrend multiplier", "Indicators");
		_factor2 = Param(nameof(Factor2), 1.618m).SetGreaterThanZero().SetDisplay("Factor 2", "Second SuperTrend multiplier", "Indicators");
		_factor3 = Param(nameof(Factor3), 2.618m).SetGreaterThanZero().SetDisplay("Factor 3", "Third SuperTrend multiplier", "Indicators");
		_smoothLength = Param(nameof(SmoothLength), 21).SetGreaterThanZero().SetDisplay("Smoothing", "EMA length for smoothing", "Indicators");
		_swingLength = Param(nameof(SwingLength), 5).SetGreaterThanZero().SetDisplay("Swing Length", "Pivot lookback", "Trendlines");
		_emaPeriod = Param(nameof(EmaPeriod), 20).SetGreaterThanZero().SetDisplay("EMA Period", "EMA for trend direction", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_highs.Clear();
		_lows.Clear();
		_upperTrend = null;
		_lowerTrend = null;
		_prevAboveUpper = false;
		_prevBelowLower = false;
		_stInitialized = false;
		_smoothedTrend = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var stdDev = new StandardDeviation { Length = VolPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal volVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (volVal <= 0)
			return;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		var bufferSize = SwingLength * 2 + 1;
		while (_highs.Count > bufferSize)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		// SuperTrend calculations using stdDev as volatility proxy
		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var upper1 = median + Factor1 * volVal;
		var lower1 = median - Factor1 * volVal;
		var upper2 = median + Factor2 * volVal;
		var lower2 = median - Factor2 * volVal;
		var upper3 = median + Factor3 * volVal;
		var lower3 = median - Factor3 * volVal;

		if (!_stInitialized)
		{
			_st1 = upper1;
			_st2 = upper2;
			_st3 = upper3;
			_trend1 = _trend2 = _trend3 = true;
			_smoothedTrend = median;
			_stInitialized = true;
			return;
		}

		_st1 = candle.ClosePrice > _st1 ? Math.Max(lower1, _st1) : Math.Min(upper1, _st1);
		_trend1 = candle.ClosePrice > _st1 ? true : candle.ClosePrice < _st1 ? false : _trend1;
		_st2 = candle.ClosePrice > _st2 ? Math.Max(lower2, _st2) : Math.Min(upper2, _st2);
		_trend2 = candle.ClosePrice > _st2 ? true : candle.ClosePrice < _st2 ? false : _trend2;
		_st3 = candle.ClosePrice > _st3 ? Math.Max(lower3, _st3) : Math.Min(upper3, _st3);
		_trend3 = candle.ClosePrice > _st3 ? true : candle.ClosePrice < _st3 ? false : _trend3;

		var avg = (_st1 + _st2 + _st3) / 3m;
		_smoothedTrend += (avg - _smoothedTrend) / SmoothLength;

		// Pivot detection for trendlines
		if (_highs.Count >= bufferSize)
		{
			var pivot = SwingLength;
			var ph = _highs[pivot];
			var isPivotHigh = true;
			for (var i = 0; i < _highs.Count; i++)
			{
				if (i == pivot) continue;
				if (ph <= _highs[i]) { isPivotHigh = false; break; }
			}
			if (isPivotHigh)
			{
				_upperTrend = ph;
				_upperSlope = volVal / SwingLength;
			}

			var pl = _lows[pivot];
			var isPivotLow = true;
			for (var i = 0; i < _lows.Count; i++)
			{
				if (i == pivot) continue;
				if (pl >= _lows[i]) { isPivotLow = false; break; }
			}
			if (isPivotLow)
			{
				_lowerTrend = pl;
				_lowerSlope = volVal / SwingLength;
			}
		}

		// Move trendlines
		if (_upperTrend != null)
			_upperTrend -= _upperSlope;
		if (_lowerTrend != null)
			_lowerTrend += _lowerSlope;

		// Detect breakouts
		var aboveUpper = _upperTrend != null && candle.ClosePrice > _upperTrend;
		var belowLower = _lowerTrend != null && candle.ClosePrice < _lowerTrend;

		var upperBreakout = aboveUpper && !_prevAboveUpper;
		var lowerBreakout = belowLower && !_prevBelowLower;

		_prevAboveUpper = aboveUpper;
		_prevBelowLower = belowLower;

		// Trading signals: trendline breakout + smoothed SuperTrend confirmation + EMA direction
		var isUpTrend = candle.ClosePrice > emaVal && candle.ClosePrice > _smoothedTrend;
		var isDownTrend = candle.ClosePrice < emaVal && candle.ClosePrice < _smoothedTrend;

		if (upperBreakout && isUpTrend && Position <= 0)
		{
			BuyMarket();
		}
		else if (lowerBreakout && isDownTrend && Position >= 0)
		{
			SellMarket();
		}
		// Exit on smoothed trend cross
		else if (Position > 0 && candle.ClosePrice < _smoothedTrend)
		{
			SellMarket();
		}
		else if (Position < 0 && candle.ClosePrice > _smoothedTrend)
		{
			BuyMarket();
		}
	}
}
