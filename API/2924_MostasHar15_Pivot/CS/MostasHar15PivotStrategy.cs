using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pivot based strategy converted from the MostasHaR15 Pivot Expert Advisor.
/// </summary>
public class MostasHar15PivotStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _minDistancePips;
	private readonly StrategyParam<decimal> _emaSlopePips;
	private readonly StrategyParam<int> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _dailyCandleType;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _emaClosePeriod;
	private readonly StrategyParam<int> _emaOpenPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;

	private EMA? _emaClose;
	private EMA? _emaOpen;
	private AverageDirectionalIndex? _adx;
	private MovingAverageConvergenceDivergence? _macd;

	private decimal? _prevEmaClose;
	private decimal? _prevEmaOpen;
	private decimal? _prevOsma;
	private decimal? _prevPlusDi;
	private decimal? _prevMinusDi;

	private ICandleMessage _previousDailyCandle;
	private ICandleMessage _lastDailyCandle;

	private decimal? _pipSize;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal? _longTarget;
	private decimal? _shortTarget;

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	public decimal MinimumDistancePips
	{
		get => _minDistancePips.Value;
		set => _minDistancePips.Value = value;
	}

	public decimal EmaSlopePips
	{
		get => _emaSlopePips.Value;
		set => _emaSlopePips.Value = value;
	}

	public int AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	public int EmaClosePeriod
	{
		get => _emaClosePeriod.Value;
		set => _emaClosePeriod.Value = value;
	}

	public int EmaOpenPeriod
	{
		get => _emaOpenPeriod.Value;
		set => _emaOpenPeriod.Value = value;
	}

	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	public MostasHar15PivotStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetDisplay("Stop Loss (pips)", "Distance to stop loss in pips", "Risk")
			.SetCanOptimize(true);
		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop size in pips", "Risk")
			.SetCanOptimize(true);
		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step (pips)", "Minimum move before updating trailing stop", "Risk")
			.SetCanOptimize(true);
		_minDistancePips = Param(nameof(MinimumDistancePips), 14m)
			.SetDisplay("Minimum Distance (pips)", "Required distance to support/resistance", "Filters")
			.SetCanOptimize(true);
		_emaSlopePips = Param(nameof(EmaSlopePips), 5m)
			.SetDisplay("EMA Slope (pips)", "Required EMA separation in pips", "Filters")
			.SetCanOptimize(true);
		_adxThreshold = Param(nameof(AdxThreshold), 20)
			.SetDisplay("ADX Threshold", "Minimum ADX value", "Filters")
			.SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Primary Candle Type", "Intraday candle type", "General");
		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Daily Candle Type", "Daily candle series for pivots", "General");
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Length of Average Directional Index", "Indicators")
			.SetCanOptimize(true);
		_emaClosePeriod = Param(nameof(EmaClosePeriod), 5)
			.SetDisplay("EMA Close Period", "EMA length on close price", "Indicators")
			.SetCanOptimize(true);
		_emaOpenPeriod = Param(nameof(EmaOpenPeriod), 8)
			.SetDisplay("EMA Open Period", "EMA length on open price", "Indicators")
			.SetCanOptimize(true);
		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetDisplay("MACD Fast Period", "Fast EMA length for MACD", "Indicators")
			.SetCanOptimize(true);
		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow Period", "Slow EMA length for MACD", "Indicators")
			.SetCanOptimize(true);
		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal Period", "Signal line length for MACD", "Indicators")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		// Reset cached indicator values and pivot levels.
		_prevEmaClose = null;
		_prevEmaOpen = null;
		_prevOsma = null;
		_prevPlusDi = null;
		_prevMinusDi = null;
		_previousDailyCandle = null;
		_lastDailyCandle = null;
		_pipSize = null;
		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Trailing requires both distance and step just like in the original EA.
		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
		{
			LogError("Trailing stop requires a positive trailing step.");
			Stop();
			return;
		}

		// Cache pip size once the security information is available.
		_pipSize = GetPipSize();

		// Create indicators mirroring the MQL inputs.
		_emaClose = new EMA { Length = EmaClosePeriod };
		_emaOpen = new EMA { Length = EmaOpenPeriod };
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_macd = new MovingAverageConvergenceDivergence
		{
			ShortLength = MacdFastPeriod,
			LongLength = MacdSlowPeriod,
			SignalLength = MacdSignalPeriod
		};

		// Subscribe to daily candles used for pivot calculations.
		SubscribeCandles(DailyCandleType).Bind(ProcessDailyCandle).Start();

		// Subscribe to the primary intraday series with indicator binding.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, _macd, ProcessCandle)
			.Start();

		// Draw key elements on the chart if it is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaClose);
			DrawIndicator(area, _emaOpen);
			DrawIndicator(area, _adx);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		// Skip unfinished daily candles.
		if (candle.State != CandleStates.Finished)
			return;

		// Shift history so that _previousDailyCandle always stores yesterday.
		_previousDailyCandle = _lastDailyCandle;
		_lastDailyCandle = candle;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue macdValue)
	{
		// Work only with completed candles.
		if (candle.State != CandleStates.Finished)
			return;

		// Respect strategy readiness helpers.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_emaClose == null || _emaOpen == null || _adx == null || _macd == null || _pipSize == null)
			return;

		// Update the EMA indicators manually to emulate the original data feeds.
		var emaCloseValue = _emaClose.Process(new DecimalIndicatorValue(_emaClose, candle.ClosePrice, candle.OpenTime));
		var emaOpenValue = _emaOpen.Process(new DecimalIndicatorValue(_emaOpen, candle.OpenPrice, candle.OpenTime));

		if (!emaCloseValue.IsFinal || !emaOpenValue.IsFinal)
			return;

		// Ensure ADX and MACD values are ready.
		if (!adxValue.IsFinal || adxValue is not AverageDirectionalIndexValue adxData)
			return;

		if (!macdValue.IsFinal || macdValue is not MovingAverageConvergenceDivergenceValue macdData)
			return;

		// Pivot calculation requires the previous daily candle.
		if (_previousDailyCandle == null)
			return;

		// Manage existing trades before looking for fresh entries.
		if (ManageOpenPositions(candle))
			return;

		var levels = CalculatePivotLevels(_previousDailyCandle);

		if (!TrySelectRange(candle.ClosePrice, levels, out var support, out var resistance))
		{
			UpdatePreviousValues(emaCloseValue.ToDecimal(), emaOpenValue.ToDecimal(), macdData.Histogram, adxData.Dx.Plus, adxData.Dx.Minus);
			return;
		}

		var pip = _pipSize.Value;
		var dif1 = pip > 0m ? (candle.ClosePrice - support) / pip : 0m;
		var dif2 = pip > 0m ? (resistance - candle.ClosePrice) / pip : 0m;

		var emaCloseCurrent = emaCloseValue.ToDecimal();
		var emaOpenCurrent = emaOpenValue.ToDecimal();
		var osmaCurrent = macdData.Histogram;
		var plusDi = adxData.Dx.Plus;
		var minusDi = adxData.Dx.Minus;
		var adx = adxData.MovingAverage;

		if (_prevEmaClose is not decimal prevClose ||
			_prevEmaOpen is not decimal prevOpen ||
			_prevOsma is not decimal prevOsma ||
			_prevPlusDi is not decimal prevPlus ||
			_prevMinusDi is not decimal prevMinus)
		{
			UpdatePreviousValues(emaCloseCurrent, emaOpenCurrent, osmaCurrent, plusDi, minusDi);
			return;
		}

		var emaSlopeThreshold = EmaSlopePips * pip;
		var minDistance = MinimumDistancePips;

		// Long setup mirrors the MQL checks.
		var longSignal = Position <= 0 &&
			dif2 > minDistance &&
			adx > AdxThreshold &&
			plusDi > prevPlus &&
			plusDi > minusDi &&
			emaCloseCurrent - emaOpenCurrent >= emaSlopeThreshold &&
			prevClose > prevOpen &&
			osmaCurrent > prevOsma;

		// Short setup mirrors the bearish branch from the EA.
		var shortSignal = Position >= 0 &&
			dif1 > minDistance &&
			adx > AdxThreshold &&
			minusDi > prevMinus &&
			plusDi < minusDi &&
			emaOpenCurrent - emaCloseCurrent >= emaSlopeThreshold &&
			prevOpen > prevClose &&
			osmaCurrent < prevOsma;

		if (longSignal)
		{
			OpenLong(candle.ClosePrice, resistance, pip);
			UpdatePreviousValues(emaCloseCurrent, emaOpenCurrent, osmaCurrent, plusDi, minusDi);
			return;
		}

		if (shortSignal)
		{
			OpenShort(candle.ClosePrice, support, pip);
			UpdatePreviousValues(emaCloseCurrent, emaOpenCurrent, osmaCurrent, plusDi, minusDi);
			return;
		}

		UpdatePreviousValues(emaCloseCurrent, emaOpenCurrent, osmaCurrent, plusDi, minusDi);
	}

	private void OpenLong(decimal price, decimal target, decimal pip)
	{
		// Enter long using the strategy volume.
		BuyMarket();

		_longEntryPrice = price;
		_longTarget = target;
		_shortEntryPrice = null;
		_shortTarget = null;
		_shortStop = null;

		// Stop-loss replicates the original pip based offset.
		_longStop = StopLossPips > 0m ? price - StopLossPips * pip : null;
	}

	private void OpenShort(decimal price, decimal target, decimal pip)
	{
		// Enter short using the strategy volume.
		SellMarket();

		_shortEntryPrice = price;
		_shortTarget = target;
		_longEntryPrice = null;
		_longTarget = null;
		_longStop = null;

		_shortStop = StopLossPips > 0m ? price + StopLossPips * pip : null;
	}

	private bool ManageOpenPositions(ICandleMessage candle)
	{
		if (_pipSize == null)
			return false;

		var pip = _pipSize.Value;

		if (Position > 0)
		{
			// Exit long on stop-loss.
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return true;
			}

			// Exit long on the predefined profit target.
			if (_longTarget.HasValue && candle.HighPrice >= _longTarget.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return true;
			}

			// Update trailing stop mimicking the EA behaviour.
			if (TrailingStopPips > 0m && TrailingStepPips > 0m && _longEntryPrice.HasValue)
			{
				var profit = candle.ClosePrice - _longEntryPrice.Value;
				var trailingDistance = TrailingStopPips * pip;
				var trailingStep = TrailingStepPips * pip;

				if (profit > trailingDistance + trailingStep)
				{
					var threshold = candle.ClosePrice - (trailingDistance + trailingStep);
					if (!_longStop.HasValue || _longStop.Value < threshold)
					{
						_longStop = candle.ClosePrice - trailingDistance;
					}
				}
			}
		}
		else if (Position < 0)
		{
			// Exit short on stop-loss.
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return true;
			}

			// Exit short on the pivot based profit target.
			if (_shortTarget.HasValue && candle.LowPrice <= _shortTarget.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return true;
			}

			// Trailing logic mirrors the sell branch of the EA.
			if (TrailingStopPips > 0m && TrailingStepPips > 0m && _shortEntryPrice.HasValue)
			{
				var profit = _shortEntryPrice.Value - candle.ClosePrice;
				var trailingDistance = TrailingStopPips * pip;
				var trailingStep = TrailingStepPips * pip;

				if (profit > trailingDistance + trailingStep)
				{
					var threshold = candle.ClosePrice + (trailingDistance + trailingStep);
					if (!_shortStop.HasValue || _shortStop.Value > threshold || _shortStop.Value == 0m)
					{
						_shortStop = candle.ClosePrice + trailingDistance;
					}
				}
			}
		}
		else
		{
			ResetLongState();
			ResetShortState();
		}

		return false;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStop = null;
		_longTarget = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStop = null;
		_shortTarget = null;
	}

	private void UpdatePreviousValues(decimal emaClose, decimal emaOpen, decimal osma, decimal plusDi, decimal minusDi)
	{
		_prevEmaClose = emaClose;
		_prevEmaOpen = emaOpen;
		_prevOsma = osma;
		_prevPlusDi = plusDi;
		_prevMinusDi = minusDi;
	}

	private static (decimal Pivot, decimal R1, decimal R2, decimal R3, decimal S1, decimal S2, decimal S3, decimal M0, decimal M1, decimal M2, decimal M3, decimal M4, decimal M5) CalculatePivotLevels(ICandleMessage candle)
	{
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		var pivot = (high + low + close) / 3m;
		var r1 = 2m * pivot - low;
		var s1 = 2m * pivot - high;
		var r2 = pivot + (high - low);
		var s2 = pivot - (high - low);
		var r3 = 2m * pivot + (high - 2m * low);
		var s3 = 2m * pivot - (2m * high - low);
		var m5 = (r2 + r3) / 2m;
		var m4 = (r1 + r2) / 2m;
		var m3 = (pivot + r1) / 2m;
		var m2 = (pivot + s1) / 2m;
		var m1 = (s1 + s2) / 2m;
		var m0 = (s2 + s3) / 2m;

		return (pivot, r1, r2, r3, s1, s2, s3, m0, m1, m2, m3, m4, m5);
	}

	private static bool TrySelectRange(decimal price, (decimal Pivot, decimal R1, decimal R2, decimal R3, decimal S1, decimal S2, decimal S3, decimal M0, decimal M1, decimal M2, decimal M3, decimal M4, decimal M5) levels, out decimal support, out decimal resistance)
	{
		support = 0m;
		resistance = 0m;

		if (IsBetween(price, levels.S3, levels.M0))
		{
			support = levels.S3;
			resistance = levels.M0;
			return true;
		}

		if (IsBetween(price, levels.M0, levels.S2))
		{
			support = levels.M0;
			resistance = levels.S2;
			return true;
		}

		if (IsBetween(price, levels.S2, levels.M1))
		{
			support = levels.S2;
			resistance = levels.M1;
			return true;
		}

		if (IsBetween(price, levels.M1, levels.S1))
		{
			support = levels.M1;
			resistance = levels.S1;
			return true;
		}

		if (IsBetween(price, levels.S1, levels.M2))
		{
			support = levels.S1;
			resistance = levels.M2;
			return true;
		}

		if (IsBetween(price, levels.M2, levels.Pivot))
		{
			support = levels.M2;
			resistance = levels.Pivot;
			return true;
		}

		if (IsBetween(price, levels.Pivot, levels.M3))
		{
			support = levels.Pivot;
			resistance = levels.M3;
			return true;
		}

		if (IsBetween(price, levels.M3, levels.R1))
		{
			support = levels.M3;
			resistance = levels.R1;
			return true;
		}

		if (IsBetween(price, levels.R1, levels.M4))
		{
			support = levels.R1;
			resistance = levels.M4;
			return true;
		}

		if (IsBetween(price, levels.M4, levels.R2))
		{
			support = levels.M4;
			resistance = levels.R2;
			return true;
		}

		if (IsBetween(price, levels.R2, levels.M5))
		{
			support = levels.R2;
			resistance = levels.M5;
			return true;
		}

		if (IsBetween(price, levels.M5, levels.R3))
		{
			support = levels.S3;
			resistance = levels.M0;
			return true;
		}

		return false;
	}

	private static bool IsBetween(decimal price, decimal lower, decimal upper)
	{
		return (price - lower) * (price - upper) < 0m;
	}

	private decimal GetPipSize()
	{
		if (Security?.PriceStep is decimal step && step > 0m)
		{
			var decimals = Security.Decimals;
			if (decimals == 3 || decimals == 5)
				return step * 10m;

			return step;
		}

		return 0.0001m;
	}
}
