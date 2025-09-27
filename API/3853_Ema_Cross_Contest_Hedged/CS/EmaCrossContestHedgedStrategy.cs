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
/// EMA crossover strategy with hedged pending orders converted from the MQL "EMA_CROSS_CONTEST_HEDGED" expert.
/// </summary>
public class EmaCrossContestHedgedStrategy : Strategy
{

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _hedgeLevelPips;
	private readonly StrategyParam<bool> _useCloseOnOpposite;
	private readonly StrategyParam<bool> _useMacdFilter;
	private readonly StrategyParam<int> _pendingExpirationSeconds;
	private readonly StrategyParam<int> _shortEmaLength;
	private readonly StrategyParam<int> _longEmaLength;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<int> _signalBarShift;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _pendingOrderCount;

	private decimal? _shortEmaCurrent;
	private decimal? _shortEmaPrevious;
	private decimal? _longEmaCurrent;
	private decimal? _longEmaPrevious;
	private decimal? _macdLinePrevious;

	private decimal _positionVolume;
	private decimal _averagePrice;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	private readonly List<PendingOrder> _pendingOrders = new();

	private sealed class PendingOrder
	{
		public Sides Side { get; init; }
		public decimal Price { get; init; }
		public decimal? StopLoss { get; init; }
		public decimal? TakeProfit { get; init; }
		public DateTimeOffset Expiration { get; init; }
	}

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public int HedgeLevelPips
	{
		get => _hedgeLevelPips.Value;
		set => _hedgeLevelPips.Value = value;
	}

	public bool UseCloseOnOpposite
	{
		get => _useCloseOnOpposite.Value;
		set => _useCloseOnOpposite.Value = value;
	}

	public bool UseMacdFilter
	{
		get => _useMacdFilter.Value;
		set => _useMacdFilter.Value = value;
	}

	public int PendingExpirationSeconds
	{
		get => _pendingExpirationSeconds.Value;
		set => _pendingExpirationSeconds.Value = value;
	}

	public int ShortEmaLength
	{
		get => _shortEmaLength.Value;
		set => _shortEmaLength.Value = value;
	}

