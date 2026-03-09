using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "Mean Reversion" MetaTrader expert.
/// Buys after multi-bar sell-off when RSI is oversold, sells after multi-bar rally when RSI is overbought.
/// Uses consecutive bar count for exhaustion detection with RSI confirmation.
/// </summary>
public class MeanReversionMomentumStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _barsToCount;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;

	private RelativeStrengthIndex _rsi;
	private readonly List<decimal> _closeHistory = new();

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BarsToCount
	{
		get => _barsToCount.Value;
		set => _barsToCount.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	public MeanReversionMomentumStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_barsToCount = Param(nameof(BarsToCount), 5)
			.SetGreaterThanZero()
			.SetDisplay("Bars To Count", "Number of consecutive bars for exhaustion detection", "Signal");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period for confirmation", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI level for sell signal", "Signals");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI level for buy signal", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_closeHistory.Clear();
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closeHistory.Add(candle.ClosePrice);
		if (_closeHistory.Count > BarsToCount + 1)
			_closeHistory.RemoveAt(0);

		if (!_rsi.IsFormed || _closeHistory.Count < BarsToCount + 1)
			return;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Count consecutive down bars
		var downCount = 0;
		for (int i = _closeHistory.Count - 1; i >= 1; i--)
		{
			if (_closeHistory[i] < _closeHistory[i - 1])
				downCount++;
			else
				break;
		}

		// Count consecutive up bars
		var upCount = 0;
		for (int i = _closeHistory.Count - 1; i >= 1; i--)
		{
			if (_closeHistory[i] > _closeHistory[i - 1])
				upCount++;
			else
				break;
		}

		// Multi-bar sell-off + RSI oversold -> mean reversion buy
		if (downCount >= BarsToCount && rsiValue < RsiOversold)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		// Multi-bar rally + RSI overbought -> mean reversion sell
		else if (upCount >= BarsToCount && rsiValue > RsiOverbought)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_closeHistory.Clear();
		_rsi = null;

		base.OnReseted();
	}
}
