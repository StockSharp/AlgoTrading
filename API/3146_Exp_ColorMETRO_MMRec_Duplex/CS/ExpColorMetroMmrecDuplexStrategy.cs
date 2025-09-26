using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the Exp_ColorMETRO_MMRec_Duplex MetaTrader expert advisor.
/// Couples two ColorMETRO modules with MMRec position sizing.
/// </summary>
public class ExpColorMetroMmrecDuplexStrategy : Strategy
{
	private readonly Module _longModule;
	private readonly Module _shortModule;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpColorMetroMmrecDuplexStrategy"/> class.
	/// </summary>
	public ExpColorMetroMmrecDuplexStrategy()
	{
		_longModule = new Module(
		strategy: this,
		key: "Long",
		isLong: true,
		defaultCandleType: TimeSpan.FromHours(4).TimeFrame(),
		defaultMagic: 777,
		defaultTotalTrigger: 5,
		defaultLossTrigger: 3,
		defaultSmallMm: 0.01m,
		defaultMm: 0.1m,
		defaultMarginMode: MarginMode.Lot,
		defaultStopLoss: 1000m,
		defaultTakeProfit: 2000m,
		defaultDeviation: 10m,
		defaultPeriod: 7,
		defaultFastStep: 5,
		defaultSlowStep: 15,
		defaultSignalBar: 1,
		openAllowed: true,
		closeAllowed: true);

		_shortModule = new Module(
		strategy: this,
		key: "Short",
		isLong: false,
		defaultCandleType: TimeSpan.FromHours(4).TimeFrame(),
		defaultMagic: 555,
		defaultTotalTrigger: 5,
		defaultLossTrigger: 3,
		defaultSmallMm: 0.01m,
		defaultMm: 0.1m,
		defaultMarginMode: MarginMode.Lot,
		defaultStopLoss: 1000m,
		defaultTakeProfit: 2000m,
		defaultDeviation: 10m,
		defaultPeriod: 7,
		defaultFastStep: 5,
		defaultSlowStep: 15,
		defaultSignalBar: 1,
		openAllowed: true,
		closeAllowed: true);
	}

