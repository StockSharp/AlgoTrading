using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD Histogram Reversal strategy.
/// The position turns long when the MACD histogram (MACD - Signal) turns positive and short when it turns negative;
/// a zero histogram keeps its previous sign. A percent stop limits the loss.
/// </summary>
public class MacdHistogramReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private bool? _prevPositive;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MacdHistogramReversalStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetRange(8, 16)
			.SetDisplay("Fast Period", "Fast EMA period for MACD", "MACD");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetRange(20, 30)
			.SetDisplay("Slow Period", "Slow EMA period for MACD", "MACD");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetRange(7, 13)
			.SetDisplay("Signal Period", "Signal line period", "MACD");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");
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
		_prevPositive = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevPositive = null;

		var macdHist = new MovingAverageConvergenceDivergenceHistogram
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macdHist, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macdHist);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdIv)
	{
		if (candle.State != CandleStates.Finished || !macdIv.IsFormed)
			return;

		var mv = (IMacdHistogramValue)macdIv;

		if (mv.Macd is not decimal macdVal || mv.Signal is not decimal signalVal)
			return;

		var histogram = macdVal - signalVal;

		if (histogram == 0)
			return;

		var positive = histogram > 0;
		var wasPositive = _prevPositive;
		_prevPositive = positive;

		if (wasPositive is not bool previous || previous == positive || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (positive && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (!positive && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
