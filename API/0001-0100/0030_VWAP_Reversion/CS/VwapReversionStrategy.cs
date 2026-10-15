using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VWAP Reversion strategy that trades on deviations from Volume Weighted Average Price.
/// Opens positions when price deviates from VWAP and exits when price returns.
/// </summary>
public class VwapReversionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _deviationPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;

	private Order _pendingOrder;
	private VolumeWeightedAveragePrice _vwap;
	private RelativeStrengthIndex _rsi;
	private DateTime? _sessionDate;

	/// <summary>
	/// Deviation percentage from VWAP required for entry.
	/// </summary>
	public decimal DeviationPercent
	{
		get => _deviationPercent.Value;
		set => _deviationPercent.Value = value;
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
	/// RSI period confirming the entries.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level below which a long entry is confirmed.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// RSI level above which a short entry is confirmed.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// Initialize the VWAP Reversion strategy.
	/// </summary>
	public VwapReversionStrategy()
	{
		_deviationPercent = Param(nameof(DeviationPercent), 2m).SetGreaterThanZero()
			.SetDisplay("Deviation %", "Deviation from VWAP for entry", "Entry")
			.SetOptimize(0.2m, 2.0m, 0.2m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection");

		_rsiPeriod = Param(nameof(RsiPeriod), 14).SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period of the RSI confirming entries", "Entry")
			.SetOptimize(7, 21, 7);

		_rsiOversold = Param(nameof(RsiOversold), 30m).SetRange(0m, 100m)
			.SetDisplay("RSI Oversold", "Long entries require RSI below this level", "Entry");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m).SetRange(0m, 100m)
			.SetDisplay("RSI Overbought", "Short entries require RSI above this level", "Entry");
		OrderRegistering += order => _pendingOrder = order;
	}

	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_pendingOrder = null;
		_sessionDate = null;
		_vwap = null;
		_rsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_vwap = new VolumeWeightedAveragePrice();
		Indicators.Add(_vwap);
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		Indicators.Add(_rsi);
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _vwap);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before the callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// RSI runs over every finished candle and, unlike VWAP, is never reset per session.
		var rsiValue = _rsi.Process(candle);

		var date = candle.OpenTime.ToUniversalTime().Date;
		if (_sessionDate != date)
		{
			_sessionDate = date;
			// Reset BEFORE processing the first final bar of the new explicit UTC session.
			_vwap.Reset();
		}
		var value = _vwap.Process(candle);
		if (value.IsEmpty || !value.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		var vwap = value.GetValue<decimal>();
		if (vwap <= 0m)
			return;
		var deviation = 100m * (candle.ClosePrice - vwap) / vwap;
		if (Position > 0m && candle.ClosePrice >= vwap)
			SellMarket(Position);
		else if (Position < 0m && candle.ClosePrice <= vwap)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m && rsiValue.IsFormed && deviation < -DeviationPercent && rsiValue.GetValue<decimal>() < RsiOversold)
			BuyMarket(Volume);
		else if (Position == 0m && rsiValue.IsFormed && deviation > DeviationPercent && rsiValue.GetValue<decimal>() > RsiOverbought)
			SellMarket(Volume);
	}
}
