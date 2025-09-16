using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that mirrors the "Last ZZ50" MetaTrader expert.
/// It reads the latest ZigZag pivots and places pending orders at half of the last two legs.
/// </summary>
public class LastZz50Strategy : Strategy
{
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<int> _zigZagDepth;
	private readonly StrategyParam<int> _zigZagDeviation;
	private readonly StrategyParam<int> _zigZagBackstep;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<DayOfWeek> _startDay;
	private readonly StrategyParam<DayOfWeek> _endDay;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<bool> _closeOutsideSession;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(DateTimeOffset Time, decimal Price)> _pivots = new();

	private Order? _orderAb;
	private Order? _orderBc;
	private Order? _stopOrder;

	private decimal? _orderAbPrice;
	private decimal? _orderBcPrice;
	private decimal? _stopPrice;

	private bool _orderAbIsBuy;
	private bool _orderBcIsBuy;
	private bool _stopForLong;

	private decimal? _lastAbPriceA;
	private decimal? _lastAbPriceB;
	private decimal? _lastBcPriceB;
	private decimal? _lastBcPriceC;

	private decimal? _lastStoredA;
	private decimal? _lastStoredB;
	private decimal? _lastStoredC;

	private decimal _pipSize;
	private decimal _volumeStep;

	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	public int ZigZagDepth
	{
		get => _zigZagDepth.Value;
		set => _zigZagDepth.Value = value;
	}

	public int ZigZagDeviation
	{
		get => _zigZagDeviation.Value;
		set => _zigZagDeviation.Value = value;
	}

	public int ZigZagBackstep
	{
		get => _zigZagBackstep.Value;
		set => _zigZagBackstep.Value = value;
	}

	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	public DayOfWeek StartDay
	{
		get => _startDay.Value;
		set => _startDay.Value = value;
	}

