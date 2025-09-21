using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily breakout strategy that places pending stop orders around the previous session extremes.
/// It mimics the behaviour of the "Daily STP Entry Frame" MetaTrader expert using the StockSharp high level API.
/// </summary>
public class DailyStpEntryFrameStrategy : Strategy
{
	private enum EntrySide
	{
		Short = -1,
		Both = 0,
		Long = 1,
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingSlope;
	private readonly StrategyParam<int> _sideFilter;
	private readonly StrategyParam<decimal> _thresholdPoints;
	private readonly StrategyParam<decimal> _spreadPoints;
	private readonly StrategyParam<decimal> _slippagePoints;
	private readonly StrategyParam<int> _noNewOrdersHour;
	private readonly StrategyParam<int> _fridayCutoffHour;
	private readonly StrategyParam<int> _earliestOrderHour;
	private readonly StrategyParam<int> _dayFilter;
	private readonly StrategyParam<int> _closeAfterSeconds;
	private readonly StrategyParam<decimal> _percentOfProfit;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _maximumDrawdownPercent;

	private decimal _pipSize;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;
	private decimal _thresholdOffset;
	private decimal _spreadOffset;
	private decimal _slippageOffset;

	private decimal _startEquity;
	private decimal _maxEquity;

	private decimal? _previousDayHigh;
	private decimal? _previousDayLow;

	private DateTime? _currentTradingDay;
	private decimal? _todayOpenPrice;
	private DateTime? _lastBuyOrderDay;
	private DateTime? _lastSellOrderDay;

	private Order? _buyStopOrder;
	private Order? _sellStopOrder;

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal? _lastPrice;

	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	private DateTimeOffset? _positionEntryTime;

	/// <summary>
	/// Candle type used to monitor the previous session range.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in base points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in base points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing slope coefficient. Values below one enable trailing behaviour.
	/// </summary>
	public decimal TrailingSlope
	{
		get => _trailingSlope.Value;
		set => _trailingSlope.Value = value;
	}

	/// <summary>
	/// Defines allowed trade direction: -1 short, 0 both, 1 long.
	/// </summary>
	public int SideFilter
	{
		get => _sideFilter.Value;
		set => _sideFilter.Value = value;
	}

	/// <summary>
	/// Minimum distance from the previous high/low required to arm a stop order.
	/// </summary>
	public decimal ThresholdPoints
	{
		get => _thresholdPoints.Value;
		set => _thresholdPoints.Value = value;
	}

	/// <summary>
	/// Additional offset (in base points) added to the entry to compensate for spread.
	/// </summary>
	public decimal SpreadPoints
	{
		get => _spreadPoints.Value;
		set => _spreadPoints.Value = value;
	}

	/// <summary>
	/// Extra safety buffer (in base points) used when validating minimum stop distances.
	/// </summary>
	public decimal SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	/// <summary>
	/// Hour after which pending orders are cancelled on regular trading days.
	/// </summary>
	public int NoNewOrdersHour
	{
		get => _noNewOrdersHour.Value;
		set => _noNewOrdersHour.Value = value;
	}

	/// <summary>
	/// Hour after which pending orders are cancelled on Fridays.
	/// </summary>
	public int NoNewOrdersHourFriday
	{
		get => _fridayCutoffHour.Value;
		set => _fridayCutoffHour.Value = value;
	}

	/// <summary>
	/// Earliest hour when new pending orders may be submitted.
	/// </summary>
	public int EarliestOrderHour
	{
		get => _earliestOrderHour.Value;
		set => _earliestOrderHour.Value = value;
	}

	/// <summary>
	/// Trading day filter. Use 6 to allow all days or 0-5 to trade Sunday-Friday respectively.
	/// </summary>
	public int DayFilter
	{
		get => _dayFilter.Value;
		set => _dayFilter.Value = value;
	}

	/// <summary>
	/// Time based exit in seconds. Zero disables the timer.
	/// </summary>
	public int CloseAfterSeconds
	{
		get => _closeAfterSeconds.Value;
		set => _closeAfterSeconds.Value = value;
	}

	/// <summary>
	/// Percentage of accumulated profit used for position sizing.
	/// </summary>
	public decimal PercentOfProfit
	{
		get => _percentOfProfit.Value;
		set => _percentOfProfit.Value = value;
	}

	/// <summary>
	/// Minimum allowed trade volume.
	/// </summary>
	public decimal MinVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

