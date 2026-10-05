using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibonacci Swing Trading Bot strategy.
/// Retracement levels FiboLevel1 and FiboLevel2 are measured down from the highest high of the last 50 candles towards the
/// lowest low. A close breaking above the FiboLevel1 level goes long and a close breaking below the FiboLevel2 level goes short,
/// reversing an opposite position. Each position is protected by a percent stop loss and a take profit RiskRewardRatio times
/// further away.
/// </summary>
public class FibonacciSwingTradingBotStrategy : Strategy
{
	private const int _rangeLength = 50;

	private readonly StrategyParam<decimal> _fiboLevel1;
	private readonly StrategyParam<decimal> _fiboLevel2;
	private readonly StrategyParam<decimal> _riskRewardRatio;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevLevel1;
	private decimal? _prevLevel2;

	/// <summary>
	/// Retracement ratio of the level whose upward break goes long.
	/// </summary>
	public decimal FiboLevel1
	{
		get => _fiboLevel1.Value;
		set => _fiboLevel1.Value = value;
	}

	/// <summary>
	/// Retracement ratio of the level whose downward break goes short.
	/// </summary>
	public decimal FiboLevel2
	{
		get => _fiboLevel2.Value;
		set => _fiboLevel2.Value = value;
	}

	/// <summary>
	/// Take profit distance as a multiple of the stop distance.
	/// </summary>
	public decimal RiskRewardRatio
	{
		get => _riskRewardRatio.Value;
		set => _riskRewardRatio.Value = value;
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
	public FibonacciSwingTradingBotStrategy()
	{
		_fiboLevel1 = Param(nameof(FiboLevel1), 0.618m)
			.SetDisplay("Fibo Level 1", "Retracement ratio of the long breakout level", "Fibonacci");

		_fiboLevel2 = Param(nameof(FiboLevel2), 0.786m)
			.SetDisplay("Fibo Level 2", "Retracement ratio of the short breakout level", "Fibonacci");

		_riskRewardRatio = Param(nameof(RiskRewardRatio), 2m)
			.SetNotNegative()
			.SetDisplay("Risk/Reward", "Take profit distance as a multiple of the stop distance", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_prevClose = null;
		_prevLevel1 = null;
		_prevLevel2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var highest = new Highest { Length = _rangeLength };
		var lowest = new Lowest { Length = _rangeLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(StopLossPercent * RiskRewardRatio, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true,
			isLocalStop: true);

		// The stop and target have to see prices between candles, not only at their close.
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
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, decimal high, decimal low)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = high - low;
		var level1 = high - range * FiboLevel1;
		var level2 = high - range * FiboLevel2;

		var prevClose = _prevClose;
		var prevLevel1 = _prevLevel1;
		var prevLevel2 = _prevLevel2;

		_prevClose = candle.ClosePrice;
		_prevLevel1 = level1;
		_prevLevel2 = level2;

		if (prevClose is not decimal lastClose || prevLevel1 is not decimal lastLevel1 || prevLevel2 is not decimal lastLevel2)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (lastClose <= lastLevel1 && close > level1 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (lastClose >= lastLevel2 && close < level2 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
