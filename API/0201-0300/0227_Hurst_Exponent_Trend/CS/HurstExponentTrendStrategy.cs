using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hurst Exponent Trend strategy.
/// A HurstPeriod Hurst exponent above HurstThreshold marks a trending market. Then a close above the MaPeriod simple moving average goes long
/// and a close below it goes short, reversing an opposite position. A long closes once the close is below the average or the exponent
/// drops below the threshold, a short mirrors it, and a percent stop limits the loss.
/// </summary>
public class HurstExponentTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _hurstPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _hurstThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period of the Hurst exponent.
	/// </summary>
	public int HurstPeriod
	{
		get => _hurstPeriod.Value;
		set => _hurstPeriod.Value = value;
	}

	/// <summary>
	/// Period of the simple moving average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Hurst exponent level of a trending market.
	/// </summary>
	public decimal HurstThreshold
	{
		get => _hurstThreshold.Value;
		set => _hurstThreshold.Value = value;
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
	public HurstExponentTrendStrategy()
	{
		_hurstPeriod = Param(nameof(HurstPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Hurst Period", "Period of the Hurst exponent", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period of the simple moving average", "Indicators");

		_hurstThreshold = Param(nameof(HurstThreshold), 0.55m)
			.SetDisplay("Hurst Threshold", "Hurst exponent level of a trending market", "Indicators");

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
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var hurst = new HurstExponent { Length = HurstPeriod };
		var sma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(hurst, sma, ProcessCandle)
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
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, hurst);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue hurstValue, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!hurstValue.IsFormed || !smaValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hurst = hurstValue.GetValue<decimal>();
		var sma = smaValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var trending = hurst > HurstThreshold;

		if (trending && close > sma && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (trending && close < sma && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && (close < sma || hurst < HurstThreshold))
			SellMarket(Position);
		else if (Position < 0 && (close > sma || hurst < HurstThreshold))
			BuyMarket(-Position);
	}
}
