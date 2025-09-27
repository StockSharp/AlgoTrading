namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class AscPlusPlusStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<int> _entryStopLevel;
	private readonly StrategyParam<int> _entryRange;
	private readonly StrategyParam<int> _riskLevel;
	private readonly StrategyParam<int> _signalConfirmation;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _trailingStopPoints;

	private WilliamsR _fastWpr = null!;
	private WilliamsR _slowWpr = null!;
	private AverageTrueRange _atr = null!;

	private decimal _priceStep;
	private int _consecutiveBuySignals;
	private int _consecutiveSellSignals;
	private decimal? _lastBuyStopPrice;
	private decimal? _lastSellStopPrice;

	private Order _buyStopOrder;
	private Order _sellStopOrder;

	public AscPlusPlusStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Base timeframe for candle subscription", "General");

		_fastLength = Param(nameof(FastLength), 9)
			.SetDisplay("Fast WPR Length", "Williams %R length for the fast oscillator", "Indicators")
			.SetCanOptimize(true);

		_slowLength = Param(nameof(SlowLength), 54)
			.SetDisplay("Slow WPR Length", "Williams %R length for the slow oscillator", "Indicators")
			.SetCanOptimize(true);

		_rangeLength = Param(nameof(RangeLength), 10)
			.SetDisplay("Range Length", "Averaging window for the true range filter", "Risk Management")
			.SetCanOptimize(true);

		_entryStopLevel = Param(nameof(EntryStopLevel), 10)
			.SetDisplay("Entry Offset (points)", "Offset in price steps for breakout entries", "Orders")
			.SetCanOptimize(true);

		_entryRange = Param(nameof(EntryRange), 27)
			.SetDisplay("Max Range (points)", "Maximum average range allowed before placing pending orders", "Risk Management")
			.SetCanOptimize(true);

		_riskLevel = Param(nameof(RiskLevel), 3)
			.SetDisplay("Risk Level", "Adjusts WPR confirmation thresholds", "Signals")
			.SetRange(1, 20)
			.SetCanOptimize(true);

		_signalConfirmation = Param(nameof(SignalConfirmation), 5)
			.SetDisplay("Signal Confirmations", "Number of consecutive candles required to confirm a signal", "Signals")
			.SetRange(1, 10)
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100)
			.SetDisplay("Take Profit (points)", "Protective take profit distance in price steps", "Risk Management")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 40)
			.SetDisplay("Stop Loss (points)", "Protective stop loss distance in price steps", "Risk Management")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 20)
			.SetDisplay("Trailing Stop (points)", "Enables trailing for the protective stop", "Risk Management")
			.SetCanOptimize(true);
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
	}

	public int EntryStopLevel
	{
		get => _entryStopLevel.Value;
		set => _entryStopLevel.Value = value;
	}

	public int EntryRange
	{
		get => _entryRange.Value;
		set => _entryRange.Value = value;
	}

	public int RiskLevel
	{
		get => _riskLevel.Value;
		set => _riskLevel.Value = value;
	}

	public int SignalConfirmation
	{
		get => _signalConfirmation.Value;
		set => _signalConfirmation.Value = value;
	}

	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public int TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;

		_fastWpr = new WilliamsR { Length = FastLength };
		_slowWpr = new WilliamsR { Length = SlowLength };
		_atr = new AverageTrueRange { Length = RangeLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastWpr, _slowWpr, _atr, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfitPoints * _priceStep, UnitTypes.Price),
			new Unit(StopLossPoints * _priceStep, UnitTypes.Price),
			isStopTrailing: TrailingStopPoints > 0
		);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastWprValue, decimal slowWprValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_fastWpr.IsFormed || !_slowWpr.IsFormed || !_atr.IsFormed)
			return;

		var averageRange = atrValue;
		var entryRangeLimit = EntryRange * _priceStep;

		if (averageRange <= 0 || averageRange > entryRangeLimit)
		{
			CancelPendingOrders();
			_consecutiveBuySignals = 0;
			_consecutiveSellSignals = 0;
			return;
		}

		var x1 = 67m + RiskLevel;
		var x2 = 33m - RiskLevel;
		var value2 = 100m - Math.Abs(fastWprValue);

		var buyTrigger = value2 <= x2;
		var sellTrigger = value2 >= x1;
		var momentumUp = fastWprValue > slowWprValue;
		var momentumDown = fastWprValue < slowWprValue;

		if (buyTrigger)
		{
			_consecutiveBuySignals++;
			_consecutiveSellSignals = 0;
			_lastBuyStopPrice = candle.HighPrice + averageRange * 0.5m + EntryStopLevel * _priceStep;
		}
		else if (sellTrigger)
		{
			_consecutiveSellSignals++;
			_consecutiveBuySignals = 0;
			_lastSellStopPrice = candle.LowPrice - averageRange * 0.5m - EntryStopLevel * _priceStep;
		}
		else
		{
			_consecutiveBuySignals = 0;
			_consecutiveSellSignals = 0;
		}

		if (buyTrigger && momentumUp && _consecutiveBuySignals >= SignalConfirmation)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (_lastBuyStopPrice is decimal buyStopPrice)
				PlaceBuyStop(buyStopPrice);
		}
		else if (sellTrigger && momentumDown && _consecutiveSellSignals >= SignalConfirmation)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			if (_lastSellStopPrice is decimal sellStopPrice)
				PlaceSellStop(sellStopPrice);
		}

		if (buyTrigger && Position < 0 && _consecutiveBuySignals >= SignalConfirmation)
			BuyMarket(Math.Abs(Position));

		if (sellTrigger && Position > 0 && _consecutiveSellSignals >= SignalConfirmation)
			SellMarket(Math.Abs(Position));
	}

	private void PlaceBuyStop(decimal price)
	{
		CancelOrder(_sellStopOrder);
		_sellStopOrder = null;

		var volume = Volume + Math.Max(0m, -Position);
		if (volume <= 0)
			return;

		if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
		{
			if (Math.Abs(_buyStopOrder.Price - price) <= _priceStep / 2m)
				return;

			CancelOrder(_buyStopOrder);
		}

		_buyStopOrder = BuyStop(volume, price);
	}

	private void PlaceSellStop(decimal price)
	{
		CancelOrder(_buyStopOrder);
		_buyStopOrder = null;

		var volume = Volume + Math.Max(0m, Position);
		if (volume <= 0)
			return;

		if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
		{
			if (Math.Abs(_sellStopOrder.Price - price) <= _priceStep / 2m)
				return;

			CancelOrder(_sellStopOrder);
		}

		_sellStopOrder = SellStop(volume, price);
	}

	private void CancelPendingOrders()
	{
		if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
			CancelOrder(_buyStopOrder);
		if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
			CancelOrder(_sellStopOrder);
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == null)
			return;

		if (order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			if (order == _buyStopOrder)
				_buyStopOrder = null;
			else if (order == _sellStopOrder)
				_sellStopOrder = null;
		}
	}
}

