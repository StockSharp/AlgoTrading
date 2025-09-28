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

using StockSharp.Algo;

/// <summary>
/// Port of the MetaTrader expert advisor "TRAYLERv" that maintains fractal-based trailing stops
/// and optional take-profit levels for existing positions.
/// </summary>
public class TraylerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _deleteAllPendingOrders;
	private readonly StrategyParam<bool> _deleteOwnPendingOrders;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<bool> _enableSound;
	private readonly StrategyParam<bool> _showCommentary;
	private readonly StrategyParam<int> _stopFractalDepth;
	private readonly StrategyParam<int> _takeProfitFractalDepth;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<CandleSnapshot> _history = new();
	private readonly List<(int Index, decimal Price)> _upperFractals = new();
	private readonly List<(int Index, decimal Price)> _lowerFractals = new();
	private readonly List<Order> _ordersBuffer = new();

	private Order _stopOrder;
	private Order _takeProfitOrder;
	private int _lastIndex;
	private decimal _priceStep;
	private decimal _pipSize;
	private decimal _minPriceIncrement;

	/// <summary>
	/// Initializes a new instance of the <see cref="TraylerStrategy"/> class.
	/// </summary>
	public TraylerStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order volume", "Default order volume used for manual trades.", "Trading");

		_deleteAllPendingOrders = Param(nameof(DeleteAllPendingOrders), false)
			.SetDisplay("Delete all pendings", "Cancel every active pending order on each update.", "Orders");

		_deleteOwnPendingOrders = Param(nameof(DeleteOwnPendingOrders), false)
			.SetDisplay("Delete symbol pendings", "Cancel pending orders for the current symbol only.", "Orders");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use take profit", "Enable fractal-based take-profit management.", "Protection");

		_enableSound = Param(nameof(EnableSound), true)
			.SetDisplay("Enable sound", "Keeps the original expert option for notification sounds.", "Legacy");

		_showCommentary = Param(nameof(ShowCommentary), true)
			.SetDisplay("Show commentary", "Preserves the original informational display toggle.", "Legacy");

		_stopFractalDepth = Param(nameof(StopFractalDepth), 7)
			.SetGreaterThanZero()
			.SetDisplay("Stop fractal depth", "Bars to scan for trailing stop fractals.", "Fractals");

		_takeProfitFractalDepth = Param(nameof(TakeProfitFractalDepth), 21)
			.SetGreaterThanZero()
			.SetDisplay("Take-profit fractal depth", "Bars to scan for profit target fractals.", "Fractals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe used for fractal analysis.", "Data");
	}

	/// <summary>
	/// Default volume suggested for manual interactions.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Cancel all pending orders, regardless of symbol, on every candle update.
	/// </summary>
	public bool DeleteAllPendingOrders
	{
		get => _deleteAllPendingOrders.Value;
		set => _deleteAllPendingOrders.Value = value;
	}

	/// <summary>
	/// Cancel pending orders that belong to the current symbol only.
	/// </summary>
	public bool DeleteOwnPendingOrders
	{
		get => _deleteOwnPendingOrders.Value;
		set => _deleteOwnPendingOrders.Value = value;
	}

	/// <summary>
	/// Enable fractal-based take-profit placement and adjustments.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Preserve the legacy flag from the original expert advisor.
	/// </summary>
	public bool EnableSound
	{
		get => _enableSound.Value;
		set => _enableSound.Value = value;
	}

	/// <summary>
	/// Preserve the legacy comment toggle from the original expert advisor.
	/// </summary>
	public bool ShowCommentary
	{
		get => _showCommentary.Value;
		set => _showCommentary.Value = value;
	}

	/// <summary>
	/// Number of finished bars to examine when searching for stop-loss fractals.
	/// </summary>
	public int StopFractalDepth
	{
		get => _stopFractalDepth.Value;
		set => _stopFractalDepth.Value = value;
	}

	/// <summary>
	/// Number of finished bars to examine when searching for take-profit fractals.
	/// </summary>
	public int TakeProfitFractalDepth
	{
		get => _takeProfitFractalDepth.Value;
		set => _takeProfitFractalDepth.Value = value;
	}

	/// <summary>
	/// Data type used to build the primary candle series.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;
		if (security != null)
			yield return (security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_history.Clear();
		_upperFractals.Clear();
		_lowerFractals.Clear();
		_ordersBuffer.Clear();
		_stopOrder = null;
		_takeProfitOrder = null;
		_lastIndex = 0;
		_priceStep = 0m;
		_pipSize = 0m;
		_minPriceIncrement = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = GetPriceStep();
		_pipSize = GetPipSize();
		_minPriceIncrement = _priceStep > 0m ? _priceStep : (_pipSize > 0m ? _pipSize : 0.0001m);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
			EnsureProtectionCleared();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		AppendCandle(candle);

		if (DeleteAllPendingOrders || DeleteOwnPendingOrders)
			CancelPendingOrders();

		if (Position == 0m)
		{
			EnsureProtectionCleared();
			return;
		}

		if (!IsEvenMinute(candle.OpenTime))
			return;

		if (Position > 0m)
			ManageLongPosition(candle);
		else
			ManageShortPosition(candle);
	}

	private void AppendCandle(ICandleMessage candle)
	{
		_lastIndex++;
		var snapshot = new CandleSnapshot(_lastIndex, candle.HighPrice, candle.LowPrice, candle.ClosePrice);
		_history.Add(snapshot);

		if (_history.Count >= 5)
		{
			var center = _history.Count - 3;
			if (center >= 2 && center + 2 < _history.Count)
				RegisterFractals(center);
		}

		if (_history.Count > 200)
			_history.RemoveAt(0);
	}

	private void RegisterFractals(int center)
	{
		var left2 = _history[center - 2];
		var left1 = _history[center - 1];
		var mid = _history[center];
		var right1 = _history[center + 1];
		var right2 = _history[center + 2];

		if (mid.High > left2.High && mid.High > left1.High && mid.High > right1.High && mid.High > right2.High)
			_upperFractals.Add((mid.Index, mid.High));

		if (mid.Low < left2.Low && mid.Low < left1.Low && mid.Low < right1.Low && mid.Low < right2.Low)
			_lowerFractals.Add((mid.Index, mid.Low));

		TrimFractals();
	}

	private void TrimFractals()
	{
		var maxDepth = Math.Max(StopFractalDepth, TakeProfitFractalDepth) + 10;
		var minIndex = _lastIndex - maxDepth;

		for (var i = _upperFractals.Count - 1; i >= 0; i--)
		{
			if (_upperFractals[i].Index < minIndex)
				_upperFractals.RemoveAt(i);
		}

		for (var i = _lowerFractals.Count - 1; i >= 0; i--)
		{
			if (_lowerFractals[i].Index < minIndex)
				_lowerFractals.RemoveAt(i);
		}
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		volume = NormalizeVolume(volume);
		var close = candle.ClosePrice;

		if (UseTakeProfit)
		{
			var profit = close - PositionPrice;
			if (profit > 0m)
			{
				var fractal = GetFractalPrice(_upperFractals, TakeProfitFractalDepth);
				if (fractal is decimal upper)
				{
					var target = upper - GetSpreadBuffer();
					if (target > close)
						UpdateTakeProfit(true, target, volume);
				}
			}
		}
		else
		{
			CancelProtectiveOrder(ref _takeProfitOrder);
		}

		var stopCandidate = GetFractalPrice(_lowerFractals, StopFractalDepth);
		decimal? stopPrice = null;

		if (stopCandidate is decimal lower)
			stopPrice = lower - (GetSpreadBuffer() + 2m * _minPriceIncrement);
		else
		{
			var fallback = GetLowAtShift(3);
			if (fallback is decimal lowFallback)
				stopPrice = lowFallback - 2m * _minPriceIncrement;
		}

		if (stopPrice is decimal stop && stop > 0m)
			UpdateStop(true, stop, volume);
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		volume = NormalizeVolume(volume);
		var close = candle.ClosePrice;

		if (UseTakeProfit)
		{
			var profit = PositionPrice - close;
			if (profit > 0m)
			{
				var fractal = GetFractalPrice(_lowerFractals, TakeProfitFractalDepth);
				if (fractal is decimal lower)
				{
					var target = lower - GetSpreadBuffer();
					if (target < close)
						UpdateTakeProfit(false, target, volume);
				}
			}
		}
		else
		{
			CancelProtectiveOrder(ref _takeProfitOrder);
		}

		var stopCandidate = GetFractalPrice(_upperFractals, StopFractalDepth);
		decimal? stopPrice = null;

		if (stopCandidate is decimal upper)
			stopPrice = upper + (GetSpreadBuffer() + 2m * _minPriceIncrement);
		else
		{
			var fallback = GetHighAtShift(3);
			if (fallback is decimal highFallback)
				stopPrice = highFallback + 2m * _minPriceIncrement;
		}

		if (stopPrice is decimal stop && stop > 0m)
			UpdateStop(false, stop, volume);
	}

	private void UpdateStop(bool isLong, decimal price, decimal volume)
	{
		var normalizedPrice = NormalizePrice(price);
		if (normalizedPrice <= 0m || volume <= 0m)
			return;

		if (_stopOrder != null)
		{
			var difference = normalizedPrice - _stopOrder.Price;
			if (isLong)
			{
				if (difference <= _minPriceIncrement / 2m)
					return;
			}
			else
			{
				if (-difference <= _minPriceIncrement / 2m)
					return;
			}

			if (_stopOrder.State == OrderStates.Active || _stopOrder.State == OrderStates.Pending)
				CancelOrder(_stopOrder);
		}

		_stopOrder = isLong
			? SellStop(volume, normalizedPrice)
			: BuyStop(volume, normalizedPrice);
	}

	private void UpdateTakeProfit(bool isLong, decimal price, decimal volume)
	{
		var normalizedPrice = NormalizePrice(price);
		if (normalizedPrice <= 0m || volume <= 0m)
			return;

		if (_takeProfitOrder != null)
		{
			var difference = normalizedPrice - _takeProfitOrder.Price;
			if (isLong)
			{
				if (difference <= _minPriceIncrement / 2m)
					return;
				if (normalizedPrice <= _takeProfitOrder.Price)
					return;
			}
			else
			{
				if (-difference <= _minPriceIncrement / 2m)
					return;
				if (normalizedPrice >= _takeProfitOrder.Price)
					return;
			}

			if (_takeProfitOrder.State == OrderStates.Active || _takeProfitOrder.State == OrderStates.Pending)
				CancelOrder(_takeProfitOrder);
		}

		_takeProfitOrder = isLong
			? SellLimit(volume, normalizedPrice)
			: BuyLimit(volume, normalizedPrice);
	}

	private void CancelPendingOrders()
	{
		_ordersBuffer.Clear();

		foreach (var order in Orders)
		{
			if (order.State != OrderStates.Active && order.State != OrderStates.Pending)
				continue;

			if (!IsPending(order))
				continue;

			if (DeleteAllPendingOrders || Equals(order.Security, Security))
				_ordersBuffer.Add(order);
		}

		for (var i = 0; i < _ordersBuffer.Count; i++)
			CancelOrder(_ordersBuffer[i]);
	}

	private static bool IsPending(Order order)
	{
		return order.Type == OrderTypes.Limit || order.Type == OrderTypes.Stop || order.Type == OrderTypes.StopLimit;
	}

	private void EnsureProtectionCleared()
	{
		CancelProtectiveOrder(ref _stopOrder);
		CancelProtectiveOrder(ref _takeProfitOrder);
	}

	private void CancelProtectiveOrder(ref Order order)
	{
		if (order == null)
			return;

		if (order.State == OrderStates.Active || order.State == OrderStates.Pending)
			CancelOrder(order);

		order = null;
	}

	private decimal? GetFractalPrice(List<(int Index, decimal Price)> source, int depth)
	{
		if (source.Count == 0)
			return null;

		var maxShift = depth + 1;

		for (var i = source.Count - 1; i >= 0; i--)
		{
			var shift = _lastIndex - source[i].Index;
			if (shift < 2)
				continue;

			if (shift <= maxShift)
				return source[i].Price;

			if (shift > maxShift)
				break;
		}

		return null;
	}

	private decimal? GetLowAtShift(int shift)
	{
		var index = _history.Count - 1 - shift;
		if (index >= 0 && index < _history.Count)
			return _history[index].Low;

		return null;
	}

	private decimal? GetHighAtShift(int shift)
	{
		var index = _history.Count - 1 - shift;
		if (index >= 0 && index < _history.Count)
			return _history[index].High;

		return null;
	}

	private static bool IsEvenMinute(DateTimeOffset time)
	{
		return time.Minute % 2 == 0;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (_minPriceIncrement <= 0m)
			return price;

		var steps = Math.Round(price / _minPriceIncrement, MidpointRounding.AwayFromZero);
		return steps * _minPriceIncrement;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Round(volume / step, MidpointRounding.AwayFromZero);
			volume = steps * step;
		}

		var min = security.MinVolume;
		if (min != null && volume < min.Value)
			volume = min.Value;

		var max = security.MaxVolume;
		if (max != null && volume > max.Value)
			volume = max.Value;

		return volume;
	}

	private decimal GetSpreadBuffer()
	{
		var security = Security;
		if (security == null)
			return _minPriceIncrement;

		var ask = security.BestAsk?.Price;
		var bid = security.BestBid?.Price;

		if (ask is decimal a && bid is decimal b && a > b)
			return a - b;

		return _minPriceIncrement;
	}

	private decimal GetPriceStep()
	{
		var security = Security;
		if (security == null)
			return 0m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value > 0)
				step = (decimal)Math.Pow(10, -decimals.Value);
		}

		return step;
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value > 0)
				step = (decimal)Math.Pow(10, -decimals.Value);
		}

		if (step <= 0m)
			step = 0.0001m;

		var digits = security.Decimals;
		var multiplier = 1m;
		if (digits != null && (digits.Value == 3 || digits.Value == 5))
			multiplier = 10m;

		return step * multiplier;
	}

	private sealed class CandleSnapshot
	{
		public CandleSnapshot(int index, decimal high, decimal low, decimal close)
		{
			Index = index;
			High = high;
			Low = low;
			Close = close;
		}

		public int Index { get; }
		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
	}
}

