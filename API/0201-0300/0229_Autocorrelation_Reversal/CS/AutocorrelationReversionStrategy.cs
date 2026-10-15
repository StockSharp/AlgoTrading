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
/// Autocorrelation Reversal strategy.
/// The autocorrelation is the lag-one autocorrelation of the close-to-close changes over the last AutoCorrPeriod closes. Below
/// AutoCorrThreshold a close under the AutoCorrPeriod simple moving average goes long and a close above it goes short, reversing an opposite
/// position. A long closes once the close is above the average or the autocorrelation rises above the threshold, a short mirrors it,
/// and a percent stop limits the loss.
/// </summary>
public class AutocorrelationReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _autoCorrPeriod;
	private readonly StrategyParam<decimal> _autoCorrThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _closes = [];

	/// <summary>
	/// Closes the autocorrelation and the average span.
	/// </summary>
	public int AutoCorrPeriod
	{
		get => _autoCorrPeriod.Value;
		set => _autoCorrPeriod.Value = value;
	}

	/// <summary>
	/// Autocorrelation below which the market reverts.
	/// </summary>
	public decimal AutoCorrThreshold
	{
		get => _autoCorrThreshold.Value;
		set => _autoCorrThreshold.Value = value;
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
	public AutocorrelationReversionStrategy()
	{
		_autoCorrPeriod = Param(nameof(AutoCorrPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Autocorrelation Period", "Closes the autocorrelation and the average span", "Indicators");

		_autoCorrThreshold = Param(nameof(AutoCorrThreshold), -0.3m)
			.SetDisplay("Autocorrelation Threshold", "Autocorrelation below which the market reverts", "Indicators");

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
		_closes.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_closes.Clear();

		var sma = new SimpleMovingAverage { Length = AutoCorrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, ProcessCandle)
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
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		_closes.Enqueue(close);

		if (_closes.Count > AutoCorrPeriod)
			_closes.Dequeue();

		if (_closes.Count < AutoCorrPeriod || !smaValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var autocorrelation = CalculateAutocorrelation();
		var sma = smaValue.GetValue<decimal>();
		var reverting = autocorrelation < AutoCorrThreshold;

		if (reverting && close < sma && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (reverting && close > sma && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && (close > sma || autocorrelation > AutoCorrThreshold))
			SellMarket(Position);
		else if (Position < 0 && (close < sma || autocorrelation > AutoCorrThreshold))
			BuyMarket(-Position);
	}

	private decimal CalculateAutocorrelation()
	{
		var closes = _closes.ToArray();
		var changes = new decimal[closes.Length - 1];

		for (var i = 1; i < closes.Length; i++)
			changes[i - 1] = closes[i] - closes[i - 1];

		var mean = changes.Average();
		var numerator = 0m;
		var denominator = 0m;

		for (var i = 0; i < changes.Length; i++)
		{
			var deviation = changes[i] - mean;
			denominator += deviation * deviation;

			if (i > 0)
				numerator += (changes[i - 1] - mean) * deviation;
		}

		return denominator == 0 ? 0 : numerator / denominator;
	}
}
