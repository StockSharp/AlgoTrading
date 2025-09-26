namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Port of the universal iCustom expert advisor.
/// The strategy reads values from a user-defined indicator and reacts to buffer crossings.
/// </summary>
public class ExpICustomStrategy : Strategy
{
	private enum SignalMode
	{
		Arrows = 1,
		Cross = 2,
		Levels = 3,
		Slope = 4,
		RangeBreak = 5,
	}

	private enum ExecutionMode
	{
		Market = 0,
	}

	private sealed class IndicatorState
	{
		private readonly List<decimal?[]> _history = new();
		private readonly IIndicator _indicator;

		public IndicatorState(IIndicator indicator)
		{
			_indicator = indicator;
		}

		public IIndicator Indicator => _indicator;

		public void Reset()
		{
			_history.Clear();
			_indicator?.Reset();
		}

		public void Process(ICandleMessage candle, Func<ICandleMessage, decimal> priceSelector)
		{
			if (_indicator == null)
			return;

			var price = priceSelector(candle);
			var value = _indicator.Process(new CandleIndicatorValue(candle, price));

			if (!value.IsFinal)
			return;

			var buffers = ExtractBufferValues(value);
			if (buffers.Length == 0)
			return;

			_history.Add(buffers);
		}

		public decimal? GetValue(int bufferIndex, int shift)
		{
			if (bufferIndex < 0 || shift < 0)
			return null;

			var index = _history.Count - 1 - shift;
			if (index < 0 || index >= _history.Count)
			return null;

			var values = _history[index];
			if (bufferIndex >= values.Length)
			return null;

			return values[bufferIndex];
		}

		public int Count => _history.Count;
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _entryIndicatorName;
	private readonly StrategyParam<string> _entryIndicatorParameters;
	private readonly StrategyParam<int> _entryShift;
	private readonly StrategyParam<int> _entryBuyBufferIndex;
	private readonly StrategyParam<int> _entrySellBufferIndex;
	private readonly StrategyParam<int> _entryMainBufferIndex;
	private readonly StrategyParam<int> _entrySignalBufferIndex;
	private readonly StrategyParam<decimal> _entryBuyLevel;
	private readonly StrategyParam<decimal> _entrySellLevel;
	private readonly StrategyParam<SignalMode> _entryMode;

	private readonly StrategyParam<string> _closeIndicatorName;
	private readonly StrategyParam<string> _closeIndicatorParameters;
	private readonly StrategyParam<bool> _closeUseOpenIndicator;
	private readonly StrategyParam<int> _closeShift;
	private readonly StrategyParam<int> _closeBuyBufferIndex;
	private readonly StrategyParam<int> _closeSellBufferIndex;
	private readonly StrategyParam<int> _closeMainBufferIndex;
	private readonly StrategyParam<int> _closeSignalBufferIndex;
	private readonly StrategyParam<decimal> _closeBuyLevel;
	private readonly StrategyParam<decimal> _closeSellLevel;
	private readonly StrategyParam<SignalMode> _closeMode;

	private readonly StrategyParam<bool> _checkProfit;
	private readonly StrategyParam<decimal> _minimalProfit;
	private readonly StrategyParam<bool> _checkStopDistance;
	private readonly StrategyParam<decimal> _minimalStopDistance;

	private readonly StrategyParam<ExecutionMode> _executionMode;
	private readonly StrategyParam<int> _sleepBars;
	private readonly StrategyParam<bool> _cancelSleeping;
	private readonly StrategyParam<int> _maxOrdersCount;
	private readonly StrategyParam<int> _maxBuyCount;
	private readonly StrategyParam<int> _maxSellCount;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _baseOrderVolume;

	private readonly StrategyParam<bool> _trailingStopEnabled;
	private readonly StrategyParam<int> _trailingStartPoints;
	private readonly StrategyParam<int> _trailingDistancePoints;

	private readonly StrategyParam<bool> _breakEvenEnabled;
	private readonly StrategyParam<int> _breakEvenStartPoints;
	private readonly StrategyParam<int> _breakEvenLockPoints;

	private readonly StrategyParam<bool> _indicatorTrailingEnabled;
	private readonly StrategyParam<string> _trailingIndicatorName;
	private readonly StrategyParam<string> _trailingIndicatorParameters;
	private readonly StrategyParam<int> _trailingBuyBufferIndex;
	private readonly StrategyParam<int> _trailingSellBufferIndex;
	private readonly StrategyParam<int> _trailingShift;
	private readonly StrategyParam<int> _trailingIndentPoints;
	private readonly StrategyParam<int> _trailingProfitLockPoints;

	private IndicatorState? _entryIndicatorState;
	private IndicatorState? _closeIndicatorState;
	private IndicatorState? _trailingIndicatorState;

	private DateTimeOffset? _lastBuyBarTime;
	private DateTimeOffset? _lastSellBarTime;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal? _highestPrice;
	private decimal? _lowestPrice;

	private static readonly Dictionary<Type, PropertyInfo[]> _valuePropertyCache = new();

	/// <summary>
	/// Initializes a new instance of <see cref="ExpICustomStrategy"/>.
	/// </summary>
	public ExpICustomStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for indicator calculations", "General");

		_entryIndicatorName = Param(nameof(EntryIndicatorName), "SMA")
		.SetDisplay("Entry Indicator", "Type name of the indicator used for entries", "Entry");

		_entryIndicatorParameters = Param(nameof(EntryIndicatorParameters), "Length=14")
		.SetDisplay("Entry Parameters", "Slash separated parameter list (e.g. Length=14/Width=2)", "Entry");

