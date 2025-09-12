using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI based strategy with adjustable parameters and stop-loss.
/// Buys when RSI is below the threshold and exits on price breakout above previous high.
/// </summary>
public class RsiStrategyWithAdjustableRsiAndStopLossStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousHigh;

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI value that triggers long entry.
	/// </summary>
	public decimal RsiThreshold
	{
		get => _rsiThreshold.Value;
		set => _rsiThreshold.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RsiStrategyWithAdjustableRsiAndStopLossStrategy"/>.
	/// </summary>
	public RsiStrategyWithAdjustableRsiAndStopLossStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Number of bars for RSI calculation", "Indicator");

		_rsiThreshold = Param(nameof(RsiThreshold), 28m)
			.SetRange(1m, 50m)
			.SetDisplay("RSI Threshold", "RSI value to trigger entries", "Signal");

		_stopLossPercent = Param(nameof(StopLossPercent), 5m)
			.SetRange(0.1m, 100m)
			.SetDisplay("Stop Loss %", "Percentage based stop loss", "Risk Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_previousHigh = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiLength
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(0, UnitTypes.Absolute),
			new Unit(StopLossPercent, UnitTypes.Percent),
			false);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (rsiValue < RsiThreshold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			LogInfo($"Long entry at RSI {rsiValue} below threshold {RsiThreshold}");
		}
		else if (Position > 0 && candle.ClosePrice > _previousHigh)
		{
			SellMarket(Position);
			LogInfo($"Exit long: price {candle.ClosePrice} broke previous high {_previousHigh}");
		}

		_previousHigh = candle.HighPrice;
	}
}

