using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum strategy converted from the WOC 0.1.2 MetaTrader expert advisor.
/// Tracks rapid ask price runs and opens positions in the breakout direction.
/// </summary>
public class Woc012Strategy : Strategy
{
	// Strategy parameters controlling risk, entry detection and volume management.
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _trailingStopTicks;
	private readonly StrategyParam<int> _sequenceLength;
	private readonly StrategyParam<decimal> _sequenceTimeoutSeconds;
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<bool> _autoLotSizing;

	// Live market data snapshot.
	private decimal _currentAsk;
	private decimal _currentBid;

	// Fields used to accumulate directional ask price streaks.
	private decimal _upReferencePrice;
	private decimal _downReferencePrice;
	private int _upCount;
	private int _downCount;
	private bool _upSequenceActive;
	private bool _downSequenceActive;
	private DateTimeOffset _upStartTime;
	private DateTimeOffset _downStartTime;

	// Risk parameters translated to absolute price distances.
	private decimal _stopLossDistance;
	private decimal _trailingDistance;
	private decimal _longStopPrice;
	private decimal _shortStopPrice;

	// Remember previous position value to detect new entries.
	private decimal _previousPosition;

	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	public int TrailingStopTicks
	{
		get => _trailingStopTicks.Value;
		set => _trailingStopTicks.Value = value;
	}

	public int SequenceLength
	{
		get => _sequenceLength.Value;
		set => _sequenceLength.Value = value;
	}

	public decimal SequenceTimeoutSeconds
	{
		get => _sequenceTimeoutSeconds.Value;
		set => _sequenceTimeoutSeconds.Value = value;
	}

	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	public bool UseAutoLotSizing
	{
		get => _autoLotSizing.Value;
		set => _autoLotSizing.Value = value;
	}

	public Woc012Strategy()
	{
		_stopLossTicks = Param(nameof(StopLossTicks), 6)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Ticks", "Stop loss distance expressed in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(3, 12, 1);

		_trailingStopTicks = Param(nameof(TrailingStopTicks), 6)
			.SetNotNegative()
			.SetDisplay("Trailing Stop Ticks", "Trailing distance expressed in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(2, 12, 1);

		_sequenceLength = Param(nameof(SequenceLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Sequence Length", "Consecutive ask changes required before entry", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_sequenceTimeoutSeconds = Param(nameof(SequenceTimeoutSeconds), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Sequence Timeout (s)", "Maximum seconds allowed for the momentum sequence", "Signals");

		_lotSize = Param(nameof(LotSize), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Size", "Fixed order volume when auto sizing is disabled", "Trading");

		_autoLotSizing = Param(nameof(UseAutoLotSizing), false)
			.SetDisplay("Auto Lots", "Enable balance dependent volume calculation", "Trading");

		// Keep default volume aligned with the fixed lot parameter.
		Volume = LotSize;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_currentAsk = 0m;
		_currentBid = 0m;
		_upReferencePrice = 0m;
		_downReferencePrice = 0m;
		_upCount = 0;
		_downCount = 0;
		_upSequenceActive = false;
		_downSequenceActive = false;
		_upStartTime = default;
		_downStartTime = default;
		_stopLossDistance = 0m;
		_trailingDistance = 0m;
		_longStopPrice = 0m;
		_shortStopPrice = 0m;
		_previousPosition = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdateRiskDistances();

		// Subscribe to best bid/ask updates to mimic tick level decision making.
		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj) && askObj is decimal ask)
			_currentAsk = ask;

		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) && bidObj is decimal bid)
			_currentBid = bid;

		// Skip until both sides of the spread are known.
		if (_currentAsk <= 0m || _currentBid <= 0m)
			return;

		UpdateRiskDistances();

		var time = level1.ServerTime != default ? level1.ServerTime : CurrentTime;

		if (Position == 0)
		{
			// Flat state: accumulate directional streaks to detect breakouts.
			ProcessFlatState(time);
		}
		else
		{
			// Position exists: manage protective stops and trailing logic.
			ProcessPositionState();
		}

