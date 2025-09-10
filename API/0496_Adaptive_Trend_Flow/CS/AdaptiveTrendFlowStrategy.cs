using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive Trend Flow Strategy with SMA and MACD filters.
/// </summary>
public class AdaptiveTrendFlowStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<decimal> _sensitivity;
	private readonly StrategyParam<bool> _useSmaFilter;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<bool> _useMacdFilter;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private StandardDeviation _stdDev;
	private ExponentialMovingAverage _smoothVol;
	private SimpleMovingAverage _sma;
	private MovingAverageConvergenceDivergenceSignal _macd;

	private int _trend;
	private int _prevTrend;
	private decimal _level;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Main calculation length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Volatility smoothing length.
	/// </summary>
	public int SmoothLength
	{
		get => _smoothLength.Value;
		set => _smoothLength.Value = value;
	}

	/// <summary>
	/// Channel sensitivity multiplier.
	/// </summary>
	public decimal Sensitivity
	{
		get => _sensitivity.Value;
		set => _sensitivity.Value = value;
	}

	/// <summary>
	/// Enable SMA filter.
	/// </summary>
	public bool UseSmaFilter
	{
		get => _useSmaFilter.Value;
		set => _useSmaFilter.Value = value;
	}

	/// <summary>
	/// Length for SMA filter.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Enable MACD filter.
	/// </summary>
	public bool UseMacdFilter
	{
		get => _useMacdFilter.Value;
		set => _useMacdFilter.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal EMA period for MACD.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AdaptiveTrendFlowStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_length = Param(nameof(Length), 2)
			.SetGreaterThanZero()
			.SetDisplay("Main Length", "Main calculation length", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 2);

		_smoothLength = Param(nameof(SmoothLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "Volatility smoothing length", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 2);

		_sensitivity = Param(nameof(Sensitivity), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Sensitivity", "Channel sensitivity multiplier", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_useSmaFilter = Param(nameof(UseSmaFilter), true)
			.SetDisplay("Use SMA Filter", "Enable SMA filter", "Filters");

		_smaLength = Param(nameof(SmaLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Length for SMA filter", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 2);

		_useMacdFilter = Param(nameof(UseMacdFilter), true)
			.SetDisplay("Use MACD Filter", "Enable MACD filter", "Filters");

		_macdFastLength = Param(nameof(MacdFastLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast Length", "Fast EMA period for MACD", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 2);

		_macdSlowLength = Param(nameof(MacdSlowLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow Length", "Slow EMA period for MACD", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 5);

		_macdSignalLength = Param(nameof(MacdSignalLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal Length", "Signal EMA period for MACD", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 2);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma = new ExponentialMovingAverage { Length = Length };
		_slowEma = new ExponentialMovingAverage { Length = Length * 2 };
		_stdDev = new StandardDeviation { Length = Length };
		_smoothVol = new ExponentialMovingAverage { Length = SmoothLength };
		_sma = new SimpleMovingAverage { Length = SmaLength };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength }
			},
			SignalMa = { Length = MacdSignalLength }
		};

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

		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		var fastValue = _fastEma.Process(typical, candle.ServerTime, true);
		var slowValue = _slowEma.Process(typical, candle.ServerTime, true);
		var stdValue = _stdDev.Process(typical, candle.ServerTime, true);
		var smoothVolValue = _smoothVol.Process(stdValue.ToDecimal(), candle.ServerTime, true);
		var smaValue = _sma.Process(candle.ClosePrice, candle.ServerTime, true);
		var macdValue = _macd.Process(candle.ClosePrice, candle.ServerTime, true);

		if (!_smoothVol.IsFormed || (UseSmaFilter && !_sma.IsFormed) || (UseMacdFilter && !_macd.IsFormed))
			return;

		var basis = (fastValue.ToDecimal() + slowValue.ToDecimal()) / 2m;
		var upper = basis + smoothVolValue.ToDecimal() * Sensitivity;
		var lower = basis - smoothVolValue.ToDecimal() * Sensitivity;

		_prevTrend = _trend;

		if (_trend == 0)
		{
			_trend = candle.ClosePrice > basis ? 1 : -1;
			_level = _trend == 1 ? lower : upper;
			return;
		}

		if (_trend == 1)
		{
			if (candle.ClosePrice < lower)
			{
				_trend = -1;
				_level = upper;
			}
			else
			{
				_level = lower;
			}
		}
		else
		{
			if (candle.ClosePrice > upper)
			{
				_trend = 1;
				_level = lower;
			}
			else
			{
				_level = upper;
			}
		}

		if (_prevTrend == _trend)
			return;

		var smaCond = !UseSmaFilter || candle.ClosePrice > smaValue.ToDecimal();
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdCond = !UseMacdFilter || macdTyped.Macd > macdTyped.Signal;

		if (_trend == 1 && _prevTrend == -1 && smaCond && macdCond && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (_trend == -1 && _prevTrend == 1 && Position > 0)
		{
			SellMarket(Position);
		}
	}
}
