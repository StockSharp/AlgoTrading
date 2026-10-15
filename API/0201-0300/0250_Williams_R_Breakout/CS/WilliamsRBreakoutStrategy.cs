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
/// Williams R Breakout strategy.
/// The bands lie Multiplier standard deviations around the average of the last AvgPeriod %R values, the current one included.
/// %R above the upper band goes long and %R below the lower band goes short,
/// reversing an opposite position. A long closes once %R is back below its average and a short once it is back above it, and a percent stop limits the loss.
/// </summary>
public class WilliamsRBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _williamsRPeriod;
	private readonly StrategyParam<int> _avgPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _values = [];

	/// <summary>
	/// Period of Williams %R.
	/// </summary>
	public int WilliamsRPeriod
	{
		get => _williamsRPeriod.Value;
		set => _williamsRPeriod.Value = value;
	}

	/// <summary>
	/// Values of %R the average and the standard deviation span.
	/// </summary>
	public int AvgPeriod
	{
		get => _avgPeriod.Value;
		set => _avgPeriod.Value = value;
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
	public WilliamsRBreakoutStrategy()
	{
		_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Period of Williams %R", "Indicators");

		_avgPeriod = Param(nameof(AvgPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Average Period", "Values of %R the average and the standard deviation span", "Indicators");

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

		var williams = new WilliamsR { Length = WilliamsRPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(williams, ProcessCandle)
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
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, williams);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue williamsValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!williamsValue.IsFormed)
			return;

		var value = williamsValue.GetValue<decimal>();

		_values.Enqueue(value);

		if (_values.Count > AvgPeriod)
			_values.Dequeue();

		if (_values.Count < AvgPeriod)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var mean = _values.Average();
		var deviation = (decimal)Math.Sqrt((double)_values.Average(v => (v - mean) * (v - mean)));
		var upper = mean + Multiplier * deviation;
		var lower = mean - Multiplier * deviation;

		if (value > upper && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (value < lower && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && value < mean)
			SellMarket(Position);
		else if (Position < 0 && value > mean)
			BuyMarket(-Position);
	}
}
