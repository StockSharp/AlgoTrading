using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that mirrors the original "V1N1 LONNY" MQL expert advisor.
/// The strategy watches the London/New York overlap, forms an opening range, and
/// enters when a candle closes outside that range while trend and momentum filters agree.
/// </summary>
public class V1n1LonnyBreakoutStrategy : Strategy
{
	/// <summary>
	/// Modes for daylight-saving adjustments.
	/// </summary>
	public enum DstSwitchMode
	{
		/// <summary>
		/// Follow European daylight-saving transitions (no extra shift).
		/// </summary>
		Europe,
		/// <summary>
		/// Align London and New York daylight-saving transitions.
		/// </summary>
		Usa,
		/// <summary>
		/// Disable daylight-saving compensation.
		/// </summary>
		None
	}

	/// <summary>
	/// Position-sizing modes.
	/// </summary>
	public enum RiskMode
	{
		/// <summary>
		/// Calculate volume from a percentage of account equity.
		/// </summary>
		Percent,
		/// <summary>
		/// Use a fixed order volume.
		/// </summary>
		FixedVolume
	}

	private readonly StrategyParam<TimeSpan> _startTrade;
	private readonly StrategyParam<TimeSpan> _endTrade;
	private readonly StrategyParam<DstSwitchMode> _switchDst;
	private readonly StrategyParam<RiskMode> _riskMode;
	private readonly StrategyParam<decimal> _positionRisk;
	private readonly StrategyParam<int> _tradeRange;
	private readonly StrategyParam<decimal> _minRangePoints;
	private readonly StrategyParam<decimal> _maxRangePoints;
	private readonly StrategyParam<decimal> _minBreakRange;
	private readonly StrategyParam<decimal> _maxBreakRange;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _tpFactor;
	private readonly StrategyParam<decimal> _trailStopPoints;
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<int> _overPeriod;
	private readonly StrategyParam<int> _overLevels;
	private readonly StrategyParam<int> _barsToClose;
	private readonly StrategyParam<decimal> _maxSpreadPoints;
	private readonly StrategyParam<decimal> _slippagePoints;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema = null!;
	private StochasticOscillator _stochastic = null!;

	private decimal? _emaPrevious;
	private decimal? _emaTwoBack;
	private decimal? _stochasticPrevious;

	private DateTime? _sessionDate;
	private DateTimeOffset _sessionStart;
	private DateTimeOffset _sessionEnd;
	private bool _sessionInitialized;

	private decimal[]? _preSessionHighs;
	private decimal[]? _preSessionLows;
	private int _preSessionIndex;
	private int _preSessionCount;

	private bool _rangeReady;
	private decimal _rangeHigh;
	private decimal _rangeLow;

	private bool _breakoutUpSeen;
	private bool _breakoutDownSeen;

	private decimal? _bestBid;
	private decimal? _bestAsk;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private int _barsSinceEntry;

	private decimal _minRangeDistance;
	private decimal _maxRangeDistance;
	private decimal _minBreakDistance;
	private decimal _maxBreakDistance;
	private decimal _stopDistance;
	private decimal _trailDistance;
	private decimal _maxSpreadDistance;

