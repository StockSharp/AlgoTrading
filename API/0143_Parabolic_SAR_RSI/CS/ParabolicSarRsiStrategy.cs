using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that combines Parabolic SAR for trend direction
/// and RSI for entry confirmation with oversold/overbought conditions.
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
	/// Parabolic SAR acceleration factor.
	/// </summary>
	public decimal SarAf
	{
		get => _sarAf.Value;
		set => _sarAf.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration factor.
	/// </summary>
	public decimal SarMaxAf
	{
		get => _sarMaxAf.Value;
		set => _sarMaxAf.Value = value;
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
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Strategy constructor.
	/// </summary>
	public ParabolicSarRsiStrategy()
	{
		_sarAf = Param(nameof(SarAf), 0.02m)
			.SetRange(0.01m, 0.1m)
			.SetDisplay("SAR Acceleration Factor", "Initial acceleration factor for Parabolic SAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.05m, 0.01m);

		_sarMaxAf = Param(nameof(SarMaxAf), 0.2m)
			.SetRange(0.1m, 0.5m)
			.SetDisplay("SAR Max Acceleration", "Maximum acceleration factor for Parabolic SAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.3m, 0.1m);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period of the RSI indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 7);

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetNotNegative()
			.SetDisplay("RSI Oversold", "RSI level considered oversold", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetNotNegative()
			.SetDisplay("RSI Overbought", "RSI level considered overbought", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		// Create indicators
		var parabolicSar = new ParabolicSar
		{
			Acceleration = SarAf,
			AccelerationMax = SarMaxAf,
			AccelerationStep = SarAf // Using initial AF as the step
		};

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(parabolicSar, rsi, ProcessCandles)
			.Start();

		// Enable dynamic stop-loss using Parabolic SAR
		StartProtection(
			takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
			stopLoss: new Unit(0, UnitTypes.Absolute) // No fixed stop loss - using dynamic SAR
		);

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	/// <summary>
	/// Process candles and indicator values.
	/// </summary>
	private void ProcessCandles(ICandleMessage candle, decimal sarValue, decimal rsiValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Long entry: price above SAR and RSI oversold
		if (candle.ClosePrice > sarValue && rsiValue < RsiOversold && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		// Short entry: price below SAR and RSI overbought
		else if (candle.ClosePrice < sarValue && rsiValue > RsiOverbought && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		// Long exit: price falls below SAR (trend change)
		else if (Position > 0 && candle.ClosePrice < sarValue)
		{
			SellMarket(Math.Abs(Position));
		}
		// Short exit: price rises above SAR (trend change)
		else if (Position < 0 && candle.ClosePrice > sarValue)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}