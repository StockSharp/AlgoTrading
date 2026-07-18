using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD strategy that adapts entry thresholds to the rolling distribution of the histogram.
/// </summary>
public class MacdAdaptiveHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _histogramAvgPeriod;
	private readonly StrategyParam<decimal> _stdDevMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd;
	private SimpleMovingAverage _histAvg;
	private StandardDeviation _histStdDev;
	private int _cooldown;

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period for the histogram statistics.
	/// </summary>
	public int HistogramAvgPeriod
	{
		get => _histogramAvgPeriod.Value;
		set => _histogramAvgPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for adaptive histogram thresholds.
	/// </summary>
	public decimal StdDevMultiplier
	{
		get => _stdDevMultiplier.Value;
		set => _stdDevMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Bars to wait after each order.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public MacdAdaptiveHistogramStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetRange(2, 50)
			.SetDisplay("Fast Period", "Fast EMA period for MACD", "MACD");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetRange(3, 100)
			.SetDisplay("Slow Period", "Slow EMA period for MACD", "MACD");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetRange(2, 50)
			.SetDisplay("Signal Period", "Signal line period for MACD", "MACD");

		_histogramAvgPeriod = Param(nameof(HistogramAvgPeriod), 20)
			.SetRange(5, 100)
			.SetDisplay("Histogram Avg Period", "Lookback period for histogram statistics", "Signals");

		_stdDevMultiplier = Param(nameof(StdDevMultiplier), 1.2m)
			.SetRange(0.1m, 5m)
			.SetDisplay("StdDev Multiplier", "Standard deviation multiplier for adaptive thresholds", "Signals");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 16)
			.SetRange(1, 200)
			.SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for the strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_macd = null;
		_histAvg = null;
		_histStdDev = null;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod },
		};
		_histAvg = new SimpleMovingAverage { Length = HistogramAvgPeriod };
		_histStdDev = new StandardDeviation { Length = HistogramAvgPeriod };
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopLossPercent, UnitTypes.Percent), false);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typedValue = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (typedValue.Macd is not decimal macd ||
			typedValue.Signal is not decimal signal)
			return;

		var histogram = macd - signal;
		var histogramAverage = _histAvg.Process(histogram, candle.OpenTime, true).ToDecimal();
		var histogramStdDev = _histStdDev.Process(histogram, candle.OpenTime, true).ToDecimal();

		if (!_macd.IsFormed || !_histAvg.IsFormed || !_histStdDev.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		if (histogramStdDev <= 0)
			return;

		var upperThreshold = histogramAverage + StdDevMultiplier * histogramStdDev;
		var lowerThreshold = histogramAverage - StdDevMultiplier * histogramStdDev;

		if (Position == 0)
		{
			if (histogram >= upperThreshold && histogram > 0)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (histogram <= lowerThreshold && histogram < 0)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}

			return;
		}

		if (Position > 0 && histogram <= histogramAverage)
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && histogram >= histogramAverage)
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}
