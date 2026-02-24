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
/// Smart Ass Trade strategy using MACD histogram direction and moving average trend filter.
/// </summary>
public class SmartAssTradeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMacd;
	private decimal _prevSignal;
	private decimal _prevMa;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SmartAssTradeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Base timeframe", "General");
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

		var macd = new MovingAverageConvergenceDivergenceSignal();
		var sma = new SimpleMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var mv = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (mv.Macd is not decimal macd || mv.Signal is not decimal signal)
			return;

		if (!smaValue.IsFinal)
			return;

		var ma = smaValue.GetValue<decimal>();
		var hist = macd - signal;

		if (_prevMa == 0)
		{
			_prevMacd = macd;
			_prevSignal = signal;
			_prevMa = ma;
			return;
		}

		var prevHist = _prevMacd - _prevSignal;
		var osmaUp = hist > prevHist;
		var maUp = ma > _prevMa;

		if (osmaUp && maUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (!osmaUp && !maUp && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevMacd = macd;
		_prevSignal = signal;
		_prevMa = ma;
	}
}
