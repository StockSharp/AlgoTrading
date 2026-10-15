namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Opens flat-position trades on native RMI momentum and SMA alignment,
/// then exits only at a ratcheted SMA/Wilder ATR price stop.
/// </summary>
public class PresentTrendRmiSynergyStrategy : Strategy
{
	private readonly StrategyParam<int> _rmiPeriod;
	private readonly StrategyParam<int> _rmiMomentum;
	private readonly StrategyParam<int> _superTrendLength;
	private readonly StrategyParam<decimal> _superTrendMultiplier;
	private readonly StrategyParam<Sides?> _direction;
	private readonly StrategyParam<DataType> _candleType;
	private decimal? _stopPrice;
	private Order _pendingOrder;

	/// <summary>Initializes RMI21 with momentum3, SMA/ATR5, multiplier4 and both directions on five-minute candles.</summary>
	public PresentTrendRmiSynergyStrategy()
	{
		_rmiPeriod = Param(nameof(RmiPeriod), 21).SetGreaterThanZero()
			.SetDisplay("RMI Period", "Native rolling mean period of positive/negative momentum", "Indicators");
		_rmiMomentum = Param(nameof(RmiMomentum), 3).SetGreaterThanZero()
			.SetDisplay("RMI Momentum", "Closed-bar lag of each close-price momentum sample", "Indicators");
		_superTrendLength = Param(nameof(SuperTrendLength), 5).SetGreaterThanZero()
			.SetDisplay("SuperTrend Length", "Shared close SMA and Wilder ATR period for trailing bands", "Protection");
		_superTrendMultiplier = Param(nameof(SuperTrendMultiplier), 4m).SetGreaterThanZero()
			.SetDisplay("SuperTrend Multiplier", "ATR width of the ratcheted SMA price stop", "Protection");
		_direction = Param<Sides?>(nameof(Direction), null)
			.SetDisplay("Direction", "Entry side filter: Buy for long, Sell for short, unset for both", "Trading");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Finished candles supplying RMI, SMA and ATR", "General");
		OrderRegistering += order => _pendingOrder = order;
	}

	/// <summary>Positive native RMI rolling mean period.</summary>
	public int RmiPeriod { get => _rmiPeriod.Value; set => _rmiPeriod.Value = value; }

	/// <summary>Positive close-price momentum lag, in finished candles.</summary>
	public int RmiMomentum { get => _rmiMomentum.Value; set => _rmiMomentum.Value = value; }

	/// <summary>Positive shared SMA and Wilder ATR period.</summary>
	public int SuperTrendLength { get => _superTrendLength.Value; set => _superTrendLength.Value = value; }

	/// <summary>Positive ATR multiplier of the trailing stop.</summary>
	public decimal SuperTrendMultiplier { get => _superTrendMultiplier.Value; set => _superTrendMultiplier.Value = value; }

	/// <summary>Allowed entry side; null permits both. Stops always close the held side.</summary>
	public Sides? Direction { get => _direction.Value; set => _direction.Value = value; }

	/// <summary>Finished candle type; five minutes by default.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	private void ResetState()
	{
		_stopPrice = null;
		_pendingOrder = null;
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
		if (RmiPeriod <= 0 || RmiMomentum <= 0 || SuperTrendLength <= 0 || SuperTrendMultiplier <= 0m
			|| Direction is Sides direction && direction is not (Sides.Buy or Sides.Sell))
			throw new ArgumentException("Periods and ATR multiplier must be positive; direction must be Buy, Sell or unset for both.");
		ResetState();
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveQuote).Start();
		}
		var rmi = new RelativeMomentumIndex { Length = RmiPeriod, MomentumPeriod = RmiMomentum };
		var mean = new SimpleMovingAverage { Length = SuperTrendLength };
		var atr = new AverageTrueRange { Length = SuperTrendLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx([rmi, mean, atr], ProcessCandle, allowEmpty: true).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, mean);
			DrawOwnTrades(area);
		}
	}

	private bool IsPending => _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed);

	private void Submit(Sides side, decimal volume, string comment)
		=> RegisterOrder(new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Type = OrderTypes.Market,
			Side = side,
			Volume = volume,
			Comment = comment,
		});

	private bool TryStopExit(decimal price)
	{
		if (Position == 0m || _stopPrice is not decimal stop || IsPending || !IsFormedAndOnlineAndAllowTrading())
			return false;
		if (Position > 0m ? price > stop : price < stop)
			return false;
		Submit(Position > 0m ? Sides.Sell : Sides.Buy, Math.Abs(Position), "RMI ATR trailing stop");
		return true;
	}

	private void ObserveQuote(Level1ChangeMessage quote)
	{
		var field = Position > 0m ? Level1Fields.BestBidPrice : Level1Fields.BestAskPrice;
		if (quote.TryGetDecimal(field) is decimal price && price > 0m)
			TryStopExit(price);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished || values[1].IsEmpty || !values[1].IsFormed
			|| values[2].IsEmpty || !values[2].IsFormed)
			return;
		var mean = values[1].ToDecimal();
		var width = values[2].ToDecimal() * SuperTrendMultiplier;
		if (Position != 0m)
		{
			var candidate = Position > 0m ? mean - width : mean + width;
			_stopPrice = _stopPrice is decimal previous ? Position > 0m ? Math.Max(previous, candidate) : Math.Min(previous, candidate) : candidate;
			TryStopExit(candle.ClosePrice);
			return;
		}
		if (values[0].IsEmpty || !values[0].IsFormed || IsPending || !IsFormedAndOnlineAndAllowTrading())
			return;
		var rmi = values[0].ToDecimal();
		Sides? side = rmi > 60m && candle.ClosePrice > mean ? Sides.Buy
			: rmi < 40m && candle.ClosePrice < mean ? Sides.Sell : null;
		if (side is not Sides entry || Direction is Sides allowed && entry != allowed)
			return;
		_stopPrice = entry == Sides.Buy ? mean - width : mean + width;
		Submit(entry, Volume, "RMI SMA aligned entry");
	}
}
