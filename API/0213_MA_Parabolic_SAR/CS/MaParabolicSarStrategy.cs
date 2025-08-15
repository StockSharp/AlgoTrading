using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;
	
/// <summary>
/// Strategy based on Moving Average and Parabolic SAR indicators.
/// Enters long when price is above MA and above SAR.
/// Enters short when price is below MA and below SAR.
/// Uses Parabolic SAR as dynamic stop-loss.
/// </summary>
public class MaParabolicSarStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaxStep;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Unit> _takeValue;
	private readonly StrategyParam<Unit> _stopValue;
	
	private SimpleMovingAverage _ma;
	private ParabolicSar _parabolicSar;
	
	private decimal _lastSarValue;
	
	/// <summary>
	/// Moving Average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}
	
	/// <summary>
	/// Parabolic SAR acceleration factor.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}
	
	/// <summary>
	/// Parabolic SAR maximum acceleration factor.
	/// </summary>
	public decimal SarMaxStep
	{
		get => _sarMaxStep.Value;
		set => _sarMaxStep.Value = value;
	}
	
	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Take profit value.
	/// </summary>
	public Unit TakeValue
	{
		get => _takeValue.Value;
		set => _takeValue.Value = value;
	}

	/// <summary>
	/// Stop loss value.
	/// </summary>
	public Unit StopValue
	{
		get => _stopValue.Value;
		set => _stopValue.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public MaParabolicSarStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);
			
		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Acceleration factor for Parabolic SAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.05m, 0.01m);
			
		_sarMaxStep = Param(nameof(SarMaxStep), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max Step", "Maximum acceleration factor for Parabolic SAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.3m, 0.05m);
			
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_takeValue = Param(nameof(TakeValue), new Unit(0, UnitTypes.Absolute))
			.SetDisplay("Take Profit", "Take profit value", "Protection");

		_stopValue = Param(nameof(StopValue), new Unit(2, UnitTypes.Percent))
			.SetDisplay("Stop Loss", "Stop loss value", "Protection");
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

		_ma = null;
		_parabolicSar = null;
		_lastSarValue = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_ma = new() { Length = MaPeriod };
		_parabolicSar = new ParabolicSar
		{
			AccelerationStep = SarStep,
			AccelerationMax = SarMaxStep
		};
		
		// Create candles subscription
		var subscription = SubscribeCandles(CandleType);
		
		// Bind indicators to subscription
		subscription
			.Bind(_ma, _parabolicSar, ProcessCandle)
			.Start();
		
		// Setup chart if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawIndicator(area, _parabolicSar);
			DrawOwnTrades(area);
		}

		// Start protection by take profit and stop loss (like SmaStrategy)
		StartProtection(TakeValue, StopValue);
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal sarValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;
			
		// Skip if strategy is not ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
			
		// Store current SAR value for stop-loss
		_lastSarValue = sarValue;
		
		// Trading logic
		bool isPriceAboveMA = candle.ClosePrice > maValue;
		bool isPriceAboveSAR = candle.ClosePrice > sarValue;
		
		// Long signal: Price above MA and above SAR
		if (isPriceAboveMA && isPriceAboveSAR)
		{
			if (Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Long Entry: Price({candle.ClosePrice}) > MA({maValue}) && Price > SAR({sarValue})");
			}
		}
		// Short signal: Price below MA and below SAR
		else if (!isPriceAboveMA && !isPriceAboveSAR)
		{
			if (Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Short Entry: Price({candle.ClosePrice}) < MA({maValue}) && Price < SAR({sarValue})");
			}
		}
		// Exit long position: Price falls below SAR
		else if (Position > 0 && !isPriceAboveSAR)
		{
			SellMarket(Math.Abs(Position));
			LogInfo($"Exit Long: Price({candle.ClosePrice}) < SAR({sarValue})");
		}
		// Exit short position: Price rises above SAR
		else if (Position < 0 && isPriceAboveSAR)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit Short: Price({candle.ClosePrice}) > SAR({sarValue})");
		}
	}
}
	
