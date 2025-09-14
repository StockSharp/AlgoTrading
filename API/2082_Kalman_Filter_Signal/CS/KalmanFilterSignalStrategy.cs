using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Kalman Filter color change signals.
/// Opens long when the filter turns upward and short when it turns downward.
/// </summary>
public class KalmanFilterSignalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _processNoise;
	private readonly StrategyParam<decimal> _measurementNoise;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<SignalMode> _signalMode;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private KalmanFilter _kalman;
	private decimal? _prevFilter;
	private decimal? _prevSignal;

	/// <summary>
	/// Kalman filter process noise coefficient.
	/// </summary>
	public decimal ProcessNoise
	{
		get => _processNoise.Value;
		set => _processNoise.Value = value;
	}

	/// <summary>
	/// Kalman filter measurement noise coefficient.
	/// </summary>
	public decimal MeasurementNoise
	{
		get => _measurementNoise.Value;
		set => _measurementNoise.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Mode used to calculate signals.
	/// </summary>
	public SignalMode Mode
	{
		get => _signalMode.Value;
		set => _signalMode.Value = value;
	}

	/// <summary>
	/// Absolute stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Absolute take profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public KalmanFilterSignalStrategy()
	{
		_processNoise = Param(nameof(ProcessNoise), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Process Noise", "Process noise coefficient", "Kalman Filter");

		_measurementNoise = Param(nameof(MeasurementNoise), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Measurement Noise", "Measurement noise coefficient", "Kalman Filter");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(3).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_signalMode = Param(nameof(Mode), SignalMode.Kalman)
			.SetDisplay("Signal Mode", "Use price vs filter or filter slope", "Signal");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Absolute stop loss", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Absolute take profit", "Risk");
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

		_prevFilter = null;
		_prevSignal = null;
		_kalman = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_kalman = new KalmanFilter
		{
			ProcessNoise = ProcessNoise,
			MeasurementNoise = MeasurementNoise
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_kalman, ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute),
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _kalman);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal filterValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var signal = Mode == SignalMode.Kalman
			? candle.ClosePrice > filterValue ? 1m : 0m
			: (_prevFilter.HasValue && filterValue >= _prevFilter.Value ? 1m : 0m);

		if (_prevSignal.HasValue && signal != _prevSignal.Value)
		{
			if (signal > 0 && Position <= 0)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				BuyMarket(Volume);
			}
			else if (signal == 0 && Position >= 0)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				SellMarket(Volume);
			}
		}

		_prevFilter = filterValue;
		_prevSignal = signal;
	}

	/// <summary>
	/// Signal calculation mode.
	/// </summary>
	public enum SignalMode
	{
		/// <summary>
		/// Use Kalman filter value relative to price.
		/// </summary>
		Kalman,

		/// <summary>
		/// Use filter slope direction.
		/// </summary>
		Trend
	}
}
