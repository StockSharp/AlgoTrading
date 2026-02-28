namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Stochastic Slope Mean Reversion Strategy - based on mean reversion of Stochastic %K slope.
/// </summary>
public class StochasticSlopeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<int> _slopeLookback;
	private readonly StrategyParam<decimal> _thresholdMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousStochK;
	private decimal _slopeSum;
	private int _slopeCount;
	private decimal _sumSquaredDiff;

	public int StochKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	public int SlopeLookback
	{
		get => _slopeLookback.Value;
		set => _slopeLookback.Value = value;
	}

	public decimal ThresholdMultiplier
	{
		get => _thresholdMultiplier.Value;
		set => _thresholdMultiplier.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public StochasticSlopeMeanReversionStrategy()
	{
		_stochKPeriod = Param(nameof(StochKPeriod), 3)
			.SetDisplay("Stoch %K Period", "Stochastic %K smoothing period", "Stochastic");

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetDisplay("Stoch %D Period", "Stochastic %D smoothing period", "Stochastic");

		_slopeLookback = Param(nameof(SlopeLookback), 20)
			.SetDisplay("Slope Lookback", "Period for slope statistics", "Slope");

		_thresholdMultiplier = Param(nameof(ThresholdMultiplier), 1.5m)
			.SetDisplay("Threshold Multiplier", "Std dev multiplier for entry", "Slope");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousStochK = 0;
		_slopeSum = 0;
		_slopeCount = 0;
		_sumSquaredDiff = 0;

		var stochastic = new StochasticOscillator
		{
			K = { Length = StochKPeriod },
			D = { Length = StochDPeriod },
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stochTyped = (StochasticOscillatorValue)stochValue;

		if (stochTyped.K is not decimal stochK)
			return;

		if (_previousStochK == 0)
		{
			_previousStochK = stochK;
			return;
		}

		var slope = stochK - _previousStochK;
		_previousStochK = stochK;

		_slopeCount++;
		_slopeSum += slope;

		var avgSlope = _slopeSum / _slopeCount;
		_sumSquaredDiff += (slope - avgSlope) * (slope - avgSlope);

		if (_slopeCount >= SlopeLookback)
		{
			var stdDev = (decimal)Math.Sqrt((double)(_sumSquaredDiff / _slopeCount));

			// Limit running count
			if (_slopeCount > SlopeLookback)
			{
				_slopeCount = SlopeLookback;
				_slopeSum = avgSlope * SlopeLookback;
				_sumSquaredDiff = stdDev * stdDev * SlopeLookback;
			}

			var lowerThreshold = avgSlope - ThresholdMultiplier * stdDev;
			var upperThreshold = avgSlope + ThresholdMultiplier * stdDev;

			if (slope < lowerThreshold && Position <= 0)
			{
				BuyMarket();
			}
			else if (slope > upperThreshold && Position >= 0)
			{
				SellMarket();
			}
			else if (slope > avgSlope && Position > 0)
			{
				SellMarket();
			}
			else if (slope < avgSlope && Position < 0)
			{
				BuyMarket();
			}
		}
	}
}
