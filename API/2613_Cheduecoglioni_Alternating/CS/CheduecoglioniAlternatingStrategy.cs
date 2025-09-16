using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alternates buy and sell market orders with fixed stop loss and take profit distances.
/// </summary>
public class CheduecoglioniAlternatingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private Sides _nextSide;
	private Sides? _activeSide;
	private bool _waitingForEntry;
	private Order _entryOrder;

	/// <summary>
	/// Initializes a new instance of the <see cref="CheduecoglioniAlternatingStrategy"/> class.
	/// </summary>
	public CheduecoglioniAlternatingStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Volume per trade", "General")
			.SetGreaterThanZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 10m)
			.SetDisplay("Take Profit (pips)", "Distance to take profit", "Risk")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 10m)
			.SetDisplay("Stop Loss (pips)", "Distance to stop loss", "Risk")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Source candles for timing", "General");
	}

	/// <summary>
	/// Volume used for each market order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Candle type that triggers trading decisions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		Volume = TradeVolume;
		_nextSide = Sides.Sell; // Start with a short position like the original expert advisor.
		_activeSide = null;
		_waitingForEntry = false;
		_entryOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume; // Align the base volume with the strategy parameter.

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		{
			var decimals = Security?.Decimals ?? 4;
			priceStep = (decimal)Math.Pow(10, -decimals);
		}

		_pipSize = priceStep;

		var secDecimals = Security?.Decimals;
		if (secDecimals is int digits && (digits == 3 || digits == 5))
		{
			_pipSize *= 10m; // Convert from fractional pip to full pip for FX symbols.
		}

		if (_pipSize <= 0m)
		{
			_pipSize = 1m; // Fallback to a neutral value if the instrument metadata is missing.
		}

		StartProtection(
			stopLoss: new Unit(StopLossPips * _pipSize, UnitTypes.Absolute),
			takeProfit: new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute),
			useMarketOrders: true);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0 || _waitingForEntry)
			return; // Skip if a position exists or a market order is still pending.

		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		Order order = _nextSide == Sides.Buy
			? BuyMarket(volume)
			: SellMarket(volume);

		if (order == null)
			return;

		_entryOrder = order;
		_waitingForEntry = true; // Prevent duplicate submissions until the order is filled or rejected.
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0)
		{
			_activeSide = Sides.Buy;
			_waitingForEntry = false;
			_entryOrder = null;
			return;
		}

		if (Position < 0)
		{
			_activeSide = Sides.Sell;
			_waitingForEntry = false;
			_entryOrder = null;
			return;
		}

		if (_activeSide.HasValue)
		{
			_nextSide = _activeSide == Sides.Buy ? Sides.Sell : Sides.Buy; // Alternate direction after a flat position.
			_activeSide = null;
		}

		_waitingForEntry = false;
		_entryOrder = null;
	}

	/// <inheritdoc />
	protected override void OnOrderFailed(Order order)
	{
		base.OnOrderFailed(order);

		if (order != _entryOrder)
			return;

		_waitingForEntry = false; // Allow the next candle to retry when the broker rejected the order.
		_entryOrder = null;
	}
}
