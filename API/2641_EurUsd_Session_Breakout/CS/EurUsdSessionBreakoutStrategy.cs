using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that trades the US session after a tight EU session range.
/// </summary>
public class EurUsdSessionBreakoutStrategy : Strategy
{
	private const int EuSessionLengthBars = 24;

	// Strategy parameters
	private readonly StrategyParam<int> _startHourEuSession;
	private readonly StrategyParam<int> _startHourUsSession;
	private readonly StrategyParam<int> _endHourUsSession;
	private readonly StrategyParam<decimal> _smallSessionPips;
	private readonly StrategyParam<bool> _tradeOnMonday;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _breakoutBufferPoints;
	private readonly StrategyParam<DataType> _candleType;

	// Indicators and cached values
	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private decimal _currentHighest;
	private decimal _currentLowest;
	private decimal _euSessionHigh;
	private decimal _euSessionLow;
	private bool _sessionFound;
	private bool _smallSession;
	private bool _longOpened;
	private bool _shortOpened;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private DateTime _currentDate;

	// Price conversions
	private decimal _pipSize;
	private decimal _smallSessionThreshold;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _breakoutBuffer;

	public EurUsdSessionBreakoutStrategy()
	{
		_startHourEuSession = Param(nameof(StartHourEuSession), 5)
			.SetDisplay("EU Session Start", "Start hour of the EU session", "Schedule")
			.SetCanOptimize();

		_startHourUsSession = Param(nameof(StartHourUsSession), 2)
			.SetDisplay("US Session Start", "Start hour of the US session", "Schedule")
			.SetCanOptimize();

		_endHourUsSession = Param(nameof(EndHourUsSession), 16)
			.SetDisplay("US Session End", "End hour of the US session", "Schedule")
			.SetCanOptimize();

		_smallSessionPips = Param(nameof(SmallSessionPips), 72m)
			.SetDisplay("Small EU Session (pips)", "Maximum EU session range to trade", "Risk")
			.SetCanOptimize();

		_tradeOnMonday = Param(nameof(TradeOnMonday), false)
			.SetDisplay("Trade On Monday", "Allow trading on Mondays", "Schedule");

		_stopLossPips = Param(nameof(StopLossPips), 12m)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
			.SetCanOptimize();

		_takeProfitPips = Param(nameof(TakeProfitPips), 15m)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
			.SetCanOptimize();

		_breakoutBufferPoints = Param(nameof(BreakoutBufferPoints), 3m)
			.SetDisplay("Breakout Buffer (points)", "Extra points added to the breakout trigger", "Entries")
			.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for calculations", "General");
	}

	public int StartHourEuSession
	{
		get => _startHourEuSession.Value;
		set => _startHourEuSession.Value = value;
	}

	public int StartHourUsSession
	{
		get => _startHourUsSession.Value;
		set => _startHourUsSession.Value = value;
	}

	public int EndHourUsSession
	{
		get => _endHourUsSession.Value;
		set => _endHourUsSession.Value = value;
	}

	public decimal SmallSessionPips
	{
		get => _smallSessionPips.Value;
		set => _smallSessionPips.Value = value;
	}

