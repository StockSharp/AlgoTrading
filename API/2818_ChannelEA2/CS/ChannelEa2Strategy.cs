using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that places stop orders at the extremes of the intraday channel.
/// </summary>
public class ChannelEa2Strategy : Strategy
{
	private readonly StrategyParam<int> _beginHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopBufferMultiplier;

	private DateTimeOffset? _sessionStart;
	private decimal? _sessionHigh;
	private decimal? _sessionLow;
	private decimal? _entryHigh;
	private decimal? _entryLow;
	private bool _ordersPlaced;
	private DateTimeOffset? _prevCandleTime;
	private DateTimeOffset? _prevPrevCandleTime;
	private Order _longEntryOrder;
	private Order _shortEntryOrder;
	private Order _protectiveOrder;
	private decimal? _longStopLevel;
	private decimal? _shortStopLevel;

	/// <summary>
	/// Trading session start hour.
	/// </summary>
	public int BeginHour
	{
		get => _beginHour.Value;
		set => _beginHour.Value = value;
	}

	/// <summary>
	/// Trading session end hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for channel detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of price steps added as a buffer to entry and protective orders.
	/// </summary>
	public decimal StopBufferMultiplier
	{
		get => _stopBufferMultiplier.Value;
		set => _stopBufferMultiplier.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ChannelEa2Strategy"/> class.
	/// </summary>
	public ChannelEa2Strategy()
	{
		_beginHour = Param(nameof(BeginHour), 1)
			.SetDisplay("Begin Hour", "Hour when the session resets", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0, 23, 1);

		_endHour = Param(nameof(EndHour), 10)
			.SetDisplay("End Hour", "Hour when breakout orders are scheduled", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0, 23, 1);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Volume", "Order volume", "Trading")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for the channel", "General");

		_stopBufferMultiplier = Param(nameof(StopBufferMultiplier), 2m)
			.SetDisplay("Stop Buffer", "Price step multiplier for safety offsets", "Risk")
			.SetGreaterOrEqual(0m);
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

		_sessionStart = null;
		_sessionHigh = null;
		_sessionLow = null;
		_entryHigh = null;
		_entryLow = null;
		_ordersPlaced = false;
		_prevCandleTime = null;
		_prevPrevCandleTime = null;
		_longEntryOrder = null;
		_shortEntryOrder = null;
		_protectiveOrder = null;
		_longStopLevel = null;
		_shortStopLevel = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openTime = candle.OpenTime;
		var sessionCandidate = GetPotentialSessionStart(openTime);

		if ((_sessionStart is null || sessionCandidate > _sessionStart) &&
			(_prevCandleTime is null || _prevCandleTime < sessionCandidate) &&
			openTime >= sessionCandidate)
		{
			StartNewSession(sessionCandidate, candle);
		}

		if (_sessionStart.HasValue)
		{
			var sessionEnd = GetSessionEnd(_sessionStart.Value);

			var shouldPlaceOrders = !_ordersPlaced &&
				_prevCandleTime.HasValue &&
				_prevPrevCandleTime.HasValue &&
				_prevCandleTime.Value >= sessionEnd &&
				_prevPrevCandleTime.Value < sessionEnd;

			if (shouldPlaceOrders && IsFormedAndOnlineAndAllowTrading())
				PlaceBreakoutOrders();

			if (!_ordersPlaced && openTime >= _sessionStart.Value)
				UpdateSessionRange(candle);
		}

		_prevPrevCandleTime = _prevCandleTime;
		_prevCandleTime = openTime;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0)
		{
			if (_shortEntryOrder?.State == OrderStates.Active)
				CancelOrder(_shortEntryOrder);

			RegisterProtectiveOrder(true);
		}
		else if (Position < 0)
		{
			if (_longEntryOrder?.State == OrderStates.Active)
				CancelOrder(_longEntryOrder);

			RegisterProtectiveOrder(false);
		}
		else
		{
			CancelOrderIfActive(ref _protectiveOrder);
			_longEntryOrder = null;
			_shortEntryOrder = null;
		}
	}

	private void StartNewSession(DateTimeOffset sessionStart, ICandleMessage candle)
	{
		CancelActiveOrders();

		if (Position != 0)
			ClosePosition();

		CancelOrderIfActive(ref _protectiveOrder);

		_sessionStart = sessionStart;
		_sessionHigh = candle.HighPrice;
		_sessionLow = candle.LowPrice;
		_entryHigh = null;
		_entryLow = null;
		_ordersPlaced = false;
		_longEntryOrder = null;
		_shortEntryOrder = null;
		_longStopLevel = null;
		_shortStopLevel = null;
	}

	private void UpdateSessionRange(ICandleMessage candle)
	{
		if (_sessionHigh is null || candle.HighPrice > _sessionHigh)
			_sessionHigh = candle.HighPrice;

		if (_sessionLow is null || candle.LowPrice < _sessionLow)
			_sessionLow = candle.LowPrice;
	}

	private void PlaceBreakoutOrders()
	{
		if (_sessionHigh is not decimal high ||
			_sessionLow is not decimal low ||
			high <= low ||
			TradeVolume <= 0m)
		{
			_ordersPlaced = true;
			return;
		}

		CancelActiveOrders();

		_entryHigh = high;
		_entryLow = low;
		_longStopLevel = low;
		_shortStopLevel = high;

		var buyPrice = AdjustActivationPrice(high, true);
		var sellPrice = AdjustActivationPrice(low, false);

		_longEntryOrder = BuyStop(TradeVolume, buyPrice);
		_shortEntryOrder = SellStop(TradeVolume, sellPrice);

		_ordersPlaced = true;
	}

	private void RegisterProtectiveOrder(bool isLong)
	{
		var stopLevel = isLong ? _longStopLevel : _shortStopLevel;
		if (stopLevel is not decimal baseStop)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var stopPrice = AdjustStopPrice(baseStop, isLong);
		if (stopPrice <= 0m)
			return;

		CancelOrderIfActive(ref _protectiveOrder);

		_protectiveOrder = isLong
			? SellStop(volume, stopPrice)
			: BuyStop(volume, stopPrice);
	}

	private void CancelOrderIfActive(ref Order order)
	{
		if (order?.State == OrderStates.Active)
			CancelOrder(order);

		order = null;
	}

	private DateTimeOffset GetPotentialSessionStart(DateTimeOffset time)
	{
		var candidate = new DateTimeOffset(time.Year, time.Month, time.Day, BeginHour, 0, 0, time.Offset);

		if (BeginHour <= EndHour)
		{
			if (time < candidate)
				candidate = candidate.AddDays(-1);
		}
		else
		{
			if (time.Hour < BeginHour)
				candidate = candidate.AddDays(-1);
		}

		return candidate;
	}

	private DateTimeOffset GetSessionEnd(DateTimeOffset sessionStart)
	{
		var endDate = sessionStart.Date;
		if (BeginHour > EndHour)
			endDate = endDate.AddDays(1);

		return new DateTimeOffset(endDate.Year, endDate.Month, endDate.Day, EndHour, 0, 0, sessionStart.Offset);
	}

	private decimal AdjustActivationPrice(decimal price, bool isBuy)
	{
		var buffer = GetPriceBuffer();
		return isBuy ? price + buffer : Math.Max(price - buffer, 0m);
	}

	private decimal AdjustStopPrice(decimal price, bool isLong)
	{
		var buffer = GetPriceBuffer();
		var adjusted = isLong ? price - buffer : price + buffer;
		return adjusted > 0m ? adjusted : 0m;
	}

	private decimal GetPriceBuffer()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m || StopBufferMultiplier <= 0m)
			return 0m;

		return step * StopBufferMultiplier;
	}
}
