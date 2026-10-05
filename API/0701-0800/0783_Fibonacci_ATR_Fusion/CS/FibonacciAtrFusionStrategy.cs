using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibonacci ATR Fusion strategy.
/// For each Fibonacci period 8, 13, 21, 34 and 55 the buying pressure (close minus the true low) summed over the period is divided
/// by the true range summed over the same period. The ratios are averaged with weights 5, 4, 3, 2, 1 and scaled to 0..100. A cross
/// above LongEntryThreshold goes long and a cross below ShortEntryThreshold goes short, reversing an opposite position; a long
/// closes when the average crosses below LongExitThreshold and a short when it crosses above ShortExitThreshold. Three take-profit
/// layers at Tp1Atr, Tp2Atr and Tp3Atr ATRs from the entry each close their percentage of the entry volume.
/// </summary>
public class FibonacciAtrFusionStrategy : Strategy
{
	private static readonly int[] _periods = [8, 13, 21, 34, 55];
	private static readonly decimal[] _weights = [5m, 4m, 3m, 2m, 1m];
	private const int _atrLength = 14;

	private readonly StrategyParam<decimal> _longEntryThreshold;
	private readonly StrategyParam<decimal> _shortEntryThreshold;
	private readonly StrategyParam<decimal> _longExitThreshold;
	private readonly StrategyParam<decimal> _shortExitThreshold;
	private readonly StrategyParam<decimal> _tp1Atr;
	private readonly StrategyParam<decimal> _tp2Atr;
	private readonly StrategyParam<decimal> _tp3Atr;
	private readonly StrategyParam<decimal> _tp1Percent;
	private readonly StrategyParam<decimal> _tp2Percent;
	private readonly StrategyParam<decimal> _tp3Percent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal bp, decimal tr)> _history = [];
	private decimal? _prevClose;
	private decimal? _prevAverage;
	private decimal _entryPrice;
	private decimal _entryAtr;
	private decimal _entryVolume;
	private readonly bool[] _tpDone = new bool[3];

	/// <summary>
	/// Level the average crosses upward to go long.
	/// </summary>
	public decimal LongEntryThreshold
	{
		get => _longEntryThreshold.Value;
		set => _longEntryThreshold.Value = value;
	}

	/// <summary>
	/// Level the average crosses downward to go short.
	/// </summary>
	public decimal ShortEntryThreshold
	{
		get => _shortEntryThreshold.Value;
		set => _shortEntryThreshold.Value = value;
	}

	/// <summary>
	/// Level the average crosses downward to close a long.
	/// </summary>
	public decimal LongExitThreshold
	{
		get => _longExitThreshold.Value;
		set => _longExitThreshold.Value = value;
	}

	/// <summary>
	/// Level the average crosses upward to close a short.
	/// </summary>
	public decimal ShortExitThreshold
	{
		get => _shortExitThreshold.Value;
		set => _shortExitThreshold.Value = value;
	}

	/// <summary>
	/// First take profit distance in ATRs.
	/// </summary>
	public decimal Tp1Atr
	{
		get => _tp1Atr.Value;
		set => _tp1Atr.Value = value;
	}

	/// <summary>
	/// Second take profit distance in ATRs.
	/// </summary>
	public decimal Tp2Atr
	{
		get => _tp2Atr.Value;
		set => _tp2Atr.Value = value;
	}

	/// <summary>
	/// Third take profit distance in ATRs.
	/// </summary>
	public decimal Tp3Atr
	{
		get => _tp3Atr.Value;
		set => _tp3Atr.Value = value;
	}

	/// <summary>
	/// Percent of the entry volume closed at the first take profit.
	/// </summary>
	public decimal Tp1Percent
	{
		get => _tp1Percent.Value;
		set => _tp1Percent.Value = value;
	}

	/// <summary>
	/// Percent of the entry volume closed at the second take profit.
	/// </summary>
	public decimal Tp2Percent
	{
		get => _tp2Percent.Value;
		set => _tp2Percent.Value = value;
	}

	/// <summary>
	/// Percent of the entry volume closed at the third take profit.
	/// </summary>
	public decimal Tp3Percent
	{
		get => _tp3Percent.Value;
		set => _tp3Percent.Value = value;
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
	public FibonacciAtrFusionStrategy()
	{
		_longEntryThreshold = Param(nameof(LongEntryThreshold), 58m)
			.SetDisplay("Long Entry", "Level the average crosses upward to go long", "Signals");

		_shortEntryThreshold = Param(nameof(ShortEntryThreshold), 42m)
			.SetDisplay("Short Entry", "Level the average crosses downward to go short", "Signals");

		_longExitThreshold = Param(nameof(LongExitThreshold), 42m)
			.SetDisplay("Long Exit", "Level the average crosses downward to close a long", "Signals");

		_shortExitThreshold = Param(nameof(ShortExitThreshold), 58m)
			.SetDisplay("Short Exit", "Level the average crosses upward to close a short", "Signals");

		_tp1Atr = Param(nameof(Tp1Atr), 3m)
			.SetNotNegative()
			.SetDisplay("TP1 ATR", "First take profit distance in ATRs", "Take Profit");

		_tp2Atr = Param(nameof(Tp2Atr), 8m)
			.SetNotNegative()
			.SetDisplay("TP2 ATR", "Second take profit distance in ATRs", "Take Profit");

		_tp3Atr = Param(nameof(Tp3Atr), 14m)
			.SetNotNegative()
			.SetDisplay("TP3 ATR", "Third take profit distance in ATRs", "Take Profit");

		_tp1Percent = Param(nameof(Tp1Percent), 12m)
			.SetNotNegative()
			.SetDisplay("TP1 %", "Percent of the entry volume closed at the first take profit", "Take Profit");

		_tp2Percent = Param(nameof(Tp2Percent), 12m)
			.SetNotNegative()
			.SetDisplay("TP2 %", "Percent of the entry volume closed at the second take profit", "Take Profit");

		_tp3Percent = Param(nameof(Tp3Percent), 12m)
			.SetNotNegative()
			.SetDisplay("TP3 %", "Percent of the entry volume closed at the third take profit", "Take Profit");

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
		_history.Clear();
		_prevClose = null;
		_prevAverage = null;
		ResetEntry(0m, 0m, 0m);
	}

	private void ResetEntry(decimal price, decimal atr, decimal volume)
	{
		_entryPrice = price;
		_entryAtr = atr;
		_entryVolume = volume;
		Array.Clear(_tpDone);
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var atr = new AverageTrueRange { Length = _atrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var average = UpdateAverage(candle);
		if (average is not decimal current)
			return;

		var prev = _prevAverage;
		_prevAverage = current;

		if (prev is not decimal last)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var longEntry = last <= LongEntryThreshold && current > LongEntryThreshold;
		var shortEntry = last >= ShortEntryThreshold && current < ShortEntryThreshold;
		var longExit = last >= LongExitThreshold && current < LongExitThreshold;
		var shortExit = last <= ShortExitThreshold && current > ShortExitThreshold;

		if (longEntry && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			ResetEntry(candle.ClosePrice, atr, Volume);
			return;
		}

		if (shortEntry && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			ResetEntry(candle.ClosePrice, atr, Volume);
			return;
		}

		if (Position > 0 && longExit)
		{
			SellMarket(Position);
			return;
		}

		if (Position < 0 && shortExit)
		{
			BuyMarket(-Position);
			return;
		}

		if (Position != 0)
			CheckTakeProfits(candle);
	}

	private decimal? UpdateAverage(ICandleMessage candle)
	{
		var prevClose = _prevClose;
		_prevClose = candle.ClosePrice;

		if (prevClose is not decimal lastClose)
			return null;

		var trueLow = Math.Min(candle.LowPrice, lastClose);
		var trueHigh = Math.Max(candle.HighPrice, lastClose);

		_history.Add((candle.ClosePrice - trueLow, trueHigh - trueLow));

		var maxPeriod = _periods[^1];
		if (_history.Count > maxPeriod)
			_history.RemoveAt(0);

		if (_history.Count < maxPeriod)
			return null;

		var weighted = 0m;
		var totalWeight = 0m;

		for (var i = 0; i < _periods.Length; i++)
		{
			var period = _periods[i];
			var bpSum = 0m;
			var trSum = 0m;

			for (var j = _history.Count - period; j < _history.Count; j++)
			{
				bpSum += _history[j].bp;
				trSum += _history[j].tr;
			}

			if (trSum <= 0)
				return null;

			weighted += _weights[i] * bpSum / trSum;
			totalWeight += _weights[i];
		}

		return weighted / totalWeight * 100m;
	}

	private void CheckTakeProfits(ICandleMessage candle)
	{
		if (_entryAtr <= 0)
			return;

		var distances = new[] { Tp1Atr, Tp2Atr, Tp3Atr };
		var percents = new[] { Tp1Percent, Tp2Percent, Tp3Percent };

		for (var i = 0; i < distances.Length; i++)
		{
			if (_tpDone[i] || distances[i] <= 0 || percents[i] <= 0)
				continue;

			var isLong = Position > 0;
			var target = isLong ? _entryPrice + distances[i] * _entryAtr : _entryPrice - distances[i] * _entryAtr;
			var hit = isLong ? candle.HighPrice >= target : candle.LowPrice <= target;

			if (!hit)
				continue;

			_tpDone[i] = true;

			var volume = Math.Min(Math.Abs(Position), RoundVolume(_entryVolume * percents[i] / 100m));
			if (volume <= 0)
				continue;

			if (isLong)
				SellMarket(volume);
			else
				BuyMarket(volume);

			return;
		}
	}

	private decimal RoundVolume(decimal volume)
	{
		var step = Security?.VolumeStep ?? 0m;
		return step > 0 ? Math.Floor(volume / step) * step : volume;
	}
}
