using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Fractal Adaptive Simple Moving Average (FRASMAv2).
/// Computes FRAMA from fractal dimension, trades on color (slope direction) changes.
/// </summary>
public class FrasmaV2Strategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private bool _isFirst;
	private decimal _prevFrama;
	private int _prevColor;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FrasmaV2Strategy()
	{
		_period = Param(nameof(Period), 30)
			.SetGreaterThanZero()
			.SetDisplay("Period", "FRAMA calculation period", "Indicator");

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

		_isFirst = true;
		_prevFrama = 0;
		_prevColor = 1;

		var fdi = new FractalDimension { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fdi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fdiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fdiValue.IsFinal)
			return;

		var fdi = fdiValue.GetValue<decimal>();
		var alpha = (decimal)Math.Exp(-4.6 * ((double)fdi - 1.0));
		alpha = Math.Max(0.01m, Math.Min(1m, alpha));

		var price = candle.ClosePrice;
		var frama = _isFirst ? price : alpha * price + (1 - alpha) * _prevFrama;

		int color;
		if (_isFirst)
		{
			color = 1;
			_isFirst = false;
		}
		else if (frama > _prevFrama)
			color = 0; // Uptrend
		else if (frama < _prevFrama)
			color = 2; // Downtrend
		else
			color = 1; // Flat

		// Uptrend ended (color was 0, now > 0) -> sell signal
		if (_prevColor == 0 && color > 0 && Position >= 0)
			SellMarket();
		// Downtrend ended (color was 2, now < 2) -> buy signal
		else if (_prevColor == 2 && color < 2 && Position <= 0)
			BuyMarket();

		_prevFrama = frama;
		_prevColor = color;
	}
}
