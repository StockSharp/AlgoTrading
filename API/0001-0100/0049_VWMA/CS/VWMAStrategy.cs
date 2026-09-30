using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades actual Close crossings of native rolling Close/volume VWMA.
/// The opposite crossing reverses the position; actual-fill percent protection flattens it.
/// </summary>
public class VWMAStrategy : Strategy
{
	private readonly StrategyParam<int> _vwmaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private decimal? _previousClose;
	private decimal _previousVwma;
	private Order _pendingOrder;

	public int VWMAPeriod { get => _vwmaPeriod.Value; set => _vwmaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	public VWMAStrategy()
	{
		_vwmaPeriod = Param(nameof(VWMAPeriod), 14).SetGreaterThanZero()
			.SetDisplay("VWMA Period", "Current-inclusive rolling Close/volume mean length", "Indicators")
			.SetOptimize(5, 30, 5);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection");
		OrderRegistering += order => _pendingOrder = order;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ClearSignalState();
	}

	private void ClearSignalState()
	{
		_previousClose = null;
		_previousVwma = 0m;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ClearSignalState();
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		var vwma = new VolumeWeightedMovingAverage { Length = VWMAPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(vwma, ProcessCandle, false).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwma);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before this callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;
		if (value.IsEmpty || !value.Indicator.IsFormed)
		{
			// A zero-volume window has no mean and cannot connect crossing history across it.
			_previousClose = null;
			return;
		}
		var vwma = value.GetValue<decimal>();
		var upwardCross = _previousClose is decimal up && up <= _previousVwma && candle.ClosePrice > vwma;
		var downwardCross = _previousClose is decimal down && down >= _previousVwma && candle.ClosePrice < vwma;
		// The first formed value only seeds; history continues while an order is pending.
		_previousClose = candle.ClosePrice;
		_previousVwma = vwma;
		if (!IsFormedAndOnlineAndAllowTrading() ||
			_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (upwardCross && Position <= 0m) BuyMarket(this.ReversalVolume());
		else if (downwardCross && Position >= 0m) SellMarket(this.ReversalVolume());
	}
}
