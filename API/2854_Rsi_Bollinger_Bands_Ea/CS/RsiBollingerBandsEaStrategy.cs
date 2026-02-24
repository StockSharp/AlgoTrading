namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// RSI Bollinger Bands strategy.
/// Buys when RSI is oversold and price is near the lower Bollinger Band.
/// Sells when RSI is overbought and price is near the upper Bollinger Band.
/// </summary>
public class RsiBollingerBandsEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public int BbPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	public RsiBollingerBandsEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI length", "Indicators");

		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands length", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 35m)
			.SetDisplay("RSI Oversold", "Oversold level for buy signal", "Signals");

		_rsiOverbought = Param(nameof(RsiOverbought), 65m)
			.SetDisplay("RSI Overbought", "Overbought level for sell signal", "Signals");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var bb = new BollingerBands { Length = BbPeriod, Width = 2m };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, bb, (ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue bbValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var rsiVal = rsiValue.ToDecimal();
				var bbTyped = (BollingerBandsValue)bbValue;
				var bbUpper = bbTyped.UpBand;
				var bbLower = bbTyped.LowBand;

				// Buy when RSI is oversold and price near lower band
				if (rsiVal < RsiOversold && candle.ClosePrice <= bbLower && Position <= 0)
				{
					BuyMarket();
				}
				// Sell when RSI is overbought and price near upper band
				else if (rsiVal > RsiOverbought && candle.ClosePrice >= bbUpper && Position >= 0)
				{
					SellMarket();
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}
}
