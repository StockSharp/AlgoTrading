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
/// Multi-indicator trend strategy converted from the MetaTrader SSB5_123 expert advisor.
/// Combines AO, MACD, OsMA, SMMA, and Stochastic filters to confirm breakouts.
/// </summary>
public class Ssb5123Strategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousOpen;
	private decimal? _previousAo;
	private decimal? _previousMacdMain;
	private decimal? _previousHistogram;

	/// <summary>
	/// Smoothed moving average period used as a directional filter.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Fast period for the MACD calculation.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow period for the MACD calculation.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal period for the MACD smoothing.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Main period of the stochastic oscillator.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period for the stochastic %D line.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing applied to the stochastic %K line.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type that feeds all indicators.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="Ssb5123Strategy"/> with default parameters.
	/// </summary>
	public Ssb5123Strategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 45)
			.SetGreaterThanZero()
			.SetDisplay("SMMA Period", "Length of the smoothed moving average filter", "Trend")
			.SetCanOptimize(true);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 47)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period for MACD", "MACD")
			.SetCanOptimize(true);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 95)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period for MACD", "MACD")
			.SetCanOptimize(true);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 74)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA period for MACD", "MACD")
			.SetCanOptimize(true);

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Main period of the stochastic oscillator", "Stochastic")
			.SetCanOptimize(true);

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "Smoothing period for the %D line", "Stochastic")
			.SetCanOptimize(true);

		_stochasticSlowing = Param(nameof(StochasticSlowing), 56)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Slowing", "Additional smoothing for %K", "Stochastic")
			.SetCanOptimize(true);

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume for market entries", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for calculations", "Data");
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
		_previousOpen = null;
		_previousAo = null;
		_previousMacdMain = null;
		_previousHistogram = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators that mirror the original expert advisor filters.
		var smma = new SmoothedMovingAverage { Length = MaPeriod };

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod }
			},
			SignalMa = { Length = MacdSignalPeriod }
		};

		var awesome = new AwesomeOscillator();

		var stochastic = new StochasticOscillator
		{
			KPeriod = StochasticKPeriod,
			DPeriod = StochasticDPeriod,
			Slowing = StochasticSlowing
		};

		// Subscribe to candles and bind indicators to a single processing pipeline.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(smma, macd, awesome, stochastic, ProcessCandle)
			.Start();

		// Render visual components when a chart is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, smma);
			DrawIndicator(area, macd);
			DrawIndicator(area, awesome);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smmaValue, IIndicatorValue macdValue, IIndicatorValue aoValue, IIndicatorValue stochasticValue)
	{
		// Only act on completed candles to avoid premature signals.
		if (candle.State != CandleStates.Finished)
			return;

		if (!smmaValue.IsFinal || !macdValue.IsFinal || !aoValue.IsFinal || !stochasticValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var smma = smmaValue.ToDecimal();

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macdMain || macdTyped.Signal is not decimal macdSignal)
			return;
		var histogram = macdMain - macdSignal;

		var ao = aoValue.ToDecimal();

		var stochTyped = (StochasticOscillatorValue)stochasticValue;
		if (stochTyped.K is not decimal stochK || stochTyped.D is not decimal stochD)
			return;

		// Ensure we have reference values that represent the previous completed candle.
		if (_previousOpen is not decimal prevOpen ||
			_previousAo is not decimal prevAo ||
			_previousMacdMain is not decimal prevMacd ||
			_previousHistogram is not decimal prevHistogram)
		{
			UpdateState(candle.OpenPrice, ao, macdMain, histogram);
			return;
		}

		var candleGap = Compare(candle.OpenPrice, prevOpen);
		var aoDirection = Compare(ao, 0m);
		var aoMomentum = Compare(ao - prevAo, 0m);
		var macdDirection = Compare(macdMain, 0m);
		var macdMomentum = Compare(macdMain - prevMacd, 0m);
		var osmaMomentum = Compare(histogram - prevHistogram, 0m);
		var smmaDirection = Compare(candle.OpenPrice - smma, 0m);
		var stochKDirection = Compare(stochK - 50m, 0m);
		var stochDDirection = Compare(stochD - 50m, 0m);

		// Long setup mirrors LongSignal() from the original code.
		var longSignal = candleGap >= 0 &&
					aoDirection >= 0 &&
					aoMomentum >= 0 &&
					macdDirection >= 0 &&
					macdMomentum >= 0 &&
					osmaMomentum >= 0 &&
					smmaDirection >= 0 &&
					stochKDirection >= 0 &&
					stochDDirection >= 0;

		// Short setup mirrors ShortSignal() from the original code.
		var shortSignal = candleGap <= 0 &&
					aoDirection <= 0 &&
					aoMomentum <= 0 &&
					macdDirection <= 0 &&
					macdMomentum <= 0 &&
					osmaMomentum <= 0 &&
					smmaDirection <= 0 &&
					stochKDirection <= 0 &&
					stochDDirection <= 0;

		// Manage existing positions exactly as the MQL version.
		if (Position > 0)
		{
			if (shortSignal)
			{
				SellMarket(Math.Abs(Position));
			}
			UpdateState(candle.OpenPrice, ao, macdMain, histogram);
			return;
		}

		if (Position < 0)
		{
			if (longSignal)
			{
				BuyMarket(Math.Abs(Position));
			}
			UpdateState(candle.OpenPrice, ao, macdMain, histogram);
			return;
		}

		// Enter new positions only when flat, respecting the original precedence (longs checked first).
		if (longSignal)
		{
			BuyMarket(OrderVolume);
		}
		else if (shortSignal)
		{
			SellMarket(OrderVolume);
		}

		UpdateState(candle.OpenPrice, ao, macdMain, histogram);
	}

	private void UpdateState(decimal openPrice, decimal ao, decimal macdMain, decimal histogram)
	{
		// Store references required to reproduce indicator comparisons on the next candle.
		_previousOpen = openPrice;
		_previousAo = ao;
		_previousMacdMain = macdMain;
		_previousHistogram = histogram;
	}

	private static int Compare(decimal current, decimal reference)
	{
		// Return 1 when the current value is below the reference, -1 when above, and 0 when equal.
		if (current < reference)
			return 1;

		if (current > reference)
			return -1;

		return 0;
	}
}