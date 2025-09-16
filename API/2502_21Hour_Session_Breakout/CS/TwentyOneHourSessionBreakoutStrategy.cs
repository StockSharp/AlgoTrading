using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 21-hour session breakout strategy converted from the original MQL expert advisor.
/// The strategy places stop orders at the beginning of configured sessions and closes everything at their end.
/// </summary>
public class TwentyOneHourSessionBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _firstSessionStartHour;
	private readonly StrategyParam<int> _firstSessionStopHour;
	private readonly StrategyParam<int> _secondSessionStartHour;
	private readonly StrategyParam<int> _secondSessionStopHour;
	private readonly StrategyParam<decimal> _stepPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	private Order? _takeProfitOrder;
	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// First trading window start hour (server time).
	/// </summary>
	public int FirstSessionStartHour
	{
		get => _firstSessionStartHour.Value;
		set => _firstSessionStartHour.Value = value;
	}

	/// <summary>
	/// First trading window stop hour (server time).
	/// </summary>
	public int FirstSessionStopHour
	{
		get => _firstSessionStopHour.Value;
		set => _firstSessionStopHour.Value = value;
	}

	/// <summary>
	/// Second trading window start hour (server time).
	/// </summary>
	public int SecondSessionStartHour
	{
		get => _secondSessionStartHour.Value;
		set => _secondSessionStartHour.Value = value;
	}

	/// <summary>
	/// Second trading window stop hour (server time).
	/// </summary>
	public int SecondSessionStopHour
	{
		get => _secondSessionStopHour.Value;
		set => _secondSessionStopHour.Value = value;
	}

	/// <summary>
	/// Distance from the best price to the stop orders expressed in price steps.
	/// </summary>
	public decimal StepPoints
	{
		get => _stepPoints.Value;
		set => _stepPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the intraday schedule.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TwentyOneHourSessionBreakoutStrategy"/>.
	/// </summary>
	public TwentyOneHourSessionBreakoutStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume used for entries", "Trading")
			.SetCanOptimize(true);

		_firstSessionStartHour = Param(nameof(FirstSessionStartHour), 8)
			.SetDisplay("First Session Start", "Hour of the first trading window start (0-23)", "Schedule")
			.SetCanOptimize(true);

		_firstSessionStopHour = Param(nameof(FirstSessionStopHour), 21)
			.SetDisplay("First Session Stop", "Hour of the first trading window stop (0-23)", "Schedule")
			.SetCanOptimize(true);

		_secondSessionStartHour = Param(nameof(SecondSessionStartHour), 22)
			.SetDisplay("Second Session Start", "Hour of the second trading window start (0-23)", "Schedule")
			.SetCanOptimize(true);

		_secondSessionStopHour = Param(nameof(SecondSessionStopHour), 23)
			.SetDisplay("Second Session Stop", "Hour of the second trading window stop (0-23)", "Schedule")
			.SetCanOptimize(true);

		_stepPoints = Param(nameof(StepPoints), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Step Points", "Distance from price to stop orders in price steps", "Orders")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 40m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Points", "Take-profit distance in price steps", "Orders")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used to drive the trading schedule", "Data");
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

		_buyStopOrder = null;
		_sellStopOrder = null;
		_takeProfitOrder = null;
		_bestBidPrice = null;
		_bestAskPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Validate configured session hours before subscribing to data streams.
		ValidateSchedule();

		if (Security?.PriceStep is null || Security.PriceStep <= 0)
			throw new InvalidOperationException("Security price step must be defined and positive.");

		// Subscribe to timing candles and start streaming updates.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.WhenNew(ProcessCandle)
			.Start();

		// Track best bid/ask quotes to compute stop order activation prices.
		SubscribeOrderBook()
			.Bind(depth =>
			{
				_bestBidPrice = depth.GetBestBid()?.Price ?? _bestBidPrice;
				_bestAskPrice = depth.GetBestAsk()?.Price ?? _bestAskPrice;
			})
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Process only completed candles to avoid duplicate scheduling events.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Keep at most one active position by removing pending entries once we are in the market.
		if (Position != 0)
			CancelEntryOrders();

		var time = candle.OpenTime;
		var hour = time.Hour;
		var minute = time.Minute;

		if (minute != 0)
			return;

		// Execute session start or stop logic exactly on the scheduled hour.
		if (hour == FirstSessionStartHour || hour == SecondSessionStartHour)
		{
			PlaceEntryOrders(candle.ClosePrice);
		}
		else if (hour == FirstSessionStopHour || hour == SecondSessionStopHour)
		{
			CloseActivePositionAndOrders();
		}
	}

	private void PlaceEntryOrders(decimal fallbackPrice)
	{
		if (Volume <= 0)
			return;

		// Guard against missing price step data or stale quotes.
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0)
			return;

		var ask = _bestAskPrice ?? fallbackPrice;
		var bid = _bestBidPrice ?? fallbackPrice;

		if (ask <= 0 || bid <= 0)
			return;

		var stepOffset = StepPoints * priceStep;
		var buyActivation = ask + stepOffset;
		var sellActivation = bid - stepOffset;

		// Replace any existing pending entries before registering new ones.
		CancelEntryOrders();
		CancelTakeProfitOrder();

		_buyStopOrder = BuyStop(Volume, buyActivation);
		_sellStopOrder = SellStop(Volume, sellActivation);
	}

	private void CloseActivePositionAndOrders()
	{
		// Force the strategy into a flat state at the end of each session.
		CancelEntryOrders();
		CancelTakeProfitOrder();

		if (Position > 0)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null)
			return;

		if (trade.Order == _buyStopOrder)
		{
			_buyStopOrder = null;
			CancelEntryOrder(ref _sellStopOrder);
			RegisterTakeProfit(true, trade.Trade.Price, trade.Trade.Volume);
		}
		else if (trade.Order == _sellStopOrder)
		{
			_sellStopOrder = null;
			CancelEntryOrder(ref _buyStopOrder);
			RegisterTakeProfit(false, trade.Trade.Price, trade.Trade.Volume);
		}
		else if (trade.Order == _takeProfitOrder)
		{
			_takeProfitOrder = null;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0)
			return;

		CancelTakeProfitOrder();
	}

	private void RegisterTakeProfit(bool isLong, decimal entryPrice, decimal tradeVolume)
	{
		// Remove any previous protective order before placing a fresh take-profit.
		CancelTakeProfitOrder();

		if (TakeProfitPoints <= 0)
			return;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0)
			return;

		var targetOffset = TakeProfitPoints * priceStep;

		// Use filled volume when available, otherwise fall back to current position or configured volume.
		var volume = Math.Abs(tradeVolume);
		if (volume <= 0)
			volume = Math.Abs(Position);
		if (volume <= 0)
			volume = Volume;

		var targetPrice = isLong
			? entryPrice + targetOffset
			: entryPrice - targetOffset;

		_takeProfitOrder = isLong
			? SellLimit(volume, targetPrice)
			: BuyLimit(volume, targetPrice);
	}

	private void CancelEntryOrders()
	{
		// Helper method to cancel both stop entries.
		CancelEntryOrder(ref _buyStopOrder);
		CancelEntryOrder(ref _sellStopOrder);
	}

	private void CancelEntryOrder(ref Order? order)
	{
		if (order == null)
			return;

		// Cancel only if the order is still active on the exchange.
		if (order.State == OrderStates.Active)
			CancelOrder(order);

		order = null;
	}

	private void CancelTakeProfitOrder()
	{
		if (_takeProfitOrder == null)
			return;

		// Remove pending take-profit when it is no longer relevant.
		if (_takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);

		_takeProfitOrder = null;
	}

	private void ValidateSchedule()
	{
		// Ensure session hours follow the same constraints as the original MQL implementation.
		ValidateHour(FirstSessionStartHour, nameof(FirstSessionStartHour));
		ValidateHour(FirstSessionStopHour, nameof(FirstSessionStopHour));
		ValidateHour(SecondSessionStartHour, nameof(SecondSessionStartHour));
		ValidateHour(SecondSessionStopHour, nameof(SecondSessionStopHour));

		if (FirstSessionStartHour == FirstSessionStopHour)
			throw new InvalidOperationException("First session start hour must not equal the stop hour.");

		if (SecondSessionStartHour == SecondSessionStopHour)
			throw new InvalidOperationException("Second session start hour must not equal the stop hour.");

		if (FirstSessionStartHour >= SecondSessionStartHour)
			throw new InvalidOperationException("First session start hour must be earlier than the second session start hour.");

		if (FirstSessionStopHour >= SecondSessionStopHour)
			throw new InvalidOperationException("First session stop hour must be earlier than the second session stop hour.");

		if (FirstSessionStopHour < FirstSessionStartHour)
			throw new InvalidOperationException("First session stop hour must be greater than the start hour.");

		if (SecondSessionStopHour < SecondSessionStartHour)
			throw new InvalidOperationException("Second session stop hour must be greater than the start hour.");

		if (FirstSessionStartHour < SecondSessionStartHour && SecondSessionStartHour < FirstSessionStopHour)
			throw new InvalidOperationException("Second session start hour must be outside of the first session range.");
	}

	private static void ValidateHour(int hour, string name)
	{
		// Hours are expressed in 24-hour format and must stay inside the daily range.
		if (hour < 0 || hour > 23)
			throw new InvalidOperationException($"{name} must be within 0..23 hours.");
	}
}
