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
/// when price moves favorably by a specified StdDev multiple.
/// </summary>
public class StopLossMoverStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _stopMult;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _isStopMoved;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal StopMult { get => _stopMult.Value; set => _stopMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public StopLossMoverStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowLength = Param(nameof(SlowLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_stopMult = Param(nameof(StopMult), 1.5m)
			.SetDisplay("Stop Mult", "StdDev multiplier for initial stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_prevFast = 0;
		_prevSlow = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };
		var stdDev = new StandardDeviation { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, stdDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stdVal <= 0)
			return;

		var close = candle.ClosePrice;

		// Check stop-loss hit
		if (Position > 0 && _stopPrice > 0 && close <= _stopPrice)
		{
			SellMarket();
			_entryPrice = 0;
			_stopPrice = 0;
			_isStopMoved = false;
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}
		else if (Position < 0 && _stopPrice > 0 && close >= _stopPrice)
		{
			BuyMarket();
			_entryPrice = 0;
			_stopPrice = 0;
			_isStopMoved = false;
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		// Move stop to break-even when price moves favorably by 2*stdDev
		if (Position > 0 && !_isStopMoved && _entryPrice > 0)
		{
			if (close >= _entryPrice + 2 * stdVal)
			{
				_stopPrice = _entryPrice;
				_isStopMoved = true;
			}
		}
		else if (Position < 0 && !_isStopMoved && _entryPrice > 0)
		{
			if (close <= _entryPrice - 2 * stdVal)
			{
				_stopPrice = _entryPrice;
				_isStopMoved = true;
			}
		}

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		// Entry signals: EMA crossover
		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		if (crossUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_entryPrice = close;
			_stopPrice = close - StopMult * stdVal;
			_isStopMoved = false;
		}
		else if (crossDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_entryPrice = close;
			_stopPrice = close + StopMult * stdVal;
			_isStopMoved = false;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
