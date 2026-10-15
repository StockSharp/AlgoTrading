using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hull Suite No SL/TP strategy.
/// Computes the Hull-type average selected by Mode on the close: HMA = WMA(2*WMA(n/2) - WMA(n), sqrt(n)), EHMA is the same with
/// EMAs, and THMA = WMA(3*WMA(m/3) - WMA(m/2) - WMA(m), m) with m = Length / 2. The strategy goes long when the average is above
/// its value two bars ago and short when it is below, reversing an opposite position.
/// </summary>
public class HullSuiteNoSlTpStrategy : Strategy
{
	/// <summary>
	/// Hull average variants.
	/// </summary>
	public enum HullModes
	{
		/// <summary>
		/// Hull moving average.
		/// </summary>
		Hma,

		/// <summary>
		/// Exponential Hull moving average.
		/// </summary>
		Ehma,

		/// <summary>
		/// Triple Hull moving average.
		/// </summary>
		Thma,
	}

	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<HullModes> _mode;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _first;
	private IIndicator _second;
	private IIndicator _third;
	private IIndicator _smooth;
	private decimal? _prev1;
	private decimal? _prev2;

	/// <summary>
	/// Hull average length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Hull average variant.
	/// </summary>
	public HullModes Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
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
	public HullSuiteNoSlTpStrategy()
	{
		_length = Param(nameof(Length), 55)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Hull average length", "Indicators");

		_mode = Param(nameof(Mode), HullModes.Hma)
			.SetDisplay("Mode", "Hull average variant", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_prev1 = null;
		_prev2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prev1 = null;
		_prev2 = null;

		var length = Length;

		switch (Mode)
		{
			case HullModes.Ehma:
				_first = new ExponentialMovingAverage { Length = Math.Max(1, length / 2) };
				_second = new ExponentialMovingAverage { Length = length };
				_third = null;
				_smooth = new ExponentialMovingAverage { Length = Math.Max(1, (int)Math.Round(Math.Sqrt(length))) };
				break;

			case HullModes.Thma:
			{
				var half = Math.Max(1, length / 2);
				_first = new WeightedMovingAverage { Length = Math.Max(1, half / 3) };
				_second = new WeightedMovingAverage { Length = Math.Max(1, half / 2) };
				_third = new WeightedMovingAverage { Length = half };
				_smooth = new WeightedMovingAverage { Length = half };
				break;
			}

			default:
				_first = new WeightedMovingAverage { Length = Math.Max(1, length / 2) };
				_second = new WeightedMovingAverage { Length = length };
				_third = null;
				_smooth = new WeightedMovingAverage { Length = Math.Max(1, (int)Math.Round(Math.Sqrt(length))) };
				break;
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _smooth);
			DrawOwnTrades(area);
		}
	}

	private static decimal ProcessValue(IIndicator indicator, decimal input, DateTime time)
	{
		return indicator.Process(new DecimalIndicatorValue(indicator, input, time) { IsFinal = true }).GetValue<decimal>();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var time = candle.OpenTime;

		var first = ProcessValue(_first, close, time);
		var second = ProcessValue(_second, close, time);
		var third = _third != null ? ProcessValue(_third, close, time) : 0m;

		if (!_first.IsFormed || !_second.IsFormed || (_third != null && !_third.IsFormed))
			return;

		var raw = _third != null
			? 3m * first - second - third
			: 2m * first - second;

		var ma = ProcessValue(_smooth, raw, time);
		if (!_smooth.IsFormed)
			return;

		var maTwoBarsAgo = _prev2;
		_prev2 = _prev1;
		_prev1 = ma;

		if (maTwoBarsAgo is not decimal older)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (ma > older && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (ma < older && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
