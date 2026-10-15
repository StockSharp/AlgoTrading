using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Crunchster's Turtle and Trend System strategy.
/// The trend signal is the difference between a FastEmaPeriod EMA and an EMA five times slower: crossing above zero goes long and
/// crossing below zero goes short (TrendEnabled). The breakout signal goes long on a close above the highest high of the previous
/// BreakoutPeriod candles and short on a close below the lowest low (BreakoutEnabled). An opposite signal reverses the position.
/// A long closes below the lowest low of the previous TrailPeriod candles, a short above the highest high, and a stop
/// StopAtrMultiple ATRs from the entry limits the loss.
/// </summary>
public class CrunchstersTurtleAndTrendSystemStrategy : Strategy
{
	private const int _atrLength = 14;
	private const int _slowEmaFactor = 5;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _breakoutPeriod;
	private readonly StrategyParam<int> _trailPeriod;
	private readonly StrategyParam<decimal> _stopAtrMultiple;
	private readonly StrategyParam<decimal> _orderPercent;
	private readonly StrategyParam<bool> _trendEnabled;
	private readonly StrategyParam<bool> _breakoutEnabled;

	private Highest _breakoutHigh;
	private Lowest _breakoutLow;
	private Highest _trailHigh;
	private Lowest _trailLow;
	private decimal? _prevDiff;
	private decimal? _prevBreakoutHigh;
	private decimal? _prevBreakoutLow;
	private decimal? _prevTrailHigh;
	private decimal? _prevTrailLow;
	private decimal? _stopPrice;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA period; the slow EMA is five times longer.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Donchian breakout period.
	/// </summary>
	public int BreakoutPeriod
	{
		get => _breakoutPeriod.Value;
		set => _breakoutPeriod.Value = value;
	}

	/// <summary>
	/// Trailing Donchian exit period.
	/// </summary>
	public int TrailPeriod
	{
		get => _trailPeriod.Value;
		set => _trailPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiple for the stop.
	/// </summary>
	public decimal StopAtrMultiple
	{
		get => _stopAtrMultiple.Value;
		set => _stopAtrMultiple.Value = value;
	}

	/// <summary>
	/// Percent of equity per order (informational; the strategy trades Volume).
	/// </summary>
	public decimal OrderPercent
	{
		get => _orderPercent.Value;
		set => _orderPercent.Value = value;
	}

	/// <summary>
	/// Trade the EMA trend signal.
	/// </summary>
	public bool TrendEnabled
	{
		get => _trendEnabled.Value;
		set => _trendEnabled.Value = value;
	}

	/// <summary>
	/// Trade the Donchian breakout signal.
	/// </summary>
	public bool BreakoutEnabled
	{
		get => _breakoutEnabled.Value;
		set => _breakoutEnabled.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CrunchstersTurtleAndTrendSystemStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period; the slow EMA is five times longer", "Trend");

		_breakoutPeriod = Param(nameof(BreakoutPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Breakout Period", "Donchian breakout period", "Breakout");

		_trailPeriod = Param(nameof(TrailPeriod), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Trail Period", "Trailing Donchian exit period", "Risk");

		_stopAtrMultiple = Param(nameof(StopAtrMultiple), 20m)
			.SetNotNegative()
			.SetDisplay("Stop ATR Multiple", "ATR multiple for the stop", "Risk");

		_orderPercent = Param(nameof(OrderPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Order %", "Percent of equity per order", "Risk");

		_trendEnabled = Param(nameof(TrendEnabled), true)
			.SetDisplay("Trend Enabled", "Trade the EMA trend signal", "Trend");

		_breakoutEnabled = Param(nameof(BreakoutEnabled), false)
			.SetDisplay("Breakout Enabled", "Trade the Donchian breakout signal", "Breakout");
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
		_breakoutHigh = null;
		_breakoutLow = null;
		_trailHigh = null;
		_trailLow = null;
	}

	private void ResetState()
	{
		_prevDiff = null;
		_prevBreakoutHigh = null;
		_prevBreakoutLow = null;
		_prevTrailHigh = null;
		_prevTrailLow = null;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		var slowEma = new ExponentialMovingAverage { Length = FastEmaPeriod * _slowEmaFactor };
		var atr = new AverageTrueRange { Length = _atrLength };
		_breakoutHigh = new Highest { Length = BreakoutPeriod };
		_breakoutLow = new Lowest { Length = BreakoutPeriod };
		_trailHigh = new Highest { Length = TrailPeriod };
		_trailLow = new Lowest { Length = TrailPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastEma, slowEma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Channels are measured on the candles before this one.
		var breakoutHigh = _prevBreakoutHigh;
		var breakoutLow = _prevBreakoutLow;
		var trailHigh = _prevTrailHigh;
		var trailLow = _prevTrailLow;

		_prevBreakoutHigh = Track(_breakoutHigh, candle.HighPrice, candle.OpenTime);
		_prevBreakoutLow = Track(_breakoutLow, candle.LowPrice, candle.OpenTime);
		_prevTrailHigh = Track(_trailHigh, candle.HighPrice, candle.OpenTime);
		_prevTrailLow = Track(_trailLow, candle.LowPrice, candle.OpenTime);

		if (!fastValue.IsFormed || !slowValue.IsFormed)
			return;

		var diff = fastValue.ToDecimal() - slowValue.ToDecimal();
		var prevDiff = _prevDiff;
		_prevDiff = diff;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		var longSignal = false;
		var shortSignal = false;

		if (TrendEnabled && prevDiff is decimal pd)
		{
			longSignal |= pd <= 0 && diff > 0;
			shortSignal |= pd >= 0 && diff < 0;
		}

		if (BreakoutEnabled)
		{
			longSignal |= breakoutHigh is decimal bh && close > bh;
			shortSignal |= breakoutLow is decimal bl && close < bl;
		}

		var stopDistance = atrValue.IsFormed && StopAtrMultiple > 0 ? atrValue.ToDecimal() * StopAtrMultiple : (decimal?)null;

		if (longSignal && !shortSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - stopDistance;
			return;
		}

		if (shortSignal && !longSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + stopDistance;
			return;
		}

		if (Position > 0)
		{
			if ((_stopPrice is decimal stop && candle.LowPrice <= stop) || (trailLow is decimal tl && close < tl))
			{
				SellMarket(Position);
				_stopPrice = null;
			}
		}
		else if (Position < 0)
		{
			if ((_stopPrice is decimal stop && candle.HighPrice >= stop) || (trailHigh is decimal th && close > th))
			{
				BuyMarket(-Position);
				_stopPrice = null;
			}
		}
	}

	private static decimal? Track(IIndicator indicator, decimal value, DateTime time)
	{
		var result = indicator.Process(value, time, true);
		return result.IsFormed ? result.ToDecimal() : null;
	}
}
