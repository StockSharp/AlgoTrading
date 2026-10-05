using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gap Fill Reversal strategy.
/// A gap is an open at least MinGapPercent away from the previous close. When the same candle trades back to the previous close,
/// filling the gap, the position turns against the gap: short after a gap up, long after a gap down. A percent stop limits the loss.
/// </summary>
public class GapFillReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _minGapPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;

	/// <summary>
	/// Minimum gap between the previous close and the open, in percent.
	/// </summary>
	public decimal MinGapPercent
	{
		get => _minGapPercent.Value;
		set => _minGapPercent.Value = value;
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
	public GapFillReversalStrategy()
	{
		_minGapPercent = Param(nameof(MinGapPercent), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Min Gap %", "Minimum gap between the previous close and the open", "Pattern");

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
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
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
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevClose = _prevClose;
		_prevClose = candle.ClosePrice;

		if (prevClose is not decimal last || last <= 0 || !IsFormedAndOnlineAndAllowTrading())
			return;

		var gap = (candle.OpenPrice - last) / last * 100m;

		if (gap >= MinGapPercent && candle.LowPrice <= last && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (-gap >= MinGapPercent && candle.HighPrice >= last && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
	}
}
