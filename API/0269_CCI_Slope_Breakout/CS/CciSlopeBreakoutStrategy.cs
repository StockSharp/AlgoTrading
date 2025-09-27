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
/// CCI Slope Breakout Strategy
/// </summary>
public class CciSlopeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _slopePeriod;
	private readonly StrategyParam<decimal> _breakoutMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private CommodityChannelIndex _cci;
	private LinearRegression _cciSlope;
	private decimal _prevCciSlopeValue;
	private decimal _slopeAvg;
	private decimal _slopeStdDev;
	private decimal _sumSlope;
	private decimal _sumSlopeSquared;
	private readonly Queue<decimal> _slopeValues = [];

	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	public int SlopePeriod
	{
		get => _slopePeriod.Value;
		set => _slopePeriod.Value = value;
	}

	public decimal BreakoutMultiplier
	{
		get => _breakoutMultiplier.Value;
		set => _breakoutMultiplier.Value = value;
	}

	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	public CciSlopeBreakoutStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for CCI calculation", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);
			
		_slopePeriod = Param(nameof(SlopePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slope Period", "Period for slope average and standard deviation", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);
			
		_breakoutMultiplier = Param(nameof(BreakoutMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Breakout Multiplier", "Standard deviation multiplier for breakout", "Signal")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 3.0m, 0.5m);
			
		_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 3.0m, 0.5m);
			
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
		_prevCciSlopeValue = 0;
		_slopeAvg = 0;
		_slopeStdDev = 0;
		_sumSlope = 0;
		_sumSlopeSquared = 0;
		_slopeValues.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		// Initialize indicators
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_cciSlope = new LinearRegression { Length = 2 }; // For calculating slope
		
		
		// Create subscription and bind indicator
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cci, ProcessCandle)
			.Start();
		
		// Setup chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
		
		// Enable position protection
		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		// Calculate CCI slope
		var currentSlopeTyped = (LinearRegressionValue)_cciSlope.Process(cciValue, candle.ServerTime, candle.State == CandleStates.Finished);

		if (currentSlopeTyped.LinearReg is not decimal currentSlopeValue)
			return;

		// Update slope stats when we have 2 values to calculate slope
		if (_prevCciSlopeValue != 0)
		{
			// Calculate simple slope from current and previous values
			decimal slope = currentSlopeValue - _prevCciSlopeValue;
			
			// Update running statistics
			_slopeValues.Enqueue(slope);
			_sumSlope += slope;
			_sumSlopeSquared += slope * slope;
			
			// Remove oldest value if we have enough
			if (_slopeValues.Count > SlopePeriod)
			{
				var oldSlope = _slopeValues.Dequeue();
				_sumSlope -= oldSlope;
				_sumSlopeSquared -= oldSlope * oldSlope;
			}
			
			// Calculate average and standard deviation
			_slopeAvg = _sumSlope / _slopeValues.Count;
			decimal variance = (_sumSlopeSquared / _slopeValues.Count) - (_slopeAvg * _slopeAvg);
			_slopeStdDev = variance <= 0 ? 0 : (decimal)Math.Sqrt((double)variance);
			
			// Generate signals if we have enough data for statistics
			if (_slopeValues.Count >= SlopePeriod)
			{
				// Breakout logic
				if (slope > _slopeAvg + BreakoutMultiplier * _slopeStdDev && Position <= 0)
				{
					// Long position on bullish slope breakout
					BuyMarket(Volume + Math.Abs(Position));
					LogInfo($"Long entry: CCI slope breakout above {_slopeAvg + BreakoutMultiplier * _slopeStdDev:F2}");
				}
				else if (slope < _slopeAvg - BreakoutMultiplier * _slopeStdDev && Position >= 0)
				{
					// Short position on bearish slope breakout
					SellMarket(Volume + Math.Abs(Position));
					LogInfo($"Short entry: CCI slope breakout below {_slopeAvg - BreakoutMultiplier * _slopeStdDev:F2}");
				}
				
				// Exit logic - Return to mean
				if (Position > 0 && slope < _slopeAvg)
				{
					SellMarket(Math.Abs(Position));
					LogInfo("Long exit: CCI slope returned to mean");
				}
				else if (Position < 0 && slope > _slopeAvg)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo("Short exit: CCI slope returned to mean");
				}
			}
		}
		
		// Update previous value for next iteration
		_prevCciSlopeValue = currentSlopeValue;
	}
}
