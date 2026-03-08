using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA trend following with ATR trailing stop management.
/// Enters on EMA crossover, exits via trailing stop based on ATR.
/// </summary>
public class ManagerTrailingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _trailMult;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;
	private decimal _trailStop;
	private decimal _highSinceLong;
	private decimal _lowSinceShort;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal TrailMult { get => _trailMult.Value; set => _trailMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ManagerTrailingStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for trailing", "Indicators");
		_trailMult = Param(nameof(TrailMult), 2.0m)
			.SetDisplay("Trail Mult", "ATR multiplier for trailing stop", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0; _prevSlow = 0; _hasPrev = false;
		_trailStop = 0; _highSinceLong = 0; _lowSinceShort = decimal.MaxValue;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };
		var atr = new StandardDeviation { Length = AtrPeriod };

		SubscribeCandles(CandleType)
			.Bind(fast, slow, atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev) { _prevFast = fast; _prevSlow = slow; _hasPrev = true; return; }

		var close = candle.ClosePrice;

		// Trail stop management
		if (Position > 0 && atr > 0)
		{
			_highSinceLong = Math.Max(_highSinceLong, candle.HighPrice);
			var newTrail = _highSinceLong - atr * TrailMult;
			if (newTrail > _trailStop) _trailStop = newTrail;

			if (close <= _trailStop)
			{
				SellMarket();
				_trailStop = 0;
				_prevFast = fast; _prevSlow = slow;
				return;
			}
		}
		else if (Position < 0 && atr > 0)
		{
			_lowSinceShort = Math.Min(_lowSinceShort, candle.LowPrice);
			var newTrail = _lowSinceShort + atr * TrailMult;
			if (newTrail < _trailStop || _trailStop == 0) _trailStop = newTrail;

			if (close >= _trailStop)
			{
				BuyMarket();
				_trailStop = 0;
				_prevFast = fast; _prevSlow = slow;
				return;
			}
		}

		// Entry signals: EMA crossover
		if (_prevFast <= _prevSlow && fast > slow)
		{
			if (Position < 0) BuyMarket();
			if (Position <= 0)
			{
				BuyMarket();
				_highSinceLong = candle.HighPrice;
				_trailStop = close - atr * TrailMult;
			}
		}
		else if (_prevFast >= _prevSlow && fast < slow)
		{
			if (Position > 0) SellMarket();
			if (Position >= 0)
			{
				SellMarket();
				_lowSinceShort = candle.LowPrice;
				_trailStop = close + atr * TrailMult;
			}
		}

		_prevFast = fast; _prevSlow = slow;
	}
}
