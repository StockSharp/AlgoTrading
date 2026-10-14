using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hurst Exponent Reversion strategy.
/// A HurstPeriod Hurst exponent below HurstThreshold marks a mean-reverting market. Then a close below the AveragePeriod simple moving average
/// goes long and a close above it goes short, reversing an opposite position. A long closes once the close is back at or above the average
/// or the exponent rises above the threshold, a short mirrors it, and a percent stop limits the loss.
/// </summary>
public class HurstExponentReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _hurstPeriod;
	private readonly StrategyParam<int> _averagePeriod;
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
	public int AveragePeriod
	{
		get => _averagePeriod.Value;
		set => _averagePeriod.Value = value;
	}

	/// <summary>
	/// Hurst exponent level below which the market reverts.
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
	public HurstExponentReversionStrategy()
	{
		_hurstPeriod = Param(nameof(HurstPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Hurst Period", "Period of the Hurst exponent", "Indicators");

		_averagePeriod = Param(nameof(AveragePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Average Period", "Period of the simple moving average", "Indicators");

		_hurstThreshold = Param(nameof(HurstThreshold), 0.7m)
			.SetDisplay("Hurst Threshold", "Hurst exponent level below which the market reverts", "Indicators");

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
		var sma = new SimpleMovingAverage { Length = AveragePeriod };

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
		var reverting = hurst < HurstThreshold;

		if (reverting && close < sma && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (reverting && close > sma && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && (close >= sma || hurst > HurstThreshold))
			SellMarket(Position);
		else if (Position < 0 && (close <= sma || hurst > HurstThreshold))
			BuyMarket(-Position);
	}
}
