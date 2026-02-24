using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Karpenko Channel strategy.
/// Generates signals based on dynamic channel and SMA baseline crossover.
/// Long when price is below channel baseline, short when above.
/// </summary>
public class KarpenkoChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _basicMa;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevMa;
	private bool _initialized;

	/// <summary>
	/// Period for base moving average.
	/// </summary>
	public int BasicMa { get => _basicMa.Value; set => _basicMa.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public KarpenkoChannelStrategy()
	{
		_basicMa = Param(nameof(BasicMa), 20)
			.SetGreaterThanZero()
			.SetDisplay("Base MA", "Length of base moving average", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = BasicMa };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_initialized)
		{
			_prevClose = candle.ClosePrice;
			_prevMa = maValue;
			_initialized = true;
			return;
		}

		// Cross above MA -> buy signal
		var crossUp = _prevClose <= _prevMa && candle.ClosePrice > maValue;
		// Cross below MA -> sell signal
		var crossDown = _prevClose >= _prevMa && candle.ClosePrice < maValue;

		if (crossUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (crossDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevClose = candle.ClosePrice;
		_prevMa = maValue;
	}
}
