using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that enters on EMA crossover and moves stop-loss to break-even
/// when price moves favorably by a specified ATR multiple.
/// </summary>
public class StopLossMoverStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _stopAtrMult;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _isStopMoved;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal StopAtrMult { get => _stopAtrMult.Value; set => _stopAtrMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public StopLossMoverStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowLength = Param(nameof(SlowLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_stopAtrMult = Param(nameof(StopAtrMult), 1.5m)
			.SetDisplay("Stop ATR Mult", "ATR multiplier for initial stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0;
		_stopPrice = 0;
		_isStopMoved = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };
		var atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrVal <= 0)
			return;

		var close = candle.ClosePrice;

		// Check stop-loss hit
		if (Position > 0 && _stopPrice > 0 && close <= _stopPrice)
		{
			SellMarket();
			_entryPrice = 0;
			_stopPrice = 0;
			_isStopMoved = false;
			return;
		}
		else if (Position < 0 && _stopPrice > 0 && close >= _stopPrice)
		{
			BuyMarket();
			_entryPrice = 0;
			_stopPrice = 0;
			_isStopMoved = false;
			return;
		}

		// Move stop to break-even when price moves favorably by 2*ATR
		if (Position > 0 && !_isStopMoved && _entryPrice > 0)
		{
			if (close >= _entryPrice + 2 * atrVal)
			{
				_stopPrice = _entryPrice;
				_isStopMoved = true;
			}
		}
		else if (Position < 0 && !_isStopMoved && _entryPrice > 0)
		{
			if (close <= _entryPrice - 2 * atrVal)
			{
				_stopPrice = _entryPrice;
				_isStopMoved = true;
			}
		}

		// Entry signals: EMA crossover
		if (Position == 0)
		{
			if (fast > slow)
			{
				BuyMarket();
				_entryPrice = close;
				_stopPrice = close - StopAtrMult * atrVal;
				_isStopMoved = false;
			}
			else if (fast < slow)
			{
				SellMarket();
				_entryPrice = close;
				_stopPrice = close + StopAtrMult * atrVal;
				_isStopMoved = false;
			}
		}
		// Reverse on crossover
		else if (Position > 0 && fast < slow)
		{
			SellMarket();
			SellMarket();
			_entryPrice = close;
			_stopPrice = close + StopAtrMult * atrVal;
			_isStopMoved = false;
		}
		else if (Position < 0 && fast > slow)
		{
			BuyMarket();
			BuyMarket();
			_entryPrice = close;
			_stopPrice = close - StopAtrMult * atrVal;
			_isStopMoved = false;
		}
	}
}
