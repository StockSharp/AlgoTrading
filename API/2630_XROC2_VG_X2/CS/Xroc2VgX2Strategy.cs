using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe XROC2 VG strategy that combines two smoothed rate-of-change streams.
/// The higher timeframe defines the directional bias while the lower timeframe handles entries and exits.
/// </summary>
public class Xroc2VgX2Strategy : Strategy
{
	/// <summary>
	/// Available rate-of-change calculation modes.
	/// </summary>
	public enum RocMode
	{
		Momentum,
		RateOfChange,
		RateOfChangePercent,
		RateOfChangeRatio,
		RateOfChangeRatioPercent,
	}

	/// <summary>
	/// Smoothing methods supported by the strategy.
	/// </summary>
	public enum SmoothingMethod
	{
		Sma,
		Ema,
		Smma,
		Lwma,
		Jurik,
		Jurx,
		Parma,
		T3,
		Vidya,
		Ama,
	}

	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<DataType> _lowerCandleType;
	private readonly StrategyParam<int> _higherSignalBar;
	private readonly StrategyParam<int> _lowerSignalBar;
	private readonly StrategyParam<RocMode> _higherRocMode;
	private readonly StrategyParam<int> _higherFastPeriod;
	private readonly StrategyParam<SmoothingMethod> _higherFastMethod;
	private readonly StrategyParam<int> _higherFastLength;
	private readonly StrategyParam<int> _higherFastPhase;
	private readonly StrategyParam<int> _higherSlowPeriod;
	private readonly StrategyParam<SmoothingMethod> _higherSlowMethod;
	private readonly StrategyParam<int> _higherSlowLength;
	private readonly StrategyParam<int> _higherSlowPhase;
	private readonly StrategyParam<RocMode> _lowerRocMode;
	private readonly StrategyParam<int> _lowerFastPeriod;
	private readonly StrategyParam<SmoothingMethod> _lowerFastMethod;
	private readonly StrategyParam<int> _lowerFastLength;
	private readonly StrategyParam<int> _lowerFastPhase;
	private readonly StrategyParam<int> _lowerSlowPeriod;
	private readonly StrategyParam<SmoothingMethod> _lowerSlowMethod;
	private readonly StrategyParam<int> _lowerSlowLength;
	private readonly StrategyParam<int> _lowerSlowPhase;
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _closeBuyOnTrendFlip;
	private readonly StrategyParam<bool> _closeSellOnTrendFlip;
	private readonly StrategyParam<bool> _closeBuyOnLower;
	private readonly StrategyParam<bool> _closeSellOnLower;

	private Xroc2VgSeries _higherSeries = default!;
	private Xroc2VgSeries _lowerSeries = default!;
	private int _trend;

