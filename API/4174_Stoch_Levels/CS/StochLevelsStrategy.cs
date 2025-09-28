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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily range based pending order strategy converted from the MQL4 script "Stoch.mq4".
/// Places limit orders each session using the previous candle range and manages bracket exits.
/// </summary>
public class StochLevelsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _rangeMultiplier;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private Order _buyLimitOrder;
	private Order _sellLimitOrder;
	private Order _protectiveStopOrder;
	private Order _takeProfitOrder;
	private DateTime? _lastProcessedDay;
	private decimal? _buyStopLossPrice;
	private decimal? _buyTakeProfitPrice;
	private decimal? _sellStopLossPrice;
	private decimal? _sellTakeProfitPrice;

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the previous candle range.
	/// </summary>
	public decimal RangeMultiplier
	{
		get => _rangeMultiplier.Value;
		set => _rangeMultiplier.Value = value;
	}

	/// <summary>
	/// Order volume for pending entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the daily calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public StochLevelsStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20m)
			.SetDisplay("Take Profit (steps)", "Distance for take profit orders in price steps", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetOptimize(10m, 60m, 10m);

		_stopLossPoints = Param(nameof(StopLossPoints), 40m)
			.SetDisplay("Stop Loss (steps)", "Distance for stop loss orders in price steps", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetOptimize(20m, 100m, 10m);

		_rangeMultiplier = Param(nameof(RangeMultiplier), 1.1m)
			.SetDisplay("Range Multiplier", "Multiplier applied to the previous candle range", "Calculation")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.1m);

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetDisplay("Order Volume", "Volume for pending limit orders", "Trading")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_buyLimitOrder = null;
		_sellLimitOrder = null;
		_protectiveStopOrder = null;
		_takeProfitOrder = null;
		_lastProcessedDay = null;
		_buyStopLossPrice = null;
		_buyTakeProfitPrice = null;
		_sellStopLossPrice = null;
		_sellTakeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.WhenCandlesFinished(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		ResetOrders();
		base.OnStopped();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
			return;

		var volume = trade.Trade.Volume;

		if (trade.Order == _buyLimitOrder)
		{
			CancelOrderIfActive(ref _takeProfitOrder);
			CancelOrderIfActive(ref _protectiveStopOrder);

			if (_buyTakeProfitPrice.HasValue)
				_takeProfitOrder = SellLimit(volume, _buyTakeProfitPrice.Value);

			if (_buyStopLossPrice.HasValue)
				_protectiveStopOrder = SellStop(volume, _buyStopLossPrice.Value);

			_buyLimitOrder = null;

			LogInfo($"Long entry filled at {trade.Trade.Price}. Protective orders placed.");
		}
		else if (trade.Order == _sellLimitOrder)
		{
			CancelOrderIfActive(ref _takeProfitOrder);
			CancelOrderIfActive(ref _protectiveStopOrder);

			if (_sellTakeProfitPrice.HasValue)
				_takeProfitOrder = BuyLimit(volume, _sellTakeProfitPrice.Value);

			if (_sellStopLossPrice.HasValue)
				_protectiveStopOrder = BuyStop(volume, _sellStopLossPrice.Value);

			_sellLimitOrder = null;

			LogInfo($"Short entry filled at {trade.Trade.Price}. Protective orders placed.");
		}
		else if (trade.Order == _takeProfitOrder || trade.Order == _protectiveStopOrder)
		{
			CancelOrderIfActive(ref _takeProfitOrder);
			CancelOrderIfActive(ref _protectiveStopOrder);

			LogInfo($"Position exited at {trade.Trade.Price}.");
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var candleDay = candle.OpenTime.Date;

		if (_lastProcessedDay.HasValue && _lastProcessedDay.Value == candleDay)
			return;

		ResetOrders();

		var range = candle.HighPrice - candle.LowPrice;

		if (range <= 0m)
		{
			_lastProcessedDay = candleDay;
			LogInfo($"Skipped day {candleDay:yyyy-MM-dd} because candle range is non-positive.");
			return;
		}

		var adjustedRange = range * RangeMultiplier;
		var halfRange = adjustedRange / 2m;

		var sellPrice = candle.ClosePrice + halfRange;
		var buyPrice = candle.ClosePrice - halfRange;

		var priceStep = Security?.PriceStep ?? 0m;

		if (priceStep <= 0m)
		{
			priceStep = 1m;
			LogInfo("Security price step is not provided. Fallback to 1.");
		}

		var takeProfitOffset = TakeProfitPoints * priceStep;
		var stopLossOffset = StopLossPoints * priceStep;

		_buyTakeProfitPrice = takeProfitOffset > 0m ? buyPrice + takeProfitOffset : null;
		_buyStopLossPrice = stopLossOffset > 0m ? buyPrice - stopLossOffset : null;
		_sellTakeProfitPrice = takeProfitOffset > 0m ? sellPrice - takeProfitOffset : null;
		_sellStopLossPrice = stopLossOffset > 0m ? sellPrice + stopLossOffset : null;

		if (OrderVolume <= 0m)
		{
			_lastProcessedDay = candleDay;
			LogInfo("Order volume is non-positive. Pending orders are not placed.");
			return;
		}

		_sellLimitOrder = SellLimit(sellPrice, OrderVolume);
		_buyLimitOrder = BuyLimit(buyPrice, OrderVolume);

		_lastProcessedDay = candleDay;

		LogInfo($"Placed new pending orders for {candleDay:yyyy-MM-dd}. Sell limit: {sellPrice}, Buy limit: {buyPrice}.");
	}

	private void ResetOrders()
	{
		CancelOrderIfActive(ref _takeProfitOrder);
		CancelOrderIfActive(ref _protectiveStopOrder);
		CancelOrderIfActive(ref _buyLimitOrder);
		CancelOrderIfActive(ref _sellLimitOrder);

		if (Position != 0m)
		{
			ClosePosition();
			LogInfo("Closing remaining position at session reset.");
		}

		_buyStopLossPrice = null;
		_buyTakeProfitPrice = null;
		_sellStopLossPrice = null;
		_sellTakeProfitPrice = null;
	}

	private void CancelOrderIfActive(ref Order order)
	{
		if (order == null)
			return;

		if (order.State is OrderStates.Active or OrderStates.Pending or OrderStates.Initialized)
			CancelOrder(order);

		order = null;
	}
}