		_entryShift = Param(nameof(EntryShift), 1)
		.SetDisplay("Entry Shift", "Historical shift used when reading indicator buffers", "Entry");

		_entryBuyBufferIndex = Param(nameof(EntryBuyBufferIndex), 0)
		.SetDisplay("Buy Buffer", "Buffer index used for buy signals", "Entry");

		_entrySellBufferIndex = Param(nameof(EntrySellBufferIndex), 1)
		.SetDisplay("Sell Buffer", "Buffer index used for sell signals", "Entry");

		_entryMainBufferIndex = Param(nameof(EntryMainBufferIndex), 0)
		.SetDisplay("Main Buffer", "Primary buffer for cross/level checks", "Entry");

		_entrySignalBufferIndex = Param(nameof(EntrySignalBufferIndex), 1)
		.SetDisplay("Signal Buffer", "Secondary buffer for cross checks", "Entry");

		_entryBuyLevel = Param(nameof(EntryBuyLevel), 20m)
		.SetDisplay("Buy Level", "Level for long signals in level mode", "Entry");

		_entrySellLevel = Param(nameof(EntrySellLevel), 80m)
		.SetDisplay("Sell Level", "Level for short signals in level mode", "Entry");

		_entryMode = Param(nameof(EntryMode), SignalMode.Arrows)
		.SetDisplay("Entry Mode", "How indicator buffers are interpreted for entries", "Entry");

		_closeIndicatorName = Param(nameof(CloseIndicatorName), string.Empty)
		.SetDisplay("Close Indicator", "Type name of the indicator used for exits", "Exit");

		_closeIndicatorParameters = Param(nameof(CloseIndicatorParameters), string.Empty)
		.SetDisplay("Close Parameters", "Slash separated parameter list for the exit indicator", "Exit");

		_closeUseOpenIndicator = Param(nameof(CloseUseOpenIndicator), true)
		.SetDisplay("Reuse Entry Indicator", "Use entry indicator for exit signals when possible", "Exit");

		_closeShift = Param(nameof(CloseShift), 1)
		.SetDisplay("Close Shift", "Historical shift when reading exit buffers", "Exit");

		_closeBuyBufferIndex = Param(nameof(CloseBuyBufferIndex), 0)
		.SetDisplay("Close Buy Buffer", "Buffer index that closes long positions", "Exit");

		_closeSellBufferIndex = Param(nameof(CloseSellBufferIndex), 1)
		.SetDisplay("Close Sell Buffer", "Buffer index that closes short positions", "Exit");

		_closeMainBufferIndex = Param(nameof(CloseMainBufferIndex), 0)
		.SetDisplay("Close Main Buffer", "Primary buffer for exit cross/level checks", "Exit");

		_closeSignalBufferIndex = Param(nameof(CloseSignalBufferIndex), 1)
		.SetDisplay("Close Signal Buffer", "Secondary buffer for exit cross checks", "Exit");

		_closeBuyLevel = Param(nameof(CloseBuyLevel), 80m)
		.SetDisplay("Close Buy Level", "Level that forces long exit in level mode", "Exit");

		_closeSellLevel = Param(nameof(CloseSellLevel), 20m)
		.SetDisplay("Close Sell Level", "Level that forces short exit in level mode", "Exit");

		_closeMode = Param(nameof(CloseMode), SignalMode.Arrows)
		.SetDisplay("Close Mode", "How exit buffers are interpreted", "Exit");

		_checkProfit = Param(nameof(CheckProfit), false)
		.SetDisplay("Check Profit", "Only close positions when profit exceeds minimal threshold", "Exit");

		_minimalProfit = Param(nameof(MinimalProfit), 0m)
		.SetDisplay("Minimal Profit", "Minimal profit in points required for discretionary exits", "Exit");

		_checkStopDistance = Param(nameof(CheckStopDistance), false)
		.SetDisplay("Check Stop Distance", "Skip closing if stop is farther than the specified distance", "Exit");

		_minimalStopDistance = Param(nameof(MinimalStopDistance), 0m)
		.SetDisplay("Minimal Stop Distance", "Threshold in points used when Check Stop Distance is enabled", "Exit");

		_executionMode = Param(nameof(ExecutionMode), ExecutionMode.Market)
		.SetDisplay("Order Execution", "Execution mode (only market supported in the port)", "Trading");

		_sleepBars = Param(nameof(SleepBars), 1)
		.SetDisplay("Sleep Bars", "Minimal number of bars between new entries of the same direction", "Trading");

		_cancelSleeping = Param(nameof(CancelSleeping), true)
		.SetDisplay("Cancel Sleeping", "Reset sleep timer after opposite trade", "Trading");

		_maxOrdersCount = Param(nameof(MaxOrdersCount), -1)
		.SetDisplay("Max Orders", "Maximum simultaneous net orders (-1 = unlimited)", "Trading");

		_maxBuyCount = Param(nameof(MaxBuyCount), -1)
		.SetDisplay("Max Buy Orders", "Maximum stacked long trades (-1 = unlimited)", "Trading");

