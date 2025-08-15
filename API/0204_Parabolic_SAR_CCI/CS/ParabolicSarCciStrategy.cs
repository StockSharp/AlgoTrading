using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Parabolic SAR and CCI indicators
/// </summary>
public class ParabolicSarCciStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarAccelerationFactor;
	private readonly StrategyParam<decimal> _sarMaxAccelerationFactor;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Parabolic SAR acceleration factor
	/// </summary>
	public decimal SarAccelerationFactor
	{
		get => _sarAccelerationFactor.Value;
		set => _sarAccelerationFactor.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration factor
	/// </summary>
	public decimal SarMaxAccelerationFactor
	{
		get => _sarMaxAccelerationFactor.Value;
		set => _sarMaxAccelerationFactor.Value = value;
	}

	/// <summary>
	/// CCI period
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for strategy
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	public ParabolicSarCciStrategy()
	{
		_sarAccelerationFactor = Param(nameof(SarAccelerationFactor), 0.02m)
			.SetRange(0.01m, 0.05m)
			.SetDisplay("SAR AF", "Parabolic SAR acceleration factor", "Indicators")
			.SetCanOptimize(true);

		_sarMaxAccelerationFactor = Param(nameof(SarMaxAccelerationFactor), 0.2m)
			.SetRange(0.1m, 0.5m)
			.SetDisplay("SAR Max AF", "Parabolic SAR maximum acceleration factor", "Indicators")
			.SetCanOptimize(true);

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("CCI Period", "Period for CCI indicator", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
	}

	/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
				base.OnStarted(time);

		// Initialize indicators
		var parabolicSar = new ParabolicSar
		{
			Acceleration = SarAccelerationFactor,
			AccelerationMax = SarMaxAccelerationFactor
		};

		var cci = new CommodityChannelIndex { Length = CciPeriod };

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(parabolicSar, cci, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue, decimal cciValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;

		// Trading logic:
		// Long: Price > SAR && CCI < -100 (trend up with oversold conditions)
		// Short: Price < SAR && CCI > 100 (trend down with overbought conditions)
		
		if (price > sarValue && cciValue < -100 && Position <= 0)
		{
			// Buy signal - trend up with oversold CCI
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (price < sarValue && cciValue > 100 && Position >= 0)
		{
			// Sell signal - trend down with overbought CCI
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		// Exit conditions based on SAR breakout (dynamic stop-loss)
		else if (Position > 0 && price < sarValue)
		{
			// Exit long position when price drops below SAR
			SellMarket(Position);
		}
		else if (Position < 0 && price > sarValue)
		{
			// Exit short position when price rises above SAR
			BuyMarket(Math.Abs(Position));
		}
	}
}
