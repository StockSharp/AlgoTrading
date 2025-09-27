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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Time-based breakout strategy converted from the "Strategy of Regularities of Exchange Rates" MQL expert advisor.
/// Places symmetric stop orders at a scheduled hour and manages them until the daily closing hour.
/// </summary>
public class RegularitiesOfExchangeRatesStrategy : Strategy
{
	private readonly StrategyParam<int> _openingHour;
	private readonly StrategyParam<int> _closingHour;
	private readonly StrategyParam<decimal> _entryOffsetPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pointSize;
	private DateTime? _lastEntryDate;
	private Order _buyStopOrder;
	private Order _sellStopOrder;

	/// <summary>
	/// Hour (0-23) when the pending stop orders are placed.
	/// </summary>
	public int OpeningHour
	{
		get => _openingHour.Value;
		set => _openingHour.Value = value;
	}

	/// <summary>
	/// Hour (0-23) when the strategy cancels pending orders and exits trades.
	/// </summary>
	public int ClosingHour
	{
		get => _closingHour.Value;
		set => _closingHour.Value = value;
	}

	/// <summary>
	/// Distance in broker points between the reference price and the stop orders.
	/// </summary>
	public decimal EntryOffsetPoints
	{
		get => _entryOffsetPoints.Value;
		set => _entryOffsetPoints.Value = value;
	}

	/// <summary>
	/// Profit target distance measured in broker points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in broker points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Volume of each pending stop order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle series used to evaluate the trading schedule.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Creates the strategy parameters and sets their defaults.
	/// </summary>
	public RegularitiesOfExchangeRatesStrategy()
	{
		_openingHour = Param(nameof(OpeningHour), 9)
			.SetDisplay("Opening Hour", "Hour (0-23) when new pending orders are submitted", "Schedule")
			.SetRange(0, 23);

		_closingHour = Param(nameof(ClosingHour), 2)
			.SetDisplay("Closing Hour", "Hour (0-23) when the strategy exits and cancels", "Schedule")
			.SetRange(0, 23);

		_entryOffsetPoints = Param(nameof(EntryOffsetPoints), 20m)
			.SetDisplay("Entry Offset (points)", "Distance from bid/ask to place stop orders", "Orders")
			.SetGreaterThanZero();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20m)
			.SetDisplay("Take Profit (points)", "Profit target distance measured in broker points", "Risk")
			.SetNotNegative();

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
			.SetDisplay("Stop Loss (points)", "Protective stop distance attached to filled trades", "Risk")
			.SetGreaterThanZero();

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetDisplay("Order Volume", "Volume for each stop order", "Orders")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to evaluate trading hours", "General");
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

		_pointSize = 0m;
		_lastEntryDate = null;
		_buyStopOrder = null;
		_sellStopOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = CalculatePointSize();

		var stopLossUnit = StopLossPoints > 0m
			? new Unit(StopLossPoints * _pointSize, UnitTypes.Absolute)
			: new Unit();

		// Attach a platform-managed protective stop that mirrors the original MQL stop-loss parameter.
		StartProtection(stopLoss: stopLossUnit, useMarketOrders: true);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private decimal CalculatePointSize()
	{
		var step = Security?.PriceStep ?? 1m;

		if (step <= 0m)
			return 1m;

		var digits = 0;
		var temp = step;

		// Count decimals to emulate MetaTrader's Point-to-pip conversion on 3/5 digit quotes.
		while (temp < 1m && digits < 10)
		{
			temp *= 10m;
			digits++;
		}

		if (digits == 3 || digits == 5)
			step *= 10m;

		return step;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var isClosingHour = candle.OpenTime.Hour == ClosingHour;

		// Evaluate the take-profit logic first to avoid missing fast moves.
		ManageTakeProfit(candle.ClosePrice, isClosingHour);

		if (isClosingHour)
		{
			// Closing hour: cancel pending orders and flatten any remaining position.
			CancelPendingOrders();
			CloseActivePosition();
		}

		if (candle.OpenTime.Hour == OpeningHour && ShouldPlaceOrders(candle.OpenTime))
		{
			if (PlacePendingOrders(candle.ClosePrice))
				_lastEntryDate = candle.OpenTime.Date;
		}
	}

	private void ManageTakeProfit(decimal closePrice, bool forceExit)
	{
		if (Position == 0m || _pointSize <= 0m)
			return;

		var takeProfitDistance = TakeProfitPoints * _pointSize;

		if (Position > 0m)
		{
			// Close the long position when the profit target is reached or at the scheduled close hour.
			if (forceExit || (takeProfitDistance > 0m && closePrice - PositionPrice >= takeProfitDistance))
				SellMarket(Position);
		}
		else if (Position < 0m)
		{
			// Close the short position when the profit target is reached or at the scheduled close hour.
			if (forceExit || (takeProfitDistance > 0m && PositionPrice - closePrice >= takeProfitDistance))
				BuyMarket(-Position);
		}
	}

	private void CloseActivePosition()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(-Position);
		}
	}

	private bool ShouldPlaceOrders(DateTimeOffset time)
	{
		var date = time.Date;
		return !_lastEntryDate.HasValue || _lastEntryDate.Value != date;
	}

	private bool PlacePendingOrders(decimal referencePrice)
	{
		CancelPendingOrders();

		if (_pointSize <= 0m || EntryOffsetPoints <= 0m || OrderVolume <= 0m)
			return false;

		var offset = EntryOffsetPoints * _pointSize;

		var bestBid = Security?.BestBid?.Price;
		var bestAsk = Security?.BestAsk?.Price;

		if (bestBid is null || bestBid <= 0m)
			bestBid = referencePrice;

		if (bestAsk is null || bestAsk <= 0m)
			bestAsk = referencePrice;

		var sellPrice = NormalizePrice(bestBid.Value - offset);
		var buyPrice = NormalizePrice(bestAsk.Value + offset);

		if (sellPrice <= 0m || buyPrice <= 0m)
			return false;

		// Register symmetric stop orders around the current spread.
		_sellStopOrder = SellStop(OrderVolume, sellPrice);
		_buyStopOrder = BuyStop(OrderVolume, buyPrice);

		return true;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;

		if (security?.PriceStep > 0m)
			return security.ShrinkPrice(price);

		return price;
	}

	private void CancelPendingOrders()
	{
		if (_buyStopOrder != null)
		{
			CancelOrder(_buyStopOrder);
			_buyStopOrder = null;
		}

		if (_sellStopOrder != null)
		{
			CancelOrder(_sellStopOrder);
			_sellStopOrder = null;
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		CancelPendingOrders();

		base.OnStopped();
	}
}