	/// <summary>
	/// Candle type used for the long ColorMETRO module.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longModule.CandleType;
		set => _longModule.CandleType = value;
	}

	/// <summary>
	/// Candle type used for the short ColorMETRO module.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortModule.CandleType;
		set => _shortModule.CandleType = value;
	}

	/// <summary>
	/// Total number of recent long trades to inspect when applying MMRec.
	/// </summary>
	public int LongTotalTrigger
	{
		get => _longModule.TotalTrigger;
		set => _longModule.TotalTrigger = value;
	}

	/// <summary>
	/// Total number of recent short trades to inspect when applying MMRec.
	/// </summary>
	public int ShortTotalTrigger
	{
		get => _shortModule.TotalTrigger;
		set => _shortModule.TotalTrigger = value;
	}

	/// <summary>
	/// Loss threshold that switches the long multiplier to the reduced value.
	/// </summary>
	public int LongLossTrigger
	{
		get => _longModule.LossTrigger;
		set => _longModule.LossTrigger = value;
	}

	/// <summary>
	/// Loss threshold that switches the short multiplier to the reduced value.
	/// </summary>
	public int ShortLossTrigger
	{
		get => _shortModule.LossTrigger;
		set => _shortModule.LossTrigger = value;
	}

	/// <summary>
	/// Reduced multiplier for long trades after repeated losses.
	/// </summary>
	public decimal LongSmallMm
	{
		get => _longModule.SmallMm;
		set => _longModule.SmallMm = value;
	}

	/// <summary>
	/// Reduced multiplier for short trades after repeated losses.
	/// </summary>
	public decimal ShortSmallMm
	{
		get => _shortModule.SmallMm;
		set => _shortModule.SmallMm = value;
	}

	/// <summary>
	/// Default multiplier for long trades.
	/// </summary>
	public decimal LongMm
	{
		get => _longModule.Mm;
		set => _longModule.Mm = value;
	}

	/// <summary>
	/// Default multiplier for short trades.
	/// </summary>
	public decimal ShortMm
	{
		get => _shortModule.Mm;
		set => _shortModule.Mm = value;
	}

	/// <summary>
	/// Enables opening long positions.
	/// </summary>
	public bool LongEnableOpen
	{
		get => _longModule.OpenAllowed;
		set => _longModule.OpenAllowed = value;
	}

	/// <summary>
	/// Enables closing long positions.
	/// </summary>
	public bool LongEnableClose
	{
		get => _longModule.CloseAllowed;
		set => _longModule.CloseAllowed = value;
	}

	/// <summary>
	/// Enables opening short positions.
	/// </summary>
	public bool ShortEnableOpen
	{
		get => _shortModule.OpenAllowed;
		set => _shortModule.OpenAllowed = value;
	}

	/// <summary>
	/// Enables closing short positions.
	/// </summary>
	public bool ShortEnableClose
	{
		get => _shortModule.CloseAllowed;
		set => _shortModule.CloseAllowed = value;
	}

	/// <summary>
	/// RSI period used inside the long ColorMETRO module.
	/// </summary>
	public int LongPeriodRsi
	{
		get => _longModule.Period;
		set => _longModule.Period = value;
	}

	/// <summary>
	/// RSI period used inside the short ColorMETRO module.
	/// </summary>
	public int ShortPeriodRsi
	{
		get => _shortModule.Period;
		set => _shortModule.Period = value;
	}

	/// <summary>
	/// Fast step size for the long ColorMETRO band.
	/// </summary>
	public int LongStepSizeFast
	{
		get => _longModule.StepSizeFast;
		set => _longModule.StepSizeFast = value;
	}

	/// <summary>
	/// Fast step size for the short ColorMETRO band.
	/// </summary>
	public int ShortStepSizeFast
	{
		get => _shortModule.StepSizeFast;
		set => _shortModule.StepSizeFast = value;
	}

	/// <summary>
	/// Slow step size for the long ColorMETRO band.
	/// </summary>
	public int LongStepSizeSlow
	{
		get => _longModule.StepSizeSlow;
		set => _longModule.StepSizeSlow = value;
	}

	/// <summary>
	/// Slow step size for the short ColorMETRO band.
	/// </summary>
	public int ShortStepSizeSlow
	{
		get => _shortModule.StepSizeSlow;
		set => _shortModule.StepSizeSlow = value;
	}

	/// <summary>
	/// Historical shift used when evaluating the long signals.
	/// </summary>
	public int LongSignalBar
	{
		get => _longModule.SignalBar;
		set => _longModule.SignalBar = value;
	}

	/// <summary>
	/// Historical shift used when evaluating the short signals.
	/// </summary>
	public int ShortSignalBar
	{
		get => _shortModule.SignalBar;
		set => _shortModule.SignalBar = value;
	}

	/// <summary>
	/// Compatibility parameter mirroring the MT5 magic number for longs.
	/// </summary>
	public int LongMagic
	{
		get => _longModule.Magic;
		set => _longModule.Magic = value;
	}

	/// <summary>
	/// Compatibility parameter mirroring the MT5 magic number for shorts.
	/// </summary>
	public int ShortMagic
	{
		get => _shortModule.Magic;
		set => _shortModule.Magic = value;
	}

	/// <summary>
	/// Compatibility stop-loss placeholder for the long module.
	/// </summary>
	public decimal LongStopLossTicks
	{
		get => _longModule.StopLossTicks;
		set => _longModule.StopLossTicks = value;
	}

	/// <summary>
	/// Compatibility stop-loss placeholder for the short module.
	/// </summary>
	public decimal ShortStopLossTicks
	{
		get => _shortModule.StopLossTicks;
		set => _shortModule.StopLossTicks = value;
	}

	/// <summary>
	/// Compatibility take-profit placeholder for the long module.
	/// </summary>
	public decimal LongTakeProfitTicks
	{
		get => _longModule.TakeProfitTicks;
		set => _longModule.TakeProfitTicks = value;
	}

	/// <summary>
	/// Compatibility take-profit placeholder for the short module.
	/// </summary>
	public decimal ShortTakeProfitTicks
	{
		get => _shortModule.TakeProfitTicks;
		set => _shortModule.TakeProfitTicks = value;
	}

	/// <summary>
	/// Maximum allowed deviation for the long module (compatibility only).
	/// </summary>
	public decimal LongDeviationTicks
	{
		get => _longModule.DeviationTicks;
		set => _longModule.DeviationTicks = value;
	}

	/// <summary>
	/// Maximum allowed deviation for the short module (compatibility only).
	/// </summary>
	public decimal ShortDeviationTicks
	{
		get => _shortModule.DeviationTicks;
		set => _shortModule.DeviationTicks = value;
	}

	/// <summary>
	/// Money management mode for the long module (kept for reference).
	/// </summary>
	public MarginMode LongMarginMode
	{
		get => _longModule.MarginMode;
		set => _longModule.MarginMode = value;
	}

	/// <summary>
	/// Money management mode for the short module (kept for reference).
	/// </summary>
	public MarginMode ShortMarginMode
	{
		get => _shortModule.MarginMode;
		set => _shortModule.MarginMode = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var seen = new HashSet<DataType>();

		foreach (var type in new[] { LongCandleType, ShortCandleType })
		{
			if (seen.Add(type))
			yield return (Security, type);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longModule.Reset();
		_shortModule.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartModule(_longModule);
		StartModule(_shortModule);
	}

	private void StartModule(Module module)
	{
		var indicator = new ColorMetroMmrecIndicator
		{
			Length = module.Period,
			StepSizeFast = module.StepSizeFast,
			StepSizeSlow = module.StepSizeSlow
		};

		var subscription = SubscribeCandles(module.CandleType);
		subscription
		.BindEx(indicator, (candle, value) => ProcessModule(module, candle, value))
		.Start();

		module.SetIndicator(indicator);
	}

	private void ProcessModule(Module module, ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (indicatorValue is not ColorMetroMmrecValue metroValue)
		return;

		if (!metroValue.IsReady)
		return;

		module.Add(metroValue, candle.CloseTime);

		if (!module.TryCreateSnapshot(out var snapshot))
		return;

		var shouldOpen = module.ShouldOpen(snapshot);
		var shouldClose = module.ShouldClose(snapshot);

		if (module.IsLong)
		{
			if (shouldClose && module.CloseAllowed && Position > 0m)
			{
				SellMarket(Position);
				module.FinalizeTrade(candle.ClosePrice);
			}

			if (shouldOpen && module.OpenAllowed && Position <= 0m)
			{
				if (Position < 0m)
				{
					BuyMarket(-Position);
					_shortModule.FinalizeTrade(candle.ClosePrice);
				}

				var volume = module.CalculateVolume(Volume);
				if (volume > 0m)
				{
					BuyMarket(volume);
					module.RegisterEntry(candle.ClosePrice);
				}
			}
		}
		else
		{
			if (shouldClose && module.CloseAllowed && Position < 0m)
			{
				BuyMarket(-Position);
				module.FinalizeTrade(candle.ClosePrice);
			}

			if (shouldOpen && module.OpenAllowed && Position >= 0m)
			{
				if (Position > 0m)
				{
					SellMarket(Position);
					_longModule.FinalizeTrade(candle.ClosePrice);
				}

				var volume = module.CalculateVolume(Volume);
				if (volume > 0m)
				{
					SellMarket(volume);
					module.RegisterEntry(candle.ClosePrice);
				}
			}
		}
	}

	/// <summary>
	/// Money management modes preserved from the MT5 source for reference.
	/// </summary>
	public enum MarginMode
	{
		/// <summary>
		/// Position size derived from the available free margin.
		/// </summary>
		FreeMargin = 0,

		/// <summary>
		/// Position size derived from the account balance.
		/// </summary>
		Balance = 1,

		/// <summary>
		/// Uses loss-adjusted free margin (not implemented in the port).
		/// </summary>
		LossFreeMargin = 2,

		/// <summary>
		/// Uses loss-adjusted balance (not implemented in the port).
		/// </summary>
		LossBalance = 3,

		/// <summary>
		/// Treats MM values as raw lots (behaviour used in this port).
		/// </summary>
		Lot = 4
	}

	private sealed class Module
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _totalTrigger;
		private readonly StrategyParam<int> _lossTrigger;
		private readonly StrategyParam<decimal> _smallMm;
		private readonly StrategyParam<decimal> _mm;
		private readonly StrategyParam<bool> _openAllowed;
		private readonly StrategyParam<bool> _closeAllowed;
		private readonly StrategyParam<int> _period;
		private readonly StrategyParam<int> _fastStep;
		private readonly StrategyParam<int> _slowStep;
		private readonly StrategyParam<int> _signalBar;
		private readonly StrategyParam<int> _magic;
		private readonly StrategyParam<decimal> _stopLossTicks;
		private readonly StrategyParam<decimal> _takeProfitTicks;
		private readonly StrategyParam<decimal> _deviationTicks;
		private readonly StrategyParam<MarginMode> _marginMode;

		private readonly List<decimal> _upHistory = new();
		private readonly List<decimal> _downHistory = new();
		private readonly List<DateTimeOffset> _timeHistory = new();
		private readonly Queue<bool> _recentLosses = new();

		private int _lossCount;
		private decimal? _entryPrice;

		public Module(
		ExpColorMetroMmrecDuplexStrategy strategy,
		string key,
		bool isLong,
		DataType defaultCandleType,
		int defaultMagic,
		int defaultTotalTrigger,
		int defaultLossTrigger,
		decimal defaultSmallMm,
		decimal defaultMm,
		MarginMode defaultMarginMode,
		decimal defaultStopLoss,
		decimal defaultTakeProfit,
		decimal defaultDeviation,
		int defaultPeriod,
		int defaultFastStep,
		int defaultSlowStep,
		int defaultSignalBar,
		bool openAllowed,
		bool closeAllowed)
		{
			IsLong = isLong;
			Key = key;

			_candleType = strategy.Param(key + "_CandleType", defaultCandleType)
			.SetDisplay(key + " Candle Type", "Time-frame for the " + key.ToLowerInvariant() + " module", key);

			_totalTrigger = strategy.Param(key + "_TotalMMTrigger", defaultTotalTrigger)
			.SetDisplay(key + " Total MM Trigger", "Number of recent trades inspected for MMRec", key)
			.SetRange(0, 100);

			_lossTrigger = strategy.Param(key + "_LossMMTrigger", defaultLossTrigger)
			.SetDisplay(key + " Loss MM Trigger", "Losses required to activate the reduced multiplier", key)
			.SetRange(0, 100);

			_smallMm = strategy.Param(key + "_SmallMM", defaultSmallMm)
			.SetDisplay(key + " Small MM", "Reduced multiplier applied after a loss streak", key)
			.SetRange(0m, 10m);

			_mm = strategy.Param(key + "_MM", defaultMm)
			.SetDisplay(key + " MM", "Default multiplier for new trades", key)
			.SetRange(0m, 10m);

			_openAllowed = strategy.Param(key + "_OpenAllowed", openAllowed)
			.SetDisplay(key + " Open Allowed", "Allow the module to open positions", key);

			_closeAllowed = strategy.Param(key + "_CloseAllowed", closeAllowed)
			.SetDisplay(key + " Close Allowed", "Allow the module to close positions", key);

			_period = strategy.Param(key + "_PeriodRSI", defaultPeriod)
			.SetDisplay(key + " RSI Period", "Length of the RSI driving ColorMETRO", key)
			.SetGreaterThanZero();

			_fastStep = strategy.Param(key + "_StepSizeFast", defaultFastStep)
			.SetDisplay(key + " Fast Step", "Fast ColorMETRO step size", key)
			.SetGreaterThanZero();

			_slowStep = strategy.Param(key + "_StepSizeSlow", defaultSlowStep)
			.SetDisplay(key + " Slow Step", "Slow ColorMETRO step size", key)
			.SetGreaterThanZero();

			_signalBar = strategy.Param(key + "_SignalBar", defaultSignalBar)
			.SetDisplay(key + " Signal Bar", "Historical shift for signal evaluation", key)
			.SetNotNegative();

			_magic = strategy.Param(key + "_Magic", defaultMagic)
			.SetDisplay(key + " Magic", "Original MT5 magic number (for reference)", key);

			_stopLossTicks = strategy.Param(key + "_StopLoss", defaultStopLoss)
			.SetDisplay(key + " Stop Loss", "Reserved stop-loss distance in ticks", key)
			.SetNotNegative();

			_takeProfitTicks = strategy.Param(key + "_TakeProfit", defaultTakeProfit)
			.SetDisplay(key + " Take Profit", "Reserved take-profit distance in ticks", key)
			.SetNotNegative();

			_deviationTicks = strategy.Param(key + "_Deviation", defaultDeviation)
			.SetDisplay(key + " Deviation", "Maximum allowed slippage", key)
			.SetNotNegative();

			_marginMode = strategy.Param(key + "_MMMode", defaultMarginMode)
			.SetDisplay(key + " MM Mode", "Money management mode (informational)", key);
		}

		public bool IsLong { get; }

		public string Key { get; }

		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		public int TotalTrigger
		{
			get => Math.Max(0, _totalTrigger.Value);
			set => _totalTrigger.Value = value;
		}

		public int LossTrigger
		{
			get => Math.Max(0, _lossTrigger.Value);
			set => _lossTrigger.Value = value;
		}

		public decimal SmallMm
		{
			get => Math.Abs(_smallMm.Value);
			set => _smallMm.Value = value;
		}

		public decimal Mm
		{
			get => Math.Abs(_mm.Value);
			set => _mm.Value = value;
		}

		public bool OpenAllowed
		{
			get => _openAllowed.Value;
			set => _openAllowed.Value = value;
		}

		public bool CloseAllowed
		{
			get => _closeAllowed.Value;
			set => _closeAllowed.Value = value;
		}

		public int Period
		{
			get => Math.Max(1, _period.Value);
			set => _period.Value = value;
		}

		public int StepSizeFast
		{
			get => Math.Max(1, _fastStep.Value);
			set => _fastStep.Value = value;
		}

		public int StepSizeSlow
		{
			get => Math.Max(1, _slowStep.Value);
			set => _slowStep.Value = value;
		}

		public int SignalBar
		{
			get => Math.Max(0, _signalBar.Value);
			set => _signalBar.Value = value;
		}

		public int Magic
		{
			get => _magic.Value;
			set => _magic.Value = value;
		}

		public decimal StopLossTicks
		{
			get => _stopLossTicks.Value;
			set => _stopLossTicks.Value = value;
		}

		public decimal TakeProfitTicks
		{
			get => _takeProfitTicks.Value;
			set => _takeProfitTicks.Value = value;
		}

		public decimal DeviationTicks
		{
			get => _deviationTicks.Value;
			set => _deviationTicks.Value = value;
		}

		public MarginMode MarginMode
		{
			get => _marginMode.Value;
			set => _marginMode.Value = value;
		}

		public ColorMetroMmrecIndicator Indicator { get; private set; }

		public void SetIndicator(ColorMetroMmrecIndicator indicator)
		{
			Indicator = indicator;
		}

		public void Reset()
		{
			_upHistory.Clear();
			_downHistory.Clear();
			_timeHistory.Clear();
			_recentLosses.Clear();
			_lossCount = 0;
			_entryPrice = null;

			Indicator?.Reset();
		}

		public void Add(ColorMetroMmrecValue value, DateTimeOffset closeTime)
		{
			if (_timeHistory.Count > 0 && _timeHistory[^1] == closeTime)
			{
				_upHistory[^1] = value.Up;
				_downHistory[^1] = value.Down;
				return;
			}

			_upHistory.Add(value.Up);
			_downHistory.Add(value.Down);
			_timeHistory.Add(closeTime);

			var maxSize = SignalBar + 5;
			if (_upHistory.Count > maxSize)
			{
				_upHistory.RemoveAt(0);
				_downHistory.RemoveAt(0);
				_timeHistory.RemoveAt(0);
			}
		}

		public bool TryCreateSnapshot(out Snapshot snapshot)
		{
			snapshot = default;

			var shiftIndex = _upHistory.Count - 1 - SignalBar;
			if (shiftIndex <= 0)
			return false;

			var previousIndex = shiftIndex - 1;
			if (previousIndex < 0)
			return false;

			snapshot = new Snapshot(
			upCurrent: _upHistory[shiftIndex],
			upPrevious: _upHistory[previousIndex],
			downCurrent: _downHistory[shiftIndex],
			downPrevious: _downHistory[previousIndex]);

			return true;
		}

		public bool ShouldOpen(Snapshot snapshot)
		{
			if (IsLong)
			return snapshot.UpPrevious > snapshot.DownPrevious && snapshot.UpCurrent <= snapshot.DownCurrent;

			return snapshot.UpPrevious < snapshot.DownPrevious && snapshot.UpCurrent >= snapshot.DownCurrent;
		}

		public bool ShouldClose(Snapshot snapshot)
		{
			if (IsLong)
			return snapshot.DownPrevious > snapshot.UpPrevious;

			return snapshot.DownPrevious < snapshot.UpPrevious;
		}

		public void RegisterEntry(decimal price)
		{
			_entryPrice = price;
		}

		public void FinalizeTrade(decimal exitPrice)
		{
			if (_entryPrice is not decimal entry)
			return;

			var isLoss = IsLong ? exitPrice < entry : exitPrice > entry;
			RecordResult(isLoss);
			_entryPrice = null;
		}

		public decimal CalculateVolume(decimal baseVolume)
		{
			var absoluteBase = Math.Abs(baseVolume);
			var multiplier = Mm;

			if (LossTrigger > 0 && _lossCount >= LossTrigger)
			multiplier = SmallMm;

			return Math.Max(0m, absoluteBase * multiplier);
		}

		private void RecordResult(bool isLoss)
		{
			if (TotalTrigger <= 0)
			{
				_recentLosses.Clear();
				_lossCount = isLoss ? 1 : 0;
				if (isLoss)
				_recentLosses.Enqueue(true);
				return;
			}

			_recentLosses.Enqueue(isLoss);
			if (isLoss)
			_lossCount++;

			while (_recentLosses.Count > TotalTrigger)
			{
				if (_recentLosses.Dequeue())
				_lossCount--;
			}
		}

		public readonly struct Snapshot
		{
			public Snapshot(decimal upCurrent, decimal upPrevious, decimal downCurrent, decimal downPrevious)
			{
				UpCurrent = upCurrent;
				UpPrevious = upPrevious;
				DownCurrent = downCurrent;
				DownPrevious = downPrevious;
			}

			public decimal UpCurrent { get; }

			public decimal UpPrevious { get; }

			public decimal DownCurrent { get; }

			public decimal DownPrevious { get; }
		}
	}
}

