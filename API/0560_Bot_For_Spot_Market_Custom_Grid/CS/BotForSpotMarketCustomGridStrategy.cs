using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bot for Spot Market - Custom Grid strategy.
/// </summary>
public class BotForSpotMarketCustomGridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderValue;
	private readonly StrategyParam<decimal> _minAmountMovement;
	private readonly StrategyParam<int> _rounding;
	private readonly StrategyParam<decimal> _nextEntryPercent;
	private readonly StrategyParam<decimal> _profitPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;

	private decimal _lastEntryPrice;
	private decimal _avgPrice;
	private decimal _positionVolume;
	private bool _initialOrderSent;

	/// <summary>
	/// Order value in asset currency.
	/// </summary>
	public decimal OrderValue { get => _orderValue.Value; set => _orderValue.Value = value; }

	/// <summary>
	/// Minimum amount movement.
	/// </summary>
	public decimal MinAmountMovement { get => _minAmountMovement.Value; set => _minAmountMovement.Value = value; }

	/// <summary>
	/// Number of decimal places for rounding.
	/// </summary>
	public int Rounding { get => _rounding.Value; set => _rounding.Value = value; }

	/// <summary>
	/// Percentage drop from last entry to place next buy.
	/// </summary>
	public decimal NextEntryPercent { get => _nextEntryPercent.Value; set => _nextEntryPercent.Value = value; }

	/// <summary>
	/// Profit target percentage from average entry price.
	/// </summary>
	public decimal ProfitPercent { get => _profitPercent.Value; set => _profitPercent.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Start time for trading.
	/// </summary>
	public DateTimeOffset StartTime { get => _startTime.Value; set => _startTime.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="BotForSpotMarketCustomGridStrategy"/>.
	/// </summary>
	public BotForSpotMarketCustomGridStrategy()
	{
		_orderValue = Param(nameof(OrderValue), 10m)
			.SetDisplay("Order Value", "Desired order value", "Parameters");

		_minAmountMovement = Param(nameof(MinAmountMovement), 0.00001m)
			.SetDisplay("Min Amount Movement", "Minimum allowed amount movement", "Parameters");

		_rounding = Param(nameof(Rounding), 5)
			.SetDisplay("Rounding", "Decimal places for rounding", "Parameters");

		_nextEntryPercent = Param(nameof(NextEntryPercent), 0.5m)
			.SetDisplay("Next Entry Less Than (%)", "Price drop from last entry to add new order", "Parameters");

		_profitPercent = Param(nameof(ProfitPercent), 2m)
			.SetDisplay("Profit (%)", "Profit target from average price", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_startTime = Param(nameof(StartTime), new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Time", "Time to begin trading", "Time");
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
		_lastEntryPrice = 0;
		_avgPrice = 0;
		_positionVolume = 0;
		_initialOrderSent = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.OpenTime < StartTime)
			return;

		var price = candle.ClosePrice;
		var quantity = GetQuantity(price);

		if (Position == 0 && !_initialOrderSent)
		{
			BuyMarket(quantity);
			_initialOrderSent = true;
			return;
		}

		if (Position > 0 && price < _lastEntryPrice * (1 - NextEntryPercent / 100m))
		{
			BuyMarket(quantity);
			return;
		}

		if (Position > 0)
		{
			var openPnL = Position * (price - _avgPrice);
			var target = _avgPrice * (1 + ProfitPercent / 100m);

			if (openPnL > 0 && price > target)
				SellMarket(Position);
		}
	}

	private decimal GetQuantity(decimal closePrice)
	{
		var price = OrderValue / closePrice;
		var round = Math.Round(price, Rounding);
		var quant = round >= price ? round + MinAmountMovement : round + (MinAmountMovement * 2m);
		return quant;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		if (trade.Order == null)
			return;

		if (trade.Order.Side == Sides.Buy)
		{
			var newVolume = _positionVolume + trade.Trade.Volume;
			_avgPrice = (_avgPrice * _positionVolume + trade.Trade.Price * trade.Trade.Volume) / newVolume;
			_positionVolume = newVolume;
			_lastEntryPrice = trade.Trade.Price;
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			_positionVolume -= trade.Trade.Volume;
			if (_positionVolume <= 0)
			{
				_positionVolume = 0;
				_avgPrice = 0;
				_lastEntryPrice = 0;
				_initialOrderSent = false;
			}
		}
	}
}