	/// <summary>
	/// Initializes a new instance of the <see cref="Xroc2VgX2Strategy"/> class.
	/// </summary>
	public Xroc2VgX2Strategy()
	{
		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Higher TF", "Higher timeframe candles", "General");

		_lowerCandleType = Param(nameof(LowerCandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Lower TF", "Lower timeframe candles", "General");

		_higherSignalBar = Param(nameof(HigherSignalBar), 1)
			.SetGreaterThanZero()
			.SetDisplay("Higher Signal Bar", "Shift used for trend evaluation", "General");

		_lowerSignalBar = Param(nameof(LowerSignalBar), 1)
			.SetGreaterThanZero()
			.SetDisplay("Lower Signal Bar", "Shift used for lower timeframe signals", "General");

		_higherRocMode = Param(nameof(HigherRocMode), RocMode.Momentum)
			.SetDisplay("Higher ROC Mode", "ROC calculation mode for the bias", "Higher Timeframe");

		_higherFastPeriod = Param(nameof(HigherFastPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Higher Fast ROC", "Fast ROC period for bias", "Higher Timeframe");

		_higherFastMethod = Param(nameof(HigherFastMethod), SmoothingMethod.Jurik)
			.SetDisplay("Higher Fast Method", "Smoother for fast ROC", "Higher Timeframe");

		_higherFastLength = Param(nameof(HigherFastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Higher Fast Length", "Length of fast smoother", "Higher Timeframe");

		_higherFastPhase = Param(nameof(HigherFastPhase), 15)
			.SetDisplay("Higher Fast Phase", "Phase parameter for fast smoother", "Higher Timeframe");

		_higherSlowPeriod = Param(nameof(HigherSlowPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Higher Slow ROC", "Slow ROC period for bias", "Higher Timeframe");

		_higherSlowMethod = Param(nameof(HigherSlowMethod), SmoothingMethod.Jurik)
			.SetDisplay("Higher Slow Method", "Smoother for slow ROC", "Higher Timeframe");

		_higherSlowLength = Param(nameof(HigherSlowLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Higher Slow Length", "Length of slow smoother", "Higher Timeframe");

		_higherSlowPhase = Param(nameof(HigherSlowPhase), 15)
			.SetDisplay("Higher Slow Phase", "Phase parameter for slow smoother", "Higher Timeframe");

		_lowerRocMode = Param(nameof(LowerRocMode), RocMode.Momentum)
			.SetDisplay("Lower ROC Mode", "ROC calculation mode for entries", "Lower Timeframe");

		_lowerFastPeriod = Param(nameof(LowerFastPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Lower Fast ROC", "Fast ROC period for entries", "Lower Timeframe");

		_lowerFastMethod = Param(nameof(LowerFastMethod), SmoothingMethod.Jurik)
			.SetDisplay("Lower Fast Method", "Smoother for fast ROC", "Lower Timeframe");

		_lowerFastLength = Param(nameof(LowerFastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lower Fast Length", "Length of fast smoother", "Lower Timeframe");

		_lowerFastPhase = Param(nameof(LowerFastPhase), 15)
			.SetDisplay("Lower Fast Phase", "Phase parameter for fast smoother", "Lower Timeframe");

		_lowerSlowPeriod = Param(nameof(LowerSlowPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Lower Slow ROC", "Slow ROC period for entries", "Lower Timeframe");

		_lowerSlowMethod = Param(nameof(LowerSlowMethod), SmoothingMethod.Jurik)
			.SetDisplay("Lower Slow Method", "Smoother for slow ROC", "Lower Timeframe");

		_lowerSlowLength = Param(nameof(LowerSlowLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lower Slow Length", "Length of slow smoother", "Lower Timeframe");

		_lowerSlowPhase = Param(nameof(LowerSlowPhase), 15)
			.SetDisplay("Lower Slow Phase", "Phase parameter for slow smoother", "Lower Timeframe");

		_allowBuyOpen = Param(nameof(AllowBuyOpen), true)
			.SetDisplay("Allow Long Entries", "Enable long entries", "Signals");

		_allowSellOpen = Param(nameof(AllowSellOpen), true)
			.SetDisplay("Allow Short Entries", "Enable short entries", "Signals");

		_closeBuyOnTrendFlip = Param(nameof(CloseBuyOnTrendFlip), true)
			.SetDisplay("Close Long On Trend", "Close longs when higher trend turns bearish", "Signals");

		_closeSellOnTrendFlip = Param(nameof(CloseSellOnTrendFlip), true)
			.SetDisplay("Close Short On Trend", "Close shorts when higher trend turns bullish", "Signals");

		_closeBuyOnLower = Param(nameof(CloseBuyOnLower), true)
			.SetDisplay("Close Long On Lower", "Close longs when lower ROC crosses down", "Signals");

		_closeSellOnLower = Param(nameof(CloseSellOnLower), true)
			.SetDisplay("Close Short On Lower", "Close shorts when lower ROC crosses up", "Signals");
	}

	/// <summary>
	/// Higher timeframe candle type.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Lower timeframe candle type.
	/// </summary>
	public DataType LowerCandleType
	{
		get => _lowerCandleType.Value;
		set => _lowerCandleType.Value = value;
	}

	/// <summary>
	/// Number of bars to shift when reading higher timeframe values.
	/// </summary>
	public int HigherSignalBar
	{
		get => _higherSignalBar.Value;
		set => _higherSignalBar.Value = value;
	}

	/// <summary>
	/// Number of bars to shift when reading lower timeframe values.
	/// </summary>
	public int LowerSignalBar
	{
		get => _lowerSignalBar.Value;
		set => _lowerSignalBar.Value = value;
	}

	/// <summary>
	/// Rate-of-change mode for the higher timeframe stream.
	/// </summary>
	public RocMode HigherRocMode
	{
		get => _higherRocMode.Value;
		set => _higherRocMode.Value = value;
	}

	/// <summary>
	/// Fast ROC period for the higher timeframe.
	/// </summary>
	public int HigherFastPeriod
	{
		get => _higherFastPeriod.Value;
		set => _higherFastPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing method for the higher timeframe fast line.
	/// </summary>
	public SmoothingMethod HigherFastMethod
	{
		get => _higherFastMethod.Value;
		set => _higherFastMethod.Value = value;
	}

	/// <summary>
	/// Smoothing length for the higher timeframe fast line.
	/// </summary>
	public int HigherFastLength
	{
		get => _higherFastLength.Value;
		set => _higherFastLength.Value = value;
	}

	/// <summary>
	/// Phase parameter for the higher timeframe fast smoother.
	/// </summary>
	public int HigherFastPhase
	{
		get => _higherFastPhase.Value;
		set => _higherFastPhase.Value = value;
	}

	/// <summary>
	/// Slow ROC period for the higher timeframe.
	/// </summary>
	public int HigherSlowPeriod
	{
		get => _higherSlowPeriod.Value;
		set => _higherSlowPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing method for the higher timeframe slow line.
	/// </summary>
	public SmoothingMethod HigherSlowMethod
	{
		get => _higherSlowMethod.Value;
		set => _higherSlowMethod.Value = value;
	}

	/// <summary>
	/// Smoothing length for the higher timeframe slow line.
	/// </summary>
	public int HigherSlowLength
	{
		get => _higherSlowLength.Value;
		set => _higherSlowLength.Value = value;
	}

	/// <summary>
	/// Phase parameter for the higher timeframe slow smoother.
	/// </summary>
	public int HigherSlowPhase
	{
		get => _higherSlowPhase.Value;
		set => _higherSlowPhase.Value = value;
	}

	/// <summary>
	/// Rate-of-change mode for the lower timeframe stream.
	/// </summary>
	public RocMode LowerRocMode
	{
		get => _lowerRocMode.Value;
		set => _lowerRocMode.Value = value;
	}

	/// <summary>
	/// Fast ROC period for the lower timeframe.
	/// </summary>
	public int LowerFastPeriod
	{
		get => _lowerFastPeriod.Value;
		set => _lowerFastPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing method for the lower timeframe fast line.
	/// </summary>
	public SmoothingMethod LowerFastMethod
	{
		get => _lowerFastMethod.Value;
		set => _lowerFastMethod.Value = value;
	}

	/// <summary>
	/// Smoothing length for the lower timeframe fast line.
	/// </summary>
	public int LowerFastLength
	{
		get => _lowerFastLength.Value;
		set => _lowerFastLength.Value = value;
	}

	/// <summary>
	/// Phase parameter for the lower timeframe fast smoother.
	/// </summary>
	public int LowerFastPhase
	{
		get => _lowerFastPhase.Value;
		set => _lowerFastPhase.Value = value;
	}

	/// <summary>
	/// Slow ROC period for the lower timeframe.
	/// </summary>
	public int LowerSlowPeriod
	{
		get => _lowerSlowPeriod.Value;
		set => _lowerSlowPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing method for the lower timeframe slow line.
	/// </summary>
	public SmoothingMethod LowerSlowMethod
	{
		get => _lowerSlowMethod.Value;
		set => _lowerSlowMethod.Value = value;
	}

	/// <summary>
	/// Smoothing length for the lower timeframe slow line.
	/// </summary>
	public int LowerSlowLength
	{
		get => _lowerSlowLength.Value;
		set => _lowerSlowLength.Value = value;
	}

	/// <summary>
	/// Phase parameter for the lower timeframe slow smoother.
	/// </summary>
	public int LowerSlowPhase
	{
		get => _lowerSlowPhase.Value;
		set => _lowerSlowPhase.Value = value;
	}

	/// <summary>
	/// Allow long entries when signals align.
	/// </summary>
	public bool AllowBuyOpen
	{
		get => _allowBuyOpen.Value;
		set => _allowBuyOpen.Value = value;
	}

	/// <summary>
	/// Allow short entries when signals align.
	/// </summary>
	public bool AllowSellOpen
	{
		get => _allowSellOpen.Value;
		set => _allowSellOpen.Value = value;
	}

	/// <summary>
	/// Close long positions when the higher timeframe turns bearish.
	/// </summary>
	public bool CloseBuyOnTrendFlip
	{
		get => _closeBuyOnTrendFlip.Value;
		set => _closeBuyOnTrendFlip.Value = value;
	}

	/// <summary>
	/// Close short positions when the higher timeframe turns bullish.
	/// </summary>
	public bool CloseSellOnTrendFlip
	{
		get => _closeSellOnTrendFlip.Value;
		set => _closeSellOnTrendFlip.Value = value;
	}

	/// <summary>
	/// Close long positions when the lower timeframe shows a bearish cross.
	/// </summary>
	public bool CloseBuyOnLower
	{
		get => _closeBuyOnLower.Value;
		set => _closeBuyOnLower.Value = value;
	}

	/// <summary>
	/// Close short positions when the lower timeframe shows a bullish cross.
	/// </summary>
	public bool CloseSellOnLower
	{
		get => _closeSellOnLower.Value;
		set => _closeSellOnLower.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, HigherCandleType);
		yield return (Security, LowerCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_higherSeries = null!;
		_lowerSeries = null!;
		_trend = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_higherSeries = new Xroc2VgSeries(
			HigherRocMode,
			HigherFastPeriod,
			HigherFastMethod,
			HigherFastLength,
			HigherFastPhase,
			HigherSlowPeriod,
			HigherSlowMethod,
			HigherSlowLength,
			HigherSlowPhase);

		_lowerSeries = new Xroc2VgSeries(
			LowerRocMode,
			LowerFastPeriod,
			LowerFastMethod,
			LowerFastLength,
			LowerFastPhase,
			LowerSlowPeriod,
			LowerSlowMethod,
			LowerSlowLength,
			LowerSlowPhase);

		_trend = 0;

		var higherSubscription = SubscribeCandles(HigherCandleType);
		higherSubscription.Bind(ProcessHigherCandle).Start();

		var lowerSubscription = SubscribeCandles(LowerCandleType);
		lowerSubscription.Bind(ProcessLowerCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, lowerSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessHigherCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_higherSeries.Process(candle))
			return;

		if (_higherSeries.TryGetValue(HigherSignalBar, out var value))
			_trend = value.up > value.down ? 1 : value.up < value.down ? -1 : 0;
	}

	private void ProcessLowerCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_lowerSeries.Process(candle))
			return;

		if (!_lowerSeries.TryGetPair(LowerSignalBar, out var current, out var previous))
			return;

		if (_trend == 0)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var buyClose = CloseBuyOnLower && previous.up < previous.down;
		var sellClose = CloseSellOnLower && previous.up > previous.down;

		if (_trend < 0 && CloseBuyOnTrendFlip)
			buyClose = true;

		if (_trend > 0 && CloseSellOnTrendFlip)
			sellClose = true;

		var buyOpen = _trend > 0 && AllowBuyOpen && current.up <= current.down && previous.up > previous.down;
		var sellOpen = _trend < 0 && AllowSellOpen && current.up >= current.down && previous.up < previous.down;

		ExecuteSignals(buyOpen, sellOpen, buyClose, sellClose);
	}

	private void ExecuteSignals(bool buyOpen, bool sellOpen, bool buyClose, bool sellClose)
	{
		var position = Position;

		if (buyClose && position > 0m)
		{
			var volume = position.Abs();
			if (volume > 0m)
				SellMarket(volume);

			position = Position;
		}

		if (sellClose && position < 0m)
		{
			var volume = position.Abs();
			if (volume > 0m)
				BuyMarket(volume);

			position = Position;
		}

		if (buyOpen && position == 0m)
		{
			var volume = Volume;
			if (volume > 0m)
				BuyMarket(volume);

			return;
		}

		if (sellOpen && position == 0m)
		{
			var volume = Volume;
			if (volume > 0m)
				SellMarket(volume);
		}
	}

	private sealed class Xroc2VgSeries
	{
		private readonly RocSmoother _fast;
		private readonly RocSmoother _slow;
		private readonly List<(decimal up, decimal down)> _history = new();
		private readonly int _maxHistory;

		public Xroc2VgSeries(
			RocMode mode,
			int fastPeriod,
			SmoothingMethod fastMethod,
			int fastLength,
			int fastPhase,
			int slowPeriod,
			SmoothingMethod slowMethod,
			int slowLength,
			int slowPhase,
			int maxHistory = 1024)
		{
			_fast = new RocSmoother(mode, fastPeriod, fastMethod, fastLength, fastPhase);
			_slow = new RocSmoother(mode, slowPeriod, slowMethod, slowLength, slowPhase);
			_maxHistory = maxHistory;
		}

		public bool Process(ICandleMessage candle)
		{
			var fast = _fast.Process(candle.ClosePrice, candle.OpenTime);
			var slow = _slow.Process(candle.ClosePrice, candle.OpenTime);

			if (!fast.HasValue || !slow.HasValue)
				return false;

			_history.Add((fast.Value, slow.Value));

			if (_history.Count > _maxHistory)
				_history.RemoveRange(0, _history.Count - _maxHistory);

			return true;
		}

		public bool TryGetValue(int signalBar, out (decimal up, decimal down) value)
		{
			value = default;

			if (signalBar <= 0)
				return false;

			var index = _history.Count - signalBar;
			if (index < 0 || index >= _history.Count)
				return false;

			value = _history[index];
			return true;
		}

		public bool TryGetPair(int signalBar, out (decimal up, decimal down) current, out (decimal up, decimal down) previous)
		{
			current = default;
			previous = default;

			if (signalBar <= 0)
				return false;

			var index = _history.Count - signalBar;
			if (index < 1 || index >= _history.Count)
				return false;

			current = _history[index];
			previous = _history[index - 1];
			return true;
		}
	}

	private sealed class RocSmoother
	{
		private readonly RocMode _mode;
		private readonly int _period;
		private readonly IIndicator _smoother;
		private readonly Queue<decimal> _window = new();

		public RocSmoother(RocMode mode, int period, SmoothingMethod method, int length, int phase)
		{
			_mode = mode;
			_period = Math.Max(1, period);
			_smoother = CreateSmoother(method, length, phase);
		}

		public decimal? Process(decimal close, DateTimeOffset time)
		{
			_window.Enqueue(close);

			if (_window.Count < _period + 1)
				return null;

			if (_window.Count > _period + 1)
				_window.Dequeue();

			var prev = _window.Peek();

			decimal roc;
			switch (_mode)
			{
				case RocMode.Momentum:
					roc = close - prev;
					break;
				case RocMode.RateOfChange:
					if (prev == 0m)
						return null;
					roc = (close / prev - 1m) * 100m;
					break;
				case RocMode.RateOfChangePercent:
					if (prev == 0m)
						return null;
					roc = (close - prev) / prev;
					break;
				case RocMode.RateOfChangeRatio:
					if (prev == 0m)
						return null;
					roc = close / prev;
					break;
				case RocMode.RateOfChangeRatioPercent:
					if (prev == 0m)
						return null;
					roc = (close / prev) * 100m;
					break;
				default:
					roc = close - prev;
					break;
			}

			var indicatorValue = _smoother.Process(new DecimalIndicatorValue(_smoother, roc, time));

			return indicatorValue switch
			{
				DecimalIndicatorValue { IsFinal: true } decimalValue => decimalValue.Value,
				{ IsFinal: true } value => value.GetValue<decimal?>(),
				_ => null,
			};
		}
	}

	private static IIndicator CreateSmoother(SmoothingMethod method, int length, int phase)
	{
		var len = Math.Max(1, length);

		return method switch
		{
			SmoothingMethod.Sma => new SimpleMovingAverage { Length = len },
			SmoothingMethod.Ema => new ExponentialMovingAverage { Length = len },
			SmoothingMethod.Smma => new SmoothedMovingAverage { Length = len },
			SmoothingMethod.Lwma => new WeightedMovingAverage { Length = len },
			SmoothingMethod.Jurik => new JurikMovingAverage { Length = len },
			SmoothingMethod.Jurx => new JurikMovingAverage { Length = len },
			SmoothingMethod.Ama => new KaufmanAdaptiveMovingAverage { Length = len },
			_ => new ExponentialMovingAverage { Length = len },
		};
	}
}