/// <summary>
/// Indicator value used by <see cref="ColorMetroMmrecIndicator"/>.
/// </summary>
public sealed class ColorMetroMmrecValue : ComplexIndicatorValue
{
	public ColorMetroMmrecValue(IIndicator indicator, IIndicatorValue input, decimal up, decimal down, decimal rsi, bool isReady)
	: base(indicator, input, (nameof(Up), up), (nameof(Down), down), (nameof(Rsi), rsi))
	{
		IsReady = isReady;
	}

	/// <summary>
	/// Fast ColorMETRO band value.
	/// </summary>
	public decimal Up => (decimal)GetValue(nameof(Up));

	/// <summary>
	/// Slow ColorMETRO band value.
	/// </summary>
	public decimal Down => (decimal)GetValue(nameof(Down));

	/// <summary>
	/// Internal RSI value driving the step envelopes.
	/// </summary>
	public decimal Rsi => (decimal)GetValue(nameof(Rsi));

	/// <summary>
	/// Indicates whether both bands are available for trading decisions.
	/// </summary>
	public bool IsReady { get; }
}

/// <summary>
/// ColorMETRO indicator replicated for the MMRec duplex strategy.
/// </summary>
public sealed class ColorMetroMmrecIndicator : BaseIndicator<ColorMetroMmrecValue>
{
	private readonly RelativeStrengthIndex _rsi = new();

