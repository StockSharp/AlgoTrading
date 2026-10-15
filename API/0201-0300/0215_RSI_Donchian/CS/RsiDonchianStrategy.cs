using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI Donchian strategy.
/// The channel spans the highest high and lowest low of the previous DonchianPeriod candles. A close above it with RSI above RsiOverbought
/// confirms momentum and goes long, a close below it with RSI below RsiOversold goes short, reversing an opposite position. A long closes
/// when price falls below the channel middle and a short when it rises above it, and a percent stop limits the loss.
/// </summary>
public class RsiDonchianStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevUpper;
	private decimal? _prevLower;

	/// <summary>
	/// Period of RSI.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Previous candles the channel spans.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}

	/// <summary>
	/// RSI level that confirms an upside breakout.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI level that confirms a downside breakout.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
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
	public RsiDonchianStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period of RSI", "Indicators");

		_donchianPeriod = Param(nameof(DonchianPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Period", "Previous candles the channel spans", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI level that confirms an upside breakout", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI level that confirms a downside breakout", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		var middle = (channelHigh + channelLow) / 2;

		if (close > channelHigh && rsi > RsiOverbought && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < channelLow && rsi < RsiOversold && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close < middle)
			SellMarket(Position);
		else if (Position < 0 && close > middle)
			BuyMarket(-Position);
	}
}