	/// <summary>
	/// Initializes a new instance of the <see cref="V1n1LonnyBreakoutStrategy"/> class.
	/// </summary>
	public V1n1LonnyBreakoutStrategy()
	{
		_startTrade = Param(nameof(StartTrade), TimeSpan.FromHours(10))
		.SetDisplay("Start Time", "Session start time", "Trading Hours");

		_endTrade = Param(nameof(EndTrade), TimeSpan.FromHours(22))
		.SetDisplay("End Time", "Session end time", "Trading Hours");

		_switchDst = Param(nameof(SwitchDst), DstSwitchMode.Europe)
		.SetDisplay("DST Mode", "Daylight-saving adjustment mode", "Trading Hours");

		_riskMode = Param(nameof(PositionRiskMode), RiskMode.Percent)
		.SetDisplay("Risk Mode", "Position sizing mode", "Risk Management");

		_positionRisk = Param(nameof(PositionRisk), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Value", "Risk percent or fixed volume", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 3m, 0.5m);

		_tradeRange = Param(nameof(TradeRange), 2)
		.SetGreaterThanZero()
		.SetDisplay("Range Bars", "Bars used to build the opening range", "Breakout")
		.SetCanOptimize(true)
		.SetOptimize(2, 6, 1);

		_minRangePoints = Param(nameof(MinRangePoints), 0m)
		.SetGreaterOrEquals(0m)
		.SetDisplay("Min Range", "Minimum opening range size (points)", "Breakout");

		_maxRangePoints = Param(nameof(MaxRangePoints), 1450m)
		.SetGreaterThanZero()
		.SetDisplay("Max Range", "Maximum opening range size (points)", "Breakout");

		_minBreakRange = Param(nameof(MinBreakRange), 15m)
		.SetGreaterThanZero()
		.SetDisplay("Min Break", "Minimum breakout distance (points)", "Breakout");

		_maxBreakRange = Param(nameof(MaxBreakRange), 460m)
		.SetGreaterThanZero()
		.SetDisplay("Max Break", "Maximum breakout distance (points)", "Breakout");

		_stopLossPoints = Param(nameof(StopLossPoints), 750m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Stop distance from range (points)", "Risk Management");

		_tpFactor = Param(nameof(TpFactor), 1.2m)
		.SetGreaterThanZero()
		.SetDisplay("TP Factor", "Take-profit multiplier of stop distance", "Risk Management");

		_trailStopPoints = Param(nameof(TrailStopPoints), 800m)
		.SetGreaterOrEquals(0m)
		.SetDisplay("Trailing", "Trailing stop distance (points)", "Risk Management");

		_trendPeriod = Param(nameof(TrendPeriod), 248)
		.SetGreaterThanZero()
		.SetDisplay("Trend EMA", "EMA period for trend filter", "Indicators");

		_overPeriod = Param(nameof(OverPeriod), 56)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Period", "Main stochastic period", "Indicators");

		_overLevels = Param(nameof(OverLevels), 25)
		.SetGreaterOrEquals(0)
		.SetLessOrEquals(25)
		.SetDisplay("Stochastic Offset", "Allowed distance from 50", "Indicators");

		_barsToClose = Param(nameof(BarsToClose), 120)
		.SetGreaterOrEquals(0)
		.SetDisplay("Max Bars", "Maximum bars to keep a trade open", "Risk Management");

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 100m)
		.SetGreaterOrEquals(0m)
		.SetDisplay("Max Spread", "Maximum allowed spread (points)", "Risk Management");

		_slippagePoints = Param(nameof(SlippagePoints), 20m)
		.SetGreaterOrEquals(0m)
		.SetDisplay("Slippage", "Reference slippage (points)", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Candle type and timeframe", "General");
	}

	/// <summary>
	/// Trading session start time.
	/// </summary>
	public TimeSpan StartTrade
	{
		get => _startTrade.Value;
		set => _startTrade.Value = value;
	}

	/// <summary>
	/// Trading session end time.
	/// </summary>
	public TimeSpan EndTrade
	{
		get => _endTrade.Value;
		set => _endTrade.Value = value;
	}

	/// <summary>
	/// Daylight-saving adjustment mode.
	/// </summary>
	public DstSwitchMode SwitchDst
	{
		get => _switchDst.Value;
		set => _switchDst.Value = value;
	}

	/// <summary>
	/// Position-sizing mode.
	/// </summary>
	public RiskMode PositionRiskMode
	{
		get => _riskMode.Value;
		set => _riskMode.Value = value;
	}

	/// <summary>
	/// Risk percentage or fixed volume value.
	/// </summary>
	public decimal PositionRisk
	{
		get => _positionRisk.Value;
		set => _positionRisk.Value = value;
	}

	/// <summary>
	/// Number of bars used to form the opening range.
	/// </summary>
	public int TradeRange
	{
		get => _tradeRange.Value;
		set => _tradeRange.Value = value;
	}

	/// <summary>
	/// Minimum allowed opening range size in points.
	/// </summary>
	public decimal MinRangePoints
	{
		get => _minRangePoints.Value;
		set => _minRangePoints.Value = value;
	}

	/// <summary>
	/// Maximum allowed opening range size in points.
	/// </summary>
	public decimal MaxRangePoints
	{
		get => _maxRangePoints.Value;
		set => _maxRangePoints.Value = value;
	}

	/// <summary>
	/// Minimum breakout distance in points.
	/// </summary>
	public decimal MinBreakRange
	{
		get => _minBreakRange.Value;
		set => _minBreakRange.Value = value;
	}

	/// <summary>
	/// Maximum breakout distance in points.
	/// </summary>
	public decimal MaxBreakRange
	{
		get => _maxBreakRange.Value;
		set => _maxBreakRange.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points from the range.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit multiplier.
	/// </summary>
	public decimal TpFactor
	{
		get => _tpFactor.Value;
		set => _tpFactor.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance in points.
	/// </summary>
	public decimal TrailStopPoints
	{
		get => _trailStopPoints.Value;
		set => _trailStopPoints.Value = value;
	}

	/// <summary>
	/// EMA period for the trend filter.
	/// </summary>
	public int TrendPeriod
	{
		get => _trendPeriod.Value;
		set => _trendPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator period.
	/// </summary>
	public int OverPeriod
	{
		get => _overPeriod.Value;
		set => _overPeriod.Value = value;
	}

	/// <summary>
	/// Allowed offset of the stochastic value from the 50 midpoint.
	/// </summary>
	public int OverLevels
	{
		get => _overLevels.Value;
		set => _overLevels.Value = value;
	}

	/// <summary>
	/// Maximum bars to keep a position open.
	/// </summary>
	public int BarsToClose
	{
		get => _barsToClose.Value;
		set => _barsToClose.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread in points.
	/// </summary>
	public decimal MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Reference slippage value in points.
	/// </summary>
	public decimal SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_emaPrevious = null;
		_emaTwoBack = null;
		_stochasticPrevious = null;

		_sessionDate = null;
		_sessionInitialized = false;
		_sessionStart = DateTimeOffset.MinValue;
		_sessionEnd = DateTimeOffset.MinValue;

		_preSessionHighs = null;
		_preSessionLows = null;
		_preSessionIndex = 0;
		_preSessionCount = 0;

		_rangeReady = false;
		_rangeHigh = 0m;
		_rangeLow = 0m;

		_breakoutUpSeen = false;
		_breakoutDownSeen = false;

		_bestBid = null;
		_bestAsk = null;

		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_barsSinceEntry = 0;

		_minRangeDistance = 0m;
		_maxRangeDistance = 0m;
		_minBreakDistance = 0m;
		_maxBreakDistance = 0m;
		_stopDistance = 0m;
		_trailDistance = 0m;
		_maxSpreadDistance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators that mirror the MQL implementation.
		_ema = new ExponentialMovingAverage { Length = TrendPeriod };

		// MQL version uses smoothed %K/%D with 60% of the main length rounded to the nearest integer.
		var smoothing = Math.Max(1, (int)Math.Round(OverPeriod * 0.6m, MidpointRounding.AwayFromZero));
		_stochastic = new StochasticOscillator
		{
			Length = OverPeriod,
			K = { Length = smoothing },
			D = { Length = smoothing }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_ema, ProcessCandle)
		.Start();

		SubscribeOrderBook()
		.Bind(depth =>
		{
			var bestBid = depth.GetBestBid();
			if (bestBid != null)
			_bestBid = bestBid.Price;

			var bestAsk = depth.GetBestAsk();
			if (bestAsk != null)
			_bestAsk = bestAsk.Price;
		})
		.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawIndicator(priceArea, _ema);

			var oscillatorArea = CreateChartArea();
			if (oscillatorArea != null)
			DrawIndicator(oscillatorArea, _stochastic);

			DrawOwnTrades(priceArea);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateSessionTimes(candle.OpenTime);
		UpdateDistances();

		// Process the finished candle through the stochastic oscillator to replicate shift(1) behaviour.
		var stochValue = _stochastic.Process(candle);
		if (!stochValue.IsFinal)
		{
			UpdateIndicatorHistory(emaValue, null);
			return;
		}

		var stochTyped = (StochasticOscillatorValue)stochValue;
		if (stochTyped.K is not decimal stochCurrent)
		{
			UpdateIndicatorHistory(emaValue, null);
			return;
		}

		var emaPrev = _emaPrevious;
		var emaPrevPrev = _emaTwoBack;
		var stochPrev = _stochasticPrevious;

		if (candle.OpenTime < _sessionStart)
		{
			// Collect bars before the trading session to build the opening range.
			AddPreSessionCandle(candle);
		}
		else if (!_rangeReady)
		{
			TryBuildRange();
		}

		if (emaPrev.HasValue && emaPrevPrev.HasValue && stochPrev.HasValue)
		// Manage existing trades before checking for new entries.
		ManageOpenPosition(candle, emaPrev.Value, emaPrevPrev.Value, stochPrev.Value);

		// Only evaluate entries while the time is inside the configured session.
		var insideSession = candle.OpenTime >= _sessionStart && candle.OpenTime < _sessionEnd;
		if (insideSession && _rangeReady && Position == 0 && emaPrev.HasValue && emaPrevPrev.HasValue && stochPrev.HasValue && IsFormedAndOnlineAndAllowTrading())
		{
			var rangeSize = _rangeHigh - _rangeLow;
			var minOk = MinRangePoints <= 0m || rangeSize >= _minRangeDistance;
			var maxOk = rangeSize <= _maxRangeDistance;

			if (minOk && maxOk)
			{
				// Attempt a long breakout first; if conditions fail fall back to the short setup.
				if (!TryOpenLong(candle, emaPrev.Value, emaPrevPrev.Value, stochPrev.Value))
				TryOpenShort(candle, emaPrev.Value, emaPrevPrev.Value, stochPrev.Value);
			}
		}

		if (_rangeReady)
		{
			if (candle.ClosePrice >= _rangeHigh + _minBreakDistance)
			_breakoutUpSeen = true;

			if (candle.ClosePrice <= _rangeLow - _minBreakDistance)
			_breakoutDownSeen = true;
		}

		UpdateIndicatorHistory(emaValue, stochCurrent);
	}

	private void UpdateIndicatorHistory(decimal emaValue, decimal? stochCurrent)
	{
		_emaTwoBack = _emaPrevious;
		_emaPrevious = emaValue;

		if (stochCurrent.HasValue)
		_stochasticPrevious = stochCurrent.Value;
	}

	private void UpdateSessionTimes(DateTimeOffset time)
	{
		var day = time.Date;
		if (_sessionInitialized && _sessionDate == day)
		return;

		_sessionInitialized = true;
		_sessionDate = day;

		var baseDate = new DateTimeOffset(day, time.Offset);
		var dstShift = GetDstShift(time);

		_sessionStart = baseDate + StartTrade + dstShift;
		_sessionEnd = baseDate + EndTrade + dstShift;

		ResetRangeBuffers();
	}

	private void ResetRangeBuffers()
	{
		var size = Math.Max(TradeRange, 1);

		if (_preSessionHighs == null || _preSessionHighs.Length != size)
		{
			_preSessionHighs = new decimal[size];
			_preSessionLows = new decimal[size];
		}

		Array.Clear(_preSessionHighs!, 0, _preSessionHighs!.Length);
		Array.Clear(_preSessionLows!, 0, _preSessionLows!.Length);

		_preSessionIndex = 0;
		_preSessionCount = 0;

		_rangeReady = false;
		_rangeHigh = 0m;
		_rangeLow = 0m;

		_breakoutUpSeen = false;
		_breakoutDownSeen = false;
	}

	private void AddPreSessionCandle(ICandleMessage candle)
	{
		var size = Math.Max(TradeRange, 1);
		if (_preSessionHighs == null || _preSessionHighs.Length != size)
		{
			_preSessionHighs = new decimal[size];
			_preSessionLows = new decimal[size];
			_preSessionIndex = 0;
			_preSessionCount = 0;
		}

		_preSessionHighs![_preSessionIndex] = candle.HighPrice;
		_preSessionLows![_preSessionIndex] = candle.LowPrice;

		if (_preSessionCount < size)
		_preSessionCount++;

		_preSessionIndex++;
		if (_preSessionIndex >= size)
		_preSessionIndex = 0;
	}

	private void TryBuildRange()
	{
		var size = TradeRange;
		if (!_sessionInitialized || size <= 0)
		return;

		if (_preSessionCount < size)
		return;

		if (_preSessionHighs == null || _preSessionLows == null)
		return;

		var high = _preSessionHighs[0];
		var low = _preSessionLows[0];

		for (var i = 1; i < size; i++)
		{
			var h = _preSessionHighs[i];
			if (h > high)
			high = h;

			var l = _preSessionLows[i];
			if (l < low)
			low = l;
		}

		_rangeHigh = high;
		_rangeLow = low;
		_rangeReady = true;
	}

	private void ManageOpenPosition(ICandleMessage candle, decimal emaPrev, decimal emaPrevPrev, decimal stochPrev)
	{
		if (Position > 0)
		{
			// Track how many bars elapsed since the long entry.
			_barsSinceEntry++;

			var exitByTrend = emaPrev < emaPrevPrev;
			var exitByStochastic = stochPrev > 80m;
			var exitByBars = BarsToClose > 0 && _barsSinceEntry >= BarsToClose;

			if (exitByTrend || exitByStochastic || exitByBars)
			{
				SellMarket(Position);
				ResetLongState();
				return;
			}

			if (_trailDistance > 0m && _longEntryPrice.HasValue)
			{
				// Raise the trailing stop only when the trade moves further into profit.
				var candidate = candle.ClosePrice - _trailDistance;
				if (_longEntryPrice.Value <= candidate && (!_longStop.HasValue || candidate > _longStop.Value))
				_longStop = candidate;
			}

			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Position);
				ResetLongState();
				return;
			}

			if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				SellMarket(Position);
				ResetLongState();
			}
		}
		else if (Position < 0)
		{
			// Track how many bars elapsed since the short entry.
			_barsSinceEntry++;

			var exitByTrend = emaPrev > emaPrevPrev;
			var exitByStochastic = stochPrev < 20m;
			var exitByBars = BarsToClose > 0 && _barsSinceEntry >= BarsToClose;

			if (exitByTrend || exitByStochastic || exitByBars)
			{
				BuyMarket(-Position);
				ResetShortState();
				return;
			}

			if (_trailDistance > 0m && _shortEntryPrice.HasValue)
			{
				// Lower the trailing stop for short trades when price moves favourably.
				var candidate = candle.ClosePrice + _trailDistance;
				if (_shortEntryPrice.Value >= candidate && (!_shortStop.HasValue || candidate < _shortStop.Value))
				_shortStop = candidate;
			}

			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(-Position);
				ResetShortState();
				return;
			}

			if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				BuyMarket(-Position);
				ResetShortState();
			}
		}
		else
		{
			_barsSinceEntry = 0;
		}
	}

