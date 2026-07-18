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
/// Compares current price with the price from a past moment and trades on large deviations.
/// </summary>
public class BackToTheFutureStrategy : Strategy
{
	private readonly StrategyParam<decimal> _barSize;
	private readonly StrategyParam<int> _historyMinutes;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<(DateTimeOffset time, decimal price)> _history = new();
	private decimal _entryPrice;
	private int _barsSinceTrade;

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
	/// Bars to wait after a completed trade.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
		_barSize = Param(nameof(BarSize), 1500m)
			.SetGreaterThanZero()
			.SetDisplay("Price Difference", "Threshold to trigger trades", "General")
			;

		_historyMinutes = Param(nameof(HistoryMinutes), 240)
			.SetGreaterThanZero()
			.SetDisplay("History Minutes", "Minutes back for price comparison", "General")
			;

		_takeProfit = Param(nameof(TakeProfit), 1500m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Distance from entry", "Risk")
			;

		_stopLoss = Param(nameof(StopLoss), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Distance from entry", "Risk")
			;

		_cooldownBars = Param(nameof(CooldownBars), 2)
			.SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		_barsSinceTrade = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		if (_barsSinceTrade < CooldownBars)
			_barsSinceTrade++;

		var oldest = _history.Peek().price;
		var diff = candle.ClosePrice - oldest;

		if (Position > 0)
		{
			if (candle.ClosePrice >= _entryPrice + TakeProfit || candle.ClosePrice <= _entryPrice - StopLoss)
			{
				SellMarket(Position);
				_barsSinceTrade = 0;
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice <= _entryPrice - TakeProfit || candle.ClosePrice >= _entryPrice + StopLoss)
			{
				BuyMarket(-Position);
				_barsSinceTrade = 0;
			}
		}
		else if (_barsSinceTrade >= CooldownBars)
		{
			if (diff > BarSize)
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
				_barsSinceTrade = 0;
			}
			else if (diff < -BarSize)
			{
				var volume = Volume + (Position > 0 ? Position : 0m);
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
				_barsSinceTrade = 0;
			}
		}
	}
}
