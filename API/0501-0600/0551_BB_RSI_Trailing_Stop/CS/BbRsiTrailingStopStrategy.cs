namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Bollinger excursions with RSI recovery and fill-anchored conditional quote trailing.</summary>
public class BbRsiTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailOffsetPoints;
	private readonly StrategyParam<decimal> _trailStopPoints;
	private readonly StrategyParam<DataType> _candleType;
	private Order _pending;
	private decimal? _previousRsi;
	private decimal _entryCost, _entryQuantity, _extreme, _stop;
	private decimal _initialDistance, _activationDistance, _trailingDistance;
	private bool _trailing;

	/// <summary>Initializes the published signal and risk parameters.</summary>
	public BbRsiTrailingStopStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 25).SetGreaterThanZero().SetOptimize(10, 50, 5)
			.SetDisplay("BollingerPeriod", "Bollinger close window", "Parameters");
		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m).SetGreaterThanZero().SetOptimize(1m, 3m, 0.5m)
			.SetDisplay("BollingerDeviation", "Bollinger standard deviation multiplier", "Parameters");
		_rsiPeriod = Param(nameof(RsiPeriod), 14).SetGreaterThanZero().SetOptimize(7, 21, 7)
			.SetDisplay("RsiPeriod", "Wilder RSI window", "Parameters");
		_rsiOverbought = Param(nameof(RsiOverbought), 60m).SetGreaterThanZero().SetOptimize(50m, 80m, 5m)
			.SetDisplay("RsiOverbought", "RSI upper recovery threshold", "Parameters");
		_rsiOversold = Param(nameof(RsiOversold), 33m).SetGreaterThanZero().SetOptimize(20m, 40m, 5m)
			.SetDisplay("RsiOversold", "RSI lower recovery threshold", "Parameters");
		_stopLossPoints = Param(nameof(StopLossPoints), 50m).SetGreaterThanZero().SetOptimize(20m, 100m, 10m)
			.SetDisplay("StopLossPoints", "Initial stop distance in price units", "Parameters");
		_trailOffsetPoints = Param(nameof(TrailOffsetPoints), 99m).SetGreaterThanZero().SetOptimize(50m, 150m, 10m)
			.SetDisplay("TrailOffsetPoints", "Favorable price movement required to activate trailing", "Parameters");
		_trailStopPoints = Param(nameof(TrailStopPoints), 40m).SetGreaterThanZero().SetOptimize(20m, 80m, 10m)
			.SetDisplay("TrailStopPoints", "Trailing distance in price units", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("CandleType", "Finished signal candle interval", "Parameters");
	}

	/// <summary>Bollinger close window.</summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>Bollinger standard deviation multiplier.</summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>Wilder RSI window.</summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>RSI upper recovery threshold.</summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>RSI lower recovery threshold.</summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>Initial stop distance in price units.</summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>Favorable price movement required to activate trailing.</summary>
	public decimal TrailOffsetPoints
	{
		get => _trailOffsetPoints.Value;
		set => _trailOffsetPoints.Value = value;
	}

	/// <summary>Trailing distance in price units.</summary>
	public decimal TrailStopPoints
	{
		get => _trailStopPoints.Value;
		set => _trailStopPoints.Value = value;
	}

	/// <summary>Finished signal candle interval.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private bool IsPending => _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed);

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_pending = null;
		_previousRsi = null;
		_entryCost = _entryQuantity = _extreme = _stop = 0m;
		_initialDistance = _activationDistance = _trailingDistance = 0m;
		_trailing = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		if (RsiOversold >= RsiOverbought || RsiOverbought >= 100m)
			throw new InvalidOperationException("RSI thresholds must satisfy 0 < oversold < overbought < 100.");
		base.OnStarted2(time);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quote = new Subscription(DataType.Level1, Security);
			quote.MarketData.BuildField = field;
			SubscribeLevel1(quote).Bind(ProcessQuote).Start();
		}
		var bb = new BollingerBands { Length = BollingerPeriod, Width = BollingerDeviation };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var candles = SubscribeCandles(CandleType);
		candles.BindEx([bb, rsi], ProcessCandle).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, candles);
			DrawIndicator(area, bb);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void Submit(Sides side, decimal quantity, string reason)
	{
		_pending = CreateOrder(side, 0m, quantity);
		_pending.Type = OrderTypes.Market;
		_pending.Comment = "0551" + reason;
		RegisterOrder(_pending);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished || !values[0].IsFormed || !values[1].IsFormed
			|| values[0] is not BollingerBandsValue bb || bb.LowBand is not decimal lower || bb.UpBand is not decimal upper)
			return;
		var rsi = values[1].GetValue<decimal>();
		var previous = _previousRsi;
		_previousRsi = rsi;
		if (previous is null || Position != 0m || IsPending || !IsFormedAndOnlineAndAllowTrading())
			return;
		Sides? side = candle.LowPrice < lower && previous <= RsiOversold && rsi > RsiOversold ? Sides.Buy
			: candle.HighPrice > upper && previous >= RsiOverbought && rsi < RsiOverbought ? Sides.Sell : null;
		if (side is null)
			return;
		_entryCost = _entryQuantity = 0m;
		_trailing = false;
		_initialDistance = StopLossPoints;
		_activationDistance = TrailOffsetPoints;
		_trailingDistance = TrailStopPoints;
		Submit(side.Value, Volume, "Entry");
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		if (trade.Order.Comment == "0551Entry")
		{
			_entryCost += trade.Trade.Price * trade.Trade.Volume;
			_entryQuantity += trade.Trade.Volume;
			var entry = _entryCost / _entryQuantity;
			_extreme = entry;
			_stop = trade.Order.Side == Sides.Buy ? entry - _initialDistance : entry + _initialDistance;
		}
	}

	private void ProcessQuote(Level1ChangeMessage quote)
	{
		if (Position == 0m || _entryQuantity == 0m || IsPending || !IsFormedAndOnlineAndAllowTrading())
			return;
		var field = Position > 0m ? Level1Fields.BestBidPrice : Level1Fields.BestAskPrice;
		if (!quote.Changes.TryGetValue(field, out var raw) || raw is not decimal price || price <= 0m)
			return;
		var entry = _entryCost / _entryQuantity;
		_extreme = Position > 0m ? Math.Max(_extreme, price) : Math.Min(_extreme, price);
		if (Position > 0m ? _extreme - entry >= _activationDistance : entry - _extreme >= _activationDistance)
			_trailing = true;
		if (_trailing)
			_stop = Position > 0m ? Math.Max(_stop, _extreme - _trailingDistance) : Math.Min(_stop, _extreme + _trailingDistance);
		if (Position > 0m ? price <= _stop : price >= _stop)
			Submit(Position > 0m ? Sides.Sell : Sides.Buy, Math.Abs(Position), "Stop");
	}
}
