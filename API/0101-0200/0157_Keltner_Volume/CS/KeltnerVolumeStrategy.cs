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
/// Keltner Volume strategy.
/// The Keltner Channel is the EmaPeriod EMA plus and minus Multiplier times the AtrPeriod ATR. A close below the lower band on volume above
/// the average of the previous VolumeAvgPeriod candles goes long and a close above the upper band on such volume goes short, reversing
/// an opposite position. The position closes when price crosses the EMA. The stop lies StopLossAtr ATR from the entry close
/// and is checked on candle closes.
/// </summary>
public class KeltnerVolumeStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<decimal> _stopLossAtr;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _volumes = [];
	private decimal _stopPrice;

	/// <summary>
	/// Period of the channel EMA.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the channel and stop ATR.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the channel width.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Previous candles the volume is averaged over.
	/// </summary>
	public int VolumeAvgPeriod
	{
		get => _volumeAvgPeriod.Value;
		set => _volumeAvgPeriod.Value = value;
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
	public KeltnerVolumeStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period of the channel EMA", "Keltner");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period of the channel and stop ATR", "Keltner");

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "ATR multiplier of the channel width", "Keltner");

		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Average Period", "Previous candles the volume is averaged over", "Volume");

		_stopLossAtr = Param(nameof(StopLossAtr), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss ATR", "Stop distance from the entry in ATRs", "Risk");

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
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_volumes.Clear();
		_stopPrice = default;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Volume is compared with the candles before this one.
		var average = _volumes.Count == VolumeAvgPeriod ? _volumes.Average() : (decimal?)null;

		_volumes.Add(candle.TotalVolume);

		if (_volumes.Count > VolumeAvgPeriod)
			_volumes.RemoveAt(0);

		if (!emaValue.IsFormed || !atrValue.IsFormed || average is not decimal avgVolume)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var middle = emaValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var upper = middle + Multiplier * atr;
		var lower = middle - Multiplier * atr;
		var close = candle.ClosePrice;
		var surge = candle.TotalVolume > avgVolume;

		if (close < lower && surge && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - StopLossAtr * atr;
		}
		else if (close > upper && surge && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + StopLossAtr * atr;
		}
		else if (Position > 0 && (close > middle || (StopLossAtr > 0 && close <= _stopPrice)))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (close < middle || (StopLossAtr > 0 && close >= _stopPrice)))
		{
			BuyMarket(-Position);
		}
	}
}
