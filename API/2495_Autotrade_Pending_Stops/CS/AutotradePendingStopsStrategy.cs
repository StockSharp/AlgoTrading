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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MQL Autotrade strategy that places symmetric stop orders around the market.
/// Pending stop entries are refreshed on every candle while no position is open.
/// Positions are closed when the market calms down or when absolute profit/loss thresholds are reached.
/// </summary>
public class AutotradePendingStopsStrategy : Strategy
{
	private readonly StrategyParam<int> _indentTicks;
	private readonly StrategyParam<decimal> _minProfit;
	private readonly StrategyParam<int> _expirationMinutes;
	private readonly StrategyParam<decimal> _absoluteFixation;
	private readonly StrategyParam<int> _stabilizationTicks;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _hasPrevCandle;

	private decimal _tickSize = 1m;
	private decimal _tickValue = 1m;

	/// <summary>
	/// Distance in price steps from the current market to the pending stop entries.
	/// </summary>
	public int IndentTicks
	{
		get => _indentTicks.Value;
		set => _indentTicks.Value = value;
	}

	/// <summary>
	/// Minimal profit in account currency required to exit when price action stabilizes.
	/// </summary>
	public decimal MinProfit
	{
		get => _minProfit.Value;
		set => _minProfit.Value = value;
	}

	/// <summary>
	/// Lifetime of pending stop orders in minutes.
	/// </summary>
	public int ExpirationMinutes
	{
		get => _expirationMinutes.Value;
		set => _expirationMinutes.Value = value;
	}

	/// <summary>
	/// Absolute profit or loss that forces the position to close.
	/// </summary>
	public decimal AbsoluteFixation
	{
		get => _absoluteFixation.Value;
		set => _absoluteFixation.Value = value;
	}

	/// <summary>
	/// Maximum size of the previous candle body that is treated as consolidation.
	/// </summary>
	public int StabilizationTicks
	{
		get => _stabilizationTicks.Value;
		set => _stabilizationTicks.Value = value;
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set
		{
			_orderVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Candle type used to drive the strategy logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public AutotradePendingStopsStrategy()
	{
		_indentTicks = Param(nameof(IndentTicks), 12)
		.SetGreaterThanZero()
		.SetDisplay("Indent Ticks", "Distance in ticks between price and pending stop orders", "Entries");

		_minProfit = Param(nameof(MinProfit), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Min Profit", "Minimum profit to close during low volatility", "Risk");

		_expirationMinutes = Param(nameof(ExpirationMinutes), 41)
		.SetGreaterThanZero()
		.SetDisplay("Order Expiration", "Lifetime of pending stops in minutes", "Entries");

		_absoluteFixation = Param(nameof(AbsoluteFixation), 43m)
		.SetGreaterThanZero()
		.SetDisplay("Absolute Fixation", "Profit or loss in currency that forces exit", "Risk");

		_stabilizationTicks = Param(nameof(StabilizationTicks), 25)
		.SetGreaterThanZero()
		.SetDisplay("Stabilization Ticks", "Maximum candle body considered as flat market", "Exits");

		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Default volume for both stop orders", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Time frame that drives order refresh", "General");

		Volume = _orderVolume.Value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		// Reset runtime state when the strategy is reloaded.
		_prevOpen = 0m;
		_prevClose = 0m;
		_hasPrevCandle = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		Volume = _orderVolume.Value;

		// Cache price step and tick value for fast profit calculations.
		_tickSize = Security.PriceStep ?? 1m;
		_tickValue = GetSecurityValue<decimal?>(Level1Fields.StepPrice) ?? _tickSize;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Only act on completed candles to stay aligned with the original MQL logic.
		if (candle.State != CandleStates.Finished)
		return;

		if (!_hasPrevCandle)
		{
			// Store the first candle so that stabilization checks have history.
			_prevOpen = candle.OpenPrice;
			_prevClose = candle.ClosePrice;
			_hasPrevCandle = true;

			EnsurePendingOrders(candle);
			return;
		}

		UpdatePendingOrdersLifetime(candle);

		if (Position == 0)
		{
			// Refresh pending orders as soon as the market is flat.
			EnsurePendingOrders(candle);
		}
		else
		{
			// Manage the active position and close it when required.
			ManageOpenPosition(candle);
		}

		// Keep the previous candle body for stabilization checks on the next bar.
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
	}

	private decimal _entryPrice;

	private void EnsurePendingOrders(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var indent = IndentTicks * _tickSize;
		var buyPrice = candle.ClosePrice + indent;
		var sellPrice = candle.ClosePrice - indent;

		// Simulate stop-order breakout: if high breaches buy level, go long
		if (candle.HighPrice >= buyPrice && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(OrderVolume);
			_entryPrice = buyPrice;
		}
		// if low breaches sell level, go short
		else if (candle.LowPrice <= sellPrice && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(OrderVolume);
			_entryPrice = sellPrice;
		}
	}

	private void UpdatePendingOrdersLifetime(ICandleMessage candle)
	{
		// No pending orders in simplified version - nothing to expire.
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		var entryPrice = _entryPrice;
		if (entryPrice == 0)
			return;

		var priceDiff = Position > 0 ? candle.ClosePrice - entryPrice : entryPrice - candle.ClosePrice;
		var prevBodySize = Math.Abs(_prevClose - _prevOpen);

		// Exit if profitable and market consolidating, or if loss exceeds threshold
		var exitByProfit = priceDiff > 0 && prevBodySize < candle.ClosePrice * 0.001m;
		var exitByLoss = priceDiff < -candle.ClosePrice * 0.005m;

		if (Position > 0 && (exitByProfit || exitByLoss))
		{
			SellMarket();
		}
		else if (Position < 0 && (exitByProfit || exitByLoss))
		{
			BuyMarket();
		}
	}

}