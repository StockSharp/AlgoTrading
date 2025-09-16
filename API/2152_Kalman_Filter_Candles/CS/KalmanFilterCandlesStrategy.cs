namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on Kalman filtered candle colors.
/// </summary>
public class KalmanFilterCandlesStrategy : Strategy
{
	private readonly StrategyParam<decimal> _processNoise;
	private readonly StrategyParam<DataType> _candleType;

	private KalmanFilter _openFilter;
	private KalmanFilter _closeFilter;

	private int _prevColor;
	private bool _hasPrev;

	/// <summary>
	/// Kalman filter process noise coefficient.
	/// </summary>
	public decimal ProcessNoise
	{
		get => _processNoise.Value;
		set => _processNoise.Value = value;
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
	/// Initializes a new instance of the <see cref="KalmanFilterCandlesStrategy"/> class.
	/// </summary>
	public KalmanFilterCandlesStrategy()
	{
		_processNoise = Param(nameof(ProcessNoise), 1m)
			.SetDisplay("Process Noise", "Kalman filter smoothing factor", "Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "Common");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_openFilter = null;
		_closeFilter = null;
		_prevColor = 1;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_openFilter = new KalmanFilter
		{
			ProcessNoise = ProcessNoise,
			MeasurementNoise = ProcessNoise
		};

		_closeFilter = new KalmanFilter
		{
			ProcessNoise = ProcessNoise,
			MeasurementNoise = ProcessNoise
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_closeFilter, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _closeFilter);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal closeValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var openRes = _openFilter.Process(new DecimalIndicatorValue(_openFilter, candle.OpenPrice));
		if (!openRes.IsFinal || openRes is not DecimalIndicatorValue openVal)
			return;

		var color = openVal.Value < closeValue ? 2 : openVal.Value > closeValue ? 0 : 1;

		if (_hasPrev)
		{
			if (color == 2 && _prevColor != 2)
			{
				if (Position < 0)
					BuyMarket(Volume + Math.Abs(Position));
				else if (Position == 0)
					BuyMarket();
			}
			else if (color == 0 && _prevColor != 0)
			{
				if (Position > 0)
					SellMarket(Volume + Math.Abs(Position));
				else if (Position == 0)
					SellMarket();
			}
		}

		_prevColor = color;
		_hasPrev = true;
	}
}
