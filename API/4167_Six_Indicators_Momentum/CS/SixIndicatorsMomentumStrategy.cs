using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the "6xIndics" MetaTrader expert advisor.
/// Combines Accelerator Oscillator, Awesome Oscillator momentum and a stochastic filter to open a single position.
/// Includes optional martingale position sizing, stop-loss/take-profit and trailing stop logic.
/// </summary>
public class SixIndicatorsMomentumStrategy : Strategy
{
	private const int HistoryLength = 256;

	private readonly StrategyParam<bool> _allowBuy;
	private readonly StrategyParam<bool> _allowSell;
	private readonly StrategyParam<bool> _closeOnReverseSignal;
	private readonly StrategyParam<int> _firstSource;
	private readonly StrategyParam<int> _secondSource;
	private readonly StrategyParam<int> _thirdSource;
	private readonly StrategyParam<int> _fourthSource;
	private readonly StrategyParam<int> _fifthSource;
	private readonly StrategyParam<int> _sixthSource;
	private readonly StrategyParam<int> _aoShift;
	private readonly StrategyParam<int> _acPrimaryShift;
	private readonly StrategyParam<int> _acSecondaryShift;
	private readonly StrategyParam<decimal> _sensitivityMultiplier;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _requireProfitForTrailing;
	private readonly StrategyParam<decimal> _lockProfitPips;
	private readonly StrategyParam<bool> _useMartingale;
	private readonly StrategyParam<DataType> _candleType;

private readonly SimpleMovingAverage _acAverage = new() { Length = 5 };

	private readonly decimal[] _acHistory = new decimal[HistoryLength];
	private readonly decimal[] _aoHistory = new decimal[HistoryLength];
	private int _acCount;
	private int _aoCount;

	private decimal? _previousStochastic;
	private decimal? _entryPrice;
	private Sides? _entrySide;
	private decimal _currentVolume;
	private decimal _nextVolume;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

