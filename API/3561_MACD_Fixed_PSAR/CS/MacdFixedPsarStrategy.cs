using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor EA_MACD_FixedPSAR.
/// Combines MACD crossovers with an EMA trend filter, optional fixed or PSAR trailing stops,
/// and classic take-profit / stop-loss management expressed in pips.
/// </summary>
public class MacdFixedPsarStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<TrailingMode> _trailingMode;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _psarStep;
	private readonly StrategyParam<decimal> _psarMaximum;
	private readonly StrategyParam<decimal> _macdOpenLevelPips;
	private readonly StrategyParam<decimal> _macdCloseLevelPips;
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal? _macd;
	private ExponentialMovingAverage? _trendEma;

	private bool _hasPrevValues;
	private decimal _prevMacd;
	private decimal _prevSignal;
	private decimal _prevEma;

	private decimal _pipSize;
	private decimal _macdOpenThreshold;
	private decimal _macdCloseThreshold;
	private decimal _takeProfitDistance;
	private decimal _stopLossDistance;
	private decimal _trailingStopDistance;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;

	private decimal _longPsarExtreme;
	private decimal _shortPsarExtreme;
	private decimal _longPsarStep;
	private decimal _shortPsarStep;

	/// <summary>
	/// Trailing modes matching the original expert advisor options.
	/// </summary>
	public enum TrailingMode
	{
		/// <summary>
		/// Trailing is disabled and only the initial stop-loss is used.
		/// </summary>
		None = 0,

		/// <summary>
		/// Uses a fixed distance trailing stop measured in pips.
		/// </summary>
		Fixed = 1,

		/// <summary>
		/// Applies the incremental Parabolic SAR style trailing stop.
		/// </summary>
		FixedPsar = 2,
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
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Selected trailing stop mode.
	/// </summary>
	public TrailingMode TrailMode
	{
		get => _trailingMode.Value;
		set => _trailingMode.Value = value;
	}

	/// <summary>
	/// Fixed trailing stop distance in pips (used when <see cref="TrailMode"/> equals <see cref="TrailingMode.Fixed"/>).
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Initial increment for the Parabolic SAR trailing mode.
	/// </summary>
	public decimal PsarStep
	{
		get => _psarStep.Value;
		set => _psarStep.Value = value;
	}

	/// <summary>
	/// Maximum increment allowed for the Parabolic SAR trailing mode.
	/// </summary>
	public decimal PsarMaximum
	{
		get => _psarMaximum.Value;
		set => _psarMaximum.Value = value;
	}

	/// <summary>
	/// MACD magnitude required to open new positions, measured in pips.
	/// </summary>
	public decimal MacdOpenLevelPips
	{
		get => _macdOpenLevelPips.Value;
		set => _macdOpenLevelPips.Value = value;
	}

	/// <summary>
	/// MACD magnitude required to close existing positions, measured in pips.
	/// </summary>
	public decimal MacdCloseLevelPips
	{
		get => _macdCloseLevelPips.Value;
		set => _macdCloseLevelPips.Value = value;
	}

	/// <summary>
	/// EMA period used as trend confirmation for MACD entries.
	/// </summary>
	public int TrendPeriod
	{
		get => _trendPeriod.Value;
		set => _trendPeriod.Value = value;
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
	/// Initialize all configurable parameters.
	/// </summary>
	public MacdFixedPsarStrategy()
	{

		_takeProfitPips = Param(nameof(TakeProfitPips), 200m)
		.SetCanOptimize(true)
		.SetDisplay("Take Profit (pips)", "Take-profit distance expressed in pips", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetCanOptimize(true)
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance expressed in pips", "Risk");

		_trailingMode = Param(nameof(TrailMode), TrailingMode.FixedPsar)
		.SetDisplay("Trailing Mode", "Trailing logic: None, Fixed, or Fixed PSAR", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 30m)
		.SetDisplay("Trailing Stop (pips)", "Trailing distance used in fixed mode", "Risk");

		_psarStep = Param(nameof(PsarStep), 0.02m)
		.SetDisplay("PSAR Step", "Initial Parabolic SAR acceleration factor", "Risk");

		_psarMaximum = Param(nameof(PsarMaximum), 0.2m)
		.SetDisplay("PSAR Maximum", "Maximum Parabolic SAR acceleration factor", "Risk");

		_macdOpenLevelPips = Param(nameof(MacdOpenLevelPips), 3m)
		.SetDisplay("MACD Open Level", "MACD magnitude in pips required for entry", "Indicators");

		_macdCloseLevelPips = Param(nameof(MacdCloseLevelPips), 2m)
		.SetDisplay("MACD Close Level", "MACD magnitude in pips required for exit", "Indicators");

		_trendPeriod = Param(nameof(TrendPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("Trend EMA Period", "EMA period used as trend filter", "Indicators");

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
		_stopLossDistance = 0m;
		_trailingStopDistance = 0m;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakeProfitPrice = null;
		_shortTakeProfitPrice = null;
		_longPsarExtreme = 0m;
		_shortPsarExtreme = 0m;
		_longPsarStep = 0m;
		_shortPsarStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_hasPrevValues = false;
		_prevMacd = 0m;
		_prevSignal = 0m;
		_prevEma = 0m;

		_pipSize = CalculatePipSize();
		_macdOpenThreshold = ConvertPipsToPrice(MacdOpenLevelPips);
		_macdCloseThreshold = ConvertPipsToPrice(MacdCloseLevelPips);
		_takeProfitDistance = ConvertPipsToPrice(TakeProfitPips);
		_stopLossDistance = ConvertPipsToPrice(StopLossPips);
		_trailingStopDistance = ConvertPipsToPrice(TrailingStopPips);

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 },
			},
			SignalMa = { Length = 9 }
		};

		_trendEma = new ExponentialMovingAverage { Length = TrendPeriod };

		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakeProfitPrice = null;
		_shortTakeProfitPrice = null;
		_longPsarExtreme = 0m;
		_shortPsarExtreme = 0m;
		_longPsarStep = PsarStep;
		_shortPsarStep = PsarStep;

		StartProtection();

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
				BuyMarket(Volume);
				BeginLongPosition(candle.ClosePrice);
				UpdatePreviousValues(macdCurrent, signalCurrent, emaCurrent);
				return;
			}

			if (ShouldOpenShort(macdCurrent, crossedBelow, emaCurrent))
			{
				SellMarket(Volume);
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
		var exitByTakeProfit = _longTakeProfitPrice is decimal tp && candle.HighPrice >= tp;
		var exitByStop = _longStopPrice is decimal sl && candle.LowPrice <= sl;

		if (exitByTakeProfit)
		{
			LogInfo($"Exit long by take-profit at {candle.HighPrice:F5}");
			return true;
		}

		if (exitByStop)
		{
			LogInfo($"Exit long by protective stop at {candle.LowPrice:F5}");
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
		var exitByTakeProfit = _shortTakeProfitPrice is decimal tp && candle.LowPrice <= tp;
		var exitByStop = _shortStopPrice is decimal sl && candle.HighPrice >= sl;

		if (exitByTakeProfit)
		{
			LogInfo($"Exit short by take-profit at {candle.LowPrice:F5}");
			return true;
		}

		if (exitByStop)
		{
			LogInfo($"Exit short by protective stop at {candle.HighPrice:F5}");
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
		if (macdCurrent >= 0m)
		return false;

		if (!crossedAbove)
		return false;

		if (Math.Abs(macdCurrent) <= _macdOpenThreshold)
		return false;

		if (emaCurrent <= _prevEma)
		return false;

		return true;
	}

	private bool ShouldOpenShort(decimal macdCurrent, bool crossedBelow, decimal emaCurrent)
	{
		if (macdCurrent <= 0m)
		return false;

		if (!crossedBelow)
		return false;

		if (Math.Abs(macdCurrent) <= _macdOpenThreshold)
		return false;

		if (emaCurrent >= _prevEma)
		return false;

		return true;
	}

	private void BeginLongPosition(decimal entryPrice)
	{
		_longEntryPrice = entryPrice;
		_longStopPrice = _stopLossDistance > 0m ? entryPrice - _stopLossDistance : null;
		_longTakeProfitPrice = _takeProfitDistance > 0m ? entryPrice + _takeProfitDistance : null;
		_longPsarExtreme = entryPrice;
		_longPsarStep = PsarStep;
	}

	private void BeginShortPosition(decimal entryPrice)
	{
		_shortEntryPrice = entryPrice;
		_shortStopPrice = _stopLossDistance > 0m ? entryPrice + _stopLossDistance : null;
		_shortTakeProfitPrice = _takeProfitDistance > 0m ? entryPrice - _takeProfitDistance : null;
		_shortPsarExtreme = entryPrice;
		_shortPsarStep = PsarStep;
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (_longEntryPrice is null)
		return;

		switch (TrailMode)
		{
			case TrailingMode.None:
			return;

			case TrailingMode.Fixed:
			UpdateLongFixedTrailing(candle);
			break;

			case TrailingMode.FixedPsar:
			UpdateLongPsarTrailing(candle);
			break;
		}
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (_shortEntryPrice is null)
		return;

		switch (TrailMode)
		{
			case TrailingMode.None:
			return;

			case TrailingMode.Fixed:
			UpdateShortFixedTrailing(candle);
			break;

			case TrailingMode.FixedPsar:
			UpdateShortPsarTrailing(candle);
			break;
		}
	}

	private void UpdateLongFixedTrailing(ICandleMessage candle)
	{
		if (_trailingStopDistance <= 0m)
		return;

		var potentialStop = candle.ClosePrice - _trailingStopDistance;
		if (potentialStop <= candle.LowPrice)
		return;

		if (_longStopPrice is decimal currentStop)
		{
			if (potentialStop > currentStop)
			_longStopPrice = potentialStop;
		}
		else
		{
			_longStopPrice = potentialStop;
		}
	}

	private void UpdateShortFixedTrailing(ICandleMessage candle)
	{
		if (_trailingStopDistance <= 0m)
		return;

		var potentialStop = candle.ClosePrice + _trailingStopDistance;
		if (potentialStop >= candle.HighPrice)
		return;

		if (_shortStopPrice is decimal currentStop)
		{
			if (potentialStop < currentStop)
			_shortStopPrice = potentialStop;
		}
		else
		{
			_shortStopPrice = potentialStop;
		}
	}

	private void UpdateLongPsarTrailing(ICandleMessage candle)
	{
		if (_longEntryPrice is not decimal entry)
		return;

		if (candle.HighPrice <= _longPsarExtreme)
		return;

		_longPsarExtreme = candle.HighPrice;
		_longPsarStep = Math.Min(PsarMaximum, _longPsarStep + PsarStep);

		var baseStop = _longStopPrice ?? (_stopLossDistance > 0m ? entry - _stopLossDistance : 0m);
		var newStop = baseStop + (_longPsarExtreme - baseStop) * _longPsarStep;

		if (newStop >= candle.ClosePrice)
		return;

		if (_longStopPrice is null || newStop > _longStopPrice.Value)
		_longStopPrice = newStop;
	}

	private void UpdateShortPsarTrailing(ICandleMessage candle)
	{
		if (_shortEntryPrice is not decimal entry)
		return;

		if (candle.LowPrice >= _shortPsarExtreme)
		return;

		_shortPsarExtreme = candle.LowPrice;
		_shortPsarStep = Math.Min(PsarMaximum, _shortPsarStep + PsarStep);

		var baseStop = _shortStopPrice ?? (_stopLossDistance > 0m ? entry + _stopLossDistance : 0m);
		var newStop = baseStop - (baseStop - _shortPsarExtreme) * _shortPsarStep;

		if (newStop <= candle.ClosePrice)
		return;

		if (_shortStopPrice is null || newStop < _shortStopPrice.Value)
		_shortStopPrice = newStop;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
		_longPsarExtreme = 0m;
		_longPsarStep = PsarStep;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
		_shortPsarExtreme = 0m;
		_shortPsarStep = PsarStep;
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