	public int LongEmaLength
	{
		get => _longEmaLength.Value;
		set => _longEmaLength.Value = value;
	}
	/// <summary>
	/// Fast MACD EMA length.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}
	/// <summary>
	/// Slow MACD EMA length.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}
	/// <summary>
	/// MACD signal line smoothing length.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	public int SignalBarShift
	{
		get => _signalBarShift.Value;
		set => _signalBarShift.Value = value;
	}
	/// <summary>
	/// Number of layered pending hedge orders per side.
	/// </summary>
	public int PendingOrderCount
	{
		get => _pendingOrderCount.Value;
		set => _pendingOrderCount.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public EmaCrossContestHedgedStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetDisplay("Order Volume", "Order size", "Trading")
		.SetGreaterThanZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 150)
		.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 150)
		.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 40)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_hedgeLevelPips = Param(nameof(HedgeLevelPips), 6)
		.SetDisplay("Hedge Level (pips)", "Distance between pending hedges", "Orders");

		_useCloseOnOpposite = Param(nameof(UseCloseOnOpposite), true)
		.SetDisplay("Use Close", "Close position on opposite crossover", "Trading");

		_useMacdFilter = Param(nameof(UseMacdFilter), true)
		.SetDisplay("Use MACD", "Require MACD confirmation", "Filters");

		_pendingExpirationSeconds = Param(nameof(PendingExpirationSeconds), 7200)
		.SetDisplay("Expiration (s)", "Pending order lifetime in seconds", "Orders");
		_pendingOrderCount = Param(nameof(PendingOrderCount), 4)
			.SetDisplay("Pending Orders", "Number of layered pending hedges per side", "Orders")
			.SetGreaterThanZero();

		_shortEmaLength = Param(nameof(ShortEmaLength), 4)
		.SetGreaterThanZero()
		.SetDisplay("Short EMA", "Fast EMA length", "Indicators");
		_macdFastLength = Param(nameof(MacdFastLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length used inside MACD", "Indicators");
		_macdSlowLength = Param(nameof(MacdSlowLength), 24)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length used inside MACD", "Indicators");
		_macdSignalLength = Param(nameof(MacdSignalLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line smoothing length", "Indicators");

		_longEmaLength = Param(nameof(LongEmaLength), 24)
		.SetGreaterThanZero()
		.SetDisplay("Long EMA", "Slow EMA length", "Indicators");

		_signalBarShift = Param(nameof(SignalBarShift), 1)
		.SetDisplay("Signal Bar", "0=current bar, 1=previous", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for calculations", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_shortEmaCurrent = null;
		_shortEmaPrevious = null;
		_longEmaCurrent = null;
		_longEmaPrevious = null;
		_macdLinePrevious = null;

		_positionVolume = 0m;
		_averagePrice = 0m;
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_pendingOrders.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (ShortEmaLength >= LongEmaLength)
			throw new InvalidOperationException("Short EMA length must be less than the long EMA length.");

		Volume = OrderVolume;

		var shortEma = new EMA { Length = ShortEmaLength };
		var longEma = new EMA { Length = LongEmaLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength }
			},
			SignalMa = { Length = MacdSignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(shortEma, longEma, macd, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortEma);
			DrawIndicator(area, longEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue shortValue, IIndicatorValue longValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!shortValue.IsFinal || !longValue.IsFinal)
			return;

		var emaShort = shortValue.ToDecimal();
		var emaLong = longValue.ToDecimal();

		decimal? macdLine = null;
		if (macdValue.IsFinal)
		{
			var macdSignalValue = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
			if (macdSignalValue.Macd is decimal macdDecimal)
				macdLine = macdDecimal;
		}

		ProcessPendingOrders(candle);

		var cross = DetectCross(emaShort, emaLong);
		var macdFilter = UseMacdFilter ? GetMacdFilter(macdLine) : 0m;

		var canTrade = IsFormedAndOnlineAndAllowTrading();

		if (_positionVolume > 0m)
		{
			if (UseCloseOnOpposite && cross == -1)
			{
				ExitLong();
				UpdateHistory(emaShort, emaLong, macdLine);
				return;
			}

			if (CheckLongStops(candle))
			{
				UpdateHistory(emaShort, emaLong, macdLine);
				return;
			}
		}
		else if (_positionVolume < 0m)
		{
			if (UseCloseOnOpposite && cross == 1)
			{
				ExitShort();
				UpdateHistory(emaShort, emaLong, macdLine);
				return;
			}

			if (CheckShortStops(candle))
			{
				UpdateHistory(emaShort, emaLong, macdLine);
				return;
			}
		}

		if (canTrade && _positionVolume == 0m)
		{
			if (cross == 1 && (!UseMacdFilter || macdFilter >= 0m))
			{
				EnterLong(candle.ClosePrice, candle.CloseTime);
				UpdateHistory(emaShort, emaLong, macdLine);
				return;
			}

			if (cross == -1 && (!UseMacdFilter || macdFilter <= 0m))
			{
				EnterShort(candle.ClosePrice, candle.CloseTime);
				UpdateHistory(emaShort, emaLong, macdLine);
				return;
			}
		}

		UpdateHistory(emaShort, emaLong, macdLine);
	}

	private decimal GetMacdFilter(decimal? macdLine)
	{
		if (SignalBarShift == 0)
			return macdLine ?? decimal.MinValue;

		return _macdLinePrevious ?? decimal.MinValue;
	}

	private void ProcessPendingOrders(ICandleMessage candle)
	{
		if (_pendingOrders.Count == 0)
			return;

		var time = candle.CloseTime;

		for (var i = _pendingOrders.Count - 1; i >= 0; i--)
		{
			var pending = _pendingOrders[i];

			if (pending.Expiration <= time)
			{
				_pendingOrders.RemoveAt(i);
				continue;
			}

			var triggered = pending.Side == Sides.Buy
			? candle.HighPrice >= pending.Price
			: candle.LowPrice <= pending.Price;

			if (!triggered)
			continue;

			_pendingOrders.RemoveAt(i);

			if (pending.Side == Sides.Buy)
			{
				BuyMarket(OrderVolume);
				RegisterLongEntry(pending.Price, OrderVolume, pending.StopLoss, pending.TakeProfit);
			}
			else
			{
				SellMarket(OrderVolume);
				RegisterShortEntry(pending.Price, OrderVolume, pending.StopLoss, pending.TakeProfit);
			}
		}
	}

	private void EnterLong(decimal price, DateTimeOffset time)
	{
		BuyMarket(OrderVolume);

		RegisterLongEntry(price, OrderVolume,
			StopLossPips > 0 ? price - PipToPrice(StopLossPips) : null,
			TakeProfitPips > 0 ? price + PipToPrice(TakeProfitPips) : null);

		_shortStopPrice = null;
		_shortTakePrice = null;
		_shortTrailingStop = null;

		CreatePendingOrders(time, price, Sides.Buy);
	}

	private void EnterShort(decimal price, DateTimeOffset time)
	{
		SellMarket(OrderVolume);

		RegisterShortEntry(price, OrderVolume,
			StopLossPips > 0 ? price + PipToPrice(StopLossPips) : null,
			TakeProfitPips > 0 ? price - PipToPrice(TakeProfitPips) : null);

		_longStopPrice = null;
		_longTakePrice = null;
		_longTrailingStop = null;

		CreatePendingOrders(time, price, Sides.Sell);
	}

	private void RegisterLongEntry(decimal price, decimal volume, decimal? stop, decimal? take)
	{
		var previousVolume = _positionVolume;
		_positionVolume += volume;

		if (previousVolume <= 0m)
			_averagePrice = price;
		else
			_averagePrice = ((previousVolume * _averagePrice) + (volume * price)) / _positionVolume;

		if (stop.HasValue)
			_longStopPrice = _longStopPrice.HasValue ? Math.Max(_longStopPrice.Value, stop.Value) : stop;

		if (take.HasValue)
			_longTakePrice = _longTakePrice.HasValue ? Math.Max(_longTakePrice.Value, take.Value) : take;

		_longTrailingStop = null;
	}

	private void RegisterShortEntry(decimal price, decimal volume, decimal? stop, decimal? take)
	{
		var previousVolume = _positionVolume;
		_positionVolume -= volume;

		if (previousVolume >= 0m)
			_averagePrice = price;
		else
			_averagePrice = ((Math.Abs(previousVolume) * _averagePrice) + (volume * price)) / Math.Abs(_positionVolume);

		if (stop.HasValue)
			_shortStopPrice = _shortStopPrice.HasValue ? Math.Min(_shortStopPrice.Value, stop.Value) : stop;

		if (take.HasValue)
			_shortTakePrice = _shortTakePrice.HasValue ? Math.Min(_shortTakePrice.Value, take.Value) : take;

		_shortTrailingStop = null;
	}

	private bool CheckLongStops(ICandleMessage candle)
	{
		var trailingDistance = PipToPrice(TrailingStopPips);

		if (TrailingStopPips > 0 && _positionVolume > 0m)
		{
			var profit = candle.ClosePrice - _averagePrice;
			if (profit > trailingDistance)
			{
				var newStop = candle.ClosePrice - trailingDistance;
				if (!_longTrailingStop.HasValue || _longTrailingStop.Value < newStop)
					_longTrailingStop = newStop;
			}
		}

		var effectiveStop = _longTrailingStop.HasValue
			? (_longStopPrice.HasValue ? Math.Max(_longStopPrice.Value, _longTrailingStop.Value) : _longTrailingStop)
			: _longStopPrice;

		if (effectiveStop.HasValue && candle.LowPrice <= effectiveStop.Value)
		{
			ExitLong();
			return true;
		}

		if (_longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value)
		{
			ExitLong();
			return true;
		}

		return false;
	}

	private bool CheckShortStops(ICandleMessage candle)
	{
		var trailingDistance = PipToPrice(TrailingStopPips);

		if (TrailingStopPips > 0 && _positionVolume < 0m)
		{
			var profit = _averagePrice - candle.ClosePrice;
			if (profit > trailingDistance)
			{
				var newStop = candle.ClosePrice + trailingDistance;
				if (!_shortTrailingStop.HasValue || _shortTrailingStop.Value > newStop)
					_shortTrailingStop = newStop;
			}
		}

		var effectiveStop = _shortTrailingStop.HasValue
			? (_shortStopPrice.HasValue ? Math.Min(_shortStopPrice.Value, _shortTrailingStop.Value) : _shortTrailingStop)
			: _shortStopPrice;

		if (effectiveStop.HasValue && candle.HighPrice >= effectiveStop.Value)
		{
			ExitShort();
			return true;
		}

		if (_shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value)
		{
			ExitShort();
			return true;
		}

		return false;
	}

	private void ExitLong()
	{
		if (_positionVolume <= 0m)
			return;

		SellMarket(_positionVolume);

		_positionVolume = 0m;
		_averagePrice = 0m;
		_longStopPrice = null;
		_longTakePrice = null;
		_longTrailingStop = null;
	}

	private void ExitShort()
	{
		if (_positionVolume >= 0m)
			return;

		BuyMarket(Math.Abs(_positionVolume));

		_positionVolume = 0m;
		_averagePrice = 0m;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_shortTrailingStop = null;
	}

	private int DetectCross(decimal emaShort, decimal emaLong)
	{
		if (SignalBarShift == 0)
		{
			if (!_shortEmaCurrent.HasValue || !_longEmaCurrent.HasValue)
				return 0;

			if (_shortEmaCurrent < _longEmaCurrent && emaShort > emaLong)
				return 1;

			if (_shortEmaCurrent > _longEmaCurrent && emaShort < emaLong)
				return -1;

			return 0;
		}

		if (!_shortEmaCurrent.HasValue || !_longEmaCurrent.HasValue || !_shortEmaPrevious.HasValue || !_longEmaPrevious.HasValue)
			return 0;

		if (_shortEmaPrevious < _longEmaPrevious && _shortEmaCurrent > _longEmaCurrent)
			return 1;

		if (_shortEmaPrevious > _longEmaPrevious && _shortEmaCurrent < _longEmaCurrent)
			return -1;

		return 0;
	}

	private void UpdateHistory(decimal emaShort, decimal emaLong, decimal? macdLine)
	{
		_shortEmaPrevious = _shortEmaCurrent;
		_longEmaPrevious = _longEmaCurrent;
		_shortEmaCurrent = emaShort;
		_longEmaCurrent = emaLong;

		if (macdLine.HasValue)
			_macdLinePrevious = macdLine;
	}

	private decimal PipToPrice(int pips)
	{
		if (pips <= 0)
			return 0m;

		var step = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals ?? 0;
		var multiplier = (decimals == 3 || decimals == 5) ? 10m : 1m;

		return pips * step * multiplier;
	}

	private void CreatePendingOrders(DateTimeOffset time, decimal price, Sides side)
	{
		_pendingOrders.Clear();

		if (HedgeLevelPips <= 0)
			return;

		var distance = PipToPrice(HedgeLevelPips);
		if (distance <= 0m)
			return;

		var expiration = PendingExpirationSeconds > 0
			? time + TimeSpan.FromSeconds(PendingExpirationSeconds)
			: DateTimeOffset.MaxValue;

		var stopOffset = StopLossPips > 0 ? PipToPrice(StopLossPips) : 0m;
		var takeOffset = TakeProfitPips > 0 ? PipToPrice(TakeProfitPips) : 0m;

		for (var i = 1; i <= PendingOrderCount; i++)
		{
			var levelPrice = side == Sides.Buy
			? price + distance * i
			: price - distance * i;

			decimal? stop = null;
			decimal? take = null;

			if (StopLossPips > 0)
				stop = side == Sides.Buy ? levelPrice - stopOffset : levelPrice + stopOffset;

			if (TakeProfitPips > 0)
				take = side == Sides.Buy ? levelPrice + takeOffset : levelPrice - takeOffset;

			_pendingOrders.Add(new PendingOrder
			{
				Side = side,
				Price = levelPrice,
				StopLoss = stop,
				TakeProfit = take,
				Expiration = expiration
			});
		}
	}
}

