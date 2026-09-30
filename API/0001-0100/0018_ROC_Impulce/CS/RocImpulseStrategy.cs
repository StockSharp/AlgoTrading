using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Signed percentage ROC breakouts, zero exits and native actual-fill ATR protection.
/// </summary>
public class RocImpulseStrategy : Strategy
{
	private readonly StrategyParam<int> _rocPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _thresholdPercent;

	private decimal _prevRoc;
	private bool _hasPrevValues;
	private Unit _stopDistance;
	private bool _protectionStarted;
	private decimal _requestedDistance;
	private Order _entryOrder;
	private Order _pendingOrder;

	/// <summary>
	/// Percentage rate-of-change period.
	/// </summary>
	public int RocPeriod
	{
		get => _rocPeriod.Value;
		set => _rocPeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal ThresholdPercent { get => _thresholdPercent.Value; set => _thresholdPercent.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="RocImpulseStrategy"/>.
	/// </summary>
	public RocImpulseStrategy()
	{
		_rocPeriod = Param(nameof(RocPeriod), 12)
			.SetDisplay("ROC Period", "Lookback for percentage rate of change.", "Indicators")
			.SetOptimize(8, 20, 4);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Wilder ATR lookback.", "Indicators");
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m).SetNotNegative()
			.SetDisplay("ATR Multiplier", "Entry ATR stop distance; zero disables it.", "Protection");
		_thresholdPercent = Param(nameof(ThresholdPercent), 0.5m).SetGreaterThanZero()
			.SetDisplay("Threshold (%)", "Symmetric percentage ROC breakout level.", "Signal");
		OrderRegistering += order => _pendingOrder = order;
		Trades.TradeAdded += ProcessEntryFill;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRoc = default;
		_hasPrevValues = default;
		_stopDistance = null;
		_protectionStarted = false;
		_requestedDistance = 0m;
		_entryOrder = _pendingOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var roc = new RateOfChange { Length = RocPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(roc, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, roc);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before the callback, including between bars.
	}

	private void ProcessCandle(ICandleMessage candle, decimal rocValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			_prevRoc = rocValue;
			return;
		}

		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
		{
			_prevRoc = rocValue;
			return;
		}

		if (_prevRoc <= ThresholdPercent && rocValue > ThresholdPercent && Position <= 0m)
			Enter(Sides.Buy, atrValue);
		else if (_prevRoc >= -ThresholdPercent && rocValue < -ThresholdPercent && Position >= 0m)
			Enter(Sides.Sell, atrValue);
		else if (Position > 0m && rocValue <= 0m)
			SellMarket(Position);
		else if (Position < 0m && rocValue >= 0m)
			BuyMarket(Math.Abs(Position));

		_prevRoc = rocValue;
	}

	private void Enter(Sides side, decimal atr)
	{
		_requestedDistance = atr * AtrMultiplier;
		_stopDistance ??= new Unit(_requestedDistance);
		if (Position == 0m) _stopDistance.Value = _requestedDistance;
		if (!_protectionStarted && _requestedDistance > 0m)
		{
			StartProtection(new Unit(), _stopDistance, useMarketOrders: true, isLocalStop: true);
			_protectionStarted = true;
		}
		_entryOrder = new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Type = OrderTypes.Market,
			Side = side,
			Volume = Volume + Math.Abs(Position),
			Comment = "ROC signal",
		};
		RegisterOrder(_entryOrder);
	}

	private void ProcessEntryFill(MyTrade trade)
	{
		// Preserve the old position's distance until a partial reversal actually changes direction.
		// Native processors retain this Unit reference and anchor themselves to actual fills.
		if (trade.Order == _entryOrder && Position != 0m && (Position > 0m) == (trade.Order.Side == Sides.Buy))
			_stopDistance.Value = _requestedDistance;
	}
}
