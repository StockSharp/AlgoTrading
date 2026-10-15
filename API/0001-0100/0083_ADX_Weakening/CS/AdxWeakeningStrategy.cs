using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ADX Weakening strategy.
/// While flat, a fall of ADX from the previous candle buys when the close is above the SMA and sells when it is below.
/// The position is held until ADX rises again or the percent stop is hit.
/// </summary>
public class AdxWeakeningStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevAdx;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// SMA period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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
	public AdxWeakeningStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for ADX", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for SMA", "Indicators");

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
		_prevAdx = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAdx = null;

		var sma = new SimpleMovingAverage { Length = MaPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, adx, ProcessCandle)
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
			DrawIndicator(area, sma);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaIv, IIndicatorValue adxIv)
	{
		if (candle.State != CandleStates.Finished || !adxIv.IsFormed || !smaIv.IsFormed)
			return;

		if (adxIv is not AverageDirectionalIndexValue { MovingAverage: decimal adx })
			return;

		var prevAdx = _prevAdx;
		_prevAdx = adx;

		if (prevAdx is not decimal lastAdx || !IsFormedAndOnlineAndAllowTrading())
			return;

		var sma = smaIv.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (adx > lastAdx)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (adx > lastAdx)
				BuyMarket(-Position);
		}
		else if (adx < lastAdx)
		{
			if (close > sma)
				BuyMarket(Volume);
			else if (close < sma)
				SellMarket(Volume);
		}
	}
}