	private bool TryOpenLong(ICandleMessage candle, decimal emaPrev, decimal emaPrevPrev, decimal stochPrev)
	{
		if (emaPrev <= emaPrevPrev)
		return false;

		var threshold = 50m + OverLevels;
		// Stochastic must be below the upper threshold to avoid overbought entries.
		if (stochPrev >= threshold)
		return false;

		if (candle.OpenPrice > _rangeHigh)
		return false;

		var minBreakPrice = _rangeHigh + _minBreakDistance;
		var maxBreakPrice = _rangeHigh + _maxBreakDistance;

		if (candle.ClosePrice < minBreakPrice || candle.ClosePrice > maxBreakPrice)
		return false;

		if (_breakoutUpSeen)
		return false;

		if (!IsSpreadAcceptable())
		return false;

		// Use current ask if available; otherwise fall back to the candle close.
		var entryPrice = _bestAsk ?? candle.ClosePrice;
		var stopPrice = _rangeLow - _stopDistance;
		var takePrice = entryPrice + (entryPrice - stopPrice) * TpFactor;

		if (stopPrice >= entryPrice)
		return false;

		var volume = CalculateVolume(entryPrice, stopPrice);
		if (volume <= 0m)
		return false;

		BuyMarket(volume);

		_longEntryPrice = entryPrice;
		_longStop = stopPrice;
		_longTake = takePrice;
		_barsSinceEntry = 0;
		_breakoutUpSeen = true;

		return true;
	}

