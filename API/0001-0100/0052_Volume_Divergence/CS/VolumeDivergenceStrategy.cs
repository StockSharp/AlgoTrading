using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Buys a lower close and sells a higher close when volume rises over the previous candle.
/// Fully exits when Close crosses the SMA in either direction or on actual-fill entry-ATR protection.
/// </summary>
public class VolumeDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _ma;
	private AverageTrueRange _atr;
	private decimal? _previousClose;
	private decimal? _previousVolume;
	private decimal? _previousMa;
	private Order _pendingOrder;
	private Unit _stopDistance;
	private bool _protectionStarted;

	public int MAPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int ATRPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal StopLossATRMultiplier { get => _stopLossAtrMultiplier.Value; set => _stopLossAtrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VolumeDivergenceStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Current-inclusive Close SMA length", "Indicators")
			.SetOptimize(10, 50, 10);
		_atrPeriod = Param(nameof(ATRPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Wilder ATR length for actual-fill protection", "Indicators")
			.SetOptimize(7, 28, 7);
		_stopLossAtrMultiplier = Param(nameof(StopLossATRMultiplier), 2m).SetNotNegative()
			.SetDisplay("ATR Stop Multiplier", "Frozen signal ATR distance from actual fills; zero disables it", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		OrderRegistering += order => _pendingOrder = order;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ClearSignalState();
	}

	private void ClearSignalState()
	{
		_ma = null;
		_atr = null;
		_previousClose = null;
		_previousVolume = null;
		_previousMa = null;
		_pendingOrder = null;
		_stopDistance = null;
		_protectionStarted = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ClearSignalState();
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		_ma = new SimpleMovingAverage { Length = MAPeriod };
		_atr = new AverageTrueRange { Length = ATRPeriod };
		Indicators.Add(_ma);
		Indicators.Add(_atr);
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before this callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var maValue = _ma.Process(candle);
		var atrValue = _atr.Process(candle);
		decimal? ma = !maValue.IsEmpty && _ma.IsFormed ? maValue.GetValue<decimal>() : null;
		decimal? atr = !atrValue.IsEmpty && _atr.IsFormed ? atrValue.GetValue<decimal>() : null;
		var close = candle.ClosePrice;
		var volume = candle.TotalVolume;
		var priceDown = _previousClose is decimal down && close < down;
		var priceUp = _previousClose is decimal up && close > up;
		var volumeUp = _previousVolume is decimal priorVolume && volume > priorVolume;
		var maUp = _previousClose is decimal priorUpClose && _previousMa is decimal priorUpMa &&
			ma is decimal currentUpMa && priorUpClose <= priorUpMa && close > currentUpMa;
		var maDown = _previousClose is decimal priorDownClose && _previousMa is decimal priorDownMa &&
			ma is decimal currentDownMa && priorDownClose >= priorDownMa && close < currentDownMa;
		// Warmup and unavailable trading still advance raw price/volume and formed MA history.
		_previousClose = close;
		_previousVolume = volume;
		_previousMa = ma;
		if (ma is null || atr is not decimal currentAtr ||
			!IsFormedAndOnlineAndAllowTrading() ||
			_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		var maCross = maUp || maDown;
		if (Position > 0m && maCross) SellMarket(Position);
		else if (Position < 0m && maCross) BuyMarket(Math.Abs(Position));
		else if (Position == 0m)
		{
			if (priceDown && volumeUp) Enter(Sides.Buy, currentAtr);
			else if (priceUp && volumeUp) Enter(Sides.Sell, currentAtr);
		}
	}

	private void Enter(Sides side, decimal atr)
	{
		var distance = atr * StopLossATRMultiplier;
		_stopDistance ??= new Unit(distance);
		// The cached native controller keeps this same Unit reference across entries.
		_stopDistance.Value = distance;
		if (!_protectionStarted && distance > 0m)
		{
			StartProtection(new Unit(), _stopDistance, useMarketOrders: true, isLocalStop: true);
			_protectionStarted = true;
		}
		RegisterOrder(new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Type = OrderTypes.Market,
			Side = side,
			Volume = Volume,
			Comment = "Volume divergence entry",
		});
	}
}
