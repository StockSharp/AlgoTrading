using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class LivermoreSeykotaBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _trailAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _candles = new();
	private decimal? _lastPivotHigh;
	private decimal? _lastPivotLow;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int PivotLength { get => _pivotLength.Value; set => _pivotLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal TrailAtrMultiplier { get => _trailAtrMultiplier.Value; set => _trailAtrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LivermoreSeykotaBreakoutStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 20)
			.SetDisplay("EMA Length", "EMA trend period", "Indicators");
		_pivotLength = Param(nameof(PivotLength), 3)
			.SetDisplay("Pivot Length", "Bars for pivot", "General");
		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period", "Indicators");
		_trailAtrMultiplier = Param(nameof(TrailAtrMultiplier), 3m)
			.SetDisplay("Trail ATR Mult", "ATR trailing mult", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_candles.Clear();
		_lastPivotHigh = null;
		_lastPivotLow = null;
		_highestSinceEntry = 0;
		_lowestSinceEntry = decimal.MaxValue;

		var ema = new EMA { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track pivots
		_candles.Add(candle);
		var maxCount = PivotLength * 2 + 1;
		if (_candles.Count > maxCount)
			_candles.RemoveAt(0);

		if (_candles.Count == maxCount)
		{
			var pivotIndex = PivotLength;
			var pivotCandle = _candles[pivotIndex];
			var isHigh = true;
			var isLow = true;

			for (var i = 0; i < maxCount; i++)
			{
				if (i == pivotIndex) continue;
				if (_candles[i].HighPrice >= pivotCandle.HighPrice) isHigh = false;
				if (_candles[i].LowPrice <= pivotCandle.LowPrice) isLow = false;
			}

			if (isHigh) _lastPivotHigh = pivotCandle.HighPrice;
			if (isLow) _lastPivotLow = pivotCandle.LowPrice;
		}

		if (atr <= 0) return;

		// Exit logic first
		if (Position > 0)
		{
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
			var trailStop = _highestSinceEntry - atr * TrailAtrMultiplier;
			if (candle.ClosePrice <= trailStop)
			{
				SellMarket();
				return;
			}
		}
		else if (Position < 0)
		{
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);
			var trailStop = _lowestSinceEntry + atr * TrailAtrMultiplier;
			if (candle.ClosePrice >= trailStop)
			{
				BuyMarket();
				return;
			}
		}

		// Entry logic - only when flat
		if (Position == 0)
		{
			if (_lastPivotHigh.HasValue && candle.ClosePrice > _lastPivotHigh.Value
				&& candle.ClosePrice > emaVal)
			{
				BuyMarket();
				_highestSinceEntry = candle.HighPrice;
			}
			else if (_lastPivotLow.HasValue && candle.ClosePrice < _lastPivotLow.Value
				&& candle.ClosePrice < emaVal)
			{
				SellMarket();
				_lowestSinceEntry = candle.LowPrice;
			}
		}
	}
}
