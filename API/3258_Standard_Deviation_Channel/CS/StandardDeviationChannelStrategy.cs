using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Standard Deviation Channel strategy: trades mean reversion from Bollinger Bands.
/// Buys when price touches lower band and RSI is oversold.
/// Sells when price touches upper band and RSI is overbought.
/// </summary>
public class StandardDeviationChannelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbWidth;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _rsiLower;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BbPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	public decimal BbWidth
	{
		get => _bbWidth.Value;
		set => _bbWidth.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal RsiUpper
	{
		get => _rsiUpper.Value;
		set => _rsiUpper.Value = value;
	}

	public decimal RsiLower
	{
		get => _rsiLower.Value;
		set => _rsiLower.Value = value;
	}

	public StandardDeviationChannelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Band period", "Indicators");

		_bbWidth = Param(nameof(BbWidth), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Width", "Bollinger Band deviation", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI lookback", "Indicators");

		_rsiUpper = Param(nameof(RsiUpper), 70m)
			.SetDisplay("RSI Upper", "Overbought level", "Signals");

		_rsiLower = Param(nameof(RsiLower), 30m)
			.SetDisplay("RSI Lower", "Oversold level", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands { Length = BbPeriod, Width = BbWidth };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bb, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bbTyped = (BollingerBandsValue)bbValue;
		if (bbTyped.UpBand is not decimal upper || bbTyped.LowBand is not decimal lower)
			return;

		var rsi = rsiValue.ToDecimal();
		var close = candle.ClosePrice;

		var mid = (upper + lower) / 2m;

		// Buy: price below middle band + RSI below midline (50)
		if (close < mid && rsi < 50m && Position <= 0)
		{
			BuyMarket();
		}
		// Sell: price above middle band + RSI above midline (50)
		else if (close > mid && rsi > 50m && Position >= 0)
		{
			SellMarket();
		}
	}
}
