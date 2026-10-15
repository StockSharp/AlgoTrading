using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility Breakout strategy.
/// A close more than Multiplier times the Period ATR above the Period simple moving average goes long and one that far below it goes short,
/// reversing an opposite position. The position stays open until the opposite breakout, or until the stop Multiplier ATR from the entry
/// close is hit, checked on candle closes.
/// </summary>
public class VolatilityBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;

	/// <summary>
	/// Period for SMA and ATR.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the breakout threshold and the stop.
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
	public VolatilityBreakoutStrategy()
	{
		_period = Param(nameof(Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Period for SMA and ATR", "Parameters");

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "ATR multiplier for the breakout threshold and the stop", "Parameters");

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

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, atr);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!smaValue.IsFormed || !atrValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var sma = smaValue.GetValue<decimal>();
		var distance = Multiplier * atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (close > sma + distance && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - distance;
		}
		else if (close < sma - distance && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + distance;
		}
		else if (Position > 0 && close <= _stopPrice)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && close >= _stopPrice)
		{
			BuyMarket(-Position);
		}
	}
}
