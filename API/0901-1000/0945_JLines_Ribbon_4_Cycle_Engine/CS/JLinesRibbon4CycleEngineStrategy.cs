using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// J-Lines Ribbon 4-Cycle Engine strategy.
/// The market is CHOP while ADX is below AdxFloor, LONG when EMA72 is above EMA89 and price closes above EMA126, and SHORT in the mirror case.
/// A long opens on a new LONG cycle or when price dips to EMA72/EMA126 and closes back above it while EMA72 is above EMA89; shorts mirror this,
/// reversing an opposite position. The stop is the last swing low (high) and a position also closes when EMA72 crosses EMA89 against it.
/// </summary>
public class JLinesRibbon4CycleEngineStrategy : Strategy
{
	private enum Cycles
	{
		Chop,
		Long,
		Short,
	}

	private readonly StrategyParam<int> _dmiLength;
	private readonly StrategyParam<decimal> _adxFloor;
	private readonly StrategyParam<int> _swingLength;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _swingHigh;
	private Lowest _swingLow;
	private Cycles _prevCycle;
	private decimal? _prevEma72;
	private decimal? _prevEma89;
	private decimal? _prevSwingHigh;
	private decimal? _prevSwingLow;
	private decimal? _stopPrice;

	/// <summary>
	/// Period of the DMI/ADX.
	/// </summary>
	public int DmiLength
	{
		get => _dmiLength.Value;
		set => _dmiLength.Value = value;
	}

	/// <summary>
	/// ADX level below which the market is CHOP.
	/// </summary>
	public decimal AdxFloor
	{
		get => _adxFloor.Value;
		set => _adxFloor.Value = value;
	}

	/// <summary>
	/// Candles the swing high/low spans.
	/// </summary>
	public int SwingLength
	{
		get => _swingLength.Value;
		set => _swingLength.Value = value;
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
	/// Initialize <see cref="JLinesRibbon4CycleEngineStrategy"/>.
	/// </summary>
	public JLinesRibbon4CycleEngineStrategy()
	{
		_dmiLength = Param(nameof(DmiLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("DMI Length", "Period of the DMI/ADX", "Indicators");

		_adxFloor = Param(nameof(AdxFloor), 12m)
			.SetNotNegative()
			.SetDisplay("ADX Floor", "ADX level below which the market is CHOP", "Indicators");

		_swingLength = Param(nameof(SwingLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Swing Length", "Candles the swing high/low spans", "Risk");

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
		ResetState();
	}

	private void ResetState()
	{
		_prevCycle = Cycles.Chop;
		_prevEma72 = _prevEma89 = null;
		_prevSwingHigh = _prevSwingLow = null;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ema72 = new ExponentialMovingAverage { Length = 72 };
		var ema89 = new ExponentialMovingAverage { Length = 89 };
		var ema126 = new ExponentialMovingAverage { Length = 126 };
		var adx = new AverageDirectionalIndex { Length = DmiLength };
		_swingHigh = new Highest { Length = SwingLength };
		_swingLow = new Lowest { Length = SwingLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema72, ema89, ema126, adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema72);
			DrawIndicator(area, ema89);
			DrawIndicator(area, ema126);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, adx);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ema72Value, IIndicatorValue ema89Value, IIndicatorValue ema126Value, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The swing is measured on the candles before this one.
		var swingHigh = _prevSwingHigh;
		var swingLow = _prevSwingLow;

		var highValue = _swingHigh.Process(new DecimalIndicatorValue(_swingHigh, candle.HighPrice, candle.OpenTime) { IsFinal = true });
		var lowValue = _swingLow.Process(new DecimalIndicatorValue(_swingLow, candle.LowPrice, candle.OpenTime) { IsFinal = true });
		_prevSwingHigh = _swingHigh.IsFormed ? highValue.GetValue<decimal>() : null;
		_prevSwingLow = _swingLow.IsFormed ? lowValue.GetValue<decimal>() : null;

		if (!ema72Value.IsFormed || !ema89Value.IsFormed || !ema126Value.IsFormed || !adxValue.IsFormed
			|| adxValue is not AverageDirectionalIndexValue { MovingAverage: decimal adx })
			return;

		var ema72 = ema72Value.GetValue<decimal>();
		var ema89 = ema89Value.GetValue<decimal>();
		var ema126 = ema126Value.GetValue<decimal>();
		var close = candle.ClosePrice;

		var cycle = adx < AdxFloor
			? Cycles.Chop
			: ema72 > ema89 && close > ema126
				? Cycles.Long
				: ema72 < ema89 && close < ema126
					? Cycles.Short
					: Cycles.Chop;

		var prevCycle = _prevCycle;
		var prevEma72 = _prevEma72;
		var prevEma89 = _prevEma89;

		_prevCycle = cycle;
		_prevEma72 = ema72;
		_prevEma89 = ema89;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossDown = prevEma72 is decimal p72 && prevEma89 is decimal p89 && p72 >= p89 && ema72 < ema89;
		var crossUp = prevEma72 is decimal q72 && prevEma89 is decimal q89 && q72 <= q89 && ema72 > ema89;

		if (Position > 0 && (crossDown || _stopPrice is decimal longStop && candle.LowPrice <= longStop))
		{
			SellMarket(Position);
			_stopPrice = null;
			return;
		}

		if (Position < 0 && (crossUp || _stopPrice is decimal shortStop && candle.HighPrice >= shortStop))
		{
			BuyMarket(-Position);
			_stopPrice = null;
			return;
		}

		var reboundUp = (candle.LowPrice <= ema72 && close > ema72) || (candle.LowPrice <= ema126 && close > ema126);
		var reboundDown = (candle.HighPrice >= ema72 && close < ema72) || (candle.HighPrice >= ema126 && close < ema126);

		var longSignal = (cycle == Cycles.Long && prevCycle != Cycles.Long) || (ema72 > ema89 && reboundUp);
		var shortSignal = (cycle == Cycles.Short && prevCycle != Cycles.Short) || (ema72 < ema89 && reboundDown);

		if (longSignal && Position <= 0 && swingLow is decimal stopLow && stopLow < close)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = stopLow;
		}
		else if (shortSignal && Position >= 0 && swingHigh is decimal stopHigh && stopHigh > close)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = stopHigh;
		}
	}
}
