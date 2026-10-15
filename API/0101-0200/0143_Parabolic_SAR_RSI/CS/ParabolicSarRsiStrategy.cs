using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR RSI strategy.
/// A close above the Parabolic SAR with RSI below RsiOversold goes long and a close below the SAR with RSI above RsiOverbought goes short,
/// reversing an opposite position. The SAR is the trailing stop: a long closes when price closes below it and a short when price closes above it.
/// </summary>
public class ParabolicSarRsiStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarAf;
	private readonly StrategyParam<decimal> _sarMaxAf;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Initial acceleration factor of the SAR.
	/// </summary>
	public decimal SarAf
	{
		get => _sarAf.Value;
		set => _sarAf.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor of the SAR.
	/// </summary>
	public decimal SarMaxAf
	{
		get => _sarMaxAf.Value;
		set => _sarMaxAf.Value = value;
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
	public ParabolicSarRsiStrategy()
	{
		_sarAf = Param(nameof(SarAf), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Acceleration", "Initial acceleration factor of the SAR", "SAR");

		_sarMaxAf = Param(nameof(SarMaxAf), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max Acceleration", "Maximum acceleration factor of the SAR", "SAR");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period of RSI", "RSI");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI level for longs", "RSI");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
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

		var sar = new ParabolicSar
		{
			Acceleration = SarAf,
			AccelerationMax = SarMaxAf,
		};
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sar, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, rsi);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue sarValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The first SAR value is formed but empty.
		if (!sarValue.IsFormed || sarValue.IsEmpty || !rsiValue.IsFormed)
			return;

		var sar = sarValue.GetValue<decimal>();
		var rsi = rsiValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (close > sar && rsi < RsiOversold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < sar && rsi > RsiOverbought && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close < sar)
			SellMarket(Position);
		else if (Position < 0 && close > sar)
			BuyMarket(-Position);
	}
}
