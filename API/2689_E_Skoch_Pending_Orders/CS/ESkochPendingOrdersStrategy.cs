using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pending breakout strategy based on the e-Skoch pending orders idea.
/// </summary>
public class ESkochPendingOrdersStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfitBuyPips;
	private readonly StrategyParam<decimal> _stopLossBuyPips;
	private readonly StrategyParam<decimal> _takeProfitSellPips;
	private readonly StrategyParam<decimal> _stopLossSellPips;
	private readonly StrategyParam<decimal> _indentHighPips;
	private readonly StrategyParam<decimal> _indentLowPips;
	private readonly StrategyParam<bool> _checkExistingTrade;
	private readonly StrategyParam<decimal> _percentEquity;

	private decimal? _prevHigh1;
	private decimal? _prevHigh2;
	private decimal? _prevLow1;
	private decimal? _prevLow2;

	private decimal? _prevDailyHigh1;
	private decimal? _prevDailyHigh2;
	private decimal? _prevDailyLow1;
	private decimal? _prevDailyLow2;

	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	private Order? _stopLossOrder;
	private Order? _takeProfitOrder;

	private decimal? _buyStopLossPrice;
	private decimal? _buyTakeProfitPrice;
	private decimal? _sellStopLossPrice;
	private decimal? _sellTakeProfitPrice;

	private decimal _pipValue;
	private decimal _baselineEquity;

	/// <summary>
	/// Main candle type for signal evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Take profit distance for long entries in pips.
	/// </summary>
	public decimal TakeProfitBuyPips
	{
		get => _takeProfitBuyPips.Value;
		set => _takeProfitBuyPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance for long entries in pips.
	/// </summary>
	public decimal StopLossBuyPips
	{
		get => _stopLossBuyPips.Value;
		set => _stopLossBuyPips.Value = value;
	}

	/// <summary>
	/// Take profit distance for short entries in pips.
	/// </summary>
	public decimal TakeProfitSellPips
	{
		get => _takeProfitSellPips.Value;
		set => _takeProfitSellPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance for short entries in pips.
	/// </summary>
	public decimal StopLossSellPips
	{
		get => _stopLossSellPips.Value;
		set => _stopLossSellPips.Value = value;
	}

	/// <summary>
	/// Additional distance above the recent high for buy stop orders in pips.
	/// </summary>
	public decimal IndentHighPips
	{
		get => _indentHighPips.Value;
		set => _indentHighPips.Value = value;
	}

	/// <summary>
	/// Additional distance below the recent low for sell stop orders in pips.
	/// </summary>
	public decimal IndentLowPips
	{
		get => _indentLowPips.Value;
		set => _indentLowPips.Value = value;
	}

	/// <summary>
	/// When enabled the strategy avoids placing new orders if any position exists.
	/// </summary>
	public bool CheckExistingTrade
	{
		get => _checkExistingTrade.Value;
		set => _checkExistingTrade.Value = value;
	}

	/// <summary>
	/// Target profit in percent of equity before closing positions.
	/// </summary>
	public decimal PercentEquity
	{
		get => _percentEquity.Value;
		set => _percentEquity.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ESkochPendingOrdersStrategy"/>.
	/// </summary>
	public ESkochPendingOrdersStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe", "General");

		_takeProfitBuyPips = Param(nameof(TakeProfitBuyPips), 60m)
		.SetGreaterThanZero()
		.SetDisplay("Buy TP (pips)", "Long take profit distance", "Trading");

		_stopLossBuyPips = Param(nameof(StopLossBuyPips), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Buy SL (pips)", "Long stop loss distance", "Trading");

		_takeProfitSellPips = Param(nameof(TakeProfitSellPips), 60m)
		.SetGreaterThanZero()
		.SetDisplay("Sell TP (pips)", "Short take profit distance", "Trading");

		_stopLossSellPips = Param(nameof(StopLossSellPips), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Sell SL (pips)", "Short stop loss distance", "Trading");

		_indentHighPips = Param(nameof(IndentHighPips), 70m)
		.SetGreaterThanZero()
		.SetDisplay("High Indent", "Buy stop offset", "Trading");

		_indentLowPips = Param(nameof(IndentLowPips), 70m)
		.SetGreaterThanZero()
		.SetDisplay("Low Indent", "Sell stop offset", "Trading");

		_checkExistingTrade = Param(nameof(CheckExistingTrade), true)
		.SetDisplay("Block During Position", "Skip signals when a position exists", "Risk");

		_percentEquity = Param(nameof(PercentEquity), 2.2m)
		.SetGreaterThanZero()
		.SetDisplay("Equity Target %", "Close positions after reaching profit", "Risk");

		Volume = 0.01m;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHigh1 = null;
		_prevHigh2 = null;
		_prevLow1 = null;
		_prevLow2 = null;

		_prevDailyHigh1 = null;
		_prevDailyHigh2 = null;
		_prevDailyLow1 = null;
		_prevDailyLow2 = null;

		_buyStopOrder = null;
		_sellStopOrder = null;
		_stopLossOrder = null;
		_takeProfitOrder = null;

		_buyStopLossPrice = null;
		_buyTakeProfitPrice = null;
		_sellStopLossPrice = null;
		_sellTakeProfitPrice = null;

		_pipValue = 1m;
		_baselineEquity = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var priceStep = Security?.PriceStep ?? 0m;
		var decimals = Security?.Decimals ?? 0;
		_pipValue = priceStep <= 0m ? 1m : priceStep;

		if ((decimals == 3 || decimals == 5) && priceStep > 0m)
		{
			// Forex symbols often report points instead of pips, adjust to pip value.
			_pipValue = priceStep * 10m;
		}

		_baselineEquity = Portfolio?.CurrentValue ?? 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessMainCandle).Start();

		SubscribeCandles(TimeSpan.FromDays(1).TimeFrame()).Bind(ProcessDailyCandle).Start();
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_prevDailyHigh1 is null)
		{
			_prevDailyHigh1 = candle.HighPrice;
			_prevDailyLow1 = candle.LowPrice;
			return;
		}

		if (_prevDailyHigh2 is null)
		{
			_prevDailyHigh2 = _prevDailyHigh1;
			_prevDailyLow2 = _prevDailyLow1;
			_prevDailyHigh1 = candle.HighPrice;
			_prevDailyLow1 = candle.LowPrice;
			return;
		}

		_prevDailyHigh2 = _prevDailyHigh1;
		_prevDailyLow2 = _prevDailyLow1;
		_prevDailyHigh1 = candle.HighPrice;
		_prevDailyLow1 = candle.LowPrice;
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_prevHigh1 is null)
		{
			_prevHigh1 = candle.HighPrice;
			_prevLow1 = candle.LowPrice;
			return;
		}

		if (_prevHigh2 is null)
		{
			_prevHigh2 = _prevHigh1;
			_prevLow2 = _prevLow1;
			_prevHigh1 = candle.HighPrice;
			_prevLow1 = candle.LowPrice;
			return;
		}

		UpdateOrderReferences();

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (!HasOpenPositionOrOrders() && equity > 0m)
		{
			// Capture baseline equity when the account is flat.
			_baselineEquity = equity;
		}

		if (_baselineEquity > 0m && PercentEquity > 0m)
		{
			var growthPercent = (equity - _baselineEquity) / _baselineEquity * 100m;
			if (growthPercent >= PercentEquity)
			{
				CloseAllPositions();
				ShiftHistory(candle);
				return;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			ShiftHistory(candle);
			return;
		}

		if (_prevDailyHigh1 is null || _prevDailyHigh2 is null || _prevDailyLow1 is null || _prevDailyLow2 is null)
		{
			ShiftHistory(candle);
			return;
		}

		var hasPosition = Position != 0;
		var hasLong = Position > 0;
		var hasShort = Position < 0;

		// Place buy stop above the recent high when both highs are falling.
		var dailyHighFalling = _prevDailyHigh2 > _prevDailyHigh1;
		var dailyLowRising = _prevDailyLow2 < _prevDailyLow1;
		var intradayHighFalling = _prevHigh2 > _prevHigh1;
		var intradayLowRising = _prevLow2 < _prevLow1;

		if (dailyHighFalling && intradayHighFalling && !hasLong && (!CheckExistingTrade || !hasPosition))
		{
			var buyPrice = NormalizePrice(_prevHigh1.Value + _pipValue * IndentHighPips);
			var stopLoss = StopLossBuyPips > 0m ? NormalizePrice(buyPrice - _pipValue * StopLossBuyPips) : (decimal?)null;
			var takeProfit = TakeProfitBuyPips > 0m ? NormalizePrice(buyPrice + _pipValue * TakeProfitBuyPips) : (decimal?)null;

			if (!IsOrderActive(_buyStopOrder) || buyPrice < (_buyStopOrder?.Price ?? decimal.MaxValue))
			{
				PlaceBuyStop(buyPrice, stopLoss, takeProfit);
			}
		}

		// Place sell stop below the recent low when lows are rising.
		if (dailyLowRising && intradayLowRising && !hasShort && (!CheckExistingTrade || !hasPosition))
		{
			var sellPrice = NormalizePrice(_prevLow1.Value - _pipValue * IndentLowPips);
			var stopLoss = StopLossSellPips > 0m ? NormalizePrice(sellPrice + _pipValue * StopLossSellPips) : (decimal?)null;
			var takeProfit = TakeProfitSellPips > 0m ? NormalizePrice(sellPrice - _pipValue * TakeProfitSellPips) : (decimal?)null;

			if (!IsOrderActive(_sellStopOrder) || sellPrice > (_sellStopOrder?.Price ?? decimal.MinValue))
			{
				PlaceSellStop(sellPrice, stopLoss, takeProfit);
			}
		}

		ShiftHistory(candle);
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		if (trade.Order == null)
		return;

		if (trade.Order == _buyStopOrder)
		{
			RegisterProtection(isLong: true);
			if (_buyStopOrder?.State != OrderStates.Active)
			{
				_buyStopOrder = null;
			}
		}
		else if (trade.Order == _sellStopOrder)
		{
			RegisterProtection(isLong: false);
			if (_sellStopOrder?.State != OrderStates.Active)
			{
				_sellStopOrder = null;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			CancelProtectionOrders();
		}
	}

	private void PlaceBuyStop(decimal price, decimal? stopLoss, decimal? takeProfit)
	{
		if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
		{
			CancelOrder(_buyStopOrder);
		}

		_buyStopOrder = BuyStop(price: price, volume: Volume);
		_buyStopLossPrice = stopLoss;
		_buyTakeProfitPrice = takeProfit;
	}

	private void PlaceSellStop(decimal price, decimal? stopLoss, decimal? takeProfit)
	{
		if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
		{
			CancelOrder(_sellStopOrder);
		}

		_sellStopOrder = SellStop(price: price, volume: Volume);
		_sellStopLossPrice = stopLoss;
		_sellTakeProfitPrice = takeProfit;
	}

	private void RegisterProtection(bool isLong)
	{
		// Create protective stop-loss and take-profit orders after entry execution.
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		CancelProtectionOrders();

		var stopLoss = isLong ? _buyStopLossPrice : _sellStopLossPrice;
		var takeProfit = isLong ? _buyTakeProfitPrice : _sellTakeProfitPrice;

		if (stopLoss.HasValue)
		{
			_stopLossOrder = isLong
			? SellStop(price: stopLoss.Value, volume: volume)
			: BuyStop(price: stopLoss.Value, volume: volume);
		}

		if (takeProfit.HasValue)
		{
			_takeProfitOrder = isLong
			? SellLimit(price: takeProfit.Value, volume: volume)
			: BuyLimit(price: takeProfit.Value, volume: volume);
		}
	}

	private void CancelProtectionOrders()
	{
		if (_stopLossOrder != null)
		{
			if (_stopLossOrder.State == OrderStates.Active)
			CancelOrder(_stopLossOrder);
			_stopLossOrder = null;
		}

		if (_takeProfitOrder != null)
		{
			if (_takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);
			_takeProfitOrder = null;
		}
	}

	private void CloseAllPositions()
	{
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(-Position);
		}

		CancelProtectionOrders();
	}

	private void ShiftHistory(ICandleMessage candle)
	{
		_prevHigh2 = _prevHigh1;
		_prevLow2 = _prevLow1;
		_prevHigh1 = candle.HighPrice;
		_prevLow1 = candle.LowPrice;
	}

	private void UpdateOrderReferences()
	{
		if (_buyStopOrder != null && _buyStopOrder.State != OrderStates.Active)
		{
			_buyStopOrder = null;
		}

		if (_sellStopOrder != null && _sellStopOrder.State != OrderStates.Active)
		{
			_sellStopOrder = null;
		}

		if (_stopLossOrder != null && _stopLossOrder.State != OrderStates.Active)
		{
			_stopLossOrder = null;
		}

		if (_takeProfitOrder != null && _takeProfitOrder.State != OrderStates.Active)
		{
			_takeProfitOrder = null;
		}
	}

	private bool HasOpenPositionOrOrders()
	{
		return Position != 0
		|| IsOrderActive(_buyStopOrder)
		|| IsOrderActive(_sellStopOrder)
		|| IsOrderActive(_stopLossOrder)
		|| IsOrderActive(_takeProfitOrder);
	}

	private static bool IsOrderActive(Order? order)
	{
		return order != null && order.State == OrderStates.Active;
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return price;

		var rounded = Math.Round(price / step, MidpointRounding.AwayFromZero) * step;
		return rounded;
	}
}
