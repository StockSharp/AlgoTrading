namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class ChartPlusChartStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _channelIndex;

	private decimal _lastClosePrice;

	public ChartPlusChartStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe processed by the strategy.", "General");

		_channelIndex = Param(nameof(ChannelIndex), 10)
			.SetGreaterOrEqualZero()
			.SetDisplay("Channel index", "Base slot used when publishing shared values.", "General")
			.SetCanOptimize(false);
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int ChannelIndex
	{
		get => _channelIndex.Value;
		set => _channelIndex.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_lastClosePrice = 0m;
		UpdateSharedValues();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		ChartPlusChartSharedStorage.ClearChannel(ChannelIndex);
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var price = trade.Trade?.Price;
		if (price is decimal executionPrice)
		{
			UpdateSharedValues(executionPrice);
		}
		else
		{
			UpdateSharedValues();
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return; // Only publish data when the candle is final, just like MetaTrader's tick callback.

		_lastClosePrice = candle.ClosePrice;

		UpdateSharedValues();
	}

	private void UpdateSharedValues(decimal? priceOverride = null)
	{
		var baseIndex = ChannelIndex;
		if (baseIndex < 0)
			return; // Negative indexes are ignored to mirror the defensive behaviour of the original DLL wrapper.

		var lastPrice = priceOverride ?? _lastClosePrice;
		if (lastPrice == 0m)
		{
			var lastTrade = Security?.LastTrade?.Price;
			if (lastTrade is decimal tradePrice)
				lastPrice = tradePrice;
		}

		var ordersTotal = CountActiveOrders();
		var accountBalance = GetPortfolioValue();
		var floatingProfit = CalculateFloatingPnL(lastPrice);

		ChartPlusChartSharedStorage.SetFloat(baseIndex, lastPrice);
		ChartPlusChartSharedStorage.SetInt(baseIndex, ordersTotal);
		ChartPlusChartSharedStorage.SetFloat(baseIndex + 1, accountBalance);
		ChartPlusChartSharedStorage.SetFloat(baseIndex + 2, floatingProfit);
	}

	private int CountActiveOrders()
	{
		var count = 0;
		foreach (var order in Orders)
		{
			var state = order.State;
			if (state == OrderStates.Done || state == OrderStates.Failed || state == OrderStates.Canceled)
				continue; // Only live or pending orders are counted, matching MetaTrader's OrdersTotal function.

			count++;
		}

		return count;
	}

	private decimal GetPortfolioValue()
	{
		var portfolio = Portfolio;
		if (portfolio is null)
			return 0m;

		if (portfolio.CurrentValue != 0m)
			return portfolio.CurrentValue;

		return portfolio.BeginValue;
	}

	private decimal CalculateFloatingPnL(decimal price)
	{
		if (price == 0m)
			return 0m;

		if (Position == 0m)
			return 0m;

		var averagePrice = Position.AveragePrice;
		if (averagePrice is null)
			return 0m;

		var volume = Math.Abs(Position);
		if (volume == 0m)
			return 0m;

		return Position > 0m
			? (price - averagePrice.Value) * volume
			: (averagePrice.Value - price) * volume;
	}
}

public static class ChartPlusChartSharedStorage
{
	private static readonly ConcurrentDictionary<int, decimal> _floatValues = new();
	private static readonly ConcurrentDictionary<int, int> _intValues = new();

	public static void SetFloat(int index, decimal value)
	{
		_floatValues[index] = value;
	}

	public static void SetInt(int index, int value)
	{
		_intValues[index] = value;
	}

	public static bool TryGetFloat(int index, out decimal value)
	{
		return _floatValues.TryGetValue(index, out value);
	}

	public static bool TryGetInt(int index, out int value)
	{
		return _intValues.TryGetValue(index, out value);
	}

	public static void ClearChannel(int baseIndex)
	{
		_floatValues.TryRemove(baseIndex, out _);
		_intValues.TryRemove(baseIndex, out _);
		_floatValues.TryRemove(baseIndex + 1, out _);
		_floatValues.TryRemove(baseIndex + 2, out _);
	}
}
