using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Exit-only manager for an existing position. It never submits an entry.
/// Initial risk and trailing activation are anchored to the reported average fill price;
/// executable-side Level1 quotes trigger the market exit, not an old candle's high/low.
/// </summary>
public class TrailingStopActivationStrategy : Strategy
{
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _entryPrice;
	private decimal? _extremePrice;
	private decimal? _stopPrice;
	private decimal _trackedVolume;
	private Order _pendingExit;

	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrailingStopActivationStrategy()
	{
		_trailingStop = Param(nameof(TrailingStop), 500m).SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Profit required to activate, and trailing distance in price units", "Risk");
		_stopLoss = Param(nameof(StopLoss), 0m).SetNotNegative()
			.SetDisplay("Initial Stop Loss", "Distance from actual entry price; zero disables it", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Optional chart candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Level1), (Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = _extremePrice = _stopPrice = null;
		_trackedVolume = 0m;
		_pendingExit = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var subscription = new Subscription(DataType.Level1, Security);
			subscription.MarketData.BuildField = field;
			SubscribeLevel1(subscription).Bind(ProcessQuote).Start();
		}
		// CandleType remains a configurable visualization stream; risk is quote driven.
		var candles = SubscribeCandles(CandleType);
		candles.Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candles);
			DrawOwnTrades(area);
		}
	}

	protected override void OnNewPosition(Position position)
	{
		base.OnNewPosition(position);
		ObservePosition(position);
	}

	protected override void OnPositionChanged(Position position)
	{
		base.OnPositionChanged(position);
		ObservePosition(position);
	}

	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);
		ObservePosition(position);
	}

	private void ObservePosition(Position position)
	{
		if (position.Security != Security || position.Portfolio != Portfolio)
			return;

		var volume = position.CurrentValue ?? 0m;
		if (volume == 0m)
		{
			_entryPrice = _extremePrice = _stopPrice = null;
			_trackedVolume = 0m;
			_pendingExit = null;
			return;
		}

		// A missing average fill is not a price quote: do not invent a stop anchor.
		var average = position.AveragePrice;
		if (average is not > 0m)
			return;

		if (_entryPrice == average && _trackedVolume == volume)
			return;

		// A partial reduction of the same-side position must never discard an
		// already tightened stop; only added exposure/new average reanchors.
		if (_entryPrice == average && Math.Sign(volume) == Math.Sign(_trackedVolume)
			&& Math.Abs(volume) < Math.Abs(_trackedVolume))
		{
			_trackedVolume = volume;
			return;
		}

		_entryPrice = average;
		_trackedVolume = volume;
		_extremePrice = average;
		_stopPrice = StopLoss > 0m
			? volume > 0m ? average - StopLoss : average + StopLoss
			: null;
	}

	private void ProcessQuote(Level1ChangeMessage quote)
	{
		if (Position == 0m || _entryPrice is not decimal entry || !IsFormedAndOnlineAndAllowTrading())
			return;
		if (_pendingExit is not null && _pendingExit.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		// A long can be liquidated only at the bid, a short only at the ask.
		var field = Position > 0m ? Level1Fields.BestBidPrice : Level1Fields.BestAskPrice;
		if (!quote.Changes.TryGetValue(field, out var raw) || raw is not decimal price || price <= 0m)
			return;

		var longPosition = Position > 0m;
		var extreme = _extremePrice ?? entry;
		_extremePrice = extreme = longPosition ? Math.Max(extreme, price) : Math.Min(extreme, price);

		// TrailingStop is both activation profit and the distance from the best executable quote.
		if (longPosition ? extreme - entry > TrailingStop : entry - extreme > TrailingStop)
		{
			var candidate = longPosition ? extreme - TrailingStop : extreme + TrailingStop;
			_stopPrice = _stopPrice is decimal old
				? longPosition ? Math.Max(old, candidate) : Math.Min(old, candidate)
				: candidate;
		}

		if (_stopPrice is not decimal stop || !(longPosition ? price <= stop : price >= stop))
			return;

		var exit = new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Type = OrderTypes.Market,
			Side = longPosition ? Sides.Sell : Sides.Buy,
			Volume = Math.Abs(Position),
			Comment = "Trailing/initial stop activation",
		};
		_pendingExit = exit;
		RegisterOrder(exit);
	}
}
