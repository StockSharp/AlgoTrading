using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Time based strategy comparing open prices with fixed stop loss and take profit.
/// </summary>
public class GeedoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _tradeTime;
	private readonly StrategyParam<int> _t1;
	private readonly StrategyParam<int> _t2;
	private readonly StrategyParam<int> _deltaLong;
	private readonly StrategyParam<int> _deltaShort;
	private readonly StrategyParam<int> _takeProfitLong;
	private readonly StrategyParam<int> _stopLossLong;
	private readonly StrategyParam<int> _takeProfitShort;
	private readonly StrategyParam<int> _stopLossShort;
	private readonly StrategyParam<int> _maxOpenTime;
	private readonly StrategyParam<decimal> _volume;

	private readonly List<decimal> _openHistory = new();
	private bool _canTrade = true;
	private DateTimeOffset? _entryTime;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="GeedoStrategy"/> class.
	/// </summary>
	public GeedoStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Source candles", "General");

		_tradeTime = Param(nameof(TradeTime), 18)
			.SetDisplay("Trade Time", "Hour to enter the market", "General");

		_t1 = Param(nameof(T1), 6)
			.SetDisplay("T1", "Index of first open price", "Signal");

		_t2 = Param(nameof(T2), 2)
			.SetDisplay("T2", "Index of second open price", "Signal");

		_deltaLong = Param(nameof(DeltaLong), 6)
			.SetDisplay("Delta Long", "Required rise in points", "Signal");

		_deltaShort = Param(nameof(DeltaShort), 21)
			.SetDisplay("Delta Short", "Required drop in points", "Signal");

		_takeProfitLong = Param(nameof(TakeProfitLong), 39)
			.SetDisplay("TP Long", "Take profit for long in points", "Risk");

		_stopLossLong = Param(nameof(StopLossLong), 147)
			.SetDisplay("SL Long", "Stop loss for long in points", "Risk");

		_takeProfitShort = Param(nameof(TakeProfitShort), 15)
			.SetDisplay("TP Short", "Take profit for short in points", "Risk");

		_stopLossShort = Param(nameof(StopLossShort), 6000)
			.SetDisplay("SL Short", "Stop loss for short in points", "Risk");

		_maxOpenTime = Param(nameof(MaxOpenTime), 504)
			.SetDisplay("Max Open Time", "Maximum holding time in hours", "Risk");

		_volume = Param(nameof(Volume), 0.01m)
			.SetDisplay("Volume", "Order volume", "Trading");
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
	/// Hour to enter the market.
	/// </summary>
	public int TradeTime
	{
		get => _tradeTime.Value;
		set => _tradeTime.Value = value;
	}

	/// <summary>
	/// Index of the first open price.
	/// </summary>
	public int T1
	{
		get => _t1.Value;
		set => _t1.Value = value;
	}

	/// <summary>
	/// Index of the second open price.
	/// </summary>
	public int T2
	{
		get => _t2.Value;
		set => _t2.Value = value;
	}

	/// <summary>
	/// Required rise in points to go long.
	/// </summary>
	public int DeltaLong
	{
		get => _deltaLong.Value;
		set => _deltaLong.Value = value;
	}

	/// <summary>
	/// Required drop in points to go short.
	/// </summary>
	public int DeltaShort
	{
		get => _deltaShort.Value;
		set => _deltaShort.Value = value;
	}

	/// <summary>
	/// Take profit for long positions in points.
	/// </summary>
	public int TakeProfitLong
	{
		get => _takeProfitLong.Value;
		set => _takeProfitLong.Value = value;
	}

	/// <summary>
	/// Stop loss for long positions in points.
	/// </summary>
	public int StopLossLong
	{
		get => _stopLossLong.Value;
		set => _stopLossLong.Value = value;
	}

	/// <summary>
	/// Take profit for short positions in points.
	/// </summary>
	public int TakeProfitShort
	{
		get => _takeProfitShort.Value;
		set => _takeProfitShort.Value = value;
	}

	/// <summary>
	/// Stop loss for short positions in points.
	/// </summary>
	public int StopLossShort
	{
		get => _stopLossShort.Value;
		set => _stopLossShort.Value = value;
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
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
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

		_openHistory.Clear();
		_canTrade = true;
		_entryTime = null;
		_stopPrice = null;
		_takeProfitPrice = null;
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

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_openHistory.Add(candle.OpenPrice);
		var maxSize = Math.Max(T1, T2) + 1;
		if (_openHistory.Count > maxSize)
			_openHistory.RemoveAt(0);

		if (Position != 0)
		{
			CheckExit(candle);
			return;
		}

		if (candle.OpenTime.Hour > TradeTime)
			_canTrade = true;

		if (!_canTrade || candle.OpenTime.Hour != TradeTime)
			return;

		if (_openHistory.Count <= Math.Max(T1, T2))
			return;

		var openT1 = _openHistory[^1 - T1];
		var openT2 = _openHistory[^1 - T2];

		var step = Security.PriceStep ?? 1m;

		var diffShort = openT1 - openT2;
		var diffLong = openT2 - openT1;

		if (diffShort > DeltaShort * step)
		{
			SellMarket(Volume);
			_entryTime = candle.OpenTime;
			_stopPrice = candle.ClosePrice + StopLossShort * step;
			_takeProfitPrice = candle.ClosePrice - TakeProfitShort * step;
			_canTrade = false;
		}
		else if (diffLong > DeltaLong * step)
		{
			BuyMarket(Volume);
			_entryTime = candle.OpenTime;
			_stopPrice = candle.ClosePrice - StopLossLong * step;
			_takeProfitPrice = candle.ClosePrice + TakeProfitLong * step;
			_canTrade = false;
		}
	}

	private void CheckExit(ICandleMessage candle)
	{
		if (_entryTime is DateTimeOffset entry)
		{
			var hours = (candle.OpenTime - entry).TotalHours;
			if (hours >= MaxOpenTime)
			{
				ClosePosition();
				return;
			}
		}

		if (Position > 0)
		{
			if ((_stopPrice is decimal sl && candle.LowPrice <= sl) ||
				(_takeProfitPrice is decimal tp && candle.HighPrice >= tp))
			{
				SellMarket(Position);
				_entryTime = null;
				_stopPrice = null;
				_takeProfitPrice = null;
			}
		}
		else if (Position < 0)
		{
			if ((_stopPrice is decimal sl && candle.HighPrice >= sl) ||
				(_takeProfitPrice is decimal tp && candle.LowPrice <= tp))
			{
				BuyMarket(Math.Abs(Position));
				_entryTime = null;
				_stopPrice = null;
				_takeProfitPrice = null;
			}
		}
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));

		_entryTime = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}
}

