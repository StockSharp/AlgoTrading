using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VWAP Mean Reversion strategy.
/// The market trades around the clock, so the session VWAP restarts with each UTC day and weighs each candle's typical price by its volume.
/// A close more than K times the AtrPeriod ATR below VWAP goes long and one that far above it goes short, reversing an opposite position.
/// A long closes once the close is back at or above VWAP and a short once it is back at or below it. The stop lies K ATR from the entry
/// close and is checked on candle closes.
/// </summary>
public class VwapMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _k;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _day;
	private decimal _cumulativePriceVolume;
	private decimal _cumulativeVolume;
	private decimal _stopPrice;

	/// <summary>
	/// ATR multiplier for the entry distance and the stop.
	/// </summary>
	public decimal K
	{
		get => _k.Value;
		set => _k.Value = value;
	}

	/// <summary>
	/// Period of the ATR.
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
	public VwapMeanReversionStrategy()
	{
		_k = Param(nameof(K), 2m)
			.SetGreaterThanZero()
			.SetDisplay("K", "ATR multiplier for the entry distance and the stop", "Parameters");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period of the ATR", "Parameters");

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
		_day = null;
		_cumulativePriceVolume = 0;
		_cumulativeVolume = 0;
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_day = null;
		_cumulativePriceVolume = 0;
		_cumulativeVolume = 0;
		_stopPrice = default;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, atr);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;

		if (_day != day)
		{
			_day = day;
			_cumulativePriceVolume = 0;
			_cumulativeVolume = 0;
		}

		var typicalPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3;
		_cumulativePriceVolume += typicalPrice * candle.TotalVolume;
		_cumulativeVolume += candle.TotalVolume;

		if (!atrValue.IsFormed || _cumulativeVolume <= 0)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var vwap = _cumulativePriceVolume / _cumulativeVolume;
		var distance = K * atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (close < vwap - distance && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - distance;
		}
		else if (close > vwap + distance && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + distance;
		}
		else if (Position > 0 && (close >= vwap || close <= _stopPrice))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (close <= vwap || close >= _stopPrice))
		{
			BuyMarket(-Position);
		}
	}
}
