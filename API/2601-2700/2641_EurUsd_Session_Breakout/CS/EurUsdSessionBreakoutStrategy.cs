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

		_smallSessionThreshold = Param(nameof(SmallSessionThreshold), 200m)
			.SetDisplay("Small Session Threshold", "Maximum range session price range to trigger trading", "Risk");

		_stopLossDistance = Param(nameof(StopLossDistance), 5m)
			.SetDisplay("Stop Loss Distance", "Stop loss distance in price units", "Risk");

		_takeProfitDistance = Param(nameof(TakeProfitDistance), 8m)
			.SetDisplay("Take Profit Distance", "Take profit distance in price units", "Risk");

		_breakoutBuffer = Param(nameof(BreakoutBuffer), 0m)
			.SetDisplay("Breakout Buffer", "Extra price buffer added to breakout trigger", "Entries");

		_euSessionLengthBars = Param(nameof(EuSessionLengthBars), 10)
			.SetRange(1, 72)
			.SetDisplay("Range Session Length (bars)", "Number of bars representing the range session", "Schedule");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_highest = null!;
		_lowest = null!;
		_currentHighest = 0;
		_currentLowest = 0;
		_entryPrice = 0;
		_stopPrice = 0;
		_takePrice = 0;
		_currentDate = default;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = EuSessionLengthBars };
		_lowest = new Lowest { Length = EuSessionLengthBars };

		_currentDate = time.Date;

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

		// Update rolling highest/lowest
		var previousHighest = _currentHighest;
		var previousLowest = _currentLowest;

		_currentHighest = _highest.Process(candle).ToDecimal();
		_currentLowest = _lowest.Process(candle).ToDecimal();

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		if (previousHighest <= 0 || previousLowest <= 0)
			return;

		if (Position != 0)
			return;

		// Breakout above previous rolling highest
		if (candle.ClosePrice > previousHighest + BreakoutBuffer)
		{
			BuyMarket();
			SetLongTargets(candle.ClosePrice);
		}
		// Breakout below previous rolling lowest
		else if (candle.ClosePrice < previousLowest - BreakoutBuffer)
		{
			SellMarket();
			SetShortTargets(candle.ClosePrice);
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

		if (Position == 0)
			ClearTargets();
	}
}
