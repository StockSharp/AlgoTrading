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
/// Strategy based on MACD indicator.
/// It enters long position when MACD crosses above signal line and short position when MACD crosses below signal line,
/// reversing on the opposite cross, and protects the position with a percent stop.
/// </summary>
public class MacdTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	// Side of the signal line on the previous candle; unknown until the indicator forms.
	private bool? _prevIsMacdAboveSignal;

	/// <summary>
	/// Period for fast EMA in MACD.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period for slow EMA in MACD.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period for signal line in MACD.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss as percentage of entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	/// Initialize the MACD Trend strategy.
	/// </summary>
	public MacdTrendStrategy()
	{
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Period", "Period for fast EMA in MACD", "Indicators")

			.SetOptimize(8, 16, 2);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Period", "Period for slow EMA in MACD", "Indicators")

			.SetOptimize(20, 32, 2);

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Period for signal line in MACD", "Indicators")

			.SetOptimize(5, 13, 2);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Stop loss as a percentage of entry price", "Risk parameters")
			
			.SetOptimize(1, 3, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevIsMacdAboveSignal = null;

	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create MACD indicator with signal line

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastEmaPeriod },
				LongMa = { Length = SlowEmaPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};
		// Create subscription and bind indicator
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}

	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished || !macdValue.IsFormed)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		var isMacdAboveSignal = macd > signal;
		var wasMacdAboveSignal = _prevIsMacdAboveSignal;
		_prevIsMacdAboveSignal = isMacdAboveSignal;

		if (wasMacdAboveSignal is not bool wasAbove || wasAbove == isMacdAboveSignal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (isMacdAboveSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			LogInfo($"Buy signal: MACD ({macd:F5}) crossed above Signal ({signal:F5})");
		}
		else if (!isMacdAboveSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			LogInfo($"Sell signal: MACD ({macd:F5}) crossed below Signal ({signal:F5})");
		}
	}
}
