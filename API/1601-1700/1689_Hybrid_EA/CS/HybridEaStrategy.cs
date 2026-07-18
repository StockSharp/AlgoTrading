using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hybrid EA strategy combining EMA crossover with RSI momentum filter.
/// Enters long on EMA cross up + RSI above 50, short on cross down + RSI below 50.
/// </summary>
public class HybridEaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;
	private decimal _entryPrice;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public HybridEaStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for stops", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0; _prevSlow = 0; _hasPrev = false; _entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var atr = new StandardDeviation { Length = AtrPeriod };

		SubscribeCandles(CandleType).Bind(fast, slow, rsi, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev) { _prevFast = fast; _prevSlow = slow; _hasPrev = true; return; }

		var close = candle.ClosePrice;

		// EMA cross up + RSI above 50 => long
		if (_prevFast <= _prevSlow && fast > slow && rsi > 50)
		{
			if (Position < 0) BuyMarket();
			if (Position <= 0)
			{
				BuyMarket();
				_entryPrice = close;
			}
		}
		// EMA cross down + RSI below 50 => short
		else if (_prevFast >= _prevSlow && fast < slow && rsi < 50)
		{
			if (Position > 0) SellMarket();
			if (Position >= 0)
			{
				SellMarket();
				_entryPrice = close;
			}
		}
		// Exit long
		else if (Position > 0)
		{
			if (fast < slow || (atr > 0 && _entryPrice > 0 && close <= _entryPrice - atr * 2))
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		// Exit short
		else if (Position < 0)
		{
			if (fast > slow || (atr > 0 && _entryPrice > 0 && close >= _entryPrice + atr * 2))
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		_prevFast = fast; _prevSlow = slow;
	}
}
