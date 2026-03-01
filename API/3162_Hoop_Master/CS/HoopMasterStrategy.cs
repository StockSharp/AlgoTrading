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
/// Ported version of the Hoop Master pending breakout strategy.
/// Places symmetric stop orders above and below the market with optional martingale sizing.
/// </summary>
public class HoopMasterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _indentPips;

	private decimal _pipSize;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal? _lastTradePrice;
	private decimal? _lastClosePrice;

	private decimal? _pendingBuyPrice;
	private decimal? _pendingSellPrice;
	private decimal _pendingVolume;

	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal? _entryPrice;
	private Sides? _positionSide;

	/// <summary>
	/// Initializes a new instance of <see cref="HoopMasterStrategy"/>.
	/// </summary>
	public HoopMasterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetDisplay("Order Volume", "Volume for entry orders", "Trading")
			
			.SetOptimize(0.1m, 5m, 0.1m);

		_stopLossPips = Param(nameof(StopLossPips), 25)
			.SetDisplay("Stop Loss (pips)", "Initial stop loss in pips", "Protection")
			.SetNotNegative()
			
			.SetOptimize(5, 80, 5);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetDisplay("Take Profit (pips)", "Initial take profit in pips", "Protection")
			.SetNotNegative()
			
			.SetOptimize(10, 120, 5);

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
			.SetDisplay("Trailing Stop (pips)", "Trailing distance in pips", "Protection")
			.SetNotNegative()
			
			.SetOptimize(0, 120, 5);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Minimal step to move trailing stop", "Protection")
			.SetNotNegative()
			
			.SetOptimize(1, 40, 1);

		_indentPips = Param(nameof(IndentPips), 15)
			.SetDisplay("Indent (pips)", "Distance for pending stops", "Trading")
			.SetNotNegative()
			
			.SetOptimize(1, 50, 1);
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base volume for the breakout orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Distance for trailing stop calculations.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimal movement required to advance the trailing stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Distance from the market to place stop orders.
	/// </summary>
	public int IndentPips
	{
		get => _indentPips.Value;
		set => _indentPips.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, CandleType),
			(Security, DataType.Level1)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBid = null;
		_bestAsk = null;
		_lastTradePrice = null;
		_lastClosePrice = null;

		_pendingBuyPrice = null;
		_pendingSellPrice = null;
		_pendingVolume = 0m;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_entryPrice = null;
		_positionSide = null;

		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio is not specified.");

		_pipSize = CalculatePipSize();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_bestBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_bestAsk = (decimal)ask;

		if (level1.Changes.TryGetValue(Level1Fields.LastTradePrice, out var last))
			_lastTradePrice = (decimal)last;

		UpdateTrailing();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastClosePrice = candle.ClosePrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Check protective stops/takes for active position
		if (Position > 0m)
		{
			if (_stopLossPrice is decimal sl && candle.LowPrice <= sl)
			{
				SellMarket(Math.Abs(Position));
				ResetPosition();
				return;
			}
			if (_takeProfitPrice is decimal tp && candle.HighPrice >= tp)
			{
				SellMarket(Math.Abs(Position));
				ResetPosition();
				return;
			}
		}
		else if (Position < 0m)
		{
			if (_stopLossPrice is decimal sl && candle.HighPrice >= sl)
			{
				BuyMarket(Math.Abs(Position));
				ResetPosition();
				return;
			}
			if (_takeProfitPrice is decimal tp && candle.LowPrice <= tp)
			{
				BuyMarket(Math.Abs(Position));
				ResetPosition();
				return;
			}
		}

		UpdateTrailing(candle.ClosePrice);

		// Check pending breakout entries
		if (_pendingBuyPrice is decimal buyTrigger && candle.HighPrice >= buyTrigger && Position <= 0m)
		{
			if (Position < 0m)
				BuyMarket(Math.Abs(Position));

			BuyMarket(_pendingVolume);
			HandleEntryFill(Sides.Buy, buyTrigger);
			return;
		}

		if (_pendingSellPrice is decimal sellTrigger && candle.LowPrice <= sellTrigger && Position >= 0m)
		{
			if (Position > 0m)
				SellMarket(Math.Abs(Position));

			SellMarket(_pendingVolume);
			HandleEntryFill(Sides.Sell, sellTrigger);
			return;
		}

		if (Position == 0m)
			SetPendingEntries(OrderVolume);
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
			ResetPosition();
	}

	private void ResetPosition()
	{
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_entryPrice = null;
		_positionSide = null;
	}

	private void HandleEntryFill(Sides side, decimal entryPrice)
	{
		_entryPrice = entryPrice;
		_positionSide = side;
		_pendingBuyPrice = null;
		_pendingSellPrice = null;

		SetProtection(side, entryPrice);

		var martingaleVolume = Math.Max(OrderVolume * 2m, OrderVolume);
		SetPendingEntries(martingaleVolume);
	}

	private void SetPendingEntries(decimal volume)
	{
		if (volume <= 0m)
			return;

		if (!TryGetMarketPrices(out var ask, out var bid))
			return;

		var indent = Math.Max(0, IndentPips) * _pipSize;

		var buyPriceBase = ask > 0m ? ask : bid;
		var sellPriceBase = bid > 0m ? bid : ask;

		_pendingBuyPrice = AlignPrice(buyPriceBase + indent);
		_pendingSellPrice = AlignPrice(sellPriceBase - indent);
		_pendingVolume = volume;
	}

	private void SetProtection(Sides side, decimal entryPrice)
	{
		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;

		if (stopDistance > 0m)
		{
			_stopLossPrice = side == Sides.Buy
				? AlignPrice(entryPrice - stopDistance)
				: AlignPrice(entryPrice + stopDistance);
		}
		else
		{
			_stopLossPrice = null;
		}

		if (takeDistance > 0m)
		{
			_takeProfitPrice = side == Sides.Buy
				? AlignPrice(entryPrice + takeDistance)
				: AlignPrice(entryPrice - takeDistance);
		}
		else
		{
			_takeProfitPrice = null;
		}
	}

	private void UpdateTrailing(decimal? fallbackPrice = null)
	{
		if (TrailingStopPips <= 0 || TrailingStepPips <= 0)
			return;

		if (Position == 0m)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (Position > 0m)
		{
			var price = GetLongMarketPrice(fallbackPrice);
			if (price == null)
				return;

			var desiredStop = AlignPrice(price.Value - trailingDistance);
			if (desiredStop <= 0m)
				return;

			if (_stopLossPrice is decimal current && desiredStop - current < trailingStep)
				return;

			MoveStop(Sides.Sell, desiredStop);
		}
		else if (Position < 0m)
		{
			var price = GetShortMarketPrice(fallbackPrice);
			if (price == null)
				return;

			var desiredStop = AlignPrice(price.Value + trailingDistance);
			if (desiredStop <= 0m)
				return;

			if (_stopLossPrice is decimal current && current - desiredStop < trailingStep)
				return;

			MoveStop(Sides.Buy, desiredStop);
		}
	}

	private decimal? GetLongMarketPrice(decimal? fallback)
	{
		if (_bestBid.HasValue && _bestBid.Value > 0m)
			return _bestBid.Value;

		if (fallback.HasValue && fallback.Value > 0m)
			return fallback.Value;

		if (_lastTradePrice.HasValue && _lastTradePrice.Value > 0m)
			return _lastTradePrice.Value;

		if (_lastClosePrice.HasValue && _lastClosePrice.Value > 0m)
			return _lastClosePrice.Value;

		return null;
	}

	private decimal? GetShortMarketPrice(decimal? fallback)
	{
		if (_bestAsk.HasValue && _bestAsk.Value > 0m)
			return _bestAsk.Value;

		if (fallback.HasValue && fallback.Value > 0m)
			return fallback.Value;

		if (_lastTradePrice.HasValue && _lastTradePrice.Value > 0m)
			return _lastTradePrice.Value;

		if (_lastClosePrice.HasValue && _lastClosePrice.Value > 0m)
			return _lastClosePrice.Value;

		return null;
	}

	private void MoveStop(Sides side, decimal price)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		_stopLossPrice = price;
	}

	private bool TryGetMarketPrices(out decimal ask, out decimal bid)
	{
		ask = _bestAsk ?? _lastTradePrice ?? _lastClosePrice ?? 0m;
		bid = _bestBid ?? _lastTradePrice ?? _lastClosePrice ?? 0m;

		if (ask <= 0m && bid <= 0m)
			return false;

		if (ask <= 0m)
			ask = bid;

		if (bid <= 0m)
			bid = ask;

		return ask > 0m && bid > 0m;
	}

	private decimal AlignPrice(decimal price)
	{
		var step = Security?.PriceStep;
		if (step == null || step.Value <= 0m)
			return price;

		var steps = Math.Round(price / step.Value, MidpointRounding.AwayFromZero);
		return steps * step.Value;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals ?? 0;

		return decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}

}

