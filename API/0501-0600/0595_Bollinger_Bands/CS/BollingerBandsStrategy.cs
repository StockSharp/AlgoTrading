using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands strategy.
/// A close above the upper Bollinger band goes long and a close below the lower band goes short, reversing an opposite position.
/// A long closes when the close falls below SMA(SmaLength) and a short when it rises above it, and a percent stop limits the loss.
/// </summary>
public class BollingerBandsStrategy : Strategy
{
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbDeviation;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Bollinger period.
	/// </summary>
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger standard deviation multiplier.
	/// </summary>
	public decimal BbDeviation
	{
		get => _bbDeviation.Value;
		set => _bbDeviation.Value = value;
	}

	/// <summary>
	/// Period of the exit SMA.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
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
	public BollingerBandsStrategy()
	{
		_bbLength = Param(nameof(BbLength), 120)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger period", "Bollinger");

		_bbDeviation = Param(nameof(BbDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Deviation", "Bollinger standard deviation multiplier", "Bollinger");

		_smaLength = Param(nameof(SmaLength), 110)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Period of the exit SMA", "Exit");

		_stopLossPercent = Param(nameof(StopLossPercent), 6m)
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
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bollinger = new BollingerBands { Length = BbLength, Width = BbDeviation };
		var sma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, sma, ProcessCandle)
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
			DrawIndicator(area, bollinger);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFormed || !smaValue.IsFormed)
			return;

		var bands = (BollingerBandsValue)bollingerValue;

		if (bands.UpBand is not decimal upper || bands.LowBand is not decimal lower)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var sma = smaValue.GetValue<decimal>();

		if (close > upper && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < lower && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close < sma)
			SellMarket(Position);
		else if (Position < 0 && close > sma)
			BuyMarket(-Position);
	}
}
