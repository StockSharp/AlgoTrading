using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD Slope Breakout Strategy
/// </summary>
public class MacdSlopeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEma;
	private readonly StrategyParam<int> _slowEma;
	private readonly StrategyParam<int> _signalMa;
	private readonly StrategyParam<int> _slopePeriod;
	private readonly StrategyParam<decimal> _breakoutMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private MovingAverageConvergenceDivergenceSignal _macd;
	private LinearRegression _macdHistSlope;
	private decimal _prevSlopeValue;
	private decimal _slopeAvg;
	private decimal _slopeStdDev;
	private decimal _sumSlope;
	private decimal _sumSlopeSquared;
	private readonly Queue<decimal> _slopeValues = [];

	public int FastEma
	{
		get => _fastEma.Value;
		set => _fastEma.Value = value;
	}

	public int SlowEma
	{
		get => _slowEma.Value;
		set => _slowEma.Value = value;
	}

	public int SignalMa
	{
		get => _signalMa.Value;
		set => _signalMa.Value = value;
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
	
	public MacdSlopeBreakoutStrategy()
	{
		_fastEma = Param(nameof(FastEma), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(8, 16, 2);
			
		_slowEma = Param(nameof(SlowEma), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(20, 30, 2);
			
		_signalMa = Param(nameof(SignalMa), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal MA", "Signal MA period", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(7, 12, 1);
		
		_slopePeriod = Param(nameof(SlopePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slope Period", "Period for slope average and standard deviation", "Signal")
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
		_prevSlopeValue = 0;
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
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastEma },
				LongMa = { Length = SlowEma },
			},
			SignalMa = { Length = SignalMa }
		};
		_macdHistSlope = new LinearRegression { Length = 2 }; // For calculating slope
		
		
		// Create subscription and bind indicator
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();
		
		// Setup chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
		
		// Enable position protection
		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		
		if (macdTyped.Macd is not decimal macd ||
			macdTyped.Signal is not decimal signal)
			return;

		// Calculate MACD histogram value (MACD - Signal)
		decimal macdHist = macd - signal;
		
		// Calculate MACD histogram slope
		var currentSlopeTyped = (LinearRegressionValue)_macdHistSlope.Process(macdHist, candle.ServerTime, candle.State == CandleStates.Finished);

		if (currentSlopeTyped.LinearReg is not decimal currentSlopeValue)
			return;

		// Update slope stats when we have 2 values to calculate slope
		if (_prevSlopeValue != 0)
		{
			// Calculate simple slope from current and previous values
			decimal slope = currentSlopeValue - _prevSlopeValue;
			
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
					// Long position on bullish histogram slope breakout
					BuyMarket(Volume + Math.Abs(Position));
					LogInfo($"Long entry: MACD histogram slope breakout above {_slopeAvg + BreakoutMultiplier * _slopeStdDev:F5}");
				}
				else if (slope < _slopeAvg - BreakoutMultiplier * _slopeStdDev && Position >= 0)
				{
					// Short position on bearish histogram slope breakout
					SellMarket(Volume + Math.Abs(Position));
					LogInfo($"Short entry: MACD histogram slope breakout below {_slopeAvg - BreakoutMultiplier * _slopeStdDev:F5}");
				}
				
				// Exit logic - Return to mean
				if (Position > 0 && slope < _slopeAvg)
				{
					SellMarket(Math.Abs(Position));
					LogInfo("Long exit: MACD histogram slope returned to mean");
				}
				else if (Position < 0 && slope > _slopeAvg)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo("Short exit: MACD histogram slope returned to mean");
				}
			}
		}
		
		// Update previous value for next iteration
		_prevSlopeValue = currentSlopeValue;
	}
}
