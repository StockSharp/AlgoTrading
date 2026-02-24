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
/// XAUUSD 10-minute strategy using RSI, dual EMA crossover, and Bollinger bands.
/// Combines momentum (RSI), trend (EMA cross), and volatility (StdDev bands) signals.
/// </summary>
public class Xauusd10MinuteStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _fastEma;
	private readonly StrategyParam<int> _slowEma;
	private readonly StrategyParam<decimal> _stopMult;
	private readonly StrategyParam<decimal> _tpMult;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFastEma;
	private decimal _prevSlowEma;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public int FastEma { get => _fastEma.Value; set => _fastEma.Value = value; }
	public int SlowEma { get => _slowEma.Value; set => _slowEma.Value = value; }
	public decimal StopMult { get => _stopMult.Value; set => _stopMult.Value = value; }
	public decimal TpMult { get => _tpMult.Value; set => _tpMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Xauusd10MinuteStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 65m)
			.SetDisplay("RSI Overbought", "Overbought level", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 35m)
			.SetDisplay("RSI Oversold", "Oversold level", "Indicators");

		_fastEma = Param(nameof(FastEma), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowEma = Param(nameof(SlowEma), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_stopMult = Param(nameof(StopMult), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Mult", "StdDev mult for stop", "Risk");

		_tpMult = Param(nameof(TpMult), 5m)
			.SetGreaterThanZero()
			.SetDisplay("TP Mult", "StdDev mult for TP", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFastEma = 0;
		_prevSlowEma = 0;
		_entryPrice = 0;
		_stopPrice = 0;
		_takePrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var fastEma = new ExponentialMovingAverage { Length = FastEma };
		var slowEma = new ExponentialMovingAverage { Length = SlowEma };
		var stdDev = new StandardDeviation { Length = 14 };

		_prevFastEma = 0;
		_prevSlowEma = 0;
		_entryPrice = 0;
		_stopPrice = 0;
		_takePrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, fastEma, slowEma, stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal fastVal, decimal slowVal, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// TP/SL management
		if (Position > 0 && _entryPrice > 0)
		{
			if (candle.ClosePrice <= _stopPrice || candle.ClosePrice >= _takePrice)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (candle.ClosePrice >= _stopPrice || candle.ClosePrice <= _takePrice)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		if (_prevFastEma == 0 || _prevSlowEma == 0 || stdVal <= 0)
		{
			_prevFastEma = fastVal;
			_prevSlowEma = slowVal;
			return;
		}

		var emaCrossUp = _prevFastEma <= _prevSlowEma && fastVal > slowVal;
		var emaCrossDown = _prevFastEma >= _prevSlowEma && fastVal < slowVal;
		var buySignal = emaCrossUp || rsiVal < RsiOversold;
		var sellSignal = emaCrossDown || rsiVal > RsiOverbought;

		if (buySignal && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice - StopMult * stdVal;
			_takePrice = _entryPrice + TpMult * stdVal;
		}
		else if (sellSignal && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice + StopMult * stdVal;
			_takePrice = _entryPrice - TpMult * stdVal;
		}

		_prevFastEma = fastVal;
		_prevSlowEma = slowVal;
	}
}
