using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bot for Spot Market - Custom Grid strategy.
/// Long only: buys an OrderValue worth of the asset as soon as it is flat, adds another OrderValue whenever the close drops
/// NextEntryPercent below the last entry price, and sells the whole position once the close exceeds the average entry price
/// by ProfitPercent.
/// </summary>
public class BotForSpotMarketCustomGridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderValue;
	private readonly StrategyParam<decimal> _minAmountMovement;
	private readonly StrategyParam<int> _rounding;
	private readonly StrategyParam<decimal> _nextEntryPercent;
	private readonly StrategyParam<decimal> _profitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastEntryPrice;
	private decimal _avgPrice;
	private decimal _boughtVolume;

	/// <summary>
	/// Value of each order in quote currency.
	/// </summary>
	public decimal OrderValue
	{
		get => _orderValue.Value;
		set => _orderValue.Value = value;
	}

	/// <summary>
	/// Smallest amount increment added to the rounded quantity.
	/// </summary>
	public decimal MinAmountMovement
	{
		get => _minAmountMovement.Value;
		set => _minAmountMovement.Value = value;
	}

	/// <summary>
	/// Decimal places the quantity is rounded to.
	/// </summary>
	public int Rounding
	{
		get => _rounding.Value;
		set => _rounding.Value = value;
	}

	/// <summary>
	/// Drop below the last entry price, in percent, that adds a new order.
	/// </summary>
	public decimal NextEntryPercent
	{
		get => _nextEntryPercent.Value;
		set => _nextEntryPercent.Value = value;
	}

	/// <summary>
	/// Rise above the average entry price, in percent, that closes the position.
	/// </summary>
	public decimal ProfitPercent
	{
		get => _profitPercent.Value;
		set => _profitPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BotForSpotMarketCustomGridStrategy()
	{
		_orderValue = Param(nameof(OrderValue), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Order Value", "Value of each order in quote currency", "Parameters");

		_minAmountMovement = Param(nameof(MinAmountMovement), 0.00001m)
			.SetNotNegative()
			.SetDisplay("Min Amount Movement", "Smallest amount increment added to the rounded quantity", "Parameters");

		_rounding = Param(nameof(Rounding), 5)
			.SetNotNegative()
			.SetDisplay("Rounding", "Decimal places the quantity is rounded to", "Parameters");

		_nextEntryPercent = Param(nameof(NextEntryPercent), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Next Entry Less Than (%)", "Drop below the last entry price that adds a new order", "Parameters");

		_profitPercent = Param(nameof(ProfitPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Profit (%)", "Rise above the average entry price that closes the position", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetGrid();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetGrid();

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

	private void ResetGrid()
	{
		_lastEntryPrice = 0;
		_avgPrice = 0;
		_boughtVolume = 0;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;

		if (Position <= 0)
		{
			BuyMarket(GetQuantity(price));
			return;
		}

		if (_lastEntryPrice > 0 && price < _lastEntryPrice * (1m - NextEntryPercent / 100m))
		{
			BuyMarket(GetQuantity(price));
			return;
		}

		if (_avgPrice > 0 && price > _avgPrice * (1m + ProfitPercent / 100m))
			SellMarket(Position);
	}

	private decimal GetQuantity(decimal price)
	{
		var raw = OrderValue / price;
		var rounded = Math.Round(raw, Rounding);
		var quantity = rounded >= raw ? rounded + MinAmountMovement : rounded + MinAmountMovement * 2m;

		// The instrument cannot accept less than its minimum volume or a fraction of its volume step.
		if (Security?.VolumeStep is decimal step && step > 0)
			quantity = Math.Ceiling(quantity / step) * step;

		if (Security?.MinVolume is decimal minVolume && quantity < minVolume)
			quantity = minVolume;

		return quantity;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null || trade.Trade == null)
			return;

		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;

		if (trade.Order.Side == Sides.Buy)
		{
			var newVolume = _boughtVolume + volume;
			if (newVolume > 0)
				_avgPrice = (_avgPrice * _boughtVolume + price * volume) / newVolume;

			_boughtVolume = newVolume;
			_lastEntryPrice = price;
		}
		else if (Position <= 0)
		{
			ResetGrid();
		}
	}
}