	private bool TryOpenShort(ICandleMessage candle, decimal emaPrev, decimal emaPrevPrev, decimal stochPrev)
	{
		if (emaPrev >= emaPrevPrev)
		return false;

		var threshold = 50m - OverLevels;
		// Stochastic must be above the lower threshold to avoid oversold entries.
		if (stochPrev <= threshold)
		return false;

		if (candle.OpenPrice < _rangeLow)
		return false;

		var minBreakPrice = _rangeLow - _minBreakDistance;
		var maxBreakPrice = _rangeLow - _maxBreakDistance;

		if (candle.ClosePrice > minBreakPrice || candle.ClosePrice < maxBreakPrice)
		return false;

		if (_breakoutDownSeen)
		return false;

		if (!IsSpreadAcceptable())
		return false;

		// Use current bid if available; otherwise fall back to the candle close.
		var entryPrice = _bestBid ?? candle.ClosePrice;
		var stopPrice = _rangeHigh + _stopDistance;
		var takePrice = entryPrice - (stopPrice - entryPrice) * TpFactor;

		if (stopPrice <= entryPrice)
		return false;

		var volume = CalculateVolume(entryPrice, stopPrice);
		if (volume <= 0m)
		return false;

		SellMarket(volume);

		_shortEntryPrice = entryPrice;
		_shortStop = stopPrice;
		_shortTake = takePrice;
		_barsSinceEntry = 0;
		_breakoutDownSeen = true;

		return true;
	}

