using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hull MA ADX strategy.
/// The Hull average turns up when it rises after falling and turns down when it falls after rising. While ADX is above AdxThreshold, a turn up
/// goes long and a turn down goes short, reversing an opposite position. A long closes when the Hull average falls, a short when it rises,
/// and either closes once ADX drops below AdxExitThreshold. The stop lies AtrMultiplier ATR from the entry close and is checked on candle closes.
/// </summary>
public class HullMaAdxStrategy : Strategy
{
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _adxExitThreshold;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevHull;
	private decimal? _prevPrevHull;
	private decimal _stopPrice;

	/// <summary>
	/// Period of the Hull moving average.
	/// </summary>
	public int HmaPeriod
	{
		get => _hmaPeriod.Value;
		set => _hmaPeriod.Value = value;
	}

	/// <summary>
	/// Period of ADX.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX level required to enter.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// ADX level below which the position closes.
	/// </summary>
	public decimal AdxExitThreshold
	{
		get => _adxExitThreshold.Value;
		set => _adxExitThreshold.Value = value;
	}

	/// <summary>
	/// Stop distance from the entry in ATRs.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Period of the stop ATR.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
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
	public HullMaAdxStrategy()
	{
		_hmaPeriod = Param(nameof(HmaPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("HMA Period", "Period of the Hull moving average", "Indicators");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period of ADX", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "ADX level required to enter", "Indicators");

		_adxExitThreshold = Param(nameof(AdxExitThreshold), 20m)
			.SetDisplay("ADX Exit Threshold", "ADX level below which the position closes", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "Stop distance from the entry in ATRs", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period of the stop ATR", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_prevHull = null;
		_prevPrevHull = null;
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHull = null;
		_prevPrevHull = null;
		_stopPrice = default;

		var hull = new HullMovingAverage { Length = HmaPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(hull, adx, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hull);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, adx);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue hullValue, IIndicatorValue adxValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!hullValue.IsFormed || !adxValue.IsFormed || !atrValue.IsFormed)
			return;

		if (adxValue is not AverageDirectionalIndexValue { MovingAverage: decimal strength })
			return;

		var hull = hullValue.GetValue<decimal>();
		var prevHull = _prevHull;
		var prevPrevHull = _prevPrevHull;
		_prevPrevHull = prevHull;
		_prevHull = hull;

		if (prevHull is not decimal last || prevPrevHull is not decimal beforeLast)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var rising = hull > last;
		var falling = hull < last;
		var turnsUp = rising && last < beforeLast;
		var turnsDown = falling && last > beforeLast;
		var strong = strength > AdxThreshold;
		var weak = strength < AdxExitThreshold;

		if (turnsUp && strong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - AtrMultiplier * atr;
		}
		else if (turnsDown && strong && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + AtrMultiplier * atr;
		}
		else if (Position > 0 && (falling || weak || (AtrMultiplier > 0 && close <= _stopPrice)))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (rising || weak || (AtrMultiplier > 0 && close >= _stopPrice)))
		{
			BuyMarket(-Position);
		}
	}
}
