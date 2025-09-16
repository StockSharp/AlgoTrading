namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Binario strategy converted from MetaTrader 5.
/// Places symmetrical stop entries around moving averages and manages trades with trailing stops.
/// </summary>
public class BinarioStrategy : Strategy
{
	private sealed record PendingSetup(decimal StopLoss, decimal TakeProfit, decimal EntryPrice);

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<string> _highMaMethod;
	private readonly StrategyParam<string> _lowMaMethod;
	private readonly StrategyParam<decimal> _pointValue;
	private readonly StrategyParam<decimal> _differencePips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;

	private IIndicator? _highMa;
	private IIndicator? _lowMa;
	private readonly List<decimal> _highMaHistory = new();
	private readonly List<decimal> _lowMaHistory = new();

	private decimal? _bestBid;
	private decimal? _bestAsk;

	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	private PendingSetup? _pendingLong;
	private PendingSetup? _pendingShort;

	private Order? _longStopOrder;
	private Order? _longTakeProfitOrder;
	private Order? _shortStopOrder;
	private Order? _shortTakeProfitOrder;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;

	private decimal _previousPosition;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average period for both envelopes.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Horizontal shift (in bars) applied to both moving averages.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Smoothing method for the upper moving average.
	/// </summary>
	public string HighMaMethod
	{
		get => _highMaMethod.Value;
		set => _highMaMethod.Value = value;
	}

	/// <summary>
	/// Smoothing method for the lower moving average.
	/// </summary>
	public string LowMaMethod
	{
		get => _lowMaMethod.Value;
		set => _lowMaMethod.Value = value;
	}

	/// <summary>
	/// Price offset that corresponds to one pip.
	/// </summary>
	public decimal PointValue
	{
		get => _pointValue.Value;
		set => _pointValue.Value = value;
	}

	/// <summary>
	/// Difference buffer between price and pending orders, in pips.
	/// </summary>
	public decimal DifferencePips
	{
		get => _differencePips.Value;
		set => _differencePips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step threshold in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public BinarioStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for candles", "General");

		_maPeriod = Param(nameof(MaPeriod), 144)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Moving average length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(50, 250, 10);

		_maShift = Param(nameof(MaShift), 0)
		.SetDisplay("MA Shift", "Horizontal shift in bars", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0, 5, 1);

		_highMaMethod = Param(nameof(HighMaMethod), "SMA")
		.SetDisplay("Upper MA Method", "Smoothing for the high MA", "Indicators");

		_lowMaMethod = Param(nameof(LowMaMethod), "SMA")
		.SetDisplay("Lower MA Method", "Smoothing for the low MA", "Indicators");

		_pointValue = Param(nameof(PointValue), 0.0001m)
		.SetGreaterThanZero()
		.SetDisplay("Point Value", "Price offset that corresponds to one pip", "Risk");

		_differencePips = Param(nameof(DifferencePips), 25m)
		.SetGreaterThanZero()
		.SetDisplay("Difference (pips)", "Extra distance added to entry", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5m, 50m, 5m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (pips)", "Distance to take profit", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(20m, 100m, 10m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 15m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5m, 50m, 5m);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetDisplay("Trailing Step (pips)", "Minimum progress before adjusting stop", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 20m, 1m);
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

		_highMa = null;
		_lowMa = null;
		_highMaHistory.Clear();
		_lowMaHistory.Clear();

		_bestBid = null;
		_bestAsk = null;

		_buyStopOrder = null;
		_sellStopOrder = null;
		_pendingLong = null;
		_pendingShort = null;

		_longStopOrder = null;
		_longTakeProfitOrder = null;
		_shortStopOrder = null;
		_shortTakeProfitOrder = null;

		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;

		_previousPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highMa = CreateMovingAverage(HighMaMethod, MaPeriod);
		_lowMa = CreateMovingAverage(LowMaMethod, MaPeriod);

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_bestBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_bestAsk = (decimal)ask;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_highMa is null || _lowMa is null)
			return;

		var highValue = _highMa.Process(candle.HighPrice, candle.ServerTime, true);
		var lowValue = _lowMa.Process(candle.LowPrice, candle.ServerTime, true);

		if (!highValue.IsFinal || !lowValue.IsFinal)
		{
			_previousPosition = Position;
			return;
		}

		if (!_highMa.IsFormed || !_lowMa.IsFormed)
		{
			_previousPosition = Position;
			return;
		}

		var maHigh = GetShiftedValue(_highMaHistory, highValue.ToDecimal(), MaShift);
		var maLow = GetShiftedValue(_lowMaHistory, lowValue.ToDecimal(), MaShift);

		if (maHigh is null || maLow is null)
		{
			_previousPosition = Position;
			return;
		}

		HandlePositionTransitions(candle.ClosePrice);
		UpdateTrailing(candle.ClosePrice);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousPosition = Position;
			return;
		}

