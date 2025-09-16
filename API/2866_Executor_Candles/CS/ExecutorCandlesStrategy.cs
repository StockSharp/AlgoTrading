using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Candle pattern strategy converted from the Executor Candles MetaTrader expert.
/// </summary>
public class ExecutorCandlesStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossBuyPips;
	private readonly StrategyParam<int> _takeProfitBuyPips;
	private readonly StrategyParam<int> _trailingStopBuyPips;
	private readonly StrategyParam<int> _stopLossSellPips;
	private readonly StrategyParam<int> _takeProfitSellPips;
	private readonly StrategyParam<int> _trailingStopSellPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _trendCandleType;

	private ICandleMessage? _prev1;
	private ICandleMessage? _prev2;
	private ICandleMessage? _prev3;

	private decimal? _entryPrice;
	private decimal? _stopLevel;
	private decimal? _takeLevel;

	private bool? _trendDown;

	private decimal _priceStep;
	private decimal _tolerance;

	/// <summary>
	/// Stop loss distance for long positions in pips.
	/// </summary>
	public int StopLossBuyPips
	{
		get => _stopLossBuyPips.Value;
		set => _stopLossBuyPips.Value = value;
	}

	/// <summary>
	/// Take profit distance for long positions in pips.
	/// </summary>
	public int TakeProfitBuyPips
	{
		get => _takeProfitBuyPips.Value;
		set => _takeProfitBuyPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance for long positions in pips.
	/// </summary>
	public int TrailingStopBuyPips
	{
		get => _trailingStopBuyPips.Value;
		set => _trailingStopBuyPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance for short positions in pips.
	/// </summary>
	public int StopLossSellPips
	{
		get => _stopLossSellPips.Value;
		set => _stopLossSellPips.Value = value;
	}

	/// <summary>
	/// Take profit distance for short positions in pips.
	/// </summary>
	public int TakeProfitSellPips
	{
		get => _takeProfitSellPips.Value;
		set => _takeProfitSellPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance for short positions in pips.
	/// </summary>
	public int TrailingStopSellPips
	{
		get => _trailingStopSellPips.Value;
		set => _trailingStopSellPips.Value = value;
	}

	/// <summary>
	/// Minimum trailing step in pips required before adjusting the stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Volume used for market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Enables confirmation from a higher timeframe candle.
	/// </summary>
	public bool UseTrendFilter
	{
		get => _useTrendFilter.Value;
		set => _useTrendFilter.Value = value;
	}

	/// <summary>
	/// Trading candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type for the trend filter.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExecutorCandlesStrategy"/>.
	/// </summary>
	public ExecutorCandlesStrategy()
	{
		_stopLossBuyPips = Param(nameof(StopLossBuyPips), 50)
		.SetDisplay("Stop Loss Buy", "Stop loss for long trades", "Risk");

		_takeProfitBuyPips = Param(nameof(TakeProfitBuyPips), 50)
		.SetDisplay("Take Profit Buy", "Take profit for long trades", "Risk");

		_trailingStopBuyPips = Param(nameof(TrailingStopBuyPips), 15)
		.SetDisplay("Trailing Stop Buy", "Trailing stop for long trades", "Risk");

		_stopLossSellPips = Param(nameof(StopLossSellPips), 50)
		.SetDisplay("Stop Loss Sell", "Stop loss for short trades", "Risk");

		_takeProfitSellPips = Param(nameof(TakeProfitSellPips), 50)
		.SetDisplay("Take Profit Sell", "Take profit for short trades", "Risk");

		_trailingStopSellPips = Param(nameof(TrailingStopSellPips), 15)
		.SetDisplay("Trailing Stop Sell", "Trailing stop for short trades", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetDisplay("Trailing Step", "Minimum trailing step", "Risk");

		_volume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");

		_useTrendFilter = Param(nameof(UseTrendFilter), false)
		.SetDisplay("Use Trend Filter", "Enable higher timeframe confirmation", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Trading timeframe", "General");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Trend Candle Type", "Higher timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (UseTrendFilter)
		return [(Security, CandleType), (Security, TrendCandleType)];

		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prev1 = null;
		_prev2 = null;
		_prev3 = null;
		_entryPrice = null;
		_stopLevel = null;
		_takeLevel = null;
		_trendDown = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0.0001m;
		if (_priceStep <= 0m)
		_priceStep = 0.0001m;

		_tolerance = _priceStep / 2m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		if (UseTrendFilter)
		{
			var trendSubscription = SubscribeCandles(TrendCandleType);
			trendSubscription.Bind(ProcessTrendCandle).Start();
		}
		else
		{
			_trendDown = false;
		}
	}

	private void ProcessTrendCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_trendDown = candle.OpenPrice >= candle.ClosePrice;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_prev3 = _prev2;
		_prev2 = _prev1;
		_prev1 = candle;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (UseTrendFilter && _trendDown == null)
		return;

		if (Position == 0)
		TryOpenPosition();

		ManageActivePosition(candle);
	}

	private void TryOpenPosition()
	{
		if (_prev1 == null || _prev2 == null)
		return;

		var trendDown = _trendDown ?? false;

		if (IsHammer(trendDown, _prev1, _prev2))
		{
			OpenLong(_prev1.ClosePrice);
			return;
		}

		if (IsBullishEngulfing(trendDown, _prev1, _prev2))
		{
			OpenLong(_prev1.ClosePrice);
			return;
		}

		if (IsPiercing(trendDown, _prev1, _prev2))
		{
			OpenLong(_prev1.ClosePrice);
			return;
		}

		if (_prev3 != null && IsMorningStar(_prev1, _prev2, _prev3))
		{
			OpenLong(_prev1.ClosePrice);
			return;
		}

		if (_prev3 != null && IsMorningDojiStar(_prev1, _prev2, _prev3))
		{
			OpenLong(_prev1.ClosePrice);
			return;
		}

		if (IsHangingMan(trendDown, _prev1, _prev2))
		{
			OpenShort(_prev1.ClosePrice);
			return;
		}

		if (IsBearishEngulfing(trendDown, _prev1, _prev2))
		{
			OpenShort(_prev1.ClosePrice);
			return;
		}

		if (IsDarkCloudCover(trendDown, _prev1, _prev2))
		{
			OpenShort(_prev1.ClosePrice);
			return;
		}

		if (_prev3 != null && IsEveningStar(_prev1, _prev2, _prev3))
		{
			OpenShort(_prev1.ClosePrice);
			return;
		}

		if (_prev3 != null && IsEveningDojiStar(_prev1, _prev2, _prev3))
		OpenShort(_prev1.ClosePrice);
	}

	private void OpenLong(decimal price)
	{
		BuyMarket(OrderVolume);

		_entryPrice = price;
		_stopLevel = StopLossBuyPips > 0 ? price - StopLossBuyPips * _priceStep : null;
		_takeLevel = TakeProfitBuyPips > 0 ? price + TakeProfitBuyPips * _priceStep : null;
	}

	private void OpenShort(decimal price)
	{
		SellMarket(OrderVolume);

		_entryPrice = price;
		_stopLevel = StopLossSellPips > 0 ? price + StopLossSellPips * _priceStep : null;
		_takeLevel = TakeProfitSellPips > 0 ? price - TakeProfitSellPips * _priceStep : null;
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			HandleLongPosition(candle);
		}
		else if (Position < 0)
		{
			HandleShortPosition(candle);
		}
		else
		{
			ResetPositionState();
		}
	}

	private void HandleLongPosition(ICandleMessage candle)
	{
		if (_stopLevel.HasValue && candle.LowPrice <= _stopLevel.Value)
		{
			SellMarket(Math.Abs(Position));
			ResetPositionState();
			return;
		}

		if (_takeLevel.HasValue && candle.HighPrice >= _takeLevel.Value)
		{
			SellMarket(Math.Abs(Position));
			ResetPositionState();
			return;
		}

		if (_entryPrice == null)
		return;

		if (TrailingStopBuyPips <= 0 || TrailingStepPips <= 0)
		return;

		var trailingDistance = TrailingStopBuyPips * _priceStep;
		var trailingStep = TrailingStepPips * _priceStep;
		var progress = candle.ClosePrice - _entryPrice.Value;

		if (progress <= trailingDistance + trailingStep)
		return;

		var newStop = candle.ClosePrice - trailingDistance;

		if (!_stopLevel.HasValue || newStop - _stopLevel.Value >= trailingStep)
		_stopLevel = newStop;
	}

	private void HandleShortPosition(ICandleMessage candle)
	{
		if (_stopLevel.HasValue && candle.HighPrice >= _stopLevel.Value)
		{
			BuyMarket(Math.Abs(Position));
			ResetPositionState();
			return;
		}

		if (_takeLevel.HasValue && candle.LowPrice <= _takeLevel.Value)
		{
			BuyMarket(Math.Abs(Position));
			ResetPositionState();
			return;
		}

		if (_entryPrice == null)
		return;

		if (TrailingStopSellPips <= 0 || TrailingStepPips <= 0)
		return;

		var trailingDistance = TrailingStopSellPips * _priceStep;
		var trailingStep = TrailingStepPips * _priceStep;
		var progress = _entryPrice.Value - candle.ClosePrice;

		if (progress <= trailingDistance + trailingStep)
		return;

		var newStop = candle.ClosePrice + trailingDistance;

		if (!_stopLevel.HasValue || _stopLevel.Value - newStop >= trailingStep)
		_stopLevel = newStop;
	}

	private void ResetPositionState()
	{
		if (Position == 0)
		{
			_entryPrice = null;
			_stopLevel = null;
			_takeLevel = null;
		}
	}

	private bool IsHammer(bool trendDown, ICandleMessage current, ICandleMessage previous)
	{
		if (AreEqual(current.ClosePrice, current.OpenPrice))
		return false;

		if (!trendDown && current.ClosePrice > current.OpenPrice && previous.OpenPrice > previous.ClosePrice)
		{
			var body = current.ClosePrice - current.OpenPrice;
			if (body <= 0m)
			return false;

			var upper = (current.HighPrice - current.ClosePrice) * 100m / body;
			var lower = (current.OpenPrice - current.LowPrice) * 100m / body;

			return upper > 200m && lower < 15m;
		}

		return false;
	}

	private bool IsBullishEngulfing(bool trendDown, ICandleMessage current, ICandleMessage previous)
	{
		if (AreEqual(previous.OpenPrice, previous.ClosePrice))
		return false;

		if (!trendDown && current.ClosePrice > current.OpenPrice && previous.OpenPrice > previous.ClosePrice)
		{
			if (current.ClosePrice < previous.OpenPrice)
			return false;

			if (current.OpenPrice > previous.ClosePrice)
			return false;

			var prevBody = previous.OpenPrice - previous.ClosePrice;
			var currBody = current.ClosePrice - current.OpenPrice;

			if (prevBody == 0m)
			return false;

			return currBody / prevBody > 1.5m;
		}

		return false;
	}

	private bool IsPiercing(bool trendDown, ICandleMessage current, ICandleMessage previous)
	{
		if (AreEqual(previous.HighPrice, previous.LowPrice))
		return false;

		if (!trendDown && current.ClosePrice > current.OpenPrice && previous.OpenPrice > previous.ClosePrice)
		{
			var body = previous.OpenPrice - previous.ClosePrice;
			var range = previous.HighPrice - previous.LowPrice;

			if (range == 0m)
			return false;

			var ratio = body / range;
			var midpoint = previous.ClosePrice + body / 2m;

			return ratio > 0.6m && current.OpenPrice < previous.LowPrice && current.ClosePrice > midpoint;
		}

		return false;
	}

	private bool IsMorningStar(ICandleMessage current, ICandleMessage middle, ICandleMessage older)
	{
		if (AreEqual(older.OpenPrice, older.ClosePrice))
		return false;

		if (AreEqual(older.HighPrice, older.LowPrice))
		return false;

		if (AreEqual(middle.HighPrice, middle.LowPrice))
		return false;

		if (AreEqual(current.HighPrice, current.LowPrice))
		return false;

		if (older.OpenPrice > older.ClosePrice && middle.ClosePrice > middle.OpenPrice && current.ClosePrice > current.OpenPrice)
		{
			if (middle.ClosePrice >= older.ClosePrice)
			return false;

			if (current.OpenPrice <= middle.ClosePrice)
			return false;

			var numerator = Math.Abs(older.OpenPrice - current.ClosePrice) + Math.Abs(current.OpenPrice - older.ClosePrice);
			var denominator = older.OpenPrice - older.ClosePrice;

			if (denominator == 0m)
			return false;

			var olderRatio = denominator / (older.HighPrice - older.LowPrice);
			var middleRatio = (middle.ClosePrice - middle.OpenPrice) / (middle.HighPrice - middle.LowPrice);
			var currentRatio = (current.ClosePrice - current.OpenPrice) / (current.HighPrice - current.LowPrice);

			return numerator / denominator < 0.1m && olderRatio > 0.8m && middleRatio < 0.3m && currentRatio > 0.8m;
		}

		return false;
	}

	private bool IsMorningDojiStar(ICandleMessage current, ICandleMessage middle, ICandleMessage older)
	{
		if (AreEqual(older.OpenPrice, older.ClosePrice))
		return false;

		if (older.OpenPrice <= older.ClosePrice)
		return false;

		if (!AreEqual(middle.ClosePrice, middle.OpenPrice))
		return false;

		if (current.ClosePrice <= current.OpenPrice)
		return false;

		if (middle.ClosePrice > older.ClosePrice)
		return false;

		if (current.OpenPrice < middle.ClosePrice)
		return false;

		var numerator = Math.Abs(older.OpenPrice - current.ClosePrice) + Math.Abs(current.OpenPrice - older.ClosePrice);
		var denominator = older.OpenPrice - older.ClosePrice;

		if (denominator == 0m)
		return false;

		return numerator / denominator < 0.1m;
	}

	private bool IsHangingMan(bool trendDown, ICandleMessage current, ICandleMessage previous)
	{
		if (AreEqual(current.OpenPrice, current.ClosePrice))
		return false;

		if (trendDown && current.OpenPrice > current.ClosePrice && previous.OpenPrice < previous.ClosePrice)
		{
			var body = current.OpenPrice - current.ClosePrice;
			if (body <= 0m)
			return false;

			var upper = (current.HighPrice - current.OpenPrice) * 100m / body;
			var lower = (current.ClosePrice - current.LowPrice) * 100m / body;

			return upper < 15m && lower > 200m;
		}

		return false;
	}

	private bool IsBearishEngulfing(bool trendDown, ICandleMessage current, ICandleMessage previous)
	{
		if (AreEqual(previous.ClosePrice, previous.OpenPrice))
		return false;

		if (trendDown && current.OpenPrice > current.ClosePrice && previous.ClosePrice > previous.OpenPrice)
		{
			if (current.OpenPrice < previous.ClosePrice)
			return false;

			if (current.ClosePrice > previous.OpenPrice)
			return false;

			var prevBody = previous.ClosePrice - previous.OpenPrice;
			var currBody = current.OpenPrice - current.ClosePrice;

			if (prevBody == 0m)
			return false;

			return currBody / (-prevBody) > 1.5m;
		}

		return false;
	}

	private bool IsDarkCloudCover(bool trendDown, ICandleMessage current, ICandleMessage previous)
	{
		if (AreEqual(previous.HighPrice, previous.LowPrice))
		return false;

		if (trendDown && current.OpenPrice > current.ClosePrice && previous.ClosePrice > previous.OpenPrice)
		{
			var body = previous.ClosePrice - previous.OpenPrice;
			var range = previous.HighPrice - previous.LowPrice;

			if (range == 0m)
			return false;

			var ratio = body / range;
			var midpoint = previous.OpenPrice + (previous.ClosePrice - previous.OpenPrice) / 2m;

			return ratio > 0.6m && current.OpenPrice > previous.HighPrice && current.ClosePrice < midpoint;
		}

		return false;
	}

	private bool IsEveningStar(ICandleMessage current, ICandleMessage middle, ICandleMessage older)
	{
		if (AreEqual(older.ClosePrice, older.OpenPrice))
		return false;

		if (AreEqual(older.HighPrice, older.LowPrice))
		return false;

		if (AreEqual(current.HighPrice, current.LowPrice))
		return false;

		if (older.OpenPrice < older.ClosePrice && middle.ClosePrice < middle.OpenPrice && current.ClosePrice < current.OpenPrice)
		{
			if (middle.ClosePrice <= older.ClosePrice)
			return false;

			if (current.OpenPrice >= middle.ClosePrice)
			return false;

			var numerator = Math.Abs(older.OpenPrice - current.ClosePrice) + Math.Abs(current.OpenPrice - older.ClosePrice);
			var denominator = older.ClosePrice - older.OpenPrice;

			if (denominator == 0m)
			return false;

			var olderRatio = (older.ClosePrice - older.OpenPrice) / (older.HighPrice - older.LowPrice);
			var middleRatio = (middle.OpenPrice - middle.ClosePrice) / (middle.HighPrice - middle.LowPrice);
			var currentRatio = (current.OpenPrice - current.ClosePrice) / (current.HighPrice - current.LowPrice);

			return numerator / denominator < 0.1m && olderRatio > 0.8m && middleRatio < 0.3m && currentRatio > 0.8m;
		}

		return false;
	}

	private bool IsEveningDojiStar(ICandleMessage current, ICandleMessage middle, ICandleMessage older)
	{
		if (AreEqual(older.OpenPrice, older.ClosePrice))
		return false;

		if (older.OpenPrice >= older.ClosePrice)
		return false;

		if (!AreEqual(middle.ClosePrice, middle.OpenPrice))
		return false;

		if (current.ClosePrice >= current.OpenPrice)
		return false;

		if (middle.ClosePrice < older.ClosePrice)
		return false;

		if (current.OpenPrice > middle.ClosePrice)
		return false;

		var numerator = Math.Abs(older.OpenPrice - current.ClosePrice) + Math.Abs(current.OpenPrice - older.ClosePrice);
		var denominator = older.OpenPrice - older.ClosePrice;

		if (denominator == 0m)
		return false;

		return numerator / denominator < 0.1m;
	}

	private bool AreEqual(decimal a, decimal b)
	{
		return Math.Abs(a - b) <= _tolerance;
	}
}
