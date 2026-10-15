using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on ADX and Directional Movement indicators.
/// Trades strict DI crossings with rising strong ADX, exits on weakening/opposite crosses,
/// and protects actual fills with entry-frozen native ATR distances.
/// </summary>
public class AdxDiStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;

	private decimal _prevPlusDi, _prevMinusDi, _prevAdx;
	private bool _hasPrevValues;
	private Order _pendingOrder, _entryOrder;
	private Unit _stopDistance;
	private decimal _requestedDistance;
	private bool _protectionStarted;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX threshold for trend confirmation.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
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

	/// <summary>
	/// Initializes a new instance of the <see cref="AdxDiStrategy"/>.
	/// </summary>
	public AdxDiStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
			.SetOptimize(10, 20, 2);

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "ADX level to confirm trend", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Wilder ATR lookback for entry-frozen protection.", "Protection");
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m).SetNotNegative()
			.SetDisplay("ATR Multiplier", "Signal ATR distance; zero disables the stop.", "Protection");
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
		_prevPlusDi = _prevMinusDi = _prevAdx = 0m;
		_hasPrevValues = default;
		_pendingOrder = _entryOrder = null;
		_stopDistance = null;
		_requestedDistance = 0m;
		_protectionStarted = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, atr, ProcessCandle, false)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before the callback, including between bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adx = (IAverageDirectionalIndexValue)adxValue;
		if (adx.MovingAverage is not decimal adxMain || adx.Dx.Plus is not decimal plusDi || adx.Dx.Minus is not decimal minusDi)
			return;

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			SavePrevious(plusDi, minusDi, adxMain);
			return;
		}

		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
		{
			SavePrevious(plusDi, minusDi, adxMain);
			return;
		}

		// Equality alone is not a bearish crossing.
		var upCross = _prevPlusDi <= _prevMinusDi && plusDi > minusDi;
		var downCross = _prevMinusDi <= _prevPlusDi && minusDi > plusDi;
		var confirmed = adxMain >= AdxThreshold && adxMain > _prevAdx;
		if (confirmed && upCross && Position <= 0m)
			Enter(Sides.Buy, atrValue.GetValue<decimal>());
		else if (confirmed && downCross && Position >= 0m)
			Enter(Sides.Sell, atrValue.GetValue<decimal>());
		else if (Position > 0m && (adxMain < _prevAdx || downCross))
			SellMarket(Position);
		else if (Position < 0m && (adxMain < _prevAdx || upCross))
			BuyMarket(Math.Abs(Position));

		SavePrevious(plusDi, minusDi, adxMain);
	}

	private void SavePrevious(decimal plus, decimal minus, decimal adx)
	{
		_prevPlusDi = plus;
		_prevMinusDi = minus;
		_prevAdx = adx;
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
			Comment = "ADX signal",
		};
		RegisterOrder(_entryOrder);
	}

	private void ProcessEntryFill(MyTrade trade)
	{
		// Keep the old native distance until a partial reversal actually changes position direction.
		if (trade.Order == _entryOrder && Position != 0m && (Position > 0m) == (trade.Order.Side == Sides.Buy))
			_stopDistance.Value = _requestedDistance;
	}
}
