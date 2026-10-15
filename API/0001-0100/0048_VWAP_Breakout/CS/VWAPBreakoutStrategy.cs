using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades actual same-UTC-session Close crossings of cumulative candle VWAP.
/// The opposite crossing reverses the position; actual-fill percent protection flattens it.
/// </summary>
public class VWAPBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private VolumeWeightedAveragePrice _vwap;
	private DateTime? _sessionDate;
	private decimal? _previousClose;
	private decimal _previousVwap;
	private Order _pendingOrder;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	public VWAPBreakoutStrategy()
	{
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
		_vwap = null;
	}

	private void ClearSignalState()
	{
		_sessionDate = null;
		_previousClose = null;
		_previousVwap = 0m;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ClearSignalState();
		_vwap = new VolumeWeightedAveragePrice();
		Indicators.Add(_vwap);
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _vwap);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before this callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var date = candle.OpenTime.ToUniversalTime().Date;
		if (_sessionDate != date)
		{
			_sessionDate = date;
			// Reset BEFORE this day's first final bar; a reset jump is not a price crossing.
			_vwap.Reset();
			_previousClose = null;
		}
		var value = _vwap.Process(candle);
		if (value.IsEmpty || !_vwap.IsFormed)
			return;
		var vwap = value.GetValue<decimal>();
		var upwardCross = _previousClose is decimal up && up <= _previousVwap && candle.ClosePrice > vwap;
		var downwardCross = _previousClose is decimal down && down >= _previousVwap && candle.ClosePrice < vwap;
		// Seed the first valid session value and advance history even while orders are pending.
		_previousClose = candle.ClosePrice;
		_previousVwap = vwap;
		if (!IsFormedAndOnlineAndAllowTrading() ||
			_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (upwardCross && Position <= 0m) BuyMarket(this.ReversalVolume());
		else if (downwardCross && Position >= 0m) SellMarket(this.ReversalVolume());
	}
}
