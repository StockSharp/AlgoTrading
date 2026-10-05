using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// OBV Mean Reversion strategy.
/// The bands lie Multiplier standard deviations around the average of the last AveragePeriod OBV values, the current one included.
/// OBV below the lower band with the close below the AveragePeriod simple moving average goes long and OBV above the upper band with the close above it goes short,
/// reversing an opposite position. A long closes once OBV is back above its average and a short once it is back below it, and a percent stop limits the loss.
/// </summary>
public class ObvMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _averagePeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _values = [];

	/// <summary>
	/// Values of OBV the average and the standard deviation span.
	/// </summary>
	public int AveragePeriod
	{
		get => _averagePeriod.Value;
		set => _averagePeriod.Value = value;
	}

	/// <summary>
	/// Standard deviations between the average and a band.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
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
	public ObvMeanReversionStrategy()
	{
		_averagePeriod = Param(nameof(AveragePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Average Period", "Values of OBV the average and the standard deviation span", "Indicators");

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Standard deviations between the average and a band", "Indicators");

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
		_values.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_values.Clear();

		var obv = new OnBalanceVolume();
		var sma = new SimpleMovingAverage { Length = AveragePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(obv, sma, ProcessCandle)
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
				DrawIndicator(oscillators, obv);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue obvValue, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!smaValue.IsFormed)
			return;

		if (!obvValue.IsFormed)
			return;

		var value = obvValue.GetValue<decimal>();

		_values.Enqueue(value);

		if (_values.Count > AveragePeriod)
			_values.Dequeue();

		if (_values.Count < AveragePeriod)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var mean = _values.Average();
		var deviation = (decimal)Math.Sqrt((double)_values.Average(v => (v - mean) * (v - mean)));
		var upper = mean + Multiplier * deviation;
		var lower = mean - Multiplier * deviation;
		var close = candle.ClosePrice;
		var ma = smaValue.GetValue<decimal>();

		if (value < lower && close < ma && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (value > upper && close > ma && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && value > mean)
			SellMarket(Position);
		else if (Position < 0 && value < mean)
			BuyMarket(-Position);
	}
}
