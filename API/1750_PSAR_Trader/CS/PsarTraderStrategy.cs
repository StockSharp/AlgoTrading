using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR cross strategy with optional time filter.
/// Opens long when price moves above SAR and short when price moves below SAR.
/// </summary>
public class PsarTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaxStep;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<bool> _closeOnOpposite;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Unit> _takeValue;
	private readonly StrategyParam<Unit> _stopValue;
	
	private ParabolicSar _parabolicSar;
	private bool? _prevPriceAboveSar;
	
	/// <summary>
	/// Parabolic SAR acceleration factor.
	/// </summary>
	public decimal SarStep { get => _sarStep.Value; set => _sarStep.Value = value; }
	
	/// <summary>
	/// Parabolic SAR maximum acceleration factor.
	/// </summary>
	public decimal SarMaxStep { get => _sarMaxStep.Value; set => _sarMaxStep.Value = value; }
	
	/// <summary>
	/// UTC start hour for trading.
	/// </summary>
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	
	/// <summary>
	/// UTC end hour for trading.
	/// </summary>
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }
	
	/// <summary>
	/// Close existing position on opposite signal.
	/// </summary>
	public bool CloseOnOpposite { get => _closeOnOpposite.Value; set => _closeOnOpposite.Value = value; }
	
	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Take profit value.
	/// </summary>
	public Unit TakeValue { get => _takeValue.Value; set => _takeValue.Value = value; }
	
	/// <summary>
	/// Stop loss value.
	/// </summary>
	public Unit StopValue { get => _stopValue.Value; set => _stopValue.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public PsarTraderStrategy()
	{
		_sarStep = Param(nameof(SarStep), 0.001m)
		.SetGreaterThanZero()
		.SetDisplay("SAR Step", "Acceleration factor for Parabolic SAR", "Parabolic SAR")
		.SetCanOptimize(true)
		.SetOptimize(0.0005m, 0.02m, 0.0005m);
		
		_sarMaxStep = Param(nameof(SarMaxStep), 0.2m)
		.SetGreaterThanZero()
		.SetDisplay("SAR Max Step", "Maximum acceleration factor for Parabolic SAR", "Parabolic SAR")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 0.3m, 0.05m);
		
		_startHour = Param(nameof(StartHour), 0)
		.SetDisplay("Start Hour", "Start hour of trading session (UTC)", "Session");
		
		_endHour = Param(nameof(EndHour), 23)
		.SetDisplay("End Hour", "End hour of trading session (UTC)", "Session");
		
		_closeOnOpposite = Param(nameof(CloseOnOpposite), true)
		.SetDisplay("Close On Opposite", "Reverse position on opposite signal", "Trading");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
		
		_takeValue = Param(nameof(TakeValue), new Unit(50, UnitTypes.Absolute))
		.SetDisplay("Take Profit", "Take profit value", "Protection");
		
		_stopValue = Param(nameof(StopValue), new Unit(50, UnitTypes.Absolute))
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
		
		_parabolicSar = null;
		_prevPriceAboveSar = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_parabolicSar = new ParabolicSar
		{
			AccelerationStep = SarStep,
			AccelerationMax = SarMaxStep
		};
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_parabolicSar, ProcessCandle).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _parabolicSar);
			DrawOwnTrades(area);
		}
		
		StartProtection(TakeValue, StopValue);
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		// Work only with finished candles
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		// Session filter
		var hour = candle.OpenTime.UtcDateTime.Hour;
		var inSession = hour >= StartHour && hour <= EndHour;
		
		var isPriceAboveSar = candle.ClosePrice > sarValue;
		var cross = _prevPriceAboveSar.HasValue && _prevPriceAboveSar.Value != isPriceAboveSar;
		
		if (cross && inSession)
		{
			var volume = Volume + Math.Abs(Position);
			
			if (isPriceAboveSar && Position <= 0)
			{
				BuyMarket(volume);
			}
			else if (!isPriceAboveSar && Position >= 0)
			{
				SellMarket(volume);
			}
		}
		else if (CloseOnOpposite && inSession)
		{
			if (isPriceAboveSar && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
			}
			else if (!isPriceAboveSar && Position > 0)
			{
				SellMarket(Math.Abs(Position));
			}
		}
		
		_prevPriceAboveSar = isPriceAboveSar;
	}
}
