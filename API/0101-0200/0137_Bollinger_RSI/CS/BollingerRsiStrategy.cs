using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger RSI strategy.
/// A close above the upper band that is higher than the previous close above that band while RSI is lower than it was then
/// is a bearish divergence and goes short; the mirror below the lower band goes long, reversing an opposite position.
/// The position closes once price closes back inside the bands or RSI crosses back over 50, and a percent stop limits the loss.
/// </summary>
public class BollingerRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	// The previous close outside each band and RSI at that close.
	private decimal? _upperClose;
	private decimal _upperRsi;
	private decimal? _lowerClose;
	private decimal _lowerRsi;

	/// <summary>
	/// Period of the Bollinger Bands.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier of the bands.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
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
	public BollingerRsiStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Period of the Bollinger Bands", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier of the bands", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period of RSI", "Indicators");

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
		_upperClose = null;
		_upperRsi = default;
		_lowerClose = null;
		_lowerRsi = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_upperClose = null;
		_upperRsi = default;
		_lowerClose = null;
		_lowerRsi = default;

		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerDeviation };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, rsi, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFormed || !rsiValue.IsFormed)
			return;

		var bands = (BollingerBandsValue)bollingerValue;

		if (bands.UpBand is not decimal upper || bands.LowBand is not decimal lower)
			return;

		var rsi = rsiValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var signal = 0;

		if (close > upper)
		{
			if (_upperClose is decimal previousClose && close > previousClose && rsi < _upperRsi)
				signal = -1;

			_upperClose = close;
			_upperRsi = rsi;
		}
		else if (close < lower)
		{
			if (_lowerClose is decimal previousClose && close < previousClose && rsi > _lowerRsi)
				signal = 1;

			_lowerClose = close;
			_lowerRsi = rsi;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (signal > 0)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (signal < 0)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0)
		{
			if (close >= lower || rsi > 50m)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (close <= upper || rsi < 50m)
				BuyMarket(-Position);
		}
	}
}
