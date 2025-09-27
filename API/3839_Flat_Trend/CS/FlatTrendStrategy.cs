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
/// Flat trend inspired strategy that analyses multiple trend filters, ADX and volatility to trade breakouts from ranging phases.
/// </summary>
public class FlatTrendStrategy : Strategy
{
	private enum TrendState
	{
		Neutral,
		Bull,
		StrongBull,
		Bear,
		StrongBear,
	}

	private readonly StrategyParam<int> _triggerLength;
	private readonly StrategyParam<int> _filterLength1;
	private readonly StrategyParam<int> _filterLength2;
	private readonly StrategyParam<bool> _useOnlyPrimaryIndicators;
	private readonly StrategyParam<bool> _ignoreModerateForEntry;
	private readonly StrategyParam<bool> _ignoreModerateForExit;
	private readonly StrategyParam<bool> _useTradingHours;
	private readonly StrategyParam<int> _tradingHourBegin;
	private readonly StrategyParam<int> _tradingHourEnd;
	private readonly StrategyParam<bool> _useJuiceFilter;
	private readonly StrategyParam<int> _juicePeriod;
	private readonly StrategyParam<decimal> _juiceThreshold;
	private readonly StrategyParam<bool> _useAdxFilter;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<bool> _useDirectionalFilter;
	private readonly StrategyParam<bool> _useAdrForStop;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingDivisor;
	private readonly StrategyParam<decimal> _breakEvenPips;
	private readonly StrategyParam<decimal> _breakEvenLockPips;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _triggerEma = null!;
	private ExponentialMovingAverage _filterEma1 = null!;
	private ExponentialMovingAverage _filterEma2 = null!;
	private StandardDeviation _juiceStdDev = null!;
	private AverageTrueRange _atr = null!;
	private AverageDirectionalIndex _adx = null!;

	private decimal? _prevTrigger;
	private decimal? _prevFilter1;
	private decimal? _prevFilter2;

	private decimal _lastJuice;
	private decimal _lastAtr;
	private decimal _lastAdx;
	private decimal _lastPlusDi;
	private decimal _lastMinusDi;
	private bool _isAdxReady;

	private decimal _entryPrice;
	private decimal _currentStopPrice;
	private decimal _highestPriceSinceEntry;
	private decimal _lowestPriceSinceEntry;
	private bool _isLongPosition;

	/// <summary>
	/// Fast EMA length that defines the trigger trend state.
	/// </summary>
	public int TriggerLength
	{
		get => _triggerLength.Value;
		set => _triggerLength.Value = value;
	}

	/// <summary>
	/// First filter EMA length.
	/// </summary>
	public int FilterLength1
	{
		get => _filterLength1.Value;
		set => _filterLength1.Value = value;
	}

	/// <summary>
	/// Second filter EMA length.
	/// </summary>
	public int FilterLength2
	{
		get => _filterLength2.Value;
		set => _filterLength2.Value = value;
	}

	/// <summary>
	/// Use only trigger and first filter alignment.
	/// </summary>
	public bool UseOnlyPrimaryIndicators
	{
		get => _useOnlyPrimaryIndicators.Value;
		set => _useOnlyPrimaryIndicators.Value = value;
	}

	/// <summary>
	/// Require only the strongest bullish or bearish signals for entries.
	/// </summary>
	public bool IgnoreModerateForEntry
	{
		get => _ignoreModerateForEntry.Value;
		set => _ignoreModerateForEntry.Value = value;
	}

	/// <summary>
	/// Require only the strongest opposite signals for exits.
	/// </summary>
	public bool IgnoreModerateForExit
	{
		get => _ignoreModerateForExit.Value;
		set => _ignoreModerateForExit.Value = value;
	}

	/// <summary>
	/// Activate trading schedule filtering.
	/// </summary>
	public bool UseTradingHours
	{
		get => _useTradingHours.Value;
		set => _useTradingHours.Value = value;
	}

	/// <summary>
	/// Trading start hour in exchange time.
	/// </summary>
	public int TradingHourBegin
	{
		get => _tradingHourBegin.Value;
		set => _tradingHourBegin.Value = value;
	}

