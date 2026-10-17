using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// One initial long entry on the first completed candle, followed by two-phase
/// breakeven/trailing stop management from the reported average execution price.
/// Manually attached short positions use symmetric exits, never automated short entries.
/// Distances are absolute price units, not percentages or price-step counts.
/// </summary>
public class BreakevenTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _breakevenPlus;
	private readonly StrategyParam<decimal> _breakevenStep;
	private readonly StrategyParam<decimal> _trailingPlus;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<DataType> _candleType;

	private bool _initialEntryAttempted;
	private bool _breakevenReached;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal _observedVolume;
	private Order _pendingExit;

	public decimal BreakevenPlus { get => _breakevenPlus.Value; set => _breakevenPlus.Value = value; }
	public decimal BreakevenStep { get => _breakevenStep.Value; set => _breakevenStep.Value = value; }
	public decimal TrailingPlus { get => _trailingPlus.Value; set => _trailingPlus.Value = value; }
	public decimal TrailingStep { get => _trailingStep.Value; set => _trailingStep.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BreakevenTrailingStopStrategy()
	{
		_breakevenPlus = Param(nameof(BreakevenPlus), 5m).SetGreaterThanZero()
			.SetDisplay("Breakeven Plus", "Profit distance before breakeven in price units", "Risk");
		_breakevenStep = Param(nameof(BreakevenStep), 2m).SetNotNegative()
			.SetDisplay("Breakeven Step", "Stop offset past actual fill in price units", "Risk");
		_trailingPlus = Param(nameof(TrailingPlus), 3m).SetGreaterThanZero()
			.SetDisplay("Trailing Plus", "Price distance above current stop before trailing", "Risk");
		_trailingStep = Param(nameof(TrailingStep), 1m).SetGreaterThanZero()
			.SetDisplay("Trailing Step", "Price offset of ratcheted trailing stop", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe of entry candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_initialEntryAttempted = false;
		_breakevenReached = false;
		_entryPrice = null;
		_stopPrice = null;
		_observedVolume = 0m;
		_pendingExit = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetState();
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ProcessQuote).Start();
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _initialEntryAttempted)
			return;
		// This demonstration enters at most once. An attached short position is left alone.
		_initialEntryAttempted = true;
		if (Position == 0m && IsFormedAndOnlineAndAllowTrading())
			BuyMarket(Volume);
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
			_entryPrice = _stopPrice = null;
			_observedVolume = 0m;
			_breakevenReached = false;
			_pendingExit = null;
			return;
		}
		if (position.AveragePrice is not decimal average || average <= 0m)
			return;
		if (average == _entryPrice && volume == _observedVolume)
			return;
		// A partial exit does not undo an already protected profit level.
		if (average == _entryPrice && Math.Sign(volume) == Math.Sign(_observedVolume)
			&& Math.Abs(volume) < Math.Abs(_observedVolume))
		{
			_observedVolume = volume;
			return;
		}
		_entryPrice = average;
		_observedVolume = volume;
		_stopPrice = null;
		_breakevenReached = false;
	}

	private void ProcessQuote(Level1ChangeMessage quote)
	{
		if (Position == 0m || _entryPrice is not decimal entry || !IsFormedAndOnlineAndAllowTrading())
			return;
		if (_pendingExit is not null && _pendingExit.State is not (OrderStates.Done or OrderStates.Failed))
			return;

		var isLong = Position > 0m;
		var side = isLong ? Level1Fields.BestBidPrice : Level1Fields.BestAskPrice;
		if (!quote.Changes.TryGetValue(side, out var raw) || raw is not decimal price || price <= 0m)
			return;

		if (!_breakevenReached)
		{
			if (isLong ? price >= entry + BreakevenPlus : price <= entry - BreakevenPlus)
			{
				_stopPrice = isLong ? entry + BreakevenStep : entry - BreakevenStep;
				_breakevenReached = true;
			}
		}
		else if (_stopPrice is decimal previous)
		{
			// Advance the stop using the current executable quote, never a previous candle wick.
			if (isLong ? price >= previous + TrailingPlus : price <= previous - TrailingPlus)
			{
				var candidate = isLong ? price - TrailingStep : price + TrailingStep;
				_stopPrice = isLong ? Math.Max(previous, candidate) : Math.Min(previous, candidate);
			}
		}

		if (_stopPrice is not decimal stop || !(isLong ? price <= stop : price >= stop))
			return;

		var exit = new Order
		{
			Security = Security, Portfolio = Portfolio, Type = OrderTypes.Market,
			Side = isLong ? Sides.Sell : Sides.Buy,
			Volume = Math.Abs(Position), Comment = "Breakeven/trailing stop",
		};
		_pendingExit = exit;
		RegisterOrder(exit);
	}
}