	private decimal? _fastMin;
	private decimal? _fastMax;
	private decimal? _slowMin;
	private decimal? _slowMax;
	private int _fastTrend;
	private int _slowTrend;

	/// <summary>
	/// RSI period used inside the indicator.
	/// </summary>
	public int Length
	{
		get => _rsi.Length;
		set => _rsi.Length = Math.Max(1, value);
	}

	/// <summary>
	/// Step size for the fast ColorMETRO band.
	/// </summary>
	public int StepSizeFast { get; set; } = 5;

	/// <summary>
	/// Step size for the slow ColorMETRO band.
	/// </summary>
	public int StepSizeSlow { get; set; } = 15;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle)
		return new ColorMetroMmrecValue(this, input, default, default, default, false);

		if (candle.State != CandleStates.Finished)
		return new ColorMetroMmrecValue(this, input, default, default, default, false);

		var price = candle.ClosePrice;
		var rsiValue = _rsi.Process(new DecimalIndicatorValue(_rsi, price, input.Time));

		if (!_rsi.IsFormed)
		return new ColorMetroMmrecValue(this, input, default, default, default, false);

		var rsi = rsiValue.ToDecimal();
		var fastStep = Math.Max(1, StepSizeFast);
		var slowStep = Math.Max(1, StepSizeSlow);

		var fastMinCandidate = rsi - 2m * fastStep;
		var fastMaxCandidate = rsi + 2m * fastStep;
		var slowMinCandidate = rsi - 2m * slowStep;
		var slowMaxCandidate = rsi + 2m * slowStep;

		if (_fastMin is null || _fastMax is null || _slowMin is null || _slowMax is null)
		{
			_fastMin = fastMinCandidate;
			_fastMax = fastMaxCandidate;
			_slowMin = slowMinCandidate;
			_slowMax = slowMaxCandidate;
			_fastTrend = 0;
			_slowTrend = 0;
			return new ColorMetroMmrecValue(this, input, default, default, rsi, false);
		}

		var fastTrend = _fastTrend;

		if (rsi > _fastMax)
		fastTrend = 1;
		else if (rsi < _fastMin)
		fastTrend = -1;

		if (fastTrend > 0 && fastMinCandidate < _fastMin)
		fastMinCandidate = _fastMin.Value;
		else if (fastTrend < 0 && fastMaxCandidate > _fastMax)
		fastMaxCandidate = _fastMax.Value;

		var slowTrend = _slowTrend;

		if (rsi > _slowMax)
		slowTrend = 1;
		else if (rsi < _slowMin)
		slowTrend = -1;

		if (slowTrend > 0 && slowMinCandidate < _slowMin)
		slowMinCandidate = _slowMin.Value;
		else if (slowTrend < 0 && slowMaxCandidate > _slowMax)
		slowMaxCandidate = _slowMax.Value;

		decimal? up = null;
		if (fastTrend > 0)
		up = fastMinCandidate + fastStep;
		else if (fastTrend < 0)
		up = fastMaxCandidate - fastStep;

		decimal? down = null;
		if (slowTrend > 0)
		down = slowMinCandidate + slowStep;
		else if (slowTrend < 0)
		down = slowMaxCandidate - slowStep;

		_fastMin = fastMinCandidate;
		_fastMax = fastMaxCandidate;
		_slowMin = slowMinCandidate;
		_slowMax = slowMaxCandidate;
		_fastTrend = fastTrend;
		_slowTrend = slowTrend;

		var isReady = up.HasValue && down.HasValue;
		return new ColorMetroMmrecValue(this, input, up ?? 0m, down ?? 0m, rsi, isReady);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_rsi.Reset();
		_fastMin = null;
		_fastMax = null;
		_slowMin = null;
		_slowMax = null;
		_fastTrend = 0;
		_slowTrend = 0;
	}
}
