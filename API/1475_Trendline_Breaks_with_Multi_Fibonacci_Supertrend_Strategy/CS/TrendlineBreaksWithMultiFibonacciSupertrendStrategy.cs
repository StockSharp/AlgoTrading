using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Detects breakouts of dynamic trendlines confirmed by multi-level SuperTrend average.
/// </summary>
public class TrendlineBreaksWithMultiFibonacciSupertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<decimal> _factor3;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<int> _swingLength;
	private readonly StrategyParam<decimal> _atrSlopeMultiplier;
	private readonly StrategyParam<int> _dmiLength;
	private readonly StrategyParam<decimal> _stopAtrMultiplier;
	private readonly StrategyParam<decimal> _takeAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _highBuffer = Array.Empty<decimal>();
	private decimal[] _lowBuffer = Array.Empty<decimal>();
	private int _bufferCount;
	private decimal? _upperTrend;
	private decimal? _lowerTrend;
	private decimal _upperSlope;
	private decimal _lowerSlope;
	private bool _prevAboveUpper;
	private bool _prevBelowLower;

	private decimal _st1;
	private decimal _st2;
	private decimal _st3;
	private bool _trend1;
	private bool _trend2;
	private bool _trend3;
	private decimal _smoothedTrend;
	private bool _stInitialized;

	private decimal _longStop;
	private decimal _longTarget;
	private decimal _shortStop;
	private decimal _shortTarget;

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// First SuperTrend factor.
	/// </summary>
	public decimal Factor1
	{
		get => _factor1.Value;
		set => _factor1.Value = value;
	}

	/// <summary>
	/// Second SuperTrend factor.
	/// </summary>
	public decimal Factor2
	{
		get => _factor2.Value;
		set => _factor2.Value = value;
	}

	/// <summary>
	/// Third SuperTrend factor.
	/// </summary>
	public decimal Factor3
	{
		get => _factor3.Value;
		set => _factor3.Value = value;
	}

	/// <summary>
	/// EMA length for smoothing SuperTrend average.
	/// </summary>
	public int SmoothLength
	{
		get => _smoothLength.Value;
		set => _smoothLength.Value = value;
	}

	/// <summary>
	/// Lookback for pivot detection.
	/// </summary>
	public int SwingLength
	{
		get => _swingLength.Value;
		set => _swingLength.Value = value;
	}

	/// <summary>
	/// Multiplier for ATR to define trendline slope.
	/// </summary>
	public decimal AtrSlopeMultiplier
	{
		get => _atrSlopeMultiplier.Value;
		set => _atrSlopeMultiplier.Value = value;
	}

	/// <summary>
	/// DMI length.
	/// </summary>
	public int DmiLength
	{
		get => _dmiLength.Value;
		set => _dmiLength.Value = value;
	}

	/// <summary>
	/// Stop-loss ATR multiplier.
	/// </summary>
	public decimal StopAtrMultiplier
	{
		get => _stopAtrMultiplier.Value;
		set => _stopAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Take-profit ATR multiplier.
	/// </summary>
	public decimal TakeAtrMultiplier
	{
		get => _takeAtrMultiplier.Value;
		set => _takeAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TrendlineBreaksWithMultiFibonacciSupertrendStrategy"/> class.
	/// </summary>
	public TrendlineBreaksWithMultiFibonacciSupertrendStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 13).SetGreaterThanZero().SetDisplay("ATR Period", "ATR length", "Indicators");
		_factor1 = Param(nameof(Factor1), 0.618m).SetGreaterThanZero().SetDisplay("Factor 1", "First multiplier", "Indicators");
		_factor2 = Param(nameof(Factor2), 1.618m).SetGreaterThanZero().SetDisplay("Factor 2", "Second multiplier", "Indicators");
		_factor3 = Param(nameof(Factor3), 2.618m).SetGreaterThanZero().SetDisplay("Factor 3", "Third multiplier", "Indicators");
		_smoothLength = Param(nameof(SmoothLength), 21).SetGreaterThanZero().SetDisplay("Smoothing", "EMA length", "Indicators");
		_swingLength = Param(nameof(SwingLength), 8).SetGreaterThanZero().SetDisplay("Swing Length", "Pivot lookback", "Trendlines");
		_atrSlopeMultiplier = Param(nameof(AtrSlopeMultiplier), 1m).SetGreaterThanZero().SetDisplay("ATR Slope Mult", "Trendline slope", "Trendlines");
		_dmiLength = Param(nameof(DmiLength), 13).SetGreaterThanZero().SetDisplay("DMI Length", "Directional index", "Indicators");
		_stopAtrMultiplier = Param(nameof(StopAtrMultiplier), 8m).SetGreaterThanZero().SetDisplay("Stop ATR Mult", "Stop-loss multiplier", "Risk");
		_takeAtrMultiplier = Param(nameof(TakeAtrMultiplier), 2m).SetGreaterThanZero().SetDisplay("Take ATR Mult", "Take-profit multiplier", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
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
		_bufferCount = 0;
		_upperTrend = null;
		_lowerTrend = null;
		_prevAboveUpper = false;
		_prevBelowLower = false;
		_stInitialized = false;
		_longStop = _longTarget = _shortStop = _shortTarget = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		_highBuffer = new decimal[SwingLength * 2 + 1];
		_lowBuffer = new decimal[SwingLength * 2 + 1];
		_bufferCount = 0;
		var dmi = new AverageDirectionalIndex { Length = DmiLength };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(dmi, atr, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue dmiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		var dmi = (AverageDirectionalIndexValue)dmiValue;
		var dx = dmi.Dx;
		if (dx.Plus is not decimal plusDi || dx.Minus is not decimal minusDi)
		return;
		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var upper1 = median + Factor1 * atrValue;
		var lower1 = median - Factor1 * atrValue;
		var upper2 = median + Factor2 * atrValue;
		var lower2 = median - Factor2 * atrValue;
		var upper3 = median + Factor3 * atrValue;
		var lower3 = median - Factor3 * atrValue;
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
		for (int i = 0; i < _highBuffer.Length - 1; i++)
		{
			_highBuffer[i] = _highBuffer[i + 1];
			_lowBuffer[i] = _lowBuffer[i + 1];
		}
		_highBuffer[^1] = candle.HighPrice;
		_lowBuffer[^1] = candle.LowPrice;
		if (_bufferCount < _highBuffer.Length)
		{
			_bufferCount++;
		}
		else
		{
			int pivot = SwingLength;
			bool isPivotHigh = true;
			var ph = _highBuffer[pivot];
			for (int i = 0; i < _highBuffer.Length; i++)
			{
				if (i == pivot) continue;
				if (ph <= _highBuffer[i])
				{
					isPivotHigh = false;
					break;
				}
			}
			if (isPivotHigh)
			{
				_upperTrend = ph;
				_upperSlope = atrValue / SwingLength * AtrSlopeMultiplier;
			}
			bool isPivotLow = true;
			var pl = _lowBuffer[pivot];
			for (int i = 0; i < _lowBuffer.Length; i++)
			{
				if (i == pivot) continue;
				if (pl >= _lowBuffer[i])
				{
					isPivotLow = false;
					break;
				}
			}
			if (isPivotLow)
			{
				_lowerTrend = pl;
				_lowerSlope = atrValue / SwingLength * AtrSlopeMultiplier;
			}
		}
		if (_upperTrend != null)
		_upperTrend -= _upperSlope;
		if (_lowerTrend != null)
		_lowerTrend += _lowerSlope;
		var upperBreakout = _upperTrend != null && candle.ClosePrice > _upperTrend && !_prevAboveUpper;
		var lowerBreakout = _lowerTrend != null && candle.ClosePrice < _lowerTrend && !_prevBelowLower;
		_prevAboveUpper = _upperTrend != null && candle.ClosePrice > _upperTrend;
		_prevBelowLower = _lowerTrend != null && candle.ClosePrice < _lowerTrend;
		var longCond = upperBreakout && plusDi > minusDi && candle.ClosePrice > _smoothedTrend;
		var shortCond = lowerBreakout && minusDi > plusDi && candle.ClosePrice < _smoothedTrend;
		var longExit = candle.ClosePrice < _smoothedTrend;
		var shortExit = candle.ClosePrice > _smoothedTrend;
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		var volume = Volume + Math.Abs(Position);
		if (longCond && Position <= 0)
		{
			BuyMarket(volume);
			_longStop = candle.ClosePrice - atrValue * StopAtrMultiplier;
			_longTarget = candle.ClosePrice + atrValue * TakeAtrMultiplier;
		}
		else if (shortCond && Position >= 0)
		{
			SellMarket(volume);
			_shortStop = candle.ClosePrice + atrValue * StopAtrMultiplier;
			_shortTarget = candle.ClosePrice - atrValue * TakeAtrMultiplier;
		}
		if (Position > 0)
		{
			if (longExit || candle.LowPrice <= _longStop || candle.HighPrice >= _longTarget)
			{
				SellMarket(Position);
				_longStop = _longTarget = 0m;
			}
		}
		else if (Position < 0)
		{
			if (shortExit || candle.HighPrice >= _shortStop || candle.LowPrice <= _shortTarget)
			{
				BuyMarket(Math.Abs(Position));
				_shortStop = _shortTarget = 0m;
			}
		}
	}
}
