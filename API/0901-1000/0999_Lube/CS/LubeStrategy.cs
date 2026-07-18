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

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private readonly List<decimal> _frictions = new();
	private readonly List<decimal> _midfHist = new();
	private readonly List<decimal> _lowf2Hist = new();
	private readonly List<decimal> _closeList = new();
	private decimal _prevFir;
	private int _barCount;
	private int _cooldown;

	public int BarsBack { get => _barsBack.Value; set => _barsBack.Value = value; }
	public int FrictionLevel { get => _frictionLevel.Value; set => _frictionLevel.Value = value; }
	public int TriggerLevel { get => _triggerLevel.Value; set => _triggerLevel.Value = value; }
	public int Range { get => _range.Value; set => _range.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LubeStrategy()
	{
		_barsBack = Param(nameof(BarsBack), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bars Back", "Bars back for friction", "General");
		_frictionLevel = Param(nameof(FrictionLevel), 50)
			.SetDisplay("Friction Level", "Stop trade level", "General");
		_triggerLevel = Param(nameof(TriggerLevel), -10)
			.SetDisplay("Trigger Level", "Initiate trade level", "General");
		_range = Param(nameof(Range), 10)
			.SetGreaterThanZero()
			.SetDisplay("Range", "Bars for friction range", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(25).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
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
		_frictions.Clear();
		_midfHist.Clear();
		_lowf2Hist.Clear();
		_closeList.Clear();
		_prevFir = default;
		_barCount = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highs.Clear();
		_lows.Clear();
		_frictions.Clear();
		_midfHist.Clear();
		_lowf2Hist.Clear();
		_closeList.Clear();
		_prevFir = 0;
		_barCount = 0;
		_cooldown = 0;

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

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		while (_highs.Count > BarsBack) _highs.RemoveAt(0);
		while (_lows.Count > BarsBack) _lows.RemoveAt(0);

		var friction = 0m;
		var len = Math.Min(_highs.Count, _lows.Count);
		for (var i = 0; i < len; i++)
		{
			if (_highs[i] >= candle.ClosePrice && _lows[i] <= candle.ClosePrice)
				friction += (1m + BarsBack) / (i + 1 + BarsBack);
		}

		_frictions.Add(friction);
		while (_frictions.Count > Range)
			_frictions.RemoveAt(0);

		var lowf = decimal.MaxValue;
		var highf = decimal.MinValue;
		for (var i = 0; i < _frictions.Count; i++)
		{
			if (_frictions[i] < lowf) lowf = _frictions[i];
			if (_frictions[i] > highf) highf = _frictions[i];
		}

		var fl = FrictionLevel / 100m;
		var tl = TriggerLevel / 100m;
		var midf = lowf * (1m - fl) + highf * fl;
		var lowf2 = lowf * (1m - tl) + highf * tl;

		_midfHist.Add(midf);
		_lowf2Hist.Add(lowf2);
		if (_midfHist.Count > 6) _midfHist.RemoveAt(0);
		if (_lowf2Hist.Count > 6) _lowf2Hist.RemoveAt(0);

		var midf5 = _midfHist.Count == 6 ? _midfHist[0] : midf;
		var lowf25 = _lowf2Hist.Count == 6 ? _lowf2Hist[0] : lowf2;

		_closeList.Add(candle.ClosePrice);
		if (_closeList.Count > 4) _closeList.RemoveAt(0);
		if (_closeList.Count < 4) return;

		var fir = (4m * _closeList[3] + 3m * _closeList[2] + 2m * _closeList[1] + _closeList[0]) / 10m;
		var trend = fir > _prevFir ? 1 : -1;
		_prevFir = fir;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var longSignal = friction < lowf25 && trend == 1;
		var shortSignal = friction < lowf25 && trend == -1;
		var end = friction > midf5;

		if (longSignal && _barCount > 10 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_cooldown = 10;
			return;
		}

		if (shortSignal && _barCount > 10 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_cooldown = 10;
			return;
		}

		if (Position > 0 && end)
		{
			SellMarket();
			_cooldown = 10;
		}
		else if (Position < 0 && end)
		{
			BuyMarket();
			_cooldown = 10;
		}
	}
}
