using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi indicator swing strategy.
/// A long needs the close above Parabolic SAR, an up-trending SuperTrend, ADX above AdxThreshold with +DI above -DI, and a volume
/// delta ratio above DeltaThreshold; a short needs all of the mirrored conditions. An opposite signal reverses the position and
/// percent stop loss / take profit levels protect it.
/// </summary>
public class MultiIndicatorSwingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _psarStart;
	private readonly StrategyParam<decimal> _psarIncrement;
	private readonly StrategyParam<decimal> _psarMaximum;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _deltaLength;
	private readonly StrategyParam<int> _deltaSmooth;
	private readonly StrategyParam<decimal> _deltaThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private readonly Queue<decimal> _deltas = new();

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Parabolic SAR start acceleration.
	/// </summary>
	public decimal PsarStart
	{
		get => _psarStart.Value;
		set => _psarStart.Value = value;
	}

	/// <summary>
	/// Parabolic SAR acceleration increment.
	/// </summary>
	public decimal PsarIncrement
	{
		get => _psarIncrement.Value;
		set => _psarIncrement.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration.
	/// </summary>
	public decimal PsarMaximum
	{
		get => _psarMaximum.Value;
		set => _psarMaximum.Value = value;
	}

	/// <summary>
	/// SuperTrend ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// SuperTrend ATR multiplier.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
	}

	/// <summary>
	/// Minimum ADX for a trend.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Candles over which the typical absolute volume delta is averaged.
	/// </summary>
	public int DeltaLength
	{
		get => _deltaLength.Value;
		set => _deltaLength.Value = value;
	}

	/// <summary>
	/// Candles over which the volume delta is smoothed.
	/// </summary>
	public int DeltaSmooth
	{
		get => _deltaSmooth.Value;
		set => _deltaSmooth.Value = value;
	}

	/// <summary>
	/// Minimum normalized volume delta that confirms a direction.
	/// </summary>
	public decimal DeltaThreshold
	{
		get => _deltaThreshold.Value;
		set => _deltaThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MultiIndicatorSwingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(2).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_psarStart = Param(nameof(PsarStart), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("PSAR Start", "Parabolic SAR start acceleration", "PSAR");

		_psarIncrement = Param(nameof(PsarIncrement), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("PSAR Increment", "Parabolic SAR acceleration increment", "PSAR");

		_psarMaximum = Param(nameof(PsarMaximum), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("PSAR Maximum", "Parabolic SAR maximum acceleration", "PSAR");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "SuperTrend ATR period", "SuperTrend");

		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "SuperTrend ATR multiplier", "SuperTrend");

		_adxLength = Param(nameof(AdxLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "ADX period", "ADX");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetNotNegative()
			.SetDisplay("ADX Threshold", "Minimum ADX for a trend", "ADX");

		_deltaLength = Param(nameof(DeltaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Delta Length", "Candles for the typical absolute volume delta", "Volume Delta");

		_deltaSmooth = Param(nameof(DeltaSmooth), 3)
			.SetGreaterThanZero()
			.SetDisplay("Delta Smooth", "Candles for smoothing the volume delta", "Volume Delta");

		_deltaThreshold = Param(nameof(DeltaThreshold), 0.5m)
			.SetNotNegative()
			.SetDisplay("Delta Threshold", "Minimum normalized volume delta", "Volume Delta");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");
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
		_deltas.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_deltas.Clear();

		var psar = new ParabolicSar
		{
			Acceleration = PsarStart,
			AccelerationStep = PsarIncrement,
			AccelerationMax = PsarMaximum,
		};
		var supertrend = new SuperTrend { Length = AtrPeriod, Multiplier = AtrMultiplier };
		var adx = new AverageDirectionalIndex { Length = AdxLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(psar, supertrend, adx, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: TakeProfitPercent > 0 ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit(),
			stopLoss: StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, psar);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, adx);
		}
	}

	private decimal? UpdateDeltaRatio(ICandleMessage candle)
	{
		// Volume delta estimated from where the candle closed inside its range.
		var range = candle.HighPrice - candle.LowPrice;
		var delta = range > 0 ? candle.TotalVolume * (candle.ClosePrice - candle.OpenPrice) / range : 0m;

		_deltas.Enqueue(delta);
		var capacity = Math.Max(DeltaLength, DeltaSmooth);
		while (_deltas.Count > capacity)
			_deltas.Dequeue();

		if (_deltas.Count < capacity)
			return null;

		var smoothed = _deltas.Skip(_deltas.Count - DeltaSmooth).Average();
		var typical = _deltas.Skip(_deltas.Count - DeltaLength).Average(Math.Abs);

		// The smoothed delta is expressed in units of the typical absolute delta.
		return typical > 0 ? smoothed / typical : 0m;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue psarValue, IIndicatorValue supertrendValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var deltaRatio = UpdateDeltaRatio(candle);

		if (!psarValue.IsFormed || !supertrendValue.IsFormed || !adxValue.IsFormed || deltaRatio is not decimal ratio)
			return;

		if (supertrendValue is not SuperTrendIndicatorValue st)
			return;

		if (adxValue is not AverageDirectionalIndexValue adxData
			|| adxData.MovingAverage is not decimal adx || adxData.Dx.Plus is not decimal plusDi || adxData.Dx.Minus is not decimal minusDi)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var sar = psarValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var strongTrend = adx > AdxThreshold;

		var longSignal = close > sar && st.IsUpTrend && strongTrend && plusDi > minusDi && ratio > DeltaThreshold;
		var shortSignal = close < sar && !st.IsUpTrend && strongTrend && minusDi > plusDi && ratio < -DeltaThreshold;

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