		var spread = GetSpread();
		var point = PointValue;
		var difference = DifferencePips * point;
		var takeProfitOffset = TakeProfitPips * point;
		var trailingStopOffset = TrailingStopPips * point;
		var trailingStepOffset = TrailingStepPips * point;

		var buyPrice = maHigh.Value + spread + difference;
		var buyStopLoss = maLow.Value - point;
		var buyTakeProfit = maHigh.Value + difference + takeProfitOffset;

		var sellPrice = maLow.Value - difference;
		var sellStopLoss = maHigh.Value + spread + point;
		var sellTakeProfit = maLow.Value - spread - (difference + takeProfitOffset);

		var close = candle.ClosePrice;

		if (close < maHigh.Value && close > maLow.Value)
		{
			if (Position <= 0 && !IsOrderActive(_buyStopOrder))
			{
				SubmitBuyStop(buyPrice, buyStopLoss, buyTakeProfit);
			}

			if (Position >= 0 && !IsOrderActive(_sellStopOrder))
			{
				SubmitSellStop(sellPrice, sellStopLoss, sellTakeProfit);
			}
		}

		if (trailingStopOffset > 0m)
		{
			AdjustTrailingStops(close, trailingStopOffset, trailingStepOffset);
		}

		_previousPosition = Position;
	}

	private void SubmitBuyStop(decimal price, decimal stopLoss, decimal takeProfit)
	{
		var volume = Volume + (Position < 0 ? Math.Abs(Position) : 0m);

		if (volume <= 0)
			return;

		_buyStopOrder = BuyStop(volume, price);
		_pendingLong = new PendingSetup(stopLoss, takeProfit, price);
	}

	private void SubmitSellStop(decimal price, decimal stopLoss, decimal takeProfit)
	{
		var volume = Volume + (Position > 0 ? Math.Abs(Position) : 0m);

		if (volume <= 0)
			return;

		_sellStopOrder = SellStop(volume, price);
		_pendingShort = new PendingSetup(stopLoss, takeProfit, price);
	}

	private void HandlePositionTransitions(decimal referencePrice)
	{
		if (Position > 0)
		{
			if (_previousPosition < 0)
				OnExitShort();

			if (_previousPosition <= 0)
				OnEnterLong(referencePrice);
		}
		else if (Position < 0)
		{
			if (_previousPosition > 0)
				OnExitLong();

			if (_previousPosition >= 0)
				OnEnterShort(referencePrice);
		}
		else
		{
			if (_previousPosition > 0)
				OnExitLong();
			else if (_previousPosition < 0)
				OnExitShort();
		}
	}

	private void OnEnterLong(decimal referencePrice)
	{
		CancelActiveOrder(_sellStopOrder);
		_sellStopOrder = null;
		_buyStopOrder = null;

		if (_pendingLong is null)
		{
			_pendingLong = new PendingSetup(referencePrice - PointValue, referencePrice + TakeProfitPips * PointValue, referencePrice);
		}

		_longEntryPrice = _pendingLong.EntryPrice;
		_longStopPrice = _pendingLong.StopLoss;

		RegisterLongProtection();

		_pendingLong = null;
	}

	private void OnEnterShort(decimal referencePrice)
	{
		CancelActiveOrder(_buyStopOrder);
		_buyStopOrder = null;
		_sellStopOrder = null;

		if (_pendingShort is null)
		{
			_pendingShort = new PendingSetup(referencePrice + PointValue, referencePrice - TakeProfitPips * PointValue, referencePrice);
		}

		_shortEntryPrice = _pendingShort.EntryPrice;
		_shortStopPrice = _pendingShort.StopLoss;

		RegisterShortProtection();

		_pendingShort = null;
	}

	private void OnExitLong()
	{
		CancelActiveOrder(_longStopOrder);
		CancelActiveOrder(_longTakeProfitOrder);
		_longStopOrder = null;
		_longTakeProfitOrder = null;
		_longEntryPrice = null;
		_longStopPrice = null;
	}

	private void OnExitShort()
	{
		CancelActiveOrder(_shortStopOrder);
		CancelActiveOrder(_shortTakeProfitOrder);
		_shortStopOrder = null;
		_shortTakeProfitOrder = null;
		_shortEntryPrice = null;
		_shortStopPrice = null;
	}

	private void RegisterLongProtection()
	{
		if (_longEntryPrice is null || _longStopPrice is null || _pendingLong is null)
			return;

		var volume = Math.Abs(Position);

		if (volume <= 0)
			return;

		CancelActiveOrder(_longStopOrder);
		CancelActiveOrder(_longTakeProfitOrder);

		_longStopOrder = SellStop(volume, _pendingLong.StopLoss);
		_longTakeProfitOrder = SellLimit(volume, _pendingLong.TakeProfit);
	}

	private void RegisterShortProtection()
	{
		if (_shortEntryPrice is null || _shortStopPrice is null || _pendingShort is null)
			return;

		var volume = Math.Abs(Position);

		if (volume <= 0)
			return;

		CancelActiveOrder(_shortStopOrder);
		CancelActiveOrder(_shortTakeProfitOrder);

		_shortStopOrder = BuyStop(volume, _pendingShort.StopLoss);
		_shortTakeProfitOrder = BuyLimit(volume, _pendingShort.TakeProfit);
	}

	private void UpdateTrailing(decimal referencePrice)
	{
		if (Position > 0)
		{
			if (_longEntryPrice is null && _pendingLong is not null)
				_longEntryPrice = _pendingLong.EntryPrice;
		}
		else if (Position < 0)
		{
			if (_shortEntryPrice is null && _pendingShort is not null)
				_shortEntryPrice = _pendingShort.EntryPrice;
		}
	}

	private void AdjustTrailingStops(decimal closePrice, decimal trailingStopOffset, decimal trailingStepOffset)
	{
		if (Position > 0 && _longEntryPrice is decimal entry)
		{
			var profit = closePrice - entry;
			if (profit > trailingStopOffset + trailingStepOffset)
			{
				var candidateStop = closePrice - trailingStopOffset;
				if (_longStopPrice is null || candidateStop >= _longStopPrice.Value + trailingStepOffset)
				{
					_longStopPrice = candidateStop;
					ReplaceOrder(ref _longStopOrder, false, candidateStop);
				}
			}
		}
		else if (Position < 0 && _shortEntryPrice is decimal entryShort)
		{
			var profit = entryShort - closePrice;
			if (profit > trailingStopOffset + trailingStepOffset)
			{
				var candidateStop = closePrice + trailingStopOffset;
				if (_shortStopPrice is null || candidateStop <= _shortStopPrice.Value - trailingStepOffset)
				{
					_shortStopPrice = candidateStop;
					ReplaceOrder(ref _shortStopOrder, true, candidateStop);
				}
			}
		}
	}

	private void ReplaceOrder(ref Order? order, bool isBuyStop, decimal newPrice)
	{
		var volume = Math.Abs(Position);

		if (volume <= 0)
			return;

		CancelActiveOrder(order);

		order = isBuyStop
			? BuyStop(volume, newPrice)
			: SellStop(volume, newPrice);
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

	private decimal? GetShiftedValue(List<decimal> buffer, decimal current, int shift)
	{
		buffer.Add(current);

		var required = shift + 1;
		if (buffer.Count < required)
			return null;

		if (buffer.Count > required)
			buffer.RemoveAt(0);

		return buffer[^1 - shift];
	}

	private decimal GetSpread()
	{
		if (_bestBid is decimal bid && _bestAsk is decimal ask && ask > 0m && bid > 0m && ask >= bid)
			return ask - bid;

		var step = Security.MinPriceStep ?? 0m;
		if (step > 0m)
			return step;

		return PointValue;
	}

	private static IIndicator CreateMovingAverage(string method, int length)
	{
		return method.ToUpperInvariant() switch
		{
			"EMA" => new ExponentialMovingAverage { Length = length },
			"SMMA" or "RMA" => new ModifiedMovingAverage { Length = length },
			"WMA" or "LWMA" => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}
}
