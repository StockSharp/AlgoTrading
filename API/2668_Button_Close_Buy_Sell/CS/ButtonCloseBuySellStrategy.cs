namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Utility strategy that closes open long or short positions on demand.
/// </summary>
public class ButtonCloseBuySellStrategy : Strategy
{
	private readonly StrategyParam<int> _slippage;
	private readonly StrategyParam<bool> _closeBuyParam;
	private readonly StrategyParam<bool> _closeSellParam;

	private decimal _longVolume;
	private decimal _longAveragePrice;
	private decimal _shortVolume;
	private decimal _shortAveragePrice;
	private decimal _lastTradePrice;
	private decimal _openBuyProfit;
	private decimal _openSellProfit;

	/// <summary>
	/// Maximum acceptable slippage expressed in price steps.
	/// </summary>
	public int Slippage
	{
		get => _slippage.Value;
		set => _slippage.Value = value;
	}

	/// <summary>
	/// Request to close all open long positions.
	/// </summary>
	public bool CloseBuyPositions
	{
		get => _closeBuyParam.Value;
		set
		{
			if (_closeBuyParam.Value == value)
				return;

			_closeBuyParam.Value = value;

			if (value)
			{
				TryCloseLongPositions();
				_closeBuyParam.Value = false;
			}
		}
	}

	/// <summary>
	/// Request to close all open short positions.
	/// </summary>
	public bool CloseSellPositions
	{
		get => _closeSellParam.Value;
		set
		{
			if (_closeSellParam.Value == value)
				return;

			_closeSellParam.Value = value;

			if (value)
			{
				TryCloseShortPositions();
				_closeSellParam.Value = false;
			}
		}
	}

	/// <summary>
	/// Floating profit for open long positions.
	/// </summary>
	public decimal OpenBuyProfit => _openBuyProfit;

	/// <summary>
	/// Floating profit for open short positions.
	/// </summary>
	public decimal OpenSellProfit => _openSellProfit;

	/// <summary>
	/// Initializes a new instance of <see cref="ButtonCloseBuySellStrategy"/>.
	/// </summary>
	public ButtonCloseBuySellStrategy()
	{
		_slippage = Param(nameof(Slippage), 3)
			.SetGreaterOrEqualZero()
			.SetDisplay("Slippage", "Maximum acceptable slippage in price steps", "General");

		_closeBuyParam = Param(nameof(CloseBuyPositions), false)
			.SetDisplay("Close Buy Positions", "Trigger to close all long positions", "Controls");

		_closeSellParam = Param(nameof(CloseSellPositions), false)
			.SetDisplay("Close Sell Positions", "Trigger to close all short positions", "Controls");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Ticks)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longVolume = 0m;
		_longAveragePrice = 0m;
		_shortVolume = 0m;
		_shortAveragePrice = 0m;
		_lastTradePrice = 0m;
		_openBuyProfit = 0m;
		_openSellProfit = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Use trades to keep track of the most recent market price.
		SubscribeTrades()
			.Bind(ProcessTrade)
			.Start();
	}

	/// <summary>
	/// Update the latest traded price from the exchange feed.
	/// </summary>
	/// <param name="trade">Incoming trade message.</param>
	private void ProcessTrade(ExecutionMessage trade)
	{
		if (trade.TradePrice is not decimal price)
			return;

		_lastTradePrice = price;
		UpdateFloatingProfit();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		if (trade.Order == null || trade.Trade == null)
			return;

		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;

		if (volume <= 0m)
			return;

		_lastTradePrice = price;

		if (trade.Order.Side == Sides.Buy)
		{
			// Buy trades reduce short exposure first and then add to long exposure.
			var remaining = volume;

			if (_shortVolume > 0m)
			{
				var closing = Math.Min(remaining, _shortVolume);
				_shortVolume -= closing;

				if (_shortVolume <= 0m)
				{
					_shortVolume = 0m;
					_shortAveragePrice = 0m;
				}

				remaining -= closing;
			}

			if (remaining > 0m)
			{
				var newVolume = _longVolume + remaining;
				_longAveragePrice = newVolume > 0m
					? (_longAveragePrice * _longVolume + price * remaining) / newVolume
					: 0m;
				_longVolume = newVolume;
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			// Sell trades reduce long exposure first and then add to short exposure.
			var remaining = volume;

			if (_longVolume > 0m)
			{
				var closing = Math.Min(remaining, _longVolume);
				_longVolume -= closing;

				if (_longVolume <= 0m)
				{
					_longVolume = 0m;
					_longAveragePrice = 0m;
				}

				remaining -= closing;
			}

			if (remaining > 0m)
			{
				var newVolume = _shortVolume + remaining;
				_shortAveragePrice = newVolume > 0m
					? (_shortAveragePrice * _shortVolume + price * remaining) / newVolume
					: 0m;
				_shortVolume = newVolume;
			}
		}

		UpdateFloatingProfit();
	}

	/// <summary>
	/// Attempt to close every open long position using a market order.
	/// </summary>
	private void TryCloseLongPositions()
	{
		if (!IsFormedAndOnline)
		{
			LogInfo("Strategy is not ready to trade; cannot close longs.");
			return;
		}

		var volume = _longVolume > 0m ? _longVolume : Math.Max(Position, 0m);

		if (volume <= 0m)
		{
			LogInfo("No long positions to close.");
			return;
		}

		LogInfo($"Closing long positions with volume {volume} and slippage allowance {Slippage}.");
		SellMarket(volume);
	}

	/// <summary>
	/// Attempt to close every open short position using a market order.
	/// </summary>
	private void TryCloseShortPositions()
	{
		if (!IsFormedAndOnline)
		{
			LogInfo("Strategy is not ready to trade; cannot close shorts.");
			return;
		}

		var volume = _shortVolume > 0m ? _shortVolume : Math.Max(-Position, 0m);

		if (volume <= 0m)
		{
			LogInfo("No short positions to close.");
			return;
		}

		LogInfo($"Closing short positions with volume {volume} and slippage allowance {Slippage}.");
		BuyMarket(volume);
	}

	/// <summary>
	/// Update floating profit metrics for both long and short exposure.
	/// </summary>
	private void UpdateFloatingProfit()
	{
		if (_lastTradePrice <= 0m)
		{
			_openBuyProfit = 0m;
			_openSellProfit = 0m;
			return;
		}

		_openBuyProfit = _longVolume > 0m
			? CalculateProfit(_lastTradePrice - _longAveragePrice, _longVolume)
			: 0m;

		_openSellProfit = _shortVolume > 0m
			? CalculateProfit(_shortAveragePrice - _lastTradePrice, _shortVolume)
			: 0m;
	}

	/// <summary>
	/// Convert price difference and volume into monetary profit.
	/// </summary>
	/// <param name="priceDifference">Price delta relative to the entry price.</param>
	/// <param name="volume">Volume in lots.</param>
	/// <returns>Approximate profit in portfolio currency.</returns>
	private decimal CalculateProfit(decimal priceDifference, decimal volume)
	{
		if (Security?.PriceStep is decimal step && step > 0m && Security.PriceStepValue is decimal stepValue)
		{
			return priceDifference / step * stepValue * volume;
		}

		return priceDifference * volume;
	}
}
