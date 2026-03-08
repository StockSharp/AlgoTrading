using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Lego V3 strategy using MA crossover with ATR-based stops.
/// </summary>
public class LegoV3Strategy : Strategy
{
	private readonly StrategyParam<int> _fastMa;
	private readonly StrategyParam<int> _slowMa;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;
	private bool _hasPrev;

	public int FastMa { get => _fastMa.Value; set => _fastMa.Value = value; }
	public int SlowMa { get => _slowMa.Value; set => _slowMa.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LegoV3Strategy()
	{
		_fastMa = Param(nameof(FastMa), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast EMA period", "Indicators");

		_slowMa = Param(nameof(SlowMa), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow EMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastMa };
		var slow = new ExponentialMovingAverage { Length = SlowMa };
		var atr = new StandardDeviation { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atr <= 0) return;

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		var close = candle.ClosePrice;

		// ATR stop check
		if (Position > 0 && _entryPrice > 0 && close < _entryPrice - 2 * atr)
		{
			SellMarket();
			_entryPrice = 0;
		}
		else if (Position < 0 && _entryPrice > 0 && close > _entryPrice + 2 * atr)
		{
			BuyMarket();
			_entryPrice = 0;
		}

		// MA crossover
		if (_prevFast <= _prevSlow && fast > slow && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_entryPrice = close;
		}
		else if (_prevFast >= _prevSlow && fast < slow && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_entryPrice = close;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
