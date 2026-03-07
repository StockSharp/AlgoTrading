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
/// Bollinger Bands with Kalman Filter Strategy.
/// Enters positions when price is at Bollinger extremes and confirmed by Kalman Filter trend direction.
/// </summary>
public class BollingerKalmanFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _kalmanQ; // Process noise
	private readonly StrategyParam<decimal> _kalmanR; // Measurement noise
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _signalCooldownBars;
	private static readonly object _sync = new();
	private decimal _upperBand;
	private decimal _lowerBand;
	private decimal _midBand;
	private decimal _kalmanValue;
	private decimal? _previousKalmanValue;
	private int _cooldownRemaining;

	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Kalman Filter process noise.
	/// </summary>
	public decimal KalmanQ
	{
		get => _kalmanQ.Value;
		set => _kalmanQ.Value = value;
	}

	/// <summary>
	/// Kalman Filter measurement noise.
	/// </summary>
	public decimal KalmanR
	{
		get => _kalmanR.Value;
		set => _kalmanR.Value = value;
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
	/// Closed candles to wait before taking the next signal.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>
	/// Initialize strategy.
	/// </summary>
	public BollingerKalmanFilterStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Length of the Bollinger Bands", "Bollinger Settings")
			
			.SetOptimize(10, 30, 5);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Bollinger Settings")
			
			.SetOptimize(1.5m, 2.5m, 0.5m);

		_kalmanQ = Param(nameof(KalmanQ), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Kalman Q", "Process noise for Kalman Filter", "Kalman Filter Settings")
			
			.SetOptimize(0.001m, 0.1m, 0.01m);

		_kalmanR = Param(nameof(KalmanR), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Kalman R", "Measurement noise for Kalman Filter", "Kalman Filter Settings")
			
			.SetOptimize(0.01m, 1.0m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(2).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 3)
			.SetNotNegative()
			.SetDisplay("Signal Cooldown", "Closed candles to wait before the next entry", "General");
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

		_upperBand = 0;
		_lowerBand = 0;
		_midBand = 0;
		_kalmanValue = 0;
		_previousKalmanValue = null;
		_cooldownRemaining = 0;
	}


	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerDeviation
		};

		var kalmanFilter = new KalmanFilter
		{
			ProcessNoise = KalmanQ,
			MeasurementNoise = KalmanR
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(candle => ProcessCandle(candle, bollinger, kalmanFilter))
			.Start();

		// Start position protection
		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, kalmanFilter);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, BollingerBands bollinger, KalmanFilter kalmanFilter)
	{
		if (candle.State != CandleStates.Finished)
			return;

		lock (_sync)
		{
			var bollingerValue = bollinger.Process(new CandleIndicatorValue(bollinger, candle) { IsFinal = true });
			var kalmanValue = kalmanFilter.Process(new DecimalIndicatorValue(kalmanFilter, candle.ClosePrice, candle.OpenTime) { IsFinal = true });
			if (!bollingerValue.IsFinal || !kalmanValue.IsFinal || !bollinger.IsFormed || !kalmanFilter.IsFormed)
				return;

			if (bollingerValue is not BollingerBandsValue bands ||
				bands.UpBand is not decimal upperBand ||
				bands.LowBand is not decimal lowerBand ||
				bands.MovingAverage is not decimal midBand)
			{
				return;
			}

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			if (_cooldownRemaining > 0)
				_cooldownRemaining--;

			var kalmanFilterValue = kalmanValue.ToDecimal();
			var kalmanTrendUp = _previousKalmanValue is decimal previous && kalmanFilterValue > previous;
			var kalmanTrendDown = _previousKalmanValue is decimal prior && kalmanFilterValue < prior;

			_upperBand = upperBand;
			_lowerBand = lowerBand;
			_midBand = midBand;
			_kalmanValue = kalmanFilterValue;

			if (_cooldownRemaining == 0 && candle.LowPrice <= lowerBand && kalmanTrendUp && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_cooldownRemaining = SignalCooldownBars;
			}
			else if (_cooldownRemaining == 0 && candle.HighPrice >= upperBand && kalmanTrendDown && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_cooldownRemaining = SignalCooldownBars;
			}
			else if (Position > 0 && candle.ClosePrice >= midBand)
			{
				SellMarket(Position);
			}
			else if (Position < 0 && candle.ClosePrice <= midBand)
			{
				BuyMarket(-Position);
			}

			_previousKalmanValue = kalmanFilterValue;
		}
	}
}
