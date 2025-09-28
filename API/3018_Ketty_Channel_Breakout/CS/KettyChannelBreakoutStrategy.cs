namespace StockSharp.Samples.Strategies;

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

using System.Globalization;

/// <summary>
/// Translates the Ketty breakout system from MQL5 to StockSharp.
/// The strategy builds a morning price channel and reacts to breakouts by
/// submitting stop orders at the channel borders with optional protection orders.
/// </summary>
public class KettyChannelBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _entryVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _channelStartHour;
	private readonly StrategyParam<int> _channelStartMinute;
	private readonly StrategyParam<int> _channelEndHour;
	private readonly StrategyParam<int> _channelEndMinute;
	private readonly StrategyParam<int> _placingStartHour;
	private readonly StrategyParam<int> _placingEndHour;
	private readonly StrategyParam<int> _channelBreakthroughPips;
	private readonly StrategyParam<int> _orderPriceShiftPips;
	private readonly StrategyParam<bool> _visualizeChannel;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDate;
	private bool _dayInitialized;
	private bool _channelWindowActive;
	private bool _channelReady;

	private decimal _channelHigh;
	private decimal _channelLow;
	private decimal _pipSize;

	private decimal _buyEntryPrice;
	private decimal _sellEntryPrice;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _longStopOrder;
	private Order _longTakeProfitOrder;
	private Order _shortStopOrder;
	private Order _shortTakeProfitOrder;

	private decimal? _pendingLongStop;
	private decimal? _pendingLongTake;
	private decimal? _pendingShortStop;
	private decimal? _pendingShortTake;

	private bool _hasPendingOrders;
	private DateTimeOffset? _lastCandleTime;

	/// <summary>
	/// Default order volume.
	/// </summary>
	public decimal EntryVolume
	{
		get => _entryVolume.Value;
		set => _entryVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Channel start hour (0-23).
	/// </summary>
	public int ChannelStartHour
	{
		get => _channelStartHour.Value;
		set => _channelStartHour.Value = value;
	}

	/// <summary>
	/// Channel start minute (0-59).
	/// </summary>
	public int ChannelStartMinute
	{
		get => _channelStartMinute.Value;
		set => _channelStartMinute.Value = value;
	}

	/// <summary>
	/// Channel end hour (0-23).
	/// </summary>
	public int ChannelEndHour
	{
		get => _channelEndHour.Value;
		set => _channelEndHour.Value = value;
	}

	/// <summary>
	/// Channel end minute (0-59).
	/// </summary>
	public int ChannelEndMinute
	{
		get => _channelEndMinute.Value;
		set => _channelEndMinute.Value = value;
	}

	/// <summary>
	/// Hour when pending orders can be created.
	/// </summary>
	public int PlacingStartHour
	{
		get => _placingStartHour.Value;
		set => _placingStartHour.Value = value;
	}

	/// <summary>
	/// Hour when pending orders must be removed.
	/// </summary>
	public int PlacingEndHour
	{
		get => _placingEndHour.Value;
		set => _placingEndHour.Value = value;
	}

	/// <summary>
	/// Breakout distance from the channel borders.
	/// </summary>
	public int ChannelBreakthroughPips
	{
		get => _channelBreakthroughPips.Value;
		set => _channelBreakthroughPips.Value = value;
	}

	/// <summary>
	/// Additional offset applied to the pending order price.
	/// </summary>
	public int OrderPriceShiftPips
	{
		get => _orderPriceShiftPips.Value;
		set => _orderPriceShiftPips.Value = value;
	}

	/// <summary>
	/// Enables drawing the channel on the chart.
	/// </summary>
	public bool VisualizeChannel
	{
		get => _visualizeChannel.Value;
		set => _visualizeChannel.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="KettyChannelBreakoutStrategy"/> class.
	/// </summary>
	public KettyChannelBreakoutStrategy()
	{
		_entryVolume = Param(nameof(EntryVolume), 0.1m)
			.SetDisplay("Volume", "Default order volume", "General")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 35)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 150, 5);

		_takeProfitPips = Param(nameof(TakeProfitPips), 75)
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 5);

		_channelStartHour = Param(nameof(ChannelStartHour), 7)
			.SetDisplay("Channel Start Hour", "Hour when the channel calculation begins", "Channel")
			.SetRange(0, 23);

		_channelStartMinute = Param(nameof(ChannelStartMinute), 0)
			.SetDisplay("Channel Start Minute", "Minute when the channel calculation begins", "Channel")
			.SetRange(0, 59);

		_channelEndHour = Param(nameof(ChannelEndHour), 8)
			.SetDisplay("Channel End Hour", "Hour when the channel calculation ends", "Channel")
			.SetRange(0, 23);

		_channelEndMinute = Param(nameof(ChannelEndMinute), 0)
			.SetDisplay("Channel End Minute", "Minute when the channel calculation ends", "Channel")
			.SetRange(0, 59);

		_placingStartHour = Param(nameof(PlacingStartHour), 8)
			.SetDisplay("Placing Start Hour", "Hour when pending orders may be placed", "Trading Window")
			.SetRange(0, 23);

		_placingEndHour = Param(nameof(PlacingEndHour), 18)
			.SetDisplay("Placing End Hour", "Hour when pending orders are cancelled", "Trading Window")
			.SetRange(0, 23);

		_channelBreakthroughPips = Param(nameof(ChannelBreakthroughPips), 30)
			.SetDisplay("Breakout Distance", "Distance beyond the channel that triggers orders", "Channel")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_orderPriceShiftPips = Param(nameof(OrderPriceShiftPips), 10)
			.SetDisplay("Order Shift", "Offset added to the channel border for the stop order", "Channel")
			.SetCanOptimize(true)
			.SetOptimize(0, 50, 1);

		_visualizeChannel = Param(nameof(VisualizeChannel), true)
			.SetDisplay("Draw Channel", "Draw the detected channel on the chart", "Visual");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for the channel", "General");
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

		_dayInitialized = false;
		_channelWindowActive = false;
		_channelReady = false;
		_channelHigh = 0m;
		_channelLow = 0m;
		_pipSize = 0m;
		_buyEntryPrice = 0m;
		_sellEntryPrice = 0m;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_longStopOrder = null;
		_longTakeProfitOrder = null;
		_shortStopOrder = null;
		_shortTakeProfitOrder = null;
		_pendingLongStop = null;
		_pendingLongTake = null;
		_pendingShortStop = null;
		_pendingShortTake = null;
		_hasPendingOrders = false;
		_lastCandleTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = EntryVolume;
		_pipSize = CalculatePipSize();

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

		if (!_dayInitialized || candle.OpenTime.Date != _currentDate)
			StartNewDay(candle.OpenTime);

		UpdateChannel(candle);

		if (VisualizeChannel && _channelReady && _lastCandleTime != null)
		{
			DrawLine(_lastCandleTime.Value, _channelHigh, candle.OpenTime, _channelHigh);
			DrawLine(_lastCandleTime.Value, _channelLow, candle.OpenTime, _channelLow);
		}

		_lastCandleTime = candle.OpenTime;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		TryHandleTradingWindow(candle);
	}

	private void StartNewDay(DateTimeOffset time)
	{
		CancelPendingOrders();

		if (Position == 0)
			CancelProtectionOrders();

		_currentDate = time.Date;
		_dayInitialized = true;
		_channelWindowActive = false;
		_channelReady = false;
		_channelHigh = 0m;
		_channelLow = 0m;
		_pendingLongStop = null;
		_pendingLongTake = null;
		_pendingShortStop = null;
		_pendingShortTake = null;
		_lastCandleTime = null;
	}

	private void UpdateChannel(ICandleMessage candle)
	{
		var startSeconds = ChannelStartHour * 3600 + ChannelStartMinute * 60;
		var endSeconds = ChannelEndHour * 3600 + ChannelEndMinute * 60;
		var (normalizedStart, normalizedEnd, currentSeconds) = NormalizeWindow(candle.OpenTime, startSeconds, endSeconds);

		if (currentSeconds < normalizedStart)
			return;

		if (currentSeconds > normalizedEnd)
		{
			FinalizeChannel();
			return;
		}

		if (!_channelWindowActive)
		{
			_channelWindowActive = true;
			_channelHigh = candle.HighPrice;
			_channelLow = candle.LowPrice;
		}
		else
		{
			_channelHigh = Math.Max(_channelHigh, candle.HighPrice);
			_channelLow = Math.Min(_channelLow, candle.LowPrice);
		}

		if (currentSeconds == normalizedEnd)
			FinalizeChannel();
	}

	private void TryHandleTradingWindow(ICandleMessage candle)
	{
		var startSeconds = PlacingStartHour * 3600;
		var endSeconds = PlacingEndHour * 3600;
		var (normalizedStart, normalizedEnd, currentSeconds) = NormalizeWindow(candle.OpenTime, startSeconds, endSeconds);

		if (currentSeconds > normalizedEnd)
		{
			if (_hasPendingOrders)
				CancelPendingOrders();

			return;
		}

		if (!_channelReady || currentSeconds < normalizedStart)
			return;

		if (_hasPendingOrders || Position != 0)
			return;

		var breakDistance = ChannelBreakthroughPips * _pipSize;
		var priceShift = OrderPriceShiftPips * _pipSize;

		_buyEntryPrice = _channelHigh + priceShift;
		_sellEntryPrice = _channelLow - priceShift;

		var buyTrigger = candle.LowPrice < _channelLow - breakDistance;
		var sellTrigger = candle.HighPrice > _channelHigh + breakDistance;

		if (EntryVolume <= 0m)
			return;

		if (buyTrigger)
		{
			PlaceBuyStop();
		}
		else if (sellTrigger)
		{
			PlaceSellStop();
		}
	}

	private void PlaceBuyStop()
	{
		CancelPendingOrders();

		_pendingLongStop = StopLossPips > 0 ? _buyEntryPrice - StopLossPips * _pipSize : null;
		_pendingLongTake = TakeProfitPips > 0 ? _buyEntryPrice + TakeProfitPips * _pipSize : null;
		_pendingShortStop = null;
		_pendingShortTake = null;

		_buyStopOrder = BuyStop(EntryVolume, _buyEntryPrice);
		_sellStopOrder = null;
		_hasPendingOrders = true;

		LogInfo($"Registered buy stop at {_buyEntryPrice:0.#####}.");
	}

	private void PlaceSellStop()
	{
		CancelPendingOrders();

		_pendingShortStop = StopLossPips > 0 ? _sellEntryPrice + StopLossPips * _pipSize : null;
		_pendingShortTake = TakeProfitPips > 0 ? _sellEntryPrice - TakeProfitPips * _pipSize : null;
		_pendingLongStop = null;
		_pendingLongTake = null;

		_sellStopOrder = SellStop(EntryVolume, _sellEntryPrice);
		_buyStopOrder = null;
		_hasPendingOrders = true;

		LogInfo($"Registered sell stop at {_sellEntryPrice:0.#####}.");
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			CancelProtectionOrders();
			_pendingLongStop = null;
			_pendingLongTake = null;
			_pendingShortStop = null;
			_pendingShortTake = null;
			_hasPendingOrders = false;
			return;
		}

		if (delta > 0m && Position > 0m)
		{
			_hasPendingOrders = false;
			_buyStopOrder = null;

			var volume = Math.Abs(Position);

			if (_pendingLongStop.HasValue)
				_longStopOrder = SellStop(volume, _pendingLongStop.Value);

			if (_pendingLongTake.HasValue)
				_longTakeProfitOrder = SellLimit(volume, _pendingLongTake.Value);

			_pendingLongStop = null;
			_pendingLongTake = null;
		}
		else if (delta < 0m && Position < 0m)
		{
			_hasPendingOrders = false;
			_sellStopOrder = null;

			var volume = Math.Abs(Position);

			if (_pendingShortStop.HasValue)
				_shortStopOrder = BuyStop(volume, _pendingShortStop.Value);

			if (_pendingShortTake.HasValue)
				_shortTakeProfitOrder = BuyLimit(volume, _pendingShortTake.Value);

			_pendingShortStop = null;
			_pendingShortTake = null;
		}
	}

	private void CancelPendingOrders()
	{
		CancelIfActive(ref _buyStopOrder);
		CancelIfActive(ref _sellStopOrder);
		_hasPendingOrders = false;
	}

	private void CancelProtectionOrders()
	{
		CancelIfActive(ref _longStopOrder);
		CancelIfActive(ref _longTakeProfitOrder);
		CancelIfActive(ref _shortStopOrder);
		CancelIfActive(ref _shortTakeProfitOrder);
	}

	private void CancelIfActive(ref Order order)
	{
		if (order == null)
			return;

		if (order.State == OrderStates.Active)
			CancelOrder(order);

		order = null;
	}

	private void FinalizeChannel()
	{
		if (!_channelWindowActive || _channelReady)
			return;

		_channelReady = true;
	}

	private (int start, int end, int current) NormalizeWindow(DateTimeOffset time, int startSeconds, int endSeconds)
	{
		var currentSeconds = GetSeconds(time);

		if (startSeconds <= endSeconds)
			return (startSeconds, endSeconds, currentSeconds);

		if (currentSeconds < startSeconds)
			currentSeconds += 24 * 3600;

		return (startSeconds, endSeconds + 24 * 3600, currentSeconds);
	}

	private static int GetSeconds(DateTimeOffset time)
	{
		return time.Hour * 3600 + time.Minute * 60 + time.Second;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
			step = 0.0001m;

		var text = step.ToString(CultureInfo.InvariantCulture);
		var separatorIndex = text.IndexOf('.');
		var decimals = separatorIndex >= 0 ? text.Length - separatorIndex - 1 : 0;

		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}
}

