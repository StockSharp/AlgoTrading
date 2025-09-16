using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Compares current price with the price from a past moment and trades on large deviations.
/// </summary>
public class BackToTheFutureStrategy : Strategy
{
	private readonly StrategyParam<decimal> _barSize;
	private readonly StrategyParam<int> _historyMinutes;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<(DateTimeOffset time, decimal price)> _history = new();
	private decimal _entryPrice;

	/// <summary>
	/// Price difference threshold.
	/// </summary>
	public decimal BarSize
	{
		get => _barSize.Value;
		set => _barSize.Value = value;
	}

	/// <summary>
	/// Minutes to look back for comparison.
	/// </summary>
	public int HistoryMinutes
	{
		get => _historyMinutes.Value;
		set => _historyMinutes.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BackToTheFutureStrategy"/>.
	/// </summary>
	public BackToTheFutureStrategy()
	{
		_barSize = Param(nameof(BarSize), 0.25m)
			.SetGreaterThanZero()
			.SetDisplay("Price Difference", "Threshold to trigger trades", "General")
			.SetCanOptimize(true);

		_historyMinutes = Param(nameof(HistoryMinutes), 60)
			.SetGreaterThanZero()
			.SetDisplay("History Minutes", "Minutes back for price comparison", "General")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Distance from entry", "Risk")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 5000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Distance from entry", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_history.Clear();
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

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

		_history.Enqueue((candle.CloseTime, candle.ClosePrice));

		var minTime = candle.CloseTime - TimeSpan.FromMinutes(HistoryMinutes);
		while (_history.Count > 0 && _history.Peek().time < minTime)
			_history.Dequeue();

		if (_history.Count == 0)
			return;

		var oldest = _history.Peek().price;
		var diff = candle.ClosePrice - oldest;

		if (Position > 0)
		{
			if (candle.ClosePrice >= _entryPrice + TakeProfit || candle.ClosePrice <= _entryPrice - StopLoss)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice <= _entryPrice - TakeProfit || candle.ClosePrice >= _entryPrice + StopLoss)
				BuyMarket(-Position);
		}
		else if (IsFormedAndOnlineAndAllowTrading())
		{
			if (diff > BarSize)
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
			else if (diff < -BarSize)
			{
				var volume = Volume + (Position > 0 ? Position : 0m);
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
		}
	}
}
