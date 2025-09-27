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
/// MACD Sample strategy converted from MetaTrader expert advisor.
/// Uses MACD crossovers with an EMA trend filter, configurable pip thresholds,
/// take-profit and trailing stop management.
/// </summary>
public class MacdSampleTrendFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<decimal> _macdOpenLevelPips;
	private readonly StrategyParam<decimal> _macdCloseLevelPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd;
	private ExponentialMovingAverage _trendEma;

	private bool _hasPrevValues;
	private decimal _prevMacd;
	private decimal _prevSignal;
	private decimal _prevEma;

	private decimal _pipSize;
	private decimal _macdOpenThreshold;
	private decimal _macdCloseThreshold;
	private decimal _takeProfitDistance;
	private decimal _trailingStopDistance;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Fast EMA period used in MACD.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period used in MACD.
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
	/// EMA period used as trend filter.
	/// </summary>
	public int TrendPeriod
	{
		get => _trendPeriod.Value;
		set => _trendPeriod.Value = value;
	}

	/// <summary>
	/// MACD magnitude required to open a position, measured in pips.
	/// </summary>
	public decimal MacdOpenLevelPips
	{
		get => _macdOpenLevelPips.Value;
		set => _macdOpenLevelPips.Value = value;
	}

	/// <summary>
	/// MACD magnitude required to close a position, measured in pips.
	/// </summary>
	public decimal MacdCloseLevelPips
	{
		get => _macdCloseLevelPips.Value;
		set => _macdCloseLevelPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize parameters for MACD Sample strategy.
	/// </summary>
	public MacdSampleTrendFilterStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Period", "Fast EMA period for MACD", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Period", "Slow EMA period for MACD", "Indicators");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Signal line period", "Indicators");

		_trendPeriod = Param(nameof(TrendPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Trend EMA Period", "EMA period for trend filter", "Indicators");

		_macdOpenLevelPips = Param(nameof(MacdOpenLevelPips), 3m)
			.SetDisplay("MACD Open Level", "MACD magnitude in pips required for entry", "Thresholds");

		_macdCloseLevelPips = Param(nameof(MacdCloseLevelPips), 2m)
			.SetDisplay("MACD Close Level", "MACD magnitude in pips required for exit", "Thresholds");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk management");

		_trailingStopPips = Param(nameof(TrailingStopPips), 30m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for processing", "General");
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

		_macd = null;
		_trendEma = null;
		_hasPrevValues = false;
		_prevMacd = 0m;
		_prevSignal = 0m;
		_prevEma = 0m;
		_pipSize = 0m;
		_macdOpenThreshold = 0m;
		_macdCloseThreshold = 0m;
		_takeProfitDistance = 0m;
		_trailingStopDistance = 0m;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_hasPrevValues = false;
		_prevMacd = 0m;
		_prevSignal = 0m;
		_prevEma = 0m;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		_trendEma = new ExponentialMovingAverage { Length = TrendPeriod };

		_pipSize = CalculatePipSize();
		_macdOpenThreshold = ConvertPipsToPrice(MacdOpenLevelPips);
		_macdCloseThreshold = ConvertPipsToPrice(MacdCloseLevelPips);
		_takeProfitDistance = ConvertPipsToPrice(TakeProfitPips);
		_trailingStopDistance = ConvertPipsToPrice(TrailingStopPips);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _trendEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _trendEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal || !emaValue.IsFinal)
			return;

		if (_macd == null || _trendEma == null)
			return;

		if (!_macd.IsFormed || !_trendEma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdData = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdCurrent = macdData.Macd;
		var signalCurrent = macdData.Signal;
		var emaCurrent = emaValue.ToDecimal();

		if (!_hasPrevValues)
		{
			_prevMacd = macdCurrent;
			_prevSignal = signalCurrent;
			_prevEma = emaCurrent;
			_hasPrevValues = true;
			return;
		}

		var crossedAbove = macdCurrent > signalCurrent && _prevMacd <= _prevSignal;
		var crossedBelow = macdCurrent < signalCurrent && _prevMacd >= _prevSignal;

		if (Position > 0)
		{
			if (ShouldExitLong(candle, macdCurrent, crossedBelow))
			{
				SellMarket(Position);
				ResetLongState();
				UpdatePreviousValues(macdCurrent, signalCurrent, emaCurrent);
				return;
			}

			UpdateLongTrailing(candle);
		}
		else if (Position < 0)
		{
			if (ShouldExitShort(candle, macdCurrent, crossedAbove))
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				UpdatePreviousValues(macdCurrent, signalCurrent, emaCurrent);
				return;
			}

			UpdateShortTrailing(candle);
		}
		else
		{
			if (ShouldOpenLong(macdCurrent, crossedAbove, emaCurrent))
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				BeginLongPosition(candle.ClosePrice);
				UpdatePreviousValues(macdCurrent, signalCurrent, emaCurrent);
				return;
			}

			if (ShouldOpenShort(macdCurrent, crossedBelow, emaCurrent))
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				BeginShortPosition(candle.ClosePrice);
				UpdatePreviousValues(macdCurrent, signalCurrent, emaCurrent);
				return;
			}
		}

		UpdatePreviousValues(macdCurrent, signalCurrent, emaCurrent);
	}

	private bool ShouldExitLong(ICandleMessage candle, decimal macdCurrent, bool crossedBelow)
	{
		var exitByMacd = macdCurrent > 0m && crossedBelow && macdCurrent > _macdCloseThreshold;
		var exitByTakeProfit = _longEntryPrice is decimal entry && _takeProfitDistance > 0m && candle.HighPrice >= entry + _takeProfitDistance;
		var exitByTrailing = _longTrailingStop is decimal trailing && candle.LowPrice <= trailing;

		if (exitByTakeProfit)
		{
			LogInfo($"Exit long by take-profit at {candle.HighPrice:F5}");
			return true;
		}

		if (exitByTrailing)
		{
			LogInfo($"Exit long by trailing stop at {candle.LowPrice:F5}");
			return true;
		}

		if (exitByMacd)
		{
			LogInfo($"Exit long by MACD crossover at {candle.ClosePrice:F5}");
			return true;
		}

		return false;
	}

	private bool ShouldExitShort(ICandleMessage candle, decimal macdCurrent, bool crossedAbove)
	{
		var exitByMacd = macdCurrent < 0m && crossedAbove && Math.Abs(macdCurrent) > _macdCloseThreshold;
		var exitByTakeProfit = _shortEntryPrice is decimal entry && _takeProfitDistance > 0m && candle.LowPrice <= entry - _takeProfitDistance;
		var exitByTrailing = _shortTrailingStop is decimal trailing && candle.HighPrice >= trailing;

		if (exitByTakeProfit)
		{
			LogInfo($"Exit short by take-profit at {candle.LowPrice:F5}");
			return true;
		}

		if (exitByTrailing)
		{
			LogInfo($"Exit short by trailing stop at {candle.HighPrice:F5}");
			return true;
		}

		if (exitByMacd)
		{
			LogInfo($"Exit short by MACD crossover at {candle.ClosePrice:F5}");
			return true;
		}

		return false;
	}

	private bool ShouldOpenLong(decimal macdCurrent, bool crossedAbove, decimal emaCurrent)
	{
		var hasMacdStrength = Math.Abs(macdCurrent) > _macdOpenThreshold;
		var emaRising = emaCurrent > _prevEma;

		if (macdCurrent < 0m && crossedAbove && hasMacdStrength && emaRising)
		{
			LogInfo($"Open long: MACD {macdCurrent:F5}, Signal crossover confirmed.");
			return true;
		}

		return false;
	}

	private bool ShouldOpenShort(decimal macdCurrent, bool crossedBelow, decimal emaCurrent)
	{
		var hasMacdStrength = macdCurrent > _macdOpenThreshold;
		var emaFalling = emaCurrent < _prevEma;

		if (macdCurrent > 0m && crossedBelow && hasMacdStrength && emaFalling)
		{
			LogInfo($"Open short: MACD {macdCurrent:F5}, Signal crossover confirmed.");
			return true;
		}

		return false;
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (_trailingStopDistance <= 0m || _longEntryPrice is not decimal entry)
			return;

		var profit = candle.HighPrice - entry;
		if (profit <= _trailingStopDistance)
			return;

		var newStop = candle.HighPrice - _trailingStopDistance;
		if (newStop < entry)
			newStop = entry;

		if (_longTrailingStop is decimal existing)
		{
			if (newStop > existing)
			{
				_longTrailingStop = newStop;
				LogInfo($"Update long trailing stop to {newStop:F5}");
			}
		}
		else
		{
			_longTrailingStop = newStop;
			LogInfo($"Activate long trailing stop at {newStop:F5}");
		}
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (_trailingStopDistance <= 0m || _shortEntryPrice is not decimal entry)
			return;

		var profit = entry - candle.LowPrice;
		if (profit <= _trailingStopDistance)
			return;

		var newStop = candle.LowPrice + _trailingStopDistance;
		if (newStop > entry)
			newStop = entry;

		if (_shortTrailingStop is decimal existing)
		{
			if (newStop < existing)
			{
				_shortTrailingStop = newStop;
				LogInfo($"Update short trailing stop to {newStop:F5}");
			}
		}
		else
		{
			_shortTrailingStop = newStop;
			LogInfo($"Activate short trailing stop at {newStop:F5}");
		}
	}

	private void BeginLongPosition(decimal entryPrice)
	{
		_longEntryPrice = entryPrice;
		_longTrailingStop = null;
		_shortEntryPrice = null;
		_shortTrailingStop = null;
		LogInfo($"Entered long at {entryPrice:F5}");
	}

	private void BeginShortPosition(decimal entryPrice)
	{
		_shortEntryPrice = entryPrice;
		_shortTrailingStop = null;
		_longEntryPrice = null;
		_longTrailingStop = null;
		LogInfo($"Entered short at {entryPrice:F5}");
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longTrailingStop = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortTrailingStop = null;
	}

	private void UpdatePreviousValues(decimal macdCurrent, decimal signalCurrent, decimal emaCurrent)
	{
		_prevMacd = macdCurrent;
		_prevSignal = signalCurrent;
		_prevEma = emaCurrent;
	}

	private decimal ConvertPipsToPrice(decimal value)
	{
		if (value <= 0m)
			return 0m;

		if (_pipSize > 0m)
			return value * _pipSize;

		return value;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 0m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;

		var scale = (decimal.GetBits(step)[3] >> 16) & 0x7F;
		if (scale == 3 || scale == 5)
			return step * 10m;

		return step;
	}
}