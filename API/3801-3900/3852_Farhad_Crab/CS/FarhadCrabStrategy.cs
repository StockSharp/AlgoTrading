namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Farhad Crab uses EMA(9) on typical M1 prices, SMA(9) of M1 opens,
/// and daily SMMA protection with direction-specific take/trailing exits.
/// </summary>
public class FarhadCrabStrategy : Strategy
{
	private readonly StrategyParam<int> _dailyMaPeriod;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _longTakeProfitPips;
	private readonly StrategyParam<decimal> _shortTakeProfitPips;
	private readonly StrategyParam<decimal> _longTrailingStopPips;
	private readonly StrategyParam<decimal> _shortTrailingStopPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _typicalSeed = new();
	private readonly Queue<decimal> _opens = new();
	private readonly Queue<decimal> _dailySeed = new();
	private decimal? _emaTypical;
	private decimal? _dailySmma;
	private decimal? _priorSmma;
	private decimal? _priorDailyClose;
	private decimal? _entryPrice;
	private decimal? _trailingLevel;
	private decimal _bestPrice;
	private Order _pendingOrder;
	private Order _entryOrder;

	public int DailyMaPeriod { get => _dailyMaPeriod.Value; set => _dailyMaPeriod.Value = value; }
	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }
	public decimal LongTakeProfitPips { get => _longTakeProfitPips.Value; set => _longTakeProfitPips.Value = value; }
	public decimal ShortTakeProfitPips { get => _shortTakeProfitPips.Value; set => _shortTakeProfitPips.Value = value; }
	public decimal LongTrailingStopPips { get => _longTrailingStopPips.Value; set => _longTrailingStopPips.Value = value; }
	public decimal ShortTrailingStopPips { get => _shortTrailingStopPips.Value; set => _shortTrailingStopPips.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FarhadCrabStrategy()
	{
		_dailyMaPeriod = Param(nameof(DailyMaPeriod), 55).SetGreaterThanZero();
		_orderVolume = Param(nameof(OrderVolume), 0.1m).SetGreaterThanZero();
		_longTakeProfitPips = Param(nameof(LongTakeProfitPips), 10m).SetNotNegative();
		_shortTakeProfitPips = Param(nameof(ShortTakeProfitPips), 10m).SetNotNegative();
		_longTrailingStopPips = Param(nameof(LongTrailingStopPips), 8m).SetNotNegative();
		_shortTrailingStopPips = Param(nameof(ShortTrailingStopPips), 8m).SetNotNegative();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame())];

	protected override void OnReseted()
	{
		base.OnReseted();
		ClearState();
	}

	private void ClearState()
	{
		_typicalSeed.Clear();
		_opens.Clear();
		_dailySeed.Clear();
		_emaTypical = null;
		_dailySmma = null;
		_priorSmma = null;
		_priorDailyClose = null;
		_entryPrice = null;
		_trailingLevel = null;
		_bestPrice = 0m;
		_pendingOrder = null;
		_entryOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ClearState();
		if ((Security?.PriceStep ?? 0m) <= 0m)
			throw new InvalidOperationException("Farhad Crab requires a positive instrument PriceStep.");
		SubscribeCandles(TimeSpan.FromDays(1).TimeFrame()).Bind(ProcessDailyCandle).Start();
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private decimal PipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		return Security?.Decimals is 3 or 5 ? step * 10m : step;
	}

	private bool Pending => _pendingOrder is not null &&
		_pendingOrder.State is not (OrderStates.Done or OrderStates.Failed);

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var priorSmma = _dailySmma;
		var priorClose = _priorDailyClose;
		if (_dailySmma is null)
		{
			_dailySeed.Enqueue(candle.ClosePrice);
			if (_dailySeed.Count == DailyMaPeriod)
				_dailySmma = _dailySeed.Average();
		}
		else
			_dailySmma = (_dailySmma.Value * (DailyMaPeriod - 1) + candle.ClosePrice) / DailyMaPeriod;

		_priorSmma = priorSmma;
		_priorDailyClose = candle.ClosePrice;

		if (Pending || Position == 0m || priorSmma is null || priorClose is null || _dailySmma is null)
			return;

		if (Position > 0m && priorSmma < priorClose && _dailySmma > candle.ClosePrice ||
			Position < 0m && priorSmma > priorClose && _dailySmma < candle.ClosePrice)
			Exit("Farhad Crab daily safety exit");
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		if (_emaTypical is null)
		{
			_typicalSeed.Enqueue(typical);
			if (_typicalSeed.Count == 9)
				_emaTypical = _typicalSeed.Average();
		}
		else
			_emaTypical = (_emaTypical.Value * 8m + typical * 2m) / 10m;

		_opens.Enqueue(candle.OpenPrice);
		while (_opens.Count > 9)
			_opens.Dequeue();

		if (Pending)
			return;
		if (Position != 0m)
		{
			HandleRisk(candle);
			return;
		}
		if (_emaTypical is null || _opens.Count < 9)
			return;

		if (candle.LowPrice > _emaTypical.Value)
			Enter(Sides.Buy);
		else if (candle.HighPrice < _opens.Average())
			Enter(Sides.Sell);
	}

	private void Enter(Sides side)
	{
		var volume = OrderVolume;
		if (Security?.VolumeStep is decimal step && step > 0m)
			volume = decimal.Floor(volume / step) * step;
		if (Security?.MinVolume is decimal min && min > 0m)
			volume = Math.Max(min, volume);
		if (Security?.MaxVolume is decimal max && max > 0m)
			volume = Math.Min(max, volume);
		if (volume > 0m)
			Submit(side, volume, "Farhad Crab entry");
	}

	private void HandleRisk(ICandleMessage candle)
	{
		if (_entryPrice is not decimal entry)
			return;
		var positive = Position > 0m;
		var pip = PipSize();
		var target = (positive ? LongTakeProfitPips : ShortTakeProfitPips) * pip;
		if (target > 0m && (positive ? candle.HighPrice >= entry + target :
			candle.LowPrice <= entry - target))
		{
			Exit("Farhad Crab take profit");
			return;
		}

		if (_trailingLevel is decimal level && (positive ? candle.LowPrice <= level :
			candle.HighPrice >= level))
		{
			Exit("Farhad Crab trailing stop");
			return;
		}

		var distance = (positive ? LongTrailingStopPips : ShortTrailingStopPips) * pip;
		if (distance <= 0m)
			return;

		_bestPrice = positive ? Math.Max(_bestPrice, candle.HighPrice) :
			Math.Min(_bestPrice, candle.LowPrice);
		var move = positive ? _bestPrice - entry : entry - _bestPrice;
		if (move < distance)
			return;
		var next = positive ? _bestPrice - distance : _bestPrice + distance;
		if (_trailingLevel is null || (positive ? next > _trailingLevel.Value : next < _trailingLevel.Value))
			_trailingLevel = next;
	}

	private void Exit(string reason)
	{
		if (!Pending && Position != 0m)
			Submit(Position > 0m ? Sides.Sell : Sides.Buy, Math.Abs(Position), reason);
	}

	private void Submit(Sides side, decimal volume, string comment)
	{
		_pendingOrder = new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Type = OrderTypes.Market,
			Side = side,
			Volume = volume,
			Comment = comment,
		};
		if (comment == "Farhad Crab entry")
			_entryOrder = _pendingOrder;
		RegisterOrder(_pendingOrder);
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		if (trade?.Order == _entryOrder && trade.Trade is not null)
		{
			_entryPrice = trade.Trade.Price;
			_bestPrice = _entryPrice.Value;
			_trailingLevel = null;
		}
		else if (Position == 0m)
		{
			_entryPrice = null;
			_trailingLevel = null;
			_bestPrice = 0m;
		}
	}
}
