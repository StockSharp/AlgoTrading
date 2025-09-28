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
/// Gann fan strategy combining linear weighted moving averages,
/// momentum confirmation, MACD direction filter, and fractal-based
/// fan orientation similar to the original MetaTrader expert.
/// Includes risk management with fixed stops, trailing, and
/// break-even logic expressed in price steps.
/// </summary>
public class GannFanStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _fractalHistory;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailTriggerPips;
	private readonly StrategyParam<decimal> _trailDistancePips;
	private readonly StrategyParam<decimal> _trailPadPips;
	private readonly StrategyParam<bool> _useCandleTrail;
	private readonly StrategyParam<int> _trailingCandles;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<bool> _useGannFilter;
	private readonly StrategyParam<bool> _forceExit;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private readonly Queue<decimal> _closeBuffer = new();
	private readonly Queue<decimal> _momentumDeviationHistory = new();
	private readonly Queue<decimal> _recentHighs = new();
	private readonly Queue<decimal> _recentLows = new();
	private readonly List<FractalPoint> _downFractals = new();
	private readonly List<FractalPoint> _upFractals = new();

	private decimal? _fastValue;
	private decimal? _slowValue;
	private decimal? _macdMain;
	private decimal? _macdSignal;
	private bool _macdReady;

	private int _bufferCount;
	private decimal _h0;
	private decimal _h1;
	private decimal _h2;
	private decimal _h3;
	private decimal _h4;
	private decimal _l0;
	private decimal _l1;
	private decimal _l2;
	private decimal _l3;
	private decimal _l4;
	private DateTimeOffset _t0;
	private DateTimeOffset _t1;
	private DateTimeOffset _t2;
	private DateTimeOffset _t3;
	private DateTimeOffset _t4;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private decimal? _longBreakEvenPrice;
	private decimal? _shortBreakEvenPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="GannFanStrategy"/>.
	/// </summary>
	public GannFanStrategy()
	{
		Param(nameof(Volume), 0.10m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Base order volume", "Trading");

		_fastMaLength = Param(nameof(FastMaLength), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast LWMA", "Fast linear weighted moving average length", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow LWMA", "Slow linear weighted moving average length", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(40, 120, 5);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Period", "Lookback for momentum calculation", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(10, 25, 1);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Threshold", "Minimum deviation from 100% momentum", "Momentum");

		_macdFast = Param(nameof(MacdFast), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length for MACD", "MACD");

		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length for MACD", "MACD");

		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA length for MACD", "MACD");

		_fractalHistory = Param(nameof(FractalHistory), 10)
		.SetGreaterThanZero()
		.SetDisplay("Fractal History", "Number of confirmed fractals kept for fan orientation", "Fractals");

		_maxTrades = Param(nameof(MaxTrades), 10)
		.SetGreaterThanZero()
		.SetDisplay("Max Trades", "Maximum number of stacked entries per direction", "Trading");

		_lotExponent = Param(nameof(LotExponent), 1.44m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Exponent", "Multiplier for each additional entry", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Stop-loss distance in price steps", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Take-profit distance in price steps", "Risk");

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Enable Trailing", "Enable trailing stop management", "Risk");

		_trailTriggerPips = Param(nameof(TrailTriggerPips), 40m)
		.SetGreaterThanZero()
		.SetDisplay("Trail Trigger", "Profit in price steps before trailing activates", "Risk");

		_trailDistancePips = Param(nameof(TrailDistancePips), 40m)
		.SetGreaterThanZero()
		.SetDisplay("Trail Distance", "Distance of the trailing stop in price steps", "Risk");

		_trailPadPips = Param(nameof(TrailPadPips), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Trail Padding", "Additional padding applied to candle trailing", "Risk");

		_useCandleTrail = Param(nameof(UseCandleTrail), true)
		.SetDisplay("Use Candle Trail", "Use recent candle lows/highs for trailing", "Risk");

		_trailingCandles = Param(nameof(TrailingCandles), 3)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Candles", "Number of recent candles considered for trailing", "Risk");

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
		.SetDisplay("Enable Break-even", "Move stop to break-even after profit trigger", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Break-even Trigger", "Profit in price steps before break-even activates", "Risk");

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Break-even Offset", "Offset added to break-even stop in price steps", "Risk");

		_useGannFilter = Param(nameof(UseGannFilter), true)
		.SetDisplay("Use Gann Filter", "Require bullish/bearish fan orientation for entries", "Filters");

		_forceExit = Param(nameof(ForceExit), false)
		.SetDisplay("Force Exit", "Close positions immediately when enabled", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series", "Data");
	}

	/// <summary>
	/// Fast LWMA length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow LWMA length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Momentum calculation period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum deviation of momentum from neutrality.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal EMA period for MACD.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Number of confirmed fractal points stored for fan orientation.
	/// </summary>
	public int FractalHistory
	{
		get => _fractalHistory.Value;
		set => _fractalHistory.Value = value;
	}

	/// <summary>
	/// Maximum number of stacked entries per direction.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Exponent multiplier for additional entries.
	/// </summary>
	public decimal LotExponent
	{
		get => _lotExponent.Value;
		set => _lotExponent.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enable trailing stop logic.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Profit distance before trailing activates.
	/// </summary>
	public decimal TrailTriggerPips
	{
		get => _trailTriggerPips.Value;
		set => _trailTriggerPips.Value = value;
	}

	/// <summary>
	/// Distance of the trailing stop.
	/// </summary>
	public decimal TrailDistancePips
	{
		get => _trailDistancePips.Value;
		set => _trailDistancePips.Value = value;
	}

	/// <summary>
	/// Additional padding applied when trailing by candles.
	/// </summary>
	public decimal TrailPadPips
	{
		get => _trailPadPips.Value;
		set => _trailPadPips.Value = value;
	}

	/// <summary>
	/// Use recent candles to anchor the trailing stop.
	/// </summary>
	public bool UseCandleTrail
	{
		get => _useCandleTrail.Value;
		set => _useCandleTrail.Value = value;
	}

	/// <summary>
	/// Number of recent candles evaluated for candle-based trailing.
	/// </summary>
	public int TrailingCandles
	{
		get => _trailingCandles.Value;
		set => _trailingCandles.Value = value;
	}

	/// <summary>
	/// Enable break-even logic.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Profit trigger in price steps for the break-even move.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Offset applied to the break-even stop in price steps.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Enable or disable Gann fan orientation filter.
	/// </summary>
	public bool UseGannFilter
	{
		get => _useGannFilter.Value;
		set => _useGannFilter.Value = value;
	}

	/// <summary>
	/// Force immediate exit from any open positions.
	/// </summary>
	public bool ForceExit
	{
		get => _forceExit.Value;
		set => _forceExit.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_closeBuffer.Clear();
		_momentumDeviationHistory.Clear();
		_recentHighs.Clear();
		_recentLows.Clear();
		_downFractals.Clear();
		_upFractals.Clear();
		_fastValue = null;
		_slowValue = null;
		_macdMain = null;
		_macdSignal = null;
		_macdReady = false;
		_bufferCount = 0;
		_h0 = _h1 = _h2 = _h3 = _h4 = 0m;
		_l0 = _l1 = _l2 = _l3 = _l4 = 0m;
		_t0 = _t1 = _t2 = _t3 = _t4 = default;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_longBreakEvenPrice = null;
		_shortBreakEvenPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new WeightedMovingAverage { Length = FastMaLength };
		_slowMa = new WeightedMovingAverage { Length = SlowMaLength };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow }
			},
			SignalMa = { Length = MacdSignal }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		var isFinal = candle.State == CandleStates.Finished;

		ProcessIndicators(candle, isFinal);

		if (!isFinal)
		return;

		UpdateBuffers(candle);
		UpdateMomentum(candle);
		UpdateRecentExtremes(candle);

		if (ForceExit)
		{
			CloseAllPositions();
			return;
		}

		ManageOpenPosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!IsSignalReady())
		return;

		TryEnter(candle);
	}

	private void ProcessIndicators(ICandleMessage candle, bool isFinal)
	{
		var typical = GetTypicalPrice(candle);

		var fastValue = _fastMa.Process(typical, candle.OpenTime, isFinal);
		if (fastValue.IsFinal)
		_fastValue = fastValue.ToDecimal();

		var slowValue = _slowMa.Process(typical, candle.OpenTime, isFinal);
		if (slowValue.IsFinal)
		_slowValue = slowValue.ToDecimal();

		var macdValue = _macd.Process(candle.ClosePrice, candle.OpenTime, isFinal);
		if (macdValue.IsFinal && macdValue is MovingAverageConvergenceDivergenceSignalValue typed)
		{
			_macdMain = typed.Macd;
			_macdSignal = typed.Signal;
			_macdReady = true;
		}
	}

	private void UpdateBuffers(ICandleMessage candle)
	{
		_h4 = _h3;
		_h3 = _h2;
		_h2 = _h1;
		_h1 = _h0;
		_h0 = candle.HighPrice;

		_l4 = _l3;
		_l3 = _l2;
		_l2 = _l1;
		_l1 = _l0;
		_l0 = candle.LowPrice;

		_t4 = _t3;
		_t3 = _t2;
		_t2 = _t1;
		_t1 = _t0;
		_t0 = candle.OpenTime;

		if (_bufferCount < 5)
		{
			_bufferCount++;
			return;
		}

		if (IsUpFractal())
		RegisterUpFractal(new FractalPoint(_t2, _h2));

		if (IsDownFractal())
		RegisterDownFractal(new FractalPoint(_t2, _l2));
	}

	private void UpdateMomentum(ICandleMessage candle)
	{
		var period = MomentumPeriod;
		if (period <= 0)
		return;

		_closeBuffer.Enqueue(candle.ClosePrice);
		while (_closeBuffer.Count > period + 1)
		_closeBuffer.Dequeue();

		if (_closeBuffer.Count < period + 1)
		return;

		var oldest = _closeBuffer.Peek();
		if (oldest == 0m)
		return;

		var momentum = 100m * candle.ClosePrice / oldest;
		var deviation = Math.Abs(100m - momentum);

		_momentumDeviationHistory.Enqueue(deviation);
		while (_momentumDeviationHistory.Count > 3)
		_momentumDeviationHistory.Dequeue();
	}

	private void UpdateRecentExtremes(ICandleMessage candle)
	{
		_recentHighs.Enqueue(candle.HighPrice);
		while (_recentHighs.Count > Math.Max(1, TrailingCandles))
		_recentHighs.Dequeue();

		_recentLows.Enqueue(candle.LowPrice);
		while (_recentLows.Count > Math.Max(1, TrailingCandles))
		_recentLows.Dequeue();
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			ManageLongPosition(candle);
		}
		else if (Position < 0m)
		{
			ManageShortPosition(candle);
		}
		else
		{
			ResetPositionState();
		}
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		var entryPrice = _longEntryPrice ?? candle.ClosePrice;
		var priceStep = GetPriceStep();
		if (priceStep <= 0m)
		return;

		var volume = Position;

		if (TakeProfitPips > 0m)
		{
			var target = entryPrice + priceStep * TakeProfitPips;
			if (candle.HighPrice >= target)
			{
				SellMarket(volume);
				ResetLongState();
				return;
			}
		}

		if (StopLossPips > 0m)
		{
			var stop = entryPrice - priceStep * StopLossPips;
			if (candle.LowPrice <= stop)
			{
				SellMarket(volume);
				ResetLongState();
				return;
			}
		}

		if (EnableBreakEven && BreakEvenTriggerPips > 0m && !_longBreakEvenPrice.HasValue)
		{
			var trigger = entryPrice + priceStep * BreakEvenTriggerPips;
			if (candle.HighPrice >= trigger)
			_longBreakEvenPrice = entryPrice + priceStep * BreakEvenOffsetPips;
		}

		if (_longBreakEvenPrice.HasValue && candle.LowPrice <= _longBreakEvenPrice.Value)
		{
			SellMarket(volume);
			ResetLongState();
			return;
		}

		if (EnableTrailing)
		{
			decimal? candidate = null;

			if (TrailDistancePips > 0m)
			{
				var trigger = priceStep * TrailTriggerPips;
				var distance = priceStep * TrailDistancePips;
				if (distance > 0m && candle.ClosePrice - entryPrice >= trigger)
				candidate = candle.ClosePrice - distance;
			}

			if (UseCandleTrail && _recentLows.Count > 0)
			{
				var lowest = decimal.MaxValue;
				foreach (var low in _recentLows)
				lowest = Math.Min(lowest, low);

				var candleBased = lowest - priceStep * TrailPadPips;
				candidate = candidate.HasValue ? Math.Max(candidate.Value, candleBased) : candleBased;
			}

			if (candidate.HasValue && (!_longTrailingStop.HasValue || candidate.Value > _longTrailingStop.Value))
			_longTrailingStop = candidate.Value;
		}

		if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
		{
			SellMarket(volume);
			ResetLongState();
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		var entryPrice = _shortEntryPrice ?? candle.ClosePrice;
		var priceStep = GetPriceStep();
		if (priceStep <= 0m)
		return;

		var volume = Math.Abs(Position);

		if (TakeProfitPips > 0m)
		{
			var target = entryPrice - priceStep * TakeProfitPips;
			if (candle.LowPrice <= target)
			{
				BuyMarket(volume);
				ResetShortState();
				return;
			}
		}

		if (StopLossPips > 0m)
		{
			var stop = entryPrice + priceStep * StopLossPips;
			if (candle.HighPrice >= stop)
			{
				BuyMarket(volume);
				ResetShortState();
				return;
			}
		}

		if (EnableBreakEven && BreakEvenTriggerPips > 0m && !_shortBreakEvenPrice.HasValue)
		{
			var trigger = entryPrice - priceStep * BreakEvenTriggerPips;
			if (candle.LowPrice <= trigger)
			_shortBreakEvenPrice = entryPrice - priceStep * BreakEvenOffsetPips;
		}

		if (_shortBreakEvenPrice.HasValue && candle.HighPrice >= _shortBreakEvenPrice.Value)
		{
			BuyMarket(volume);
			ResetShortState();
			return;
		}

		if (EnableTrailing)
		{
			decimal? candidate = null;

			if (TrailDistancePips > 0m)
			{
				var trigger = priceStep * TrailTriggerPips;
				var distance = priceStep * TrailDistancePips;
				if (distance > 0m && entryPrice - candle.ClosePrice >= trigger)
				candidate = candle.ClosePrice + distance;
			}

			if (UseCandleTrail && _recentHighs.Count > 0)
			{
				var highest = decimal.MinValue;
				foreach (var high in _recentHighs)
				highest = Math.Max(highest, high);

				var candleBased = highest + priceStep * TrailPadPips;
				candidate = candidate.HasValue ? Math.Min(candidate.Value, candleBased) : candleBased;
			}

			if (candidate.HasValue && (!_shortTrailingStop.HasValue || candidate.Value < _shortTrailingStop.Value))
			_shortTrailingStop = candidate.Value;
		}

		if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
		{
			BuyMarket(volume);
			ResetShortState();
		}
	}

	private void TryEnter(ICandleMessage candle)
	{
		var fast = _fastValue;
		var slow = _slowValue;
		if (fast is null || slow is null)
		return;

		if (ShouldEnterLong(fast.Value, slow.Value))
		{
			if (Position < 0m)
			{
				BuyMarket(-Position);
				ResetShortState();
				return;
			}

			var volume = GetNextVolume();
			if (volume > 0m)
			{
				BuyMarket(volume);
				RegisterLongEntry(candle.ClosePrice);
			}
			return;
		}

		if (ShouldEnterShort(fast.Value, slow.Value))
		{
			if (Position > 0m)
			{
				SellMarket(Position);
				ResetLongState();
				return;
			}

			var volume = GetNextVolume();
			if (volume > 0m)
			{
				SellMarket(volume);
				RegisterShortEntry(candle.ClosePrice);
			}
		}
	}

	private bool ShouldEnterLong(decimal fast, decimal slow)
	{
		if (fast <= slow)
		return false;

		if (!HasMomentumSignal())
		return false;

		if (!_macdReady || _macdMain is null || _macdSignal is null || _macdMain <= _macdSignal)
		return false;

		if (UseGannFilter && !HasBullishFan())
		return false;

		if (Volume <= 0m)
		return false;

		var steps = GetCurrentSteps();
		return steps < MaxTrades;
	}

	private bool ShouldEnterShort(decimal fast, decimal slow)
	{
		if (fast >= slow)
		return false;

		if (!HasMomentumSignal())
		return false;

		if (!_macdReady || _macdMain is null || _macdSignal is null || _macdMain >= _macdSignal)
		return false;

		if (UseGannFilter && !HasBearishFan())
		return false;

		if (Volume <= 0m)
		return false;

		var steps = GetCurrentSteps();
		return steps < MaxTrades;
	}

	private bool HasMomentumSignal()
	{
		if (_momentumDeviationHistory.Count < 3)
		return false;

		foreach (var value in _momentumDeviationHistory)
		{
			if (value >= MomentumThreshold)
			return true;
		}

		return false;
	}

	private bool HasBullishFan()
	{
		if (_downFractals.Count < 2)
		return false;

		var older = _downFractals[^2];
		var recent = _downFractals[^1];
		return recent.Time > older.Time && older.Price < recent.Price;
	}

	private bool HasBearishFan()
	{
		if (_upFractals.Count < 2)
		return false;

		var older = _upFractals[^2];
		var recent = _upFractals[^1];
		return recent.Time > older.Time && older.Price > recent.Price;
	}

	private decimal GetNextVolume()
	{
		var baseVolume = Volume;
		if (baseVolume <= 0m)
		return 0m;

		var steps = GetCurrentSteps();
		if (steps >= MaxTrades)
		return 0m;

		var multiplier = (decimal)Math.Pow((double)LotExponent, steps);
		return baseVolume * multiplier;
	}

	private int GetCurrentSteps()
	{
		var baseVolume = Volume;
		if (baseVolume <= 0m)
		return int.MaxValue;

		var positionVolume = Math.Abs(Position);
		if (positionVolume <= 0m)
		return 0;

		return (int)Math.Round((double)(positionVolume / baseVolume), MidpointRounding.AwayFromZero);
	}

	private void RegisterLongEntry(decimal price)
	{
		_longEntryPrice = price;
		_longTrailingStop = null;
		_longBreakEvenPrice = null;
	}

	private void RegisterShortEntry(decimal price)
	{
		_shortEntryPrice = price;
		_shortTrailingStop = null;
		_shortBreakEvenPrice = null;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longTrailingStop = null;
		_longBreakEvenPrice = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortTrailingStop = null;
		_shortBreakEvenPrice = null;
	}

	private void ResetPositionState()
	{
		ResetLongState();
		ResetShortState();
	}

	private void CloseAllPositions()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
			ResetLongState();
		}
		else if (Position < 0m)
		{
			BuyMarket(-Position);
			ResetShortState();
		}
	}

	private bool IsSignalReady()
	{
		return _fastValue.HasValue && _slowValue.HasValue && _macdReady;
	}

	private bool IsUpFractal()
	{
		return _h2 >= _h3 && _h2 > _h4 && _h2 >= _h1 && _h2 > _h0;
	}

	private bool IsDownFractal()
	{
		return _l2 <= _l3 && _l2 < _l4 && _l2 <= _l1 && _l2 < _l0;
	}

	private void RegisterUpFractal(FractalPoint point)
	{
		_upFractals.Add(point);
		TrimFractals(_upFractals);
	}

	private void RegisterDownFractal(FractalPoint point)
	{
		_downFractals.Add(point);
		TrimFractals(_downFractals);
	}

	private void TrimFractals(List<FractalPoint> storage)
	{
		var limit = Math.Max(2, FractalHistory);
		while (storage.Count > limit)
		storage.RemoveAt(0);
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		return step <= 0m ? 0m : step;
	}

	private static decimal GetTypicalPrice(ICandleMessage candle)
	{
		return (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
	}

	private readonly struct FractalPoint
	{
		public FractalPoint(DateTimeOffset time, decimal price)
		{
			Time = time;
			Price = price;
		}

		public DateTimeOffset Time { get; }
		public decimal Price { get; }
	}
}

