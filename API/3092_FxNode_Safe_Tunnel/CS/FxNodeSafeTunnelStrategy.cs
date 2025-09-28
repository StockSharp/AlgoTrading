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
/// Trendline touch breakout strategy converted from the FxNode Safe Tunnel MetaTrader 4 expert advisor.
/// Draws dynamic ZigZag trendlines and trades when price taps the channel boundaries while risk filters allow trading.
/// </summary>
public class FxNodeSafeTunnelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _atrMultiplier;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TrendPreferences> _trendPreference;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _maxStopLossPips;
	private readonly StrategyParam<decimal> _fixedTakeProfitPips;
	private readonly StrategyParam<decimal> _touchDistanceBuyPips;
	private readonly StrategyParam<decimal> _touchDistanceSellPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _staticVolume;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _maxSpreadPips;
	private readonly StrategyParam<decimal> _riskPercentage;
	private readonly StrategyParam<int> _maxOpenPositions;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<bool> _closeBeforeWeekend;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _zigZagDepth;
	private readonly StrategyParam<decimal> _zigZagDeviationPips;
	private readonly StrategyParam<int> _zigZagBackstep;
	private readonly StrategyParam<int> _zigZagHistory;

	private AverageTrueRange _atr = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private readonly List<ZigZagPivot> _highPivots = new();
	private readonly List<ZigZagPivot> _lowPivots = new();

	private DateTimeOffset? _lastOrderTime;
	private DateTimeOffset? _lastPivotUpdateTime;
	private decimal? _pendingStopPrice;
	private decimal? _pendingTakePrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _pendingFixedTakeProfitDistance;
	private decimal _fixedTakeProfitDistance;
	private decimal _lineHeight;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private bool _hasBestBid;
	private bool _hasBestAsk;
	private int _barsSinceHighPivot;
	private int _barsSinceLowPivot;
	private TimeSpan? _timeFrame;

	/// <summary>
	/// Initializes a new instance of the <see cref="FxNodeSafeTunnelStrategy"/> class.
	/// </summary>
	public FxNodeSafeTunnelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for trading", "General");

		_trendPreference = Param(nameof(TrendPreferences), TrendPreferences.Both)
		.SetDisplay("Trend Preference", "Directional bias for new trades", "General");

		_takeProfitPips = Param(nameof(TakeProfitPips), 800m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Maximum take profit distance in pips", "Risk");

		_maxStopLossPips = Param(nameof(MaxStopLossPips), 200m)
		.SetNotNegative()
		.SetDisplay("Max Stop Loss (pips)", "Maximum stop loss distance in pips", "Risk");

		_fixedTakeProfitPips = Param(nameof(FixedTakeProfitPips), 0m)
		.SetNotNegative()
		.SetDisplay("Fixed Profit (pips)", "Profit in pips required to close early", "Risk");

		_touchDistanceBuyPips = Param(nameof(TouchDistanceBuyPips), 20m)
		.SetNotNegative()
		.SetDisplay("Touch Distance Buy", "Distance above the lower trendline that triggers long entries", "Entries");

		_touchDistanceSellPips = Param(nameof(TouchDistanceSellPips), 20m)
		.SetNotNegative()
		.SetDisplay("Touch Distance Sell", "Distance below the upper trendline that triggers short entries", "Entries");

		_trailingStopPips = Param(nameof(TrailingStopPips), 50m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Trailing distance applied after price moves in favour", "Risk");

		_staticVolume = Param(nameof(StaticVolume), 1m)
		.SetNotNegative()
		.SetDisplay("Static Volume", "Fallback volume when risk-based sizing is unavailable", "Risk");

		_minVolume = Param(nameof(MinVolume), 0.02m)
		.SetNotNegative()
		.SetDisplay("Minimum Volume", "Lower volume bound", "Risk");

		_maxVolume = Param(nameof(MaxVolume), 10m)
		.SetNotNegative()
		.SetDisplay("Maximum Volume", "Upper volume bound", "Risk");

		_maxSpreadPips = Param(nameof(MaxSpreadPips), 15m)
		.SetNotNegative()
		.SetDisplay("Max Spread (pips)", "Highest spread tolerated before cancelling entries", "Risk");

		_riskPercentage = Param(nameof(RiskPercentage), 30m)
		.SetNotNegative()
		.SetDisplay("Risk %", "Portfolio percentage risked per trade", "Risk");

		_maxOpenPositions = Param(nameof(MaxOpenPositions), 1)
		.SetNotNegative()
		.SetDisplay("Max Open Positions", "Maximum simultaneous position count", "Risk");

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
		.SetDisplay("Use Time Filter", "Enable trading window restrictions", "Timing");

		_sessionStart = Param(nameof(SessionStart), TimeSpan.Zero)
		.SetDisplay("Session Start", "Start time of the trading window", "Timing");

		_sessionEnd = Param(nameof(SessionEnd), TimeSpan.FromHours(6))
		.SetDisplay("Session End", "End time of the trading window", "Timing");

		_closeBeforeWeekend = Param(nameof(CloseBeforeWeekend), true)
		.SetDisplay("Close Before Weekend", "Exit positions late on Friday", "Timing");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Average True Range lookback", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 10m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "Multiplier applied to ATR distance filters", "Indicators");

		_zigZagDepth = Param(nameof(ZigZagDepth), 5)
		.SetGreaterThanZero()
		.SetDisplay("ZigZag Depth", "Lookback for swing detection", "Indicators");

		_zigZagDeviationPips = Param(nameof(ZigZagDeviationPips), 3m)
		.SetNotNegative()
		.SetDisplay("ZigZag Deviation", "Minimum distance between pivots in pips", "Indicators");

		_zigZagBackstep = Param(nameof(ZigZagBackstep), 1)
		.SetNotNegative()
		.SetDisplay("ZigZag Backstep", "Bars between consecutive pivots", "Indicators");

		_zigZagHistory = Param(nameof(ZigZagHistory), 10)
		.SetGreaterThanZero()
		.SetDisplay("ZigZag History", "Stored pivot count for drawing trendlines", "Indicators");
	}

	/// <summary>
	/// Primary candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Preferred trade direction.
	/// </summary>
	public TrendPreferences TrendPreference
	{
		get => _trendPreference.Value;
		set => _trendPreference.Value = value;
	}

	/// <summary>
	/// Maximum take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Maximum stop loss distance in pips.
	/// </summary>
	public decimal MaxStopLossPips
	{
		get => _maxStopLossPips.Value;
		set => _maxStopLossPips.Value = value;
	}

	/// <summary>
	/// Profit distance that triggers early exit.
	/// </summary>
	public decimal FixedTakeProfitPips
	{
		get => _fixedTakeProfitPips.Value;
		set => _fixedTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Distance above the lower trendline to trigger long entries.
	/// </summary>
	public decimal TouchDistanceBuyPips
	{
		get => _touchDistanceBuyPips.Value;
		set => _touchDistanceBuyPips.Value = value;
	}

	/// <summary>
	/// Distance below the upper trendline to trigger short entries.
	/// </summary>
	public decimal TouchDistanceSellPips
	{
		get => _touchDistanceSellPips.Value;
		set => _touchDistanceSellPips.Value = value;
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
	/// Static fallback volume used when risk sizing cannot be calculated.
	/// </summary>
	public decimal StaticVolume
	{
		get => _staticVolume.Value;
		set => _staticVolume.Value = value;
	}

	/// <summary>
	/// Minimum allowed order volume.
	/// </summary>
	public decimal MinVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

	/// <summary>
	/// Maximum allowed order volume.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread in pips for new entries.
	/// </summary>
	public decimal MaxSpreadPips
	{
		get => _maxSpreadPips.Value;
		set => _maxSpreadPips.Value = value;
	}

	/// <summary>
	/// Percentage of portfolio risked per trade.
	/// </summary>
	public decimal RiskPercentage
	{
		get => _riskPercentage.Value;
		set => _riskPercentage.Value = value;
	}

	/// <summary>
	/// Maximum simultaneous open positions.
	/// </summary>
	public int MaxOpenPositions
	{
		get => _maxOpenPositions.Value;
		set => _maxOpenPositions.Value = value;
	}

	/// <summary>
	/// Enables the trading session filter.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Trading window start time.
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// Trading window end time.
	/// </summary>
	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
	}

	/// <summary>
	/// Whether to force exits near the weekend.
	/// </summary>
	public bool CloseBeforeWeekend
	{
		get => _closeBeforeWeekend.Value;
		set => _closeBeforeWeekend.Value = value;
	}

	/// <summary>
	/// ATR indicator length.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR-derived distance filters.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// ZigZag pivot search depth.
	/// </summary>
	public int ZigZagDepth
	{
		get => _zigZagDepth.Value;
		set => _zigZagDepth.Value = value;
	}

	/// <summary>
	/// Minimum distance between ZigZag pivots in pips.
	/// </summary>
	public decimal ZigZagDeviationPips
	{
		get => _zigZagDeviationPips.Value;
		set => _zigZagDeviationPips.Value = value;
	}

	/// <summary>
	/// Minimum bars between consecutive pivots.
	/// </summary>
	public int ZigZagBackstep
	{
		get => _zigZagBackstep.Value;
		set => _zigZagBackstep.Value = value;
	}

	/// <summary>
	/// Number of pivots stored for trendline calculations.
	/// </summary>
	public int ZigZagHistory
	{
		get => _zigZagHistory.Value;
		set => _zigZagHistory.Value = value;
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

		_highPivots.Clear();
		_lowPivots.Clear();
		_lastOrderTime = null;
		_lastPivotUpdateTime = null;
		_pendingStopPrice = null;
		_pendingTakePrice = null;
		_stopPrice = null;
		_takePrice = null;
		_pendingFixedTakeProfitDistance = 0m;
		_fixedTakeProfitDistance = 0m;
		_lineHeight = 0m;
		_bestBid = null;
		_bestAsk = null;
		_hasBestBid = false;
		_hasBestAsk = false;
		_barsSinceHighPivot = 0;
		_barsSinceLowPivot = 0;
		_timeFrame = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_highest = new Highest { Length = Math.Max(1, ZigZagDepth), CandlePrice = CandlePrice.High };
		_lowest = new Lowest { Length = Math.Max(1, ZigZagDepth), CandlePrice = CandlePrice.Low };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_atr, _highest, _lowest, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}

		_timeFrame = GetTimeFrame();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var time = GetCandleTime(candle);

		UpdateZigZagPivots(candle, highestValue, lowestValue);

		if (_lastPivotUpdateTime != time)
		_lineHeight = CalculateLineHeight();

		_lastPivotUpdateTime = time;

		if (CloseBeforeWeekend)
		CheckWeekendExit(time);

		if (HandleActivePosition(candle))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_atr.IsFormed || !_highest.IsFormed || !_lowest.IsFormed)
		return;

		if (!HasTrendlines())
		return;

		if (UseTimeFilter && !IsWithinSession(time.TimeOfDay))
		return;

		if (!AllowNewOrder(time))
		return;

		EvaluateEntrySignals(candle, atrValue, time);
	}

	private void EvaluateEntrySignals(ICandleMessage candle, decimal atrValue, DateTimeOffset time)
	{
		var support = GetTrendlineValue(_lowPivots, time);
		var resistance = GetTrendlineValue(_highPivots, time);
		if (support is null || resistance is null)
		return;

		var pipSize = GetPipSize();
		if (pipSize <= 0m)
		return;

		var touchBuyDistance = GetPipValue(TouchDistanceBuyPips);
		var touchSellDistance = GetPipValue(TouchDistanceSellPips);

		var atrDistance = Math.Max(atrValue * AtrMultiplier, pipSize);
		var maxStopDistance = GetPipValue(MaxStopLossPips);
		if (maxStopDistance > 0m && atrDistance > maxStopDistance)
		atrDistance = maxStopDistance;

		var takeDistance = _lineHeight;
		var maxTakeDistance = GetPipValue(TakeProfitPips);
		if (maxTakeDistance > 0m && takeDistance > maxTakeDistance)
		takeDistance = maxTakeDistance;

		var fixedProfitDistance = GetPipValue(FixedTakeProfitPips);

		var ask = _hasBestAsk && _bestAsk is decimal bestAsk ? bestAsk : candle.ClosePrice;
		var bid = _hasBestBid && _bestBid is decimal bestBid ? bestBid : candle.ClosePrice;

		if (TrendPreferences != TrendPreferences.SellOnly && Position <= 0)
		{
			var takeLong = support is decimal sup && ask > sup && ask <= sup + touchBuyDistance;
			if (takeLong)
			{
				var volume = CalculateOrderVolume(atrDistance);
				if (volume > 0m && HasCapacityForNewPosition(volume))
				{
					_pendingStopPrice = Math.Max(ask - atrDistance, 0m);
					_pendingTakePrice = takeDistance > 0m ? ask + takeDistance : (decimal?)null;
					_pendingFixedTakeProfitDistance = fixedProfitDistance;

					BuyMarket(volume);
					_lastOrderTime = time;
				}
			}
		}

		if (TrendPreferences != TrendPreferences.BuyOnly && Position >= 0)
		{
			var takeShort = resistance is decimal res && bid < res && bid >= res - touchSellDistance;
			if (takeShort)
			{
				var volume = CalculateOrderVolume(atrDistance);
				if (volume > 0m && HasCapacityForNewPosition(volume))
				{
					_pendingStopPrice = bid + atrDistance;
					_pendingTakePrice = takeDistance > 0m ? bid - takeDistance : (decimal?)null;
					_pendingFixedTakeProfitDistance = fixedProfitDistance;

					SellMarket(volume);
					_lastOrderTime = time;
				}
			}
		}
	}

	private bool HandleActivePosition(ICandleMessage candle)
	{
		if (Position == 0)
		return false;

		var trailingDistance = GetPipValue(TrailingStopPips);
		if (trailingDistance > 0m)
		UpdateTrailingStop(candle, trailingDistance);

		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}

			if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}

			if (_fixedTakeProfitDistance > 0m && PositionPrice > 0m && candle.ClosePrice - PositionPrice >= _fixedTakeProfitDistance)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return true;
			}

			if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return true;
			}

			if (_fixedTakeProfitDistance > 0m && PositionPrice > 0m && PositionPrice - candle.ClosePrice >= _fixedTakeProfitDistance)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return true;
			}
		}

		return false;
	}

	private void UpdateTrailingStop(ICandleMessage candle, decimal trailingDistance)
	{
		if (Position > 0)
		{
			var profit = candle.ClosePrice - PositionPrice;
			if (profit <= trailingDistance)
			return;

			var desired = candle.ClosePrice - trailingDistance;
			if (_stopPrice is null || desired > _stopPrice)
			_stopPrice = desired;
		}
		else if (Position < 0)
		{
			var profit = PositionPrice - candle.ClosePrice;
			if (profit <= trailingDistance)
			return;

			var desired = candle.ClosePrice + trailingDistance;
			if (_stopPrice is null || desired < _stopPrice)
			_stopPrice = desired;
		}
	}

	private void UpdateZigZagPivots(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		_barsSinceHighPivot++;
		_barsSinceLowPivot++;

		var deviation = GetPipValue(ZigZagDeviationPips);
		if (deviation <= 0m)
		deviation = GetPipSize();

		var minBackstep = Math.Max(1, ZigZagBackstep);

		if (_highest.IsFormed && candle.HighPrice >= highestValue && _barsSinceHighPivot >= minBackstep)
		{
			var isNewHigh = _highPivots.Count == 0 || Math.Abs(candle.HighPrice - _highPivots[0].Price) >= deviation;
			if (isNewHigh)
			{
				RegisterPivot(_highPivots, candle.OpenTime, candle.HighPrice);
				_barsSinceHighPivot = 0;
			}
		}

		if (_lowest.IsFormed && candle.LowPrice <= lowestValue && _barsSinceLowPivot >= minBackstep)
		{
			var isNewLow = _lowPivots.Count == 0 || Math.Abs(candle.LowPrice - _lowPivots[0].Price) >= deviation;
			if (isNewLow)
			{
				RegisterPivot(_lowPivots, candle.OpenTime, candle.LowPrice);
				_barsSinceLowPivot = 0;
			}
		}
	}

	private void RegisterPivot(List<ZigZagPivot> pivots, DateTimeOffset time, decimal price)
	{
		if (pivots.Count > 0 && pivots[0].Time == time && pivots[0].Price == price)
		return;

		pivots.Insert(0, new ZigZagPivot
		{
			Time = time,
			Price = price
		});

		var limit = Math.Max(2, ZigZagHistory);
		if (pivots.Count > limit)
		pivots.RemoveRange(limit, pivots.Count - limit);
	}

	private bool HasTrendlines()
	{
		return _highPivots.Count >= 2 && _lowPivots.Count >= 2;
	}

	private decimal CalculateLineHeight()
	{
		if (_highPivots.Count == 0 || _lowPivots.Count == 0)
		return 0m;

		var high = _highPivots[0].Price;
		var low = _lowPivots[0].Price;
		return Math.Abs(high - low);
	}

	private decimal? GetTrendlineValue(List<ZigZagPivot> pivots, DateTimeOffset time)
	{
		if (pivots.Count < 2)
		return null;

		var recent = pivots[0];
		var previous = pivots[1];

		var totalTicks = (recent.Time - previous.Time).Ticks;
		if (totalTicks == 0)
		return recent.Price;

		var slope = (recent.Price - previous.Price) / totalTicks;
		var deltaTicks = (time - recent.Time).Ticks;
		return recent.Price + slope * deltaTicks;
	}

	private bool AllowNewOrder(DateTimeOffset time)
	{
		if (MaxOpenPositions <= 0)
		return false;

		if (time.DayOfWeek is DayOfWeek.Sunday or DayOfWeek.Friday or DayOfWeek.Saturday)
		return false;

		if (_lastOrderTime.HasValue && _timeFrame.HasValue)
		{
			var sinceLast = time - _lastOrderTime.Value;
			if (sinceLast < _timeFrame.Value)
			return false;
		}

		var spreadLimit = GetPipValue(MaxSpreadPips);
		if (spreadLimit > 0m)
		{
			var spread = GetCurrentSpread();
			if (spread > spreadLimit && spread > 0m)
			return false;
		}

		return true;
	}

	private bool HasCapacityForNewPosition(decimal volume)
	{
		if (MaxOpenPositions <= 0)
		return false;

		var currentExposure = Math.Abs(Position);
		var maximumExposure = MaxOpenPositions * volume;

		if (maximumExposure <= 0m)
		{
			var fallback = StaticVolume > 0m ? StaticVolume : 1m;
			maximumExposure = MaxOpenPositions * fallback;
		}

		return currentExposure + volume <= maximumExposure + volume * 0.0001m;
	}

	private decimal CalculateOrderVolume(decimal stopDistance)
	{
		var volume = StaticVolume > 0m ? StaticVolume : 1m;

		if (RiskPercentage > 0m && stopDistance > 0m)
		{
			var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
			var stepPrice = Security?.StepPrice ?? 0m;
			var priceStep = GetPipSize();

			if (portfolioValue > 0m && stepPrice > 0m && priceStep > 0m)
			{
				var stopSteps = stopDistance / priceStep;
				if (stopSteps > 0m)
				{
					var lossPerLot = stopSteps * stepPrice;
					if (lossPerLot > 0m)
					{
						var riskAmount = portfolioValue * RiskPercentage / 100m;
						volume = riskAmount / lossPerLot;
					}
				}
			}
		}

		if (volume <= 0m)
		volume = StaticVolume > 0m ? StaticVolume : 1m;

		if (MinVolume > 0m && volume < MinVolume)
		volume = MinVolume;

		if (MaxVolume > 0m && volume > MaxVolume)
		volume = MaxVolume;

		return volume;
	}

	private void ResetPositionState()
	{
		_stopPrice = null;
		_takePrice = null;
		_fixedTakeProfitDistance = 0m;
	}

	private void CheckWeekendExit(DateTimeOffset time)
	{
		if (Position == 0)
		return;

		if (time.DayOfWeek != DayOfWeek.Friday)
		return;

		var cutoff = new TimeSpan(23, 50, 0);
		if (time.TimeOfDay < cutoff)
		return;

		if (Position > 0)
		SellMarket(Position);
		else
		BuyMarket(-Position);

		ResetPositionState();
	}

	private bool IsWithinSession(TimeSpan time)
	{
		var start = SessionStart;
		var end = SessionEnd;

		if (start == end)
		return false;

		if (start < end)
		return time >= start && time <= end;

		return time >= start || time <= end;
	}

	private decimal GetCurrentSpread()
	{
		if (!_hasBestBid || !_hasBestAsk || _bestBid is null || _bestAsk is null)
		return 0m;

		var spread = _bestAsk.Value - _bestBid.Value;
		return spread > 0m ? spread : 0m;
	}

	private decimal GetPipValue(decimal pips)
	{
		var point = GetPipSize();
		return point <= 0m ? 0m : pips * point;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		step = Security?.MinPriceStep ?? 0m;
		return step;
	}

	private DateTimeOffset GetCandleTime(ICandleMessage candle)
	{
		if (candle.CloseTime != default)
		return candle.CloseTime;

		if (_timeFrame.HasValue)
		return candle.OpenTime + _timeFrame.Value;

		return candle.OpenTime;
	}

	private TimeSpan? GetTimeFrame()
	{
		return CandleType.Arg is TimeSpan frame ? frame : null;
	}

	/// <inheritdoc />
	protected override void OnLevel1(Security security, Level1ChangeMessage message)
	{
		base.OnLevel1(security, message);

		if (security != Security)
		return;

		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
		{
			_bestBid = bidPrice;
			_hasBestBid = true;
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
		{
			_bestAsk = askPrice;
			_hasBestAsk = true;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0)
		{
			ResetPositionState();
			_pendingStopPrice = null;
			_pendingTakePrice = null;
			_pendingFixedTakeProfitDistance = 0m;
			return;
		}

		if (_pendingStopPrice is decimal stop)
		_stopPrice = stop;

		if (_pendingTakePrice is decimal take)
		_takePrice = take;

		_fixedTakeProfitDistance = _pendingFixedTakeProfitDistance;

		_pendingStopPrice = null;
		_pendingTakePrice = null;
		_pendingFixedTakeProfitDistance = 0m;
	}

	private sealed class ZigZagPivot
	{
		public DateTimeOffset Time { get; set; }

		public decimal Price { get; set; }
	}

	/// <summary>
	/// Trade direction preference parameter.
	/// </summary>
	public enum TrendPreferences
	{
		BuyOnly,
		SellOnly,
		Both
	}
}

