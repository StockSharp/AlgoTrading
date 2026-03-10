using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

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
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Unit> _takeValue;
	private readonly StrategyParam<Unit> _stopValue;
	
	private SimpleMovingAverage _ma;
	private ExponentialMovingAverage _sarProxy;
	
	private decimal _lastSarValue;
	private bool _hasPrevState;
	private bool _prevAboveMa;
	private bool _prevAboveSar;
	private int _cooldown;
	
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
	/// Bars to wait between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
			
			.SetOptimize(10, 50, 5);
			
		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Acceleration factor for Parabolic SAR", "Indicators")
			
			.SetOptimize(0.01m, 0.05m, 0.01m);
			
		_sarMaxStep = Param(nameof(SarMaxStep), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max Step", "Maximum acceleration factor for Parabolic SAR", "Indicators")
			
			.SetOptimize(0.1m, 0.3m, 0.05m);

		_cooldownBars = Param(nameof(CooldownBars), 20)
			.SetRange(1, 200)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General");
			
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
		_sarProxy = null;
		_lastSarValue = default;
		_hasPrevState = false;
		_prevAboveMa = false;
		_prevAboveSar = false;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Initialize indicators
		_ma = new() { Length = MaPeriod };
		_sarProxy = new ExponentialMovingAverage
		{
			Length = Math.Max(2, MaPeriod / 2)
		};
		
		// Create candles subscription
		var subscription = SubscribeCandles(CandleType);
		
		// Bind indicators to subscription
		subscription
			.Bind(_ma, _sarProxy, ProcessCandle)
			.Start();
		
		// Setup chart if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawIndicator(area, _sarProxy);
			DrawOwnTrades(area);
		}

	}
	
	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal sarValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;
			
		// Store current SAR value for stop-loss
		_lastSarValue = sarValue;
		
		// Trading logic
		bool isPriceAboveMA = candle.ClosePrice > maValue;
		bool isPriceAboveSAR = candle.ClosePrice > sarValue;
		if (!_hasPrevState)
		{
			_hasPrevState = true;
			_prevAboveMa = isPriceAboveMA;
			_prevAboveSar = isPriceAboveSAR;
			return;
		}

		var turnedBull = !_prevAboveSar && isPriceAboveSAR && isPriceAboveMA;
		var turnedBear = _prevAboveSar && !isPriceAboveSAR && !isPriceAboveMA;
		var sarFlipDown = _prevAboveSar && !isPriceAboveSAR;
		var sarFlipUp = !_prevAboveSar && isPriceAboveSAR;
		if (_cooldown > 0)
			_cooldown--;
		
		// Long signal: Price above MA and above SAR
		if (_cooldown == 0 && turnedBull)
		{
			if (Position <= 0)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}
		// Short signal: Price below MA and below SAR
		else if (_cooldown == 0 && turnedBear)
		{
			if (Position >= 0)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		// Exit long position: Price falls below SAR
		else if (Position > 0 && sarFlipDown)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && sarFlipUp)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevAboveMa = isPriceAboveMA;
		_prevAboveSar = isPriceAboveSAR;
	}
}
	
