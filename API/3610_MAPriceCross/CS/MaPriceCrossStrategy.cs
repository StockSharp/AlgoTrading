using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "MA Price Cross" MetaTrader expert.
/// Enters when SMA crosses above/below the current close price.
/// </summary>
public class MaPriceCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;

	private SimpleMovingAverage _sma;
	private decimal? _prevAverage;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public MaPriceCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for MA cross detection", "General");

		_maPeriod = Param(nameof(MaPeriod), 160)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "SMA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma = new SimpleMovingAverage { Length = MaPeriod };
		_prevAverage = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma.IsFormed)
		{
			_prevAverage = smaValue;
			return;
		}

		if (_prevAverage is null)
		{
			_prevAverage = smaValue;
			return;
		}

		var close = candle.ClosePrice;
		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// MA was below price, now crosses above -> sell signal (price goes under MA)
		var sellSignal = _prevAverage.Value < close && smaValue > close;
		// MA was above price, now crosses below -> buy signal (price goes above MA)
		var buySignal = _prevAverage.Value > close && smaValue < close;

		if (buySignal)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (sellSignal)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);
		}

		_prevAverage = smaValue;
	}
}
