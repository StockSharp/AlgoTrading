namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Beta-neutral pairs: synchronized log-spread mean reversion and a filled two-leg loss limit.</summary>
public class BetaAdjustedPairsStrategy : Strategy
{
	private readonly StrategyParam<Security> _asset2;
	private readonly StrategyParam<Portfolio> _asset2Portfolio;
	private readonly StrategyParam<decimal> _betaAsset1;
	private readonly StrategyParam<decimal> _betaAsset2;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;
	private readonly SortedDictionary<DateTime, decimal> _firstBars = new(), _secondBars = new();
	private readonly Queue<decimal> _spreads = new();
	private readonly List<Order> _orders = new();
	private DateTime _lastPair;
	private decimal? _base1, _base2, _previousZ;
	private decimal _cost1, _cost2, _quantity1, _quantity2, _primaryQuantity, _hedgeQuantity;
	private int _side, _bar;
	private int? _lastEntry;

	/// <summary>Initializes the published pair inputs, signal and loss limit.</summary>
	public BetaAdjustedPairsStrategy()
	{
		_asset2 = Param<Security>(nameof(Asset2)).SetRequired()
			.SetDisplay("Asset2", "Explicit second execution instrument", "Parameters");
		_asset2Portfolio = Param<Portfolio>(nameof(Asset2Portfolio)).SetRequired()
			.SetDisplay("Asset2Portfolio", "Explicit portfolio for the hedge leg", "Parameters");
		_betaAsset1 = Param(nameof(BetaAsset1), 1m).SetGreaterThanZero()
			.SetDisplay("BetaAsset1", "Positive primary beta", "Parameters");
		_betaAsset2 = Param(nameof(BetaAsset2), 1m).SetGreaterThanZero()
			.SetDisplay("BetaAsset2", "Positive secondary beta", "Parameters");
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20).SetRange(2, 100000)
			.SetDisplay("LookbackPeriod", "Matched spread observations in the z-score window", "Parameters");
		_entryThreshold = Param(nameof(EntryThreshold), 2m).SetGreaterThanZero()
			.SetDisplay("EntryThreshold", "Extreme z-score boundary to cross back toward zero", "Parameters");
		_exitThreshold = Param(nameof(ExitThreshold), 0.15m).SetNotNegative()
			.SetDisplay("ExitThreshold", "Mean-reversion exit band", "Parameters");
		_stopLoss = Param(nameof(StopLoss), 2m).SetGreaterThanZero()
			.SetDisplay("StopLoss", "Loss limit as a percentage of gross filled entry notional", "Parameters");
		_cooldownBars = Param(nameof(CooldownBars), 120).SetNotNegative()
			.SetDisplay("CooldownBars", "Finished matched bars between pair entries", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("CandleType", "Same finished candle interval on both legs", "Parameters");
	}

	/// <summary>Explicit second execution instrument.</summary>
	public Security Asset2
	{
		get => _asset2.Value;
		set => _asset2.Value = value;
	}

	/// <summary>Explicit portfolio for the hedge leg.</summary>
	public Portfolio Asset2Portfolio
	{
		get => _asset2Portfolio.Value;
		set => _asset2Portfolio.Value = value;
	}

	/// <summary>Positive primary beta.</summary>
	public decimal BetaAsset1
	{
		get => _betaAsset1.Value;
		set => _betaAsset1.Value = value;
	}

	/// <summary>Positive secondary beta.</summary>
	public decimal BetaAsset2
	{
		get => _betaAsset2.Value;
		set => _betaAsset2.Value = value;
	}

	/// <summary>Matched spread observations in the z-score window.</summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>Extreme z-score boundary to cross back toward zero.</summary>
	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	/// <summary>Mean-reversion exit band.</summary>
	public decimal ExitThreshold
	{
		get => _exitThreshold.Value;
		set => _exitThreshold.Value = value;
	}

