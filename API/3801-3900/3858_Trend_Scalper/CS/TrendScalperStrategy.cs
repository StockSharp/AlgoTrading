namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Fast EMA versus slow SMA direction filtered by a sweep of the previous
/// candle-high/low band. Independent natural exits and a native stop protect fills.
/// </summary>
public class TrendScalperStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _breakoutWindow;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<int> _moneyManagementMode;
	private readonly StrategyParam<decimal> _moneyManagementRisk;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _fastSeed = new();
	private readonly Queue<decimal> _slowCloses = new();
	private readonly Queue<decimal> _highs = new();
	private readonly Queue<decimal> _lows = new();
	private decimal? _fastEma;
	private Order _pendingOrder;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int BreakoutWindow { get => _breakoutWindow.Value; set => _breakoutWindow.Value = value; }
	public decimal FixedVolume { get => _fixedVolume.Value; set => _fixedVolume.Value = value; }
	public int MoneyManagementMode { get => _moneyManagementMode.Value; set => _moneyManagementMode.Value = value; }
	public decimal MoneyManagementRisk { get => _moneyManagementRisk.Value; set => _moneyManagementRisk.Value = value; }
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendScalperStrategy()
	{
		_fastLength = Param(nameof(FastLength), 6).SetGreaterThanZero();
		_slowLength = Param(nameof(SlowLength), 12).SetGreaterThanZero();
		_breakoutWindow = Param(nameof(BreakoutWindow), 6).SetGreaterThanZero();
		_fixedVolume = Param(nameof(FixedVolume), 0.1m).SetGreaterThanZero();
		_moneyManagementMode = Param(nameof(MoneyManagementMode), 0);
		_moneyManagementRisk = Param(nameof(MoneyManagementRisk), 40m).SetNotNegative();
		_stopLossPoints = Param(nameof(StopLossPoints), 50m).SetNotNegative();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ClearState();
	}

	private void ClearState()
	{
		_fastSeed.Clear();
		_slowCloses.Clear();
		_highs.Clear();
		_lows.Clear();
		_fastEma = null;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ClearState();
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			throw new InvalidOperationException("Trend Scalper requires a positive PriceStep.");

		if (StopLossPoints > 0m)
		{
			StartProtection(new Unit(), new Unit(StopLossPoints * step, UnitTypes.Absolute),
				useMarketOrders: true, isLocalStop: true);
			foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
			{
				var quotes = new Subscription(DataType.Level1, Security);
				quotes.MarketData.BuildField = field;
				SubscribeLevel1(quotes).Bind(ObserveQuote).Start();
			}
		}
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ObserveQuote(Level1ChangeMessage message)
	{
		// A Level1 stream is needed for native stop processing between candle closes.
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hasBand = _highs.Count == BreakoutWindow;
		var bandHigh = hasBand ? _highs.Max() : 0m;
		var bandLow = hasBand ? _lows.Min() : 0m;
		_highs.Enqueue(candle.HighPrice);
		_lows.Enqueue(candle.LowPrice);
		while (_highs.Count > BreakoutWindow) _highs.Dequeue();
		while (_lows.Count > BreakoutWindow) _lows.Dequeue();

		var price = candle.ClosePrice;
		if (_fastEma is null)
		{
			_fastSeed.Enqueue(price);
			if (_fastSeed.Count == FastLength)
				_fastEma = _fastSeed.Average();
		}
		else
			_fastEma += (price - _fastEma.Value) * 2m / (FastLength + 1);

		_slowCloses.Enqueue(price);
		while (_slowCloses.Count > SlowLength)
			_slowCloses.Dequeue();
		if (!hasBand || _fastEma is null || _slowCloses.Count < SlowLength ||
			_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;

		if (Position > 0m && candle.HighPrice > bandHigh)
		{
			Submit(Sides.Sell, Position, "Trend scalper band exit");
			return;
		}
		if (Position < 0m && candle.LowPrice < bandLow)
		{
			Submit(Sides.Buy, Math.Abs(Position), "Trend scalper band exit");
			return;
		}
		if (Position != 0m)
			return;

		var slow = _slowCloses.Average();
		if (_fastEma > slow && candle.LowPrice <= bandLow)
			Enter(Sides.Buy);
		else if (_fastEma < slow && candle.HighPrice >= bandHigh)
			Enter(Sides.Sell);
	}

	private void Enter(Sides side)
	{
		var volume = CalculateOrderVolume();
		if (volume > 0m)
			Submit(side, volume, "Trend scalper entry");
	}

	private decimal CalculateOrderVolume()
	{
		var volume = FixedVolume;
		if (MoneyManagementMode != 0)
		{
			var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
			if (balance > 0m)
			{
				var fractional = Math.Ceiling(balance * MoneyManagementRisk / 10000m) / 10m;
				volume = MoneyManagementMode > 0 ? Math.Max(1m, Math.Ceiling(fractional)) : fractional;
				volume = Math.Min(volume, 100m);
			}
		}
		if (Security?.VolumeStep is decimal step && step > 0m)
			volume = decimal.Floor(volume / step) * step;
		if (Security?.MinVolume is decimal min && min > 0m)
			volume = Math.Max(volume, min);
		if (Security?.MaxVolume is decimal max && max > 0m)
			volume = Math.Min(volume, max);
		return volume;
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
		RegisterOrder(_pendingOrder);
	}
}
