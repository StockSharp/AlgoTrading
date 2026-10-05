using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchian MACD strategy.
/// The channel spans the highest high and lowest low of the previous DonchianPeriod candles. A close above it with MACD above its signal
/// line goes long and a close below it with MACD below the signal line goes short, reversing an opposite position. A long closes when MACD
/// crosses below the signal line and a short when it crosses above it, and a percent stop limits the loss.
/// </summary>
public class DonchianMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevUpper;
	private decimal? _prevLower;

	/// <summary>
	/// Previous candles the channel spans.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA period of MACD.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow EMA period of MACD.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal line period of MACD.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
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
	/// Constructor.
	/// </summary>
	public DonchianMacdStrategy()
	{
		_donchianPeriod = Param(nameof(DonchianPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Period", "Previous candles the channel spans", "Indicators");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period of MACD", "MACD");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period of MACD", "MACD");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line period of MACD", "MACD");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

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
		_prevUpper = null;
		_prevLower = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevUpper = null;
		_prevLower = null;

		var donchian = new DonchianChannels { Length = DonchianPeriod };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, macd, ProcessCandle)
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
			DrawIndicator(area, donchian);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, macd);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The channel is measured on the candles before this one.
		var upper = _prevUpper;
		var lower = _prevLower;

		if (donchianValue.IsFormed && donchianValue is IDonchianChannelsValue { UpperBand: decimal currentUpper, LowerBand: decimal currentLower })
		{
			_prevUpper = currentUpper;
			_prevLower = currentLower;
		}

		if (!macdValue.IsFormed || upper is not decimal channelHigh || lower is not decimal channelLow)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (close > channelHigh && macd > signal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < channelLow && macd < signal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && macd < signal)
			SellMarket(Position);
		else if (Position < 0 && macd > signal)
			BuyMarket(-Position);
	}
}
