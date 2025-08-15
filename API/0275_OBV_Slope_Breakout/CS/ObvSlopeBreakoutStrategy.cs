namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// OBV Slope Breakout Strategy.
/// Strategy enters when OBV slope breaks out of its average range.
/// </summary>
public class ObvSlopeBreakoutStrategy : Strategy
{
	private OnBalanceVolume _obv;
	private LinearRegression _obvSlope;
	private SimpleMovingAverage _obvSlopeAvg;
	private StandardDeviation _obvSlopeStdDev;
	private decimal _lastObvValue;
	private decimal _lastObvSlope;
	private decimal _lastSlopeAvg;
	private decimal _lastSlopeStdDev;

	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _slopeLength;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period for calculating average and standard deviation of OBV slope.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Period for calculating slope using linear regression.
	/// </summary>
	public int SlopeLength
	{
		get => _slopeLength.Value;
		set => _slopeLength.Value = value;
	}

	/// <summary>
	/// Multiplier for standard deviation to determine breakout threshold.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss as a percentage of entry price.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Type of candles to use in the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ObvSlopeBreakoutStrategy"/>.
	/// </summary>
	public ObvSlopeBreakoutStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for calculating average and standard deviation of OBV slope", "Strategy Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_slopeLength = Param(nameof(SlopeLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slope Length", "Period for calculating slope using linear regression", "Strategy Parameters")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Std Dev Multiplier", "Multiplier for standard deviation to determine breakout threshold", "Strategy Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_stopLoss = Param(nameof(StopLoss), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop-loss as a percentage of entry price", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use in the strategy", "General");
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
		_lastObvSlope = default;
		_lastObvValue = default;
		_lastSlopeAvg = default;
		_lastSlopeStdDev = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);


		// Initialize indicators
		_obv = new OnBalanceVolume();
		_obvSlope = new LinearRegression { Length = SlopeLength };
		_obvSlopeAvg = new SimpleMovingAverage { Length = LookbackPeriod };
		_obvSlopeStdDev = new StandardDeviation { Length = LookbackPeriod };

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_obv, ProcessObv)
			.Start();

		// Set up position protection
		StartProtection(new(), new Unit(StopLoss, UnitTypes.Percent));

		// Create chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessObv(ICandleMessage candle, decimal obvValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate OBV slope
		var slopeTyped = (LinearRegressionValue)_obvSlope.Process(obvValue, candle.ServerTime, candle.State == CandleStates.Finished);
		if (!slopeTyped.IsFinal)
			return;

		if (slopeTyped.LinearReg is not decimal slopeValue)
			return;

		_lastObvSlope = slopeValue;

		// Calculate slope average and standard deviation
		var avgValue = _obvSlopeAvg.Process(slopeValue, candle.ServerTime, candle.State == CandleStates.Finished);
		var stdDevValue = _obvSlopeStdDev.Process(slopeValue, candle.ServerTime, candle.State == CandleStates.Finished);
		
		// Store values for decision making
		_lastObvValue = obvValue;
		
		if (avgValue.IsFinal && stdDevValue.IsFinal)
		{
			_lastSlopeAvg = avgValue.ToDecimal();
			_lastSlopeStdDev = stdDevValue.ToDecimal();
			
			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Calculate breakout thresholds
			var upperThreshold = _lastSlopeAvg + Multiplier * _lastSlopeStdDev;
			var lowerThreshold = _lastSlopeAvg - Multiplier * _lastSlopeStdDev;
			
			// Trading logic
			if (_lastObvSlope > upperThreshold && Position <= 0)
			{
				// OBV slope breaks out upward - Go Long
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (_lastObvSlope < lowerThreshold && Position >= 0)
			{
				// OBV slope breaks out downward - Go Short
				SellMarket(Volume + Math.Abs(Position));
			}
		}
	}
}
