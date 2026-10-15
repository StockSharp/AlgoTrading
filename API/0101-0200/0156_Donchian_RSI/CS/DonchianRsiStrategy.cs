using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchian RSI strategy.
/// The channel spans the highest high and lowest low of the previous DonchianPeriod candles. A close above it while RSI is still below
/// RsiOverboughtLevel goes long and a close below it while RSI is above RsiOversoldLevel goes short, reversing an opposite position.
/// The breakout fails, closing the position, when price closes back beyond the broken level, and a percent stop limits the loss.
/// </summary>
public class DonchianRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOverboughtLevel;
	private readonly StrategyParam<decimal> _rsiOversoldLevel;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevUpper;
	private decimal? _prevLower;
	private decimal _breakoutLevel;

	/// <summary>
	/// Previous candles the channel spans.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}

	/// <summary>
	/// Period of RSI.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level a long breakout must stay below.
	/// </summary>
	public decimal RsiOverboughtLevel
	{
		get => _rsiOverboughtLevel.Value;
		set => _rsiOverboughtLevel.Value = value;
	}

	/// <summary>
	/// RSI level a short breakout must stay above.
	/// </summary>
	public decimal RsiOversoldLevel
	{
		get => _rsiOversoldLevel.Value;
		set => _rsiOversoldLevel.Value = value;
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
	public DonchianRsiStrategy()
	{
		_donchianPeriod = Param(nameof(DonchianPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Period", "Previous candles the channel spans", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period of RSI", "Indicators");

		_rsiOverboughtLevel = Param(nameof(RsiOverboughtLevel), 70m)
			.SetDisplay("RSI Overbought", "RSI level a long breakout must stay below", "Indicators");

		_rsiOversoldLevel = Param(nameof(RsiOversoldLevel), 30m)
			.SetDisplay("RSI Oversold", "RSI level a short breakout must stay above", "Indicators");

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
		_breakoutLevel = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevUpper = null;
		_prevLower = null;
		_breakoutLevel = default;

		var donchian = new DonchianChannels { Length = DonchianPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, rsi, ProcessCandle)
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
				DrawIndicator(oscillators, rsi);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue rsiValue)
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

		if (!rsiValue.IsFormed || upper is not decimal channelHigh || lower is not decimal channelLow)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsi = rsiValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (close > channelHigh && rsi < RsiOverboughtLevel && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_breakoutLevel = channelHigh;
		}
		else if (close < channelLow && rsi > RsiOversoldLevel && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_breakoutLevel = channelLow;
		}
		else if (Position > 0 && close < _breakoutLevel)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && close > _breakoutLevel)
		{
			BuyMarket(-Position);
		}
	}
}
