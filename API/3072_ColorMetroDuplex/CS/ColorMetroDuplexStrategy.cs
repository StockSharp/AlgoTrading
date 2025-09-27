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
/// Conversion of the MT5 expert "Exp_ColorMETRO_Duplex" that trades on two independent ColorMETRO modules.
/// </summary>
public class ColorMetroDuplexStrategy : Strategy
{
	private readonly SignalModule _longModule;
	private readonly SignalModule _shortModule;

	public ColorMetroDuplexStrategy()
	{
		_longModule = new SignalModule(
			strategy: this,
			key: "Long",
			isLong: true,
			defaultCandleType: TimeSpan.FromHours(4).TimeFrame(),
			defaultMagic: 777,
			defaultVolume: 0.1m,
			defaultStopLossTicks: 1000m,
			defaultTakeProfitTicks: 2000m,
			defaultDeviationTicks: 10m,
			defaultPeriod: 7,
			defaultFastStep: 5,
			defaultSlowStep: 15,
			defaultSignalBar: 1,
			openAllowed: true,
			closeAllowed: true);

		_shortModule = new SignalModule(
			strategy: this,
			key: "Short",
			isLong: false,
			defaultCandleType: TimeSpan.FromHours(4).TimeFrame(),
			defaultMagic: 555,
			defaultVolume: 0.1m,
			defaultStopLossTicks: 1000m,
			defaultTakeProfitTicks: 2000m,
			defaultDeviationTicks: 10m,
			defaultPeriod: 7,
			defaultFastStep: 5,
			defaultSlowStep: 15,
			defaultSignalBar: 1,
			openAllowed: true,
			closeAllowed: true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var seen = new HashSet<DataType>();

		foreach (var dataType in new[] { _longModule.CandleType, _shortModule.CandleType })
		{
			if (seen.Add(dataType))
				yield return (Security, dataType);
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

	private void StartModule(SignalModule module)
	{
		var indicator = new ColorMetroIndicator
		{
			Length = module.Period,
			StepSizeFast = module.StepSizeFast,
			StepSizeSlow = module.StepSizeSlow,
			PriceMode = module.PriceMode
		};

		var subscription = SubscribeCandles(module.CandleType);
		subscription
			.BindEx(indicator, (candle, indicatorValue) => ProcessModule(module, candle, indicatorValue))
			.Start();

		module.SetIndicator(indicator);
	}

	private void ProcessModule(SignalModule module, ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (indicatorValue is not ColorMetroValue metroValue)
			return;

		if (!metroValue.IsReady)
			return;

		module.Add(metroValue, candle.CloseTime);

		if (!module.TryCreateSnapshot(out var snapshot))
			return;

		var openSignal = module.ShouldOpen(snapshot);
		var closeSignal = module.ShouldClose(snapshot);

		if (module.IsLong)
		{
			if (closeSignal && module.CloseAllowed && Position > 0m)
			{
				SellMarket(Position);
			}

			if (openSignal && module.OpenAllowed && module.Volume > 0m && Position <= 0m)
			{
				var desiredVolume = module.Volume;

				if (Position < 0m)
				{
					BuyMarket(desiredVolume - Position);
				}
				else
				{
					BuyMarket(desiredVolume);
				}
			}
		}
		else
		{
			if (closeSignal && module.CloseAllowed && Position < 0m)
			{
				BuyMarket(-Position);
			}

			if (openSignal && module.OpenAllowed && module.Volume > 0m && Position >= 0m)
			{
				var desiredVolume = module.Volume;

				if (Position > 0m)
				{
					SellMarket(desiredVolume + Position);
				}
				else
				{
					SellMarket(desiredVolume);
				}
			}
		}
	}

	private sealed class SignalModule
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _period;
		private readonly StrategyParam<int> _fastStep;
		private readonly StrategyParam<int> _slowStep;
		private readonly StrategyParam<int> _signalBar;
		private readonly StrategyParam<decimal> _volume;
		private readonly StrategyParam<int> _magic;
		private readonly StrategyParam<decimal> _stopLossTicks;
		private readonly StrategyParam<decimal> _takeProfitTicks;
		private readonly StrategyParam<decimal> _deviationTicks;
		private readonly StrategyParam<bool> _openAllowed;
		private readonly StrategyParam<bool> _closeAllowed;
		private readonly StrategyParam<MarginMode> _marginMode;
		private readonly StrategyParam<MetroAppliedPrice> _priceMode;

		private readonly List<decimal> _upHistory = new();
		private readonly List<decimal> _downHistory = new();
		private readonly List<DateTimeOffset> _timeHistory = new();

		public SignalModule(
			ColorMetroDuplexStrategy strategy,
			string key,
			bool isLong,
			DataType defaultCandleType,
			int defaultMagic,
			decimal defaultVolume,
			decimal defaultStopLossTicks,
			decimal defaultTakeProfitTicks,
			decimal defaultDeviationTicks,
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
				.SetDisplay(key + " Candle Type", "Timeframe for the " + key.ToLowerInvariant() + " module", key);

			_period = strategy.Param(key + "_PeriodRSI", defaultPeriod)
				.SetDisplay(key + " RSI Period", "Length of the RSI used inside ColorMETRO", key)
				.SetGreaterThanZero();

			_fastStep = strategy.Param(key + "_StepSizeFast", defaultFastStep)
				.SetDisplay(key + " Fast Step", "Step size for the fast ColorMETRO band", key)
				.SetGreaterThanZero();

			_slowStep = strategy.Param(key + "_StepSizeSlow", defaultSlowStep)
				.SetDisplay(key + " Slow Step", "Step size for the slow ColorMETRO band", key)
				.SetGreaterThanZero();

			_signalBar = strategy.Param(key + "_SignalBar", defaultSignalBar)
				.SetDisplay(key + " Signal Bar", "Shift used when evaluating indicator buffers", key)
				.SetNotNegative();

			_volume = strategy.Param(key + "_Volume", defaultVolume)
				.SetDisplay(key + " Volume", "Position size for new entries", key);

			_magic = strategy.Param(key + "_Magic", defaultMagic)
				.SetDisplay(key + " Magic", "Original MT5 magic number (for reference only)", key);

			_stopLossTicks = strategy.Param(key + "_StopLoss", defaultStopLossTicks)
				.SetDisplay(key + " Stop Loss", "Reserved stop loss distance in price ticks", key)
				.SetNotNegative();

			_takeProfitTicks = strategy.Param(key + "_TakeProfit", defaultTakeProfitTicks)
				.SetDisplay(key + " Take Profit", "Reserved take profit distance in price ticks", key)
				.SetNotNegative();

			_deviationTicks = strategy.Param(key + "_Deviation", defaultDeviationTicks)
				.SetDisplay(key + " Deviation", "Maximum allowed slippage (compatibility parameter)", key)
				.SetNotNegative();

			_openAllowed = strategy.Param(key + "_OpenAllowed", openAllowed)
				.SetDisplay(key + " Open Allowed", "Allow this module to open positions", key);

			_closeAllowed = strategy.Param(key + "_CloseAllowed", closeAllowed)
				.SetDisplay(key + " Close Allowed", "Allow this module to close positions", key);

			_marginMode = strategy.Param(key + "_MarginMode", MarginMode.Lot)
				.SetDisplay(key + " Margin Mode", "Money management mode (not used in this port)", key);

			_priceMode = strategy.Param(key + "_AppliedPrice", MetroAppliedPrice.Close)
				.SetDisplay(key + " Price", "Price source for the ColorMETRO calculation", key);
		}

		public bool IsLong { get; }

		public string Key { get; }

		public DataType CandleType => _candleType.Value;

		public int Period => Math.Max(1, _period.Value);

		public int StepSizeFast => Math.Max(1, _fastStep.Value);

		public int StepSizeSlow => Math.Max(1, _slowStep.Value);

		public int SignalBar => Math.Max(0, _signalBar.Value);

		public decimal Volume => Math.Abs(_volume.Value);

		public bool OpenAllowed => _openAllowed.Value;

		public bool CloseAllowed => _closeAllowed.Value;

		public MarginMode MarginMode => _marginMode.Value;

		public MetroAppliedPrice PriceMode => _priceMode.Value;

		public decimal StopLossTicks => _stopLossTicks.Value;

		public decimal TakeProfitTicks => _takeProfitTicks.Value;

		public decimal DeviationTicks => _deviationTicks.Value;

		public int Magic => _magic.Value;

		public ColorMetroIndicator Indicator { get; private set; } = default!;

		public void SetIndicator(ColorMetroIndicator indicator)
		{
			Indicator = indicator;
		}

		public void Reset()
		{
			_upHistory.Clear();
			_downHistory.Clear();
			_timeHistory.Clear();

			Indicator?.Reset();
		}

		public void Add(ColorMetroValue value, DateTimeOffset closeTime)
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

		public bool TryCreateSnapshot(out SignalSnapshot snapshot)
		{
			snapshot = default;

			var shiftIndex = _upHistory.Count - 1 - SignalBar;
			if (shiftIndex <= 0)
				return false;

			var previousIndex = shiftIndex - 1;
			if (previousIndex < 0)
				return false;

			snapshot = new SignalSnapshot(
				upCurrent: _upHistory[shiftIndex],
				upPrevious: _upHistory[previousIndex],
				downCurrent: _downHistory[shiftIndex],
				downPrevious: _downHistory[previousIndex],
				signalTime: _timeHistory[shiftIndex]);

			return true;
		}

		public bool ShouldOpen(SignalSnapshot snapshot)
		{
			if (IsLong)
				return snapshot.UpPrevious > snapshot.DownPrevious && snapshot.UpCurrent <= snapshot.DownCurrent;

			return snapshot.UpPrevious < snapshot.DownPrevious && snapshot.UpCurrent >= snapshot.DownCurrent;
		}

		public bool ShouldClose(SignalSnapshot snapshot)
		{
			if (IsLong)
				return snapshot.DownPrevious > snapshot.UpPrevious;

			return snapshot.DownPrevious < snapshot.UpPrevious;
		}
	}

	private readonly struct SignalSnapshot
	{
		public SignalSnapshot(decimal upCurrent, decimal upPrevious, decimal downCurrent, decimal downPrevious, DateTimeOffset signalTime)
		{
			UpCurrent = upCurrent;
			UpPrevious = upPrevious;
			DownCurrent = downCurrent;
			DownPrevious = downPrevious;
			SignalTime = signalTime;
		}

		public decimal UpCurrent { get; }

		public decimal UpPrevious { get; }

		public decimal DownCurrent { get; }

		public decimal DownPrevious { get; }

		public DateTimeOffset SignalTime { get; }
	}

	/// <summary>
	/// Money management modes replicated from the MT5 expert for compatibility.
	/// </summary>
	public enum MarginMode
	{
		FreeMargin = 0,
		Balance = 1,
		LossFreeMargin = 2,
		LossBalance = 3,
		Lot = 4
	}

	/// <summary>
	/// Price options supported by the ColorMETRO indicator.
	/// </summary>
	public enum MetroAppliedPrice
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted
	}
}

/// <summary>
/// Indicator output for the ColorMETRO calculation.
/// </summary>
public sealed class ColorMetroValue : ComplexIndicatorValue
{
	public ColorMetroValue(IIndicator indicator, IIndicatorValue input, decimal up, decimal down, decimal rsi, bool isReady)
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
	/// RSI value that drives the step envelopes.
	/// </summary>
	public decimal Rsi => (decimal)GetValue(nameof(Rsi));

