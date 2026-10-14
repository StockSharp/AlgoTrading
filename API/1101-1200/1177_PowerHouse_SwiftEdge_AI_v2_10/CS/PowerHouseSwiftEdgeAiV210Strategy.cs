namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trades confirmed-pivot breakouts with signed close momentum and finished-bar spacing,
/// protected by fixed price-point distances from actual fill prices.
/// </summary>
public class PowerHouseSwiftEdgeAiV210Strategy : Strategy
{
	private readonly StrategyParam<int> _pivotPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _minSignalBars;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;
	private readonly List<(decimal high, decimal low, decimal close)> _bars = [];
	private decimal? _pivotHigh;
	private decimal? _pivotLow;
	private long _barCount;
	private long? _lastSignal;
	private Order _pendingOrder;
	private decimal _filledPosition;
	private decimal _entryPrice;

	/// <summary>Initializes two-bar pivots, ten-bar momentum, five-bar spacing and 50/100 price-point exits.</summary>
	public PowerHouseSwiftEdgeAiV210Strategy()
	{
		_pivotPeriod = Param(nameof(PivotPeriod), 2).SetGreaterThanZero()
			.SetDisplay("Pivot Period", "Strict neighboring bars on each side of a confirmed pivot", "Signals");
		_momentumPeriod = Param(nameof(MomentumPeriod), 10).SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Finished-bar close lookback", "Signals");
		_momentumThreshold = Param(nameof(MomentumThreshold), 0m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("Momentum Threshold", "Minimum signed close change in absolute price units", "Signals");
		_minSignalBars = Param(nameof(MinSignalBars), 5).SetRange(0, int.MaxValue)
			.SetDisplay("Minimum Signal Bars", "Finished-bar interval between accepted entries; zero disables spacing", "Signals");
		_stopLossPoints = Param(nameof(StopLossPoints), 50m).SetGreaterThanZero()
			.SetDisplay("Stop Loss Points", "Fixed absolute price distance from the actual fill basis", "Protection");
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100m).SetGreaterThanZero()
			.SetDisplay("Take Profit Points", "Fixed absolute price distance from the actual fill basis", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Finished candles supplying pivots, momentum and spacing", "General");
		OrderRegistering += order => _pendingOrder = order;
		OwnTradeReceived += ObserveFill;
	}

	/// <summary>Strict neighboring bars on each side of a pivot.</summary>
	public int PivotPeriod { get => _pivotPeriod.Value; set => _pivotPeriod.Value = value; }
	/// <summary>Lookback for the signed close-price difference.</summary>
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }
	/// <summary>Nonnegative absolute-price threshold, exceeded strictly in the entry direction.</summary>
	public decimal MomentumThreshold { get => _momentumThreshold.Value; set => _momentumThreshold.Value = value; }
	/// <summary>Minimum finished-bar interval between accepted entry orders; zero disables it.</summary>
	public int MinSignalBars { get => _minSignalBars.Value; set => _minSignalBars.Value = value; }
	/// <summary>Stop distance in absolute quote-price points, not ticks or percent.</summary>
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	/// <summary>Take-profit distance in absolute quote-price points, not ticks or percent.</summary>
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	/// <summary>Base finished-candle type; five minutes by default.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	private void ResetState()
	{
		_bars.Clear();
		_pivotHigh = _pivotLow = null;
		_barCount = 0;
		_lastSignal = null;
		_pendingOrder = null;
		_filledPosition = _entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (PivotPeriod <= 0 || MomentumPeriod <= 0 || MomentumThreshold < 0m
			|| MinSignalBars < 0 || StopLossPoints <= 0m || TakeProfitPoints <= 0m)
			throw new ArgumentException("Pivot/momentum periods and point distances must be positive; thresholds and spacing cannot be negative.");
		ResetState();
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveQuote).Start();
		}
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private bool IsPending => _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed);

	private void Submit(Sides side, decimal volume, string comment)
		=> RegisterOrder(new Order { Security = Security, Portfolio = Portfolio, Type = OrderTypes.Market, Side = side, Volume = volume, Comment = comment });

	private void ObserveFill(Subscription subscription, MyTrade trade)
	{
		var previous = _filledPosition;
		var signed = trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
		_filledPosition += signed;
		if (_filledPosition == 0m)
			_entryPrice = 0m;
		else if (previous == 0m || Math.Sign(_filledPosition) != Math.Sign(previous))
			_entryPrice = trade.Trade.Price;
		else if (Math.Sign(signed) == Math.Sign(previous))
			_entryPrice = (_entryPrice * Math.Abs(previous) + trade.Trade.Price * Math.Abs(signed)) / Math.Abs(_filledPosition);
	}

	private bool TryRiskExit(decimal price)
	{
		if (Position == 0m || _entryPrice <= 0m || IsPending || !IsFormedAndOnlineAndAllowTrading())
			return false;
		var profit = (price - _entryPrice) * Math.Sign(Position);
		var comment = profit <= -StopLossPoints ? "SwiftEdge stop loss" : profit >= TakeProfitPoints ? "SwiftEdge take profit" : null;
		if (comment is null)
			return false;
		Submit(Position > 0m ? Sides.Sell : Sides.Buy, Math.Abs(Position), comment);
		return true;
	}

	private void ObserveQuote(Level1ChangeMessage quote)
	{
		var field = Position > 0m ? Level1Fields.BestBidPrice : Level1Fields.BestAskPrice;
		if (quote.TryGetDecimal(field) is decimal price && price > 0m)
			TryRiskExit(price);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		_barCount++;
		var width = checked(2 * PivotPeriod + 1);
		_bars.Add((candle.HighPrice, candle.LowPrice, candle.ClosePrice));
		if (_bars.Count > Math.Max(width, checked(MomentumPeriod + 1)))
			_bars.RemoveAt(0);
		if (_bars.Count >= width)
		{
			var start = _bars.Count - width;
			var center = start + PivotPeriod;
			var high = true;
			var low = true;
			for (var i = start; i < _bars.Count; i++)
			{
				if (i == center)
					continue;
				high &= _bars[center].high > _bars[i].high;
				low &= _bars[center].low < _bars[i].low;
			}
			if (high)
				_pivotHigh = _bars[center].high;
			if (low)
				_pivotLow = _bars[center].low;
		}
		if (TryRiskExit(candle.ClosePrice) || IsPending || !IsFormedAndOnlineAndAllowTrading()
			|| _bars.Count <= MomentumPeriod || _bars.Count < 2
			|| _lastSignal is long last && _barCount - last < MinSignalBars)
			return;
		var previous = _bars[^2].close;
		var momentum = candle.ClosePrice - _bars[_bars.Count - MomentumPeriod - 1].close;
		Sides? side = null;
		if (_pivotHigh is decimal pivotHigh && previous <= pivotHigh && candle.ClosePrice > pivotHigh
			&& momentum > MomentumThreshold && Position <= 0m)
			side = Sides.Buy;
		else if (_pivotLow is decimal pivotLow && previous >= pivotLow && candle.ClosePrice < pivotLow
			&& momentum < -MomentumThreshold && Position >= 0m)
			side = Sides.Sell;
		if (side is Sides direction)
		{
			_lastSignal = _barCount;
			Submit(direction, Volume + Math.Abs(Position), "SwiftEdge pivot breakout");
		}
	}
}
