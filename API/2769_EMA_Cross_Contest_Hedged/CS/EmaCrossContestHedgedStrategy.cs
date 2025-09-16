using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with hedged stop orders and trailing management.
/// Converted from the MQL version of "EMA Cross Contest Hedged".
/// </summary>
public class EmaCrossContestHedgedStrategy : Strategy
{
	public enum TradeBarOption
	{
		Current,
		Previous
	}

	private const int PendingOrderCount = 4;
	private const int MacdFastLength = 4;
	private const int MacdSlowLength = 24;
	private const int MacdSignalLength = 12;

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _hedgeLevelPips;
	private readonly StrategyParam<bool> _closeOppositePositions;
	private readonly StrategyParam<bool> _useMacdFilter;
	private readonly StrategyParam<int> _pendingExpirationSeconds;
	private readonly StrategyParam<int> _shortMaPeriod;
	private readonly StrategyParam<int> _longMaPeriod;
	private readonly StrategyParam<TradeBarOption> _tradeBar;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _emaShortLast;
	private decimal? _emaShortPrevLast;
	private decimal? _emaLongLast;
	private decimal? _emaLongPrevLast;
	private decimal? _macdLast;

	private decimal _currentVolume;
	private decimal _entryPrice;
	private decimal? _longStop;
	private decimal? _longTakeProfit;
	private decimal? _shortStop;
	private decimal? _shortTakeProfit;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	private readonly List<PendingOrder> _pendingOrders = new();

	private sealed class PendingOrder
	{
		public Sides Side { get; init; }
		public decimal Price { get; init; }
		public decimal? StopLoss { get; init; }
		public decimal? TakeProfit { get; init; }
		public DateTimeOffset ExpireTime { get; init; }
	}

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	public int HedgeLevelPips
	{
		get => _hedgeLevelPips.Value;
		set => _hedgeLevelPips.Value = value;
	}

	public bool CloseOppositePositions
	{
		get => _closeOppositePositions.Value;
		set => _closeOppositePositions.Value = value;
	}

	public bool UseMacdFilter
	{
		get => _useMacdFilter.Value;
		set => _useMacdFilter.Value = value;
	}

	public int PendingExpirationSeconds
	{
		get => _pendingExpirationSeconds.Value;
		set => _pendingExpirationSeconds.Value = value;
	}

	public int ShortMaPeriod
	{
		get => _shortMaPeriod.Value;
		set => _shortMaPeriod.Value = value;
	}

	public int LongMaPeriod
	{
		get => _longMaPeriod.Value;
		set => _longMaPeriod.Value = value;
	}

