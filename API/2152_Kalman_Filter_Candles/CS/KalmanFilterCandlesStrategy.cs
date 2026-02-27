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

	public decimal ProcessNoise { get => _processNoise.Value; set => _processNoise.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public KalmanFilterCandlesStrategy()
	{
		_processNoise = Param(nameof(ProcessNoise), 1m)
			.SetDisplay("Process Noise", "Kalman filter smoothing factor", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "Common");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevColor = 1;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_openFilter = new KalmanFilter { ProcessNoise = ProcessNoise, MeasurementNoise = ProcessNoise };
		_closeFilter = new KalmanFilter { ProcessNoise = ProcessNoise, MeasurementNoise = ProcessNoise };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openInput = new DecimalIndicatorValue(_openFilter, candle.OpenPrice, candle.OpenTime) { IsFinal = true };
		var closeInput = new DecimalIndicatorValue(_closeFilter, candle.ClosePrice, candle.OpenTime) { IsFinal = true };

		var openRes = _openFilter.Process(openInput);
		var closeRes = _closeFilter.Process(closeInput);

		if (!_openFilter.IsFormed || !_closeFilter.IsFormed)
			return;

		var openVal = openRes.ToDecimal();
		var closeVal = closeRes.ToDecimal();

		var color = openVal < closeVal ? 2 : openVal > closeVal ? 0 : 1;

		if (_hasPrev)
		{
			if (color == 2 && _prevColor != 2)
			{
				if (Position < 0)
					BuyMarket();
				if (Position <= 0)
					BuyMarket();
			}
			else if (color == 0 && _prevColor != 0)
			{
				if (Position > 0)
					SellMarket();
				if (Position >= 0)
					SellMarket();
			}
		}

		_prevColor = color;
		_hasPrev = true;
	}
}
