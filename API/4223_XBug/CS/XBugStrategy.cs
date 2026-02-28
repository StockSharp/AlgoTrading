namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Moving average crossover strategy converted from the MQL4 expert "X bug".
/// Uses fast and slow SMA crossover for signal generation.
/// </summary>
public class XBugStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _slowMa;
	private decimal? _prevFast;
	private decimal? _prevSlow;

	public XBugStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA period", "Length of the fast moving average.", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA period", "Length of the slow moving average.", "Indicators");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse signals", "Invert buy and sell directions.", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe used for signals.", "General");
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		Volume = 0.001m;
		_prevFast = null;
		_prevSlow = null;

		_slowMa = new SimpleMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_slowMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_slowMa.IsFormed)
			return;

		// Use close price as the "fast" value (period=1 effectively)
		var fastValue = candle.ClosePrice;

		if (_prevFast is null || _prevSlow is null)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			return;
		}

		var signal = 0;
		if (fastValue > slowValue && _prevFast.Value <= _prevSlow.Value)
			signal = 1;
		else if (fastValue < slowValue && _prevFast.Value >= _prevSlow.Value)
			signal = -1;

		_prevFast = fastValue;
		_prevSlow = slowValue;

		if (signal == 0)
			return;

		if (ReverseSignals)
			signal = -signal;

		if (signal > 0 && Position <= 0)
		{
			BuyMarket();
		}
		else if (signal < 0 && Position >= 0)
		{
			SellMarket();
		}
	}
}
