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

using StockSharp.Algo.Candles;

/// <summary>
/// Places breakout stop orders ahead of scheduled news releases.
/// </summary>
public class NewsTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _biasPips;
	private readonly StrategyParam<int> _leadMinutes;
	private readonly StrategyParam<int> _newsYear;
	private readonly StrategyParam<int> _newsMonth;
	private readonly StrategyParam<int> _newsDay;
	private readonly StrategyParam<int> _newsHour;
	private readonly StrategyParam<int> _newsMinute;
	private readonly StrategyParam<DataType> _candleType;

	private bool _ordersPlaced;
	private DateTimeOffset? _activationTime;
	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _stopLossOrder;
	private Order _takeProfitOrder;
	private decimal _buyStopLossPrice;
	private decimal _buyTakeProfitPrice;
	private decimal _sellStopLossPrice;
	private decimal _sellTakeProfitPrice;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public NewsTraderStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Contracts to trade on breakout", "General")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 10)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
			.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 10)
			.SetDisplay("Take Profit (pips)", "Target distance in pips", "Risk")
			.SetNotNegative();

		_biasPips = Param(nameof(BiasPips), 20)
			.SetDisplay("Bias (pips)", "Distance to place pending stop orders", "Entries")
			.SetGreaterThanZero();

		_leadMinutes = Param(nameof(LeadMinutes), 10)
			.SetDisplay("Lead Minutes", "Minutes before news to place orders", "Schedule")
			.SetNotNegative();

		_newsYear = Param(nameof(NewsYear), 2010)
			.SetDisplay("News Year", "Year of scheduled news", "Schedule");

		_newsMonth = Param(nameof(NewsMonth), 3)
			.SetDisplay("News Month", "Month of scheduled news", "Schedule")
			.SetGreaterThanZero();

		_newsDay = Param(nameof(NewsDay), 8)
			.SetDisplay("News Day", "Day of scheduled news", "Schedule")
			.SetGreaterThanZero();

		_newsHour = Param(nameof(NewsHour), 1)
			.SetDisplay("News Hour", "Hour of scheduled news (platform time)", "Schedule")
			.SetNotNegative();

		_newsMinute = Param(nameof(NewsMinute), 30)
			.SetDisplay("News Minute", "Minute of scheduled news", "Schedule")
			.SetNotNegative();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to monitor the clock", "General");
	}

	/// <summary>
	/// Contracts used for each entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss size expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit size expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Offset from the current price in pips used for pending orders.
	/// </summary>
	public int BiasPips
	{
		get => _biasPips.Value;
		set => _biasPips.Value = value;
	}

	/// <summary>
	/// Minutes before the news when orders should be submitted.
	/// </summary>
	public int LeadMinutes
	{
		get => _leadMinutes.Value;
		set => _leadMinutes.Value = value;
	}

	/// <summary>
	/// Scheduled news year.
	/// </summary>
	public int NewsYear
	{
		get => _newsYear.Value;
		set => _newsYear.Value = value;
	}

	/// <summary>
	/// Scheduled news month.
	/// </summary>
	public int NewsMonth
	{
		get => _newsMonth.Value;
		set => _newsMonth.Value = value;
	}

	/// <summary>
	/// Scheduled news day.
	/// </summary>
	public int NewsDay
	{
		get => _newsDay.Value;
		set => _newsDay.Value = value;
	}

	/// <summary>
	/// Scheduled news hour (platform time).
	/// </summary>
	public int NewsHour
	{
		get => _newsHour.Value;
		set => _newsHour.Value = value;
	}

	/// <summary>
	/// Scheduled news minute (platform time).
	/// </summary>
	public int NewsMinute
	{
		get => _newsMinute.Value;
		set => _newsMinute.Value = value;
	}

	/// <summary>
	/// Candle type used to monitor time progression.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_ordersPlaced = false;
		_activationTime = null;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_stopLossOrder = null;
		_takeProfitOrder = null;
		_buyStopLossPrice = 0m;
		_buyTakeProfitPrice = 0m;
		_sellStopLossPrice = 0m;
		_sellTakeProfitPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		if (Security?.PriceStep == null)
			throw new InvalidOperationException("Security must define a price step.");

		Volume = TradeVolume;

		var newsTime = new DateTimeOffset(NewsYear, NewsMonth, NewsDay, NewsHour, NewsMinute, 0, time.Offset);
		_activationTime = newsTime.AddMinutes(-LeadMinutes);

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

		if (_ordersPlaced || _activationTime == null)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Wait until the monitoring candle covers or surpasses the activation time.
		if (candle.OpenTime < _activationTime.Value)
			return;

		PlaceEntryOrders(candle.ClosePrice);
	}

	private void PlaceEntryOrders(decimal referencePrice)
	{
		// Convert pip distances into absolute price offsets.
		var step = Security!.PriceStep!.Value;
		var biasOffset = BiasPips * step;
		var stopOffset = StopLossPips * step;
		var takeOffset = TakeProfitPips * step;

		var buyPrice = referencePrice + biasOffset;
		var sellPrice = referencePrice - biasOffset;

		_buyStopLossPrice = buyPrice - stopOffset;
		_buyTakeProfitPrice = buyPrice + takeOffset;
		_sellStopLossPrice = sellPrice + stopOffset;
		_sellTakeProfitPrice = sellPrice - takeOffset;

		_buyStopOrder = BuyStop(volume: Volume, price: buyPrice);
		_sellStopOrder = SellStop(volume: Volume, price: sellPrice);

		_ordersPlaced = true;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade.Order;
		if (order == null)
			return;

		if (order == _buyStopOrder)
		{
			_buyStopOrder = null;
			CancelOrderSafe(ref _sellStopOrder);
			CreateProtection(isLong: true);
		}
		else if (order == _sellStopOrder)
		{
			_sellStopOrder = null;
			CancelOrderSafe(ref _buyStopOrder);
			CreateProtection(isLong: false);
		}
		else if (order == _stopLossOrder)
		{
			_stopLossOrder = null;
			CancelOrderSafe(ref _takeProfitOrder);
		}
		else if (order == _takeProfitOrder)
		{
			_takeProfitOrder = null;
			CancelOrderSafe(ref _stopLossOrder);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
			CancelProtectionOrders();
	}

	private void CreateProtection(bool isLong)
	{
		CancelProtectionOrders();

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			volume = Volume;

		if (volume <= 0m)
			return;

		if (isLong)
		{
			if (StopLossPips > 0)
				_stopLossOrder = SellStop(price: _buyStopLossPrice, volume: volume);

			if (TakeProfitPips > 0)
				_takeProfitOrder = SellLimit(price: _buyTakeProfitPrice, volume: volume);
		}
		else
		{
			if (StopLossPips > 0)
				_stopLossOrder = BuyStop(price: _sellStopLossPrice, volume: volume);

			if (TakeProfitPips > 0)
				_takeProfitOrder = BuyLimit(price: _sellTakeProfitPrice, volume: volume);
		}
	}

	private void CancelProtectionOrders()
	{
		CancelOrderSafe(ref _stopLossOrder);
		CancelOrderSafe(ref _takeProfitOrder);
	}

	private void CancelOrderSafe(ref Order order)
	{
		if (order == null)
			return;

		if (order.State == OrderStates.Active)
			CancelOrder(order);

		order = null;
	}
}
