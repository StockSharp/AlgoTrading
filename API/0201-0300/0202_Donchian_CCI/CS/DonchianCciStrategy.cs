using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchian CCI strategy.
/// The channel spans the highest high and lowest low of the previous DonchianPeriod candles. A close above it with CCI above CciOverbought
/// confirms momentum and goes long, a close below it with CCI below CciOversold goes short, reversing an opposite position. A long closes
/// when price falls below the channel middle and a short when it rises above it, and a percent stop limits the loss.
/// </summary>
public class DonchianCciStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciOverbought;
	private readonly StrategyParam<decimal> _cciOversold;
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
	/// Period of CCI.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// CCI level that confirms an upside breakout.
	/// </summary>
	public decimal CciOverbought
	{
		get => _cciOverbought.Value;
		set => _cciOverbought.Value = value;
	}

	/// <summary>
	/// CCI level that confirms a downside breakout.
	/// </summary>
	public decimal CciOversold
	{
		get => _cciOversold.Value;
		set => _cciOversold.Value = value;
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
	public DonchianCciStrategy()
	{
		_donchianPeriod = Param(nameof(DonchianPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Period", "Previous candles the channel spans", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period of CCI", "Indicators");

		_cciOverbought = Param(nameof(CciOverbought), 100m)
			.SetDisplay("CCI Overbought", "CCI level that confirms an upside breakout", "Indicators");

		_cciOversold = Param(nameof(CciOversold), -100m)
			.SetDisplay("CCI Oversold", "CCI level that confirms a downside breakout", "Indicators");

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
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, cci, ProcessCandle)
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
				DrawIndicator(oscillators, cci);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue cciValue)
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

		if (!cciValue.IsFormed || upper is not decimal channelHigh || lower is not decimal channelLow)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var cci = cciValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var middle = (channelHigh + channelLow) / 2;

		if (close > channelHigh && cci > CciOverbought && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < channelLow && cci < CciOversold && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close < middle)
			SellMarket(Position);
		else if (Position < 0 && close > middle)
			BuyMarket(-Position);
	}
}
