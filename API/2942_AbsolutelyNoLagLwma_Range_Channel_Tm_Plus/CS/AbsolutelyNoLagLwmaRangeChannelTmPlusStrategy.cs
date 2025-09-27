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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the AbsolutelyNoLagLWMA range channel expert.
/// It double-smooths highs and lows with LWMA and acts on channel breakouts.
/// </summary>
public class AbsolutelyNoLagLwmaRangeChannelTmPlusStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _enableBuyExits;
	private readonly StrategyParam<bool> _enableSellExits;
	private readonly StrategyParam<bool> _useTimeExit;
	private readonly StrategyParam<TimeSpan> _holdingLimit;

	private WeightedMovingAverage _upperStage1 = null!;
	private WeightedMovingAverage _upperStage2 = null!;
	private WeightedMovingAverage _lowerStage1 = null!;
	private WeightedMovingAverage _lowerStage2 = null!;
	private readonly List<ChannelState> _stateHistory = new();
	private DateTimeOffset? _longEntryTime;
	private DateTimeOffset? _shortEntryTime;

	/// <summary>
	/// Initializes a new instance of the strategy and exposes tunable parameters.
	/// </summary>
	public AbsolutelyNoLagLwmaRangeChannelTmPlusStrategy()
	{
		_length = Param(nameof(Length), 7)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Channel Length", "Length used by both LWMA smoothing passes", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Signal Bar Shift", "Index of the bar evaluated for signals", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe that feeds the channel", "General");

		_volume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Order Volume", "Contracts or lots used for entries", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Stop Loss (points)", "Protective stop distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetDisplay("Take Profit (points)", "Profit target distance in price steps", "Risk");

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
			.SetDisplay("Enable Long Entries", "Allow new long positions", "Trading");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
			.SetDisplay("Enable Short Entries", "Allow new short positions", "Trading");

		_enableBuyExits = Param(nameof(EnableBuyExits), true)
			.SetDisplay("Enable Long Exits", "Allow indicator exits for long trades", "Trading");

		_enableSellExits = Param(nameof(EnableSellExits), true)
			.SetDisplay("Enable Short Exits", "Allow indicator exits for short trades", "Trading");

		_useTimeExit = Param(nameof(UseTimeExit), true)
			.SetDisplay("Enable Time Exit", "Close positions after exceeding holding time", "Risk");

		_holdingLimit = Param(nameof(HoldingLimit), TimeSpan.FromMinutes(960))
			.SetDisplay("Holding Limit", "Maximum time to hold an open position", "Risk");
	}

	/// <summary>
	/// Channel smoothing length shared by both LWMA passes.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Bar index used for signal evaluation (0 = last closed bar).
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Candle type that feeds the indicator chain.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Trade volume used for fresh entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in instrument points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in instrument points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables or disables long side entries.
	/// </summary>
	public bool EnableBuyEntries
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	/// <summary>
	/// Enables or disables short side entries.
	/// </summary>
	public bool EnableSellEntries
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	/// <summary>
	/// Allows indicator-driven exits for long trades.
	/// </summary>
	public bool EnableBuyExits
	{
		get => _enableBuyExits.Value;
		set => _enableBuyExits.Value = value;
	}

	/// <summary>
	/// Allows indicator-driven exits for short trades.
	/// </summary>
	public bool EnableSellExits
	{
		get => _enableSellExits.Value;
		set => _enableSellExits.Value = value;
	}

	/// <summary>
	/// Enables the optional time based exit.
	/// </summary>
	public bool UseTimeExit
	{
		get => _useTimeExit.Value;
		set => _useTimeExit.Value = value;
	}

	/// <summary>
	/// Maximum holding time before a position is forcefully closed.
	/// </summary>
	public TimeSpan HoldingLimit
	{
		get => _holdingLimit.Value;
		set => _holdingLimit.Value = value;
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

		_stateHistory.Clear();
		_longEntryTime = null;
		_shortEntryTime = null;

		if (_upperStage1 != null)
		{
			_upperStage1.Length = Length;
			_upperStage1.Reset();
		}

		if (_upperStage2 != null)
		{
			_upperStage2.Length = Length;
			_upperStage2.Reset();
		}

		if (_lowerStage1 != null)
		{
			_lowerStage1.Length = Length;
			_lowerStage1.Reset();
		}

		if (_lowerStage2 != null)
		{
			_lowerStage2.Length = Length;
			_lowerStage2.Reset();
		}
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Prepare the double LWMA structure that emulates the original indicator.
		_upperStage1 = new WeightedMovingAverage { Length = Length };
		_upperStage2 = new WeightedMovingAverage { Length = Length };
		_lowerStage1 = new WeightedMovingAverage { Length = Length };
		_lowerStage2 = new WeightedMovingAverage { Length = Length };

		// Subscribe to the selected candle type and process finished bars only.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		// Basic visualization helps during testing when a chart is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		// Configure protective orders using point-based distances from the MQL inputs.
		var step = Security?.PriceStep ?? 0m;
		Unit take = null;
		Unit stop = null;

		if (TakeProfitPoints > 0 && step > 0)
			take = new Unit(step * TakeProfitPoints, UnitTypes.Absolute);

		if (StopLossPoints > 0 && step > 0)
			stop = new Unit(step * StopLossPoints, UnitTypes.Absolute);

		if (take != null || stop != null)
			StartProtection(take, stop);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work exclusively with closed candles to match the expert advisor behaviour.
		if (candle.State != CandleStates.Finished)
			return;

		// Check the time based exit before evaluating new signals.
		var timeExit = HandleTimeExit(candle);

		// Feed high and low prices through the two stage LWMA filters.
		var upperStage1Value = _upperStage1.Process(new DecimalIndicatorValue(_upperStage1, candle.HighPrice, candle.OpenTime));
		var lowerStage1Value = _lowerStage1.Process(new DecimalIndicatorValue(_lowerStage1, candle.LowPrice, candle.OpenTime));

		if (!upperStage1Value.IsFormed || !lowerStage1Value.IsFormed)
			return;

		var upperStage2Value = _upperStage2.Process(new DecimalIndicatorValue(_upperStage2, upperStage1Value.ToDecimal(), candle.OpenTime));
		var lowerStage2Value = _lowerStage2.Process(new DecimalIndicatorValue(_lowerStage2, lowerStage1Value.ToDecimal(), candle.OpenTime));

		if (!upperStage2Value.IsFormed || !lowerStage2Value.IsFormed)
			return;

		var upper = upperStage2Value.ToDecimal();
		var lower = lowerStage2Value.ToDecimal();

		var state = DetermineState(candle, upper, lower);
		var signalBar = SignalBar;

		UpdateStateHistory(state, signalBar);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		HandleIndicatorSignals(signalBar, timeExit);
	}

	private (bool longExit, bool shortExit) HandleTimeExit(ICandleMessage candle)
	{
		var longExit = false;
		var shortExit = false;

		if (!UseTimeExit || HoldingLimit <= TimeSpan.Zero)
			return (longExit, shortExit);

		var candleTime = candle.CloseTime ?? candle.OpenTime;

		if (Position > 0 && _longEntryTime.HasValue && candleTime - _longEntryTime.Value >= HoldingLimit)
		{
			SellMarket(Position);
			_longEntryTime = candleTime;
			longExit = true;
		}

		if (Position < 0 && _shortEntryTime.HasValue && candleTime - _shortEntryTime.Value >= HoldingLimit)
		{
			BuyMarket(Math.Abs(Position));
			_shortEntryTime = candleTime;
			shortExit = true;
		}

		return (longExit, shortExit);
	}

	private void UpdateStateHistory(ChannelState state, int signalBar)
	{
		_stateHistory.Insert(0, state);

		var maxLength = Math.Max(signalBar + 2, 2);
		if (_stateHistory.Count > maxLength)
			_stateHistory.RemoveRange(maxLength, _stateHistory.Count - maxLength);
	}

	private void HandleIndicatorSignals(int signalBar, (bool longExit, bool shortExit) timeExit)
	{
		var requiredIndex = signalBar + 1;
		if (_stateHistory.Count <= requiredIndex)
			return;

		var signalState = _stateHistory[signalBar + 1];
		var confirmState = _stateHistory[signalBar];

		if (EnableBuyEntries && signalState == ChannelState.Above && confirmState != ChannelState.Above && Position <= 0)
		{
			BuyMarket(OrderVolume + Math.Abs(Position));
		}

		if (EnableSellEntries && signalState == ChannelState.Below && confirmState != ChannelState.Below && Position >= 0)
		{
			SellMarket(OrderVolume + Math.Abs(Position));
		}

		if (!timeExit.longExit && EnableBuyExits && signalState == ChannelState.Below && Position > 0)
		{
			SellMarket(Position);
			_longEntryTime = null;
		}

		if (!timeExit.shortExit && EnableSellExits && signalState == ChannelState.Above && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			_shortEntryTime = null;
		}
	}

	private static ChannelState DetermineState(ICandleMessage candle, decimal upper, decimal lower)
	{
		if (candle.ClosePrice > upper)
			return ChannelState.Above;

		if (candle.ClosePrice < lower)
			return ChannelState.Below;

		return ChannelState.Inside;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position > 0)
		{
			_longEntryTime = trade.ServerTime;
			_shortEntryTime = null;
		}
		else if (Position < 0)
		{
			_shortEntryTime = trade.ServerTime;
			_longEntryTime = null;
		}
		else
		{
			_longEntryTime = null;
			_shortEntryTime = null;
		}
	}

	private enum ChannelState
	{
		Inside,
		Above,
		Below
	}
}