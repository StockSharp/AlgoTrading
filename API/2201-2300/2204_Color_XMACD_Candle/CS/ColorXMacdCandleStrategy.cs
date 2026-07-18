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
/// Strategy that reacts to slope changes of the MACD histogram.
/// Buys when histogram slope turns up, sells when it turns down.
/// </summary>
public class ColorXMacdCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _signalMa;
	private decimal? _prevHist;
	private decimal? _prevPrevHist;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorXMacdCandleStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast EMA period", "MACD");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow EMA period", "MACD");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Signal line period", "MACD");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for calculations", "Common");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_signalMa = null;
		_prevHist = null;
		_prevPrevHist = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var macd = new MovingAverageConvergenceDivergence();
		macd.ShortMa.Length = FastPeriod;
		macd.LongMa.Length = SlowPeriod;

		_signalMa = new ExponentialMovingAverage { Length = SignalPeriod };
		Indicators.Add(_signalMa);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFormed)
			return;

		var macdLine = macdValue.GetValue<decimal>();
		var signalResult = _signalMa.Process(macdValue);

		if (!signalResult.IsFormed)
			return;

		var signalLine = signalResult.GetValue<decimal>();
		var hist = macdLine - signalLine;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevPrevHist = _prevHist;
			_prevHist = hist;
			return;
		}

		if (_prevHist is decimal ph && _prevPrevHist is decimal pph)
		{
			var wasRising = ph > pph;
			var nowRising = hist > ph;

			// Histogram slope turns up -> buy
			if (!wasRising && nowRising && Position <= 0)
				BuyMarket();
			// Histogram slope turns down -> sell
			else if (wasRising && !nowRising && Position >= 0)
				SellMarket();
		}

		_prevPrevHist = _prevHist;
		_prevHist = hist;
	}
}