	private decimal _priceStep;
	private decimal _pipMultiplier;

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool AllowBuy
	{
		get => _allowBuy.Value;
		set => _allowBuy.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool AllowSell
	{
		get => _allowSell.Value;
		set => _allowSell.Value = value;
	}

	/// <summary>
	/// Close the current trade on an opposite signal while it is in profit.
	/// </summary>
	public bool CloseOnReverseSignal
	{
		get => _closeOnReverseSignal.Value;
		set => _closeOnReverseSignal.Value = value;
	}

	/// <summary>
	/// Source slot A (0-5) used in the composite condition.
	/// </summary>
	public int FirstSourceIndex
	{
		get => _firstSource.Value;
		set => _firstSource.Value = value;
	}

	/// <summary>
	/// Source slot B (0-5) used in the composite condition.
	/// </summary>
	public int SecondSourceIndex
	{
		get => _secondSource.Value;
		set => _secondSource.Value = value;
	}

	/// <summary>
	/// Source slot C (0-5) used in the composite condition.
	/// </summary>
	public int ThirdSourceIndex
	{
		get => _thirdSource.Value;
		set => _thirdSource.Value = value;
	}

	/// <summary>
	/// Source slot D (0-5) used in the composite condition.
	/// </summary>
	public int FourthSourceIndex
	{
		get => _fourthSource.Value;
		set => _fourthSource.Value = value;
	}

	/// <summary>
	/// Source slot E (0-5) used in the composite condition.
	/// </summary>
	public int FifthSourceIndex
	{
		get => _fifthSource.Value;
		set => _fifthSource.Value = value;
	}

	/// <summary>
	/// Source slot F (0-5) used in the composite condition.
	/// </summary>
	public int SixthSourceIndex
	{
		get => _sixthSource.Value;
		set => _sixthSource.Value = value;
	}

	/// <summary>
	/// Shift in bars used for the Awesome Oscillator momentum comparison.
	/// </summary>
	public int AoMomentumShift
	{
		get => _aoShift.Value;
		set => _aoShift.Value = value;
	}

	/// <summary>
	/// Shift in bars used for the first Accelerator difference.
	/// </summary>
	public int AcPrimaryShift
	{
		get => _acPrimaryShift.Value;
		set => _acPrimaryShift.Value = value;
	}

	/// <summary>
	/// Shift in bars used for the second Accelerator difference.
	/// </summary>
	public int AcSecondaryShift
	{
		get => _acSecondaryShift.Value;
		set => _acSecondaryShift.Value = value;
	}

	/// <summary>
	/// Sensitivity multiplier applied to the small and large thresholds.
	/// </summary>
	public decimal SensitivityMultiplier
	{
		get => _sensitivityMultiplier.Value;
		set => _sensitivityMultiplier.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Enable trailing stop logic.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Require price to exceed the lock-in distance before trailing activates.
	/// </summary>
	public bool RequireProfitForTrailing
	{
		get => _requireProfitForTrailing.Value;
		set => _requireProfitForTrailing.Value = value;
	}

	/// <summary>
	/// Additional profit (in pips) that must be locked before the trailing stop starts to move.
	/// </summary>
	public decimal LockProfitPips
	{
		get => _lockProfitPips.Value;
		set => _lockProfitPips.Value = value;
	}


	/// <summary>
	/// Enable martingale lot sizing.
	/// </summary>
	public bool UseMartingale
	{
		get => _useMartingale.Value;
		set => _useMartingale.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SixIndicatorsMomentumStrategy"/> class.
	/// </summary>
	public SixIndicatorsMomentumStrategy()
	{
		_allowBuy = Param(nameof(AllowBuy), true)
		.SetDisplay("Allow Buy", "Enable long entries", "Trading Rules");

		_allowSell = Param(nameof(AllowSell), true)
		.SetDisplay("Allow Sell", "Enable short entries", "Trading Rules");

		_closeOnReverseSignal = Param(nameof(CloseOnReverseSignal), false)
		.SetDisplay("Close On Reverse", "Close profitable trades when the signal flips", "Trading Rules");

		_firstSource = Param(nameof(FirstSourceIndex), 1)
		.SetNotNegative()
		.SetDisplay("Source A", "Slot A selection (0=AC[1], 1=AC[10], 2=AC[20], 3=AO momentum, 4=AC diff #1, 5=AC diff #2)", "Signal Mixer");

		_secondSource = Param(nameof(SecondSourceIndex), 2)
		.SetNotNegative()
		.SetDisplay("Source B", "Slot B selection", "Signal Mixer");

		_thirdSource = Param(nameof(ThirdSourceIndex), 3)
		.SetNotNegative()
		.SetDisplay("Source C", "Slot C selection", "Signal Mixer");

		_fourthSource = Param(nameof(FourthSourceIndex), 4)
		.SetNotNegative()
		.SetDisplay("Source D", "Slot D selection", "Signal Mixer");

		_fifthSource = Param(nameof(FifthSourceIndex), 3)
		.SetNotNegative()
		.SetDisplay("Source E", "Slot E selection", "Signal Mixer");

		_sixthSource = Param(nameof(SixthSourceIndex), 4)
		.SetNotNegative()
		.SetDisplay("Source F", "Slot F selection", "Signal Mixer");

		_aoShift = Param(nameof(AoMomentumShift), 10)
		.SetNotNegative()
		.SetDisplay("AO Shift", "Bars between current AO and comparison value", "Signal Mixer");

		_acPrimaryShift = Param(nameof(AcPrimaryShift), 10)
		.SetNotNegative()
		.SetDisplay("AC Shift #1", "Bars between current AC and the first comparison", "Signal Mixer");

		_acSecondaryShift = Param(nameof(AcSecondaryShift), 10)
		.SetNotNegative()
		.SetDisplay("AC Shift #2", "Bars between current AC and the second comparison", "Signal Mixer");

		_sensitivityMultiplier = Param(nameof(SensitivityMultiplier), 1m)
		.SetGreaterOrEqual(0.1m)
		.SetDisplay("Sensitivity", "Threshold multiplier applied to AC checks", "Signal Mixer");

		_takeProfitPips = Param(nameof(TakeProfitPips), 300m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk Management");

		_stopLossPips = Param(nameof(StopLossPips), 300m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk Management");

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
		.SetDisplay("Use Trailing", "Enable trailing stop logic", "Risk Management");

		_trailingStopPips = Param(nameof(TrailingStopPips), 300m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk Management");

		_requireProfitForTrailing = Param(nameof(RequireProfitForTrailing), false)
		.SetDisplay("Use Profit Lock", "Require price to move beyond lock distance before trailing", "Risk Management");

		_lockProfitPips = Param(nameof(LockProfitPips), 300m)
		.SetNotNegative()
		.SetDisplay("Lock Profit (pips)", "Additional profit before trailing activates", "Risk Management");


		_useMartingale = Param(nameof(UseMartingale), false)
		.SetDisplay("Use Martingale", "Multiply volume after losing trades", "Trading Rules");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series", "General");
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

		Array.Clear(_acHistory);
		Array.Clear(_aoHistory);
		_acCount = 0;
		_aoCount = 0;
		_previousStochastic = null;
		_entryPrice = null;
		_entrySide = null;
		_currentVolume = 0m;
		_stopPrice = null;
		_takeProfitPrice = null;
		_nextVolume = NormalizeVolume(Volume);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var security = Security;
		if (security != null)
		{
			_priceStep = security.PriceStep ?? 0.0001m;
			var decimals = security.Decimals ?? 4;
			_pipMultiplier = decimals >= 3 ? 10m : 1m;
		}
		else
		{
			_priceStep = 0.0001m;
			_pipMultiplier = 10m;
		}

		_nextVolume = NormalizeVolume(Volume);

		var awesome = new AwesomeOscillator();
		var stochastic = new StochasticOscillator
		{
			Length = 5,
		K = { Length = 5 },
		D = { Length = 5 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(awesome, stochastic, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, awesome);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue aoValue, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!aoValue.IsFinal || !stochasticValue.IsFinal)
		return;

		var ao = aoValue.ToDecimal();
		var acAverageValue = _acAverage.Process(ao);
		if (!acAverageValue.IsFinal)
		{
			UpdateStochasticHistory(stochasticValue);
			return;
		}

		var ac = ao - acAverageValue.ToDecimal();
		UpdateHistories(ao, ac);

		var previousStochastic = ExtractPreviousStochastic(stochasticValue);
		if (previousStochastic is null)
		return;

		if (!TryEvaluateSignal(previousStochastic.Value, out var signal))
		return;

		if (HandleExistingPosition(candle, signal))
		return;

		HandleEntries(candle, signal);
	}

	private void UpdateHistories(decimal ao, decimal ac)
	{
		ShiftHistory(_aoHistory, ref _aoCount, ao);
		ShiftHistory(_acHistory, ref _acCount, ac);
	}

	private static void ShiftHistory(decimal[] history, ref int count, decimal value)
	{
		for (var i = history.Length - 1; i > 0; i--)
		history[i] = history[i - 1];

		history[0] = value;
		if (count < history.Length)
		count++;
	}

	private void UpdateStochasticHistory(IIndicatorValue stochasticValue)
	{
		if (stochasticValue is not StochasticOscillatorValue stochastic || stochastic.K is not decimal currentK)
		return;

		_previousStochastic = currentK;
	}

	private decimal? ExtractPreviousStochastic(IIndicatorValue stochasticValue)
	{
		if (stochasticValue is not StochasticOscillatorValue stochastic || stochastic.K is not decimal currentK)
		return null;

		var previous = _previousStochastic;
		_previousStochastic = currentK;
		return previous;
	}

	private bool TryEvaluateSignal(decimal previousStochastic, out int signal)
	{
		signal = 0;

		if (!TryGetHistoryValue(_acHistory, _acCount, 1, out var acShift1) ||
		!TryGetHistoryValue(_acHistory, _acCount, 10, out var acShift10) ||
		!TryGetHistoryValue(_acHistory, _acCount, 20, out var acShift20))
		return false;

		if (!TryGetHistoryValue(_aoHistory, _aoCount, ClampShift(AoMomentumShift), out var aoPast))
		return false;

		if (!TryGetHistoryValue(_acHistory, _acCount, ClampShift(AcPrimaryShift), out var acPast1))
		return false;

		if (!TryGetHistoryValue(_acHistory, _acCount, ClampShift(AcSecondaryShift), out var acPast2))
		return false;

		var aoCurrent = _aoHistory[0];
		var acCurrent = _acHistory[0];

		var sources = new decimal[6];
		sources[0] = acShift1;
		sources[1] = acShift10;
		sources[2] = acShift20;
		sources[3] = aoCurrent - aoPast;
		sources[4] = acCurrent - acPast1;
		sources[5] = acCurrent - acPast2;

		var a = sources[ClampSource(FirstSourceIndex)];
		var b = sources[ClampSource(SecondSourceIndex)];
		var c = sources[ClampSource(ThirdSourceIndex)];
		var d = sources[ClampSource(FourthSourceIndex)];
		var e = sources[ClampSource(FifthSourceIndex)];
		var f = sources[ClampSource(SixthSourceIndex)];

		var small = 0.0001m * SensitivityMultiplier;
		var large = 0.0002m * SensitivityMultiplier;

		var buySignal = a > 0m && b > small && c > large && d < 0m && e < small && f < large && previousStochastic < 15m;
		var sellSignal = a < 0m && b < small && c < large && d > 0m && e > small && f > large && previousStochastic > 85m;

		signal = buySignal ? 1 : sellSignal ? -1 : 0;
		return true;
	}

	private static bool TryGetHistoryValue(decimal[] history, int count, int shift, out decimal value)
	{
		value = 0m;
		if (shift < 0)
		return false;

		if (shift >= history.Length)
		return false;

		if (shift >= count)
		return false;

		value = history[shift];
		return true;
	}

	private static int ClampSource(int index)
	{
		if (index < 0)
		return 0;
		if (index > 5)
		return 5;
		return index;
	}

	private static int ClampShift(int shift)
	{
		return shift < 0 ? 0 : shift;
	}

	private bool HandleExistingPosition(ICandleMessage candle, int signal)
	{
		if (Position > 0m)
		{
			_entrySide ??= Sides.Buy;
			_entryPrice ??= candle.ClosePrice;

			if (TryCloseLongPosition(candle, signal))
			return true;

			UpdateLongTrailing(candle);
			return true;
		}

		if (Position < 0m)
		{
			_entrySide ??= Sides.Sell;
			_entryPrice ??= candle.ClosePrice;

			if (TryCloseShortPosition(candle, signal))
			return true;

			UpdateShortTrailing(candle);
			return true;
		}

		return false;
	}

	private bool TryCloseLongPosition(ICandleMessage candle, int signal)
	{
		if (_entryPrice is not decimal entry)
		return false;

		decimal? exitPrice = null;

		if (CloseOnReverseSignal && signal < 0 && candle.ClosePrice > entry)
		exitPrice = candle.ClosePrice;

		if (exitPrice is null && _stopPrice is decimal stop && candle.LowPrice <= stop)
		exitPrice = stop;

		if (exitPrice is null && _takeProfitPrice is decimal take && candle.HighPrice >= take)
		exitPrice = take;

		if (exitPrice is null)
		return false;

		ClosePosition();
		UpdateMartingaleState(exitPrice.Value - entry);
		ResetTradeState();
		return true;
	}

	private bool TryCloseShortPosition(ICandleMessage candle, int signal)
	{
		if (_entryPrice is not decimal entry)
		return false;

		decimal? exitPrice = null;

		if (CloseOnReverseSignal && signal > 0 && candle.ClosePrice < entry)
		exitPrice = candle.ClosePrice;

		if (exitPrice is null && _stopPrice is decimal stop && candle.HighPrice >= stop)
		exitPrice = stop;

		if (exitPrice is null && _takeProfitPrice is decimal take && candle.LowPrice <= take)
		exitPrice = take;

		if (exitPrice is null)
		return false;

		ClosePosition();
		UpdateMartingaleState(entry - exitPrice.Value);
		ResetTradeState();
		return true;
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (!UseTrailingStop || TrailingStopPips <= 0m || _entryPrice is not decimal entry)
		return;

		var trailingDistance = GetPriceOffset(TrailingStopPips);
		if (trailingDistance <= 0m)
		return;

		var price = candle.ClosePrice;
		if (price <= entry)
		return;

		if (RequireProfitForTrailing)
		{
			var activation = entry + trailingDistance + GetPriceOffset(LockProfitPips);
			if (price <= activation)
			return;
		}

		var newStop = price - trailingDistance;
		if (_stopPrice is not decimal stop || newStop > stop + _priceStep / 2m)
		_stopPrice = newStop;
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (!UseTrailingStop || TrailingStopPips <= 0m || _entryPrice is not decimal entry)
		return;

		var trailingDistance = GetPriceOffset(TrailingStopPips);
		if (trailingDistance <= 0m)
		return;

		var price = candle.ClosePrice;
		if (price >= entry)
		return;

		if (RequireProfitForTrailing)
		{
			var activation = entry - trailingDistance - GetPriceOffset(LockProfitPips);
			if (price >= activation)
			return;
		}

		var newStop = price + trailingDistance;
		if (_stopPrice is not decimal stop || newStop < stop - _priceStep / 2m)
		_stopPrice = newStop;
	}

	private void HandleEntries(ICandleMessage candle, int signal)
	{
		if (signal == 0 || Position != 0m)
		return;

		var baseVolume = NormalizeVolume(Volume);
		var volume = UseMartingale ? _nextVolume : baseVolume;
		volume = NormalizeVolume(volume);

		if (volume <= 0m)
		return;

		if (signal > 0 && AllowBuy)
		{
			_entryPrice = candle.ClosePrice;
			_entrySide = Sides.Buy;
			_currentVolume = volume;
			SetupRiskLevels(Sides.Buy, _entryPrice.Value);
			BuyMarket(volume);
		}
		else if (signal < 0 && AllowSell)
		{
			_entryPrice = candle.ClosePrice;
			_entrySide = Sides.Sell;
			_currentVolume = volume;
			SetupRiskLevels(Sides.Sell, _entryPrice.Value);
			SellMarket(volume);
		}

		_nextVolume = UseMartingale ? volume : baseVolume;
	}

	private void SetupRiskLevels(Sides side, decimal entryPrice)
	{
		var stopDistance = GetPriceOffset(StopLossPips);
		var takeDistance = GetPriceOffset(TakeProfitPips);

		if (side == Sides.Buy)
		{
			_stopPrice = stopDistance > 0m ? entryPrice - stopDistance : null;
			_takeProfitPrice = takeDistance > 0m ? entryPrice + takeDistance : null;
		}
		else
		{
			_stopPrice = stopDistance > 0m ? entryPrice + stopDistance : null;
			_takeProfitPrice = takeDistance > 0m ? entryPrice - takeDistance : null;
		}
	}

	private void ResetTradeState()
	{
		_entryPrice = null;
		_entrySide = null;
		_currentVolume = 0m;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	private void UpdateMartingaleState(decimal profit)
	{
		var baseVolume = NormalizeVolume(Volume);

		if (!UseMartingale)
		{
			_nextVolume = baseVolume;
			return;
		}

		if (profit > 0m)
		{
			_nextVolume = baseVolume;
			return;
		}

		var multiplier = CalculateMartingaleMultiplier();
		if (multiplier < 1m)
		multiplier = 1m;

		var referenceVolume = _currentVolume > 0m ? _currentVolume : baseVolume;
		var candidate = referenceVolume * multiplier;
		var normalized = NormalizeVolume(candidate);
		_nextVolume = normalized > 0m ? normalized : baseVolume;
	}

	private decimal CalculateMartingaleMultiplier()
	{
		var take = TakeProfitPips;
		if (take <= 0m)
		return 1m;

		return (take + StopLossPips) / take;
	}

	private decimal GetPriceOffset(decimal pips)
	{
		if (pips <= 0m)
		return 0m;

		var step = _priceStep > 0m ? _priceStep : 0.0001m;
		var multiplier = _pipMultiplier > 0m ? _pipMultiplier : 1m;
		return pips * step * multiplier;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		var minVolume = security.MinVolume ?? 0m;
		var maxVolume = security.MaxVolume ?? decimal.MaxValue;

		if (volume < minVolume)
		volume = minVolume;
		if (volume > maxVolume)
		volume = maxVolume;

		return volume > 0m ? volume : 0m;
	}
}
