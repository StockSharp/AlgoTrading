using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Harmonic ABCD pattern strategy with Fibonacci entry, target and stop.
/// A zigzag point is set when the candle colour flips: the higher high of the two candles after an up-to-down flip,
/// the lower low after a down-to-up flip. The last four points A, B, C, D form an ABCD pattern when BC retraces 0.382-0.886 of AB
/// and CD extends 1.13-2.618 of BC; D below C is bullish and D above C bearish.
/// Fibonacci levels are measured from D back over the C-D range. A bullish pattern buys when the close is at or below the EntryRate level,
/// a bearish one sells when the close is at or above it. The TakeProfitRate and StopLossRate levels at entry close the position.
/// </summary>
public class DkoderwebRepaintingIssueFixStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeSize;
	private readonly StrategyParam<decimal> _entryRate;
	private readonly StrategyParam<decimal> _takeProfitRate;
	private readonly StrategyParam<decimal> _stopLossRate;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _points = [];
	private ICandleMessage _prevCandle;
	private int _direction;
	private decimal? _takePrice;
	private decimal? _stopPrice;

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal TradeSize { get => _tradeSize.Value; set => _tradeSize.Value = value; }

	/// <summary>
	/// Fibonacci rate of the entry level.
	/// </summary>
	public decimal EntryRate { get => _entryRate.Value; set => _entryRate.Value = value; }

	/// <summary>
	/// Fibonacci rate of the take profit level.
	/// </summary>
	public decimal TakeProfitRate { get => _takeProfitRate.Value; set => _takeProfitRate.Value = value; }

	/// <summary>
	/// Fibonacci rate of the stop loss level.
	/// </summary>
	public decimal StopLossRate { get => _stopLossRate.Value; set => _stopLossRate.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public DkoderwebRepaintingIssueFixStrategy()
	{
		_tradeSize = Param(nameof(TradeSize), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Size", "Order volume", "Trading");

		_entryRate = Param(nameof(EntryRate), 0.382m)
			.SetDisplay("Entry Rate", "Fibonacci rate of the entry level", "Fibonacci");

		_takeProfitRate = Param(nameof(TakeProfitRate), 0.618m)
			.SetDisplay("Take Profit Rate", "Fibonacci rate of the take profit level", "Fibonacci");

		_stopLossRate = Param(nameof(StopLossRate), -0.618m)
			.SetDisplay("Stop Loss Rate", "Fibonacci rate of the stop loss level", "Fibonacci");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_points.Clear();
		_prevCandle = null;
		_direction = 0;
		_takePrice = null;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();
		Volume = TradeSize;

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

		UpdateZigZag(candle);
		_prevCandle = candle;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if ((_takePrice is decimal take && candle.HighPrice >= take) || (_stopPrice is decimal stop && candle.LowPrice <= stop))
			{
				SellMarket(Position);
				_takePrice = null;
				_stopPrice = null;
				return;
			}
		}
		else if (Position < 0)
		{
			if ((_takePrice is decimal take && candle.LowPrice <= take) || (_stopPrice is decimal stop && candle.HighPrice >= stop))
			{
				BuyMarket(-Position);
				_takePrice = null;
				_stopPrice = null;
				return;
			}
		}

		if (_points.Count < 4)
			return;

		var a = _points[^4];
		var b = _points[^3];
		var c = _points[^2];
		var d = _points[^1];

		var ab = Math.Abs(a - b);
		var bc = Math.Abs(b - c);
		var cd = Math.Abs(c - d);

		if (ab == 0m || bc == 0m)
			return;

		var abc = bc / ab;
		var bcd = cd / bc;

		if (abc < 0.382m || abc > 0.886m || bcd < 1.13m || bcd > 2.618m)
			return;

		decimal Fib(decimal rate) => d > c ? d - cd * rate : d + cd * rate;

		var close = candle.ClosePrice;

		if (d < c && close <= Fib(EntryRate) && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_takePrice = Fib(TakeProfitRate);
			_stopPrice = Fib(StopLossRate);
		}
		else if (d > c && close >= Fib(EntryRate) && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_takePrice = Fib(TakeProfitRate);
			_stopPrice = Fib(StopLossRate);
		}
	}

	private void UpdateZigZag(ICandleMessage candle)
	{
		if (_prevCandle is not { } prev)
			return;

		var isUp = candle.ClosePrice >= candle.OpenPrice;
		var isDown = candle.ClosePrice <= candle.OpenPrice;
		var prevIsUp = prev.ClosePrice >= prev.OpenPrice;
		var prevIsDown = prev.ClosePrice <= prev.OpenPrice;

		var prevDirection = _direction;
		_direction = prevIsUp && isDown ? -1 : prevIsDown && isUp ? 1 : prevDirection;

		decimal? point = null;

		if (prevIsUp && isDown && prevDirection != -1)
			point = Math.Max(candle.HighPrice, prev.HighPrice);
		else if (prevIsDown && isUp && prevDirection != 1)
			point = Math.Min(candle.LowPrice, prev.LowPrice);

		if (point is decimal p)
		{
			_points.Add(p);
			if (_points.Count > 5)
				_points.RemoveAt(0);
		}
	}
}
