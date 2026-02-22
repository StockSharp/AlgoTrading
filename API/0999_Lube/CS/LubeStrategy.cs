using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Lube strategy based on friction levels and a FIR filter trend.
/// </summary>
public class LubeStrategy : Strategy
{
	private readonly StrategyParam<int> _barsBack;
	private readonly StrategyParam<int> _frictionLevel;
	private readonly StrategyParam<int> _triggerLevel;
	private readonly StrategyParam<int> _range;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _highs = new();
	private readonly Queue<decimal> _lows = new();
	private readonly Queue<decimal> _frictions = new();
	private readonly Queue<decimal> _midfHist = new();
	private readonly Queue<decimal> _lowf2Hist = new();
	private readonly Queue<decimal> _closeQueue = new();
	private decimal _prevFir;
	private int _barCount;

	public int BarsBack { get => _barsBack.Value; set => _barsBack.Value = value; }
	public int FrictionLevel { get => _frictionLevel.Value; set => _frictionLevel.Value = value; }
	public int TriggerLevel { get => _triggerLevel.Value; set => _triggerLevel.Value = value; }
	public int Range { get => _range.Value; set => _range.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LubeStrategy()
	{
		_barsBack = Param(nameof(BarsBack), 50)
			.SetGreaterThanZero()
			.SetDisplay("Bars Back", "Bars back for friction", "General");
		_frictionLevel = Param(nameof(FrictionLevel), 50)
			.SetDisplay("Friction Level", "Stop trade level", "General");
		_triggerLevel = Param(nameof(TriggerLevel), -10)
			.SetDisplay("Trigger Level", "Initiate trade level", "General");
		_range = Param(nameof(Range), 20)
			.SetGreaterThanZero()
			.SetDisplay("Range", "Bars for friction range", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highs.Clear();
		_lows.Clear();
		_frictions.Clear();
		_midfHist.Clear();
		_lowf2Hist.Clear();
		_closeQueue.Clear();
		_prevFir = 0;
		_barCount = 0;

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

		_barCount++;

		_highs.Enqueue(candle.HighPrice);
		_lows.Enqueue(candle.LowPrice);
		while (_highs.Count > BarsBack) { _highs.Dequeue(); _lows.Dequeue(); }

		var highsArr = _highs.ToArray();
		var lowsArr = _lows.ToArray();
		var friction = 0m;
		for (var i = 0; i < highsArr.Length; i++)
		{
			if (highsArr[i] >= candle.ClosePrice && lowsArr[i] <= candle.ClosePrice)
				friction += (1m + BarsBack) / (i + 1 + BarsBack);
		}

		_frictions.Enqueue(friction);
		while (_frictions.Count > Range)
			_frictions.Dequeue();

		var lowf = decimal.MaxValue;
		var highf = decimal.MinValue;
		foreach (var f in _frictions)
		{
			if (f < lowf) lowf = f;
			if (f > highf) highf = f;
		}

		var fl = FrictionLevel / 100m;
		var tl = TriggerLevel / 100m;
		var midf = lowf * (1m - fl) + highf * fl;
		var lowf2 = lowf * (1m - tl) + highf * tl;

		_midfHist.Enqueue(midf);
		_lowf2Hist.Enqueue(lowf2);
		if (_midfHist.Count > 6) _midfHist.Dequeue();
		if (_lowf2Hist.Count > 6) _lowf2Hist.Dequeue();

		var midf5 = _midfHist.Count == 6 ? _midfHist.Peek() : midf;
		var lowf25 = _lowf2Hist.Count == 6 ? _lowf2Hist.Peek() : lowf2;

		_closeQueue.Enqueue(candle.ClosePrice);
		if (_closeQueue.Count > 4) _closeQueue.Dequeue();
		if (_closeQueue.Count < 4) return;

		var closeArr = _closeQueue.ToArray();
		var fir = (4m * closeArr[3] + 3m * closeArr[2] + 2m * closeArr[1] + closeArr[0]) / 10m;
		var trend = fir > _prevFir ? 1 : -1;
		_prevFir = fir;

		var longSignal = friction < lowf25 && trend == 1;
		var shortSignal = friction < lowf25 && trend == -1;
		var end = friction > midf5;

		if (longSignal && _barCount > 10 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && _barCount > 10 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		if (Position > 0 && (shortSignal || end))
			SellMarket(Math.Abs(Position));
		else if (Position < 0 && (longSignal || end))
			BuyMarket(Math.Abs(Position));
	}
}
