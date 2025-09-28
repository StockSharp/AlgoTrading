namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Implements the "Pause Trading On Consecutive Loss" risk control module.
/// The strategy enters simple momentum trades but halts new entries after a
/// configurable number of losing positions occur within a limited time window.
/// </summary>
public class PauseTradingOnConsecutiveLossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _consecutiveLosses;
	private readonly StrategyParam<int> _withinMinutes;
	private readonly StrategyParam<int> _pauseMinutes;

	private readonly Queue<DateTimeOffset> _lossTimes = new();

	private DateTimeOffset? _pauseUntil;
	private DateTimeOffset? _lastTradeTime;
	private decimal _lastRealizedPnL;
	private decimal? _previousClose;

	/// <summary>
	/// Candle aggregation type used for the momentum entries.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Order volume for both entries and exits.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Number of consecutive losses required to trigger the pause.
	/// </summary>
	public int ConsecutiveLosses
	{
		get => _consecutiveLosses.Value;
		set => _consecutiveLosses.Value = value;
	}

	/// <summary>
	/// Time window in minutes in which the losses must occur.
	/// </summary>
	public int WithinMinutes
	{
		get => _withinMinutes.Value;
		set => _withinMinutes.Value = value;
	}

	/// <summary>
	/// Duration of the trading pause in minutes.
	/// </summary>
	public int PauseMinutes
	{
		get => _pauseMinutes.Value;
		set => _pauseMinutes.Value = value;
	}

	public PauseTradingOnConsecutiveLossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for the momentum filter", "Data");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume for entries and exits", "Execution")
			.SetCanOptimize(true);

		_consecutiveLosses = Param(nameof(ConsecutiveLosses), 3)
			.SetDisplay("Consecutive Losses", "Losses required before pausing", "Risk Management")
			.SetCanOptimize(true);

		_withinMinutes = Param(nameof(WithinMinutes), 20)
			.SetDisplay("Within Minutes", "Window in minutes that contains the loss streak", "Risk Management")
			.SetCanOptimize(true);

		_pauseMinutes = Param(nameof(PauseMinutes), 20)
			.SetDisplay("Pause Minutes", "Duration of the cool-down after the streak", "Risk Management")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lossTimes.Clear();
		_pauseUntil = null;
		_lastTradeTime = null;
		_lastRealizedPnL = 0m;
		_previousClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_lastRealizedPnL = PnLManager?.RealizedPnL ?? 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var mainArea = CreateChartArea();
		if (mainArea != null)
		{
			DrawCandles(mainArea, subscription);
			DrawOwnTrades(mainArea);
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade myTrade)
	{
		base.OnOwnTradeReceived(myTrade);

		_lastTradeTime = myTrade.Trade?.ServerTime ?? myTrade.Trade?.Time ?? CurrentTime ?? DateTimeOffset.UtcNow;
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position != 0m)
		return;

		var realized = PnLManager?.RealizedPnL ?? _lastRealizedPnL;
		var result = realized - _lastRealizedPnL;
		_lastRealizedPnL = realized;

		if (result < 0m)
		{
			RegisterLoss(_lastTradeTime ?? CurrentTime ?? DateTimeOffset.UtcNow);
		}
		else
		{
			_lossTimes.Clear();
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var closeTime = candle.CloseTime;
		if (IsPauseActive(closeTime))
		return;

		if (_previousClose is decimal previousClose)
		{
			if (Position == 0m && !HasActiveOrders())
			{
			if (candle.ClosePrice > previousClose && AllowLong())
			{
				BuyMarket(OrderVolume);
			}
			else if (candle.ClosePrice < previousClose && AllowShort())
			{
				SellMarket(OrderVolume);
			}
			}
			else if (Position > 0m && candle.ClosePrice < candle.OpenPrice && !HasActiveOrders())
			{
			var volume = Math.Abs(Position);
			if (volume > 0m)
			SellMarket(volume);
			}
			else if (Position < 0m && candle.ClosePrice > candle.OpenPrice && !HasActiveOrders())
			{
			var volume = Math.Abs(Position);
			if (volume > 0m)
			BuyMarket(volume);
			}
		}

		_previousClose = candle.ClosePrice;
	}

	private bool IsPauseActive(DateTimeOffset time)
	{
		if (_pauseUntil is not DateTimeOffset until)
		return false;

		if (time >= until)
		{
		_pauseUntil = null;
		return false;
		}

		return true;
	}

	private void RegisterLoss(DateTimeOffset time)
	{
		var requiredLosses = ConsecutiveLosses;
		if (requiredLosses <= 0)
		return;

		_lossTimes.Enqueue(time);

		while (_lossTimes.Count > requiredLosses)
		_lossTimes.Dequeue();

		if (_lossTimes.Count < requiredLosses)
		return;

		var firstLossTime = _lossTimes.Peek();
		var window = time - firstLossTime;
		var limit = WithinMinutes > 0 ? TimeSpan.FromMinutes(WithinMinutes) : TimeSpan.Zero;

		if (limit != TimeSpan.Zero && window > limit)
		return;

		if (PauseMinutes <= 0)
		return;

		var until = time + TimeSpan.FromMinutes(PauseMinutes);
		if (_pauseUntil is null || until > _pauseUntil)
		{
		_pauseUntil = until;
		LogInfo($"Trading paused until {_pauseUntil:O} after {requiredLosses} consecutive losses within {WithinMinutes} minutes.");
		}
	}

	private bool HasActiveOrders()
	{
		foreach (var order in Orders)
		{
		if (order.State.IsActive())
		return true;
		}

		return false;
	}
}