	public TradeBarOption TradeBar
	{
		get => _tradeBar.Value;
		set => _tradeBar.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public EmaCrossContestHedgedStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetDisplay("Order Volume", "Order size", "General")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 140)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 120)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 30)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 1)
			.SetDisplay("Trailing Step (pips)", "Minimum profit before trailing adjusts", "Risk");

		_hedgeLevelPips = Param(nameof(HedgeLevelPips), 6)
			.SetDisplay("Hedge Level (pips)", "Distance between hedging stop orders", "Orders");

		_closeOppositePositions = Param(nameof(CloseOppositePositions), false)
			.SetDisplay("Close Opposite", "Close positions on opposite crossover", "Risk");

		_useMacdFilter = Param(nameof(UseMacdFilter), false)
			.SetDisplay("Use MACD", "Require MACD confirmation", "Filters");

		_pendingExpirationSeconds = Param(nameof(PendingExpirationSeconds), 65535)
			.SetDisplay("Pending Expiration (s)", "Lifetime of hedging stop orders in seconds", "Orders");

		_shortMaPeriod = Param(nameof(ShortMaPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA Period", "Fast EMA length", "Indicators");

		_longMaPeriod = Param(nameof(LongMaPeriod), 24)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA Period", "Slow EMA length", "Indicators");

		_tradeBar = Param(nameof(TradeBar), TradeBarOption.Previous)
			.SetDisplay("Trade Bar", "Use current or previous bar for signals", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_emaShortLast = null;
		_emaShortPrevLast = null;
		_emaLongLast = null;
		_emaLongPrevLast = null;
		_macdLast = null;

		_currentVolume = 0m;
		_entryPrice = 0m;
		_longStop = null;
		_longTakeProfit = null;
		_shortStop = null;
		_shortTakeProfit = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_pendingOrders.Clear();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (ShortMaPeriod >= LongMaPeriod)
			throw new InvalidOperationException("Short EMA period must be less than long EMA period.");

		Volume = OrderVolume;

		var shortEma = new EMA { Length = ShortMaPeriod };
		var longEma = new EMA { Length = LongMaPeriod };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength }
			},
			SignalMa = { Length = MacdSignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(shortEma, longEma, macd, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortEma);
			DrawIndicator(area, longEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue shortValue, IIndicatorValue longValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!shortValue.IsFinal || !longValue.IsFinal)
			return;

		var emaShort = shortValue.ToDecimal();
		var emaLong = longValue.ToDecimal();

		decimal? macdCurrent = null;
		if (macdValue.IsFinal)
		{
			var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
			if (macdTyped.Macd is decimal macdLine)
				macdCurrent = macdLine;
		}

		ProcessPendingOrders(candle);

		var cross = DetectCross(emaShort, emaLong);

		decimal? macdFilterValue = null;
		if (UseMacdFilter)
		{
			macdFilterValue = TradeBar == TradeBarOption.Current ? macdCurrent : _macdLast;
			if (!macdFilterValue.HasValue)
			{
				UpdateHistory(emaShort, emaLong, macdCurrent);
				return;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateHistory(emaShort, emaLong, macdCurrent);
			return;
		}

		if (_currentVolume > 0m)
		{
			if (CloseOppositePositions && cross == 2)
			{
				ExitLong();
				UpdateHistory(emaShort, emaLong, macdCurrent);
				return;
			}

			if (CheckLongStops(candle))
			{
				UpdateHistory(emaShort, emaLong, macdCurrent);
				return;
			}
		}
		else if (_currentVolume < 0m)
		{
			if (CloseOppositePositions && cross == 1)
			{
				ExitShort();
				UpdateHistory(emaShort, emaLong, macdCurrent);
				return;
			}

			if (CheckShortStops(candle))
			{
				UpdateHistory(emaShort, emaLong, macdCurrent);
				return;
			}
		}

		if (_currentVolume == 0m)
		{
			if (cross == 1 && (!UseMacdFilter || macdFilterValue >= 0m))
			{
				EnterLong(candle.ClosePrice, candle.CloseTime);
				UpdateHistory(emaShort, emaLong, macdCurrent);
				return;
			}

			if (cross == 2 && (!UseMacdFilter || macdFilterValue <= 0m))
			{
				EnterShort(candle.ClosePrice, candle.CloseTime);
				UpdateHistory(emaShort, emaLong, macdCurrent);
				return;
			}
		}

		UpdateHistory(emaShort, emaLong, macdCurrent);
	}

	private void ProcessPendingOrders(ICandleMessage candle)
	{
		if (_pendingOrders.Count == 0)
			return;

		var now = candle.CloseTime;

		for (var i = _pendingOrders.Count - 1; i >= 0; i--)
		{
			var order = _pendingOrders[i];

			if (order.ExpireTime <= now)
			{
				_pendingOrders.RemoveAt(i);
				continue;
			}

			var triggered = order.Side == Sides.Buy
				? candle.HighPrice >= order.Price
				: candle.LowPrice <= order.Price;

			if (!triggered)
				continue;

			_pendingOrders.RemoveAt(i);

			if (order.Side == Sides.Buy)
			{
				BuyMarket(OrderVolume);
				RegisterLongEntry(order.Price, OrderVolume, order.StopLoss, order.TakeProfit);
			}
			else
			{
				SellMarket(OrderVolume);
				RegisterShortEntry(order.Price, OrderVolume, order.StopLoss, order.TakeProfit);
			}
		}
	}

	private void EnterLong(decimal price, DateTimeOffset time)
	{
		BuyMarket(OrderVolume);
		RegisterLongEntry(price, OrderVolume,
			StopLossPips > 0 ? price - PipToPrice(StopLossPips) : null,
			TakeProfitPips > 0 ? price + PipToPrice(TakeProfitPips) : null);

		_shortStop = null;
		_shortTakeProfit = null;
		_shortTrailingStop = null;

		CreatePendingOrders(time, price, Sides.Buy);
	}

	private void EnterShort(decimal price, DateTimeOffset time)
	{
		SellMarket(OrderVolume);
		RegisterShortEntry(price, OrderVolume,
			StopLossPips > 0 ? price + PipToPrice(StopLossPips) : null,
			TakeProfitPips > 0 ? price - PipToPrice(TakeProfitPips) : null);

		_longStop = null;
		_longTakeProfit = null;
		_longTrailingStop = null;

		CreatePendingOrders(time, price, Sides.Sell);
	}

	private void RegisterLongEntry(decimal price, decimal volume, decimal? stop, decimal? take)
	{
		var previousVolume = _currentVolume;
		_currentVolume += volume;

		if (previousVolume <= 0m)
			_entryPrice = price;
		else
			_entryPrice = ((previousVolume * _entryPrice) + (volume * price)) / _currentVolume;

		if (stop.HasValue)
			_longStop = _longStop.HasValue ? Math.Max(_longStop.Value, stop.Value) : stop;

		if (take.HasValue)
			_longTakeProfit = _longTakeProfit.HasValue ? Math.Max(_longTakeProfit.Value, take.Value) : take;

		_longTrailingStop = null;
	}

	private void RegisterShortEntry(decimal price, decimal volume, decimal? stop, decimal? take)
	{
		var previousVolume = _currentVolume;
		_currentVolume -= volume;

		if (previousVolume >= 0m)
			_entryPrice = price;
		else
			_entryPrice = ((Math.Abs(previousVolume) * _entryPrice) + (volume * price)) / Math.Abs(_currentVolume);

		if (stop.HasValue)
			_shortStop = _shortStop.HasValue ? Math.Min(_shortStop.Value, stop.Value) : stop;

		if (take.HasValue)
			_shortTakeProfit = _shortTakeProfit.HasValue ? Math.Min(_shortTakeProfit.Value, take.Value) : take;

		_shortTrailingStop = null;
	}

	private bool CheckLongStops(ICandleMessage candle)
	{
		var trailingDistance = PipToPrice(TrailingStopPips);
		var trailingStep = PipToPrice(TrailingStepPips);

		if (TrailingStopPips > 0 && _currentVolume > 0m)
		{
			var profit = candle.ClosePrice - _entryPrice;
			if (profit > trailingDistance + trailingStep)
			{
				var minAdvance = candle.ClosePrice - (trailingDistance + trailingStep);
				var newStop = candle.ClosePrice - trailingDistance;
				if (!_longTrailingStop.HasValue || _longTrailingStop.Value < minAdvance)
					_longTrailingStop = newStop;
			}
		}

		var effectiveStop = _longStop;
		if (_longTrailingStop.HasValue)
			effectiveStop = effectiveStop.HasValue ? Math.Max(effectiveStop.Value, _longTrailingStop.Value) : _longTrailingStop;

		if (effectiveStop.HasValue && candle.LowPrice <= effectiveStop.Value)
		{
			ExitLong();
			return true;
		}

		if (_longTakeProfit.HasValue && candle.HighPrice >= _longTakeProfit.Value)
		{
			ExitLong();
			return true;
		}

		return false;
	}

	private bool CheckShortStops(ICandleMessage candle)
	{
		var trailingDistance = PipToPrice(TrailingStopPips);
		var trailingStep = PipToPrice(TrailingStepPips);

		if (TrailingStopPips > 0 && _currentVolume < 0m)
		{
			var profit = _entryPrice - candle.ClosePrice;
			if (profit > trailingDistance + trailingStep)
			{
				var maxAdvance = candle.ClosePrice + trailingDistance + trailingStep;
				var newStop = candle.ClosePrice + trailingDistance;
				if (!_shortTrailingStop.HasValue || _shortTrailingStop.Value > maxAdvance)
					_shortTrailingStop = newStop;
			}
		}

		var effectiveStop = _shortStop;
		if (_shortTrailingStop.HasValue)
			effectiveStop = effectiveStop.HasValue ? Math.Min(effectiveStop.Value, _shortTrailingStop.Value) : _shortTrailingStop;

		if (effectiveStop.HasValue && candle.HighPrice >= effectiveStop.Value)
		{
			ExitShort();
			return true;
		}

		if (_shortTakeProfit.HasValue && candle.LowPrice <= _shortTakeProfit.Value)
		{
			ExitShort();
			return true;
		}

		return false;
	}

	private void ExitLong()
	{
		if (_currentVolume <= 0m)
			return;

		SellMarket(_currentVolume);
		_currentVolume = 0m;
		_entryPrice = 0m;
		_longStop = null;
		_longTakeProfit = null;
		_longTrailingStop = null;
	}

	private void ExitShort()
	{
		if (_currentVolume >= 0m)
			return;

		BuyMarket(Math.Abs(_currentVolume));
		_currentVolume = 0m;
		_entryPrice = 0m;
		_shortStop = null;
		_shortTakeProfit = null;
		_shortTrailingStop = null;
	}

	private int DetectCross(decimal emaShort, decimal emaLong)
	{
		decimal prevShort;
		decimal prevLong;
		decimal currentShort;
		decimal currentLong;

		if (TradeBar == TradeBarOption.Current)
		{
			if (!_emaShortLast.HasValue || !_emaLongLast.HasValue)
				return 0;

			prevShort = _emaShortLast.Value;
			prevLong = _emaLongLast.Value;
			currentShort = emaShort;
			currentLong = emaLong;
		}
		else
		{
			if (!_emaShortLast.HasValue || !_emaLongLast.HasValue || !_emaShortPrevLast.HasValue || !_emaLongPrevLast.HasValue)
				return 0;

			prevShort = _emaShortPrevLast.Value;
			prevLong = _emaLongPrevLast.Value;
			currentShort = _emaShortLast.Value;
			currentLong = _emaLongLast.Value;
		}

		if (prevShort < prevLong && currentShort > currentLong)
			return 1;

		if (prevShort > prevLong && currentShort < currentLong)
			return 2;

		return 0;
	}

	private void UpdateHistory(decimal emaShort, decimal emaLong, decimal? macdCurrent)
	{
		_emaShortPrevLast = _emaShortLast;
		_emaLongPrevLast = _emaLongLast;
		_emaShortLast = emaShort;
		_emaLongLast = emaLong;

		if (macdCurrent.HasValue)
			_macdLast = macdCurrent;
	}

	private decimal PipToPrice(int pips)
	{
		if (pips <= 0)
			return 0m;

		var step = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals ?? 0;
		var multiplier = (decimals == 3 || decimals == 5) ? 10m : 1m;

		return pips * step * multiplier;
	}

	private void CreatePendingOrders(DateTimeOffset time, decimal price, Sides side)
	{
		_pendingOrders.Clear();

		if (HedgeLevelPips <= 0)
			return;

		var distance = PipToPrice(HedgeLevelPips);
		if (distance <= 0m)
			return;

		var expiration = PendingExpirationSeconds > 0
			? time + TimeSpan.FromSeconds(PendingExpirationSeconds)
			: DateTimeOffset.MaxValue;

		var stopOffset = StopLossPips > 0 ? PipToPrice(StopLossPips) : 0m;
		var takeOffset = TakeProfitPips > 0 ? PipToPrice(TakeProfitPips) : 0m;

		for (var i = 1; i <= PendingOrderCount; i++)
		{
			var levelPrice = side == Sides.Buy
				? price + distance * i
				: price - distance * i;

			decimal? stop = null;
			decimal? take = null;

			if (StopLossPips > 0)
				stop = side == Sides.Buy
					? levelPrice - stopOffset
					: levelPrice + stopOffset;

			if (TakeProfitPips > 0)
				take = side == Sides.Buy
					? levelPrice + takeOffset
					: levelPrice - takeOffset;

			_pendingOrders.Add(new PendingOrder
			{
				Side = side,
				Price = levelPrice,
				StopLoss = stop,
				TakeProfit = take,
				ExpireTime = expiration
			});
		}
	}
}
