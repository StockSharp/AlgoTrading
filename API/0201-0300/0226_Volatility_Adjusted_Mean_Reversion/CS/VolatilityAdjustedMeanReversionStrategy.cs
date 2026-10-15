using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility Adjusted Mean Reversion strategy.
/// The threshold is Multiplier times the ATR divided by the ATR to standard deviation ratio, all over Period candles. A close more than the
/// threshold below the Period simple moving average goes long and one that far above it goes short, reversing an opposite position.
/// A long closes once the close is back at or above the average and a short once it is back at or below it. The stop lies Multiplier ATR
/// from the entry close and is checked on candle closes.
/// </summary>
public class VolatilityAdjustedMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;

	/// <summary>
	/// Period of the SMA, ATR and standard deviation.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Multiplier of the threshold and the stop.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
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
	public VolatilityAdjustedMeanReversionStrategy()
	{
		_period = Param(nameof(Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Period of the SMA, ATR and standard deviation", "Parameters");

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Multiplier of the threshold and the stop", "Parameters");

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
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stopPrice = default;

		var sma = new SimpleMovingAverage { Length = Period };
		var atr = new AverageTrueRange { Length = Period };
		var stdev = new StandardDeviation { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, atr, stdev, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, atr);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue, IIndicatorValue atrValue, IIndicatorValue stdevValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!smaValue.IsFormed || !atrValue.IsFormed || !stdevValue.IsFormed)
			return;

		var atr = atrValue.GetValue<decimal>();
		var deviation = stdevValue.GetValue<decimal>();

		if (atr <= 0 || deviation <= 0)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var sma = smaValue.GetValue<decimal>();
		var threshold = Multiplier * atr / (atr / deviation);
		var close = candle.ClosePrice;

		if (close < sma - threshold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - Multiplier * atr;
		}
		else if (close > sma + threshold && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + Multiplier * atr;
		}
		else if (Position > 0 && (close >= sma || close <= _stopPrice))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (close <= sma || close >= _stopPrice))
		{
			BuyMarket(-Position);
		}
	}
}
