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

public class PrecipiceStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossTakeProfitPips;
	private readonly StrategyParam<bool> _useBuy;
	private readonly StrategyParam<bool> _useSell;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Random _random = new(Environment.TickCount);
	private decimal _pipSize;
	private Order _stopOrder;
	private Order _takeProfitOrder;

	public PrecipiceStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade volume", "Default order volume used for entries.", "Trading")
			.SetGreaterThanZero();

		_stopLossTakeProfitPips = Param(nameof(StopLossTakeProfitPips), 100)
			.SetDisplay("TP/SL distance (pips)", "Distance between the entry price and protective orders expressed in MetaTrader pips.", "Risk")
			.SetNotNegative();

		_useBuy = Param(nameof(UseBuy), true)
			.SetDisplay("Enable buy", "Allow the strategy to open long positions.", "Signals");

		_useSell = Param(nameof(UseSell), true)
			.SetDisplay("Enable sell", "Allow the strategy to open short positions.", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe processed by the strategy.", "Data");
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public int StopLossTakeProfitPips
	{
		get => _stopLossTakeProfitPips.Value;
		set => _stopLossTakeProfitPips.Value = value;
	}

	public bool UseBuy
	{
		get => _useBuy.Value;
		set => _useBuy.Value = value;
	}

	public bool UseSell
	{
		get => _useSell.Value;
		set => _useSell.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_stopOrder = null;
		_takeProfitOrder = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume; // Align helper methods with the configured lot size.

		var security = Security ?? throw new InvalidOperationException("Security is not set.");

		var priceStep = security.PriceStep ?? 0.0001m;
		if (priceStep <= 0m)
			priceStep = 0.0001m;

		var decimals = security.Decimals;
		_pipSize = decimals == 3 || decimals == 5 ? priceStep * 10m : priceStep;
		if (_pipSize <= 0m)
			_pipSize = priceStep;
		if (_pipSize <= 0m)
			_pipSize = 0.0001m; // Fallback to a tiny increment when the security lacks metadata.

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return; // Wait for fully closed candles to reproduce MetaTrader behaviour.

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m)
			return; // Trade only when no position exists.

		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		var stopDistance = CalculateStopDistance();

		if (UseBuy && _random.NextDouble() < 0.5)
		{
			TryExecuteEntry(true, candle.ClosePrice, volume, stopDistance);
			return;
		}

		if (UseSell && _random.NextDouble() > 0.5)
			TryExecuteEntry(false, candle.ClosePrice, volume, stopDistance);
	}

	private decimal CalculateStopDistance()
	{
		var pips = StopLossTakeProfitPips;

		if (pips <= 0 || _pipSize <= 0m)
			return 0m;

		return pips * _pipSize;
	}

	private void TryExecuteEntry(bool isBuy, decimal price, decimal volume, decimal stopDistance)
	{
		if (price <= 0m)
			return; // Skip entries when candle data is not valid.

		if (isBuy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		AttachProtection(isBuy, price, volume, stopDistance);
	}

	private void AttachProtection(bool isBuy, decimal price, decimal volume, decimal stopDistance)
	{
		CancelProtection();

		if (stopDistance <= 0m)
			return; // No protective orders requested.

		var slPrice = isBuy ? price - stopDistance : price + stopDistance;
		var tpPrice = isBuy ? price + stopDistance : price - stopDistance;

		if (slPrice > 0m)
			_stopOrder = isBuy ? SellStop(volume, slPrice) : BuyStop(volume, slPrice);

		if (tpPrice > 0m)
			_takeProfitOrder = isBuy ? SellLimit(volume, tpPrice) : BuyLimit(volume, tpPrice);
	}

	private void CancelProtection()
	{
		if (_stopOrder != null)
		{
			if (_stopOrder.State == OrderStates.Active)
				CancelOrder(_stopOrder); // Cancel the active stop-loss order before replacing it.

			_stopOrder = null;
		}

		if (_takeProfitOrder != null)
		{
			if (_takeProfitOrder.State == OrderStates.Active)
				CancelOrder(_takeProfitOrder); // Cancel the active take-profit order before replacing it.

			_takeProfitOrder = null;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
			CancelProtection(); // Remove protective orders once the position is fully closed.
	}
}

