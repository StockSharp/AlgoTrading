using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AUD/USD scalping strategy.
/// In an uptrend (fast EMA above slow EMA) a candle touching the lower Bollinger Band with RSI above RsiOversold goes long; in a
/// downtrend a candle touching the upper band with RSI below RsiOverbought goes short, reversing an opposite position. Fixed
/// price distances TakeProfit and StopLoss close the position.
/// </summary>
public class AudUsdScalpingStrategy : Strategy
{
	private readonly StrategyParam<int> _emaShort;
	private readonly StrategyParam<int> _emaLong;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int EmaShort
	{
		get => _emaShort.Value;
		set => _emaShort.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int EmaLong
	{
		get => _emaLong.Value;
		set => _emaLong.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level shorts must stay below.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI level longs must stay above.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands width multiplier.
	/// </summary>
	public decimal BbMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
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
	public AudUsdScalpingStrategy()
	{
		_emaShort = Param(nameof(EmaShort), 13)
			.SetGreaterThanZero()
			.SetDisplay("EMA Short", "Fast EMA period", "Trend");

		_emaLong = Param(nameof(EmaLong), 26)
			.SetGreaterThanZero()
			.SetDisplay("EMA Long", "Slow EMA period", "Trend");

		_rsiPeriod = Param(nameof(RsiPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "RSI");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI level shorts must stay below", "RSI");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI level longs must stay above", "RSI");

		_bbLength = Param(nameof(BbLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger");

		_bbMultiplier = Param(nameof(BbMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("BB Multiplier", "Bollinger Bands width multiplier", "Bollinger");

		_takeProfit = Param(nameof(TakeProfit), 0.0005m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take-profit distance in price", "Risk");

		_stopLoss = Param(nameof(StopLoss), 0.0004m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance in price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		var emaShort = new ExponentialMovingAverage { Length = EmaShort };
		var emaLong = new ExponentialMovingAverage { Length = EmaLong };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var bollinger = new BollingerBands { Length = BbLength, Width = BbMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(emaShort, emaLong, rsi, bollinger, ProcessCandle)
			.Start();

		StartProtection(
			TakeProfit > 0m ? new Unit(TakeProfit, UnitTypes.Absolute) : new Unit(),
			StopLoss > 0m ? new Unit(StopLoss, UnitTypes.Absolute) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaShort);
			DrawIndicator(area, emaLong);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaShortValue, IIndicatorValue emaLongValue, IIndicatorValue rsiValue, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!emaShortValue.IsFormed || !emaLongValue.IsFormed || !rsiValue.IsFormed || !bollingerValue.IsFormed)
			return;

		if (bollingerValue is not BollingerBandsValue { UpBand: decimal upper, LowBand: decimal lower })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var fast = emaShortValue.ToDecimal();
		var slow = emaLongValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();

		if (fast > slow && candle.LowPrice <= lower && rsi > RsiOversold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (fast < slow && candle.HighPrice >= upper && rsi < RsiOverbought && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
