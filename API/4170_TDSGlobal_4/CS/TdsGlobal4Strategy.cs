using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor TDSGlobal 4.
/// Implements Elder's triple screen concept with MACD slope and Williams %R filters on daily candles.
/// Places breakout stop orders around the previous day's range and manages positions with optional trailing stops.
/// </summary>
public class TdsGlobal4Strategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _williamsPeriod;
	private readonly StrategyParam<decimal> _williamsBuyLevel;
	private readonly StrategyParam<decimal> _williamsSellLevel;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _entryBufferPips;
	private readonly StrategyParam<decimal> _minDistancePips;
	private readonly StrategyParam<DataType> _dailyCandleType;
	private readonly StrategyParam<DataType> _triggerCandleType;

	private Order? _buyStopOrder;
	private Order? _sellStopOrder;

	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;
	private decimal? _longTrailingPrice;
	private decimal? _shortTrailingPrice;

	private decimal? _macdYesterday;
	private decimal? _macdTwoDaysAgo;
	private decimal? _williamsYesterday;

	private decimal? _previousHigh;
	private decimal? _previousLow;

	private DateOnly? _pendingOrderDay;
	private DateOnly? _evaluatedDay;
	private DateTimeOffset? _nextActivationTime;

	private decimal _pipSize;
	private decimal _entryBuffer;
	private decimal _minDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingStopDistance;

	private decimal? _lastBid;
	private decimal? _lastAsk;

	private static readonly (int start, int end)[] _defaultWindow =
	{
		(0, 59)
	};

	private static readonly (int start, int end)[] _usdChfWindows =
	{
		(0, 1),
		(8, 9),
		(16, 17),
		(24, 25),
		(32, 33),
		(40, 41),
		(48, 49)
	};

	private static readonly (int start, int end)[] _gbpUsdWindows =
	{
		(2, 3),
		(10, 11),
		(18, 19),
		(26, 27),
		(34, 35),
		(42, 43),
		(50, 51)
	};

	private static readonly (int start, int end)[] _usdJpyWindows =
	{
		(4, 5),
		(12, 13),
		(20, 21),
		(28, 29),
		(36, 37),
		(44, 45),
		(52, 53)
	};

	private static readonly (int start, int end)[] _eurUsdWindows =
	{
		(6, 7),
		(14, 15),
		(22, 23),
		(30, 31),
		(38, 39),
		(46, 47),
		(54, 59)
	};

	/// <summary>
	/// Order volume for pending entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Fast EMA length used by the MACD slope filter.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length used by the MACD slope filter.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA length used to compute the MACD histogram (OsMA).
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Williams %R lookback for the daily filter.
	/// </summary>
	public int WilliamsPeriod
	{
		get => _williamsPeriod.Value;
		set => _williamsPeriod.Value = value;
	}

	/// <summary>
	/// Threshold that defines an overbought reading for long setups (default -25).
	/// </summary>
	public decimal WilliamsBuyLevel
	{
		get => _williamsBuyLevel.Value;
		set => _williamsBuyLevel.Value = value;
	}

	/// <summary>
	/// Threshold that defines an oversold reading for short setups (default -75).
	/// </summary>
	public decimal WilliamsSellLevel
	{
		get => _williamsSellLevel.Value;
		set => _williamsSellLevel.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips (0 disables the target).
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips (0 disables trailing).
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Offset in pips added beyond the previous day's extreme for entry and stop levels.
	/// </summary>
	public decimal EntryBufferPips
	{
		get => _entryBufferPips.Value;
		set => _entryBufferPips.Value = value;
	}

	/// <summary>
	/// Minimum pip distance from the current quote to avoid placing orders too close.
	/// </summary>
	public decimal MinDistancePips
	{
		get => _minDistancePips.Value;
		set => _minDistancePips.Value = value;
	}

	/// <summary>
	/// Daily candle type used for indicator calculations.
	/// </summary>
	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	/// <summary>
	/// Lower timeframe candle type that drives the scheduling and intraday risk checks.
	/// </summary>
	public DataType TriggerCandleType
	{
		get => _triggerCandleType.Value;
		set => _triggerCandleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="TdsGlobal4Strategy"/>.
	/// </summary>
	public TdsGlobal4Strategy()
	{
		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume for pending entries", "Trading");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA length for MACD", "Indicators");

		_williamsPeriod = Param(nameof(WilliamsPeriod), 24)
		.SetGreaterThanZero()
		.SetDisplay("Williams %R Period", "Williams %R lookback", "Indicators");

		_williamsBuyLevel = Param(nameof(WilliamsBuyLevel), -25m)
		.SetDisplay("Williams Buy Level", "Upper threshold to allow longs", "Filters");

		_williamsSellLevel = Param(nameof(WilliamsSellLevel), -75m)
		.SetDisplay("Williams Sell Level", "Lower threshold to allow shorts", "Filters");

		_takeProfitPips = Param(nameof(TakeProfitPips), 999m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk");

		_entryBufferPips = Param(nameof(EntryBufferPips), 1m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Entry Buffer (pips)", "Offset added beyond the previous extreme", "Entries");

		_minDistancePips = Param(nameof(MinDistancePips), 16m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Min Distance (pips)", "Minimum distance from current price", "Entries");

		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Daily Candle Type", "Timeframe used for MACD and Williams %R", "Data");

		_triggerCandleType = Param(nameof(TriggerCandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Trigger Candle Type", "Timeframe that schedules order placement and checks stops", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DailyCandleType);

		if (!Equals(DailyCandleType, TriggerCandleType))
		yield return (Security, TriggerCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_buyStopOrder = null;
		_sellStopOrder = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakeProfitPrice = null;
		_shortTakeProfitPrice = null;
		_longTrailingPrice = null;
		_shortTrailingPrice = null;
		_macdYesterday = null;
		_macdTwoDaysAgo = null;
		_williamsYesterday = null;
		_previousHigh = null;
		_previousLow = null;
		_pendingOrderDay = null;
		_evaluatedDay = null;
		_nextActivationTime = null;
		_lastBid = null;
		_lastAsk = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_pipSize = CalculatePipSize();
		_entryBuffer = EntryBufferPips * _pipSize;
		_minDistance = MinDistancePips * _pipSize;
		_takeProfitDistance = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;
		_trailingStopDistance = TrailingStopPips > 0m ? TrailingStopPips * _pipSize : 0m;

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod }
			},
			SignalMa = { Length = MacdSignalPeriod }
		};

		var williams = new WilliamsPercentRange { Length = WilliamsPeriod };

		var dailySubscription = SubscribeCandles(DailyCandleType);
		dailySubscription
		.BindEx(macd, williams, ProcessDailyCandle)
		.Start();

		var triggerSubscription = SubscribeCandles(TriggerCandleType);
		triggerSubscription
		.Bind(ProcessTriggerCandle)
		.Start();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, dailySubscription);
			DrawIndicator(area, macd);
			DrawIndicator(area, williams);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue williamsValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!macdValue.IsFinal || !williamsValue.IsFinal)
		return;

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macd.Macd is not decimal macdMain || macd.Signal is not decimal macdSignal)
		return;

		var williams = williamsValue.GetValue<decimal>();

		_macdTwoDaysAgo = _macdYesterday;
		_macdYesterday = macdMain;
		_williamsYesterday = williams;

		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;

		CancelAndReset(ref _buyStopOrder);
		CancelAndReset(ref _sellStopOrder);

		_pendingOrderDay = DateOnly.FromDateTime(candle.CloseTime.DateTime);
		_evaluatedDay = null;
		_nextActivationTime = DetermineActivationTime(candle.CloseTime);
	}

	private void ProcessTriggerCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		EvaluateProtectiveLevels(candle);
		TryPlaceDailyOrders(candle);
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue is decimal bid)
		_lastBid = bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue is decimal ask)
		_lastAsk = ask;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		UpdateTrailingStops();
	}

	private void TryPlaceDailyOrders(ICandleMessage candle)
	{
		if (_pendingOrderDay == null || _nextActivationTime == null)
		return;

		var candleTime = candle.OpenTime;

		if (candleTime < _nextActivationTime.Value)
		return;

		var day = DateOnly.FromDateTime(candleTime.DateTime);

		if (day != _pendingOrderDay.Value)
		return;

		if (_evaluatedDay == _pendingOrderDay)
		return;

		var minute = candleTime.Minute;

		if (!IsMinuteAllowed(minute))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_evaluatedDay = _pendingOrderDay;
			return;
		}

		if (!HasSetupReady())
		{
			_evaluatedDay = _pendingOrderDay;
			return;
		}

		var ask = _lastAsk ?? candle.ClosePrice;
		var bid = _lastBid ?? candle.ClosePrice;

		if (ask <= 0m || bid <= 0m)
		{
			_evaluatedDay = _pendingOrderDay;
			return;
		}

		PlaceEntryOrders(ask, bid);
		_evaluatedDay = _pendingOrderDay;
	}

	private void EvaluateProtectiveLevels(ICandleMessage candle)
	{
		var volume = Math.Abs(Position);

		if (volume == 0m)
		return;

		if (Position > 0m)
		{
			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(volume);
				ClearLongState();
				return;
			}

			if (_longTakeProfitPrice.HasValue && candle.HighPrice >= _longTakeProfitPrice.Value)
			{
				SellMarket(volume);
				ClearLongState();
				return;
			}
		}
		else if (Position < 0m)
		{
			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(volume);
				ClearShortState();
				return;
			}

			if (_shortTakeProfitPrice.HasValue && candle.LowPrice <= _shortTakeProfitPrice.Value)
			{
				BuyMarket(volume);
				ClearShortState();
			}
		}
	}

	private void UpdateTrailingStops()
	{
		if (_trailingStopDistance <= 0m)
		{
			_longTrailingPrice = null;
			_shortTrailingPrice = null;
			return;
		}

		var volume = Math.Abs(Position);

		if (volume == 0m)
		{
			_longTrailingPrice = null;
			_shortTrailingPrice = null;
			return;
		}

		if (Position > 0m && _lastBid is decimal bid && bid > 0m)
		{
			var entryPrice = Position.AveragePrice;
			var newStop = RoundPrice(bid - _trailingStopDistance);

			if (bid - entryPrice >= _trailingStopDistance)
			{
				if (!_longTrailingPrice.HasValue || newStop > _longTrailingPrice.Value)
				_longTrailingPrice = newStop;
			}

			if (_longTrailingPrice.HasValue && bid <= _longTrailingPrice.Value)
			{
				SellMarket(volume);
				ClearLongState();
				return;
			}
		}
		else
		{
			_longTrailingPrice = null;
		}

		if (Position < 0m && _lastAsk is decimal ask && ask > 0m)
		{
			var entryPrice = Position.AveragePrice;
			var newStop = RoundPrice(ask + _trailingStopDistance);

			if (entryPrice - ask >= _trailingStopDistance)
			{
				if (!_shortTrailingPrice.HasValue || newStop < _shortTrailingPrice.Value)
				_shortTrailingPrice = newStop;
			}

			if (_shortTrailingPrice.HasValue && ask >= _shortTrailingPrice.Value)
			{
				BuyMarket(volume);
				ClearShortState();
			}
		}
		else
		{
			_shortTrailingPrice = null;
		}
	}

	private bool PlaceEntryOrders(decimal ask, decimal bid)
	{
		if (Volume <= 0m)
		return false;

		if (_previousHigh is not decimal prevHigh || _previousLow is not decimal prevLow)
		return false;

		if (_macdYesterday is not decimal macdPrev || _macdTwoDaysAgo is not decimal macdPrev2)
		return false;

		if (_williamsYesterday is not decimal williams)
		return false;

		var direction = macdPrev.CompareTo(macdPrev2);

		var placed = false;

		if (direction > 0 && williams < WilliamsBuyLevel)
		{
			var breakout = RoundPrice(prevHigh + _entryBuffer);
			var minAllowed = RoundPrice(ask + _minDistance);
			var entryPrice = Math.Max(breakout, minAllowed);
			var stopPrice = RoundPrice(prevLow - _entryBuffer);
			var takePrice = _takeProfitDistance > 0m ? RoundPrice(entryPrice + _takeProfitDistance) : (decimal?)null;

			CancelAndReset(ref _buyStopOrder);
			_buyStopOrder = BuyStop(Volume, entryPrice);

			_longStopPrice = stopPrice;
			_longTakeProfitPrice = takePrice;
			_longTrailingPrice = null;
			placed = _buyStopOrder != null || placed;
		}
		else
		{
			CancelAndReset(ref _buyStopOrder);
			_longStopPrice = null;
			_longTakeProfitPrice = null;
			_longTrailingPrice = null;
		}

		if (direction < 0 && williams > WilliamsSellLevel)
		{
			var breakout = RoundPrice(prevLow - _entryBuffer);
			var minAllowed = RoundPrice(bid - _minDistance);
			var entryPrice = Math.Min(breakout, minAllowed);
			var stopPrice = RoundPrice(prevHigh + _entryBuffer);
			var takePrice = _takeProfitDistance > 0m ? RoundPrice(entryPrice - _takeProfitDistance) : (decimal?)null;

			CancelAndReset(ref _sellStopOrder);
			_sellStopOrder = SellStop(Volume, entryPrice);

			_shortStopPrice = stopPrice;
			_shortTakeProfitPrice = takePrice;
			_shortTrailingPrice = null;
			placed = _sellStopOrder != null || placed;
		}
		else
		{
			CancelAndReset(ref _sellStopOrder);
			_shortStopPrice = null;
			_shortTakeProfitPrice = null;
			_shortTrailingPrice = null;
		}

		return placed;
	}

	private bool HasSetupReady()
	{
		if (_previousHigh is null || _previousLow is null)
		return false;

		if (_macdYesterday is null || _macdTwoDaysAgo is null)
		return false;

		if (_williamsYesterday is null)
		return false;

		if (HasOpenExposure())
		return false;

		return true;
	}

	private bool HasOpenExposure()
	{
		if (Position != 0m)
		return true;

		if (IsOrderActive(_buyStopOrder) || IsOrderActive(_sellStopOrder))
		return true;

		return false;
	}

	private static bool IsOrderActive(Order? order)
	{
		return order is { State: OrderStates.None or OrderStates.Pending or OrderStates.Active };
	}

	private void CancelAndReset(ref Order? order)
	{
		if (order == null)
		return;

		if (order.State is OrderStates.None or OrderStates.Pending or OrderStates.Active)
		CancelOrder(order);

		order = null;
	}

	private bool IsMinuteAllowed(int minute)
	{
		foreach (var window in GetAllowedMinuteWindows())
		{
			if (minute >= window.start && minute <= window.end)
			return true;
		}

		return false;
	}

	private IReadOnlyList<(int start, int end)> GetAllowedMinuteWindows()
	{
		var symbol = Security?.Code?.ToUpperInvariant();

		return symbol switch
		{
			"USDCHF" => _usdChfWindows,
			"GBPUSD" => _gbpUsdWindows,
			"USDJPY" => _usdJpyWindows,
			"EURUSD" => _eurUsdWindows,
			_ => _defaultWindow
		};
	}

	private DateTimeOffset DetermineActivationTime(DateTimeOffset nextDayStart)
	{
		var windows = GetAllowedMinuteWindows();

		if (windows.Count == 0)
		return nextDayStart;

		var first = windows[0];
		return nextDayStart + TimeSpan.FromMinutes(first.start);
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
		return 0.0001m;

		var digits = 0;
		var value = step;

		while (value < 1m && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		if (digits is 3 or 5)
		return step * 10m;

		return step;
	}

	private decimal RoundPrice(decimal price)
	{
		var step = Security?.PriceStep;

		if (step == null || step.Value <= 0m)
		return price;

		return Math.Round(price / step.Value, MidpointRounding.AwayFromZero) * step.Value;
	}

	private void ClearLongState()
	{
		_longStopPrice = null;
		_longTakeProfitPrice = null;
		_longTrailingPrice = null;
	}

	private void ClearShortState()
	{
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
		_shortTrailingPrice = null;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (_buyStopOrder != null && order == _buyStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		_buyStopOrder = null;

		if (_sellStopOrder != null && order == _sellStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		_sellStopOrder = null;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
		return;

		if (_buyStopOrder != null && trade.Order == _buyStopOrder)
		{
			_buyStopOrder = null;
			CancelAndReset(ref _sellStopOrder);

			var entryPrice = trade.Trade.Price;
			_longTakeProfitPrice = _takeProfitDistance > 0m ? RoundPrice(entryPrice + _takeProfitDistance) : _longTakeProfitPrice;
		}
		else if (_sellStopOrder != null && trade.Order == _sellStopOrder)
		{
			_sellStopOrder = null;
			CancelAndReset(ref _buyStopOrder);

			var entryPrice = trade.Trade.Price;
			_shortTakeProfitPrice = _takeProfitDistance > 0m ? RoundPrice(entryPrice - _takeProfitDistance) : _shortTakeProfitPrice;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ClearLongState();
			ClearShortState();
			return;
		}

		if (Position > 0m)
		{
			CancelAndReset(ref _sellStopOrder);
			_shortStopPrice = null;
			_shortTakeProfitPrice = null;
			_shortTrailingPrice = null;
		}
		else if (Position < 0m)
		{
			CancelAndReset(ref _buyStopOrder);
			_longStopPrice = null;
			_longTakeProfitPrice = null;
			_longTrailingPrice = null;
		}
	}
}
