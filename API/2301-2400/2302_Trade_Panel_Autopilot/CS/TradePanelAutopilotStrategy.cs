using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trade panel autopilot strategy v2.
/// Aggregates 7 price comparison metrics over rolling window.
/// Buys when buy percentage exceeds open threshold, sells on opposite.
/// </summary>
public class TradePanelAutopilotStrategy : Strategy
{
	private readonly StrategyParam<decimal> _openThreshold;
	private readonly StrategyParam<decimal> _closeThreshold;
	private readonly StrategyParam<int> _windowSize;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<(int buy, int sell)> _signalWindow = new();
	private ICandleMessage _prevCandle;

	public decimal OpenThreshold { get => _openThreshold.Value; set => _openThreshold.Value = value; }
	public decimal CloseThreshold { get => _closeThreshold.Value; set => _closeThreshold.Value = value; }
	public int WindowSize { get => _windowSize.Value; set => _windowSize.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TradePanelAutopilotStrategy()
	{
		_openThreshold = Param(nameof(OpenThreshold), 65m)
			.SetDisplay("Open %", "Threshold for new position", "General");

		_closeThreshold = Param(nameof(CloseThreshold), 40m)
			.SetDisplay("Close %", "Threshold for closing", "General");

		_windowSize = Param(nameof(WindowSize), 8)
			.SetGreaterThanZero()
			.SetDisplay("Window Size", "Number of candles for signal aggregation", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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
		_prevCandle = null;
		_signalWindow.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCandle = null;
		_signalWindow.Clear();

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

		if (_prevCandle == null)
		{
			_prevCandle = candle;
			return;
		}

		int buy = 0, sell = 0;

		if (candle.OpenPrice > _prevCandle.OpenPrice) buy++; else sell++;
		if (candle.HighPrice > _prevCandle.HighPrice) buy++; else sell++;
		if (candle.LowPrice > _prevCandle.LowPrice) buy++; else sell++;
		if (candle.ClosePrice > _prevCandle.ClosePrice) buy++; else sell++;

		var hlCurr = (candle.HighPrice + candle.LowPrice) / 2m;
		var hlPrev = (_prevCandle.HighPrice + _prevCandle.LowPrice) / 2m;
		if (hlCurr > hlPrev) buy++; else sell++;

		var hlcCurr = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var hlcPrev = (_prevCandle.HighPrice + _prevCandle.LowPrice + _prevCandle.ClosePrice) / 3m;
		if (hlcCurr > hlcPrev) buy++; else sell++;

		var hlccCurr = (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m;
		var hlccPrev = (_prevCandle.HighPrice + _prevCandle.LowPrice + 2m * _prevCandle.ClosePrice) / 4m;
		if (hlccCurr > hlccPrev) buy++; else sell++;

		_signalWindow.Enqueue((buy, sell));

		while (_signalWindow.Count > WindowSize)
			_signalWindow.Dequeue();

		_prevCandle = candle;

		if (_signalWindow.Count < WindowSize)
			return;

		int totalBuy = 0, totalSell = 0;
		foreach (var (b, s) in _signalWindow)
		{
			totalBuy += b;
			totalSell += s;
		}

		var total = totalBuy + totalSell;
		if (total == 0) return;

		var buyPct = (decimal)totalBuy / total * 100m;
		var sellPct = (decimal)totalSell / total * 100m;

		if (Position > 0 && buyPct < CloseThreshold)
			SellMarket();
		else if (Position < 0 && sellPct < CloseThreshold)
			BuyMarket();

		if (Position == 0)
		{
			if (buyPct >= OpenThreshold)
				BuyMarket();
			else if (sellPct >= OpenThreshold)
				SellMarket();
		}
	}
}
