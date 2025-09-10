using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Combo Strategy 123 Reversal & Fractal Chaos Bands.
/// Generates signals when both a 123 reversal and Fractal Chaos Bands breakout align.
/// </summary>
public class ComboStrategy123ReversalFractalChaosBandsStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _kSmoothing;
	private readonly StrategyParam<int> _dLength;
	private readonly StrategyParam<decimal> _level;
	private readonly StrategyParam<int> _pattern;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose1;
	private decimal? _prevClose2;
	private int _rev123Pos;
	private int _fcbPos;
	private decimal? _upperFractal;
	private decimal? _lowerFractal;
	private decimal?[] _highs;
	private decimal?[] _lows;
	private int _arrayCount;

	/// <summary>
	/// Stochastic period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// %K smoothing period.
	/// </summary>
	public int KSmoothing
	{
		get => _kSmoothing.Value;
		set => _kSmoothing.Value = value;
	}

	/// <summary>
	/// %D smoothing period.
	/// </summary>
	public int DLength
	{
		get => _dLength.Value;
		set => _dLength.Value = value;
	}

	/// <summary>
	/// Threshold level for %K.
	/// </summary>
	public decimal Level
	{
		get => _level.Value;
		set => _level.Value = value;
	}

	/// <summary>
	/// Fractal pattern size.
	/// </summary>
	public int Pattern
	{
		get => _pattern.Value;
		set => _pattern.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="ComboStrategy123ReversalFractalChaosBandsStrategy"/>.
	/// </summary>
	public ComboStrategy123ReversalFractalChaosBandsStrategy()
	{
		_length = Param(nameof(Length), 15)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Stochastic period", "Indicators")
			.SetCanOptimize(true);

		_kSmoothing = Param(nameof(KSmoothing), 1)
			.SetGreaterThanZero()
			.SetDisplay("K Smoothing", "%K smoothing period", "Indicators")
			.SetCanOptimize(true);

		_dLength = Param(nameof(DLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Length", "%D smoothing period", "Indicators")
			.SetCanOptimize(true);

		_level = Param(nameof(Level), 50m)
			.SetRange(1m, 100m)
			.SetDisplay("Level", "Threshold level", "Indicators")
			.SetCanOptimize(true);

		_pattern = Param(nameof(Pattern), 1)
			.SetGreaterThanZero()
			.SetDisplay("Pattern", "Fractal pattern size", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_prevClose1 = null;
		_prevClose2 = null;
		_rev123Pos = 0;
		_fcbPos = 0;
		_upperFractal = null;
		_lowerFractal = null;
		_highs = null;
		_lows = null;
		_arrayCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highs = new decimal?[Pattern * 2 + 2];
		_lows = new decimal?[Pattern * 2 + 2];
		_arrayCount = 0;

		var stochastic = new StochasticOscillator
		{
			Length = Length,
			K = { Length = KSmoothing },
			D = { Length = DLength },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateFractals(candle);

		if (!stochValue.IsFinal)
		{
			UpdateCloses(candle);
			return;
		}

		var stoch = (StochasticOscillatorValue)stochValue;
		var kValue = stoch.K;
		var dValue = stoch.D;

		if (_prevClose2 is decimal c2 && _prevClose1 is decimal c1)
		{
			if (c2 < c1 && candle.ClosePrice > c1 && kValue < dValue && kValue > Level)
				_rev123Pos = 1;
			else if (c2 > c1 && candle.ClosePrice < c1 && kValue > dValue && kValue < Level)
				_rev123Pos = -1;
		}

		var signal = _rev123Pos == 1 && _fcbPos == 1 ? 1 :
			_rev123Pos == -1 && _fcbPos == -1 ? -1 : 0;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			var volume = Volume + Math.Abs(Position);

			if (signal == 1 && Position <= 0)
				BuyMarket(volume);
			else if (signal == -1 && Position >= 0)
				SellMarket(volume);
			else if (signal == 0)
			{
				if (Position > 0)
					SellMarket(Position);
				else if (Position < 0)
					BuyMarket(-Position);
			}
		}

		UpdateCloses(candle);
	}

	private void UpdateFractals(ICandleMessage candle)
	{
		for (var i = 0; i < _highs.Length - 1; i++)
		{
			_highs[i] = _highs[i + 1];
			_lows[i] = _lows[i + 1];
		}

		_highs[_highs.Length - 1] = candle.HighPrice;
		_lows[_lows.Length - 1] = candle.LowPrice;

		if (_arrayCount < _highs.Length)
			_arrayCount++;

		if (_arrayCount == _highs.Length)
		{
			var center = Pattern;

			var upLeft = true;
			for (var i = center + 1; i <= _highs.Length - 2; i++)
			{
				if (_highs[i] >= _highs[i - 1])
				{
					upLeft = false;
					break;
				}
			}

			var upRight = true;
			for (var i = center - 1; i >= 0; i--)
			{
				if (_highs[i] >= _highs[i + 1])
				{
					upRight = false;
					break;
				}
			}

			if (upLeft && upRight && _highs[center] is decimal up)
				_upperFractal = up;

			var lowLeft = true;
			for (var i = center + 1; i <= _lows.Length - 2; i++)
			{
				if (_lows[i] <= _lows[i - 1])
				{
					lowLeft = false;
					break;
				}
			}

			var lowRight = true;
			for (var i = center - 1; i >= 0; i--)
			{
				if (_lows[i] <= _lows[i + 1])
				{
					lowRight = false;
					break;
				}
			}

			if (lowLeft && lowRight && _lows[center] is decimal low)
				_lowerFractal = low;
		}

		if (_upperFractal is decimal upper && candle.ClosePrice > upper)
			_fcbPos = 1;
		else if (_lowerFractal is decimal lower && candle.ClosePrice < lower)
			_fcbPos = -1;
	}

	private void UpdateCloses(ICandleMessage candle)
	{
		_prevClose2 = _prevClose1;
		_prevClose1 = candle.ClosePrice;
	}
}
