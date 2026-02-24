using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that trades after a tight consolidation range.
/// Captures the range from a "quiet" session, then trades breakouts in the following session.
/// </summary>
public class EurUsdSessionBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _euSessionLengthBars;
	private readonly StrategyParam<int> _startHourRangeSession;
	private readonly StrategyParam<int> _startHourTradeSession;
	private readonly StrategyParam<int> _endHourTradeSession;
	private readonly StrategyParam<decimal> _smallSessionThreshold;
	private readonly StrategyParam<decimal> _stopLossDistance;
	private readonly StrategyParam<decimal> _takeProfitDistance;
	private readonly StrategyParam<decimal> _breakoutBuffer;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private decimal _currentHighest;
	private decimal _currentLowest;
	private decimal _rangeSessionHigh;
	private decimal _rangeSessionLow;
	private bool _sessionFound;
	private bool _smallSession;
	private bool _longOpened;
	private bool _shortOpened;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private DateTime _currentDate;

	public EurUsdSessionBreakoutStrategy()
	{
		_startHourRangeSession = Param(nameof(StartHourRangeSession), 0)
			.SetDisplay("Range Session Start", "Start hour of the consolidation range session", "Schedule");

		_startHourTradeSession = Param(nameof(StartHourTradeSession), 8)
			.SetDisplay("Trade Session Start", "Start hour of the trading session", "Schedule");

		_endHourTradeSession = Param(nameof(EndHourTradeSession), 20)
			.SetDisplay("Trade Session End", "End hour of the trading session", "Schedule");

		_smallSessionThreshold = Param(nameof(SmallSessionThreshold), 2000m)
			.SetDisplay("Small Session Threshold", "Maximum range session price range to trigger trading", "Risk");

		_stopLossDistance = Param(nameof(StopLossDistance), 500m)
			.SetDisplay("Stop Loss Distance", "Stop loss distance in price units", "Risk");

		_takeProfitDistance = Param(nameof(TakeProfitDistance), 800m)
			.SetDisplay("Take Profit Distance", "Take profit distance in price units", "Risk");

		_breakoutBuffer = Param(nameof(BreakoutBuffer), 50m)
			.SetDisplay("Breakout Buffer", "Extra price buffer added to breakout trigger", "Entries");

		_euSessionLengthBars = Param(nameof(EuSessionLengthBars), 12)
			.SetRange(1, 72)
			.SetDisplay("Range Session Length (bars)", "Number of bars representing the range session", "Schedule");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for calculations", "General");
	}

	public int StartHourRangeSession
	{
		get => _startHourRangeSession.Value;
		set => _startHourRangeSession.Value = value;
	}

	public int StartHourTradeSession
	{
		get => _startHourTradeSession.Value;
		set => _startHourTradeSession.Value = value;
	}

	public int EndHourTradeSession
	{
		get => _endHourTradeSession.Value;
		set => _endHourTradeSession.Value = value;
	}

	public decimal SmallSessionThreshold
	{
		get => _smallSessionThreshold.Value;
		set => _smallSessionThreshold.Value = value;
	}

	public decimal StopLossDistance
	{
		get => _stopLossDistance.Value;
		set => _stopLossDistance.Value = value;
	}

	public decimal TakeProfitDistance
	{
		get => _takeProfitDistance.Value;
		set => _takeProfitDistance.Value = value;
	}

	public decimal BreakoutBuffer
	{
		get => _breakoutBuffer.Value;
		set => _breakoutBuffer.Value = value;
	}

	public int EuSessionLengthBars
	{
		get => _euSessionLengthBars.Value;
		set => _euSessionLengthBars.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = EuSessionLengthBars };
		_lowest = new Lowest { Length = EuSessionLengthBars };

		_currentDate = time.Date;
		_sessionFound = false;
		_smallSession = false;
		_longOpened = false;
		_shortOpened = false;

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

		// Manage protective exits first
		ManageActivePosition(candle);

		// Detect a new day
		var candleDate = candle.OpenTime.Date;
		if (candleDate != _currentDate)
			ResetDailyState(candleDate);

		// Update rolling highest/lowest (do NOT reset daily - keep them rolling)
		var previousHighest = _currentHighest;
		var previousLowest = _currentLowest;

		var highestValue = _highest.Process(candle).ToDecimal();
		var lowestValue = _lowest.Process(candle).ToDecimal();

		_currentHighest = highestValue;
		_currentLowest = lowestValue;

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		var hour = candle.OpenTime.Hour;

		// Capture the range session high/low when the trading session starts
		if (!_sessionFound && hour >= StartHourTradeSession && previousHighest > 0 && previousLowest > 0)
		{
			_rangeSessionHigh = previousHighest;
			_rangeSessionLow = previousLowest;

			_smallSession = (_rangeSessionHigh - _rangeSessionLow) <= SmallSessionThreshold;
			_sessionFound = true;
		}

		// Trade only if the range session was calm and we are within the trade session window
		if (!_sessionFound || !_smallSession)
			return;

		if (hour < StartHourTradeSession || hour >= EndHourTradeSession)
			return;

		if (Position != 0)
			return;

		var breakoutHigh = _rangeSessionHigh + BreakoutBuffer;
		var breakoutLow = _rangeSessionLow - BreakoutBuffer;

		// Go long when the bar is fully above the range plus buffer
		if (!_longOpened && candle.LowPrice > breakoutHigh)
		{
			BuyMarket();
			SetLongTargets(candle.ClosePrice);
			_longOpened = true;
		}
		// Go short when the bar is fully below the range minus buffer
		else if (!_shortOpened && candle.HighPrice < breakoutLow)
		{
			SellMarket();
			SetShortTargets(candle.ClosePrice);
			_shortOpened = true;
		}
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position == 0)
			return;

		if (Position > 0)
		{
			var exitByStop = StopLossDistance > 0m && candle.LowPrice <= _stopPrice;
			var exitByTake = TakeProfitDistance > 0m && candle.HighPrice >= _takePrice;

			if (exitByStop || exitByTake)
			{
				SellMarket();
				ClearTargets();
			}
		}
		else if (Position < 0)
		{
			var exitByStop = StopLossDistance > 0m && candle.HighPrice >= _stopPrice;
			var exitByTake = TakeProfitDistance > 0m && candle.LowPrice <= _takePrice;

			if (exitByStop || exitByTake)
			{
				BuyMarket();
				ClearTargets();
			}
		}
	}

	private void SetLongTargets(decimal entryPrice)
	{
		_entryPrice = entryPrice;
		_stopPrice = entryPrice - StopLossDistance;
		_takePrice = entryPrice + TakeProfitDistance;
	}

	private void SetShortTargets(decimal entryPrice)
	{
		_entryPrice = entryPrice;
		_stopPrice = entryPrice + StopLossDistance;
		_takePrice = entryPrice - TakeProfitDistance;
	}

	private void ClearTargets()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	private void ResetDailyState(DateTime date)
	{
		_currentDate = date;
		_sessionFound = false;
		_smallSession = false;
		_longOpened = false;
		_shortOpened = false;
		_rangeSessionHigh = 0m;
		_rangeSessionLow = 0m;

		if (Position == 0)
			ClearTargets();
	}
}
