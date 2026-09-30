using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Bollinger Bands squeeze.
/// Trades outside-band crossings after a narrow finished bar, exits at the middle band,
/// and optionally protects actual fills with an entry-frozen ATR distance.
/// </summary>
public class BollingerSqueezeStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _squeezeThreshold;
	private readonly StrategyParam<bool> _useAtrStop;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;

	private decimal _prevBandWidth;
	private bool _hasPrevValues;
	private decimal _prevClose, _prevUpper, _prevLower;
	private Order _pendingOrder, _entryOrder;
	private Unit _stopDistance;
	private decimal _requestedDistance;
	private bool _protectionStarted;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal SqueezeThreshold { get => _squeezeThreshold.Value; set => _squeezeThreshold.Value = value; }
	public bool UseAtrStop { get => _useAtrStop.Value; set => _useAtrStop.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="BollingerSqueezeStrategy"/>.
	/// </summary>
	public BollingerSqueezeStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Indicators")
			.SetOptimize(15, 30, 5);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_squeezeThreshold = Param(nameof(SqueezeThreshold), 0.1m).SetGreaterThanZero()
			.SetDisplay("Squeeze Threshold", "Maximum preceding-bar band-width/middle ratio.", "Signal");
		_useAtrStop = Param(nameof(UseAtrStop), false)
			.SetDisplay("Use ATR Stop", "Optional actual-fill local ATR protection.", "Protection");
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Wilder ATR lookback when protection is enabled.", "Protection");
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m).SetNotNegative()
			.SetDisplay("ATR Multiplier", "Frozen signal ATR distance; zero disables the stop.", "Protection");
		OrderRegistering += order => _pendingOrder = order;
		Trades.TradeAdded += ProcessEntryFill;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return UseAtrStop ? [(Security, CandleType), (Security, DataType.Level1)] : [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevBandWidth = default;
		_hasPrevValues = default;
		_prevClose = _prevUpper = _prevLower = 0m;
		_pendingOrder = _entryOrder = null;
		_stopDistance = null;
		_requestedDistance = 0m;
		_protectionStarted = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		AverageTrueRange atr = null;
		if (UseAtrStop)
		{
			atr = new AverageTrueRange { Length = AtrPeriod };
			subscription.BindEx(bb, atr, (candle, bands, value) => ProcessCandle(candle, bands, value.GetValue<decimal>()), false);
			foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
			{
				var quotes = new Subscription(DataType.Level1, Security);
				quotes.MarketData.BuildField = field;
				SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
			}
		}
		else
			subscription.BindEx(bb, (candle, bands) => ProcessCandle(candle, bands, 0m), false);
		subscription.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			if (atr is not null) DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates optional native protection before this callback, also between bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bb = (IBollingerBandsValue)bbValue;

		if (bb.UpBand is not decimal upper ||
			bb.LowBand is not decimal lower ||
			bb.MovingAverage is not decimal middle)
			return;

		if (middle == 0)
			return;

		var bandWidth = (upper - lower) / middle;

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			SavePrevious(candle.ClosePrice, upper, lower, bandWidth);
			return;
		}

		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
		{
			SavePrevious(candle.ClosePrice, upper, lower, bandWidth);
			return;
		}

		var price = candle.ClosePrice;

		var narrow = _prevBandWidth <= SqueezeThreshold;
		if (narrow && _prevClose <= _prevUpper && price > upper && Position <= 0m)
			Enter(Sides.Buy, atrValue);
		else if (narrow && _prevClose >= _prevLower && price < lower && Position >= 0m)
			Enter(Sides.Sell, atrValue);
		else if (Position > 0m && price <= middle)
			SellMarket(Position);
		else if (Position < 0m && price >= middle)
			BuyMarket(Math.Abs(Position));

		SavePrevious(price, upper, lower, bandWidth);
	}

	private void SavePrevious(decimal close, decimal upper, decimal lower, decimal width)
	{
		_prevClose = close;
		_prevUpper = upper;
		_prevLower = lower;
		_prevBandWidth = width;
	}

	private void Enter(Sides side, decimal atr)
	{
		_requestedDistance = UseAtrStop ? atr * AtrMultiplier : 0m;
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
			Comment = "Squeeze signal",
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
