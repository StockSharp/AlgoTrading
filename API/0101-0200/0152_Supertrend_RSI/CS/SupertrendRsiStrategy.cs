using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supertrend RSI strategy.
/// A close above the Supertrend line with RSI below RsiOversold goes long and a close below it with RSI above RsiOverbought goes short,
/// reversing an opposite position. The Supertrend line is the trailing stop: a long closes when Supertrend flips down and a short
/// when it flips up.
/// </summary>
public class SupertrendRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// ATR period of Supertrend.
	/// </summary>
	public int SupertrendPeriod
	{
		get => _supertrendPeriod.Value;
		set => _supertrendPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier of Supertrend.
	/// </summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	/// <summary>
	/// Period of RSI.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level for longs.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// RSI level for shorts.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
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
	public SupertrendRsiStrategy()
	{
		_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Period", "ATR period of Supertrend", "Supertrend");

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Multiplier", "ATR multiplier of Supertrend", "Supertrend");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period of RSI", "RSI");

		_rsiOversold = Param(nameof(RsiOversold), 40m)
			.SetDisplay("RSI Oversold", "RSI level for longs", "RSI");

		_rsiOverbought = Param(nameof(RsiOverbought), 60m)
			.SetDisplay("RSI Overbought", "RSI level for shorts", "RSI");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var supertrend = new SuperTrend { Length = SupertrendPeriod, Multiplier = SupertrendMultiplier };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(supertrend, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, rsi);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue supertrendValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!supertrendValue.IsFormed || !rsiValue.IsFormed || supertrendValue is not SuperTrendIndicatorValue trend)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var line = trend.Value;
		var isUpTrend = trend.IsUpTrend;
		var rsi = rsiValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (close > line && rsi < RsiOversold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < line && rsi > RsiOverbought && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && !isUpTrend)
			SellMarket(Position);
		else if (Position < 0 && isUpTrend)
			BuyMarket(-Position);
	}
}
