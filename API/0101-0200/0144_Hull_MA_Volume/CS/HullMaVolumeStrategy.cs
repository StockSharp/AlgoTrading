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
/// Hull MA Volume strategy.
/// A rising Hull average on a candle whose volume exceeds VolumeMultiplier times the average of the previous VolumePeriod candles goes long,
/// a falling one on such volume goes short, reversing an opposite position. A long closes when the Hull average turns down and a short
/// when it turns up. The stop lies StopLossAtr ATR from the entry close and is checked on candle closes.
/// </summary>
public class HullMaVolumeStrategy : Strategy
{
	private readonly StrategyParam<int> _hullPeriod;
	private readonly StrategyParam<int> _volumePeriod;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _stopLossAtr;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _volumes = [];
	private decimal? _prevHull;
	private decimal _stopPrice;

	/// <summary>
	/// Period of the Hull moving average.
	/// </summary>
	public int HullPeriod
	{
		get => _hullPeriod.Value;
		set => _hullPeriod.Value = value;
	}

	/// <summary>
	/// Previous candles the volume is averaged over.
	/// </summary>
	public int VolumePeriod
	{
		get => _volumePeriod.Value;
		set => _volumePeriod.Value = value;
	}

	/// <summary>
	/// How many times the average volume a candle must exceed.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Stop distance from the entry in ATRs.
	/// </summary>
	public decimal StopLossAtr
	{
		get => _stopLossAtr.Value;
		set => _stopLossAtr.Value = value;
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
	public HullMaVolumeStrategy()
	{
		_hullPeriod = Param(nameof(HullPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Hull Period", "Period of the Hull moving average", "Indicators");

		_volumePeriod = Param(nameof(VolumePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Period", "Previous candles the volume is averaged over", "Indicators");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "How many times the average volume a candle must exceed", "Indicators");

		_stopLossAtr = Param(nameof(StopLossAtr), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss ATR", "Stop distance from the entry in ATRs", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period of the stop ATR", "Risk");

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
		_volumes.Clear();
		_prevHull = null;
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_volumes.Clear();
		_prevHull = null;
		_stopPrice = default;

		var hull = new HullMovingAverage { Length = HullPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(hull, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hull);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue hullValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Volume is compared with the candles before this one.
		var average = _volumes.Count == VolumePeriod ? _volumes.Average() : (decimal?)null;

		_volumes.Add(candle.TotalVolume);

		if (_volumes.Count > VolumePeriod)
			_volumes.RemoveAt(0);

		if (!hullValue.IsFormed || !atrValue.IsFormed)
			return;

		var hull = hullValue.GetValue<decimal>();
		var prevHull = _prevHull;
		_prevHull = hull;

		if (prevHull is not decimal lastHull || average is not decimal avgVolume)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var rising = hull > lastHull;
		var falling = hull < lastHull;
		var surge = candle.TotalVolume > avgVolume * VolumeMultiplier;

		if (rising && surge && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - StopLossAtr * atr;
		}
		else if (falling && surge && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + StopLossAtr * atr;
		}
		else if (Position > 0 && (falling || (StopLossAtr > 0 && close <= _stopPrice)))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (rising || (StopLossAtr > 0 && close >= _stopPrice)))
		{
			BuyMarket(-Position);
		}
	}
}
