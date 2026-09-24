using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum HurstFldTrigger
{
	Price,
	Signal,
	Trade,
	Trend,
}

/// <summary>
/// Hurst Future Lines of Demarcation strategy.
/// FLDs are represented by cycle-based displaced price lines; price crosses the signal
/// line only trade when the longer-cycle state agrees.
/// </summary>
public class HurstFutureLinesOfDemarcationStrategy : Strategy
{
	private readonly StrategyParam<bool> _smoothFld;
	private readonly StrategyParam<int> _fldSmoothing;
	private readonly StrategyParam<int> _signalCycleLength;
	private readonly StrategyParam<int> _tradeCycleLength;
	private readonly StrategyParam<int> _trendCycleLength;
	private readonly StrategyParam<HurstFldTrigger> _closeTrigger1;
	private readonly StrategyParam<HurstFldTrigger> _closeTrigger2;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = [];
	private decimal? _prevPrice;
	private decimal? _prevSignal;
	private decimal? _prevTrade;
	private decimal? _prevTrend;

	public bool SmoothFld { get => _smoothFld.Value; set => _smoothFld.Value = value; }
	public int FldSmoothing { get => _fldSmoothing.Value; set => _fldSmoothing.Value = value; }
	public int SignalCycleLength { get => _signalCycleLength.Value; set => _signalCycleLength.Value = value; }
	public int TradeCycleLength { get => _tradeCycleLength.Value; set => _tradeCycleLength.Value = value; }
	public int TrendCycleLength { get => _trendCycleLength.Value; set => _trendCycleLength.Value = value; }
	public HurstFldTrigger CloseTrigger1 { get => _closeTrigger1.Value; set => _closeTrigger1.Value = value; }
	public HurstFldTrigger CloseTrigger2 { get => _closeTrigger2.Value; set => _closeTrigger2.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public HurstFutureLinesOfDemarcationStrategy()
	{
		_smoothFld = Param(nameof(SmoothFld), false)
			.SetDisplay("Smooth FLD", "Smooth displaced FLD values.", "FLD");
		_fldSmoothing = Param(nameof(FldSmoothing), 5)
			.SetGreaterThanZero()
			.SetDisplay("FLD Smoothing", "Number of displaced observations used for smoothing.", "FLD");
		_signalCycleLength = Param(nameof(SignalCycleLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cycle", "Signal-cycle length.", "Cycles");
		_tradeCycleLength = Param(nameof(TradeCycleLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Trade Cycle", "Trade-cycle length.", "Cycles");
		_trendCycleLength = Param(nameof(TrendCycleLength), 80)
			.SetGreaterThanZero()
			.SetDisplay("Trend Cycle", "Trend-cycle length.", "Cycles");
		_closeTrigger1 = Param(nameof(CloseTrigger1), HurstFldTrigger.Price)
			.SetDisplay("Close Trigger 1", "First line used by the exit crossover.", "Exit");
		_closeTrigger2 = Param(nameof(CloseTrigger2), HurstFldTrigger.Trade)
			.SetDisplay("Close Trigger 2", "Second line used by the exit crossover.", "Exit");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used by Hurst FLDs.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
		_prevPrice = null;
		_prevSignal = null;
		_prevTrade = null;
		_prevTrend = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Add(candle.ClosePrice);

		var signal = GetFld(SignalCycleLength);
		var trade = GetFld(TradeCycleLength);
		var trend = GetFld(TrendCycleLength);

		if (signal is null || trade is null || trend is null)
		{
			TrimHistory();
			return;
		}

		var price = candle.ClosePrice;
		var state = GetTrendState(price, trade.Value, trend.Value);

		if (_prevPrice is decimal prevPrice &&
			_prevSignal is decimal prevSignal &&
			_prevTrade is decimal prevTrade &&
			_prevTrend is decimal prevTrend)
		{
			var trigger1 = Resolve(CloseTrigger1, price, signal.Value, trade.Value, trend.Value);
			var trigger2 = Resolve(CloseTrigger2, price, signal.Value, trade.Value, trend.Value);
			var prevTrigger1 = Resolve(CloseTrigger1, prevPrice, prevSignal, prevTrade, prevTrend);
			var prevTrigger2 = Resolve(CloseTrigger2, prevPrice, prevSignal, prevTrade, prevTrend);

			var exitLong = Position > 0 && prevTrigger1 >= prevTrigger2 && trigger1 < trigger2;
			var exitShort = Position < 0 && prevTrigger1 <= prevTrigger2 && trigger1 > trigger2;

			if (exitLong)
				SellMarket(Math.Abs(Position));
			else if (exitShort)
				BuyMarket(Math.Abs(Position));
			else
			{
				var crossUp = prevPrice <= prevSignal && price > signal.Value;
				var crossDown = prevPrice >= prevSignal && price < signal.Value;

				if (Position == 0 && crossUp && state == 1)
					BuyMarket();
				else if (Position == 0 && crossDown && state == 6)
					SellMarket();
			}
		}

		_prevPrice = price;
		_prevSignal = signal;
		_prevTrade = trade;
		_prevTrend = trend;
		TrimHistory();
	}

	private int GetTrendState(decimal price, decimal tradeFld, decimal trendFld)
	{
		if (price > tradeFld && tradeFld > trendFld)
			return 1;

		if (price < tradeFld && tradeFld < trendFld)
			return 6;

		return 0;
	}

	private decimal? GetFld(int cycleLength)
	{
		var displacement = Math.Max(1, cycleLength / 2);
		var target = _closes.Count - 1 - displacement;
		if (target < 0)
			return null;

		if (!SmoothFld)
			return _closes[target];

		var from = Math.Max(0, target - FldSmoothing + 1);
		var count = target - from + 1;
		return _closes.Skip(from).Take(count).Average();
	}

	private static decimal Resolve(HurstFldTrigger trigger, decimal price, decimal signal, decimal trade, decimal trend)
		=> trigger switch
		{
			HurstFldTrigger.Price => price,
			HurstFldTrigger.Signal => signal,
			HurstFldTrigger.Trade => trade,
			HurstFldTrigger.Trend => trend,
			_ => price,
		};

	private void TrimHistory()
	{
		var keep = Math.Max(TrendCycleLength, Math.Max(TradeCycleLength, SignalCycleLength)) + FldSmoothing + 4;
		if (_closes.Count > keep)
			_closes.RemoveRange(0, _closes.Count - keep);
	}
}