	/// <summary>
	/// Indicates whether both bands are available for signal evaluation.
	/// </summary>
	public bool IsReady { get; }
}

/// <summary>
/// ColorMETRO indicator recreated from the original MT5 source code.
/// </summary>
public sealed class ColorMetroIndicator : BaseIndicator<ColorMetroValue>
{
	private readonly RelativeStrengthIndex _rsi = new();

	private decimal? _fastMin;
	private decimal? _fastMax;
	private decimal? _slowMin;
	private decimal? _slowMax;
	private int _fastTrend;
	private int _slowTrend;

	/// <summary>
	/// RSI length used inside the indicator.
	/// </summary>
	public int Length
	{
		get => _rsi.Length;
		set => _rsi.Length = Math.Max(1, value);
	}

	/// <summary>
	/// Step size for the fast envelope.
	/// </summary>
	public int StepSizeFast { get; set; } = 5;

	/// <summary>
	/// Step size for the slow envelope.
	/// </summary>
	public int StepSizeSlow { get; set; } = 15;

	/// <summary>
	/// Price selection mode for RSI input.
	/// </summary>
	public ColorMetroDuplexStrategy.MetroAppliedPrice PriceMode { get; set; } = ColorMetroDuplexStrategy.MetroAppliedPrice.Close;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle)
			return new ColorMetroValue(this, input, default, default, default, false);

