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
/// FrakTrak XonaX strategy.
/// Uses fractal breakouts to generate entry signals.
/// </summary>
public class FraktrakXonaxStrategy : Strategy
{
	private readonly StrategyParam<decimal> _fractalOffset;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _h1, _h2, _h3, _h4, _h5;
	private decimal _l1, _l2, _l3, _l4, _l5;
	private decimal? _upFractal;
	private decimal? _downFractal;
	private decimal? _lastUpFractal;
	private decimal? _lastDownFractal;

	/// <summary>
	/// Price offset added beyond fractal for entry trigger.
	/// </summary>
	public decimal FractalOffset
	{
		get => _fractalOffset.Value;
		set => _fractalOffset.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public FraktrakXonaxStrategy()
	{
		_fractalOffset = Param(nameof(FractalOffset), 50m)
			.SetDisplay("Fractal Offset", "Price offset beyond fractal for entry", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Source candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_h1 = _h2 = _h3 = _h4 = _h5 = 0m;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0m;
		_upFractal = _downFractal = _lastUpFractal = _lastDownFractal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

		// Shift high and low buffers
		_h1 = _h2; _h2 = _h3; _h3 = _h4; _h4 = _h5; _h5 = candle.HighPrice;
		_l1 = _l2; _l2 = _l3; _l3 = _l4; _l4 = _l5; _l5 = candle.LowPrice;

		// Need at least 5 bars for fractal detection
		if (_h1 == 0 || _l1 == 0)
			return;

		// Detect new fractals (bar 3 is the middle of 5 bars)
		if (_h3 > _h1 && _h3 > _h2 && _h3 > _h4 && _h3 > _h5)
			_upFractal = _h3;
		if (_l3 < _l1 && _l3 < _l2 && _l3 < _l4 && _l3 < _l5)
			_downFractal = _l3;

		// Buy signal: close above up fractal + offset
		if (_upFractal is decimal up && _lastUpFractal != up)
		{
			var trigger = up + FractalOffset;
			if (candle.ClosePrice > trigger && Position <= 0)
			{
				BuyMarket();
				_lastUpFractal = up;
			}
		}

		// Sell signal: close below down fractal - offset
		if (_downFractal is decimal low && _lastDownFractal != low)
		{
			var trigger = low - FractalOffset;
			if (candle.ClosePrice < trigger && Position >= 0)
			{
				SellMarket();
				_lastDownFractal = low;
			}
		}
	}
}