		_maxSellCount = Param(nameof(MaxSellCount), -1)
		.SetDisplay("Max Sell Orders", "Maximum stacked short trades (-1 = unlimited)", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 25)
		.SetDisplay("Stop Loss", "Stop loss distance in indicator points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 25)
		.SetDisplay("Take Profit", "Take profit distance in indicator points", "Risk");

		_baseOrderVolume = Param(nameof(BaseOrderVolume), 1m)
		.SetDisplay("Base Volume", "Volume used for a single entry", "Trading");

		_trailingStopEnabled = Param(nameof(TrailingStopEnabled), false)
		.SetDisplay("Price Trailing", "Enable candle-based trailing stop", "Risk");

		_trailingStartPoints = Param(nameof(TrailingStartPoints), 50)
		.SetDisplay("Trailing Start", "Distance in points that activates trailing", "Risk");

		_trailingDistancePoints = Param(nameof(TrailingDistancePoints), 15)
		.SetDisplay("Trailing Distance", "Distance maintained by the trailing stop", "Risk");

		_breakEvenEnabled = Param(nameof(BreakEvenEnabled), false)
		.SetDisplay("Break Even", "Move stop loss to break even after specified profit", "Risk");

		_breakEvenStartPoints = Param(nameof(BreakEvenStartPoints), 30)
		.SetDisplay("Break Even Start", "Profit in points required to activate break even", "Risk");

		_breakEvenLockPoints = Param(nameof(BreakEvenLockPoints), 15)
		.SetDisplay("Break Even Lock", "Points locked in once break even triggers", "Risk");

		_indicatorTrailingEnabled = Param(nameof(IndicatorTrailingEnabled), false)
		.SetDisplay("Indicator Trailing", "Enable indicator-based trailing stop", "Risk");

		_trailingIndicatorName = Param(nameof(TrailingIndicatorName), string.Empty)
		.SetDisplay("Trailing Indicator", "Type name of the trailing indicator", "Risk");

		_trailingIndicatorParameters = Param(nameof(TrailingIndicatorParameters), string.Empty)
		.SetDisplay("Trailing Parameters", "Slash separated parameters for trailing indicator", "Risk");

		_trailingBuyBufferIndex = Param(nameof(TrailingBuyBufferIndex), 0)
		.SetDisplay("Trailing Buy Buffer", "Buffer index guiding long trailing", "Risk");

		_trailingSellBufferIndex = Param(nameof(TrailingSellBufferIndex), 1)
		.SetDisplay("Trailing Sell Buffer", "Buffer index guiding short trailing", "Risk");

		_trailingShift = Param(nameof(TrailingShift), 1)
		.SetDisplay("Trailing Shift", "Historical shift for trailing indicator", "Risk");

		_trailingIndentPoints = Param(nameof(TrailingIndentPoints), 0)
		.SetDisplay("Trailing Indent", "Additional indentation applied to trailing values", "Risk");

		_trailingProfitLockPoints = Param(nameof(TrailingProfitLockPoints), 0)
		.SetDisplay("Trailing Profit Lock", "Minimal profit required before indicator trailing activates", "Risk");
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Entry indicator type name.
	/// </summary>
	public string EntryIndicatorName
	{
		get => _entryIndicatorName.Value;
		set => _entryIndicatorName.Value = value;
	}

	/// <summary>
	/// Entry indicator parameters.
	/// </summary>
	public string EntryIndicatorParameters
	{
		get => _entryIndicatorParameters.Value;
		set => _entryIndicatorParameters.Value = value;
	}

	/// <summary>
	/// Indicator shift used for entries.
	/// </summary>
	public int EntryShift
	{
		get => _entryShift.Value;
		set => _entryShift.Value = value;
	}

	/// <summary>
	/// Buy buffer index for entries.
	/// </summary>
	public int EntryBuyBufferIndex
	{
		get => _entryBuyBufferIndex.Value;
		set => _entryBuyBufferIndex.Value = value;
	}

	/// <summary>
	/// Sell buffer index for entries.
	/// </summary>
	public int EntrySellBufferIndex
	{
		get => _entrySellBufferIndex.Value;
		set => _entrySellBufferIndex.Value = value;
	}

	/// <summary>
	/// Main buffer index used in cross/level entry modes.
	/// </summary>
	public int EntryMainBufferIndex
	{
		get => _entryMainBufferIndex.Value;
		set => _entryMainBufferIndex.Value = value;
	}

	/// <summary>
	/// Secondary buffer index used in cross entry mode.
	/// </summary>
	public int EntrySignalBufferIndex
	{
		get => _entrySignalBufferIndex.Value;
		set => _entrySignalBufferIndex.Value = value;
	}

	/// <summary>
	/// Entry buy level for level mode.
	/// </summary>
	public decimal EntryBuyLevel
	{
		get => _entryBuyLevel.Value;
		set => _entryBuyLevel.Value = value;
	}

	/// <summary>
	/// Entry sell level for level mode.
	/// </summary>
	public decimal EntrySellLevel
	{
		get => _entrySellLevel.Value;
		set => _entrySellLevel.Value = value;
	}

	/// <summary>
	/// Entry signal interpretation mode.
	/// </summary>
	public SignalMode EntryMode
	{
		get => _entryMode.Value;
		set => _entryMode.Value = value;
	}

	/// <summary>
	/// Exit indicator type name.
	/// </summary>
	public string CloseIndicatorName
	{
		get => _closeIndicatorName.Value;
		set => _closeIndicatorName.Value = value;
	}

	/// <summary>
	/// Exit indicator parameters.
	/// </summary>
	public string CloseIndicatorParameters
	{
		get => _closeIndicatorParameters.Value;
		set => _closeIndicatorParameters.Value = value;
	}

	/// <summary>
	/// Reuse entry indicator for exit signals.
	/// </summary>
	public bool CloseUseOpenIndicator
	{
		get => _closeUseOpenIndicator.Value;
		set => _closeUseOpenIndicator.Value = value;
	}

	/// <summary>
	/// Indicator shift used for exits.
	/// </summary>
	public int CloseShift
	{
		get => _closeShift.Value;
		set => _closeShift.Value = value;
	}

	/// <summary>
	/// Buffer index closing long positions.
	/// </summary>
	public int CloseBuyBufferIndex
	{
		get => _closeBuyBufferIndex.Value;
		set => _closeBuyBufferIndex.Value = value;
	}

	/// <summary>
	/// Buffer index closing short positions.
	/// </summary>
	public int CloseSellBufferIndex
	{
		get => _closeSellBufferIndex.Value;
		set => _closeSellBufferIndex.Value = value;
	}

	/// <summary>
	/// Main buffer index used for exit logic.
	/// </summary>
	public int CloseMainBufferIndex
	{
		get => _closeMainBufferIndex.Value;
		set => _closeMainBufferIndex.Value = value;
	}

	/// <summary>
	/// Secondary buffer index used in exit cross mode.
	/// </summary>
	public int CloseSignalBufferIndex
	{
		get => _closeSignalBufferIndex.Value;
		set => _closeSignalBufferIndex.Value = value;
	}

	/// <summary>
	/// Close level for locking long positions.
	/// </summary>
	public decimal CloseBuyLevel
	{
		get => _closeBuyLevel.Value;
		set => _closeBuyLevel.Value = value;
	}

	/// <summary>
	/// Close level for locking short positions.
	/// </summary>
	public decimal CloseSellLevel
	{
		get => _closeSellLevel.Value;
		set => _closeSellLevel.Value = value;
	}

	/// <summary>
	/// Exit signal interpretation mode.
	/// </summary>
	public SignalMode CloseMode
	{
		get => _closeMode.Value;
		set => _closeMode.Value = value;
	}

	/// <summary>
	/// Require profit before closing by signal.
	/// </summary>
	public bool CheckProfit
	{
		get => _checkProfit.Value;
		set => _checkProfit.Value = value;
	}

	/// <summary>
	/// Minimal profit in points before discretionary exit.
	/// </summary>
	public decimal MinimalProfit
	{
		get => _minimalProfit.Value;
		set => _minimalProfit.Value = value;
	}

	/// <summary>
	/// Require stop to be close before discretionary exit.
	/// </summary>
	public bool CheckStopDistance
	{
		get => _checkStopDistance.Value;
		set => _checkStopDistance.Value = value;
	}

	/// <summary>
	/// Minimal stop distance threshold.
	/// </summary>
	public decimal MinimalStopDistance
	{
		get => _minimalStopDistance.Value;
		set => _minimalStopDistance.Value = value;
	}

	/// <summary>
	/// Execution mode.
	/// </summary>
	public ExecutionMode ExecutionMode
	{
		get => _executionMode.Value;
		set => _executionMode.Value = value;
	}

	/// <summary>
	/// Sleep bars between same direction entries.
	/// </summary>
	public int SleepBars
	{
		get => _sleepBars.Value;
		set => _sleepBars.Value = value;
	}

	/// <summary>
	/// Reset sleep timer on opposite trade.
	/// </summary>
	public bool CancelSleeping
	{
		get => _cancelSleeping.Value;
		set => _cancelSleeping.Value = value;
	}

	/// <summary>
	/// Maximum simultaneous orders.
	/// </summary>
	public int MaxOrdersCount
	{
		get => _maxOrdersCount.Value;
		set => _maxOrdersCount.Value = value;
	}

	/// <summary>
	/// Maximum stacked long orders.
	/// </summary>
	public int MaxBuyCount
	{
		get => _maxBuyCount.Value;
		set => _maxBuyCount.Value = value;
	}

	/// <summary>
	/// Maximum stacked short orders.
	/// </summary>
	public int MaxSellCount
	{
		get => _maxSellCount.Value;
		set => _maxSellCount.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal BaseOrderVolume
	{
		get => _baseOrderVolume.Value;
		set => _baseOrderVolume.Value = value;
	}

	/// <summary>
	/// Enable price trailing stop.
	/// </summary>
	public bool TrailingStopEnabled
	{
		get => _trailingStopEnabled.Value;
		set => _trailingStopEnabled.Value = value;
	}

	/// <summary>
	/// Trailing start distance.
	/// </summary>
	public int TrailingStartPoints
	{
		get => _trailingStartPoints.Value;
		set => _trailingStartPoints.Value = value;
	}

	/// <summary>
	/// Trailing distance.
	/// </summary>
	public int TrailingDistancePoints
	{
		get => _trailingDistancePoints.Value;
		set => _trailingDistancePoints.Value = value;
	}

	/// <summary>
	/// Enable break even.
	/// </summary>
	public bool BreakEvenEnabled
	{
		get => _breakEvenEnabled.Value;
		set => _breakEvenEnabled.Value = value;
	}

	/// <summary>
	/// Break even activation threshold.
	/// </summary>
	public int BreakEvenStartPoints
	{
		get => _breakEvenStartPoints.Value;
		set => _breakEvenStartPoints.Value = value;
	}

	/// <summary>
	/// Break even lock points.
	/// </summary>
	public int BreakEvenLockPoints
	{
		get => _breakEvenLockPoints.Value;
		set => _breakEvenLockPoints.Value = value;
	}

	/// <summary>
	/// Enable indicator trailing stop.
	/// </summary>
	public bool IndicatorTrailingEnabled
	{
		get => _indicatorTrailingEnabled.Value;
		set => _indicatorTrailingEnabled.Value = value;
	}

	/// <summary>
	/// Trailing indicator name.
	/// </summary>
	public string TrailingIndicatorName
	{
		get => _trailingIndicatorName.Value;
		set => _trailingIndicatorName.Value = value;
	}

	/// <summary>
	/// Trailing indicator parameters.
	/// </summary>
	public string TrailingIndicatorParameters
	{
		get => _trailingIndicatorParameters.Value;
		set => _trailingIndicatorParameters.Value = value;
	}

	/// <summary>
	/// Buffer index used for long trailing.
	/// </summary>
	public int TrailingBuyBufferIndex
	{
		get => _trailingBuyBufferIndex.Value;
		set => _trailingBuyBufferIndex.Value = value;
	}

	/// <summary>
	/// Buffer index used for short trailing.
	/// </summary>
	public int TrailingSellBufferIndex
	{
		get => _trailingSellBufferIndex.Value;
		set => _trailingSellBufferIndex.Value = value;
	}

	/// <summary>
	/// Trailing indicator shift.
	/// </summary>
	public int TrailingShift
	{
		get => _trailingShift.Value;
		set => _trailingShift.Value = value;
	}

	/// <summary>
	/// Additional indent applied to trailing indicator value.
	/// </summary>
	public int TrailingIndentPoints
	{
		get => _trailingIndentPoints.Value;
		set => _trailingIndentPoints.Value = value;
	}

	/// <summary>
	/// Minimal profit before indicator trailing engages.
	/// </summary>
	public int TrailingProfitLockPoints
	{
		get => _trailingProfitLockPoints.Value;
		set => _trailingProfitLockPoints.Value = value;
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

		_entryIndicatorState?.Reset();
		_closeIndicatorState?.Reset();
		_trailingIndicatorState?.Reset();

		_lastBuyBarTime = null;
		_lastSellBarTime = null;
		_entryPrice = 0m;
		_stopPrice = null;
		_takeProfitPrice = null;
		_highestPrice = null;
		_lowestPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
		throw new InvalidOperationException("Security must be assigned before starting the strategy.");

		if (ExecutionMode != ExecutionMode.Market)
		throw new NotSupportedException("Only market execution mode is implemented in the StockSharp port.");

		Volume = BaseOrderVolume;

		_entryIndicatorState = new IndicatorState(CreateIndicator(EntryIndicatorName, EntryIndicatorParameters));
		_closeIndicatorState = CloseUseOpenIndicator ? _entryIndicatorState : new IndicatorState(CreateIndicator(CloseIndicatorName, CloseIndicatorParameters));
		_trailingIndicatorState = IndicatorTrailingEnabled ? new IndicatorState(CreateIndicator(TrailingIndicatorName, TrailingIndicatorParameters)) : null;

		_entryIndicatorState?.Reset();
		if (_closeIndicatorState != _entryIndicatorState)
		_closeIndicatorState?.Reset();
		_trailingIndicatorState?.Reset();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(candle => ProcessCandle(candle)).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var priceSelector = new Func<ICandleMessage, decimal>(GetPriceForIndicator);

		var processed = new HashSet<IndicatorState>();

		ProcessIndicatorState(_entryIndicatorState, candle, priceSelector, processed);
		ProcessIndicatorState(_closeIndicatorState, candle, priceSelector, processed);
		ProcessIndicatorState(_trailingIndicatorState, candle, priceSelector, processed);

		HandleExitSignals(candle);
		HandleEntrySignals(candle);
		UpdateRiskManagement(candle);
	}

	private void ProcessIndicatorState(IndicatorState? state, ICandleMessage candle, Func<ICandleMessage, decimal> priceSelector, HashSet<IndicatorState> processed)
	{
		if (state == null)
		return;

		if (!processed.Add(state))
		return;

		state.Process(candle, priceSelector);
	}

	private void HandleEntrySignals(ICandleMessage candle)
	{
		if (_entryIndicatorState == null)
		return;

		var barTime = candle.OpenTime;

		var buySignal = EvaluateSignal(_entryIndicatorState, EntryMode, EntryBuyBufferIndex, EntrySellBufferIndex, EntryMainBufferIndex, EntrySignalBufferIndex, EntryBuyLevel, EntrySellLevel, EntryShift, true);
		var sellSignal = EvaluateSignal(_entryIndicatorState, EntryMode, EntryBuyBufferIndex, EntrySellBufferIndex, EntryMainBufferIndex, EntrySignalBufferIndex, EntryBuyLevel, EntrySellLevel, EntryShift, false);

		if (buySignal && sellSignal)
		{
			buySignal = false;
			sellSignal = false;
		}

		if (buySignal)
		TryOpenPosition(true, barTime, candle);

		if (sellSignal)
		TryOpenPosition(false, barTime, candle);
	}

	private void HandleExitSignals(ICandleMessage candle)
	{
		if (_entryIndicatorState == null)
		return;

		if (EntryMode == SignalMode.Cross && CloseMode == SignalMode.Cross)
		{
			var closeBuy = EvaluateSignal(_entryIndicatorState, EntryMode, EntryBuyBufferIndex, EntrySellBufferIndex, EntryMainBufferIndex, EntrySignalBufferIndex, EntryBuyLevel, EntrySellLevel, EntryShift, false);
			var closeSell = EvaluateSignal(_entryIndicatorState, EntryMode, EntryBuyBufferIndex, EntrySellBufferIndex, EntryMainBufferIndex, EntrySignalBufferIndex, EntryBuyLevel, EntrySellLevel, EntryShift, true);
			if (closeBuy)
			TryClosePosition(true, candle);
			if (closeSell)
			TryClosePosition(false, candle);
			return;
		}

		var state = _closeIndicatorState ?? _entryIndicatorState;

		var closeLong = EvaluateSignal(state, CloseMode, CloseBuyBufferIndex, CloseSellBufferIndex, CloseMainBufferIndex, CloseSignalBufferIndex, CloseBuyLevel, CloseSellLevel, CloseShift, false);
		var closeShort = EvaluateSignal(state, CloseMode, CloseBuyBufferIndex, CloseSellBufferIndex, CloseMainBufferIndex, CloseSignalBufferIndex, CloseBuyLevel, CloseSellLevel, CloseShift, true);

		if (closeLong)
		TryClosePosition(true, candle);

		if (closeShort)
		TryClosePosition(false, candle);
	}

	private bool EvaluateSignal(IndicatorState? state, SignalMode mode, int buyBuffer, int sellBuffer, int mainBuffer, int signalBuffer, decimal buyLevel, decimal sellLevel, int shift, bool isBuy)
	{
		if (state == null)
		return false;

		switch (mode)
		{
			case SignalMode.Arrows:
			{
				var bufferIndex = isBuy ? buyBuffer : sellBuffer;
				var value = state.GetValue(bufferIndex, shift);
				return value.HasValue && value.Value != 0m;
			}
			case SignalMode.Cross:
			{
				var mainNow = state.GetValue(mainBuffer, shift);
				var signalNow = state.GetValue(signalBuffer, shift);
				var mainPrev = state.GetValue(mainBuffer, shift + 1);
				var signalPrev = state.GetValue(signalBuffer, shift + 1);

				if (!mainNow.HasValue || !signalNow.HasValue || !mainPrev.HasValue || !signalPrev.HasValue)
				return false;

				if (isBuy)
				return mainNow.Value > signalNow.Value && !(mainPrev.Value > signalPrev.Value);

				return mainNow.Value < signalNow.Value && !(mainPrev.Value < signalPrev.Value);
			}
			case SignalMode.Levels:
			{
				var current = state.GetValue(mainBuffer, shift);
				var previous = state.GetValue(mainBuffer, shift + 1);

				if (!current.HasValue || !previous.HasValue)
				return false;

				if (isBuy)
				return current.Value > buyLevel && !(previous.Value > buyLevel);

				return current.Value < sellLevel && !(previous.Value < sellLevel);
			}
			case SignalMode.Slope:
			{
				var current = state.GetValue(mainBuffer, shift);
				var mid = state.GetValue(mainBuffer, shift + 1);
				var previous = state.GetValue(mainBuffer, shift + 2);

				if (!current.HasValue || !mid.HasValue || !previous.HasValue)
				return false;

				if (isBuy)
				return current.Value > mid.Value && previous.Value > mid.Value;

				return current.Value < mid.Value && previous.Value < mid.Value;
			}
			case SignalMode.RangeBreak:
			{
				var bufferIndex = isBuy ? buyBuffer : sellBuffer;
				var current = state.GetValue(bufferIndex, shift);
				var previous = state.GetValue(bufferIndex, shift + 1);

				if (!current.HasValue)
				return false;

				var currentValid = current.Value != 0m;
				var previousValid = previous.HasValue && previous.Value != 0m && previous.Value > 0m;

				return currentValid && !previousValid;
			}
			default:
			return false;
		}
	}

	private void TryOpenPosition(bool isBuy, DateTimeOffset barTime, ICandleMessage candle)
	{
		if (!IsSleepCompleted(isBuy, barTime))
		return;

		if (!IsWithinOrderLimits(isBuy))
		return;

		var volume = BaseOrderVolume;
		var positionVolume = Math.Abs(Position);
		if (positionVolume > 0)
		volume += positionVolume;

		if (volume <= 0)
		return;

		if (isBuy)
		{
			BuyMarket(volume);
			_lastBuyBarTime = barTime;
			if (CancelSleeping)
			_lastSellBarTime = null;
			InitializePositionState(candle, true);
		}
		else
		{
			SellMarket(volume);
			_lastSellBarTime = barTime;
			if (CancelSleeping)
			_lastBuyBarTime = null;
			InitializePositionState(candle, false);
		}
	}

	private bool IsSleepCompleted(bool isBuy, DateTimeOffset barTime)
	{
		if (SleepBars <= 0)
		return true;

		var timeframe = GetTimeFrame();
		if (timeframe == null)
		return true;

		var lastTime = isBuy ? _lastBuyBarTime : _lastSellBarTime;
		if (lastTime == null)
		return true;

		var delta = barTime - lastTime.Value;
		return delta >= timeframe.Value * SleepBars;
	}

	private bool IsWithinOrderLimits(bool isBuy)
	{
		if (BaseOrderVolume <= 0)
		return false;

		var currentCount = GetStackedOrderCount();

		if (MaxOrdersCount >= 0 && currentCount >= MaxOrdersCount)
		return false;

		if (isBuy)
		{
			var longCount = Position > 0 ? GetStackedOrderCount() : 0;
			return MaxBuyCount < 0 || longCount < MaxBuyCount;
		}

		var shortCount = Position < 0 ? GetStackedOrderCount() : 0;
		return MaxSellCount < 0 || shortCount < MaxSellCount;
	}

	private int GetStackedOrderCount()
	{
		if (BaseOrderVolume <= 0)
		return 0;

		var count = (int)Math.Round(Math.Abs(Position) / BaseOrderVolume, MidpointRounding.AwayFromZero);
		return Math.Max(count, 0);
	}

	private void TryClosePosition(bool closeLong, ICandleMessage candle)
	{
		if (closeLong)
		{
			if (Position <= 0)
			return;

			if (!CanCloseWithFilters(true, candle.ClosePrice))
			return;

			SellMarket(Math.Abs(Position));
			ResetPositionState();
		}
		else
		{
			if (Position >= 0)
			return;

			if (!CanCloseWithFilters(false, candle.ClosePrice))
			return;

			BuyMarket(Math.Abs(Position));
			ResetPositionState();
		}
	}

	private bool CanCloseWithFilters(bool closingLong, decimal price)
	{
		var priceStep = GetPriceStep();

		if (CheckProfit)
		{
			var profit = closingLong ? price - _entryPrice : _entryPrice - price;
			if (profit < MinimalProfit * priceStep)
			return false;
		}

		if (CheckStopDistance && _stopPrice.HasValue)
		{
			var distance = Math.Abs(_stopPrice.Value - _entryPrice);
			if (distance >= MinimalStopDistance * priceStep)
			return false;
		}

		return true;
	}

	private void InitializePositionState(ICandleMessage candle, bool isLong)
	{
		_entryPrice = candle.ClosePrice;
		_highestPrice = candle.HighPrice;
		_lowestPrice = candle.LowPrice;

		var priceStep = GetPriceStep();

		if (StopLossPoints > 0)
		{
			var distance = StopLossPoints * priceStep;
			_stopPrice = isLong ? _entryPrice - distance : _entryPrice + distance;
		}
		else
		{
			_stopPrice = null;
		}

		if (TakeProfitPoints > 0)
		{
			var distance = TakeProfitPoints * priceStep;
			_takeProfitPrice = isLong ? _entryPrice + distance : _entryPrice - distance;
		}
		else
		{
			_takeProfitPrice = null;
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takeProfitPrice = null;
		_highestPrice = null;
		_lowestPrice = null;
	}

	private void UpdateRiskManagement(ICandleMessage candle)
	{
		if (Position > 0)
		{
			_highestPrice = _highestPrice.HasValue ? Math.Max(_highestPrice.Value, candle.HighPrice) : candle.HighPrice;
			_lowestPrice = candle.LowPrice;

			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			ApplyTrailingForLong(candle);
		}
		else if (Position < 0)
		{
			_lowestPrice = _lowestPrice.HasValue ? Math.Min(_lowestPrice.Value, candle.LowPrice) : candle.LowPrice;
			_highestPrice = candle.HighPrice;

			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			ApplyTrailingForShort(candle);
		}
	}

	private void ApplyTrailingForLong(ICandleMessage candle)
	{
		var priceStep = GetPriceStep();
		var currentProfit = candle.HighPrice - _entryPrice;

		if (TrailingStopEnabled && TrailingDistancePoints > 0 && TrailingStartPoints > 0)
		{
			var activation = TrailingStartPoints * priceStep;
			if (currentProfit >= activation)
			{
				var trailDistance = TrailingDistancePoints * priceStep;
				var newStop = candle.HighPrice - trailDistance;
				if (!_stopPrice.HasValue || newStop > _stopPrice.Value)
				_stopPrice = newStop;
			}
		}

		if (BreakEvenEnabled && BreakEvenStartPoints > 0)
		{
			var activation = BreakEvenStartPoints * priceStep;
			if (currentProfit >= activation)
			{
				var lockDistance = BreakEvenLockPoints * priceStep;
				var newStop = _entryPrice + lockDistance;
				if (!_stopPrice.HasValue || newStop > _stopPrice.Value)
				_stopPrice = newStop;
			}
		}

		if (IndicatorTrailingEnabled && _trailingIndicatorState != null)
		{
			var value = _trailingIndicatorState.GetValue(TrailingBuyBufferIndex, TrailingShift);
			if (value.HasValue)
			{
				var indent = TrailingIndentPoints * priceStep;
				var adjusted = value.Value - indent;
				var lockLevel = TrailingProfitLockPoints * priceStep;
				if (adjusted > 0m && adjusted >= _entryPrice + lockLevel)
				{
					if (!_stopPrice.HasValue || adjusted > _stopPrice.Value)
					_stopPrice = adjusted;
				}
			}
		}
	}

	private void ApplyTrailingForShort(ICandleMessage candle)
	{
		var priceStep = GetPriceStep();
		var currentProfit = _entryPrice - candle.LowPrice;

		if (TrailingStopEnabled && TrailingDistancePoints > 0 && TrailingStartPoints > 0)
		{
			var activation = TrailingStartPoints * priceStep;
			if (currentProfit >= activation)
			{
				var trailDistance = TrailingDistancePoints * priceStep;
				var newStop = candle.LowPrice + trailDistance;
				if (!_stopPrice.HasValue || newStop < _stopPrice.Value)
				_stopPrice = newStop;
			}
		}

		if (BreakEvenEnabled && BreakEvenStartPoints > 0)
		{
			var activation = BreakEvenStartPoints * priceStep;
			if (currentProfit >= activation)
			{
				var lockDistance = BreakEvenLockPoints * priceStep;
				var newStop = _entryPrice - lockDistance;
				if (!_stopPrice.HasValue || newStop < _stopPrice.Value)
				_stopPrice = newStop;
			}
		}

		if (IndicatorTrailingEnabled && _trailingIndicatorState != null)
		{
			var value = _trailingIndicatorState.GetValue(TrailingSellBufferIndex, TrailingShift);
			if (value.HasValue)
			{
				var indent = TrailingIndentPoints * priceStep;
				var adjusted = value.Value + indent;
				var lockLevel = TrailingProfitLockPoints * priceStep;
				if (adjusted > 0m && adjusted <= _entryPrice - lockLevel)
				{
					if (!_stopPrice.HasValue || adjusted < _stopPrice.Value)
					_stopPrice = adjusted;
				}
			}
		}
	}

	private TimeSpan? GetTimeFrame()
	{
		return CandleType.Arg as TimeSpan?;
	}

	private decimal GetPriceForIndicator(ICandleMessage candle)
	{
		return candle.ClosePrice;
	}

	private decimal GetPriceStep()
	{
		return Security?.PriceStep ?? 1m;
	}

	private static decimal?[] ExtractBufferValues(IIndicatorValue value)
	{
		if (value == null)
		return Array.Empty<decimal?>();

		try
		{
			var single = value.GetValue<decimal>();
			return new decimal?[] { single };
		}
		catch
		{
			// Continue with reflection.
		}

		var type = value.GetType();
		PropertyInfo[] properties;

		lock (_valuePropertyCache)
		{
			if (!_valuePropertyCache.TryGetValue(type, out properties))
			{
				properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
				_valuePropertyCache[type] = properties;
			}
		}

		var results = new List<decimal?>();

		foreach (var property in properties)
		{
			if (!property.CanRead || property.GetIndexParameters().Length != 0)
			continue;

			object raw;
			try
			{
				raw = property.GetValue(value);
			}
			catch
			{
				continue;
			}

			if (raw is decimal decimalValue)
			{
				results.Add(decimalValue);
				continue;
			}

			if (raw is decimal? nullableDecimal)
			{
				results.Add(nullableDecimal);
				continue;
			}

			if (raw is double doubleValue)
			{
				results.Add((decimal)doubleValue);
				continue;
			}

			if (raw is double? nullableDouble)
			{
				results.Add(nullableDouble.HasValue ? (decimal?)nullableDouble.Value : null);
				continue;
			}
		}

		return results.ToArray();
	}

	private static IIndicator CreateIndicator(string name, string parameters)
	{
		if (string.IsNullOrWhiteSpace(name))
		return null;

		var type = Type.GetType(name, false, true);
		if (type == null)
		type = Type.GetType($"StockSharp.Algo.Indicators.{name}, StockSharp.Algo", false, true);

		if (type == null)
		throw new InvalidOperationException($"Cannot resolve indicator type '{name}'.");

		if (!typeof(IIndicator).IsAssignableFrom(type))
		throw new InvalidOperationException($"Type '{type.FullName}' does not implement IIndicator.");

		var indicator = (IIndicator)Activator.CreateInstance(type)!;
		ApplyParameters(indicator, parameters);
		return indicator;
	}

	private static void ApplyParameters(IIndicator indicator, string parameters)
	{
		if (string.IsNullOrWhiteSpace(parameters))
		return;

		var namedValues = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
		var orderedValues = new List<string>();

		var parts = parameters.Split('/');
		foreach (var part in parts)
		{
			var trimmed = part.Trim();
			if (trimmed.Length == 0)
			continue;

			var eqIndex = trimmed.IndexOf('=');
			if (eqIndex > 0)
			{
				var propertyName = trimmed.Substring(0, eqIndex).Trim();
				var propertyValue = trimmed.Substring(eqIndex + 1).Trim();
				namedValues[propertyName] = propertyValue;
			}
			else
			{
				orderedValues.Add(trimmed);
			}
		}

		var type = indicator.GetType();
		var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

		foreach (var pair in namedValues)
		{
			var property = Array.Find(properties, p => string.Equals(p.Name, pair.Key, StringComparison.InvariantCultureIgnoreCase));
			if (property == null || !property.CanWrite)
			continue;

			var converted = ConvertString(pair.Value, property.PropertyType);
			property.SetValue(indicator, converted);
		}

		if (orderedValues.Count == 0)
		return;

		var assignable = new List<PropertyInfo>();
		foreach (var property in properties)
		{
			if (!property.CanWrite)
			continue;

			if (namedValues.ContainsKey(property.Name))
			continue;

			var typeCode = Type.GetTypeCode(property.PropertyType);
			if (typeCode == TypeCode.Int32 || typeCode == TypeCode.Decimal || typeCode == TypeCode.Double || typeCode == TypeCode.Boolean)
			assignable.Add(property);
		}

		for (var i = 0; i < orderedValues.Count && i < assignable.Count; i++)
		{
			var value = ConvertString(orderedValues[i], assignable[i].PropertyType);
			assignable[i].SetValue(indicator, value);
		}
	}

	private static object ConvertString(string value, Type targetType)
	{
		if (targetType == typeof(string))
		return value;

		if (targetType == typeof(int) || targetType == typeof(int?))
		return int.Parse(value, CultureInfo.InvariantCulture);

		if (targetType == typeof(decimal) || targetType == typeof(decimal?))
		return decimal.Parse(value, CultureInfo.InvariantCulture);

		if (targetType == typeof(double) || targetType == typeof(double?))
		return double.Parse(value, CultureInfo.InvariantCulture);

		if (targetType == typeof(bool) || targetType == typeof(bool?))
		{
			if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
			return numeric != 0;

			return bool.Parse(value);
		}

		if (targetType.IsEnum)
		{
			if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
			return Enum.ToObject(targetType, numeric);

			return Enum.Parse(targetType, value, true);
		}

		return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
	}
}
