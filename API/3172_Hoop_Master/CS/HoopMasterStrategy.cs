using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pending stop breakout strategy converted from the "Hoop master 2" MetaTrader 5 expert advisor.
/// </summary>
public class HoopMasterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _indentPips;
	private readonly StrategyParam<decimal> _lossMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	private Order? _longStopOrder;
	private Order? _longTakeProfitOrder;
	private Order? _shortStopOrder;
	private Order? _shortTakeProfitOrder;

	private decimal? _pendingLongStop;
	private decimal? _pendingLongTake;
	private decimal? _pendingShortStop;
	private decimal? _pendingShortTake;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;

	private decimal _pipSize;
	private decimal _currentVolume;
	private decimal _roundTripPnL;
	private decimal _previousPosition;

	/// <summary>
	/// Stop loss distance in MetaTrader pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in MetaTrader pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop offset in MetaTrader pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimal trailing step in MetaTrader pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Offset for pending stop orders in MetaTrader pips.
	/// </summary>
	public decimal IndentPips
	{
		get => _indentPips.Value;
		set => _indentPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the base volume after a losing trade.
	/// </summary>
	public decimal LossMultiplier
	{
		get => _lossMultiplier.Value;
		set => _lossMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type used to trigger re-arming logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HoopMasterStrategy"/> class.
	/// </summary>
	public HoopMasterStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 25m)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance from entry", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 70m)
			.SetDisplay("Take Profit (pips)", "Take profit distance from entry", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0m)
			.SetDisplay("Trailing Stop (pips)", "Distance used for trailing stop", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step (pips)", "Minimal improvement before trailing", "Risk");

		_indentPips = Param(nameof(IndentPips), 15m)
			.SetDisplay("Indent (pips)", "Offset applied to pending stop orders", "Entries");

		_lossMultiplier = Param(nameof(LossMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Loss Multiplier", "Volume multiplier after losses", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for re-arming logic", "General");
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

		_buyStopOrder = null;
		_sellStopOrder = null;
		_longStopOrder = null;
		_longTakeProfitOrder = null;
		_shortStopOrder = null;
		_shortTakeProfitOrder = null;

		_pendingLongStop = null;
		_pendingLongTake = null;
		_pendingShortStop = null;
		_pendingShortTake = null;

		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;

		_pipSize = 0m;
		_roundTripPnL = 0m;
		_previousPosition = 0m;

		_currentVolume = Volume > 0m ? Volume : 1m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPipSize();
		if (_pipSize <= 0m)
			_pipSize = Security?.PriceStep ?? 0.0001m;

		_currentVolume = Volume > 0m ? Volume : 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateTrailing(candle.ClosePrice);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			CancelActiveOrder(_sellStopOrder);
			_sellStopOrder = null;
		}
		else if (Position < 0)
		{
			CancelActiveOrder(_buyStopOrder);
			_buyStopOrder = null;
		}

		if (Position != 0)
			return;

		var indent = ConvertPips(IndentPips);
		var step = _pipSize > 0m ? _pipSize : Security?.PriceStep ?? 0.0001m;

		if (!IsOrderActive(_buyStopOrder))
		{
			var activation = NormalizePrice(candle.ClosePrice + Math.Max(indent, step));
			var stopLoss = StopLossPips > 0m ? NormalizePrice(activation - ConvertPips(StopLossPips)) : (decimal?)null;
			var takeProfit = TakeProfitPips > 0m ? NormalizePrice(activation + ConvertPips(TakeProfitPips)) : (decimal?)null;

			SubmitBuyStop(activation, stopLoss, takeProfit);
		}

		if (!IsOrderActive(_sellStopOrder))
		{
			var activation = NormalizePrice(candle.ClosePrice - Math.Max(indent, step));
			var stopLoss = StopLossPips > 0m ? NormalizePrice(activation + ConvertPips(StopLossPips)) : (decimal?)null;
			var takeProfit = TakeProfitPips > 0m ? NormalizePrice(activation - ConvertPips(TakeProfitPips)) : (decimal?)null;

			SubmitSellStop(activation, stopLoss, takeProfit);
		}
	}

	private void SubmitBuyStop(decimal price, decimal? stopLoss, decimal? takeProfit)
	{
		CancelActiveOrder(_buyStopOrder);

		var volume = _currentVolume;
		if (volume <= 0m)
			volume = 1m;

		_buyStopOrder = BuyStop(volume, price);
		_pendingLongStop = stopLoss;
		_pendingLongTake = takeProfit;
	}

	private void SubmitSellStop(decimal price, decimal? stopLoss, decimal? takeProfit)
	{
		CancelActiveOrder(_sellStopOrder);

		var volume = _currentVolume;
		if (volume <= 0m)
			volume = 1m;

		_sellStopOrder = SellStop(volume, price);
		_pendingShortStop = stopLoss;
		_pendingShortTake = takeProfit;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		var current = Position;

		if (current > 0m && _previousPosition <= 0m)
		{
			OnEnterLong();
		}
		else if (current < 0m && _previousPosition >= 0m)
		{
			OnEnterShort();
		}
		else if (current == 0m && _previousPosition != 0m)
		{
			OnExitPosition();
		}

		_previousPosition = current;
	}

	private void OnEnterLong()
	{
		CancelActiveOrder(_sellStopOrder);
		_sellStopOrder = null;

		_longEntryPrice = _buyStopOrder?.Price ?? Security?.LastTrade?.Price;
		_longStopPrice = _pendingLongStop;

		RegisterLongProtection();
	}

	private void OnEnterShort()
	{
		CancelActiveOrder(_buyStopOrder);
		_buyStopOrder = null;

		_shortEntryPrice = _sellStopOrder?.Price ?? Security?.LastTrade?.Price;
		_shortStopPrice = _pendingShortStop;

		RegisterShortProtection();
	}

	private void OnExitPosition()
	{
		CancelActiveOrder(_longStopOrder);
		CancelActiveOrder(_longTakeProfitOrder);
		CancelActiveOrder(_shortStopOrder);
		CancelActiveOrder(_shortTakeProfitOrder);

		_longStopOrder = null;
		_longTakeProfitOrder = null;
		_shortStopOrder = null;
		_shortTakeProfitOrder = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;

		if (_roundTripPnL < 0m)
		{
			_currentVolume *= LossMultiplier;
		}
		else
		{
			_currentVolume = Volume > 0m ? Volume : 1m;
		}

		_roundTripPnL = 0m;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		_roundTripPnL += trade.PnL;

		if (trade.Order == _buyStopOrder)
		{
			_longEntryPrice = trade.Trade.Price;
			_longStopPrice = _pendingLongStop;

			RegisterLongProtection();
		}
		else if (trade.Order == _sellStopOrder)
		{
			_shortEntryPrice = trade.Trade.Price;
			_shortStopPrice = _pendingShortStop;

			RegisterShortProtection();
		}
		else if (trade.Order == _longStopOrder)
		{
			CancelActiveOrder(_longTakeProfitOrder);
			_longTakeProfitOrder = null;
		}
		else if (trade.Order == _shortStopOrder)
		{
			CancelActiveOrder(_shortTakeProfitOrder);
			_shortTakeProfitOrder = null;
		}

		if (Position == 0m)
		{
			OnExitPosition();
		}
	}

	private void RegisterLongProtection()
	{
		if (_pendingLongStop is null && _pendingLongTake is null)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		CancelActiveOrder(_longStopOrder);
		CancelActiveOrder(_longTakeProfitOrder);

		if (_pendingLongStop is decimal stop)
			_longStopOrder = SellStop(volume, stop);

		if (_pendingLongTake is decimal take)
			_longTakeProfitOrder = SellLimit(volume, take);
	}

	private void RegisterShortProtection()
	{
		if (_pendingShortStop is null && _pendingShortTake is null)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		CancelActiveOrder(_shortStopOrder);
		CancelActiveOrder(_shortTakeProfitOrder);

		if (_pendingShortStop is decimal stop)
			_shortStopOrder = BuyStop(volume, stop);

		if (_pendingShortTake is decimal take)
			_shortTakeProfitOrder = BuyLimit(volume, take);
	}

	private void UpdateTrailing(decimal closePrice)
	{
		if (TrailingStopPips <= 0m)
			return;

		var trailingOffset = ConvertPips(TrailingStopPips);
		var trailingStep = Math.Max(ConvertPips(TrailingStepPips), 0m);

		if (Position > 0 && _longEntryPrice is decimal entry)
		{
			var candidate = NormalizePrice(closePrice - trailingOffset);
			if (candidate <= entry)
				return;

			if (_longStopPrice is null || candidate >= _longStopPrice.Value + trailingStep)
			{
				_longStopPrice = candidate;
				ReplaceOrder(ref _longStopOrder, false, candidate);
			}
		}
		else if (Position < 0 && _shortEntryPrice is decimal entryShort)
		{
			var candidate = NormalizePrice(closePrice + trailingOffset);
			if (candidate >= entryShort)
				return;

			if (_shortStopPrice is null || candidate <= _shortStopPrice.Value - trailingStep)
			{
				_shortStopPrice = candidate;
				ReplaceOrder(ref _shortStopOrder, true, candidate);
			}
		}
	}

	private void ReplaceOrder(ref Order? order, bool isBuyStop, decimal price)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		CancelActiveOrder(order);

		order = isBuyStop
			? BuyStop(volume, price)
			: SellStop(volume, price);
	}

	private decimal ConvertPips(decimal pips)
	{
		return pips > 0m ? pips * _pipSize : 0m;
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return price;

		var steps = decimal.Round(price / step, 0, MidpointRounding.AwayFromZero);
		return steps * step;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;

		var temp = step;
		var decimals = 0;
		while (temp != Math.Truncate(temp) && decimals < 10)
		{
			temp *= 10m;
			decimals++;
		}

		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}

	private static bool IsOrderActive(Order? order)
	{
		return order is not null && (order.State == OrderStates.Active || order.State == OrderStates.Pending);
	}

	private void CancelActiveOrder(Order? order)
	{
		if (order is not null && (order.State == OrderStates.Active || order.State == OrderStates.Pending))
			CancelOrder(order);
	}
}
