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
	private readonly StrategyParam<bool> _buyOpen, _sellOpen, _buyClose, _sellClose;
	private Order _pending;

	private decimal _b2, _b3, _c1, _c2, _c3, _c4, _w1, _w2;
	private decimal _e1, _e2, _e3, _e4, _e5, _e6;
	private decimal? _forecastPrev1, _forecastPrev2;
	private decimal? _sigPrev1, _sigPrev2, _sigPrev3;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public int T3Period { get => _t3Period.Value; set => _t3Period.Value = value; }
	public decimal BFactor { get => _bFactor.Value; set => _bFactor.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }
	public ForecastOscillatorStrategy()
	{
		_buyOpen = Param(nameof(BuyOpen), true);
		_sellOpen = Param(nameof(SellOpen), true);
		_buyClose = Param(nameof(BuyClose), true);
		_sellClose = Param(nameof(SellClose), true);
		_length = Param(nameof(Length), 15)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Regression length", "Indicators");

		_t3Period = Param(nameof(T3Period), 3)
			.SetGreaterThanZero()
			.SetDisplay("T3 Period", "T3 smoothing period", "Indicators");

		_bFactor = Param(nameof(BFactor), 0.7m)
			.SetDisplay("T3 Factor", "T3 smoothing factor", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_b2 = default; _b3 = default; _c1 = default; _c2 = default; _c3 = default; _c4 = default;
		_w1 = default; _w2 = default;
		_e1 = default; _e2 = default; _e3 = default; _e4 = default; _e5 = default; _e6 = default;
		_forecastPrev1 = default; _forecastPrev2 = default;
		_sigPrev1 = default; _sigPrev2 = default; _sigPrev3 = default;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_pending = null;

		var linReg = new LinearRegression { Length = Length };
		_linReg = linReg;

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

		Indicators.Add(linReg);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
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

		// Independent reference: current finished oscillator crosses the current T3,
		// having been on the other side at the previous finished candle.
		var buySignal = _forecastPrev1 is decimal prevFc && _sigPrev1 is decimal prevT3
			&& prevFc <= prevT3 && forecast > t3 && t3 < 0m;
		var sellSignal = _forecastPrev1 is decimal prevFc2 && _sigPrev1 is decimal prevT32
			&& prevFc2 >= prevT32 && forecast < t3 && t3 > 0m;
		if (IsFormedAndOnlineAndAllowTrading() && (_pending is null || _pending.State is OrderStates.Done or OrderStates.Failed))
		{
			if (buySignal)
			{
				if (Position < 0m && SellClose) _pending = BuyMarket(-Position + (BuyOpen ? Volume : 0m));
				else if (Position == 0m && BuyOpen) _pending = BuyMarket(Volume);
			}
			else if (sellSignal)
			{
				if (Position > 0m && BuyClose) _pending = SellMarket(Position + (SellOpen ? Volume : 0m));
				else if (Position == 0m && SellOpen) _pending = SellMarket(Volume);
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
