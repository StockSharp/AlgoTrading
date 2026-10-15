using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades actual fast/slow native TotalVolume SMA crosses with Close/SMA entry confirmation.
/// Fully exits on the opposite volume cross or actual-fill percent protection.
/// </summary>
public class VolumeMAXrossStrategy : Strategy
{
	private readonly StrategyParam<int> _priceMaPeriod;
	private readonly StrategyParam<int> _fastVolumeMaLength;
	private readonly StrategyParam<int> _slowVolumeMaLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private SimpleMovingAverage _priceMa;
	private SimpleMovingAverage _fastVolumeMa;
	private SimpleMovingAverage _slowVolumeMa;
	private decimal? _previousFast;
	private decimal? _previousSlow;
	private Order _pendingOrder;

	public int PriceMaPeriod { get => _priceMaPeriod.Value; set => _priceMaPeriod.Value = value; }
	public int FastVolumeMALength { get => _fastVolumeMaLength.Value; set => _fastVolumeMaLength.Value = value; }
	public int SlowVolumeMALength { get => _slowVolumeMaLength.Value; set => _slowVolumeMaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	public VolumeMAXrossStrategy()
	{
		_priceMaPeriod = Param(nameof(PriceMaPeriod), 20).SetGreaterThanZero()
			.SetDisplay("Price MA Period", "Current-inclusive Close SMA entry filter", "Indicators");
		_fastVolumeMaLength = Param(nameof(FastVolumeMALength), 10).SetGreaterThanZero()
			.SetDisplay("Fast Volume MA Length", "Current-inclusive fast TotalVolume SMA length", "Indicators");
		_slowVolumeMaLength = Param(nameof(SlowVolumeMALength), 50).SetGreaterThanZero()
			.SetDisplay("Slow Volume MA Length", "Current-inclusive slow TotalVolume SMA length", "Indicators");
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
		_priceMa = null;
		_fastVolumeMa = null;
		_slowVolumeMa = null;
		_previousFast = null;
		_previousSlow = null;
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
		_priceMa = new SimpleMovingAverage { Length = PriceMaPeriod, Name = "Price SMA" };
		_fastVolumeMa = new SimpleMovingAverage { Length = FastVolumeMALength, Name = "Fast volume SMA" };
		_slowVolumeMa = new SimpleMovingAverage { Length = SlowVolumeMALength, Name = "Slow volume SMA" };
		Indicators.Add(_priceMa);
		Indicators.Add(_fastVolumeMa);
		Indicators.Add(_slowVolumeMa);
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _priceMa);
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
		var priceValue = _priceMa.Process(candle);
		// Process actual candle TotalVolume, not a price or previous-volume proxy.
		var fastValue = _fastVolumeMa.Process(new DecimalIndicatorValue(_fastVolumeMa, candle.TotalVolume, candle.OpenTime) { IsFinal = true });
		var slowValue = _slowVolumeMa.Process(new DecimalIndicatorValue(_slowVolumeMa, candle.TotalVolume, candle.OpenTime) { IsFinal = true });
		decimal? priceMa = !priceValue.IsEmpty && _priceMa.IsFormed ? priceValue.GetValue<decimal>() : null;
		decimal? fast = !fastValue.IsEmpty && _fastVolumeMa.IsFormed ? fastValue.GetValue<decimal>() : null;
		decimal? slow = !slowValue.IsEmpty && _slowVolumeMa.IsFormed ? slowValue.GetValue<decimal>() : null;
		var up = _previousFast is decimal oldFastUp && _previousSlow is decimal oldSlowUp &&
			fast is decimal currentFastUp && slow is decimal currentSlowUp &&
			oldFastUp <= oldSlowUp && currentFastUp > currentSlowUp;
		var down = _previousFast is decimal oldFastDown && _previousSlow is decimal oldSlowDown &&
			fast is decimal currentFastDown && slow is decimal currentSlowDown &&
			oldFastDown >= oldSlowDown && currentFastDown < currentSlowDown;
		// Independently formed values seed the next cross even while trading is unavailable.
		_previousFast = fast;
		_previousSlow = slow;
		if (!IsFormedAndOnlineAndAllowTrading() ||
			_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position > 0m && down) SellMarket(Position);
		else if (Position < 0m && up) BuyMarket(Math.Abs(Position));
		else if (Position == 0m && priceMa is decimal mean)
		{
			if (up && candle.ClosePrice > mean) BuyMarket(Volume);
			else if (down && candle.ClosePrice < mean) SellMarket(Volume);
		}
	}
}
