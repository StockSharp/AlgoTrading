using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakthrough Volatility strategy: ATR breakout.
/// Buys when close breaks above previous high by more than ATR threshold.
/// Sells when close breaks below previous low by more than ATR threshold.
/// </summary>
public class BreakthroughVolatilityStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;

	private decimal? _prevHigh;
	private decimal? _prevLow;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	public BreakthroughVolatilityStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for volatility measurement", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier for breakout threshold", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHigh = null;
		_prevLow = null;

		var atr = new AverageTrueRange { Length = AtrPeriod };

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

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var threshold = atrValue * AtrMultiplier;

		if (_prevHigh.HasValue && _prevLow.HasValue && threshold > 0)
		{
			// Breakout above previous high + ATR threshold
			if (close > _prevHigh.Value + threshold && Position <= 0)
			{
				BuyMarket();
			}
			// Breakdown below previous low - ATR threshold
			else if (close < _prevLow.Value - threshold && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
