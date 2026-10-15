using System;
using System.Collections.Generic;

using Ecng.Common;

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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe of processed candles", "General");

		_firstLength = Param(nameof(FirstLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Price MA Length", "Length of the first smoothing", "Indicator")
			
			.SetOptimize(5, 30, 1);

		_secondLength = Param(nameof(SecondLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("Signal MA Length", "Length of the second smoothing", "Indicator")
			
			.SetOptimize(3, 20, 1);
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

		_priceMa = null;
		_signalMa = null;
		_prevValue = null;
		_prevColor = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_priceMa = new SimpleMovingAverage { Length = FirstLength };
		_signalMa = new SimpleMovingAverage { Length = SecondLength };
		_prevValue = null;
		_prevColor = null;

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

		var priceMa = _priceMa.Process(new DecimalIndicatorValue(_priceMa, price, candle.OpenTime) { IsFinal = true }).ToDecimal();

		var diff = (candle.HighPrice + candle.LowPrice - 2m * priceMa) / 2m;

		var signal = _signalMa.Process(new DecimalIndicatorValue(_signalMa, diff, candle.OpenTime) { IsFinal = true }).ToDecimal();

		if (!_priceMa.IsFormed || !_signalMa.IsFormed || !IsFormedAndOnlineAndAllowTrading())
		{
			_prevColor = _prevValue is null ? null : _prevValue < signal ? 0 : _prevValue > signal ? 2 : 1;
			_prevValue = signal;
			return;
		}

		var color = signal > 0 ? 0 : signal < 0 ? 2 : 1;
		var threshold = (Security?.PriceStep ?? 1m) * 10m;

		if (_prevValue is decimal prevSignal)
		{
			if (prevSignal <= -threshold && signal > threshold && Position <= 0)
				BuyMarket();
			else if (prevSignal >= threshold && signal < -threshold && Position >= 0)
				SellMarket();
		}

		_prevColor = color;
		_prevValue = signal;
	}
}
