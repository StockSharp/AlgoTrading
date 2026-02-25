using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on MESA Adaptive Moving Average (MAMA/FAMA) crossing.
/// Buys when MAMA crosses above FAMA and sells on opposite crossing.
/// </summary>
public class ExpMamaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _fastLimit;
	private readonly StrategyParam<decimal> _slowLimit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevMama;
	private decimal? _prevFama;

	// MAMA calculator state
	private decimal _p1, _p2, _p3;
	private decimal _s1, _s2, _s3;
	private decimal _d1, _d2, _d3;
	private decimal _q1v, _q2v, _q3v;
	private decimal _i1v, _i2v, _i3v;
	private decimal _i21, _q21;
	private decimal _re1, _im1;
	private decimal _phase1;
	private decimal _period;
	private decimal? _mamaVal;
	private decimal? _famaVal;
	private int _count;

	public decimal FastLimit { get => _fastLimit.Value; set => _fastLimit.Value = value; }
	public decimal SlowLimit { get => _slowLimit.Value; set => _slowLimit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExpMamaStrategy()
	{
		_fastLimit = Param(nameof(FastLimit), 0.5m)
			.SetDisplay("Fast Limit", "Fast alpha limit", "Indicators");

		_slowLimit = Param(nameof(SlowLimit), 0.05m)
			.SetDisplay("Slow Limit", "Slow alpha limit", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
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
		_prevMama = null;
		_prevFama = null;
		_p1 = _p2 = _p3 = 0;
		_s1 = _s2 = _s3 = 0;
		_d1 = _d2 = _d3 = 0;
		_q1v = _q2v = _q3v = 0;
		_i1v = _i2v = _i3v = 0;
		_i21 = _q21 = 0;
		_re1 = _im1 = 0;
		_phase1 = 0;
		_period = 0;
		_mamaVal = null;
		_famaVal = null;
		_count = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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
		var result = CalcMama(price, FastLimit, SlowLimit);
		if (result is null)
			return;

		var (mama, fama) = result.Value;

		if (_prevMama.HasValue && _prevFama.HasValue)
		{
			var wasAbove = _prevMama > _prevFama;
			var isAbove = mama > fama;

			// MAMA crosses below FAMA -> Buy signal
			if (wasAbove && !isAbove && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			// MAMA crosses above FAMA -> Sell signal
			else if (!wasAbove && isAbove && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevMama = mama;
		_prevFama = fama;
	}

	private (decimal mama, decimal fama)? CalcMama(decimal price, decimal fast, decimal slow)
	{
		_count++;

		var c0 = 0.0962m;
		var c1 = 0.5769m;
		var c2 = -0.5769m;
		var c3 = -0.0962m;

		var smooth = (4m * price + 3m * _p1 + 2m * _p2 + _p3) / 10m;
		var detrender = c0 * smooth + c1 * _s1 + c2 * _s2 + c3 * _s3;

		var q1 = c0 * detrender + c1 * _d1 + c2 * _d2 + c3 * _d3;
		var i1 = _d1;

		var jI = c0 * i1 + c1 * _i1v + c2 * _i2v + c3 * _i3v;
		var jQ = c0 * q1 + c1 * _q1v + c2 * _q2v + c3 * _q3v;

		var i2 = i1 - jQ;
		var q2 = q1 + jI;

		i2 = 0.2m * i2 + 0.8m * _i21;
		q2 = 0.2m * q2 + 0.8m * _q21;

		var re = i2 * _i21 + q2 * _q21;
		var im = i2 * _q21 - q2 * _i21;

		re = 0.2m * re + 0.8m * _re1;
		im = 0.2m * im + 0.8m * _im1;

		var period = _period;
		if (re != 0m && im != 0m)
		{
			var ang = (decimal)Math.Atan((double)(im / re));
			if (ang != 0m)
				period = 2m * (decimal)Math.PI / ang;
		}
		period = Math.Min(Math.Max(period, 6m), 50m);

		var phase = 0m;
		if (i1 != 0m)
			phase = (decimal)Math.Atan((double)(q1 / i1));
		var delta = phase - _phase1;
		if (delta < 1m)
			delta = 1m;
		if (delta > 1.5m)
			delta = 1.5m;

		var alpha = fast / delta;
		if (alpha < slow)
			alpha = slow;

		var mama = _mamaVal is null ? price : alpha * price + (1m - alpha) * _mamaVal.Value;
		var fama = _famaVal is null ? price : 0.5m * alpha * mama + (1m - 0.5m * alpha) * _famaVal.Value;

		_p3 = _p2; _p2 = _p1; _p1 = price;
		_s3 = _s2; _s2 = _s1; _s1 = smooth;
		_d3 = _d2; _d2 = _d1; _d1 = detrender;
		_q3v = _q2v; _q2v = _q1v; _q1v = q1;
		_i3v = _i2v; _i2v = _i1v; _i1v = i1;
		_i21 = i2; _q21 = q2;
		_re1 = re; _im1 = im;
		_phase1 = phase;
		_period = period;
		_mamaVal = mama;
		_famaVal = fama;

		if (_count < 7)
			return null;

		return (mama, fama);
	}
}
