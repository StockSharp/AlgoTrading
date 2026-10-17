using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Enters on the last completed candle's direction; manages actual filled positions
/// through best bid/ask quotes with initial SL/TP, breakeven and monotonic trailing.
/// </summary>
public class LacustStopAndBeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStart;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _breakevenGain;
	private readonly StrategyParam<decimal> _breakeven;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal? _favorableExtreme;
	private decimal _trackedVolume;
	private Order _pendingOrder;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal TrailingStart { get => _trailingStart.Value; set => _trailingStart.Value = value; }
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public decimal BreakevenGain { get => _breakevenGain.Value; set => _breakevenGain.Value = value; }
	public decimal Breakeven { get => _breakeven.Value; set => _breakeven.Value = value; }

	public LacustStopAndBeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Signal candles", "General");
		_stopLoss = Param(nameof(StopLoss), 400m).SetNotNegative()
			.SetDisplay("Stop Loss", "Initial risk distance in price units", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 2000m).SetNotNegative()
			.SetDisplay("Take Profit", "Target distance in price units", "Risk");
		_trailingStart = Param(nameof(TrailingStart), 300m).SetNotNegative()
			.SetDisplay("Trailing Start", "Profit at which trailing starts", "Risk");
		_trailingStop = Param(nameof(TrailingStop), 200m).SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing distance from favorable quote", "Risk");
		_breakevenGain = Param(nameof(BreakevenGain), 250m).SetNotNegative()
			.SetDisplay("Breakeven Gain", "Profit at which breakeven activates", "Risk");
		_breakeven = Param(nameof(Breakeven), 100m).SetNotNegative()
			.SetDisplay("Breakeven", "Profit locked on breakeven", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ClearPositionTracking();
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ClearPositionTracking();
		_pendingOrder = null;
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var subscription = new Subscription(DataType.Level1, Security);
			subscription.MarketData.BuildField = field;
			SubscribeLevel1(subscription).Bind(ProcessQuote).Start();
		}
	}

	private void ClearPositionTracking()
	{
		_entryPrice = _stopPrice = _takePrice = _favorableExtreme = null;
		_trackedVolume = 0m;
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
			ClearPositionTracking();
			return;
		}

		if (position.AveragePrice is not decimal average || average <= 0m)
			return;

		if (_entryPrice == average && _trackedVolume == volume)
			return;

		// Retain an already tightened stop after a partial same-side reduction.
		if (_entryPrice == average && _trackedVolume * volume > 0m
			&& Math.Abs(volume) < Math.Abs(_trackedVolume))
		{
			_trackedVolume = volume;
			return;
		}

		_entryPrice = average;
		_trackedVolume = volume;
		_favorableExtreme = average;
		_stopPrice = StopLoss > 0m
			? volume > 0m ? average - StopLoss : average + StopLoss
			: null;
		_takePrice = TakeProfit > 0m
			? volume > 0m ? average + TakeProfit : average - TakeProfit
			: null;
	}

	private bool HasPendingOrder()
		=> _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed);

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || Position != 0m
			|| !IsFormedAndOnlineAndAllowTrading() || HasPendingOrder())
			return;

		if (candle.ClosePrice > candle.OpenPrice)
			_pendingOrder = BuyMarket(Volume);
		else if (candle.ClosePrice < candle.OpenPrice)
			_pendingOrder = SellMarket(Volume);
	}

	private void ProcessQuote(Level1ChangeMessage quote)
	{
		if (Position == 0m || _entryPrice is not decimal entry
			|| !IsFormedAndOnlineAndAllowTrading() || HasPendingOrder())
			return;

		var isLong = Position > 0m;
		var field = isLong ? Level1Fields.BestBidPrice : Level1Fields.BestAskPrice;
		if (!quote.Changes.TryGetValue(field, out var raw) || raw is not decimal price || price <= 0m)
			return;

		var best = _favorableExtreme ?? entry;
		_favorableExtreme = best = isLong ? Math.Max(best, price) : Math.Min(best, price);
		var gain = isLong ? best - entry : entry - best;

		if (gain >= BreakevenGain)
		{
			var candidate = isLong ? entry + Breakeven : entry - Breakeven;
			TightenStop(isLong, candidate);
		}
		if (gain >= TrailingStart)
		{
			var candidate = isLong ? best - TrailingStop : best + TrailingStop;
			TightenStop(isLong, candidate);
		}

		var hitStop = _stopPrice is decimal stop && (isLong ? price <= stop : price >= stop);
		var hitTake = _takePrice is decimal take && (isLong ? price >= take : price <= take);
		if (!hitStop && !hitTake)
			return;

		var exit = new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Type = OrderTypes.Market,
			Side = isLong ? Sides.Sell : Sides.Buy,
			Volume = Math.Abs(Position),
			Comment = hitStop ? "Lacust stop / breakeven / trailing" : "Lacust take profit",
		};
		_pendingOrder = exit;
		RegisterOrder(exit);
	}

	private void TightenStop(bool isLong, decimal candidate)
		=> _stopPrice = _stopPrice is decimal old
			? isLong ? Math.Max(old, candidate) : Math.Min(old, candidate)
			: candidate;
}
