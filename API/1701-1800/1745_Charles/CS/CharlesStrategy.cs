using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Charles strategy: EMA crossover with RSI filter.
/// </summary>
public class CharlesStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private bool _wasFastBelowSlow;
	private bool _isInitialized;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CharlesStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 18)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Period of the fast EMA", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Period of the slow EMA", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation length", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_wasFastBelowSlow = false;
		_isInitialized = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		SubscribeCandles(CandleType)
			.Bind(fast, slow, rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_isInitialized)
		{
			_wasFastBelowSlow = fastValue < slowValue;
			_isInitialized = true;
			return;
		}

		var isFastBelowSlow = fastValue < slowValue;

		// Long signal: fast crosses above slow + RSI > 55
		if (_wasFastBelowSlow && !isFastBelowSlow && rsiValue > 55 && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Short signal: fast crosses below slow + RSI < 45
		else if (!_wasFastBelowSlow && isFastBelowSlow && rsiValue < 45 && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_wasFastBelowSlow = isFastBelowSlow;
	}
}
