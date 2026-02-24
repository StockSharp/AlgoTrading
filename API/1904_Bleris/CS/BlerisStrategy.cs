using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bleris strategy based on comparisons of consecutive highest highs and lowest lows.
/// </summary>
public class BlerisStrategy : Strategy
{
	private readonly StrategyParam<int> _signalBarSample;
	private readonly StrategyParam<bool> _counterTrend;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();

	private decimal _prevHigh1;
	private decimal _prevHigh2;
	private decimal _prevLow1;
	private decimal _prevLow2;

	/// <summary>
	/// Number of candles for each segment of trend detection.
	/// </summary>
	public int SignalBarSample { get => _signalBarSample.Value; set => _signalBarSample.Value = value; }

	/// <summary>
	/// Reverse trading direction when true.
	/// </summary>
	public bool CounterTrend { get => _counterTrend.Value; set => _counterTrend.Value = value; }

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BlerisStrategy()
	{
		_signalBarSample = Param(nameof(SignalBarSample), 24)
			.SetDisplay("Signal bar sample", "Signal bar sample", "General");

		_counterTrend = Param(nameof(CounterTrend), false)
			.SetDisplay("Counter trend", "Counter trend", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Candle type", "General");
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
		_highs.Clear();
		_lows.Clear();
		_prevHigh1 = 0;
		_prevHigh2 = 0;
		_prevLow1 = 0;
		_prevLow2 = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(null, null);

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

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > SignalBarSample)
			_highs.RemoveAt(0);
		if (_lows.Count > SignalBarSample)
			_lows.RemoveAt(0);

		if (_highs.Count < SignalBarSample)
			return;

		var highest = decimal.MinValue;
		var lowest = decimal.MaxValue;
		for (var i = 0; i < _highs.Count; i++)
		{
			if (_highs[i] > highest) highest = _highs[i];
			if (_lows[i] < lowest) lowest = _lows[i];
		}

		var uptrend = _prevLow2 > 0 && _prevLow2 < _prevLow1 && _prevLow1 < lowest;
		var downtrend = _prevHigh2 > 0 && _prevHigh2 > _prevHigh1 && _prevHigh1 > highest;

		_prevHigh2 = _prevHigh1;
		_prevHigh1 = highest;
		_prevLow2 = _prevLow1;
		_prevLow1 = lowest;

		if (uptrend && !downtrend)
		{
			if (CounterTrend)
			{
				if (Position >= 0)
				{
					if (Position > 0) SellMarket();
					SellMarket();
				}
			}
			else
			{
				if (Position <= 0)
				{
					if (Position < 0) BuyMarket();
					BuyMarket();
				}
			}
		}
		else if (downtrend && !uptrend)
		{
			if (CounterTrend)
			{
				if (Position <= 0)
				{
					if (Position < 0) BuyMarket();
					BuyMarket();
				}
			}
			else
			{
				if (Position >= 0)
				{
					if (Position > 0) SellMarket();
					SellMarket();
				}
			}
		}
	}
}
