using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bear Bulls Power strategy.
/// Uses smoothed difference between median price and moving average.
/// Opens long when indicator turns upward above zero, short when turns downward below zero.
/// </summary>
public class BearBullsPowerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _firstLength;
	private readonly StrategyParam<int> _secondLength;

	private SimpleMovingAverage _priceMa;
	private SimpleMovingAverage _signalMa;

	private decimal? _prevValue;
	private int? _prevColor;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for price smoothing.
	/// </summary>
	public int FirstLength
	{
		get => _firstLength.Value;
		set => _firstLength.Value = value;
	}

	/// <summary>
	/// Period for signal smoothing.
	/// </summary>
	public int SecondLength
	{
		get => _secondLength.Value;
		set => _secondLength.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BearBullsPowerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe of processed candles", "General");

		_firstLength = Param(nameof(FirstLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Price MA Length", "Length of the first smoothing", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_secondLength = Param(nameof(SecondLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Signal MA Length", "Length of the second smoothing", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceMa = new SimpleMovingAverage { Length = FirstLength };
		_signalMa = new SimpleMovingAverage { Length = SecondLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = (candle.HighPrice + candle.LowPrice) / 2m;

		var priceMa = _priceMa.Process(price, candle.OpenTime, true).ToDecimal();

		var diff = (candle.HighPrice + candle.LowPrice - 2m * priceMa) / 2m;

		var signal = _signalMa.Process(diff, candle.OpenTime, true).ToDecimal();

		int color;
		if (_prevValue is null)
			color = 1;
		else if (_prevValue < signal)
			color = 0;
		else if (_prevValue > signal)
			color = 2;
		else
			color = 1;

		if (_prevColor != color)
		{
			if (color == 0 && signal > 0 && Position <= 0)
				BuyMarket();
			else if (color == 2 && signal < 0 && Position >= 0)
				SellMarket();
		}

		_prevColor = color;
		_prevValue = signal;
	}
}
