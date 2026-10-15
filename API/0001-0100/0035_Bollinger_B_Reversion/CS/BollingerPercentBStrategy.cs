using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on Bollinger %B indicator.
/// Bollinger %B shows where price is relative to the Bollinger Bands.
/// Values below 0 or above 1 indicate price outside the bands.
/// </summary>
public class BollingerPercentBStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _exitValue;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private Order _pendingOrder;
	private decimal? _previousPercentB;

	/// <summary>
	/// Period for Bollinger Bands calculation.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Deviation for Bollinger Bands calculation.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Exit threshold for %B.
	/// </summary>
	public decimal ExitValue
	{
		get => _exitValue.Value;
		set => _exitValue.Value = value;
	}

	/// <summary>
	/// Type of candles used for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the Bollinger %B Reversion strategy.
	/// </summary>
	public BollingerPercentBStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20).SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
			.SetOptimize(10, 30, 5);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m).SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Deviation for Bollinger Bands calculation", "Indicators")
			.SetOptimize(1.5m, 2.5m, 0.25m);

		_exitValue = Param(nameof(ExitValue), 0.5m).SetRange(0m, 1m)
			.SetDisplay("Exit %B Value", "Exit threshold for %B", "Exit")
			.SetOptimize(0.3m, 0.7m, 0.1m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it", "Protection");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		OrderRegistering += order => _pendingOrder = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DataType.Level1)];
	}

	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_pendingOrder = null;
		_previousPercentB = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var percentB = new BollingerPercentB
		{
			Length = BollingerPeriod,
			StdDevMultiplier = BollingerDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(percentB, ProcessCandle, true)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, percentB);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before this callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished || !value.Indicator.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;
		// Native %B is in percent units and empty for collapsed bands.
		// Explicitly normalize to the README's scale; collapsed bands are neutral, not directional zero.
		var percentB = value.IsEmpty ? 0.5m : value.GetValue<decimal>() / 100m;
		var previous = _previousPercentB;
		_previousPercentB = percentB;
		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		var upwardCross = previous is decimal prevUp && prevUp < ExitValue && percentB >= ExitValue;
		var downwardCross = previous is decimal prevDown && prevDown > ExitValue && percentB <= ExitValue;
		if (Position > 0m && upwardCross)
			SellMarket(Position);
		else if (Position < 0m && downwardCross)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m && percentB < 0m)
			BuyMarket(Volume);
		else if (Position == 0m && percentB > 1m)
			SellMarket(Volume);
	}
}