		_previousPosition = Position;
	}

	private void ProcessFlatState(DateTimeOffset time)
	{
		if (_upReferencePrice == 0m)
			_upReferencePrice = _currentAsk;

		if (_downReferencePrice == 0m)
			_downReferencePrice = _currentAsk;

		// Increase the upward streak counter when ask keeps printing higher values.
		if (_currentAsk > _upReferencePrice)
		{
			_upCount++;
			_upReferencePrice = _currentAsk;

			if (!_upSequenceActive)
			{
				_upSequenceActive = true;
				_upStartTime = time;
			}
		}
		else
		{
			// Sequence failed, reset the upward tracker.
			ResetUpSequence();
		}

		// Increase the downward streak counter when ask trades lower.
		if (_currentAsk < _downReferencePrice)
		{
			_downCount++;
			_downReferencePrice = _currentAsk;

			if (!_downSequenceActive)
			{
				_downSequenceActive = true;
				_downStartTime = time;
			}
		}
		else
		{
			// Sequence failed, reset the downward tracker.
			ResetDownSequence();
		}

		// When either side reached the configured streak length try to enter.
		if (_upCount >= SequenceLength || _downCount >= SequenceLength)
			TryOpenPosition(time);
	}

	private void ProcessPositionState()
	{
		// While in position we ignore new streaks and focus on exit management.
		ResetSequencesAfterTrade();

		if (Position > 0)
		{
			// New long position: set initial stop below current bid.
			if (_previousPosition <= 0)
			{
				ResetProtectionLevels();
				_longStopPrice = Math.Max(0m, _currentBid - _stopLossDistance);
			}

			if (_longStopPrice <= 0m)
				_longStopPrice = Math.Max(0m, _currentBid - _stopLossDistance);

			// Exit long if the stop level is touched.
			if (_longStopPrice > 0m && _currentBid <= _longStopPrice)
			{
				SellMarket(Position);
				ResetProtectionLevels();
				return;
			}

			// Move trailing stop once price advanced enough distance.
			if (_trailingDistance > 0m)
			{
				var entryPrice = Position.AveragePrice;
				if (entryPrice > 0m && _currentBid - entryPrice > _trailingDistance)
				{
					var candidate = _currentBid - _trailingDistance;
					if (_longStopPrice < _currentBid - 2m * _trailingDistance)
						_longStopPrice = Math.Max(_longStopPrice, candidate);
				}
			}
		}
		else if (Position < 0)
		{
			// New short position: set initial stop above current ask.
			if (_previousPosition >= 0)
			{
				ResetProtectionLevels();
				_shortStopPrice = _currentAsk + _stopLossDistance;
			}

			if (_shortStopPrice <= 0m)
				_shortStopPrice = _currentAsk + _stopLossDistance;

			// Exit short if the protective stop is touched.
			if (_shortStopPrice > 0m && _currentAsk >= _shortStopPrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtectionLevels();
				return;
			}

			// Trail the short position when price falls enough.
			if (_trailingDistance > 0m)
			{
				var entryPrice = Position.AveragePrice;
				if (entryPrice > 0m && entryPrice - _currentAsk > _trailingDistance)
				{
					var candidate = _currentAsk + _trailingDistance;
					if (_shortStopPrice > _currentAsk + 2m * _trailingDistance)
						_shortStopPrice = Math.Min(_shortStopPrice, candidate);
				}
			}
		}
		else
		{
			ResetProtectionLevels();
		}
	}

	private void TryOpenPosition(DateTimeOffset time)
	{
		if (Position != 0)
		{
			ResetSequencesAfterTrade();
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			ResetSequencesAfterTrade();
			return;
		}

		var volume = GetTradeVolume();

		if (volume <= 0m)
		{
			ResetSequencesAfterTrade();
			return;
		}

		var timeout = TimeSpan.FromSeconds((double)SequenceTimeoutSeconds);

		// Decide direction based on which streak was stronger, same as the MQL logic.
		if (_upCount < _downCount)
		{
			if (_downSequenceActive && _downCount >= SequenceLength && (time - _downStartTime) <= timeout)
			{
				SellMarket(volume);
				ResetProtectionLevels();
			}
		}
		else
		{
			if (_upSequenceActive && _upCount >= SequenceLength && (time - _upStartTime) <= timeout)
			{
				BuyMarket(volume);
				ResetProtectionLevels();
			}
		}

		ResetSequencesAfterTrade();
	}

	private void ResetSequencesAfterTrade()
	{
		ResetUpSequence();
		ResetDownSequence();
	}

	private void ResetUpSequence()
	{
		_upCount = 0;
		_upSequenceActive = false;
		_upStartTime = default;
		_upReferencePrice = _currentAsk;
	}

	private void ResetDownSequence()
	{
		_downCount = 0;
		_downSequenceActive = false;
		_downStartTime = default;
		_downReferencePrice = _currentAsk;
	}

	private void ResetProtectionLevels()
	{
		_longStopPrice = 0m;
		_shortStopPrice = 0m;
	}

	private void UpdateRiskDistances()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
			step = 1m;

		_stopLossDistance = StopLossTicks * step;
		_trailingDistance = TrailingStopTicks * step;
	}

	private decimal GetTradeVolume()
	{
		if (!UseAutoLotSizing)
			return LotSize;

		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		var volume = LotSize;

		// Reproduce the tiered balance to lot mapping from the MetaTrader version.
		if (balance < 200m)
			volume = 0.02m;
		if (balance > 200m)
			volume = 0.04m;
		if (balance > 300m)
			volume = 0.05m;
		if (balance > 400m)
			volume = 0.06m;
		if (balance > 500m)
			volume = 0.07m;
		if (balance > 600m)
			volume = 0.08m;
		if (balance > 700m)
			volume = 0.09m;
		if (balance > 800m)
			volume = 0.1m;
		if (balance > 900m)
			volume = 0.2m;
		if (balance > 1000m)
			volume = 0.3m;
		if (balance > 2000m)
			volume = 0.4m;
		if (balance > 3000m)
			volume = 0.5m;
		if (balance > 4000m)
			volume = 0.6m;
		if (balance > 5000m)
			volume = 0.7m;
		if (balance > 6000m)
			volume = 0.8m;
		if (balance > 7000m)
			volume = 0.9m;
		if (balance > 8000m)
			volume = 1m;
		if (balance > 9000m)
			volume = 2m;
		if (balance > 10000m)
			volume = 3m;
		if (balance > 11000m)
			volume = 4m;
		if (balance > 12000m)
			volume = 5m;
		if (balance > 13000m)
			volume = 6m;
		if (balance > 14000m)
			volume = 7m;
		if (balance > 15000m)
			volume = 8m;
		if (balance > 20000m)
			volume = 9m;
		if (balance > 30000m)
			volume = 10m;
		if (balance > 40000m)
			volume = 11m;
		if (balance > 50000m)
			volume = 12m;
		if (balance > 60000m)
			volume = 13m;
		if (balance > 70000m)
			volume = 14m;
		if (balance > 80000m)
			volume = 15m;
		if (balance > 90000m)
			volume = 16m;
		if (balance > 100000m)
			volume = 17m;
		if (balance > 110000m)
			volume = 18m;
		if (balance > 120000m)
			volume = 19m;
		if (balance > 130000m)
			volume = 20m;

		return Math.Max(volume, 0m);
	}
}
