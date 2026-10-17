using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean-reversion Bollinger entries gated by formed ADX; the last executed
/// entry direction limits later entries. Native SL/TP uses real entry fills.
/// </summary>
public class WellMartinStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxLevel;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private readonly Queue<decimal> _closes = new();
	private Order _pending, _entryOrder;
	private Sides? _lastExecutedEntrySide;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public decimal BollingerWidth { get => _bollingerWidth.Value; set => _bollingerWidth.Value = value; }
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal AdxLevel { get => _adxLevel.Value; set => _adxLevel.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	public WellMartinStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Signal candle timeframe", "General");
		_bollingerPeriod = Param(nameof(BollingerPeriod), 84).SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Mean and deviation period", "Indicator");
		_bollingerWidth = Param(nameof(BollingerWidth), 1.8m).SetGreaterThanZero()
			.SetDisplay("Bollinger Width", "Deviation multiplier", "Indicator");
		_adxPeriod = Param(nameof(AdxPeriod), 14).SetGreaterThanZero()
			.SetDisplay("ADX Period", "Trend strength smoothing period", "Indicator");
		_adxLevel = Param(nameof(AdxLevel), 25m).SetNotNegative()
			.SetDisplay("ADX Level", "Maximum allowed trend strength", "Indicator");
		_takeProfit = Param(nameof(TakeProfit), 1200m).SetNotNegative()
			.SetDisplay("Take Profit", "Price-unit target", "Risk");
		_stopLoss = Param(nameof(StopLoss), 1400m).SetNotNegative()
			.SetDisplay("Stop Loss", "Price-unit risk", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
		_pending = _entryOrder = null;
		_lastExecutedEntrySide = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_closes.Clear();
		_pending = _entryOrder = null;
		_lastExecutedEntrySide = null;
		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute));

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		SubscribeCandles(CandleType).BindEx(adx, ProcessCandle).Start();
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		// Submitted orders are not fills: direction changes only after own trade execution.
		if (trade.Order is not null && trade.Order == _entryOrder)
			_lastExecutedEntrySide = trade.Order.Side;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Enqueue(candle.ClosePrice);
		if (_closes.Count > BollingerPeriod)
			_closes.Dequeue();
		if (_closes.Count < BollingerPeriod)
			return;

		if (!adxValue.IsFormed || adxValue is not AverageDirectionalIndexValue adx
			|| adx.MovingAverage is not decimal strength)
			return;

		var mean = _closes.Average();
		var variance = _closes.Average(price => (price - mean) * (price - mean));
		var deviation = (decimal)Math.Sqrt((double)variance);
		var lower = mean - BollingerWidth * deviation;
		var upper = mean + BollingerWidth * deviation;

		if (!IsFormedAndOnlineAndAllowTrading()
			|| (_pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed)))
			return;

		if (Position > 0m)
		{
			if (candle.ClosePrice >= upper)
				_pending = SellMarket(Math.Abs(Position));
			return;
		}
		if (Position < 0m)
		{
			if (candle.ClosePrice <= lower)
				_pending = BuyMarket(Math.Abs(Position));
			return;
		}
		if (strength >= AdxLevel)
			return;

		if (candle.ClosePrice < lower
			&& (_lastExecutedEntrySide is null || _lastExecutedEntrySide == Sides.Buy))
		{
			_entryOrder = _pending = BuyMarket(Volume);
		}
		else if (candle.ClosePrice > upper
			&& (_lastExecutedEntrySide is null || _lastExecutedEntrySide == Sides.Sell))
		{
			_entryOrder = _pending = SellMarket(Volume);
		}
	}
}