	private void ResetLongState()
	{
		_longStop = null;
		_longTake = null;
		_longEntryPrice = null;
		_barsSinceEntry = 0;
	}

	private void ResetShortState()
	{
		_shortStop = null;
		_shortTake = null;
		_shortEntryPrice = null;
		_barsSinceEntry = 0;
	}

	private bool IsSpreadAcceptable()
	{
		if (MaxSpreadPoints <= 0m)
		return true;

		if (!_bestBid.HasValue || !_bestAsk.HasValue)
		return true;

		var spread = _bestAsk.Value - _bestBid.Value;
		return spread <= _maxSpreadDistance;
	}

	private decimal CalculateVolume(decimal entryPrice, decimal stopPrice)
	{
		if (Security == null)
		return Volume;

		// Align position size with the instrument constraints.
		var volumeStep = Security.VolumeStep ?? 1m;
		if (volumeStep <= 0m)
		volumeStep = 1m;

		var minVolume = Security.MinVolume ?? volumeStep;
		var maxVolume = Security.MaxVolume ?? decimal.MaxValue;

		decimal rawVolume;

		if (PositionRiskMode == RiskMode.FixedVolume)
		{
			rawVolume = PositionRisk;
		}
		else
		{
			// Convert the risk percentage to account currency using the stop distance.
			var equity = Portfolio?.CurrentValue ?? 0m;
			if (equity <= 0m)
			return minVolume;

			var riskValue = equity * PositionRisk / 100m;
			var distance = Math.Abs(entryPrice - stopPrice);

			if (distance <= 0m)
			return minVolume;

			var priceStep = Security.PriceStep ?? 0m;
			if (priceStep <= 0m)
			priceStep = 0.0001m;

			var stepPrice = Security.StepPrice ?? priceStep;
			if (stepPrice <= 0m)
			stepPrice = priceStep;

			// Translate the stop distance into monetary risk per contract.
			var ticks = distance / priceStep;
			if (ticks <= 0m)
			return minVolume;

			var riskPerContract = ticks * stepPrice;
			if (riskPerContract <= 0m)
			return minVolume;

			rawVolume = riskValue / riskPerContract;
		}

		if (rawVolume <= 0m)
		rawVolume = minVolume;

		var steps = Math.Floor(rawVolume / volumeStep);
		var adjusted = steps * volumeStep;

		if (adjusted < minVolume)
		adjusted = minVolume;

		if (adjusted > maxVolume)
		adjusted = maxVolume;

		return adjusted;
	}