	public DayOfWeek EndDay
	{
		get => _endDay.Value;
		set => _endDay.Value = value;
	}

	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	public bool CloseOutsideSession
	{
		get => _closeOutsideSession.Value;
		set => _closeOutsideSession.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public LastZz50Strategy()
	{
		_lotMultiplier = Param(nameof(LotMultiplier), 1m)
			.SetDisplay("Lot Multiplier", "Multiplier applied to minimal tradable volume", "Risk")
			.SetGreaterThanZero();

		_zigZagDepth = Param(nameof(ZigZagDepth), 12)
			.SetDisplay("ZigZag Depth", "Number of bars used to locate pivots", "ZigZag")
			.SetGreaterThanZero();

		_zigZagDeviation = Param(nameof(ZigZagDeviation), 5)
			.SetDisplay("ZigZag Deviation", "Minimal deviation in points to confirm a pivot", "ZigZag")
			.SetGreaterThanZero();

		_zigZagBackstep = Param(nameof(ZigZagBackstep), 3)
			.SetDisplay("ZigZag Backstep", "Minimal distance between neighbouring pivots", "ZigZag")
			.SetGreaterThanZero();

		_trailingStopPips = Param(nameof(TrailingStopPips), 15m)
			.SetDisplay("Trailing Stop (pips)", "Distance of the trailing stop in pips", "Risk")
			.SetGreaterOrEqualZero();

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step (pips)", "Minimal advance before the stop is moved", "Risk")
			.SetGreaterOrEqualZero();

		_startDay = Param(nameof(StartDay), DayOfWeek.Monday)
			.SetDisplay("Start Day", "First weekday that allows trading", "Session");

		_endDay = Param(nameof(EndDay), DayOfWeek.Friday)
			.SetDisplay("End Day", "Last weekday that allows trading", "Session");

		_startTime = Param(nameof(StartTime), new TimeSpan(9, 1, 0))
			.SetDisplay("Start Time", "Intraday time when trading becomes active", "Session");

		_endTime = Param(nameof(EndTime), new TimeSpan(21, 1, 0))
			.SetDisplay("End Time", "Intraday time when trading stops", "Session");

		_closeOutsideSession = Param(nameof(CloseOutsideSession), true)
			.SetDisplay("Close Outside Session", "Close positions and cancel orders outside the session", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used to evaluate the ZigZag", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pivots.Clear();
		_orderAb = null;
		_orderBc = null;
		_stopOrder = null;
		_orderAbPrice = null;
		_orderBcPrice = null;
		_stopPrice = null;
		_lastAbPriceA = null;
		_lastAbPriceB = null;
		_lastBcPriceB = null;
		_lastBcPriceC = null;
		_lastStoredA = null;
		_lastStoredB = null;
		_lastStoredC = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 1m;
		_volumeStep = Security?.VolumeStep ?? 1m;

		var zigZag = new ZigZagIndicator
		{
			Depth = ZigZagDepth,
			Deviation = ZigZagDeviation,
			BackStep = ZigZagBackstep
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(zigZag, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, zigZag);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal zigZagValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateOrderState();

		// Store new ZigZag pivot whenever the indicator confirms it.
		if (zigZagValue != 0m)
			UpdatePivots(candle.OpenTime, zigZagValue);

		var inSession = IsWithinSession(candle.OpenTime);
		if (!inSession)
		{
			if (CloseOutsideSession)
				CloseAllPositions();

			CancelWorkingOrders();
			return;
		}

		UpdateTrailing(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pivots.Count < 3)
			return;

		var priceA = _pivots[^1].Price;
		var priceB = _pivots[^2].Price;
		var priceC = _pivots[^3].Price;

		ManageBcOrder(priceA, priceB, priceC, candle.ClosePrice);
		ManageAbOrder(priceA, priceB, candle.ClosePrice);

		_lastStoredA = priceA;
		_lastStoredB = priceB;
		_lastStoredC = priceC;
	}

	private void ManageBcOrder(decimal priceA, decimal priceB, decimal priceC, decimal currentPrice)
	{
		var midpoint = (priceB + priceC) / 2m;

		// The original expert blocks signals when the last pivot contradicts the midpoint.
		var isValid = (priceC < priceB && priceA > midpoint) || (priceC > priceB && priceA < midpoint);
		if (!isValid)
		{
			CancelBeamOrder(ref _orderBc, ref _orderBcPrice, ref _lastBcPriceB, ref _lastBcPriceC);
			return;
		}

		var isBuy = priceB < priceC;
		if ((isBuy && Position > 0) || (!isBuy && Position < 0))
		{
			CancelBeamOrder(ref _orderBc, ref _orderBcPrice, ref _lastBcPriceB, ref _lastBcPriceC);
			return;
		}

		var volume = GetOrderVolume();
		if (volume <= 0m)
			return;

		var pivotsChanged = _lastBcPriceB != priceB || _lastBcPriceC != priceC;

		if (_orderBc is null)
		{
			if (pivotsChanged || _lastStoredB != priceB || _lastStoredC != priceC)
			{
				_orderBc = PlaceEntryOrder(isBuy, midpoint, currentPrice, volume);
				_orderBcIsBuy = isBuy;
				_orderBcPrice = midpoint;
				_lastBcPriceB = priceB;
				_lastBcPriceC = priceC;
			}
			return;
		}

		if (pivotsChanged)
		{
			CancelOrderIfActive(_orderBc);
			_orderBc = PlaceEntryOrder(isBuy, midpoint, currentPrice, volume);
			_orderBcIsBuy = isBuy;
			_orderBcPrice = midpoint;
			_lastBcPriceB = priceB;
			_lastBcPriceC = priceC;
		}
		else if (_orderBcPrice != midpoint && _orderBc.State == OrderStates.Active)
		{
			CancelOrder(_orderBc);
			_orderBc = PlaceEntryOrder(_orderBcIsBuy, midpoint, currentPrice, volume);
			_orderBcPrice = midpoint;
		}
	}

	private void ManageAbOrder(decimal priceA, decimal priceB, decimal currentPrice)
	{
		var midpoint = (priceA + priceB) / 2m;
		var isBuy = priceA < priceB;

		if ((isBuy && Position > 0) || (!isBuy && Position < 0))
		{
			CancelBeamOrder(ref _orderAb, ref _orderAbPrice, ref _lastAbPriceA, ref _lastAbPriceB);
			return;
		}

		var volume = GetOrderVolume();
		if (volume <= 0m)
			return;

		var pivotsChanged = _lastAbPriceA != priceA || _lastAbPriceB != priceB;

		if (_orderAb is null)
		{
			if (pivotsChanged || _lastStoredA != priceA || _lastStoredB != priceB)
			{
				_orderAb = PlaceEntryOrder(isBuy, midpoint, currentPrice, volume);
				_orderAbIsBuy = isBuy;
				_orderAbPrice = midpoint;
				_lastAbPriceA = priceA;
				_lastAbPriceB = priceB;
			}
			return;
		}

		if (pivotsChanged)
		{
			CancelOrderIfActive(_orderAb);
			_orderAb = PlaceEntryOrder(isBuy, midpoint, currentPrice, volume);
			_orderAbIsBuy = isBuy;
			_orderAbPrice = midpoint;
			_lastAbPriceA = priceA;
			_lastAbPriceB = priceB;
		}
		else if (_orderAbPrice != midpoint && _orderAb.State == OrderStates.Active)
		{
			CancelOrder(_orderAb);
			_orderAb = PlaceEntryOrder(_orderAbIsBuy, midpoint, currentPrice, volume);
			_orderAbPrice = midpoint;
		}
	}

	private Order PlaceEntryOrder(bool isBuy, decimal price, decimal currentPrice, decimal volume)
	{
		var useStop = isBuy ? price > currentPrice : price < currentPrice;
		return useStop
			? PlaceStopOrder(isBuy, price, volume)
			: PlaceLimitOrder(isBuy, price, volume);
	}

	private Order PlaceLimitOrder(bool isBuy, decimal price, decimal volume)
	=> isBuy ? BuyLimit(price, volume) : SellLimit(price, volume);

	private Order PlaceStopOrder(bool isBuy, decimal price, decimal volume)
	=> isBuy ? BuyStop(price, volume) : SellStop(price, volume);

	private void UpdateOrderState()
	{
		if (_orderAb != null && _orderAb.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
		{
			_orderAb = null;
			_orderAbPrice = null;
		}

		if (_orderBc != null && _orderBc.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
		{
			_orderBc = null;
			_orderBcPrice = null;
		}

		if (_stopOrder != null && _stopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Cancelled)
		{
			_stopOrder = null;
			_stopPrice = null;
		}
	}

	private void UpdatePivots(DateTimeOffset time, decimal price)
	{
		// Keep only the most recent ZigZag turning points.
		var index = _pivots.FindIndex(p => p.Time == time);
		if (index >= 0)
		{
			_pivots[index] = (time, price);
		}
		else
		{
			_pivots.Add((time, price));
			if (_pivots.Count > 200)
				_pivots.RemoveAt(0);
		}
	}

	private bool IsWithinSession(DateTimeOffset time)
	{
		var day = time.DayOfWeek;
		if (day < StartDay || day > EndDay)
			return false;

		var tod = time.TimeOfDay;
		if (tod < StartTime || tod > EndTime)
			return false;

		if (tod.Hours == StartTime.Hours && tod.Minutes < StartTime.Minutes)
			return false;

		if (tod.Hours == EndTime.Hours && tod.Minutes > EndTime.Minutes)
			return false;

		return true;
	}

	private void CancelWorkingOrders()
	{
		CancelBeamOrder(ref _orderAb, ref _orderAbPrice, ref _lastAbPriceA, ref _lastAbPriceB);
		CancelBeamOrder(ref _orderBc, ref _orderBcPrice, ref _lastBcPriceB, ref _lastBcPriceC);
		ResetStop();
	}

	private void CancelBeamOrder(ref Order? order, ref decimal? storedPrice, ref decimal? firstPivot, ref decimal? secondPivot)
	{
		if (order != null)
		{
			CancelOrderIfActive(order);
			order = null;
		}

		storedPrice = null;
		firstPivot = null;
		secondPivot = null;
	}

	private void CancelOrderIfActive(Order order)
	{
		if (order.State == OrderStates.Active)
			CancelOrder(order);
	}

	private void CloseAllPositions()
	{
		CancelWorkingOrders();

		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		// Move protective stop orders once the trade gains enough profit.
		if (TrailingStopPips <= 0m)
		{
			ResetStop();
			return;
		}

		var positionVolume = Math.Abs(Position);
		if (positionVolume <= 0m)
		{
			ResetStop();
			return;
		}

		var trailingStop = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (Position > 0)
		{
			var profit = candle.ClosePrice - PositionPrice;
			if (profit <= trailingStop + trailingStep)
				return;

			var targetPrice = candle.ClosePrice - trailingStop;
			if (_stopOrder == null || !_stopForLong || _stopPrice is null || _stopPrice < targetPrice - _pipSize / 2m)
			{
				ResetStop();
				_stopOrder = SellStop(targetPrice, positionVolume);
				_stopPrice = targetPrice;
				_stopForLong = true;
			}
		}
		else
		{
			var profit = PositionPrice - candle.ClosePrice;
			if (profit <= trailingStop + trailingStep)
				return;

			var targetPrice = candle.ClosePrice + trailingStop;
			if (_stopOrder == null || _stopForLong || _stopPrice is null || _stopPrice > targetPrice + _pipSize / 2m)
			{
				ResetStop();
				_stopOrder = BuyStop(targetPrice, positionVolume);
				_stopPrice = targetPrice;
				_stopForLong = false;
			}
		}
	}

	private void ResetStop()
	{
		if (_stopOrder != null)
		{
			CancelOrderIfActive(_stopOrder);
			_stopOrder = null;
		}

		_stopPrice = null;
	}

	private decimal GetOrderVolume()
	{
		// Convert the user multiplier to the actual tradable volume.
		var step = _volumeStep <= 0m ? 1m : _volumeStep;
		return LotMultiplier * step;
	}
}
