using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multicurrency overlay hedge strategy converted from MQL.
/// Scans a universe of forex symbols, pairs positively/negatively correlated instruments and opens hedged blocks when the overlay threshold is breached.
/// </summary>
public class MulticurrencyOverlayHedgeStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<int> _correlationLookback;
	private readonly StrategyParam<int> _atrLookback;
	private readonly StrategyParam<decimal> _correlationThreshold;
	private readonly StrategyParam<decimal> _overlayThreshold;
	private readonly StrategyParam<bool> _takeProfitByPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _takeProfitByCurrency;
	private readonly StrategyParam<decimal> _takeProfitCurrency;
	private readonly StrategyParam<int> _maxOpenPairs;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<int> _recalcHour;
	private readonly StrategyParam<decimal> _maxSpread;

	private readonly Dictionary<Security, SecurityContext> _contexts = new();
	private readonly Dictionary<HedgePairKey, HedgeState> _pairs = new();
	private readonly Dictionary<Security, List<HedgePairKey>> _pairsBySecurity = new();
	private readonly List<Security> _universeList = new();

	private DateTime _lastRecalcDay = DateTime.MinValue;

	/// <summary>
	/// Securities used for correlation scan.
	/// </summary>
	public IEnumerable<Security> Universe
	{
		get => _universe.Value;
		set => _universe.Value = value;
	}

	/// <summary>
	/// Candle type used for all calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback window used to compute price ranges.
	/// </summary>
	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
	}

	/// <summary>
	/// Number of bars used to measure correlation.
	/// </summary>
	public int CorrelationLookback
	{
		get => _correlationLookback.Value;
		set => _correlationLookback.Value = value;
	}

	/// <summary>
	/// Number of bars used to compute ATR ratio.
	/// </summary>
	public int AtrLookback
	{
		get => _atrLookback.Value;
		set => _atrLookback.Value = value;
	}

	/// <summary>
	/// Minimum absolute correlation required to create a pair.
	/// </summary>
	public decimal CorrelationThreshold
	{
		get => _correlationThreshold.Value;
		set => _correlationThreshold.Value = value;
	}

	/// <summary>
	/// Overlay threshold in points for triggering a hedge.
	/// </summary>
	public decimal OverlayThreshold
	{
		get => _overlayThreshold.Value;
		set => _overlayThreshold.Value = value;
	}

	/// <summary>
	/// Enables point based mutual take profit.
	/// </summary>
	public bool TakeProfitByPoints
	{
		get => _takeProfitByPoints.Value;
		set => _takeProfitByPoints.Value = value;
	}

	/// <summary>
	/// Target points required to close the hedge block.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables currency based mutual take profit.
	/// </summary>
	public bool TakeProfitByCurrency
	{
		get => _takeProfitByCurrency.Value;
		set => _takeProfitByCurrency.Value = value;
	}

	/// <summary>
	/// Currency profit threshold for closing the hedge block.
	/// </summary>
	public decimal TakeProfitCurrency
	{
		get => _takeProfitCurrency.Value;
		set => _takeProfitCurrency.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open hedge pairs.
	/// </summary>
	public int MaxOpenPairs
	{
		get => _maxOpenPairs.Value;
		set => _maxOpenPairs.Value = value;
	}

	/// <summary>
	/// Base volume used for the secondary leg.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Hour of the day when correlations are recalculated.
	/// </summary>
	public int RecalculationHour
	{
		get => _recalcHour.Value;
		set => _recalcHour.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread in points for each leg.
	/// </summary>
	public decimal MaxSpread
	{
		get => _maxSpread.Value;
		set => _maxSpread.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MulticurrencyOverlayHedgeStrategy"/>.
	/// </summary>
	public MulticurrencyOverlayHedgeStrategy()
	{
		_universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
			.SetDisplay("Universe", "Collection of forex symbols", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for analysis", "General");

		_rangeLength = Param(nameof(RangeLength), 400)
			.SetGreaterThanZero()
			.SetDisplay("Range Length", "Bars used to build price envelopes", "Parameters");

		_correlationLookback = Param(nameof(CorrelationLookback), 500)
			.SetGreaterThanZero()
			.SetDisplay("Correlation Lookback", "Bars used for Pearson correlation", "Parameters");

		_atrLookback = Param(nameof(AtrLookback), 200)
			.SetGreaterThanZero()
			.SetDisplay("ATR Lookback", "Bars used to compute ATR ratio", "Parameters");

		_correlationThreshold = Param(nameof(CorrelationThreshold), 0.9m)
			.SetDisplay("Correlation Threshold", "Absolute correlation required for pairing", "Parameters");

		_overlayThreshold = Param(nameof(OverlayThreshold), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Overlay Threshold", "Distance in points to trigger hedging", "Trading");

		_takeProfitByPoints = Param(nameof(TakeProfitByPoints), true)
			.SetDisplay("TP by Points", "Enable point based take profit", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Points Target", "Mutual take profit in points", "Risk");

		_takeProfitByCurrency = Param(nameof(TakeProfitByCurrency), false)
			.SetDisplay("TP by Currency", "Enable currency based take profit", "Risk");

		_takeProfitCurrency = Param(nameof(TakeProfitCurrency), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Currency Target", "Mutual take profit in account currency", "Risk");

		_maxOpenPairs = Param(nameof(MaxOpenPairs), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Pairs", "Maximum simultaneously open hedges", "Risk");

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Secondary leg volume in lots", "Trading");

		_recalcHour = Param(nameof(RecalculationHour), 1)
			.SetDisplay("Recalc Hour", "Hour to rebuild pair statistics", "Trading");

		_maxSpread = Param(nameof(MaxSpread), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Max Spread", "Max allowed spread in points", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var universe = Universe;
		if (universe == null)
			yield break;

		foreach (var security in universe)
		{
			if (security == null)
				continue;

			yield return (security, CandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_contexts.Clear();
		_pairs.Clear();
		_pairsBySecurity.Clear();
		_universeList.Clear();
		_lastRecalcDay = DateTime.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var universe = Universe;
		if (universe == null)
			throw new InvalidOperationException("Universe must be configured before starting the strategy.");

		_universeList.Clear();
		foreach (var security in universe)
		{
			if (security == null)
				continue;

			if (!_universeList.Contains(security))
				_universeList.Add(security);
		}

		if (_universeList.Count < 2)
			throw new InvalidOperationException("Universe must contain at least two securities.");

		foreach (var security in _universeList)
		{
			var correlationCapacity = Math.Max(2, CorrelationLookback);
			var context = new SecurityContext(security, correlationCapacity, RangeLength, AtrLookback);

			_contexts[security] = context;
			_pairsBySecurity[security] = new List<HedgePairKey>();

			// Subscribe to finished candles for this security.
			SubscribeCandles(CandleType, true, security)
				.Bind(candle => ProcessCandle(candle, security))
				.Start();

			// Track best bid/ask for spread filtering.
			SubscribeLevel1(security)
				.Bind(message => context.UpdateLevel1(message))
				.Start();
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var context = _contexts[security];
		context.Update(candle);

		if (ShouldRecalculate(candle))
			RecalculatePairs();

		ManageOpenHedges();

		if (_pairsBySecurity.TryGetValue(security, out var pairs))
		{
			for (var i = 0; i < pairs.Count; i++)
			{
				TryOpenHedge(pairs[i]);
			}
		}
	}

	private bool ShouldRecalculate(ICandleMessage candle)
	{
		var day = candle.OpenTime.Date;
		if (day == _lastRecalcDay)
			return false;

		if (candle.OpenTime.Hour < RecalculationHour)
			return false;

		_lastRecalcDay = day;
		return true;
	}

	private void RecalculatePairs()
	{
		foreach (var list in _pairsBySecurity.Values)
			list.Clear();

		var count = _universeList.Count;
		for (var i = 0; i < count; i++)
		{
			var first = _universeList[i];
			var firstContext = _contexts[first];
			if (!firstContext.HasCorrelationData(CorrelationLookback))
				continue;

			for (var j = i + 1; j < count; j++)
			{
				var second = _universeList[j];
				var secondContext = _contexts[second];
				if (!secondContext.HasCorrelationData(CorrelationLookback))
					continue;

				var correlation = CalculateCorrelation(firstContext, secondContext);
				var absCorrelation = Math.Abs(correlation);
				if (absCorrelation < CorrelationThreshold)
					continue;

				var atrRatio = CalculateAtrRatio(firstContext, secondContext);
				if (atrRatio <= 0m)
					continue;

				var key = new HedgePairKey(first, second);
				if (!_pairs.TryGetValue(key, out var state))
				{
					state = new HedgeState(key);
					_pairs[key] = state;
				}

				state.IsPositive = correlation >= 0m;
				state.AtrRatio = atrRatio;

				_pairsBySecurity[first].Add(key);
				_pairsBySecurity[second].Add(key);
			}
		}

		var toRemove = new List<HedgePairKey>();
		foreach (var pair in _pairs)
		{
			var key = pair.Key;
			var state = pair.Value;
			if (state.IsOpen)
				continue;

			if (!_pairsBySecurity.TryGetValue(key.First, out var list) || !list.Contains(key))
				toRemove.Add(key);
		}

		for (var i = 0; i < toRemove.Count; i++)
			_pairs.Remove(toRemove[i]);
	}

	private void ManageOpenHedges()
	{
		foreach (var pair in _pairs)
		{
			var state = pair.Value;
			if (!state.IsOpen)
				continue;

			var points = CalculatePoints(state);
			if (TakeProfitByPoints && points >= TakeProfitPoints)
			{
				CloseHedge(state, "TP_POINTS");
				continue;
			}

			var currency = CalculateCurrency(state);
			if (TakeProfitByCurrency && currency >= TakeProfitCurrency)
				CloseHedge(state, "TP_CURRENCY");
		}
	}

	private void TryOpenHedge(HedgePairKey key)
	{
		if (!_pairs.TryGetValue(key, out var state))
			return;

		if (state.IsOpen)
			return;

		var firstContext = _contexts[key.First];
		var secondContext = _contexts[key.Second];

		if (!firstContext.HasRangeData(RangeLength) || !secondContext.HasRangeData(RangeLength))
			return;

		if (!IsSecurityAvailable(key.First) || !IsSecurityAvailable(key.Second))
			return;

		if (MaxOpenPairs > 0 && GetOpenPairsCount() >= MaxOpenPairs)
			return;

		if (!IsSpreadWithinLimit(firstContext) || !IsSpreadWithinLimit(secondContext))
			return;

		var action = DetermineAction(state, firstContext, secondContext);
		if (action == HedgeAction.None)
			return;

		var baseVolume = BaseVolume;
		if (baseVolume <= 0m)
			return;

		var scaledVolume = baseVolume * state.AtrRatio;
		if (scaledVolume <= 0m)
			return;

		var directions = GetDirections(action);
		var targetFirst = directions.dirFirst * scaledVolume;
		var targetSecond = directions.dirSecond * baseVolume;

		TradeToTarget(key.First, targetFirst, state.Tag);
		TradeToTarget(key.Second, targetSecond, state.Tag);

		state.Dir1 = directions.dirFirst;
		state.Dir2 = directions.dirSecond;
		state.Volume1 = scaledVolume;
		state.Volume2 = baseVolume;
		state.Entry1 = firstContext.LastClose;
		state.Entry2 = secondContext.LastClose;
		state.IsOpen = true;
	}

	private bool IsSecurityAvailable(Security security)
	{
		foreach (var pair in _pairs)
		{
			var state = pair.Value;
			if (!state.IsOpen)
				continue;

			if (pair.Key.First == security || pair.Key.Second == security)
				return false;
		}

		return true;
	}

	private int GetOpenPairsCount()
	{
		var count = 0;
		foreach (var pair in _pairs)
		{
			if (pair.Value.IsOpen)
				count++;
		}
		return count;
	}

	private bool IsSpreadWithinLimit(SecurityContext context)
	{
		if (MaxSpread <= 0m)
			return true;

		var spread = context.GetSpreadPoints();
		if (spread == decimal.MaxValue)
			return false;

		return spread <= MaxSpread;
	}

	private HedgeAction DetermineAction(HedgeState state, SecurityContext first, SecurityContext second)
	{
		var highMain = first.GetHigh(RangeLength);
		var lowMain = first.GetLow(RangeLength);
		if (highMain <= lowMain)
			return HedgeAction.None;

		decimal subHigh;
		decimal subLow;
		if (state.IsPositive)
		{
			subHigh = second.GetHigh(RangeLength);
			subLow = second.GetLow(RangeLength);
		}
		else
		{
			subHigh = second.GetLow(RangeLength);
			subLow = second.GetHigh(RangeLength);
		}

		if (subHigh <= subLow)
			return HedgeAction.None;

		var mainCenter = (highMain + lowMain) / 2m;
		var subCenter = (subHigh + subLow) / 2m;
		var denominator = subHigh - subLow;
		if (denominator == 0m)
			return HedgeAction.None;

		var pipsRatio = (highMain - lowMain) / denominator;
		if (pipsRatio == 0m)
			return HedgeAction.None;

		var subCloseOffset = second.LastClose - subCenter;
		var syntheticClose = mainCenter + subCloseOffset * pipsRatio;
		var step = first.Security.PriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		var hedgeRange = (first.LastClose - syntheticClose) / step;
		if (hedgeRange < -OverlayThreshold)
			return state.IsPositive ? HedgeAction.BuyMainSellSub : HedgeAction.BuyBoth;

		if (hedgeRange > OverlayThreshold)
			return state.IsPositive ? HedgeAction.SellMainBuySub : HedgeAction.SellBoth;

		return HedgeAction.None;
	}

	private (int dirFirst, int dirSecond) GetDirections(HedgeAction action)
	{
		return action switch
		{
			HedgeAction.BuyMainSellSub => (1, -1),
			HedgeAction.SellMainBuySub => (-1, 1),
			HedgeAction.BuyBoth => (1, 1),
			HedgeAction.SellBoth => (-1, -1),
			_ => (0, 0)
		};
	}

	private void TradeToTarget(Security security, decimal targetVolume, string tag)
	{
		if (Portfolio == null)
			return;

		var current = GetPositionValue(security, Portfolio) ?? 0m;
		var diff = targetVolume - current;
		if (Math.Abs(diff) < 1e-6m)
			return;

		var order = new Order
		{
			Security = security,
			Portfolio = Portfolio,
			Volume = Math.Abs(diff),
			Side = diff > 0m ? Sides.Buy : Sides.Sell,
			Type = OrderTypes.Market,
			Comment = tag
		};

		RegisterOrder(order);
	}

	private void CloseHedge(HedgeState state, string reason)
	{
		TradeToTarget(state.First, 0m, reason);
		TradeToTarget(state.Second, 0m, reason);

		state.IsOpen = false;
		state.Dir1 = 0;
		state.Dir2 = 0;
		state.Volume1 = 0m;
		state.Volume2 = 0m;
		state.Entry1 = 0m;
		state.Entry2 = 0m;
	}

	private decimal CalculatePoints(HedgeState state)
	{
		var first = _contexts[state.First];
		var second = _contexts[state.Second];

		var stepFirst = first.Security.PriceStep ?? 1m;
		var stepSecond = second.Security.PriceStep ?? 1m;
		if (stepFirst == 0m)
			stepFirst = 1m;
		if (stepSecond == 0m)
			stepSecond = 1m;

		var moveFirst = state.Dir1 * (first.LastClose - state.Entry1) / stepFirst * state.Volume1;
		var moveSecond = state.Dir2 * (second.LastClose - state.Entry2) / stepSecond * state.Volume2;
		return moveFirst + moveSecond;
	}

	private decimal CalculateCurrency(HedgeState state)
	{
		var first = _contexts[state.First];
		var second = _contexts[state.Second];

		var stepFirst = first.Security.PriceStep ?? 1m;
		var stepSecond = second.Security.PriceStep ?? 1m;
		if (stepFirst == 0m)
			stepFirst = 1m;
		if (stepSecond == 0m)
			stepSecond = 1m;

		var priceStepFirst = first.Security.StepPrice ?? stepFirst;
		var priceStepSecond = second.Security.StepPrice ?? stepSecond;

		var pnlFirst = state.Dir1 * (first.LastClose - state.Entry1) / stepFirst * priceStepFirst * state.Volume1;
		var pnlSecond = state.Dir2 * (second.LastClose - state.Entry2) / stepSecond * priceStepSecond * state.Volume2;
		return pnlFirst + pnlSecond;
	}

	private decimal CalculateCorrelation(SecurityContext first, SecurityContext second)
	{
		var lookback = CorrelationLookback;
		var available = Math.Min(first.CloseCount, second.CloseCount);
		if (lookback <= 0 || lookback > available)
			lookback = available;

		if (lookback < 2)
			return 0m;

		decimal sumX = 0m;
		decimal sumY = 0m;
		decimal sumXY = 0m;
		decimal sumX2 = 0m;
		decimal sumY2 = 0m;

		using var enumX = first.GetRecentCloses(lookback).GetEnumerator();
		using var enumY = second.GetRecentCloses(lookback).GetEnumerator();
		while (enumX.MoveNext() && enumY.MoveNext())
		{
			var x = enumX.Current;
			var y = enumY.Current;
			sumX += x;
			sumY += y;
			sumXY += x * y;
			sumX2 += x * x;
			sumY2 += y * y;
		}

		var numerator = lookback * sumXY - sumX * sumY;
		var denomPart1 = lookback * sumX2 - sumX * sumX;
		var denomPart2 = lookback * sumY2 - sumY * sumY;
		if (denomPart1 <= 0m || denomPart2 <= 0m)
			return 0m;

		var denominator = (decimal)Math.Sqrt((double)(denomPart1 * denomPart2));
		if (denominator == 0m)
			return 0m;

		return numerator / denominator;
	}

	private decimal CalculateAtrRatio(SecurityContext first, SecurityContext second)
	{
		var lookback = AtrLookback;
		var available = Math.Min(first.TrueRangeCount, second.TrueRangeCount);
		if (lookback <= 0 || lookback > available)
			lookback = available;

		if (lookback <= 0)
			return 0m;

		var atrFirst = first.GetAverageTrueRange(lookback);
		var atrSecond = second.GetAverageTrueRange(lookback);
		if (atrFirst <= 0m || atrSecond <= 0m)
			return 0m;

		return atrSecond / atrFirst;
	}

	private enum HedgeAction
	{
		None,
		BuyMainSellSub,
		SellMainBuySub,
		BuyBoth,
		SellBoth
	}

	private sealed class HedgeState
	{
		public HedgeState(HedgePairKey key)
		{
			Key = key;
			Tag = $"HEDGE_{key.First?.Id}_{key.Second?.Id}";
		}

		public HedgePairKey Key { get; }
		public Security First => Key.First;
		public Security Second => Key.Second;
		public bool IsPositive { get; set; }
		public decimal AtrRatio { get; set; }
		public bool IsOpen { get; set; }
		public int Dir1 { get; set; }
		public int Dir2 { get; set; }
		public decimal Volume1 { get; set; }
		public decimal Volume2 { get; set; }
		public decimal Entry1 { get; set; }
		public decimal Entry2 { get; set; }
		public string Tag { get; }
	}

	private readonly struct HedgePairKey : IEquatable<HedgePairKey>
	{
		public HedgePairKey(Security first, Security second)
		{
			First = first ?? throw new ArgumentNullException(nameof(first));
			Second = second ?? throw new ArgumentNullException(nameof(second));
		}

		public Security First { get; }
		public Security Second { get; }

		public bool Equals(HedgePairKey other)
		{
			return ReferenceEquals(First, other.First) && ReferenceEquals(Second, other.Second);
		}

		public override bool Equals(object? obj)
		{
			return obj is HedgePairKey other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(First, Second);
		}
	}

	private sealed class SecurityContext
	{
		private readonly RollingBuffer _closes;
		private readonly RollingBuffer _highs;
		private readonly RollingBuffer _lows;
		private readonly RollingBuffer _trueRanges;
		private decimal _previousClose;
		private bool _hasPreviousClose;

		public SecurityContext(Security security, int correlationCapacity, int rangeCapacity, int atrCapacity)
		{
			Security = security ?? throw new ArgumentNullException(nameof(security));
			_closes = new RollingBuffer(Math.Max(2, correlationCapacity));
			_highs = new RollingBuffer(Math.Max(1, rangeCapacity));
			_lows = new RollingBuffer(Math.Max(1, rangeCapacity));
			_trueRanges = new RollingBuffer(Math.Max(1, atrCapacity));
		}

		public Security Security { get; }
		public decimal LastClose { get; private set; }
		public decimal? BestBid { get; private set; }
		public decimal? BestAsk { get; private set; }
		public int CloseCount => _closes.Count;
		public int TrueRangeCount => _trueRanges.Count;

		public void Update(ICandleMessage candle)
		{
			_closes.Add(candle.ClosePrice);
			_highs.Add(candle.HighPrice);
			_lows.Add(candle.LowPrice);

			decimal trueRange;
			if (_hasPreviousClose)
			{
				var range = candle.HighPrice - candle.LowPrice;
				var highDiff = Math.Abs(candle.HighPrice - _previousClose);
				var lowDiff = Math.Abs(candle.LowPrice - _previousClose);
				trueRange = Math.Max(range, Math.Max(highDiff, lowDiff));
			}
			else
			{
				trueRange = candle.HighPrice - candle.LowPrice;
				_hasPreviousClose = true;
			}

			_trueRanges.Add(trueRange);
			_previousClose = candle.ClosePrice;
			LastClose = candle.ClosePrice;
		}

		public void UpdateLevel1(Level1ChangeMessage message)
		{
			BestBid = message.TryGetDecimal(Level1Fields.BestBidPrice) ?? BestBid;
			BestAsk = message.TryGetDecimal(Level1Fields.BestAskPrice) ?? BestAsk;
		}

		public bool HasCorrelationData(int required)
		{
			if (required <= 0)
				return _closes.Count >= 2;

			return _closes.Count >= required;
		}

		public bool HasRangeData(int required)
		{
			return _highs.Count >= required && _lows.Count >= required;
		}

		public IEnumerable<decimal> GetRecentCloses(int count) => _closes.EnumerateRecent(count);
		public decimal GetHigh(int count) => _highs.Max(count);
		public decimal GetLow(int count) => _lows.Min(count);
		public decimal GetAverageTrueRange(int count) => _trueRanges.Average(count);

		public decimal GetSpreadPoints()
		{
			var step = Security.PriceStep ?? 0m;
			if (BestBid is not decimal bid || BestAsk is not decimal ask || step <= 0m)
				return decimal.MaxValue;

			return (ask - bid) / step;
		}
	}

	private sealed class RollingBuffer
	{
		private readonly decimal[] _buffer;
		private int _start;
		private int _count;

		public RollingBuffer(int capacity)
		{
			_buffer = new decimal[Math.Max(1, capacity)];
			_start = 0;
			_count = 0;
		}

		public int Count => _count;

		public void Add(decimal value)
		{
			if (_count < _buffer.Length)
			{
				var index = (_start + _count) % _buffer.Length;
				_buffer[index] = value;
				_count++;
			}
			else
			{
				_buffer[_start] = value;
				_start = (_start + 1) % _buffer.Length;
			}
		}

		public IEnumerable<decimal> EnumerateRecent(int count)
		{
			if (count > _count)
				count = _count;

			for (var i = 0; i < count; i++)
			{
				var index = (_start + _count - count + i) % _buffer.Length;
				yield return _buffer[index];
			}
		}

		public decimal Max(int count)
		{
			if (_count == 0)
				return 0m;

			if (count > _count)
				count = _count;

			var max = decimal.MinValue;
			for (var i = 0; i < count; i++)
			{
				var index = (_start + _count - count + i) % _buffer.Length;
				var value = _buffer[index];
				if (value > max)
					max = value;
			}

			return max;
		}

		public decimal Min(int count)
		{
			if (_count == 0)
				return 0m;

			if (count > _count)
				count = _count;

			var min = decimal.MaxValue;
			for (var i = 0; i < count; i++)
			{
				var index = (_start + _count - count + i) % _buffer.Length;
				var value = _buffer[index];
				if (value < min)
					min = value;
			}

			return min;
		}

		public decimal Average(int count)
		{
			if (_count == 0)
				return 0m;

			if (count > _count || count <= 0)
				count = _count;

			decimal sum = 0m;
			for (var i = 0; i < count; i++)
			{
				var index = (_start + _count - count + i) % _buffer.Length;
				sum += _buffer[index];
			}

			return sum / count;
		}
	}
}
