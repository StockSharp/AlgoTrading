using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive Fibonacci Pullback strategy.
/// Three SuperTrend lines with Fibonacci multipliers are averaged and the average is smoothed by an EMA. A long needs the low to dip
/// below the average while the close stays above the smoothed line, the previous and current close above the Kaufman AMA midline and
/// RSI above RsiBuy; a short mirrors this with RSI below RsiSell. Positions close when the close crosses the smoothed line against them,
/// and percent take profit and stop loss protect every trade.
/// </summary>
public class AdaptiveFibonacciPullbackStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<decimal> _factor3;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<int> _amaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiBuy;
	private readonly StrategyParam<decimal> _rsiSell;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _smooth;
	private decimal? _prevClose;
	private decimal? _prevSmooth;

	/// <summary>
	/// ATR period of the SuperTrends.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier of the first SuperTrend.
	/// </summary>
	public decimal Factor1
	{
		get => _factor1.Value;
		set => _factor1.Value = value;
	}

	/// <summary>
	/// Multiplier of the second SuperTrend.
	/// </summary>
	public decimal Factor2
	{
		get => _factor2.Value;
		set => _factor2.Value = value;
	}

	/// <summary>
	/// Multiplier of the third SuperTrend.
	/// </summary>
	public decimal Factor3
	{
		get => _factor3.Value;
		set => _factor3.Value = value;
	}

	/// <summary>
	/// EMA length that smooths the SuperTrend average.
	/// </summary>
	public int SmoothLength
	{
		get => _smoothLength.Value;
		set => _smoothLength.Value = value;
	}

	/// <summary>
	/// Period of the AMA midline.
	/// </summary>
	public int AmaLength
	{
		get => _amaLength.Value;
		set => _amaLength.Value = value;
	}

	/// <summary>
	/// Period of RSI.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI level a long requires to exceed.
	/// </summary>
	public decimal RsiBuy
	{
		get => _rsiBuy.Value;
		set => _rsiBuy.Value = value;
	}

	/// <summary>
	/// RSI level a short requires to stay below.
	/// </summary>
	public decimal RsiSell
	{
		get => _rsiSell.Value;
		set => _rsiSell.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	public AdaptiveFibonacciPullbackStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period of the SuperTrends", "SuperTrend");

		_factor1 = Param(nameof(Factor1), 0.618m)
			.SetGreaterThanZero()
			.SetDisplay("Factor 1", "Multiplier of the first SuperTrend", "SuperTrend");

		_factor2 = Param(nameof(Factor2), 1.618m)
			.SetGreaterThanZero()
			.SetDisplay("Factor 2", "Multiplier of the second SuperTrend", "SuperTrend");

		_factor3 = Param(nameof(Factor3), 2.618m)
			.SetGreaterThanZero()
			.SetDisplay("Factor 3", "Multiplier of the third SuperTrend", "SuperTrend");

		_smoothLength = Param(nameof(SmoothLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Smooth Length", "EMA length that smooths the SuperTrend average", "SuperTrend");

		_amaLength = Param(nameof(AmaLength), 55)
			.SetGreaterThanZero()
			.SetDisplay("AMA Length", "Period of the AMA midline", "AMA");

		_rsiLength = Param(nameof(RsiLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period of RSI", "RSI");

		_rsiBuy = Param(nameof(RsiBuy), 70m)
			.SetDisplay("RSI Buy", "RSI level a long requires to exceed", "RSI");

		_rsiSell = Param(nameof(RsiSell), 30m)
			.SetDisplay("RSI Sell", "RSI level a short requires to stay below", "RSI");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 5m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 0.75m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
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
		_smooth = null;
		_prevClose = null;
		_prevSmooth = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevSmooth = null;

		var st1 = new SuperTrend { Length = AtrPeriod, Multiplier = Factor1 };
		var st2 = new SuperTrend { Length = AtrPeriod, Multiplier = Factor2 };
		var st3 = new SuperTrend { Length = AtrPeriod, Multiplier = Factor3 };
		var ama = new KaufmanAdaptiveMovingAverage { Length = AmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		_smooth = new ExponentialMovingAverage { Length = SmoothLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx([st1, st2, st3, ama, rsi], ProcessCandle)
			.Start();

		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ama);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		foreach (var value in values)
		{
			if (!value.IsFormed)
				return;
		}

		var average = (values[0].ToDecimal() + values[1].ToDecimal() + values[2].ToDecimal()) / 3m;
		var mid = values[3].ToDecimal();
		var rsi = values[4].ToDecimal();

		var smoothValue = _smooth.Process(average, candle.ServerTime, true);
		if (!_smooth.IsFormed)
			return;

		var smooth = smoothValue.ToDecimal();
		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		var prevSmooth = _prevSmooth;
		_prevClose = close;
		_prevSmooth = smooth;

		if (prevClose is not decimal pc || prevSmooth is not decimal ps)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var longEntry = candle.LowPrice < average && close > smooth && pc > mid && close > mid && rsi > RsiBuy;
		var shortEntry = candle.HighPrice > average && close < smooth && pc < mid && close < mid && rsi < RsiSell;

		if (longEntry && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortEntry && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && pc >= ps && close < smooth)
			SellMarket(Position);
		else if (Position < 0 && pc <= ps && close > smooth)
			BuyMarket(-Position);
	}
}
