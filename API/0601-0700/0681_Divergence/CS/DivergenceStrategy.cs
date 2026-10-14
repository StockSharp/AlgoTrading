using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Price and RSI divergence strategy.
/// A pivot is a candle whose low (high) is below (above) the lows (highs) of its neighbours on both sides.
/// A pivot low below the previous pivot low with a higher RSI is a bullish divergence and buys;
/// a pivot high above the previous pivot high with a lower RSI is a bearish divergence and sells.
/// TradeDirection ("Long", "Short" or "Both") limits the entries; an opposite divergence reverses or closes the position.
/// A percent stop loss and a take profit of RiskReward times the stop protect every position.
/// </summary>
public class DivergenceStrategy : Strategy
{
	private readonly StrategyParam<string> _tradeDirection;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal high, decimal low, decimal rsi)> _bars = [];
	private (decimal price, decimal rsi)? _lastPivotLow;
	private (decimal price, decimal rsi)? _lastPivotHigh;

	/// <summary>
	/// Allowed trade direction: Long, Short or Both.
	/// </summary>
	public string TradeDirection { get => _tradeDirection.Value; set => _tradeDirection.Value = value; }

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Take profit as a multiple of the stop loss.
	/// </summary>
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public DivergenceStrategy()
	{
		_tradeDirection = Param(nameof(TradeDirection), "Both")
			.SetDisplay("Trade Direction", "Allowed trade direction: Long, Short or Both", "Trading");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetNotNegative()
			.SetDisplay("Risk Reward", "Take profit as a multiple of the stop loss", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_bars.Clear();
		_lastPivotLow = null;
		_lastPivotHigh = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, ProcessCandle)
			.Start();

		var takePercent = StopLossPercent * RiskReward;
		StartProtection(
			takePercent > 0m ? new Unit(takePercent, UnitTypes.Percent) : new Unit(),
			StopLossPercent > 0m ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiValue.IsFormed)
			return;

		_bars.Add((candle.HighPrice, candle.LowPrice, rsiValue.GetValue<decimal>()));
		if (_bars.Count > 3)
			_bars.RemoveAt(0);

		if (_bars.Count < 3)
			return;

		// The middle bar is confirmed as a pivot once the bar after it has closed.
		var left = _bars[0];
		var mid = _bars[1];
		var right = _bars[2];

		var bullish = false;
		var bearish = false;

		if (mid.low < left.low && mid.low < right.low)
		{
			if (_lastPivotLow is { } prevLow && mid.low < prevLow.price && mid.rsi > prevLow.rsi)
				bullish = true;

			_lastPivotLow = (mid.low, mid.rsi);
		}

		if (mid.high > left.high && mid.high > right.high)
		{
			if (_lastPivotHigh is { } prevHigh && mid.high > prevHigh.price && mid.rsi < prevHigh.rsi)
				bearish = true;

			_lastPivotHigh = (mid.high, mid.rsi);
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var allowLong = !TradeDirection.EqualsIgnoreCase("Short");
		var allowShort = !TradeDirection.EqualsIgnoreCase("Long");

		if (bullish && !bearish)
		{
			if (allowLong && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (!allowLong && Position < 0)
				BuyMarket(-Position);
		}
		else if (bearish && !bullish)
		{
			if (allowShort && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (!allowShort && Position > 0)
				SellMarket(Position);
		}
	}
}