	public bool TradeOnMonday
	{
		get => _tradeOnMonday.Value;
		set => _tradeOnMonday.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal BreakoutBufferPoints
	{
		get => _breakoutBufferPoints.Value;
		set => _breakoutBufferPoints.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Convert pip-based parameters into price distances using the instrument metadata.
		_pipSize = GetAdjustedPoint();
		_smallSessionThreshold = SmallSessionPips * _pipSize;
		_stopLossDistance = StopLossPips * _pipSize;
		_takeProfitDistance = TakeProfitPips * _pipSize;
		_breakoutBuffer = GetPriceStep() * BreakoutBufferPoints;

		// Prepare rolling highest/lowest indicators that emulate the 24-bar EU session range.
		_highest = new Highest { Length = EuSessionLengthBars };
		_lowest = new Lowest { Length = EuSessionLengthBars };

		// Reset the per-day state before processing real data.
		ResetDailyState(time.Date);

		// Subscribe to candles and route them through the processing pipeline.
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manage protective exits before we alter any daily state.
		ManageActivePosition(candle);

		// Detect a new trading day to mimic the static variable reset from the MQL version.
		var candleDate = candle.OpenTime.Date;
		if (candleDate != _currentDate)
			ResetDailyState(candleDate);

		// Skip signal evaluation on forbidden weekdays.
		if (!IsTradingDay(candle.OpenTime.DayOfWeek))
			return;

		// Update the highest/lowest trackers before making decisions.
		var previousHighest = _currentHighest;
		var previousLowest = _currentLowest;

		var highestValue = _highest.Process(candle).ToDecimal();
		var lowestValue = _lowest.Process(candle).ToDecimal();

		_currentHighest = highestValue;
		_currentLowest = lowestValue;

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		var hour = candle.OpenTime.Hour;

		// Capture the EU session range once the configured US session hour begins.
		if (!_sessionFound && hour == StartHourUsSession)
		{
			_euSessionHigh = previousHighest;
			_euSessionLow = previousLowest;

			if (_euSessionHigh <= 0m || _euSessionLow <= 0m)
				return;

			_smallSession = (_euSessionHigh - _euSessionLow) <= _smallSessionThreshold;
			_sessionFound = true;
		}

		// Trade only if the EU session was calm and we are within the US session window.
		if (!_sessionFound || !_smallSession)
			return;

		if (hour < StartHourUsSession || hour >= EndHourUsSession)
			return;

		if (hour <= StartHourEuSession + 5 || hour >= StartHourEuSession + 10)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		var breakoutHigh = _euSessionHigh + _breakoutBuffer;
		var breakoutLow = _euSessionLow - _breakoutBuffer;

		// Go long when the previous bar stayed above the EU range plus the buffer.
		if (!_longOpened && candle.LowPrice > breakoutHigh)
		{
			BuyMarket(Volume);
			SetLongTargets(candle.ClosePrice);
			_longOpened = true;
		}
		// Go short when the previous bar closed fully below the EU range minus the buffer.
		else if (!_shortOpened && candle.HighPrice < breakoutLow)
		{
			SellMarket(Volume);
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
			var exitByStop = _stopLossDistance > 0m && candle.LowPrice <= _stopPrice;
			var exitByTake = _takeProfitDistance > 0m && candle.HighPrice >= _takePrice;

			if (exitByStop || exitByTake)
			{
				SellMarket(Math.Abs(Position));
				ClearTargets();
			}
		}
		else if (Position < 0)
		{
			var exitByStop = _stopLossDistance > 0m && candle.HighPrice >= _stopPrice;
			var exitByTake = _takeProfitDistance > 0m && candle.LowPrice <= _takePrice;

			if (exitByStop || exitByTake)
			{
				BuyMarket(Math.Abs(Position));
				ClearTargets();
			}
		}
	}

	private void SetLongTargets(decimal entryPrice)
	{
		_entryPrice = entryPrice;
		_stopPrice = entryPrice - _stopLossDistance;
		_takePrice = entryPrice + _takeProfitDistance;
	}

	private void SetShortTargets(decimal entryPrice)
	{
		_entryPrice = entryPrice;
		_stopPrice = entryPrice + _stopLossDistance;
		_takePrice = entryPrice - _takeProfitDistance;
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
		_euSessionHigh = 0m;
		_euSessionLow = 0m;
		_currentHighest = 0m;
		_currentLowest = 0m;

		if (Position == 0)
			ClearTargets();

		_highest?.Reset();
		_lowest?.Reset();
	}

	private bool IsTradingDay(DayOfWeek dayOfWeek)
	{
		if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
			return false;

		if (dayOfWeek == DayOfWeek.Monday && !TradeOnMonday)
			return false;

		return true;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep;
		if (step.HasValue && step.Value > 0m)
			return step.Value;

		return CalculateStepFromDecimals();
	}

	private decimal GetAdjustedPoint()
	{
		var baseStep = GetPriceStep();
		var decimals = Security?.Decimals ?? 4;
		var adjust = (decimals == 3 || decimals == 5) ? 10m : 1m;
		return baseStep * adjust;
	}

	private decimal CalculateStepFromDecimals()
	{
		var decimals = Security?.Decimals ?? 4;
		decimal step = 1m;
		for (var i = 0; i < decimals; i++)
			step /= 10m;
		return step;
	}
}

