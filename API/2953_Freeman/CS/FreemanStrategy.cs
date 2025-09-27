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

public class FreemanStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _filterCandleType;
	private readonly StrategyParam<int> _firstMaPeriod;
	private readonly StrategyParam<int> _secondMaPeriod;
	private readonly StrategyParam<int> _filterMaPeriod;
	private readonly StrategyParam<int> _filterRsiPeriod;
	private readonly StrategyParam<MovingAverageTypes> _maType;
	private readonly StrategyParam<int> _rsiFirstPeriod;
	private readonly StrategyParam<int> _rsiSecondPeriod;
	private readonly StrategyParam<int> _rsiSellLevel;
	private readonly StrategyParam<int> _rsiBuyLevel;
	private readonly StrategyParam<int> _rsiSellLevel2;
	private readonly StrategyParam<int> _rsiBuyLevel2;
	private readonly StrategyParam<bool> _useRsiTeacher1;
	private readonly StrategyParam<bool> _useRsiTeacher2;
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<decimal> _stopLossAtrFactor;
	private readonly StrategyParam<decimal> _takeProfitAtrFactor;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _positionsMaximum;
	private readonly StrategyParam<decimal> _distancePips;
	private readonly StrategyParam<bool> _tradeOnFriday;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _lockCoefficient;
	private readonly StrategyParam<int> _signalShift;

	private readonly Queue<decimal> _fastMaValues = new();
	private readonly Queue<decimal> _slowMaValues = new();
	private readonly Queue<decimal> _rsiFirstValues = new();
	private readonly Queue<decimal> _rsiSecondValues = new();
	private readonly Queue<decimal> _filterMaValues = new();
	private readonly Queue<decimal> _rsiFilterValues = new();

	private decimal _priceStep;
	private int _signalBufferLength;
	private decimal _distancePrice;
	private decimal _trailingStopOffset;
	private decimal _trailingStepOffset;

	private decimal _longVolume;
	private decimal _shortVolume;
	private int _longEntries;
	private int _shortEntries;
	private decimal _lastLongEntryPrice;
	private decimal _lastShortEntryPrice;
	private bool _lastLongExitWasLoss;
	private bool _lastShortExitWasLoss;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	/// <summary>
	/// Type of candles used for core calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Type of candles used for higher timeframe filters.
	/// </summary>
	public DataType FilterCandleType
	{
		get => _filterCandleType.Value;
		set => _filterCandleType.Value = value;
	}

	/// <summary>
	/// Length of the fast moving average used in the signal block.
	/// </summary>
	public int FirstMaPeriod
	{
		get => _firstMaPeriod.Value;
		set => _firstMaPeriod.Value = value;
	}

	/// <summary>
	/// Length of the slow moving average used in the signal block.
	/// </summary>
	public int SecondMaPeriod
	{
		get => _secondMaPeriod.Value;
		set => _secondMaPeriod.Value = value;
	}

	/// <summary>
	/// Length of the filter moving average calculated on the higher timeframe.
	/// </summary>
	public int FilterMaPeriod
	{
		get => _filterMaPeriod.Value;
		set => _filterMaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the RSI filter applied on the higher timeframe.
	/// </summary>
	public int FilterRsiPeriod
	{
		get => _filterRsiPeriod.Value;
		set => _filterRsiPeriod.Value = value;
	}

	/// <summary>
	/// Moving average type shared by all smoothing blocks.
	/// </summary>
	public MovingAverageTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Period of the first RSI module.
	/// </summary>
	public int RsiFirstPeriod
	{
		get => _rsiFirstPeriod.Value;
		set => _rsiFirstPeriod.Value = value;
	}

	/// <summary>
	/// Period of the second RSI module.
	/// </summary>
	public int RsiSecondPeriod
	{
		get => _rsiSecondPeriod.Value;
		set => _rsiSecondPeriod.Value = value;
	}

	/// <summary>
	/// Oversold threshold for the first RSI block.
	/// </summary>
	public int RsiSellLevel
	{
		get => _rsiSellLevel.Value;
		set => _rsiSellLevel.Value = value;
	}

	/// <summary>
	/// Overbought threshold for the first RSI block.
	/// </summary>
	public int RsiBuyLevel
	{
		get => _rsiBuyLevel.Value;
		set => _rsiBuyLevel.Value = value;
	}

	/// <summary>
	/// Oversold threshold for the second RSI block.
	/// </summary>
	public int RsiSellLevel2
	{
		get => _rsiSellLevel2.Value;
		set => _rsiSellLevel2.Value = value;
	}

	/// <summary>
	/// Overbought threshold for the second RSI block.
	/// </summary>
	public int RsiBuyLevel2
	{
		get => _rsiBuyLevel2.Value;
		set => _rsiBuyLevel2.Value = value;
	}

	/// <summary>
	/// Enables the first RSI teacher block.
	/// </summary>
	public bool UseRsiTeacher1
	{
		get => _useRsiTeacher1.Value;
		set => _useRsiTeacher1.Value = value;
	}

	/// <summary>
	/// Enables the second RSI teacher block.
	/// </summary>
	public bool UseRsiTeacher2
	{
		get => _useRsiTeacher2.Value;
		set => _useRsiTeacher2.Value = value;
	}

	/// <summary>
	/// Enables the higher timeframe trend filter.
	/// </summary>
	public bool UseTrendFilter
	{
		get => _useTrendFilter.Value;
		set => _useTrendFilter.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop-loss placement.
	/// </summary>
	public decimal StopLossAtrFactor
	{
		get => _stopLossAtrFactor.Value;
		set => _stopLossAtrFactor.Value = value;
	}

	/// <summary>
	/// ATR multiplier for take-profit placement.
	/// </summary>
	public decimal TakeProfitAtrFactor
	{
		get => _takeProfitAtrFactor.Value;
		set => _takeProfitAtrFactor.Value = value;
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
	/// Minimum step for trailing stop updates in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open entries.
	/// </summary>
	public int PositionsMaximum
	{
		get => _positionsMaximum.Value;
		set => _positionsMaximum.Value = value;
	}

	/// <summary>
	/// Minimum distance between consecutive entries in pips.
	/// </summary>
	public decimal DistancePips
	{
		get => _distancePips.Value;
		set => _distancePips.Value = value;
	}

	/// <summary>
	/// Allows trading on Fridays when enabled.
	/// </summary>
	public bool TradeOnFriday
	{
		get => _tradeOnFriday.Value;
		set => _tradeOnFriday.Value = value;
	}

	/// <summary>
	/// Trading session start hour in exchange time.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading session end hour in exchange time.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Multiplier used after a losing exit when stacking positions.
	/// </summary>
	public decimal LockCoefficient
	{
		get => _lockCoefficient.Value;
		set => _lockCoefficient.Value = value;
	}

	/// <summary>
	/// Offset applied when reading indicator values.
	/// </summary>
	public int SignalShift
	{
		get => _signalShift.Value;
		set => _signalShift.Value = value;
	}

	public FreemanStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for signals", "General");

		_filterCandleType = Param(nameof(FilterCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Filter Candle Type", "Higher timeframe used for filters", "General");

		_firstMaPeriod = Param(nameof(FirstMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Period of the first moving average", "Indicators");

		_secondMaPeriod = Param(nameof(SecondMaPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Period of the second moving average", "Indicators");

		_filterMaPeriod = Param(nameof(FilterMaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Filter MA Period", "Period of the higher timeframe moving average", "Indicators");

		_filterRsiPeriod = Param(nameof(FilterRsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Filter RSI Period", "Period of the higher timeframe RSI filter", "Indicators");

		_maType = Param(nameof(MaType), MovingAverageTypes.Simple)
			.SetDisplay("MA Type", "Type of moving averages", "Indicators");

		_rsiFirstPeriod = Param(nameof(RsiFirstPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("RSI #1 Period", "Period of the first RSI", "Indicators");

		_rsiSecondPeriod = Param(nameof(RsiSecondPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("RSI #2 Period", "Period of the second RSI", "Indicators");

		_rsiSellLevel = Param(nameof(RsiSellLevel), 34)
			.SetDisplay("RSI #1 Buy Threshold", "Oversold level for RSI #1", "Signals");

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 70)
			.SetDisplay("RSI #1 Sell Threshold", "Overbought level for RSI #1", "Signals");

		_rsiSellLevel2 = Param(nameof(RsiSellLevel2), 34)
			.SetDisplay("RSI #2 Buy Threshold", "Oversold level for RSI #2", "Signals");

		_rsiBuyLevel2 = Param(nameof(RsiBuyLevel2), 68)
			.SetDisplay("RSI #2 Sell Threshold", "Overbought level for RSI #2", "Signals");

		_useRsiTeacher1 = Param(nameof(UseRsiTeacher1), true)
			.SetDisplay("Use RSI Teacher #1", "Enable the first RSI logic block", "Signals");

		_useRsiTeacher2 = Param(nameof(UseRsiTeacher2), true)
			.SetDisplay("Use RSI Teacher #2", "Enable the second RSI logic block", "Signals");

		_useTrendFilter = Param(nameof(UseTrendFilter), false)
			.SetDisplay("Use Trend Filter", "Confirm entries with higher timeframe MA", "Signals");

		_stopLossAtrFactor = Param(nameof(StopLossAtrFactor), 14m)
			.SetNotNegative()
			.SetDisplay("SL ATR Factor", "Stop-loss multiplier over ATR", "Risk");

		_takeProfitAtrFactor = Param(nameof(TakeProfitAtrFactor), 2m)
			.SetNotNegative()
			.SetDisplay("TP ATR Factor", "Take-profit multiplier over ATR", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Distance for trailing stop", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Minimum move before trailing update", "Risk");

		_positionsMaximum = Param(nameof(PositionsMaximum), 5)
			.SetNotNegative()
			.SetDisplay("Positions Maximum", "Maximum simultaneous entries", "Money Management");

		_distancePips = Param(nameof(DistancePips), 10m)
			.SetNotNegative()
			.SetDisplay("Distance (pips)", "Minimum distance between entries", "Money Management");

		_tradeOnFriday = Param(nameof(TradeOnFriday), true)
			.SetDisplay("Trade On Friday", "Allow signals on Fridays", "Trading Hours");

		_startHour = Param(nameof(StartHour), 0)
			.SetNotNegative()
			.SetDisplay("Start Hour", "Trading start hour", "Trading Hours");

		_endHour = Param(nameof(EndHour), 0)
			.SetNotNegative()
			.SetDisplay("End Hour", "Trading end hour", "Trading Hours");

		_lockCoefficient = Param(nameof(LockCoefficient), 1.61m)
			.SetGreaterThanZero()
			.SetDisplay("Lock Coefficient", "Multiplier after losing exits", "Money Management");

		_signalShift = Param(nameof(SignalShift), 0)
			.SetNotNegative()
			.SetDisplay("Signal Shift", "Indicator shift used for signals", "Indicators");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		if (!Equals(CandleType, FilterCandleType))
			yield return (Security, FilterCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastMaValues.Clear();
		_slowMaValues.Clear();
		_rsiFirstValues.Clear();
		_rsiSecondValues.Clear();
		_filterMaValues.Clear();
		_rsiFilterValues.Clear();

		_longVolume = 0;
		_shortVolume = 0;
		_longEntries = 0;
		_shortEntries = 0;
		_lastLongEntryPrice = 0;
		_lastShortEntryPrice = 0;
		_lastLongExitWasLoss = false;
		_lastShortExitWasLoss = false;
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_signalBufferLength = Math.Max(2, SignalShift + 2);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_signalBufferLength = Math.Max(2, SignalShift + 2);

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0)
			_priceStep = 1m;

		_distancePrice = DistancePips * _priceStep;
		_trailingStopOffset = TrailingStopPips * _priceStep;
		_trailingStepOffset = TrailingStepPips * _priceStep;

		var fastMa = CreateMovingAverage(MaType, FirstMaPeriod);
		var slowMa = CreateMovingAverage(MaType, SecondMaPeriod);
		var atr = new AverageTrueRange { Length = FirstMaPeriod };
		var rsiFirst = new RelativeStrengthIndex { Length = RsiFirstPeriod };
		var rsiSecond = new RelativeStrengthIndex { Length = RsiSecondPeriod };

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription.Bind(fastMa, slowMa, rsiFirst, rsiSecond, atr, ProcessMainCandle).Start();

		var filterMa = CreateMovingAverage(MaType, FilterMaPeriod);
		var filterRsi = new RelativeStrengthIndex { Length = FilterRsiPeriod };

		var filterSubscription = SubscribeCandles(FilterCandleType);
		filterSubscription.Bind(filterMa, filterRsi, ProcessFilterCandle).Start();
	}

	private void ProcessFilterCandle(ICandleMessage candle, decimal filterMaValue, decimal filterRsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateQueue(_filterMaValues, filterMaValue);
		UpdateQueue(_rsiFilterValues, filterRsiValue);
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal rsiFirstValue, decimal rsiSecondValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateTrailingStops(candle);
		CheckExitTargets(candle);

		UpdateQueue(_fastMaValues, fastValue);
		UpdateQueue(_slowMaValues, slowValue);
		_distancePrice = DistancePips * _priceStep;
		_trailingStopOffset = TrailingStopPips * _priceStep;
		_trailingStepOffset = TrailingStepPips * _priceStep;
		UpdateQueue(_rsiFirstValues, rsiFirstValue);
		UpdateQueue(_rsiSecondValues, rsiSecondValue);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsTradingTime(candle.OpenTime))
			return;

		if (!HasIndicatorData())
			return;

		var fastArray = _fastMaValues.ToArray();
		var slowArray = _slowMaValues.ToArray();
		var rsiFirstArray = _rsiFirstValues.ToArray();
		var rsiSecondArray = _rsiSecondValues.ToArray();
		var filterArray = _filterMaValues.ToArray();
		var filterRsiArray = _rsiFilterValues.ToArray();

		var index = fastArray.Length - 1 - SignalShift;
		var fastCurrent = fastArray[index];
		var fastPrevious = fastArray[index - 1];
		var slowCurrent = slowArray[index];
		var slowPrevious = slowArray[index - 1];
		var rsiFirstCurrent = rsiFirstArray[index];
		var rsiFirstPrevious = rsiFirstArray[index - 1];
		var rsiSecondCurrent = rsiSecondArray[index];
		var rsiSecondPrevious = rsiSecondArray[index - 1];

		var filterIndex = filterArray.Length - 1 - SignalShift;
		var filterCurrent = filterArray[filterIndex];
		var filterPrevious = filterArray[filterIndex - 1];
		var rsiFilterCurrent = filterRsiArray[filterIndex];
		var rsiFilterPrevious = filterRsiArray[filterIndex - 1];

		var buySignal = CalculateBuySignal(fastCurrent, fastPrevious, slowCurrent, slowPrevious, rsiFirstCurrent, rsiFirstPrevious, rsiSecondCurrent, rsiSecondPrevious, filterCurrent, filterPrevious, rsiFilterCurrent, rsiFilterPrevious);
		var sellSignal = CalculateSellSignal(fastCurrent, fastPrevious, slowCurrent, slowPrevious, rsiFirstCurrent, rsiFirstPrevious, rsiSecondCurrent, rsiSecondPrevious, filterCurrent, filterPrevious, rsiFilterCurrent, rsiFilterPrevious);

		var totalEntries = _longEntries + _shortEntries;
		var canOpenMore = PositionsMaximum == 0 || totalEntries < PositionsMaximum;

		if (canOpenMore)
		{
			if (_longVolume == 0 && _shortVolume == 0 && buySignal)
			{
				ExecuteLongEntry(candle, atrValue);
				return;
			}

			if (_shortVolume == 0 && _longVolume == 0 && sellSignal)
			{
				ExecuteShortEntry(candle, atrValue);
				return;
			}
		}

		if (_longVolume > 0 && buySignal && canOpenMore)
		{
			if (DistancePips == 0m || Math.Abs(candle.ClosePrice - _lastLongEntryPrice) > _distancePrice)
			{
				ExecuteLongEntry(candle, atrValue);
			}
		}
		else if (_shortVolume > 0 && sellSignal && canOpenMore)
		{
			if (DistancePips == 0m || Math.Abs(candle.ClosePrice - _lastShortEntryPrice) > _distancePrice)
			{
				ExecuteShortEntry(candle, atrValue);
			}
		}
	}

	private bool HasIndicatorData()
	{
		if (_fastMaValues.Count < _signalBufferLength)
			return false;

		if (_slowMaValues.Count < _signalBufferLength)
			return false;

		if (_rsiFirstValues.Count < _signalBufferLength)
			return false;

		if (_rsiSecondValues.Count < _signalBufferLength)
			return false;

		if (_filterMaValues.Count < _signalBufferLength)
			return false;

		if (_rsiFilterValues.Count < _signalBufferLength)
			return false;

		return true;
	}

	private void ExecuteLongEntry(ICandleMessage candle, decimal atrValue)
	{
		if (_shortVolume > 0)
			return;

		var volume = Volume;

		if (_longVolume > 0 && _lastLongExitWasLoss)
			volume *= LockCoefficient;

		if (volume <= 0)
			return;

		var stopPrice = StopLossAtrFactor > 0m && atrValue > 0m ? candle.ClosePrice - atrValue * StopLossAtrFactor : (decimal?)null;
		var takePrice = TakeProfitAtrFactor > 0m && atrValue > 0m ? candle.ClosePrice + atrValue * TakeProfitAtrFactor : (decimal?)null;

		if (stopPrice.HasValue && stopPrice.Value >= candle.ClosePrice)
			stopPrice = null;

		if (takePrice.HasValue && takePrice.Value <= candle.ClosePrice)
			takePrice = null;

		BuyMarket(volume);

		_lastLongEntryPrice = candle.ClosePrice;
		_longStopPrice = stopPrice;
		_longTakePrice = takePrice;
	}

	private void ExecuteShortEntry(ICandleMessage candle, decimal atrValue)
	{
		if (_longVolume > 0)
			return;

		var volume = Volume;

		if (_shortVolume > 0 && _lastShortExitWasLoss)
			volume *= LockCoefficient;

		if (volume <= 0)
			return;

		var stopPrice = StopLossAtrFactor > 0m && atrValue > 0m ? candle.ClosePrice + atrValue * StopLossAtrFactor : (decimal?)null;
		var takePrice = TakeProfitAtrFactor > 0m && atrValue > 0m ? candle.ClosePrice - atrValue * TakeProfitAtrFactor : (decimal?)null;

		if (stopPrice.HasValue && stopPrice.Value <= candle.ClosePrice)
			stopPrice = null;

		if (takePrice.HasValue && takePrice.Value >= candle.ClosePrice)
			takePrice = null;

		SellMarket(volume);

		_lastShortEntryPrice = candle.ClosePrice;
		_shortStopPrice = stopPrice;
		_shortTakePrice = takePrice;
	}

	private void CheckExitTargets(ICandleMessage candle)
	{
		if (_longVolume > 0)
		{
			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(_longVolume);
				return;
			}

			if (_longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value)
			{
				SellMarket(_longVolume);
				return;
			}
		}

		if (_shortVolume > 0)
		{
			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(_shortVolume);
				return;
			}

			if (_shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value)
			{
				BuyMarket(_shortVolume);
			}
		}
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (_trailingStopOffset <= 0m)
			return;

		if (_longVolume > 0)
		{
			var potentialStop = candle.ClosePrice - _trailingStopOffset;
			if (candle.ClosePrice - _lastLongEntryPrice > _trailingStopOffset + _trailingStepOffset)
			{
				if (!_longStopPrice.HasValue || potentialStop > _longStopPrice.Value + _trailingStepOffset)
					_longStopPrice = potentialStop;
			}
		}

		if (_shortVolume > 0)
		{
			var potentialStop = candle.ClosePrice + _trailingStopOffset;
			if (_lastShortEntryPrice - candle.ClosePrice > _trailingStopOffset + _trailingStepOffset)
			{
				if (!_shortStopPrice.HasValue || potentialStop < _shortStopPrice.Value - _trailingStepOffset)
					_shortStopPrice = potentialStop;
			}
		}
	}

	private bool CalculateBuySignal(decimal fastCurrent, decimal fastPrevious, decimal slowCurrent, decimal slowPrevious, decimal rsiFirstCurrent, decimal rsiFirstPrevious, decimal rsiSecondCurrent, decimal rsiSecondPrevious, decimal filterCurrent, decimal filterPrevious, decimal rsiFilterCurrent, decimal rsiFilterPrevious)
	{
		var trendAccepted = !UseTrendFilter || filterCurrent > filterPrevious;

		var teacherOne = UseRsiTeacher1
			&& rsiFirstPrevious < RsiSellLevel
			&& rsiFirstCurrent > rsiFirstPrevious
			&& rsiFilterCurrent < RsiBuyLevel
			&& fastCurrent > fastPrevious;

		var teacherTwo = UseRsiTeacher2
			&& rsiSecondPrevious < RsiSellLevel2
			&& rsiSecondCurrent > rsiSecondPrevious
			&& rsiFilterCurrent < RsiBuyLevel2
			&& slowCurrent > slowPrevious;

		return trendAccepted && (teacherOne || teacherTwo);
	}

	private bool CalculateSellSignal(decimal fastCurrent, decimal fastPrevious, decimal slowCurrent, decimal slowPrevious, decimal rsiFirstCurrent, decimal rsiFirstPrevious, decimal rsiSecondCurrent, decimal rsiSecondPrevious, decimal filterCurrent, decimal filterPrevious, decimal rsiFilterCurrent, decimal rsiFilterPrevious)
	{
		var trendAccepted = !UseTrendFilter || filterCurrent < filterPrevious;

		var teacherOne = UseRsiTeacher1
			&& rsiFirstPrevious > RsiBuyLevel
			&& rsiFirstCurrent < rsiFirstPrevious
			&& rsiFilterCurrent > RsiSellLevel
			&& fastCurrent < fastPrevious;

		var teacherTwo = UseRsiTeacher2
			&& rsiSecondPrevious > RsiBuyLevel2
			&& rsiSecondCurrent < rsiSecondPrevious
			&& rsiFilterCurrent > RsiSellLevel2
			&& slowCurrent < slowPrevious;

		return trendAccepted && (teacherOne || teacherTwo);
	}

	private bool IsTradingTime(DateTimeOffset time)
	{
		if (!TradeOnFriday && time.DayOfWeek == DayOfWeek.Friday)
			return false;

		if (StartHour > 0 || EndHour > 0)
		{
			if (StartHour > 0 && time.Hour < StartHour)
				return false;

			if (EndHour > 0 && time.Hour > EndHour)
				return false;
		}

		return true;
	}

	private void UpdateQueue(Queue<decimal> queue, decimal value)
	{
		queue.Enqueue(value);

		while (queue.Count > _signalBufferLength)
			queue.Dequeue();
	}

	private static MovingAverage CreateMovingAverage(MovingAverageTypes type, int length)
	{
		return type switch
		{
			MovingAverageTypes.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageTypes.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypes.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageTypes.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
			return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (trade.Order.Side == Sides.Buy)
		{
			if (_shortVolume > 0)
			{
				_shortVolume -= volume;
				if (_shortVolume <= 0)
				{
					_lastShortExitWasLoss = price > _lastShortEntryPrice;
					_shortVolume = 0;
					_shortEntries = 0;
					_shortStopPrice = null;
					_shortTakePrice = null;
				}
			}
			else
			{
				_longVolume += volume;
				_longEntries++;
				_lastLongEntryPrice = price;
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			if (_longVolume > 0)
			{
				_longVolume -= volume;
				if (_longVolume <= 0)
				{
					_lastLongExitWasLoss = price < _lastLongEntryPrice;
					_longVolume = 0;
					_longEntries = 0;
					_longStopPrice = null;
					_longTakePrice = null;
				}
			}
			else
			{
				_shortVolume += volume;
				_shortEntries++;
				_lastShortEntryPrice = price;
			}
		}
	}

	public enum MovingAverageTypes
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted
	}
}