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
/// Strategy using the Gap Momentum System by Perry Kaufman.
/// Buys when the gap momentum signal rises and sells or reverses when it falls.
/// </summary>
public class GapMomentumSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<bool> _longOnly;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSignal;
	private decimal? _prevClose;
	private readonly Queue<decimal> _up = new();
	private readonly Queue<decimal> _dn = new();
	private readonly Queue<decimal> _ratio = new();
	private decimal _sumUp;
	private decimal _sumDn;
	private decimal _sumRatio;
	private int _candleCount;

	/// <summary>
	/// Period for gap sums.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Period for signal moving average.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Allow only long trades.
	/// </summary>
	public bool LongOnly
	{
		get => _longOnly.Value;
		set => _longOnly.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="GapMomentumSystemStrategy"/>.
	/// </summary>
	public GapMomentumSystemStrategy()
	{
		_period = Param(nameof(Period), 40)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Gap accumulation period", "Parameters");

		_signalPeriod = Param(nameof(SignalPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "SMA period", "Parameters");

		_longOnly = Param(nameof(LongOnly), false)
			.SetDisplay("Long Only", "Only long trades", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSignal = 0m;
		_prevClose = null;
		_up.Clear();
		_dn.Clear();
		_ratio.Clear();
		_sumUp = 0m;
		_sumDn = 0m;
		_sumRatio = 0m;
		_candleCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate gap momentum
		var prevClose = _prevClose ?? candle.OpenPrice;
		var gap = candle.OpenPrice - prevClose;
		var up = gap > 0m ? gap : 0m;
		var dn = gap < 0m ? -gap : 0m;

		_sumUp += up;
		_sumDn += dn;
		_up.Enqueue(up);
		_dn.Enqueue(dn);
		if (_up.Count > Period)
			_sumUp -= _up.Dequeue();
		if (_dn.Count > Period)
			_sumDn -= _dn.Dequeue();

		var ratio = _sumDn == 0m ? 1m : 100m * _sumUp / _sumDn;
		_sumRatio += ratio;
		_ratio.Enqueue(ratio);
		if (_ratio.Count > SignalPeriod)
			_sumRatio -= _ratio.Dequeue();

		_prevClose = candle.ClosePrice;
		_candleCount++;

		if (_ratio.Count < SignalPeriod || _candleCount < Period + SignalPeriod)
		{
			_prevSignal = _sumRatio / Math.Max(1, _ratio.Count);
			return;
		}

		var signal = _sumRatio / SignalPeriod;

		if (signal > _prevSignal)
		{
			if (Position <= 0)
				BuyMarket();
		}
		else if (signal < _prevSignal)
		{
			if (Position > 0)
				SellMarket();
			else if (!LongOnly && Position >= 0)
				SellMarket();
		}

		_prevSignal = signal;
	}
}
