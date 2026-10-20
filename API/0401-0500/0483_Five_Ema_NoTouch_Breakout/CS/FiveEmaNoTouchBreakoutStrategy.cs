namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Finished no-touch EMA setups with native protection fixed from actual entry risk.</summary>
public class FiveEmaNoTouchBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _rewardRisk;
	private Order _pending;
	private ExponentialMovingAverage _ema;
	private (decimal high, decimal low, Sides side)? _setup;
	private decimal _setupStop;
	private bool _isProtectionArmed;

	/// <summary>Finished signal candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>EMA length.</summary>
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	/// <summary>Take-profit distance divided by initial risk.</summary>
	public decimal RewardRisk { get => _rewardRisk.Value; set => _rewardRisk.Value = value; }

	/// <summary>Initialize the strategy parameters.</summary>
	public FiveEmaNoTouchBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("CandleType", "Finished signal candle type.", "Parameters");
		_emaPeriod = Param(nameof(EmaPeriod), 5).SetRange(1, int.MaxValue)
			.SetDisplay("EmaPeriod", "EMA length.", "Parameters");
		_rewardRisk = Param(nameof(RewardRisk), 3m).SetRange(0.000001m, decimal.MaxValue)
			.SetDisplay("RewardRisk", "Take-profit distance divided by initial risk.", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_pending = null;
		_ema = null;
		_setup = null;
		_setupStop = 0m;
		_isProtectionArmed = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{

		base.OnStarted2(time);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quote = new Subscription(DataType.Level1, Security);
			quote.MarketData.BuildField = field;
			SubscribeLevel1(quote).Start();
		}
		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		SubscribeCandles(CandleType).Bind(_ema, ProcessCandle).Start();
	}

	private bool IsPending => _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed);

	private void Submit(Sides side, decimal quantity, string comment)
	{
		_pending = CreateOrder(side, 0m, quantity);
		_pending.Type = OrderTypes.Market;
		_pending.Comment = comment;
		RegisterOrder(_pending);
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished || !_ema.IsFormed || !IsFormedAndOnlineAndAllowTrading() || IsPending) return;
		if (Position != 0m)
		{
			_setup = null;
			return;
		}
		// Test the previous finished setup before a new candle can replace it.
		if (_setup is { } setup && ((setup.side == Sides.Buy && candle.HighPrice > setup.high && candle.ClosePrice > setup.low)
			|| (setup.side == Sides.Sell && candle.LowPrice < setup.low && candle.ClosePrice < setup.high)))
		{
			_setupStop = setup.side == Sides.Buy ? setup.low : setup.high;
			_isProtectionArmed = false;
			_setup = null;
			Submit(setup.side, Volume, "0483Entry");
			return;
		}
		if (candle.HighPrice < ema) _setup = (candle.HighPrice, candle.LowPrice, Sides.Buy);
		else if (candle.LowPrice > ema) _setup = (candle.HighPrice, candle.LowPrice, Sides.Sell);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		if (trade.Order.Comment != "0483Entry" || _isProtectionArmed) return;
		_isProtectionArmed = true;
		var risk = trade.Order.Side == Sides.Buy ? trade.Trade.Price - _setupStop : _setupStop - trade.Trade.Price;
		if (risk <= 0m)
		{
			Submit(trade.Order.Side == Sides.Buy ? Sides.Sell : Sides.Buy, Math.Abs(Position), "0483InvalidFillExit");
			return;
		}
		StartProtection(new Unit(risk * RewardRisk, UnitTypes.Absolute), new Unit(risk, UnitTypes.Absolute),
			useMarketOrders: true, isLocalStop: true);
	}

}
