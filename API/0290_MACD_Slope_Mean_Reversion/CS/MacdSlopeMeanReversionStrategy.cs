using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD slope mean reversion strategy.
/// Trades reversions from extreme MACD histogram slopes and exits when the slope returns to its recent average.
/// </summary>
public class MacdSlopeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMacdPeriod;
	private readonly StrategyParam<int> _slowMacdPeriod;
	private readonly StrategyParam<int> _signalMacdPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _fastEma;
	private decimal _slowEma;
	private decimal _signalEma;
	private decimal _previousHistogram;
	private decimal[] _slopeHistory;
	private int _currentIndex;
	private int _filledCount;
	private int _cooldown;
	private bool _isInitialized;

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int FastMacdPeriod
	{
		get => _fastMacdPeriod.Value;
		set => _fastMacdPeriod.Value = value;
	}

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int SlowMacdPeriod
	{
		get => _slowMacdPeriod.Value;
		set => _slowMacdPeriod.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int SignalMacdPeriod
	{
		get => _signalMacdPeriod.Value;
		set => _signalMacdPeriod.Value = value;
	}

	/// <summary>
	/// Period for slope statistics.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for standard deviation to determine entry threshold.
	/// </summary>
	public decimal DeviationMultiplier
	{
		get => _deviationMultiplier.Value;
		set => _deviationMultiplier.Value = value;
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
	/// Cooldown bars between orders.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MacdSlopeMeanReversionStrategy"/>.
	/// </summary>
	public MacdSlopeMeanReversionStrategy()
	{
		_fastMacdPeriod = Param(nameof(FastMacdPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicator Parameters")
			.SetOptimize(8, 20, 2);

		_slowMacdPeriod = Param(nameof(SlowMacdPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicator Parameters")
			.SetOptimize(20, 40, 2);

		_signalMacdPeriod = Param(nameof(SignalMacdPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line period for MACD", "Indicator Parameters")
			.SetOptimize(5, 15, 2);

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for slope statistics", "Strategy Parameters")
			.SetOptimize(10, 50, 5);

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Multiplier", "Multiplier for standard deviation to determine entry threshold", "Strategy Parameters")
			.SetOptimize(1m, 3m, 0.5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 1200)
			.SetRange(1, 5000)
			.SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management");

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
		_fastEma = default;
		_slowEma = default;
		_signalEma = default;
		_previousHistogram = default;
		_slopeHistory = new decimal[LookbackPeriod];
		_currentIndex = default;
		_filledCount = default;
		_cooldown = default;
		_isInitialized = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_slopeHistory = new decimal[LookbackPeriod];
		_currentIndex = 0;
		_filledCount = 0;
		_cooldown = 0;

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

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var closePrice = candle.ClosePrice;

		if (!_isInitialized)
		{
			_fastEma = closePrice;
			_slowEma = closePrice;
			_signalEma = 0;
			_previousHistogram = 0;
			_isInitialized = true;
			return;
		}

		var fastAlpha = 2m / (FastMacdPeriod + 1m);
		var slowAlpha = 2m / (SlowMacdPeriod + 1m);
		var signalAlpha = 2m / (SignalMacdPeriod + 1m);

		_fastEma += fastAlpha * (closePrice - _fastEma);
		_slowEma += slowAlpha * (closePrice - _slowEma);

		var macdLine = _fastEma - _slowEma;
		_signalEma += signalAlpha * (macdLine - _signalEma);
		var histogram = macdLine - _signalEma;
		var histogramSlope = histogram - _previousHistogram;
		_previousHistogram = histogram;

		_slopeHistory[_currentIndex] = histogramSlope;
		_currentIndex = (_currentIndex + 1) % LookbackPeriod;

		if (_filledCount < LookbackPeriod)
			_filledCount++;

		if (_filledCount < LookbackPeriod)
			return;

		var averageSlope = 0m;
		var sumSq = 0m;

		for (var i = 0; i < LookbackPeriod; i++)
			averageSlope += _slopeHistory[i];

		averageSlope /= LookbackPeriod;

		for (var i = 0; i < LookbackPeriod; i++)
		{
			var diff = _slopeHistory[i] - averageSlope;
			sumSq += diff * diff;
		}

		var slopeStdDev = (decimal)Math.Sqrt((double)(sumSq / LookbackPeriod));

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var lowerThreshold = averageSlope - DeviationMultiplier * slopeStdDev;
		var upperThreshold = averageSlope + DeviationMultiplier * slopeStdDev;

		if (Position == 0)
		{
			if (histogramSlope < lowerThreshold && histogram < 0)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (histogramSlope > upperThreshold && histogram > 0)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0 && histogramSlope >= averageSlope)
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && histogramSlope <= averageSlope)
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}
