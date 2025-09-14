using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on color-coded Laguerre oscillator.
/// </summary>
public class ColorJLaguerreStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _middleLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevRsi;

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Upper threshold for exit signals.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Middle threshold used for trend detection.
	/// </summary>
	public decimal MiddleLevel
	{
		get => _middleLevel.Value;
		set => _middleLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold for exit signals.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
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
	/// Initializes a new instance of the <see cref="ColorJLaguerreStrategy"/>.
	/// </summary>
	public ColorJLaguerreStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
		.SetRange(5, 50)
		.SetDisplay("RSI Length", "Period for RSI", "Indicators")
		.SetCanOptimize(true);

		_highLevel = Param(nameof(HighLevel), 85m)
		.SetRange(60m, 95m)
		.SetDisplay("High Level", "Upper threshold", "Levels")
		.SetCanOptimize(true);

		_middleLevel = Param(nameof(MiddleLevel), 50m)
		.SetRange(30m, 70m)
		.SetDisplay("Middle Level", "Central threshold", "Levels")
		.SetCanOptimize(true);

		_lowLevel = Param(nameof(LowLevel), 15m)
		.SetRange(5m, 40m)
		.SetDisplay("Low Level", "Lower threshold", "Levels")
		.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
		.SetRange(0.5m, 5m)
		.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create RSI indicator as approximation of Laguerre oscillator
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		// Subscribe to candle data and bind indicator
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(rsi, ProcessCandle)
		.Start();

		// Enable stop loss protection
		StartProtection(
		takeProfit: null,
		stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
		useMarketOrders: true
		);

		// Draw candles, indicator, and trades on chart
		var area = CreateChartArea();
		if (area != null)
		{
		DrawCandles(area, subscription);
		DrawIndicator(area, rsi);
		DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
		return;

		// Ensure strategy can trade
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_prevRsi is null)
		{
		_prevRsi = rsi;
		return;
		}

		// Open long position when RSI crosses above middle level
		if (_prevRsi <= MiddleLevel && rsi > MiddleLevel && Position <= 0)
		{
		BuyMarket(Volume + Math.Abs(Position));
		}
		// Open short position when RSI crosses below middle level
		else if (_prevRsi >= MiddleLevel && rsi < MiddleLevel && Position >= 0)
		{
		SellMarket(Volume + Math.Abs(Position));
		}

		// Exit long position at high level
		if (Position > 0 && rsi >= HighLevel)
		{
		SellMarket(Math.Abs(Position));
		}
		// Exit short position at low level
		else if (Position < 0 && rsi <= LowLevel)
		{
		BuyMarket(Math.Abs(Position));
		}

		_prevRsi = rsi;
	}
}
