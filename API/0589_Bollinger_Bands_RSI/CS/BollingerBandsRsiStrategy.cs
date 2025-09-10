namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on Bollinger Bands and RSI.
/// </summary>
public class BollingerBandsRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiExit;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }
	public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public decimal RsiExit { get => _rsiExit.Value; set => _rsiExit.Value = value; }

	public BollingerBandsRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Period of Bollinger Bands", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Multiplier", "Standard deviation multiplier", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period of RSI", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetNotNegative()
			.SetDisplay("RSI Overbought", "RSI level considered overbought", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetNotNegative()
			.SetDisplay("RSI Oversold", "RSI level considered oversold", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_rsiExit = Param(nameof(RsiExit), 50m)
			.SetNotNegative()
			.SetDisplay("RSI Exit", "RSI level for exiting", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(40m, 60m, 5m);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerMultiplier
		};

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bollinger, rsi, Process)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.ClosePrice < lower && rsi < RsiOversold && Position <= 0)
		{
			BuyMarket();
		}
		else if (Position > 0 && candle.ClosePrice > upper && rsi > RsiOverbought)
		{
			SellMarket();
		}
		else if (Position > 0 && rsi > RsiExit)
		{
			SellMarket();
		}
	}
}