	private void UpdateDistances()
	{
		if (Security == null)
		return;

		// Convert point-based inputs into absolute prices using the instrument step.
		var priceStep = Security.PriceStep ?? 0m;
		if (priceStep <= 0m)
		priceStep = 0.0001m;

		_minRangeDistance = MinRangePoints * priceStep;
		_maxRangeDistance = MaxRangePoints * priceStep;
		_minBreakDistance = MinBreakRange * priceStep;
		_maxBreakDistance = MaxBreakRange * priceStep;
		_stopDistance = StopLossPoints * priceStep;
		_trailDistance = TrailStopPoints * priceStep;
		_maxSpreadDistance = MaxSpreadPoints * priceStep;
	}

	private TimeSpan GetDstShift(DateTimeOffset time)
	{
		var utcDate = time.UtcDateTime.Date;
		var londonDst = IsLondonDst(utcDate);
		var newYorkDst = IsNewYorkDst(utcDate);

		// Mirror the original DST handling by applying one-hour offsets when regimes differ.
		return SwitchDst switch
		{
			DstSwitchMode.Usa when !londonDst && newYorkDst => TimeSpan.FromHours(1),
			DstSwitchMode.Usa when londonDst && !newYorkDst => TimeSpan.FromHours(-1),
			DstSwitchMode.None when londonDst => TimeSpan.FromHours(-1),
			_ => TimeSpan.Zero,
		};
	}

	private static bool IsLondonDst(DateTime date)
	{
		var start = GetLastSunday(date.Year, 3);
		var end = GetLastSunday(date.Year, 10);
		return date >= start && date < end;
	}

	private static bool IsNewYorkDst(DateTime date)
	{
		var start = GetNthSunday(date.Year, 3, 2);
		var end = GetNthSunday(date.Year, 11, 1);
		return date >= start && date < end;
	}

	private static DateTime GetLastSunday(int year, int month)
	{
		var date = new DateTime(year, month, DateTime.DaysInMonth(year, month));
		while (date.DayOfWeek != DayOfWeek.Sunday)
		date = date.AddDays(-1);
		return date;
	}

	private static DateTime GetNthSunday(int year, int month, int occurrence)
	{
		var date = new DateTime(year, month, 1);
		while (date.DayOfWeek != DayOfWeek.Sunday)
		date = date.AddDays(1);

		return date.AddDays(7 * (occurrence - 1));
	}
}
