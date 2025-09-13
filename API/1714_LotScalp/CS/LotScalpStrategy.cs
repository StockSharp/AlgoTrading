using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that opens one trade per day based on differences between past candle open prices.
/// </summary>
public class LotScalpStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfitLong;
	private readonly StrategyParam<decimal> _stopLossLong;
	private readonly StrategyParam<decimal> _takeProfitShort;
	private readonly StrategyParam<decimal> _stopLossShort;
	private readonly StrategyParam<int> _tradeTime;
	private readonly StrategyParam<int> _t1;
	private readonly StrategyParam<int> _t2;
	private readonly StrategyParam<decimal> _deltaLong;
	private readonly StrategyParam<decimal> _deltaShort;
	private readonly StrategyParam<int> _maxOpenTime;
	private readonly StrategyParam<decimal> _volume;

	private readonly Queue<decimal> _openPrices = new();
	private bool _canTrade = true;
	private decimal? _entryPrice;
	private DateTimeOffset _entryTime;

	/// <summary>
	/// Candle source for the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Take profit in points for long trades.
	/// </summary>
	public decimal TakeProfitLong
	{
		get => _takeProfitLong.Value;
		set => _takeProfitLong.Value = value;
	}

	/// <summary>
	/// Stop loss in points for long trades.
	/// </summary>
	public decimal StopLossLong
	{
		get => _stopLossLong.Value;
		set => _stopLossLong.Value = value;
	}

	/// <summary>
	/// Take profit in points for short trades.
	/// </summary>
	public decimal TakeProfitShort
	{
		get => _takeProfitShort.Value;
		set => _takeProfitShort.Value = value;
	}

	/// <summary>
	/// Stop loss in points for short trades.
	/// </summary>
	public decimal StopLossShort
	{
		get => _stopLossShort.Value;
		set => _stopLossShort.Value = value;
	}

	/// <summary>
	/// Hour of the day when signals are evaluated.
	/// </summary>
	public int TradeTime
	{
		get => _tradeTime.Value;
		set => _tradeTime.Value = value;
	}

	/// <summary>
	/// Bars back for the first open price.
	/// </summary>
	public int T1
	{
		get => _t1.Value;
		set => _t1.Value = value;
	}

	/// <summary>
	/// Bars back for the second open price.
	/// </summary>
	public int T2
	{
		get => _t2.Value;
		set => _t2.Value = value;
	}

	/// <summary>
	/// Minimum difference to open a long trade.
	/// </summary>
	public decimal DeltaLong
	{
		get => _deltaLong.Value;
		set => _deltaLong.Value = value;
	}

	/// <summary>
	/// Minimum difference to open a short trade.
	/// </summary>
	public decimal DeltaShort
	{
		get => _deltaShort.Value;
		set => _deltaShort.Value = value;
	}

	/// <summary>
	/// Maximum holding time in hours.
	/// </summary>
	public int MaxOpenTime
	{
		get => _maxOpenTime.Value;
		set => _maxOpenTime.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LotScalpStrategy"/>.
	/// </summary>
	public LotScalpStrategy()
	{
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(1)))
			.SetDisplay("Candle Type", "Candle source", "General");

		_volume = Param(nameof(Volume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_takeProfitLong = Param(nameof(TakeProfitLong), 5m)
			.SetDisplay("Long Take Profit", "Take profit in points for long trades", "Trading");

		_stopLossLong = Param(nameof(StopLossLong), 6000m)
			.SetDisplay("Long Stop Loss", "Stop loss in points for long trades", "Trading");

		_takeProfitShort = Param(nameof(TakeProfitShort), 5m)
			.SetDisplay("Short Take Profit", "Take profit in points for short trades", "Trading");

		_stopLossShort = Param(nameof(StopLossShort), 6000m)
			.SetDisplay("Short Stop Loss", "Stop loss in points for short trades", "Trading");

		_tradeTime = Param(nameof(TradeTime), 7)
			.SetDisplay("Trade Hour", "Hour of the day to evaluate signals", "Trading");

		_t1 = Param(nameof(T1), 6)
			.SetDisplay("T1", "Bars back for first open", "Trading");

		_t2 = Param(nameof(T2), 2)
			.SetDisplay("T2", "Bars back for second open", "Trading");

		_deltaLong = Param(nameof(DeltaLong), 6m)
			.SetDisplay("Delta Long", "Minimum difference to go long", "Trading");

		_deltaShort = Param(nameof(DeltaShort), 21m)
			.SetDisplay("Delta Short", "Minimum difference to go short", "Trading");

		_maxOpenTime = Param(nameof(MaxOpenTime), 504)
			.SetDisplay("Max Open Time", "Maximum holding time in hours", "Trading");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var maxShift = Math.Max(T1, T2);
		_openPrices.Enqueue(candle.OpenPrice);
		if (_openPrices.Count > maxShift + 1)
			_openPrices.Dequeue();

		var step = Security?.PriceStep ?? 1m;

		if (Position > 0 && _entryPrice is decimal entryLong)
		{
			if (candle.ClosePrice >= entryLong + TakeProfitLong * step ||
				candle.ClosePrice <= entryLong - StopLossLong * step ||
				(MaxOpenTime > 0 && candle.OpenTime - _entryTime >= TimeSpan.FromHours(MaxOpenTime)))
			{
				SellMarket(Position);
				_entryPrice = null;
			}
		}
		else if (Position < 0 && _entryPrice is decimal entryShort)
		{
			if (candle.ClosePrice <= entryShort - TakeProfitShort * step ||
				candle.ClosePrice >= entryShort + StopLossShort * step ||
				(MaxOpenTime > 0 && candle.OpenTime - _entryTime >= TimeSpan.FromHours(MaxOpenTime)))
			{
				BuyMarket(-Position);
				_entryPrice = null;
			}
		}

		var hour = candle.OpenTime.Hour;
		if (hour > TradeTime)
			_canTrade = true;

		if (_openPrices.Count <= maxShift || Position != 0 || !_canTrade || hour != TradeTime)
			return;

		var opens = _openPrices.ToArray();
		var openT1 = opens[opens.Length - 1 - T1];
		var openT2 = opens[opens.Length - 1 - T2];

		if (openT1 - openT2 > DeltaShort * step)
		{
			SellMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_entryTime = candle.OpenTime;
			_canTrade = false;
		}
		else if (openT2 - openT1 > DeltaLong * step)
		{
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_entryTime = candle.OpenTime;
			_canTrade = false;
		}
	}
}
