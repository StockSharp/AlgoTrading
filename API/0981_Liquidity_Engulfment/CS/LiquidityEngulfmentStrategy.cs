using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidity and Engulfment combination strategy.
/// </summary>
public class LiquidityEngulfmentStrategy : Strategy
{
	public enum TradeMode
	{
		Both,
		BullishOnly,
		BearishOnly
	}

	private readonly StrategyParam<TradeMode> _mode;
	private readonly StrategyParam<int> _upperLookback;
	private readonly StrategyParam<int> _lowerLookback;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<bool> _enableTakeProfit;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Highest _highest;
	private readonly Lowest _lowest;

	private ICandleMessage _prev;
	private decimal? _lastBullOpen;
	private decimal? _lastBearOpen;
	private int _lastBullIndex;
	private int _lastBearIndex;
	private string _lastSignal = string.Empty;
	private bool _touchedLower;
	private bool _touchedUpper;
	private bool _lockedBull;
	private bool _lockedBear;
	private int _sinceTouch = -1;
	private decimal _entryPrice;
	private DateTimeOffset _entryTime;
	private int _index;

	public TradeMode Mode { get => _mode.Value; set => _mode.Value = value; }
	public int UpperLookback { get => _upperLookback.Value; set => _upperLookback.Value = value; }
	public int LowerLookback { get => _lowerLookback.Value; set => _lowerLookback.Value = value; }
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public bool EnableTakeProfit { get => _enableTakeProfit.Value; set => _enableTakeProfit.Value = value; }
	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }
	public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LiquidityEngulfmentStrategy()
	{
		_mode = Param(nameof(Mode), TradeMode.Both).SetDisplay("Mode", "Trading mode", "General");
		_upperLookback = Param(nameof(UpperLookback), 10).SetGreaterThanZero().SetDisplay("Upper Lookback", "Upper liquidity", "Indicators").SetCanOptimize(true).SetOptimize(5, 20, 1);
		_lowerLookback = Param(nameof(LowerLookback), 10).SetGreaterThanZero().SetDisplay("Lower Lookback", "Lower liquidity", "Indicators").SetCanOptimize(true).SetOptimize(5, 20, 1);
		_stopLossPips = Param(nameof(StopLossPips), 10).SetGreaterThanZero().SetDisplay("Stop Loss", "Stop in pips", "Risk");
		_takeProfitPips = Param(nameof(TakeProfitPips), 20).SetGreaterThanZero().SetDisplay("Take Profit", "Target in pips", "Risk");
		_enableTakeProfit = Param(nameof(EnableTakeProfit), true).SetDisplay("Enable TP", "Use take profit", "Risk");
		_startDate = Param(nameof(StartDate), new DateTimeOffset(2024,1,1,0,0,0,TimeSpan.Zero)).SetDisplay("Start Date", "Backtest start", "General");
		_endDate = Param(nameof(EndDate), new DateTimeOffset(2024,12,31,23,59,0,TimeSpan.Zero)).SetDisplay("End Date", "Backtest end", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");

		_highest = new Highest { Length = UpperLookback };
		_lowest = new Lowest { Length = LowerLookback };
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prev = null;
		_lastBullOpen = null;
		_lastBearOpen = null;
		_lastBullIndex = 0;
		_lastBearIndex = 0;
		_lastSignal = string.Empty;
		_touchedLower = false;
		_touchedUpper = false;
		_lockedBull = false;
		_lockedBear = false;
		_sinceTouch = -1;
		_entryPrice = 0m;
		_entryTime = DateTimeOffset.MinValue;
		_index = 0;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest.Length = UpperLookback;
		_lowest.Length = LowerLookback;

		var sub = SubscribeCandles(CandleType);
		sub.ForEach(Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var highest = _highest.Process(candle.HighPrice, candle.OpenTime, true).ToDecimal();
		var lowest = _lowest.Process(candle.LowPrice, candle.OpenTime, true).ToDecimal();

		if (candle.LowPrice <= lowest)
			_touchedLower = true;
		if (candle.HighPrice >= highest)
			_touchedUpper = true;

		var bull = candle.ClosePrice > candle.OpenPrice;
		var bear = candle.ClosePrice < candle.OpenPrice;

		if (bull)
		{
			_lastBullOpen = candle.OpenPrice;
			_lastBullIndex = _index;
		}
		else if (bear)
		{
			_lastBearOpen = candle.OpenPrice;
			_lastBearIndex = _index;
		}

		var bullEngulf = _lastBearOpen.HasValue && _prev != null && candle.ClosePrice > _lastBearOpen && candle.ClosePrice > _prev.LowPrice && _index > _lastBearIndex;
		var bearEngulf = _lastBullOpen.HasValue && _prev != null && candle.ClosePrice < _lastBullOpen && candle.ClosePrice < _prev.HighPrice && _index > _lastBullIndex;

		var bullSignal = bullEngulf && _lastSignal != "bullish" && _touchedLower && !_lockedBull;
		var bearSignal = bearEngulf && _lastSignal != "bearish" && _touchedUpper && !_lockedBear;

		if (bullEngulf)
			_lastSignal = "bullish";
		if (bearEngulf)
			_lastSignal = "bearish";

		if (bullSignal)
		{
			_lockedBull = true;
			_touchedLower = false;
			_sinceTouch = 0;
		}
		if (bearSignal)
		{
			_lockedBear = true;
			_touchedUpper = false;
			_sinceTouch = 0;
		}

		if (_sinceTouch >= 0)
			_sinceTouch++;
		if (_sinceTouch >= 3)
		{
			_lockedBull = false;
			_lockedBear = false;
		}
		if (_touchedLower)
			_lockedBull = false;
		if (_touchedUpper)
			_lockedBear = false;

		var inRange = candle.OpenTime >= StartDate && candle.OpenTime <= EndDate;
		var step = Security.PriceStep ?? 1m;
		var canLong = Mode != TradeMode.BearishOnly;
		var canShort = Mode != TradeMode.BullishOnly;

		if (inRange && canShort && bearSignal && Position >= 0)
		{
			SellMarket(Volume + Position);
			_entryPrice = candle.ClosePrice;
			_entryTime = candle.OpenTime;
		}
		else if (inRange && canLong && bullSignal && Position <= 0)
		{
			BuyMarket(Volume - Position);
			_entryPrice = candle.ClosePrice;
			_entryTime = candle.OpenTime;
		}
		else
		{
			if (Position < 0 && bullSignal && candle.OpenTime > _entryTime)
				BuyMarket(-Position);
			else if (Position > 0 && bearSignal && candle.OpenTime > _entryTime)
				SellMarket(Position);
		}

		if (Position > 0)
		{
			var stop = _entryPrice - StopLossPips * step;
			var tp = _entryPrice + TakeProfitPips * step;
			if (candle.ClosePrice <= stop || (EnableTakeProfit && candle.ClosePrice >= tp))
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			var stop = _entryPrice + StopLossPips * step;
			var tp = _entryPrice - TakeProfitPips * step;
			if (candle.ClosePrice >= stop || (EnableTakeProfit && candle.ClosePrice <= tp))
				BuyMarket(-Position);
		}

		_prev = candle;
		_index++;
	}
}
