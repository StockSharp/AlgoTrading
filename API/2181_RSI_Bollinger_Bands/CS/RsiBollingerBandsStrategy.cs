namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// RSI combined with Bollinger Bands strategy.
/// Buys when RSI is oversold and price is below the lower band.
/// Sells when RSI is overbought and price is above the upper band.
/// </summary>
public class RsiBollingerBandsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;

	private RelativeStrengthIndex _rsi;
	private BollingerBands _bollinger;

	public RsiBollingerBandsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 20)
			.SetDisplay("RSI Period", "RSI calculation length", "Indicators");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("Bollinger Period", "Bollinger bands length", "Indicators");

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
			.SetDisplay("Bollinger Width", "Band width multiplier", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "Buy threshold", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "Sell threshold", "Indicators");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public decimal BollingerWidth { get => _bollingerWidth.Value; set => _bollingerWidth.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerWidth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _bollinger, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var buySignal = rsi < RsiOversold && candle.ClosePrice < lower;
		var sellSignal = rsi > RsiOverbought && candle.ClosePrice > upper;

		if (buySignal && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (sellSignal && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
	}
}

