using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gandalf PRO trend-following strategy using adaptive smoothing filter.
/// Opens trades when projected price exceeds a buffer threshold.
/// </summary>
public class GandalfProProjectionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _filterLength;
	private readonly StrategyParam<decimal> _priceFactor;
	private readonly StrategyParam<decimal> _trendFactor;
	private readonly StrategyParam<int> _atrLength;

	private readonly List<decimal> _closeBuffer = new();
	private decimal _entryPrice;

	public GandalfProProjectionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_filterLength = Param(nameof(FilterLength), 24)
			.SetDisplay("Filter Length", "Smoothing filter length.", "Filter");

		_priceFactor = Param(nameof(PriceFactor), 0.18m)
			.SetDisplay("Price Factor", "Close price weight in filter.", "Filter");

		_trendFactor = Param(nameof(TrendFactor), 0.18m)
			.SetDisplay("Trend Factor", "Trend term weight in filter.", "Filter");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period for entry buffer.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FilterLength
	{
		get => _filterLength.Value;
		set => _filterLength.Value = value;
	}

	public decimal PriceFactor
	{
		get => _priceFactor.Value;
		set => _priceFactor.Value = value;
	}

	public decimal TrendFactor
	{
		get => _trendFactor.Value;
		set => _trendFactor.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_closeBuffer.Clear();
		_entryPrice = 0;

		var atr = new AverageTrueRange { Length = AtrLength };

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

	private void ProcessCandle(ICandleMessage candle, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closeBuffer.Add(candle.ClosePrice);
		var maxDepth = FilterLength + 2;
		while (_closeBuffer.Count > maxDepth)
			_closeBuffer.RemoveAt(0);

		if (_closeBuffer.Count <= FilterLength || atrVal <= 0)
			return;

		var close = candle.ClosePrice;
		var target = CalculateTarget();
		if (target == null)
			return;

		var targetPrice = target.Value;
		var buffer = atrVal * 0.3m;

		// Manage position
		if (Position > 0)
		{
			// Exit if projection flips below close or on stop
			if (targetPrice < close - buffer)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (targetPrice > close + buffer)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entry
		if (Position == 0)
		{
			if (targetPrice > close + buffer)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (targetPrice < close - buffer)
			{
				_entryPrice = close;
				SellMarket();
			}
		}
	}

	private decimal? CalculateTarget()
	{
		var n = FilterLength;
		if (n < 2 || _closeBuffer.Count < n + 1)
			return null;

		var sum = 0m;
		for (var i = 1; i <= n; i++)
			sum += GetClose(i);

		var sm = sum / n;

		var weightedSum = 0m;
		for (var i = 0; i < n; i++)
		{
			var price = GetClose(i + 1);
			var weight = n - i;
			weightedSum += price * weight;
		}

		var denominator = (decimal)n * (n + 1) / 2m;
		if (denominator <= 0m)
			return null;

		var lm = weightedSum / denominator;
		var divisor = n - 1;
		if (divisor <= 0)
			return null;

		var s = new decimal[n + 2];
		var t = new decimal[n + 2];

		var tn = (6m * lm - 6m * sm) / divisor;
		var sn = 4m * sm - 3m * lm - tn;
		s[n] = sn;
		t[n] = tn;

		for (var k = n - 1; k > 0; k--)
		{
			var close = GetClose(k);
			s[k] = PriceFactor * close + (1m - PriceFactor) * (s[k + 1] + t[k + 1]);
			t[k] = TrendFactor * (s[k] - s[k + 1]) + (1m - TrendFactor) * t[k + 1];
		}

		return s[1] + t[1];
	}

	private decimal GetClose(int index)
	{
		var idx = _closeBuffer.Count - 1 - index;
		if (idx < 0) idx = 0;
		if (idx >= _closeBuffer.Count) idx = _closeBuffer.Count - 1;
		return _closeBuffer[idx];
	}
}
