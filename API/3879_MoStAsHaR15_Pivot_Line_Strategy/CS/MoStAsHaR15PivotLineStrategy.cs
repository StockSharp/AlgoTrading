using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implements the MoStAsHaR15 pivot line MetaTrader strategy using the high level API.
/// </summary>
public class MoStAsHaR15PivotLineStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _minimumDistancePips;
	private readonly StrategyParam<decimal> _emaSpreadPips;
	private readonly StrategyParam<int> _adxThreshold;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _emaClosePeriod;
	private readonly StrategyParam<int> _emaOpenPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<DataType> _hourlyCandleType;
	private readonly StrategyParam<DataType> _dailyCandleType;

	private EMA? _emaClose;
	private EMA? _emaOpen;
	private AverageDirectionalIndex? _adx;
	private MovingAverageConvergenceDivergence? _macd;

	private decimal? _previousEmaClose;
	private decimal? _previousEmaOpen;
	private decimal? _previousOsma;
	private decimal? _previousPlusDi;
	private decimal? _previousMinusDi;

	private ICandleMessage? _previousDailyCandle;
	private ICandleMessage? _lastDailyCandle;

	private decimal? _pipSize;

	private decimal? _longEntryPrice;
	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortEntryPrice;
	private decimal? _shortStop;
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
		get => _minimumDistancePips.Value;
		set => _minimumDistancePips.Value = value;
	}

	public decimal EmaSpreadPips
	{
		get => _emaSpreadPips.Value;
		set => _emaSpreadPips.Value = value;
	}

	public int AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
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

	public DataType HourlyCandleType
	{
		get => _hourlyCandleType.Value;
		set => _hourlyCandleType.Value = value;
	}

	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	public MoStAsHaR15PivotLineStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetDisplay("Stop Loss (pips)", "Initial stop distance in pips", "Risk")
			.SetCanOptimize(true);
		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop size in pips", "Risk")
			.SetCanOptimize(true);
		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step (pips)", "Minimum move before adjusting the trailing stop", "Risk")
			.SetCanOptimize(true);
		_minimumDistancePips = Param(nameof(MinimumDistancePips), 14m)
			.SetDisplay("Minimum Distance (pips)", "Required distance to the pivot target", "Filters")
			.SetCanOptimize(true);
		_emaSpreadPips = Param(nameof(EmaSpreadPips), 5m)
			.SetDisplay("EMA Spread (pips)", "Minimum spread between close/open EMAs", "Filters")
			.SetCanOptimize(true);
		_adxThreshold = Param(nameof(AdxThreshold), 20)
			.SetDisplay("ADX Threshold", "Minimum ADX value", "Filters")
			.SetCanOptimize(true);
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "ADX calculation period", "Indicators")
			.SetCanOptimize(true);
		_emaClosePeriod = Param(nameof(EmaClosePeriod), 5)
			.SetDisplay("EMA Close Period", "Length for EMA on closes", "Indicators")
			.SetCanOptimize(true);
		_emaOpenPeriod = Param(nameof(EmaOpenPeriod), 8)
			.SetDisplay("EMA Open Period", "Length for EMA on opens", "Indicators")
			.SetCanOptimize(true);
		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetDisplay("MACD Fast Period", "Fast EMA length", "Indicators")
			.SetCanOptimize(true);
		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow Period", "Slow EMA length", "Indicators")
			.SetCanOptimize(true);
		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal Period", "Signal EMA length", "Indicators")
			.SetCanOptimize(true);
		_hourlyCandleType = Param(nameof(HourlyCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Trading Candle Type", "Intraday candle type", "Data");
		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Daily Candle Type", "Daily candles for pivot calculation", "Data");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		// Clear cached indicator values and pivot information.
		_previousEmaClose = null;
		_previousEmaOpen = null;
		_previousOsma = null;
		_previousPlusDi = null;
		_previousMinusDi = null;
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

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
		{
			LogError("Trailing stop requires a positive trailing step.");
			Stop();
			return;
		}

		_pipSize = GetPipSize();

		_emaClose = new EMA { Length = EmaClosePeriod };
		_emaOpen = new EMA { Length = EmaOpenPeriod };
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_macd = new MovingAverageConvergenceDivergence
		{
			ShortLength = MacdFastPeriod,
			LongLength = MacdSlowPeriod,
			SignalLength = MacdSignalPeriod
		};

		SubscribeCandles(DailyCandleType).Bind(ProcessDailyCandle).Start();

		var subscription = SubscribeCandles(HourlyCandleType);
		subscription.BindEx(_adx, _macd, ProcessHourlyCandle).Start();

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
		if (candle.State != CandleStates.Finished)
			return;

		_previousDailyCandle = _lastDailyCandle;
		_lastDailyCandle = candle;
	}

	private void ProcessHourlyCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_emaClose == null || _emaOpen == null || _adx == null || _macd == null || _pipSize == null)
			return;

		var emaCloseValue = _emaClose.Process(new DecimalIndicatorValue(_emaClose, candle.ClosePrice, candle.OpenTime));
		var emaOpenValue = _emaOpen.Process(new DecimalIndicatorValue(_emaOpen, candle.OpenPrice, candle.OpenTime));

		if (!emaCloseValue.IsFinal || !emaOpenValue.IsFinal)
			return;

		if (!adxValue.IsFinal || adxValue is not AverageDirectionalIndexValue adxData)
			return;

		if (!macdValue.IsFinal || macdValue is not MovingAverageConvergenceDivergenceValue macdData)
			return;

		if (_previousDailyCandle == null)
			return;

		if (ManageOpenPositions(candle))
			return;

		var levels = CalculatePivotLevels(_previousDailyCandle);
		if (!TrySelectRange(candle.ClosePrice, levels, out var support, out var resistance))
		{
			UpdatePreviousValues(emaCloseValue.ToDecimal(), emaOpenValue.ToDecimal(), macdData.Histogram, adxData.Dx.Plus, adxData.Dx.Minus);
			return;
		}

		var pip = _pipSize.Value;
		var distanceToSupport = pip > 0m ? (candle.ClosePrice - support) / pip : 0m;
		var distanceToResistance = pip > 0m ? (resistance - candle.ClosePrice) / pip : 0m;

		var emaCloseCurrent = emaCloseValue.ToDecimal();
		var emaOpenCurrent = emaOpenValue.ToDecimal();
		var osmaCurrent = macdData.Histogram;
		var plusDi = adxData.Dx.Plus;
		var minusDi = adxData.Dx.Minus;
		var adx = adxData.MovingAverage;

		if (_previousEmaClose is not decimal prevEmaClose ||
			_previousEmaOpen is not decimal prevEmaOpen ||
			_previousOsma is not decimal prevOsma ||
			_previousPlusDi is not decimal prevPlusDi ||
			_previousMinusDi is not decimal prevMinusDi)
		{
			UpdatePreviousValues(emaCloseCurrent, emaOpenCurrent, osmaCurrent, plusDi, minusDi);
			return;
		}

		var emaSpreadThreshold = EmaSpreadPips * pip;
		var minimumDistance = MinimumDistancePips;

		var canOpenLong = Position <= 0 &&
			distanceToResistance > minimumDistance &&
			adx > AdxThreshold &&
			plusDi > prevPlusDi &&
			plusDi > minusDi &&
			emaCloseCurrent - emaOpenCurrent >= emaSpreadThreshold &&
			prevEmaClose > prevEmaOpen &&
			osmaCurrent > prevOsma;

		var canOpenShort = Position >= 0 &&
			distanceToSupport > minimumDistance &&
			adx > AdxThreshold &&
			minusDi > prevMinusDi &&
			plusDi < minusDi &&
			emaOpenCurrent - emaCloseCurrent >= emaSpreadThreshold &&
			prevEmaOpen > prevEmaClose &&
			osmaCurrent < prevOsma;

		if (canOpenLong)
		{
			OpenLong(candle.ClosePrice, resistance, pip);
			UpdatePreviousValues(emaCloseCurrent, emaOpenCurrent, osmaCurrent, plusDi, minusDi);
			return;
		}

		if (canOpenShort)
		{
			OpenShort(candle.ClosePrice, support, pip);
			UpdatePreviousValues(emaCloseCurrent, emaOpenCurrent, osmaCurrent, plusDi, minusDi);
			return;
		}

		UpdatePreviousValues(emaCloseCurrent, emaOpenCurrent, osmaCurrent, plusDi, minusDi);
	}

	private void OpenLong(decimal price, decimal target, decimal pip)
	{
		BuyMarket();
		_longEntryPrice = price;
		_longTarget = target;
		_longStop = StopLossPips > 0m ? price - StopLossPips * pip : null;
		_shortEntryPrice = null;
		_shortTarget = null;
		_shortStop = null;
	}

	private void OpenShort(decimal price, decimal target, decimal pip)
	{
		SellMarket();
		_shortEntryPrice = price;
		_shortTarget = target;
		_shortStop = StopLossPips > 0m ? price + StopLossPips * pip : null;
		_longEntryPrice = null;
		_longTarget = null;
		_longStop = null;
	}

	private bool ManageOpenPositions(ICandleMessage candle)
	{
		if (_pipSize == null)
			return false;

		var pip = _pipSize.Value;

		if (Position > 0)
		{
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return true;
			}

			if (_longTarget.HasValue && candle.HighPrice >= _longTarget.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return true;
			}

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
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return true;
			}

			if (_shortTarget.HasValue && candle.LowPrice <= _shortTarget.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return true;
			}

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
		_previousEmaClose = emaClose;
		_previousEmaOpen = emaOpen;
		_previousOsma = osma;
		_previousPlusDi = plusDi;
		_previousMinusDi = minusDi;
	}

	private static (decimal Pivot, decimal R1, decimal R2, decimal R3, decimal S1, decimal S2, decimal S3, decimal M0, decimal M1, decimal M2, decimal M3, decimal M4, decimal M5) CalculatePivotLevels(ICandleMessage candle)
	{
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;
		var pivot = (high + low + close) / 3m;
		var r1 = 2m * pivot - low;
		var s1 = 2m * pivot - high;
		var range = high - low;
		var r2 = pivot + range;
		var s2 = pivot - range;
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
