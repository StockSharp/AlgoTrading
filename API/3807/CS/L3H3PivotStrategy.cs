using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// L3/H3 pivot trading strategy converted from MQL.
/// Places pending orders around the previous session low/high with a pivot-based take-profit.
/// </summary>
public class L3H3PivotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _entryCandleType;
	private readonly StrategyParam<DataType> _pivotCandleType;
	private readonly StrategyParam<decimal> _entryOffsetPips;
	private readonly StrategyParam<decimal> _stopLossPips;

	private decimal _pipSize;
	private DateTime? _pivotForDate;
	private DateTime? _ordersPlacedDate;

	private decimal? _yesterdayHigh;
	private decimal? _yesterdayLow;
	private decimal? _yesterdayClose;
	private decimal? _yesterdayOpen;
	private decimal? _pivotLevel;

	private Order? _buyEntryOrder;
	private Order? _sellEntryOrder;

	private decimal _lastClosePrice;

	/// <summary>
	/// Main candle series used to trigger order placement.
	/// </summary>
	public DataType EntryCandleType
	{
		get => _entryCandleType.Value;
		set => _entryCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used to collect previous session statistics.
	/// </summary>
	public DataType PivotCandleType
	{
		get => _pivotCandleType.Value;
		set => _pivotCandleType.Value = value;
	}

	/// <summary>
	/// Offset in pips added above the previous low for long entries.
	/// </summary>
	public decimal EntryOffsetPips
	{
		get => _entryOffsetPips.Value;
		set => _entryOffsetPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips from the reference high/low.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="L3H3PivotStrategy"/> class.
	/// </summary>
	public L3H3PivotStrategy()
	{
		_entryCandleType = Param(nameof(EntryCandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Entry Candle Type", "Primary candle series for entry logic", "General");

		_pivotCandleType = Param(nameof(PivotCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Pivot Candle Type", "Timeframe used to measure the previous session", "General");

		_entryOffsetPips = Param(nameof(EntryOffsetPips), 2m)
			.SetDisplay("Entry Offset (pips)", "Distance added to the previous low for buy orders", "Orders")
			.SetCanOptimize(true)
			.SetOptimize(0m, 10m, 1m);

		_stopLossPips = Param(nameof(StopLossPips), 16m)
			.SetDisplay("Stop Loss (pips)", "Distance used to place protective stop orders", "Orders")
			.SetCanOptimize(true)
			.SetOptimize(8m, 40m, 2m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, EntryCandleType), (Security, PivotCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pivotForDate = null;
		_ordersPlacedDate = null;
		_yesterdayHigh = null;
		_yesterdayLow = null;
		_yesterdayClose = null;
		_yesterdayOpen = null;
		_pivotLevel = null;
		_lastClosePrice = 0m;

		CancelEntryOrders();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0m;
		if (_pipSize <= 0m)
			throw new InvalidOperationException("Security price step must be set.");

		var entrySubscription = SubscribeCandles(EntryCandleType);
		entrySubscription
			.Bind(ProcessEntryCandle)
			.Start();

		var pivotSubscription = SubscribeCandles(PivotCandleType);
		pivotSubscription
			.Bind(ProcessPivotCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, entrySubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessPivotCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_yesterdayHigh = candle.HighPrice;
		_yesterdayLow = candle.LowPrice;
		_yesterdayClose = candle.ClosePrice;
		_yesterdayOpen = candle.OpenPrice;
		_pivotLevel = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		_pivotForDate = candle.OpenTime.Date.AddDays(1);
		_ordersPlacedDate = null;

		CancelEntryOrders();

		LogInfo($"Pivot updated for {_pivotForDate:yyyy-MM-dd}. Open={_yesterdayOpen}, High={_yesterdayHigh}, Low={_yesterdayLow}, Close={_yesterdayClose}, Pivot={_pivotLevel}");
	}

	private void ProcessEntryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastClosePrice = candle.ClosePrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pivotForDate is null || _yesterdayHigh is null || _yesterdayLow is null || _pivotLevel is null)
			return;

		var currentDate = candle.OpenTime.Date;
		if (currentDate < _pivotForDate.Value)
			return;

		if (_ordersPlacedDate == currentDate)
			return;

		PlaceDailyOrders();
		_ordersPlacedDate = currentDate;
	}

	private void PlaceDailyOrders()
	{
		if (Volume <= 0m)
			return;

		var low = _yesterdayLow!.Value;
		var high = _yesterdayHigh!.Value;
		var pivot = _pivotLevel!.Value;

		var entryOffset = EntryOffsetPips * _pipSize;
		var stopDistance = StopLossPips * _pipSize;

		var buyPrice = RoundPrice(low + entryOffset);
		var sellPrice = RoundPrice(high);
		var buyStopLoss = RoundPrice(low - stopDistance);
		var sellStopLoss = RoundPrice(high + stopDistance);
		var takeProfit = RoundPrice(pivot);

		if (buyPrice <= 0m || sellPrice <= 0m)
			return;

		CancelEntryOrders();

		if (_lastClosePrice <= low)
		{
			_buyEntryOrder = BuyStop(Volume, buyPrice, stopLoss: buyStopLoss, takeProfit: takeProfit);
			LogInfo($"Placed buy stop at {buyPrice} with SL {buyStopLoss} and TP {takeProfit}.");
		}
		else
		{
			_buyEntryOrder = BuyLimit(Volume, buyPrice, stopLoss: buyStopLoss, takeProfit: takeProfit);
			LogInfo($"Placed buy limit at {buyPrice} with SL {buyStopLoss} and TP {takeProfit}.");
		}

		if (_lastClosePrice >= high)
		{
			_sellEntryOrder = SellStop(Volume, sellPrice, stopLoss: sellStopLoss, takeProfit: takeProfit);
			LogInfo($"Placed sell stop at {sellPrice} with SL {sellStopLoss} and TP {takeProfit}.");
		}
		else
		{
			_sellEntryOrder = SellLimit(Volume, sellPrice, stopLoss: sellStopLoss, takeProfit: takeProfit);
			LogInfo($"Placed sell limit at {sellPrice} with SL {sellStopLoss} and TP {takeProfit}.");
		}
	}

	private void CancelEntryOrders()
	{
		CancelOrder(ref _buyEntryOrder);
		CancelOrder(ref _sellEntryOrder);
	}

	private void CancelOrder(ref Order? order)
	{
		if (order == null)
			return;

		if (order.State == OrderStates.Active)
			CancelOrder(order);

		order = null;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (!order.State.IsFinal())
			return;

		if (_buyEntryOrder != null && order == _buyEntryOrder)
			_buyEntryOrder = null;

		if (_sellEntryOrder != null && order == _sellEntryOrder)
			_sellEntryOrder = null;
	}
}
