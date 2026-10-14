using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Kalman Filter Trend strategy.
/// A close above the Kalman filter estimate goes long and a close below it goes short, reversing an opposite position, so the position flips
/// whenever price crosses the line. The stop lies AtrMultiplier times the AtrPeriod ATR from the entry close and is checked on candle closes.
/// </summary>
public class KalmanFilterTrendStrategy : Strategy
{
	private readonly StrategyParam<decimal> _processNoise;
	private readonly StrategyParam<decimal> _measurementNoise;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;

	/// <summary>
	/// Process noise of the Kalman filter.
	/// </summary>
	public decimal ProcessNoise
	{
		get => _processNoise.Value;
		set => _processNoise.Value = value;
	}

	/// <summary>
	/// Measurement noise of the Kalman filter.
	/// </summary>
	public decimal MeasurementNoise
	{
		get => _measurementNoise.Value;
		set => _measurementNoise.Value = value;
	}

	/// <summary>
	/// Period of the stop ATR.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Stop distance from the entry in ATRs.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	/// Constructor.
	/// </summary>
	public KalmanFilterTrendStrategy()
	{
		_processNoise = Param(nameof(ProcessNoise), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Process Noise", "Process noise of the Kalman filter", "Kalman Filter");

		_measurementNoise = Param(nameof(MeasurementNoise), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Measurement Noise", "Measurement noise of the Kalman filter", "Kalman Filter");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period of the stop ATR", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "Stop distance from the entry in ATRs", "Risk");

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
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stopPrice = default;

		var kalman = new KalmanFilter
		{
			ProcessNoise = ProcessNoise,
			MeasurementNoise = MeasurementNoise,
		};
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(kalman, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, kalman);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, atr);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue kalmanValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!kalmanValue.IsFormed || !atrValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var line = kalmanValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (close > line && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - AtrMultiplier * atr;
		}
		else if (close < line && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + AtrMultiplier * atr;
		}
		else if (Position > 0 && AtrMultiplier > 0 && close <= _stopPrice)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && AtrMultiplier > 0 && close >= _stopPrice)
		{
			BuyMarket(-Position);
		}
	}
}
