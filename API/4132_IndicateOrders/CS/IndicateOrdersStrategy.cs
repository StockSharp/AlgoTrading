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

using System.Text;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that mirrors the behaviour of the MetaTrader "q_indicate_orders" indicator.
/// It keeps track of executed trades, reconstructs the outstanding buy and sell positions,
/// and logs a textual summary with floating profit for each side. The implementation assumes
/// netting behaviour: opposite trades reduce existing exposure before opening new tickets.
/// </summary>
public class IndicateOrdersStrategy : Strategy
{
	private readonly StrategyParam<int> _maxOrdersToShow;
	private readonly StrategyParam<DataType> _priceDataType;

	private readonly List<OpenTrade> _openBuyTrades = new();
	private readonly List<OpenTrade> _openSellTrades = new();

	private decimal? _lastPrice;
	private string _lastSummary;

	/// <summary>
	/// Maximum number of order details logged for each side.
	/// </summary>
	public int MaxOrdersToShow
	{
		get => _maxOrdersToShow.Value;
		set => _maxOrdersToShow.Value = value;
	}

	/// <summary>
	/// Market data series that supplies the reference price for floating profit calculations.
	/// </summary>
	public DataType PriceDataType
	{
		get => _priceDataType.Value;
		set => _priceDataType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy and exposes user parameters.
	/// </summary>
	public IndicateOrdersStrategy()
	{
		_maxOrdersToShow = Param(nameof(MaxOrdersToShow), 25)
			.SetGreaterThanZero()
			.SetDisplay("Max Orders", "Maximum number of positions displayed per side", "Display");

		_priceDataType = Param(nameof(PriceDataType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Price Data Type", "Market data source used for floating profit", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
			yield break;

		yield return (Security, PriceDataType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_openBuyTrades.Clear();
		_openSellTrades.Clear();
		_lastPrice = null;
		_lastSummary = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdateSummary();

		if (Security is null)
			return;

		SubscribeCandles(PriceDataType)
			.Bind(OnPriceCandle)
			.Start();
	}

	private void OnPriceCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastPrice = candle.ClosePrice;

		UpdateSummary();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order.Security is null)
			return;

		if (Security is not null && trade.Order.Security != Security)
			return;

		if (trade.Trade is null || trade.Trade.Volume <= 0m)
			return;

		if (trade.Order.Side == Sides.Buy)
		{
			ProcessBuyTrade(trade);
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			ProcessSellTrade(trade);
		}

		UpdateSummary();
	}

	private void ProcessBuyTrade(MyTrade trade)
	{
		var remaining = trade.Trade.Volume;

		for (var i = 0; i < _openSellTrades.Count && remaining > 0m;)
		{
			var shortTrade = _openSellTrades[i];
			var closeVolume = Math.Min(shortTrade.Volume, remaining);

			shortTrade.Volume -= closeVolume;
			remaining -= closeVolume;

			if (shortTrade.Volume == 0m)
			{
				_openSellTrades.RemoveAt(i);
				continue;
			}

			// Only advance to the next short trade when the current one is fully closed.
			break;
		}

		if (remaining <= 0m)
			return;

		var openTrade = new OpenTrade
		{
			OrderId = trade.Order.Id,
			Volume = remaining,
			Price = trade.Trade.Price,
			Time = trade.Trade.ServerTime,
		};

		_openBuyTrades.Add(openTrade);
	}

	private void ProcessSellTrade(MyTrade trade)
	{
		var remaining = trade.Trade.Volume;

		for (var i = 0; i < _openBuyTrades.Count && remaining > 0m;)
		{
			var longTrade = _openBuyTrades[i];
			var closeVolume = Math.Min(longTrade.Volume, remaining);

			longTrade.Volume -= closeVolume;
			remaining -= closeVolume;

			if (longTrade.Volume == 0m)
			{
				_openBuyTrades.RemoveAt(i);
				continue;
			}

			// Only advance to the next long trade when the current one is fully closed.
			break;
		}

		if (remaining <= 0m)
			return;

		var openTrade = new OpenTrade
		{
			OrderId = trade.Order.Id,
			Volume = remaining,
			Price = trade.Trade.Price,
			Time = trade.Trade.ServerTime,
		};

		_openSellTrades.Add(openTrade);
	}

	private void UpdateSummary()
	{
		var builder = new StringBuilder();

		if (_openBuyTrades.Count > 0)
		{
			builder.AppendLine(BuildSection("BUYS", _openBuyTrades, true));
		}

		if (_openSellTrades.Count > 0)
		{
			if (builder.Length > 0)
				builder.AppendLine();

			builder.AppendLine(BuildSection("SELLS", _openSellTrades, false));
		}

		if (builder.Length == 0)
			builder.Append("No open orders.");

		var summary = builder.ToString().TrimEnd();

		if (summary == _lastSummary)
			return;

		_lastSummary = summary;
		LogInfo(summary);
	}

	private string BuildSection(string title, List<OpenTrade> trades, bool isBuy)
	{
		var builder = new StringBuilder();
		var totalVolume = 0m;

		for (var i = 0; i < trades.Count; i++)
			totalVolume += trades[i].Volume;

		var floating = CalculateFloatingProfit(trades, isBuy);
		builder.AppendLine($"{title} | {trades.Count} | {totalVolume:0.####} | {FormatProfit(floating)}");

		var limit = Math.Min(trades.Count, MaxOrdersToShow);

		for (var i = 0; i < limit; i++)
		{
			var trade = trades[i];
			var tradeProfit = CalculateFloatingProfit(trade, isBuy);
			var timeText = trade.Time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
			builder.AppendLine($"#{trade.OrderId}: {trade.Volume:0.####} @ {trade.Price:0.#####} | {timeText} | {FormatProfit(tradeProfit)}");
		}

		if (trades.Count > MaxOrdersToShow)
		{
			var remaining = trades.Count - MaxOrdersToShow;
			builder.Append($"... {remaining} more");
		}

		return builder.ToString().TrimEnd();
	}

	private decimal? CalculateFloatingProfit(List<OpenTrade> trades, bool isBuy)
	{
		if (_lastPrice is null)
			return null;

		var profit = 0m;

		for (var i = 0; i < trades.Count; i++)
			profit += CalculateFloatingProfit(trades[i], isBuy) ?? 0m;

		return profit;
	}

	private decimal? CalculateFloatingProfit(OpenTrade trade, bool isBuy)
	{
		if (_lastPrice is null)
			return null;

		var priceDifference = isBuy
			? _lastPrice.Value - trade.Price
			: trade.Price - _lastPrice.Value;

		return priceDifference * trade.Volume;
	}

	private static string FormatProfit(decimal? profit)
	{
		if (profit is null)
			return "N/A";

		if (profit > 0m)
			return $"+{profit.Value:0.##}";

		if (profit < 0m)
			return profit.Value.ToString("0.##");

		return "0";
	}

	private sealed class OpenTrade
	{
		public long OrderId { get; init; }
		public decimal Volume { get; set; }
		public decimal Price { get; init; }
		public DateTimeOffset Time { get; init; }
	}
}
