using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Combo 123 Reversal and Fractal Chaos Bands strategy.
/// The 123 reversal is long after a close below the previous one followed by a close above it while the slow stochastic %K is
/// below %D and above Level, and short in the mirrored case. The fractal chaos bands hold the high of the last up fractal and
/// the low of the last down fractal, a fractal being a bar whose high (low) exceeds the Pattern bars on each side.
/// A long needs the 123 long signal with a close above the upper band, a short the 123 short signal with a close below the lower
/// band, and an opposite signal reverses the position.
/// </summary>
public class Combo123ReversalFractalChaosBandsStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _kSmoothing;
	private readonly StrategyParam<int> _dLength;
	private readonly StrategyParam<decimal> _level;
	private readonly StrategyParam<int> _pattern;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = [];
	private readonly List<decimal> _lows = [];
	private SimpleMovingAverage _kSmoother;
	private SimpleMovingAverage _dAverage;
	private decimal? _close1;
	private decimal? _close2;
	private decimal? _upperBand;
	private decimal? _lowerBand;

	/// <summary>
	/// Stochastic lookback.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Smoothing of %K.
	/// </summary>
	public int KSmoothing
	{
		get => _kSmoothing.Value;
		set => _kSmoothing.Value = value;
	}

	/// <summary>
	/// Length of %D.
	/// </summary>
	public int DLength
	{
		get => _dLength.Value;
		set => _dLength.Value = value;
	}

	/// <summary>
	/// Stochastic level separating the 123 signals.
	/// </summary>
	public decimal Level
	{
		get => _level.Value;
		set => _level.Value = value;
	}

	/// <summary>
	/// Bars on each side of a fractal.
	/// </summary>
	public int Pattern
	{
		get => _pattern.Value;
		set => _pattern.Value = value;
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
	/// Constructor.
	/// </summary>
	public Combo123ReversalFractalChaosBandsStrategy()
	{
		_length = Param(nameof(Length), 15)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Stochastic lookback", "123 Reversal");

		_kSmoothing = Param(nameof(KSmoothing), 1)
			.SetGreaterThanZero()
			.SetDisplay("K Smoothing", "Smoothing of %K", "123 Reversal");

		_dLength = Param(nameof(DLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Length", "Length of %D", "123 Reversal");

		_level = Param(nameof(Level), 50m)
			.SetDisplay("Level", "Stochastic level separating the 123 signals", "123 Reversal");

		_pattern = Param(nameof(Pattern), 1)
			.SetGreaterThanZero()
			.SetDisplay("Pattern", "Bars on each side of a fractal", "Fractal Chaos Bands");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
		_kSmoother = null;
		_dAverage = null;
	}

	private void ResetState()
	{
		_highs.Clear();
		_lows.Clear();
		_close1 = null;
		_close2 = null;
		_upperBand = null;
		_lowerBand = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var rawK = new StochasticK { Length = Length };
		_kSmoother = new SimpleMovingAverage { Length = KSmoothing };
		_dAverage = new SimpleMovingAverage { Length = DLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rawK, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rawK);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rawKValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateFractals(candle);

		var close = candle.ClosePrice;
		var close1 = _close1;
		var close2 = _close2;
		_close2 = _close1;
		_close1 = close;

		if (!rawKValue.IsFormed)
			return;

		var kValue = _kSmoother.Process(rawKValue.ToDecimal(), candle.OpenTime, true);
		if (!kValue.IsFormed)
			return;

		var fast = kValue.ToDecimal();
		var dValue = _dAverage.Process(fast, candle.OpenTime, true);
		if (!dValue.IsFormed)
			return;

		var slow = dValue.ToDecimal();

		if (close1 is not decimal c1 || close2 is not decimal c2 || _upperBand is not decimal upper || _lowerBand is not decimal lower)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var reversalLong = c2 < c1 && close > c1 && fast < slow && fast > Level;
		var reversalShort = c2 > c1 && close < c1 && fast > slow && fast < Level;

		if (reversalLong && close > upper && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (reversalShort && close < lower && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}

	private void UpdateFractals(ICandleMessage candle)
	{
		var window = Pattern * 2 + 1;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > window)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count < window)
			return;

		// The fractal bar sits in the middle of the window, Pattern bars back.
		var centerHigh = _highs[Pattern];
		var centerLow = _lows[Pattern];
		var isUp = true;
		var isDown = true;

		for (var i = 0; i < window; i++)
		{
			if (i == Pattern)
				continue;

			if (_highs[i] >= centerHigh)
				isUp = false;

			if (_lows[i] <= centerLow)
				isDown = false;
		}

		if (isUp)
			_upperBand = centerHigh;

		if (isDown)
			_lowerBand = centerLow;
	}
}
