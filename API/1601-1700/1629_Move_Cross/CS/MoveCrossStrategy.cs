using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RAVI based trend strategy.
/// Uses fast and slow SMA to compute RAVI oscillator. Enters on RAVI trending conditions.
/// </summary>
public class MoveCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _raviPrev1;
	private decimal _raviPrev2;
	private decimal _raviPrev3;
	private bool _hasHistory;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MoveCrossStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast SMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 24)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow SMA period", "Indicators");

		_threshold = Param(nameof(Threshold), 0.5m)
			.SetDisplay("Threshold", "RAVI threshold", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_raviPrev1 = 0;
		_raviPrev2 = 0;
		_raviPrev3 = 0;
		_hasHistory = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastSma = new SimpleMovingAverage { Length = FastPeriod };
		var slowSma = new SimpleMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, slowSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (slow == 0)
			return;

		var ravi = (fast - slow) / slow * 100m;

		if (_hasHistory)
		{
			// Buy: RAVI rising for 3 bars and above threshold
			var raviRising = ravi > _raviPrev1 && _raviPrev1 > _raviPrev2 && _raviPrev2 > _raviPrev3;
			// Sell: RAVI falling for 3 bars and below negative threshold
			var raviFalling = ravi < _raviPrev1 && _raviPrev1 < _raviPrev2 && _raviPrev2 < _raviPrev3;

			if (raviRising && ravi > Threshold && Position <= 0)
				BuyMarket();
			else if (raviFalling && ravi < -Threshold && Position >= 0)
				SellMarket();
		}

		_raviPrev3 = _raviPrev2;
		_raviPrev2 = _raviPrev1;
		_raviPrev1 = ravi;
		_hasHistory = true;
	}
}