	/// <summary>Loss limit as a percentage of gross filled entry notional.</summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>Finished matched bars between pair entries.</summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>Same finished candle interval on both legs.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Asset2, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_firstBars.Clear(); _secondBars.Clear(); _spreads.Clear(); _orders.Clear();
		_lastPair = default;
		_base1 = _base2 = _previousZ = null;
		_cost1 = _cost2 = _quantity1 = _quantity2 = _primaryQuantity = _hedgeQuantity = 0m;
		_side = _bar = 0;
		_lastEntry = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		if (Asset2 is null || Asset2Portfolio is null || Security is null
			|| string.Equals(Asset2.Id, Security.Id, StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("Asset2 must explicitly identify a different instrument, and Asset2Portfolio is required.");
		if (ExitThreshold >= EntryThreshold)
			throw new InvalidOperationException("ExitThreshold must be smaller than EntryThreshold.");
		base.OnStarted2(time);
		var first = SubscribeCandles(CandleType);
		var second = SubscribeCandles(CandleType, security: Asset2);
		first.Bind(c => ProcessCandle(c, _firstBars)).Start();
		second.Bind(c => ProcessCandle(c, _secondBars)).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, first);
			DrawCandles(area, second);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, SortedDictionary<DateTime, decimal> buffer)
	{
		if (candle.State != CandleStates.Finished || candle.ClosePrice <= 0m || candle.OpenTime <= _lastPair)
			return;
		buffer[candle.OpenTime] = candle.ClosePrice;
		// Bound unmatched observations if one feed stops; never combine candles with different times.
		while (buffer.Count > LookbackPeriod * 2)
			buffer.Remove(buffer.First().Key);
		if (!_firstBars.TryGetValue(candle.OpenTime, out var first) || !_secondBars.TryGetValue(candle.OpenTime, out var second))
			return;
		_lastPair = candle.OpenTime;
		foreach (var old in _firstBars.Keys.Where(t => t <= _lastPair).ToArray())
			_firstBars.Remove(old);
		foreach (var old in _secondBars.Keys.Where(t => t <= _lastPair).ToArray())
			_secondBars.Remove(old);
		_bar++;
		_base1 ??= first;
		_base2 ??= second;
		var beta = BetaAsset1 / BetaAsset2;
		var spread = (decimal)Math.Log((double)(first / _base1.Value)) - beta * (decimal)Math.Log((double)(second / _base2.Value));
		_spreads.Enqueue(spread);
		if (_spreads.Count > LookbackPeriod)
			_spreads.Dequeue();
		if (_spreads.Count < LookbackPeriod)
			return;
		var mean = _spreads.Average();
		var deviation = (decimal)Math.Sqrt((double)_spreads.Average(v => (v - mean) * (v - mean)));
		var previous = _previousZ;
		var z = deviation > 0m ? (spread - mean) / deviation : (decimal?)null;
		_previousZ = z;
		if (!IsFormedAndOnlineAndAllowTrading() || _orders.Any(o => o.State is not (OrderStates.Done or OrderStates.Failed)))
			return;
		_orders.Clear();
		var position1 = Position;
		var position2 = GetPositionValue(Asset2, Asset2Portfolio) ?? 0m;
		if (position1 != 0m || position2 != 0m)
		{
			if (_side == 0 || position1 != _side * _primaryQuantity || position2 != -_side * _hedgeQuantity)
			{
				ClosePair("Unbalanced");
				return;
			}
			var profit = _side * (_quantity1 * first - _cost1 - _quantity2 * second + _cost2);
			if (_cost1 + _cost2 > 0m && profit <= -(_cost1 + _cost2) * StopLoss / 100m)
				ClosePair("Stop");
			else if (z is decimal score && (_side > 0 ? score >= -ExitThreshold : score <= ExitThreshold))
				ClosePair("Mean");
			return;
		}
		_side = 0;
		if (previous is null || z is null || _lastEntry is int lastEntry && _bar - lastEntry <= CooldownBars)
			return;
		var side = previous <= -EntryThreshold && z > -EntryThreshold && z < 0m ? 1
			: previous >= EntryThreshold && z < EntryThreshold && z > 0m ? -1 : 0;
		if (side == 0)
			return;
		var step = Asset2.VolumeStep ?? 1m;
		_hedgeQuantity = Math.Floor(Volume * first * beta / second / step) * step;
		if (_hedgeQuantity < (Asset2.MinVolume ?? step) || Asset2.MaxVolume is decimal max && _hedgeQuantity > max)
			throw new InvalidOperationException("The beta-neutral hedge quantity is outside Asset2 lot limits.");
		_cost1 = _cost2 = _quantity1 = _quantity2 = 0m;
		_side = side;
		_primaryQuantity = Volume;
		_lastEntry = _bar;
		Submit(Security, Portfolio, side * _primaryQuantity, "Entry");
		Submit(Asset2, Asset2Portfolio, -side * _hedgeQuantity, "Entry");
	}

	private void Submit(Security security, Portfolio portfolio, decimal signedQuantity, string reason)
	{
		if (signedQuantity == 0m)
			return;
		var order = new Order
		{
			Security = security, Portfolio = portfolio,
			Side = signedQuantity > 0m ? Sides.Buy : Sides.Sell,
			Volume = Math.Abs(signedQuantity), Type = OrderTypes.Market, Comment = "0299" + reason,
		};
		_orders.Add(order);
		RegisterOrder(order);
	}

	private void ClosePair(string reason)
	{
		Submit(Security, Portfolio, -Position, reason);
		Submit(Asset2, Asset2Portfolio, -(GetPositionValue(Asset2, Asset2Portfolio) ?? 0m), reason);
		_side = 0;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		if (trade.Order.Comment != "0299Entry")
			return;
		if (trade.Order.Security == Security)
		{
			_quantity1 += trade.Trade.Volume;
			_cost1 += trade.Trade.Volume * trade.Trade.Price;
		}
		else
		{
			_quantity2 += trade.Trade.Volume;
			_cost2 += trade.Trade.Volume * trade.Trade.Price;
		}
	}
}
