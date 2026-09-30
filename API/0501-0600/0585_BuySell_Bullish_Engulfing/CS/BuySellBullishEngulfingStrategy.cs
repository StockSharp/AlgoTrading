using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum BullishEngulfingTrendMode
{
	None,
	SMA50,
}

/// <summary>
/// Buys bullish body engulfings, optionally following a below-SMA50 bearish bar.
/// Allocates a percentage of current equity and closes longs through local SL/TP.
/// </summary>
public class BuySellBullishEngulfingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _orderPercent;
	private readonly StrategyParam<BullishEngulfingTrendMode> _trendMode;
	private (decimal Open, decimal Close, decimal Sma, bool SmaReady)? _previousBar;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal OrderPercent { get => _orderPercent.Value; set => _orderPercent.Value = value; }
	public BullishEngulfingTrendMode TrendMode { get => _trendMode.Value; set => _trendMode.Value = value; }

	public BuySellBullishEngulfingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame());
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m).SetRange(0m, 100m);
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetRange(0m, 100m);
		_orderPercent = Param(nameof(OrderPercent), 30m).SetRange(0m, 100m).SetGreaterThanZero();
		_trendMode = Param(nameof(TrendMode), BullishEngulfingTrendMode.SMA50);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousBar = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (TrendMode is not (BullishEngulfingTrendMode.None or BullishEngulfingTrendMode.SMA50))
			throw new ArgumentOutOfRangeException(nameof(TrendMode));
		_previousBar = null;
		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true, isLocalStop: true);

		var sma = new SimpleMovingAverage { Length = TrendMode == BullishEngulfingTrendMode.SMA50 ? 50 : 1 };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, (candle, value) => ProcessCandle(candle, value, sma.IsFormed)).Start();

		// Feed real bid updates to the native long-position protection between candle closes.
		var quotes = new Subscription(DataType.Level1, Security);
		quotes.MarketData.BuildField = Level1Fields.BestBidPrice;
		SubscribeLevel1(quotes).Bind(_ => { }).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (TrendMode == BullishEngulfingTrendMode.SMA50)
				DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, bool smaReady)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var previous = _previousBar;
		_previousBar = (candle.OpenPrice, candle.ClosePrice, smaValue, smaReady);
		if (previous is not { } bar || Position != 0m || !IsFormedAndOnlineAndAllowTrading())
			return;

		var engulfing = bar.Open > bar.Close && candle.ClosePrice > candle.OpenPrice
			&& candle.OpenPrice <= bar.Close && candle.ClosePrice >= bar.Open;
		var trend = TrendMode == BullishEngulfingTrendMode.None || bar.SmaReady && bar.Close < bar.Sma;
		if (!engulfing || !trend || candle.ClosePrice <= 0m)
			return;

		var equity = Portfolio.CurrentValue ?? Portfolio.BeginValue ?? 0m;
		var step = Security.VolumeStep ?? 1m;
		if (equity <= 0m || step <= 0m)
			return;
		var volume = equity * OrderPercent / 100m / candle.ClosePrice;
		volume = Math.Floor(Math.Min(volume, Security.MaxVolume ?? decimal.MaxValue) / step) * step;
		if (volume > 0m && volume >= (Security.MinVolume ?? step))
			BuyMarket(volume);
	}
}
