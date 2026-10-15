using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic slope mean reversion.
/// The slope of the smoothed %K is compared with its average: buys when the slope is far below the average
/// and starts turning up, sells when it is far above and starts turning down.
/// Exits when the slope returns to its average.
/// </summary>
public class StochasticSlopeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<int> _slopeLookback;
	private readonly StrategyParam<decimal> _thresholdMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _kSmoothing;
	private SimpleMovingAverage _slopeAverage;
	private StandardDeviation _slopeStdDev;
	private decimal? _prevK;
	private decimal? _prevSlope;

	/// <summary>
	/// Stochastic lookback period.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period of %K.
	/// </summary>
	public int StochKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	/// <summary>
	/// Period of %D.
	/// </summary>
	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period for slope statistics.
	/// </summary>
	public int SlopeLookback
	{
		get => _slopeLookback.Value;
		set => _slopeLookback.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for extreme slope.
	/// </summary>
	public decimal ThresholdMultiplier
	{
		get => _thresholdMultiplier.Value;
		set => _thresholdMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	/// Initialize <see cref="StochasticSlopeMeanReversionStrategy"/>.
	/// </summary>
	public StochasticSlopeMeanReversionStrategy()
	{
		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Stochastic lookback period", "Stochastic");

		_stochKPeriod = Param(nameof(StochKPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stoch %K Period", "Smoothing period of %K", "Stochastic");

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stoch %D Period", "Period of %D", "Stochastic");

		_slopeLookback = Param(nameof(SlopeLookback), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slope Lookback", "Period for slope statistics", "Strategy");

		_thresholdMultiplier = Param(nameof(ThresholdMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Threshold Multiplier", "Standard deviation multiplier for extreme slope", "Strategy");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management");

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
		_kSmoothing = null;
		_slopeAverage = null;
		_slopeStdDev = null;
		_prevK = null;
		_prevSlope = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stochastic = new StochasticOscillator();
		stochastic.K.Length = StochPeriod;
		stochastic.D.Length = StochDPeriod;

		_kSmoothing = new SimpleMovingAverage { Length = StochKPeriod };
		_slopeAverage = new SimpleMovingAverage { Length = SlopeLookback };
		_slopeStdDev = new StandardDeviation { Length = SlopeLookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: null,
			stopLoss: StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : null
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var stochArea = CreateChartArea();
			if (stochArea != null)
				DrawIndicator(stochArea, stochastic);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal rawK)
			return;

		var k = _kSmoothing.Process(rawK, candle.ServerTime, true).ToDecimal();
		if (!_kSmoothing.IsFormed)
			return;

		if (_prevK is not decimal prevK)
		{
			_prevK = k;
			return;
		}

		_prevK = k;

		var slope = k - prevK;
		var avgSlope = _slopeAverage.Process(slope, candle.ServerTime, true).ToDecimal();
		var stdSlope = _slopeStdDev.Process(slope, candle.ServerTime, true).ToDecimal();

		var prevSlope = _prevSlope;
		_prevSlope = slope;

		if (!_slopeAverage.IsFormed || !_slopeStdDev.IsFormed || prevSlope is not decimal prev)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Extreme reading that has started to turn back toward the average.
		if (slope < avgSlope - ThresholdMultiplier * stdSlope && slope > prev && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (slope > avgSlope + ThresholdMultiplier * stdSlope && slope < prev && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0 && slope >= avgSlope)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && slope <= avgSlope)
		{
			BuyMarket(-Position);
		}
	}
}
