using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double Bottom and Top Hunter strategy.
/// The recent window is the previous Length candles and the wider window adds the Lookback candles before them. A double bottom is
/// a candle whose low reaches the lowest low of the older part of the wider window again while the recent candles stayed above it,
/// and that closes back above it; a double top mirrors this with highs. A double bottom goes long and a double top goes short,
/// reversing an opposite position. A long closes once price has made a new high above the recent high and then closes below the
/// recent low; a short closes once price has made a new low below the recent low and then closes above the recent high.
/// </summary>
public class DoubleBottomAndTopHunterStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = [];
	private readonly List<decimal> _lows = [];
	private bool _newExtremeSinceEntry;

	/// <summary>
	/// Candles in the recent window.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Older candles that widen the window.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
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
	public DoubleBottomAndTopHunterStrategy()
	{
		_length = Param(nameof(Length), 100)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Candles in the recent window", "Pattern");

		_lookback = Param(nameof(Lookback), 100)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Older candles that widen the window", "Pattern");

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
		_highs.Clear();
		_lows.Clear();
		_newExtremeSinceEntry = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

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

		var total = Length + Lookback;

		if (_highs.Count < total)
		{
			AddCandle(candle, total);
			return;
		}

		// Oldest first: the first Lookback entries are the older part, the last Length entries the recent one.
		var olderHigh = _highs.Take(Lookback).Max();
		var olderLow = _lows.Take(Lookback).Min();
		var recentHigh = _highs.Skip(Lookback).Max();
		var recentLow = _lows.Skip(Lookback).Min();

		AddCandle(candle, total);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		var doubleBottom = olderLow < recentLow && candle.LowPrice <= olderLow && close > olderLow;
		var doubleTop = olderHigh > recentHigh && candle.HighPrice >= olderHigh && close < olderHigh;

		if (doubleBottom && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_newExtremeSinceEntry = false;
			return;
		}

		if (doubleTop && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_newExtremeSinceEntry = false;
			return;
		}

		if (Position > 0)
		{
			if (candle.HighPrice > recentHigh)
				_newExtremeSinceEntry = true;
			else if (_newExtremeSinceEntry && close < recentLow)
			{
				SellMarket(Position);
				_newExtremeSinceEntry = false;
			}
		}
		else if (Position < 0)
		{
			if (candle.LowPrice < recentLow)
				_newExtremeSinceEntry = true;
			else if (_newExtremeSinceEntry && close > recentHigh)
			{
				BuyMarket(-Position);
				_newExtremeSinceEntry = false;
			}
		}
	}

	private void AddCandle(ICandleMessage candle, int total)
	{
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		while (_highs.Count > total)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}
	}
}