		if (candle.State != CandleStates.Finished)
			return new ColorMetroValue(this, input, default, default, default, false);

		var price = SelectPrice(candle, PriceMode);
		var rsiValue = _rsi.Process(new DecimalIndicatorValue(_rsi, price, input.Time));

		if (!_rsi.IsFormed)
			return new ColorMetroValue(this, input, default, default, default, false);

		var rsi = rsiValue.ToDecimal();
		var fastStep = StepSizeFast;
		var slowStep = StepSizeSlow;

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
			return new ColorMetroValue(this, input, default, default, rsi, false);
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
		return new ColorMetroValue(this, input, up ?? 0m, down ?? 0m, rsi, isReady);
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

	private static decimal SelectPrice(ICandleMessage candle, ColorMetroDuplexStrategy.MetroAppliedPrice priceMode)
	{
		return priceMode switch
		{
			ColorMetroDuplexStrategy.MetroAppliedPrice.Open => candle.OpenPrice,
			ColorMetroDuplexStrategy.MetroAppliedPrice.High => candle.HighPrice,
			ColorMetroDuplexStrategy.MetroAppliedPrice.Low => candle.LowPrice,
			ColorMetroDuplexStrategy.MetroAppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			ColorMetroDuplexStrategy.MetroAppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			ColorMetroDuplexStrategy.MetroAppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}
}

