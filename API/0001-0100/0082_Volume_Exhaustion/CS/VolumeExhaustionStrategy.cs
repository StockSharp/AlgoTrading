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
/// Volume Exhaustion strategy.
/// A volume spike is a candle whose volume exceeds VolumeMultiplier times the average of the previous VolumePeriod candles.
/// While flat, a bullish spike against a falling moving average buys and a bearish spike against a rising one sells.
/// The only exit is a stop that trails the close by AtrMultiplier ATRs.
/// </summary>
public class VolumeExhaustionStrategy : Strategy
{
	/// <summary>
	/// Period of the ATR that sizes the stop.
	/// </summary>
	public const int AtrPeriod = 14;

	private readonly StrategyParam<int> _volumePeriod;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _volumes = [];
	private decimal? _prevMa;
	private decimal _stopPrice;

	/// <summary>
	/// Number of previous candles the volume is averaged over.
	/// </summary>
	public int VolumePeriod
	{
		get => _volumePeriod.Value;
		set => _volumePeriod.Value = value;
	}

	/// <summary>
	/// How many times the average volume a spike must exceed.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Period of the moving average that defines the trend.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Distance of the trailing stop in ATR multiples.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	public VolumeExhaustionStrategy()
	{
		_volumePeriod = Param(nameof(VolumePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Period", "Previous candles the volume is averaged over", "Indicators");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "How many times the average volume a spike must exceed", "Indicators");

		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average that defines the trend", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Distance of the trailing stop in ATR multiples", "Risk");

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
		_prevMa = null;
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_volumes.Clear();
		_prevMa = null;
		_stopPrice = default;

		var sma = new SimpleMovingAverage { Length = MAPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The spike is measured against the candles before this one.
		var average = _volumes.Count == VolumePeriod ? _volumes.Average() : (decimal?)null;

		_volumes.Add(candle.TotalVolume);

		if (_volumes.Count > VolumePeriod)
			_volumes.RemoveAt(0);

		if (!smaValue.IsFormed || !atrValue.IsFormed)
			return;

		var ma = smaValue.GetValue<decimal>();
		var prevMa = _prevMa;
		_prevMa = ma;

		if (average is not decimal avgVolume || prevMa is not decimal lastMa || !IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var distance = AtrMultiplier * atrValue.GetValue<decimal>();

		if (Position > 0)
		{
			if (close <= _stopPrice)
				SellMarket(Position);
			else
				_stopPrice = Math.Max(_stopPrice, close - distance);

			return;
		}

		if (Position < 0)
		{
			if (close >= _stopPrice)
				BuyMarket(-Position);
			else
				_stopPrice = Math.Min(_stopPrice, close + distance);

			return;
		}

		var spike = candle.TotalVolume > avgVolume * VolumeMultiplier;

		if (!spike)
			return;

		if (close > candle.OpenPrice && ma < lastMa)
		{
			BuyMarket(Volume);
			_stopPrice = close - distance;
		}
		else if (close < candle.OpenPrice && ma > lastMa)
		{
			SellMarket(Volume);
			_stopPrice = close + distance;
		}
	}
}
