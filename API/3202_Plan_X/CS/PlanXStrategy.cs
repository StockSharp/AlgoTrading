using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy based on the Plan X expert advisor.
/// Monitors candle closes relative to a shifted reference bar and enters trades after significant breakouts.
/// Includes optional time filtering, stop-loss, take-profit and trailing stop management converted from the original MQL logic.
/// </summary>
public class PlanXStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _channelHeightPips;
	private readonly StrategyParam<int> _candleShift;
	private readonly StrategyParam<bool> _useTimeControl;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closeHistory = new();

	private decimal _pipSize;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;
	private decimal _trailingStopOffset;
	private decimal _trailingStepOffset;
	private decimal _channelHeightOffset;

	private decimal _entryPrice;
	private decimal? _activeStopPrice;
	private decimal? _activeTakePrice;

	/// <summary>Stop-loss distance expressed in pips.</summary>
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }

	/// <summary>Take-profit distance expressed in pips.</summary>
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }

	/// <summary>Trailing stop distance expressed in pips.</summary>
	public int TrailingStopPips { get => _trailingStopPips.Value; set => _trailingStopPips.Value = value; }

	/// <summary>Trailing adjustment step expressed in pips.</summary>
	public int TrailingStepPips { get => _trailingStepPips.Value; set => _trailingStepPips.Value = value; }

	/// <summary>Height of the breakout channel in pips.</summary>
	public int ChannelHeightPips { get => _channelHeightPips.Value; set => _channelHeightPips.Value = value; }

	/// <summary>Number of bars used as the comparison shift.</summary>
	public int CandleShift { get => _candleShift.Value; set => _candleShift.Value = value; }

	/// <summary>Determines whether trading is limited to a time window.</summary>
	public bool UseTimeControl { get => _useTimeControl.Value; set => _useTimeControl.Value = value; }

	/// <summary>Hour (0-23) when trading may begin.</summary>
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }

	/// <summary>Hour (0-23) when trading stops.</summary>
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }

	/// <summary>Reverses long and short breakout conditions.</summary>
	public bool ReverseSignals { get => _reverseSignals.Value; set => _reverseSignals.Value = value; }

	/// <summary>Volume used for new entries.</summary>
	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }

	/// <summary>Candle type used for calculations.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Initializes a new instance of the <see cref="PlanXStrategy"/> class.</summary>
	public PlanXStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 150, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 40)
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 150, 10);

		_trailingStopPips = Param(nameof(TrailingStopPips), 10)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 5);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Minimal profit increment before trailing adjusts", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1, 30, 1);

		_channelHeightPips = Param(nameof(ChannelHeightPips), 34)
			.SetDisplay("Channel Height (pips)", "Required breakout distance in pips", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_candleShift = Param(nameof(CandleShift), 2)
			.SetDisplay("Candle Shift", "Number of bars used as the reference", "Signals")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_useTimeControl = Param(nameof(UseTimeControl), true)
			.SetDisplay("Use Time Control", "Enable start and end hour filtering", "Time");

		_startHour = Param(nameof(StartHour), 10)
			.SetDisplay("Start Hour", "Hour when trading begins", "Time")
			.SetCanOptimize(true)
			.SetOptimize(0, 23, 1);

		_endHour = Param(nameof(EndHour), 21)
			.SetDisplay("End Hour", "Hour when trading stops", "Time")
			.SetCanOptimize(true)
			.SetOptimize(0, 23, 1);

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Flip breakout directions", "Signals");

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetDisplay("Order Volume", "Lot size for each entry", "Trading")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for analysis", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var decimals = GetDecimalPlaces(step);
		_pipSize = (decimals == 3 || decimals == 5) ? step * 10m : step;

		_stopLossOffset = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
		_takeProfitOffset = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;
		_trailingStopOffset = TrailingStopPips > 0 ? TrailingStopPips * _pipSize : 0m;
		_trailingStepOffset = TrailingStepPips > 0 ? TrailingStepPips * _pipSize : 0m;
		_channelHeightOffset = ChannelHeightPips > 0 ? ChannelHeightPips * _pipSize : 0m;

		Volume = OrderVolume;
		ResetProtection();
		_closeHistory.Clear();

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		ManageActivePosition(candle);

		_closeHistory.Add(candle.ClosePrice);
		var maxHistory = Math.Max(CandleShift + 2, 3);
		if (_closeHistory.Count > maxHistory)
			_closeHistory.RemoveRange(0, _closeHistory.Count - maxHistory);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsWithinTradingWindow(candle.OpenTime))
			return;

		if (_closeHistory.Count <= CandleShift)
			return;

		var lastIndex = _closeHistory.Count - 1;
		var compareIndex = lastIndex - CandleShift;
		if (compareIndex < 0)
			return;

		var latestClose = _closeHistory[lastIndex];
		var referenceClose = _closeHistory[compareIndex];

		var breakoutUp = latestClose > referenceClose + _channelHeightOffset;
		var breakoutDown = latestClose < referenceClose - _channelHeightOffset;

		if (!ReverseSignals)
		{
			if (breakoutUp)
				EnterLong(latestClose);
			else if (breakoutDown)
				EnterShort(latestClose);
		}
		else
		{
			if (breakoutUp)
				EnterShort(latestClose);
			else if (breakoutDown)
				EnterLong(latestClose);
		}
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_activeStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				var volume = Math.Abs(Position);
				if (volume > 0m)
					SellMarket(volume);
				ResetProtection();
				return;
			}

			if (_activeTakePrice is decimal take && candle.HighPrice >= take)
			{
				var volume = Math.Abs(Position);
				if (volume > 0m)
					SellMarket(volume);
				ResetProtection();
				return;
			}

			UpdateLongTrailing(candle);
		}
		else if (Position < 0m)
		{
			if (_activeStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				var volume = Math.Abs(Position);
				if (volume > 0m)
					BuyMarket(volume);
				ResetProtection();
				return;
			}

			if (_activeTakePrice is decimal take && candle.LowPrice <= take)
			{
				var volume = Math.Abs(Position);
				if (volume > 0m)
					BuyMarket(volume);
				ResetProtection();
				return;
			}

			UpdateShortTrailing(candle);
		}
		else
		{
			ResetProtection();
		}
	}

	private void EnterLong(decimal entryPrice)
	{
		if (OrderVolume <= 0m)
			return;

		if (Position > 0m)
			return;

		var volume = OrderVolume + (Position < 0m ? Math.Abs(Position) : 0m);
		if (volume <= 0m)
			return;

		BuyMarket(volume);

		_entryPrice = entryPrice;
		_activeStopPrice = _stopLossOffset > 0m ? entryPrice - _stopLossOffset : null;
		_activeTakePrice = _takeProfitOffset > 0m ? entryPrice + _takeProfitOffset : null;
	}

	private void EnterShort(decimal entryPrice)
	{
		if (OrderVolume <= 0m)
			return;

		if (Position < 0m)
			return;

		var volume = OrderVolume + (Position > 0m ? Math.Abs(Position) : 0m);
		if (volume <= 0m)
			return;

		SellMarket(volume);

		_entryPrice = entryPrice;
		_activeStopPrice = _stopLossOffset > 0m ? entryPrice + _stopLossOffset : null;
		_activeTakePrice = _takeProfitOffset > 0m ? entryPrice - _takeProfitOffset : null;
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (_trailingStopOffset <= 0m || _trailingStepOffset <= 0m || _entryPrice <= 0m)
			return;

		var maxPrice = candle.HighPrice;
		var advance = maxPrice - _entryPrice;

		if (advance <= _trailingStopOffset + _trailingStepOffset)
			return;

		var minAllowed = maxPrice - (_trailingStopOffset + _trailingStepOffset);
		if (_activeStopPrice is null || _activeStopPrice < minAllowed)
		{
			var newStop = maxPrice - _trailingStopOffset;
			if (_activeStopPrice is null || newStop > _activeStopPrice.Value)
				_activeStopPrice = newStop;
		}
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (_trailingStopOffset <= 0m || _trailingStepOffset <= 0m || _entryPrice <= 0m)
			return;

		var minPrice = candle.LowPrice;
		var advance = _entryPrice - minPrice;

		if (advance <= _trailingStopOffset + _trailingStepOffset)
			return;

		var maxAllowed = minPrice + (_trailingStopOffset + _trailingStepOffset);
		if (_activeStopPrice is null || _activeStopPrice > maxAllowed)
		{
			var newStop = minPrice + _trailingStopOffset;
			if (_activeStopPrice is null || newStop < _activeStopPrice.Value)
				_activeStopPrice = newStop;
		}
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		if (!UseTimeControl)
			return true;

		var start = StartHour;
		var end = EndHour;
		var hour = time.Hour;

		if (start < end)
			return hour >= start && hour < end;

		if (start > end)
			return hour >= start || hour < end;

		return false;
	}

	private void ResetProtection()
	{
		_entryPrice = 0m;
		_activeStopPrice = null;
		_activeTakePrice = null;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}
}
