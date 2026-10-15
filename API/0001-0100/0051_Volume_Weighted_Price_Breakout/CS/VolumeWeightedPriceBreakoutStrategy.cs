using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Enters on an actual Close/VWMA cross confirmed by the Close/SMA side.
/// Exits the full remaining position on an adverse Close/SMA cross or actual-fill protection.
/// </summary>
public class VolumeWeightedPriceBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _vwapPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private SimpleMovingAverage _ma;
	private VolumeWeightedMovingAverage _vwma;
	private decimal? _previousClose;
	private decimal? _previousMa;
	private decimal? _previousVwma;
	private Order _pendingOrder;

	public int MAPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	// This historical parameter name controls a rolling VWMA, not session VWAP.
	public int VWAPPeriod { get => _vwapPeriod.Value; set => _vwapPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	public VolumeWeightedPriceBreakoutStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Current-inclusive Close SMA length", "Indicators")
			.SetOptimize(10, 50, 10);
		_vwapPeriod = Param(nameof(VWAPPeriod), 20).SetGreaterThanZero()
			.SetDisplay("VWAP Period", "Current-inclusive rolling Close/volume VWMA length", "Indicators")
			.SetOptimize(10, 30, 5);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection");
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
		_vwma = null;
		_previousClose = null;
		_previousMa = null;
		_previousVwma = null;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ClearSignalState();
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		_ma = new SimpleMovingAverage { Length = MAPeriod };
		_vwma = new VolumeWeightedMovingAverage { Length = VWAPPeriod };
		Indicators.Add(_ma);
		Indicators.Add(_vwma);
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawIndicator(area, _vwma);
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
		var vwmaValue = _vwma.Process(candle);
		decimal? ma = !maValue.IsEmpty && _ma.IsFormed ? maValue.GetValue<decimal>() : null;
		decimal? vwma = !vwmaValue.IsEmpty && _vwma.IsFormed ? vwmaValue.GetValue<decimal>() : null;
		var close = candle.ClosePrice;
		var maUp = _previousClose is decimal priorMaUpClose && _previousMa is decimal priorMaUp &&
			ma is decimal currentMaUp && priorMaUpClose <= priorMaUp && close > currentMaUp;
		var maDown = _previousClose is decimal priorMaDownClose && _previousMa is decimal priorMaDown &&
			ma is decimal currentMaDown && priorMaDownClose >= priorMaDown && close < currentMaDown;
		var vwmaUp = _previousClose is decimal priorVwmaUpClose && _previousVwma is decimal priorVwmaUp &&
			vwma is decimal currentVwmaUp && priorVwmaUpClose <= priorVwmaUp && close > currentVwmaUp;
		var vwmaDown = _previousClose is decimal priorVwmaDownClose && _previousVwma is decimal priorVwmaDown &&
			vwma is decimal currentVwmaDown && priorVwmaDownClose >= priorVwmaDown && close < currentVwmaDown;
		// Raw close and each independently formed mean advance even while trading is disabled
		// or an order is pending. Undefined VWMA does not bridge a later cross.
		_previousClose = close;
		_previousMa = ma;
		_previousVwma = vwma;
		if (!IsFormedAndOnlineAndAllowTrading() ||
			_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position > 0m && maDown) SellMarket(Position);
		else if (Position < 0m && maUp) BuyMarket(Math.Abs(Position));
		else if (Position == 0m && ma is decimal confirmed)
		{
			if (vwmaUp && close > confirmed) BuyMarket(Volume);
			else if (vwmaDown && close < confirmed) SellMarket(Volume);
		}
	}
}