	/// <summary>
	/// Trading end hour in exchange time.
	/// </summary>
	public int TradingHourEnd
	{
		get => _tradingHourEnd.Value;
		set => _tradingHourEnd.Value = value;
	}

	/// <summary>
	/// Enable volatility expansion filter (Juice equivalent).
	/// </summary>
	public bool UseJuiceFilter
	{
		get => _useJuiceFilter.Value;
		set => _useJuiceFilter.Value = value;
	}

	/// <summary>
	/// Standard deviation lookback for the Juice filter.
	/// </summary>
	public int JuicePeriod
	{
		get => _juicePeriod.Value;
		set => _juicePeriod.Value = value;
	}

	/// <summary>
	/// Minimum standard deviation threshold that represents momentum expansion.
	/// </summary>
	public decimal JuiceThreshold
	{
		get => _juiceThreshold.Value;
		set => _juiceThreshold.Value = value;
	}

	/// <summary>
	/// Enable ADX based trend strength filter.
	/// </summary>
	public bool UseAdxFilter
	{
		get => _useAdxFilter.Value;
		set => _useAdxFilter.Value = value;
	}

	/// <summary>
	/// ADX calculation period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Minimal ADX value that confirms trend strength.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Require directional movement confirmation from +DI and -DI lines.
	/// </summary>
	public bool UseDirectionalFilter
	{
		get => _useDirectionalFilter.Value;
		set => _useDirectionalFilter.Value = value;
	}

	/// <summary>
	/// Use ATR based distance instead of static stop loss.
	/// </summary>
	public bool UseAdrForStop
	{
		get => _useAdrForStop.Value;
		set => _useAdrForStop.Value = value;
	}

	/// <summary>
	/// Static stop loss in pips when ATR mode is disabled.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR for trailing stop adjustment.
	/// </summary>
	public decimal TrailingDivisor
	{
		get => _trailingDivisor.Value;
		set => _trailingDivisor.Value = value;
	}

	/// <summary>
	/// Distance in pips required before moving the stop loss to break even.
	/// </summary>
	public decimal BreakEvenPips
	{
		get => _breakEvenPips.Value;
		set => _breakEvenPips.Value = value;
	}

	/// <summary>
	/// Profit lock in pips once break even is reached.
	/// </summary>
	public decimal BreakEvenLockPips
	{
		get => _breakEvenLockPips.Value;
		set => _breakEvenLockPips.Value = value;
	}