	/// <summary>
	/// Maximum allowed trade volume.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Maximum permitted equity drawdown in percent before arming new orders.
	/// </summary>
	public decimal MaximumDrawdownPercent
	{
		get => _maximumDrawdownPercent.Value;
		set => _maximumDrawdownPercent.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DailyStpEntryFrameStrategy"/>.
	/// </summary>
	public DailyStpEntryFrameStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Time-frame used to monitor the previous session range", "General");

		_stopLossPoints = Param(nameof(StopLossPoints), 8m)
			.SetNotNegative()
			.SetDisplay("Stop-Loss (points)", "Stop-loss distance in base points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20.5m)
			.SetNotNegative()
			.SetDisplay("Take-Profit (points)", "Take-profit distance in base points", "Risk");

		_trailingSlope = Param(nameof(TrailingSlope), 0.8m)
			.SetNotNegative()
			.SetDisplay("Trailing Slope", "Portion of profit retained when trailing is active", "Risk");

		_sideFilter = Param(nameof(SideFilter), (int)EntrySide.Short)
			.SetDisplay("Side Filter", "Allowed entry direction (-1 short, 0 both, 1 long)", "Signal");

		_thresholdPoints = Param(nameof(ThresholdPoints), 5m)
			.SetNotNegative()
			.SetDisplay("Threshold (points)", "Minimum distance from the previous extreme before arming", "Signal");

		_spreadPoints = Param(nameof(SpreadPoints), 3m)
			.SetNotNegative()
			.SetDisplay("Spread (points)", "Entry offset that compensates for the spread", "Signal");

		_slippagePoints = Param(nameof(SlippagePoints), 3m)
			.SetNotNegative()
			.SetDisplay("Slippage Buffer", "Additional safety distance for stop validation", "Signal");

		_noNewOrdersHour = Param(nameof(NoNewOrdersHour), 19)
			.SetDisplay("Cutoff Hour", "Hour to cancel pending orders on regular days", "Timing");

		_fridayCutoffHour = Param(nameof(NoNewOrdersHourFriday), 19)
			.SetDisplay("Friday Cutoff", "Hour to cancel pending orders on Fridays", "Timing");

		_earliestOrderHour = Param(nameof(EarliestOrderHour), 0)
			.SetDisplay("Earliest Hour", "Earliest time to arm pending orders", "Timing");

		_dayFilter = Param(nameof(DayFilter), 6)
			.SetDisplay("Day Filter", "6 for all days, or 0-5 to target Sunday-Friday", "Timing");

		_closeAfterSeconds = Param(nameof(CloseAfterSeconds), 0)
			.SetNotNegative()
			.SetDisplay("Close After (s)", "Optional position lifetime in seconds", "Risk");

		_percentOfProfit = Param(nameof(PercentOfProfit), 30m)
			.SetNotNegative()
			.SetDisplay("Percent Of Profit", "Portion of accumulated profit used for sizing", "Money Management");

		_minVolume = Param(nameof(MinVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Volume", "Lower bound for order size", "Money Management");

		_maxVolume = Param(nameof(MaxVolume), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Maximum Volume", "Upper bound for order size", "Money Management");

		_maximumDrawdownPercent = Param(nameof(MaximumDrawdownPercent), 50m)
			.SetNotNegative()
			.SetDisplay("Max Drawdown (%)", "Disable new orders after this drawdown", "Money Management");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_stopLossOffset = 0m;
		_takeProfitOffset = 0m;
		_thresholdOffset = 0m;
		_spreadOffset = 0m;
		_slippageOffset = 0m;

		_startEquity = 0m;
		_maxEquity = 0m;

		_previousDayHigh = null;
		_previousDayLow = null;

		_currentTradingDay = null;
		_todayOpenPrice = null;
		_lastBuyOrderDay = null;
		_lastSellOrderDay = null;

		_buyStopOrder = null;
		_sellStopOrder = null;

		_bestBid = null;
		_bestAsk = null;
		_lastPrice = null;

		_longTrailingStop = null;
		_shortTrailingStop = null;

		_positionEntryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
			throw new InvalidOperationException("Security must be assigned before starting the strategy.");

		_pipSize = Security.PriceStep ?? 0.0001m;
		if (Security.Decimals is 3 or 5)
			_pipSize *= 10m;

		_stopLossOffset = StopLossPoints * _pipSize;
		_takeProfitOffset = TakeProfitPoints * _pipSize;
		_thresholdOffset = ThresholdPoints * _pipSize;
		_spreadOffset = SpreadPoints * _pipSize * 0.5m;
		_slippageOffset = SlippagePoints * _pipSize;

		_startEquity = Portfolio?.CurrentValue ?? 0m;
		_maxEquity = _startEquity;

		if (Volume <= 0m)
			Volume = MinVolume;

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
			.Bind(ProcessDailyCandle)
			.Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_previousDayHigh = candle.HighPrice;
		_previousDayLow = candle.LowPrice;
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_bestBid = (decimal)bid!;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_bestAsk = (decimal)ask!;

		if (level1.Changes.TryGetValue(Level1Fields.LastTradePrice, out var last))
			_lastPrice = (decimal)last!;

		var time = level1.ServerTime != default ? level1.ServerTime : CurrentTime;
		if (time == default)
			return;

		var midPrice = _lastPrice ?? (_bestBid.HasValue && _bestAsk.HasValue ? (_bestBid.Value + _bestAsk.Value) / 2m : _bestBid ?? _bestAsk);

		UpdateTradingDay(time, midPrice);
		CancelExpiredPendingOrders(time);
		ManagePositionLifetime(time);
		ManageTrailingStops();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		TryPlaceStopOrders(time, midPrice);
	}

	private void UpdateTradingDay(DateTimeOffset time, decimal? price)
	{
		var date = time.Date;

		if (_currentTradingDay != date)
		{
			_currentTradingDay = date;
			_todayOpenPrice = price;
			_lastBuyOrderDay = null;
			_lastSellOrderDay = null;

			CancelIfActive(ref _buyStopOrder);
			CancelIfActive(ref _sellStopOrder);
		}
		else if (!_todayOpenPrice.HasValue && price.HasValue)
		{
			_todayOpenPrice = price;
		}
	}

	private void CancelExpiredPendingOrders(DateTimeOffset time)
	{
		var cutoff = time.DayOfWeek == DayOfWeek.Friday ? NoNewOrdersHourFriday : NoNewOrdersHour;
		if (cutoff >= 0 && time.Hour >= cutoff)
		{
			CancelIfActive(ref _buyStopOrder);
			CancelIfActive(ref _sellStopOrder);
		}
	}

	private void ManagePositionLifetime(DateTimeOffset time)
	{
		if (CloseAfterSeconds <= 0 || Position == 0m || _positionEntryTime == null)
			return;

		if (time - _positionEntryTime.Value >= TimeSpan.FromSeconds(CloseAfterSeconds))
		{
			if (Position > 0m)
				SellMarket(Position);
			else if (Position < 0m)
				BuyMarket(-Position);

			_positionEntryTime = null;
			_longTrailingStop = null;
			_shortTrailingStop = null;
		}
	}

	private void ManageTrailingStops()
	{
		if (Position == 0m)
			return;

		if (Position > 0m)
		{
			if (_bestBid is not decimal bid || bid <= 0m)
				return;

			var entryPrice = Position.AveragePrice;
			var baseStop = _stopLossOffset > 0m ? entryPrice - _stopLossOffset : (decimal?)null;

			if (_longTrailingStop.HasValue)
				baseStop = baseStop.HasValue ? Math.Max(baseStop.Value, _longTrailingStop.Value) : _longTrailingStop;

			if (baseStop.HasValue && bid <= baseStop.Value)
			{
				SellMarket(Position);
				_longTrailingStop = null;
				return;
			}

			if (_takeProfitOffset > 0m)
			{
				var target = entryPrice + _takeProfitOffset;
				if (bid >= target)
				{
					SellMarket(Position);
					_longTrailingStop = null;
					return;
				}
			}

			if (TrailingSlope < 1m && _stopLossOffset > 0m && bid > entryPrice)
			{
				var candidate = bid - _stopLossOffset - TrailingSlope * (bid - entryPrice);
				if (!_longTrailingStop.HasValue || candidate > _longTrailingStop.Value + 1.1m * _pipSize)
					_longTrailingStop = candidate;
			}
		}
		else if (Position < 0m)
		{
			if (_bestAsk is not decimal ask || ask <= 0m)
				return;

			var entryPrice = Position.AveragePrice;
			var baseStop = _stopLossOffset > 0m ? entryPrice + _stopLossOffset : (decimal?)null;

			if (_shortTrailingStop.HasValue)
				baseStop = baseStop.HasValue ? Math.Min(baseStop.Value, _shortTrailingStop.Value) : _shortTrailingStop;

			if (baseStop.HasValue && ask >= baseStop.Value)
			{
				BuyMarket(-Position);
				_shortTrailingStop = null;
				return;
			}

			if (_takeProfitOffset > 0m)
			{
				var target = entryPrice - _takeProfitOffset;
				if (ask <= target)
				{
					BuyMarket(-Position);
					_shortTrailingStop = null;
					return;
				}
			}

			if (TrailingSlope < 1m && _stopLossOffset > 0m && ask < entryPrice)
			{
				var candidate = ask + _stopLossOffset + TrailingSlope * (entryPrice - ask);
				if (!_shortTrailingStop.HasValue || candidate < _shortTrailingStop.Value - 1.1m * _pipSize)
					_shortTrailingStop = candidate;
			}
		}
	}

	private void TryPlaceStopOrders(DateTimeOffset time, decimal? price)
	{
		if (!price.HasValue || !_previousDayHigh.HasValue || !_previousDayLow.HasValue || !_todayOpenPrice.HasValue)
			return;

		if (!IsTradingDay(time) || time.Hour < EarliestOrderHour)
			return;

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (_startEquity == 0m)
		{
			_startEquity = equity;
			_maxEquity = equity;
		}

		if (equity > _maxEquity)
			_maxEquity = equity;

		if (MaximumDrawdownPercent > 0m)
		{
			var floor = _maxEquity * (1m - MaximumDrawdownPercent / 100m);
			if (equity < floor)
				return;
		}

		var volume = CalculateOrderVolume(price.Value);
		if (volume <= 0m)
			return;

		var date = time.Date;

		if (AllowsLongEntries() && _lastBuyOrderDay != date && !IsOrderActive(_buyStopOrder))
		{
			var prevHigh = _previousDayHigh.Value;
			var open = _todayOpenPrice.Value;
			var current = price.Value;

			if (prevHigh - current >= _thresholdOffset && open < prevHigh)
			{
				var entryPrice = prevHigh + _spreadOffset;
				var ask = _bestAsk ?? current;
				var minDistance = Math.Max(_pipSize, _slippageOffset);

				if (entryPrice > ask + minDistance)
				{
					_buyStopOrder = BuyStop(volume, entryPrice);
					if (_buyStopOrder != null)
					{
						_lastBuyOrderDay = date;
						LogInfo($"Placed buy stop at {entryPrice:0.#####} for volume {volume:0.###}.");
					}
				}
			}
		}

		if (AllowsShortEntries() && _lastSellOrderDay != date && !IsOrderActive(_sellStopOrder))
		{
			var prevLow = _previousDayLow.Value;
			var open = _todayOpenPrice.Value;
			var current = price.Value;

			if (current - prevLow >= _thresholdOffset && open >= prevLow)
			{
				var entryPrice = prevLow - _spreadOffset;
				var bid = _bestBid ?? current;
				var minDistance = Math.Max(_pipSize, _slippageOffset);

				if (entryPrice < bid - minDistance)
				{
					_sellStopOrder = SellStop(volume, entryPrice);
					if (_sellStopOrder != null)
					{
						_lastSellOrderDay = date;
						LogInfo($"Placed sell stop at {entryPrice:0.#####} for volume {volume:0.###}.");
					}
				}
			}
		}
	}

	private decimal CalculateOrderVolume(decimal referencePrice)
	{
		var volume = MinVolume;

		var equity = Portfolio?.CurrentValue ?? 0m;
		var profit = equity - _startEquity;

		if (profit > 0m && PercentOfProfit > 0m)
		{
			var riskBudget = profit * PercentOfProfit / 100m;
			var tickValue = Security?.StepPrice ?? 1m;
			var lotSize = Security?.LotSize ?? 1m;
			var distance = StopLossPoints + SlippagePoints;
			var riskPerLot = tickValue * distance * _pipSize * lotSize;

			if (riskPerLot > 0m)
			{
				var rawLots = riskBudget / riskPerLot;
				if (rawLots > 0m)
				{
					var sqrtLots = (decimal)Math.Sqrt((double)rawLots) - 0.1m;
					if (sqrtLots > 0m)
						volume = sqrtLots;
				}
			}
		}

		if (volume < MinVolume)
			volume = MinVolume;

		if (volume > MaxVolume)
			volume = MaxVolume;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		return volume;
	}

	private bool AllowsLongEntries()
	{
		return GetEntrySide() != EntrySide.Short;
	}

	private bool AllowsShortEntries()
	{
		return GetEntrySide() != EntrySide.Long;
	}

	private EntrySide GetEntrySide()
	{
		return Enum.IsDefined(typeof(EntrySide), SideFilter) ? (EntrySide)SideFilter : EntrySide.Both;
	}

	private bool IsTradingDay(DateTimeOffset time)
	{
		if (time.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
			return false;

		if (DayFilter is >= 0 and <= 5)
		{
			var requested = (DayOfWeek)DayFilter;
			return time.DayOfWeek == requested;
		}

		return true;
	}

	private static bool IsOrderActive(Order? order)
	{
		if (order == null)
			return false;

		return order.State is OrderStates.None or OrderStates.Pending or OrderStates.Active;
	}

	private void CancelIfActive(ref Order? order)
	{
		if (order == null)
			return;

		if (order.State is OrderStates.Pending or OrderStates.Active)
			CancelOrder(order);

		order = null;
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
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_positionEntryTime = null;
			_longTrailingStop = null;
			_shortTrailingStop = null;
			return;
		}

		_positionEntryTime = CurrentTime;

		if (Position > 0m)
		{
			_longTrailingStop = _stopLossOffset > 0m ? Position.AveragePrice - _stopLossOffset : (decimal?)null;
			_shortTrailingStop = null;
		}
		else if (Position < 0m)
		{
			_shortTrailingStop = _stopLossOffset > 0m ? Position.AveragePrice + _stopLossOffset : (decimal?)null;
			_longTrailingStop = null;
		}
	}
}
