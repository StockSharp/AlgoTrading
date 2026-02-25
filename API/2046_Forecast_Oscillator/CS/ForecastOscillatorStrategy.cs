using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Forecast Oscillator indicator.
/// Uses linear regression forecast with T3 smoothing for signal generation.
/// </summary>
public class ForecastOscillatorStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _t3Period;
	private readonly StrategyParam<decimal> _bFactor;
	private readonly StrategyParam<DataType> _candleType;

	private LinearRegression _linReg;

	private decimal _b2, _b3, _c1, _c2, _c3, _c4, _w1, _w2;
	private decimal _e1, _e2, _e3, _e4, _e5, _e6;
	private decimal? _forecastPrev1, _forecastPrev2;
	private decimal? _sigPrev1, _sigPrev2, _sigPrev3;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public int T3Period { get => _t3Period.Value; set => _t3Period.Value = value; }
	public decimal BFactor { get => _bFactor.Value; set => _bFactor.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ForecastOscillatorStrategy()
	{
		_length = Param(nameof(Length), 15)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Regression length", "Indicators");

		_t3Period = Param(nameof(T3Period), 3)
			.SetGreaterThanZero()
			.SetDisplay("T3 Period", "T3 smoothing period", "Indicators");

		_bFactor = Param(nameof(BFactor), 0.7m)
			.SetDisplay("T3 Factor", "T3 smoothing factor", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_linReg = new LinearRegression { Length = Length };

		// Pre-calculate T3 constants
		var b = BFactor;
		_b2 = b * b;
		_b3 = _b2 * b;
		_c1 = -_b3;
		_c2 = 3m * (_b2 + _b3);
		_c3 = -3m * (2m * _b2 + b + _b3);
		_c4 = 1m + 3m * b + _b3 + 3m * _b2;

		var n = 1m + 0.5m * ((decimal)T3Period - 1m);
		_w1 = 2m / (n + 1m);
		_w2 = 1m - _w1;

		_e1 = _e2 = _e3 = _e4 = _e5 = _e6 = 0;
		_forecastPrev1 = _forecastPrev2 = null;
		_sigPrev1 = _sigPrev2 = _sigPrev3 = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		var price = candle.ClosePrice;
		var lrResult = _linReg.Process(price, candle.OpenTime, true);
		if (!lrResult.IsFormed)
			return;

		var lrValue = (LinearRegressionValue)lrResult;
		if (lrValue.LinearReg is not decimal regValue || regValue == 0)
			return;

		var forecast = (price - regValue) / regValue * 100m;

		// T3 smoothing
		_e1 = _w1 * forecast + _w2 * _e1;
		_e2 = _w1 * _e1 + _w2 * _e2;
		_e3 = _w1 * _e2 + _w2 * _e3;
		_e4 = _w1 * _e3 + _w2 * _e4;
		_e5 = _w1 * _e4 + _w2 * _e5;
		_e6 = _w1 * _e5 + _w2 * _e6;
		var t3 = _c1 * _e6 + _c2 * _e5 + _c3 * _e4 + _c4 * _e3;

		// Cross detection: forecast crosses signal line
		if (_forecastPrev1 != null && _forecastPrev2 != null && _sigPrev1 != null && _sigPrev2 != null && _sigPrev3 != null)
		{
			var buySignal = _forecastPrev1 > _sigPrev2 && _forecastPrev2 <= _sigPrev3 && _sigPrev1 < 0;
			var sellSignal = _forecastPrev1 < _sigPrev2 && _forecastPrev2 >= _sigPrev3 && _sigPrev1 > 0;

			if (buySignal && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			else if (sellSignal && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		// Shift previous values
		_forecastPrev2 = _forecastPrev1;
		_forecastPrev1 = forecast;
		_sigPrev3 = _sigPrev2;
		_sigPrev2 = _sigPrev1;
		_sigPrev1 = t3;
	}
}