	/// <summary>
	/// ATR period used as dynamic volatility estimator.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FlatTrendStrategy()
	{
		_triggerLength = Param(nameof(TriggerLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Trigger Length", "EMA length for the trigger", "Trend");

		_filterLength1 = Param(nameof(FilterLength1), 15)
		.SetGreaterThanZero()
		.SetDisplay("Filter 1 Length", "EMA length for the first filter", "Trend");

		_filterLength2 = Param(nameof(FilterLength2), 60)
		.SetGreaterThanZero()
		.SetDisplay("Filter 2 Length", "EMA length for the second filter", "Trend");

		_useOnlyPrimaryIndicators = Param(nameof(UseOnlyPrimaryIndicators), false)
		.SetDisplay("Use Primary Filters", "Only check trigger and filter one", "Trend");

		_ignoreModerateForEntry = Param(nameof(IgnoreModerateForEntry), false)
		.SetDisplay("Strict Entry", "Allow only strongest bullish/bearish signals", "Trend");

		_ignoreModerateForExit = Param(nameof(IgnoreModerateForExit), true)
		.SetDisplay("Strict Exit", "Allow only strongest counter signals to exit", "Trend");

		_useTradingHours = Param(nameof(UseTradingHours), false)
		.SetDisplay("Use Trading Hours", "Enable trading schedule filter", "General");

		_tradingHourBegin = Param(nameof(TradingHourBegin), 0)
		.SetDisplay("Start Hour", "Trading window start hour", "General");

		_tradingHourEnd = Param(nameof(TradingHourEnd), 24)
		.SetDisplay("End Hour", "Trading window end hour", "General");

		_useJuiceFilter = Param(nameof(UseJuiceFilter), true)
		.SetDisplay("Use Juice", "Enable standard deviation breakout filter", "Volatility");

		_juicePeriod = Param(nameof(JuicePeriod), 7)
		.SetGreaterThanZero()
		.SetDisplay("Juice Period", "Standard deviation period", "Volatility");

		_juiceThreshold = Param(nameof(JuiceThreshold), 0.0004m)
		.SetGreaterThanZero()
		.SetDisplay("Juice Threshold", "Minimal standard deviation required", "Volatility");

		_useAdxFilter = Param(nameof(UseAdxFilter), true)
		.SetDisplay("Use ADX", "Enable ADX confirmation", "Trend Strength");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ADX Period", "Average Directional Index period", "Trend Strength");

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
		.SetGreaterThanZero()
		.SetDisplay("ADX Threshold", "Minimal ADX to accept trades", "Trend Strength");

		_useDirectionalFilter = Param(nameof(UseDirectionalFilter), true)
		.SetDisplay("Use DI", "Require +DI/-DI confirmation", "Trend Strength");

		_useAdrForStop = Param(nameof(UseAdrForStop), true)
		.SetDisplay("Use ATR Stop", "Derive stop distance from ATR", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 40m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss Pips", "Static stop distance in pips", "Risk");

		_trailingDivisor = Param(nameof(TrailingDivisor), 0.40m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Divisor", "ATR multiplier for trailing", "Risk");

		_breakEvenPips = Param(nameof(BreakEvenPips), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Break Even Pips", "Profit distance to secure break even", "Risk");

		_breakEvenLockPips = Param(nameof(BreakEvenLockPips), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Break Even Lock", "Profit locked after break even", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR period used for volatility", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle timeframe", "General");

		Volume = 1m;
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

		_prevTrigger = null;
		_prevFilter1 = null;
		_prevFilter2 = null;
		_lastJuice = 0m;
		_lastAtr = 0m;
		_lastAdx = 0m;
		_lastPlusDi = 0m;
		_lastMinusDi = 0m;
		_isAdxReady = false;

		_entryPrice = 0m;
		_currentStopPrice = 0m;
		_highestPriceSinceEntry = 0m;
		_lowestPriceSinceEntry = 0m;
		_isLongPosition = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_triggerEma = new ExponentialMovingAverage { Length = TriggerLength };
		_filterEma1 = new ExponentialMovingAverage { Length = FilterLength1 };
		_filterEma2 = new ExponentialMovingAverage { Length = FilterLength2 };
		_juiceStdDev = new StandardDeviation { Length = JuicePeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(_triggerEma, _filterEma1, _filterEma2, _juiceStdDev, _atr, ProcessTrendData)
		.BindEx(_adx, ProcessAdx)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _triggerEma);
			DrawIndicator(area, _filterEma1);
			DrawIndicator(area, _filterEma2);
			DrawOwnTrades(area);

			var secondary = CreateChartArea();
			if (secondary != null)
			{
				DrawIndicator(secondary, _juiceStdDev);
				DrawIndicator(secondary, _atr);
			}

			var adxArea = CreateChartArea();
			if (adxArea != null)
			{
				DrawIndicator(adxArea, _adx);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position > 0m)
		{
			if (!_isLongPosition || _entryPrice == 0m)
			{
				_isLongPosition = true;
				_entryPrice = trade.Trade.Price;
				_highestPriceSinceEntry = trade.Trade.Price;
				_lowestPriceSinceEntry = trade.Trade.Price;
				_currentStopPrice = 0m;
			}
			else
			{
				_highestPriceSinceEntry = Math.Max(_highestPriceSinceEntry, trade.Trade.Price);
			}
		}
		else if (Position < 0m)
		{
			if (_isLongPosition || _entryPrice == 0m)
			{
				_isLongPosition = false;
				_entryPrice = trade.Trade.Price;
				_highestPriceSinceEntry = trade.Trade.Price;
				_lowestPriceSinceEntry = trade.Trade.Price;
				_currentStopPrice = 0m;
			}
			else
			{
				_lowestPriceSinceEntry = Math.Min(_lowestPriceSinceEntry, trade.Trade.Price);
			}
		}
		else
		{
			_entryPrice = 0m;
			_currentStopPrice = 0m;
			_highestPriceSinceEntry = 0m;
			_lowestPriceSinceEntry = 0m;
		}
	}

	private void ProcessAdx(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var typed = (AverageDirectionalIndexValue)adxValue;
		if (typed.MovingAverage is not decimal adx)
		return;

		var plus = typed.Dx.Plus;
		var minus = typed.Dx.Minus;

		if (plus is null || minus is null)
		return;

		_lastAdx = adx;
		_lastPlusDi = plus.Value;
		_lastMinusDi = minus.Value;
		_isAdxReady = true;
	}

	private void ProcessTrendData(ICandleMessage candle, decimal triggerValue, decimal filterValue1, decimal filterValue2, decimal juiceValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_lastJuice = juiceValue;
		_lastAtr = atrValue;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (UseTradingHours && !IsWithinTradingHours(candle.OpenTime))
		return;

		if (UseJuiceFilter && _lastJuice < JuiceThreshold)
		return;

		if (UseAdxFilter && (!_isAdxReady || _lastAdx < AdxThreshold))
		return;

		var triggerState = ResolveState(candle.ClosePrice, triggerValue, _prevTrigger);
		var filterState1 = ResolveState(candle.ClosePrice, filterValue1, _prevFilter1);
		var filterState2 = ResolveState(candle.ClosePrice, filterValue2, _prevFilter2);

		_prevTrigger = triggerValue;
		_prevFilter1 = filterValue1;
		_prevFilter2 = filterValue2;

		var allowLong = CanEnterLong(triggerState, filterState1, filterState2);
		var allowShort = CanEnterShort(triggerState, filterState1, filterState2);

		if (UseAdxFilter && UseDirectionalFilter)
		{
			allowLong &= _lastPlusDi >= _lastMinusDi;
			allowShort &= _lastMinusDi >= _lastPlusDi;
		}

		if (Position <= 0m && allowLong)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Position >= 0m && allowShort)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		UpdateRiskManagement(candle, triggerState);
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var hour = time.TimeOfDay.Hours;
		if (TradingHourBegin <= TradingHourEnd)
		return hour >= TradingHourBegin && hour < TradingHourEnd;

		return hour >= TradingHourBegin || hour < TradingHourEnd;
	}

	private TrendState ResolveState(decimal price, decimal ema, decimal? prevEma)
	{
		if (prevEma is null)
		return TrendState.Neutral;

		var slope = ema - prevEma.Value;

		if (price > ema && slope > 0m)
		return TrendState.StrongBull;

		if (price > ema)
		return TrendState.Bull;

		if (price < ema && slope < 0m)
		return TrendState.StrongBear;

		if (price < ema)
		return TrendState.Bear;

		return TrendState.Neutral;
	}

	private bool CanEnterLong(TrendState triggerState, TrendState filterState1, TrendState filterState2)
	{
		var triggerOk = triggerState == TrendState.StrongBull || (!IgnoreModerateForEntry && triggerState == TrendState.Bull);
		var filter1Ok = filterState1 == TrendState.StrongBull || (!IgnoreModerateForEntry && filterState1 == TrendState.Bull);
		var filter2Ok = UseOnlyPrimaryIndicators || filterState2 == TrendState.StrongBull || (!IgnoreModerateForEntry && filterState2 == TrendState.Bull);

		return triggerOk && filter1Ok && filter2Ok;
	}

	private bool CanEnterShort(TrendState triggerState, TrendState filterState1, TrendState filterState2)
	{
		var triggerOk = triggerState == TrendState.StrongBear || (!IgnoreModerateForEntry && triggerState == TrendState.Bear);
		var filter1Ok = filterState1 == TrendState.StrongBear || (!IgnoreModerateForEntry && filterState1 == TrendState.Bear);
		var filter2Ok = UseOnlyPrimaryIndicators || filterState2 == TrendState.StrongBear || (!IgnoreModerateForEntry && filterState2 == TrendState.Bear);

		return triggerOk && filter1Ok && filter2Ok;
	}

	private bool ShouldExitLong(TrendState triggerState)
	{
		return IgnoreModerateForExit ? triggerState == TrendState.StrongBear : triggerState == TrendState.Bear || triggerState == TrendState.StrongBear;
	}

	private bool ShouldExitShort(TrendState triggerState)
	{
		return IgnoreModerateForExit ? triggerState == TrendState.StrongBull : triggerState == TrendState.Bull || triggerState == TrendState.StrongBull;
	}

	private void UpdateRiskManagement(ICandleMessage candle, TrendState triggerState)
	{
		var step = Security?.PriceStep ?? 0.0001m;
		if (step <= 0m)
		step = 0.0001m;

		if (Position > 0m)
		{
			_highestPriceSinceEntry = Math.Max(_highestPriceSinceEntry, candle.HighPrice);
			_lowestPriceSinceEntry = Math.Min(_lowestPriceSinceEntry, candle.LowPrice);

			if (_currentStopPrice == 0m)
			{
				var stopDistance = UseAdrForStop ? Math.Max(step, _lastAtr) : StopLossPips * step;
				_currentStopPrice = Math.Max(0m, _entryPrice - stopDistance);
			}

			ApplyDynamicStopsForLong(step);

			if (_currentStopPrice > 0m && candle.LowPrice <= _currentStopPrice)
			{
				ClosePosition();
				return;
			}

			if (ShouldExitLong(triggerState))
			{
				ClosePosition();
			}
		}
		else if (Position < 0m)
		{
			_lowestPriceSinceEntry = Math.Min(_lowestPriceSinceEntry, candle.LowPrice);
			_highestPriceSinceEntry = Math.Max(_highestPriceSinceEntry, candle.HighPrice);

			if (_currentStopPrice == 0m)
			{
				var stopDistance = UseAdrForStop ? Math.Max(step, _lastAtr) : StopLossPips * step;
				_currentStopPrice = _entryPrice + stopDistance;
			}

			ApplyDynamicStopsForShort(step);

			if (_currentStopPrice > 0m && candle.HighPrice >= _currentStopPrice)
			{
				ClosePosition();
				return;
			}

			if (ShouldExitShort(triggerState))
			{
				ClosePosition();
			}
		}
	}

	private void ApplyDynamicStopsForLong(decimal step)
	{
		var trailingDistance = _lastAtr * TrailingDivisor;
		if (trailingDistance > 0m)
		{
			var trailingStop = _highestPriceSinceEntry - trailingDistance;
			if (trailingStop > _currentStopPrice)
			_currentStopPrice = trailingStop;
		}

		if (BreakEvenPips > 0m)
		{
			var beDistance = BreakEvenPips * step;
			if (_highestPriceSinceEntry - _entryPrice >= beDistance)
			{
				var beStop = _entryPrice + BreakEvenLockPips * step;
				if (beStop > _currentStopPrice)
				_currentStopPrice = beStop;
			}
		}
	}

	private void ApplyDynamicStopsForShort(decimal step)
	{
		var trailingDistance = _lastAtr * TrailingDivisor;
		if (trailingDistance > 0m)
		{
			var trailingStop = _lowestPriceSinceEntry + trailingDistance;
			if (_currentStopPrice == 0m || trailingStop < _currentStopPrice)
			_currentStopPrice = trailingStop;
		}

		if (BreakEvenPips > 0m)
		{
			var beDistance = BreakEvenPips * step;
			if (_entryPrice - _lowestPriceSinceEntry >= beDistance)
			{
				var beStop = _entryPrice - BreakEvenLockPips * step;
				if (_currentStopPrice == 0m || beStop < _currentStopPrice)
				_currentStopPrice = beStop;
			}
		}
	}
}

