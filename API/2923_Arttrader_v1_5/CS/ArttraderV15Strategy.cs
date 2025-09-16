using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy converted from the Arttrader v1.5 MetaTrader expert.
/// </summary>
public class ArttraderV15Strategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _bigJump;
	private readonly StrategyParam<decimal> _doubleJump;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _emergencyLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _slopeSmall;
	private readonly StrategyParam<decimal> _slopeLarge;
	private readonly StrategyParam<int> _minutesBegin;
	private readonly StrategyParam<int> _minutesEnd;
	private readonly StrategyParam<decimal> _slipBegin;
	private readonly StrategyParam<decimal> _slipEnd;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<decimal> _adjust;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _trendCandleType;

	private readonly decimal?[] _openHistory = new decimal?[6];

	private decimal _pointFactor = 1m;
	private decimal _bigJumpAbs;
	private decimal _doubleJumpAbs;
	private decimal _stopLossAbs;
	private decimal _emergencyLossAbs;
	private decimal _takeProfitAbs;
	private decimal _slopeSmallAbs;
	private decimal _slopeLargeAbs;
	private decimal _slipBeginAbs;
	private decimal _slipEndAbs;
	private decimal _adjustAbs;

	private int _openHistoryCount;
	private decimal? _previousVolume;

	private decimal _currentEma;
	private decimal _previousEma;
	private bool _hasCurrentEma;
	private bool _hasPreviousEma;

	private decimal? _entryReferencePrice;
	private decimal? _entryPrice;
	private bool _lastTradeLong;

	/// <summary>
	/// Order volume used for market entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Period of the EMA calculated on the trend timeframe.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Maximum allowed single candle gap between consecutive opens.
	/// </summary>
	public decimal BigJump
	{
		get => _bigJump.Value;
		set => _bigJump.Value = value;
	}

	/// <summary>
	/// Maximum allowed two-candle gap between opens.
	/// </summary>
	public decimal DoubleJump
	{
		get => _doubleJump.Value;
		set => _doubleJump.Value = value;
	}

	/// <summary>
	/// Loss threshold that enables the time-based exit routine.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Hard protective stop distance from the fill price.
	/// </summary>
	public decimal EmergencyLoss
	{
		get => _emergencyLoss.Value;
		set => _emergencyLoss.Value = value;
	}

	/// <summary>
	/// Take profit offset from the fill price.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Minimum EMA slope required for entries.
	/// </summary>
	public decimal SlopeSmall
	{
		get => _slopeSmall.Value;
		set => _slopeSmall.Value = value;
	}

	/// <summary>
	/// Maximum EMA slope allowed for entries.
	/// </summary>
	public decimal SlopeLarge
	{
		get => _slopeLarge.Value;
		set => _slopeLarge.Value = value;
	}

	/// <summary>
	/// Minutes that must pass in the current hour before entries are allowed.
	/// </summary>
	public int MinutesBegin
	{
		get => _minutesBegin.Value;
		set => _minutesBegin.Value = value;
	}

	/// <summary>
	/// Minutes that must pass in the current hour before time-based exits are allowed.
	/// </summary>
	public int MinutesEnd
	{
		get => _minutesEnd.Value;
		set => _minutesEnd.Value = value;
	}

	/// <summary>
	/// Allowed distance between close and low/high during entry confirmation.
	/// </summary>
	public decimal SlipBegin
	{
		get => _slipBegin.Value;
		set => _slipBegin.Value = value;
	}

	/// <summary>
	/// Allowed distance between close and extreme during exit confirmation.
	/// </summary>
	public decimal SlipEnd
	{
		get => _slipEnd.Value;
		set => _slipEnd.Value = value;
	}

	/// <summary>
	/// Minimum volume required on the previous candle to keep a position open.
	/// </summary>
	public decimal MinVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

	/// <summary>
	/// Price adjustment applied when storing the internal reference entry price.
	/// </summary>
	public decimal Adjust
	{
		get => _adjust.Value;
		set => _adjust.Value = value;
	}

	/// <summary>
	/// Candle type used for trade signals and filters.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type used to calculate the EMA slope filter.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="ArttraderV15Strategy"/>.
	/// </summary>
	public ArttraderV15Strategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Default order volume", "Trading");

		_emaPeriod = Param(nameof(EmaPeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA length calculated on the trend timeframe", "Indicators")
			.SetCanOptimize(true);

		_bigJump = Param(nameof(BigJump), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Single Candle Jump", "Maximum single-candle open gap", "Filters")
			.SetCanOptimize(true);

		_doubleJump = Param(nameof(DoubleJump), 55m)
			.SetGreaterThanZero()
			.SetDisplay("Double Candle Jump", "Maximum two-candle open gap", "Filters")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Smart Stop Loss", "Loss that activates the time-based exit", "Risk")
			.SetCanOptimize(true);

		_emergencyLoss = Param(nameof(EmergencyLoss), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Emergency Stop", "Hard stop distance from fill price", "Risk")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 25m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance from fill price", "Risk")
			.SetCanOptimize(true);

		_slopeSmall = Param(nameof(SlopeSmall), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Slope", "Minimum EMA slope for entries", "Filters")
			.SetCanOptimize(true);

		_slopeLarge = Param(nameof(SlopeLarge), 8m)
			.SetGreaterThanZero()
			.SetDisplay("Maximum Slope", "Maximum EMA slope for entries", "Filters")
			.SetCanOptimize(true);

		_minutesBegin = Param(nameof(MinutesBegin), 25)
			.SetDisplay("Entry Delay (min)", "Minutes after the hour required before entries", "Timing")
			.SetCanOptimize(true);

		_minutesEnd = Param(nameof(MinutesEnd), 25)
			.SetDisplay("Exit Delay (min)", "Minutes after the hour required before timed exits", "Timing")
			.SetCanOptimize(true);

		_slipBegin = Param(nameof(SlipBegin), 0m)
			.SetDisplay("Entry Slip", "Maximum distance between close and extreme for entries", "Filters");

		_slipEnd = Param(nameof(SlipEnd), 0m)
			.SetDisplay("Exit Slip", "Maximum distance between close and extreme for exits", "Filters");

		_minVolume = Param(nameof(MinVolume), 0m)
			.SetDisplay("Minimum Volume", "Required previous candle volume to keep positions", "Filters");

		_adjust = Param(nameof(Adjust), 1m)
			.SetDisplay("Entry Adjustment", "Reference price adjustment that mimics spread handling", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Trading Candle Type", "Candles used for signal generation", "General");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Trend Candle Type", "Candles used for EMA slope evaluation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		if (TrendCandleType != CandleType)
			yield return (Security, TrendCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		Array.Clear(_openHistory, 0, _openHistory.Length);
		_openHistoryCount = 0;
		_previousVolume = null;

		_pointFactor = 1m;
		_bigJumpAbs = 0m;
		_doubleJumpAbs = 0m;
		_stopLossAbs = 0m;
		_emergencyLossAbs = 0m;
		_takeProfitAbs = 0m;
		_slopeSmallAbs = 0m;
		_slopeLargeAbs = 0m;
		_slipBeginAbs = 0m;
		_slipEndAbs = 0m;
		_adjustAbs = 0m;

		_currentEma = 0m;
		_previousEma = 0m;
		_hasCurrentEma = false;
		_hasPreviousEma = false;

		_entryReferencePrice = null;
		_entryPrice = null;
		_lastTradeLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var priceStep = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals;

		_pointFactor = priceStep;

		if (decimals == 3 || decimals == 5)
		_pointFactor *= 10m;

		_bigJumpAbs = BigJump * _pointFactor;
		_doubleJumpAbs = DoubleJump * _pointFactor;
		_stopLossAbs = StopLoss * _pointFactor;
		_emergencyLossAbs = EmergencyLoss * _pointFactor;
		_takeProfitAbs = TakeProfit * _pointFactor;
		_slopeSmallAbs = SlopeSmall * _pointFactor;
		_slopeLargeAbs = SlopeLarge * _pointFactor;
		_slipBeginAbs = SlipBegin * _pointFactor;
		_slipEndAbs = SlipEnd * _pointFactor;
		_adjustAbs = Adjust * _pointFactor;

		var ema = new ExponentialMovingAverage
		{
			// Use the hourly EMA of candle opens to define the dominant trend direction.
			Length = EmaPeriod,
			CandlePrice = CandlePrice.Open,
		};

		var tradeSubscription = SubscribeCandles(CandleType);

		if (TrendCandleType == CandleType)
		{
			tradeSubscription
				.Bind(ema, ProcessTrendCandle)
				.Bind(ProcessTradeCandle)
				.Start();
		}
		else
		{
			tradeSubscription
				.Bind(ProcessTradeCandle)
				.Start();

			var trendSubscription = SubscribeCandles(TrendCandleType);
			trendSubscription
				.Bind(ema, ProcessTrendCandle)
				.Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradeSubscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessTrendCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_hasCurrentEma)
		{
			_previousEma = _currentEma;
			_hasPreviousEma = true;
		}

		_currentEma = emaValue;
		_hasCurrentEma = true;
	}

	private void ProcessTradeCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateOpenHistory(candle.OpenPrice);

		var previousVolume = _previousVolume;
		var emaSlope = _hasPreviousEma ? _currentEma - _previousEma : 0m;
		var beginLong = false;
		var beginShort = false;

		var minutes = candle.CloseTime.Minute;
		// Replicate the TimeCurrent minute check from the original expert.

		if (_hasPreviousEma)
		{
			if (emaSlope >= _slopeSmallAbs && emaSlope <= _slopeLargeAbs)
			{
				var nearLow = candle.ClosePrice <= candle.LowPrice + _slipBeginAbs;
				if (minutes > MinutesBegin && candle.ClosePrice <= candle.OpenPrice && nearLow)
				beginLong = true;
			}

			if (emaSlope <= -_slopeSmallAbs && emaSlope >= -_slopeLargeAbs)
			{
				var nearHigh = candle.ClosePrice >= candle.HighPrice - _slipBeginAbs;
				if (minutes > MinutesBegin && candle.ClosePrice >= candle.OpenPrice && nearHigh)
				beginShort = true;
			}
		}

		if (ShouldSkipDueToJump())
		{
			// Cancel signals when abnormal gaps were detected in the recent history.
			beginLong = false;
			beginShort = false;
		}

		var exitLong = Position > 0 && _entryReferencePrice.HasValue && _lastTradeLong && ShouldExitLong(candle, previousVolume);
		// Execute the smart-stop logic for long positions before evaluating new entries.
		if (exitLong)
		{
			SellMarket(Position);
			ResetEntryState();
		}

		var exitShort = Position < 0 && _entryReferencePrice.HasValue && !_lastTradeLong && ShouldExitShort(candle, previousVolume);
		// Mirror the smart-stop behaviour for existing short trades.
		if (exitShort)
		{
			BuyMarket(Math.Abs(Position));
			ResetEntryState();
		}

		if (Position > 0)
			beginLong = false;
		else if (Position < 0)
			beginShort = false;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousVolume = candle.TotalVolume ?? 0m;
			return;
		}

		if (beginLong)
		{
			var volume = Volume;
			if (Position < 0)
			volume += Math.Abs(Position);

			if (volume > 0m)
			{
				BuyMarket(volume);
				_entryReferencePrice = candle.OpenPrice - _adjustAbs;
				_entryPrice = candle.ClosePrice;
				_lastTradeLong = true;
			}
		}
		else if (beginShort)
		{
			var volume = Volume;
			if (Position > 0)
			volume += Position;

			if (volume > 0m)
			{
				SellMarket(volume);
				_entryReferencePrice = candle.OpenPrice + _adjustAbs;
				_entryPrice = candle.ClosePrice;
				_lastTradeLong = false;
			}
		}

		_previousVolume = candle.TotalVolume ?? 0m;
	}

	private bool ShouldSkipDueToJump()
	{
		// Inspect the last six opens to detect unusually large gaps.
		if (_openHistoryCount < 2)
		return false;

		if (_bigJumpAbs > 0m)
		{
			for (var i = 0; i + 1 < _openHistoryCount; i++)
			{
				var first = _openHistory[i];
				var second = _openHistory[i + 1];

				if (!first.HasValue || !second.HasValue)
				continue;

				if (Math.Abs(first.Value - second.Value) >= _bigJumpAbs)
				return true;
			}
		}

		if (_doubleJumpAbs > 0m && _openHistoryCount >= 3)
		{
			for (var i = 0; i + 2 < _openHistoryCount; i++)
			{
				var first = _openHistory[i];
				var third = _openHistory[i + 2];

				if (!first.HasValue || !third.HasValue)
				continue;

				if (Math.Abs(first.Value - third.Value) >= _doubleJumpAbs)
				return true;
			}
		}

		return false;
	}

	private bool ShouldExitLong(ICandleMessage candle, decimal? previousVolume)
	{
		// Combine timed exits, emergency stops, take profits, and volume-based failsafes for longs.
		var exit = false;

		if (_takeProfitAbs > 0m && _entryPrice.HasValue && candle.HighPrice >= _entryPrice.Value + _takeProfitAbs)
		exit = true;

		if (_emergencyLossAbs > 0m && _entryPrice.HasValue && candle.LowPrice <= _entryPrice.Value - _emergencyLossAbs)
		exit = true;

		if (!exit && _stopLossAbs > 0m && _entryReferencePrice.HasValue)
		{
			var loss = candle.ClosePrice - _entryReferencePrice.Value;
			var exitTimeReached = candle.CloseTime.Minute > MinutesEnd;
			var bullishRecovery = candle.ClosePrice >= candle.OpenPrice && candle.ClosePrice >= candle.HighPrice - _slipEndAbs;

			if (loss <= -_stopLossAbs && exitTimeReached && bullishRecovery)
			exit = true;
		}

		if (!exit && previousVolume.HasValue && previousVolume.Value <= MinVolume)
		exit = true;

		return exit;
	}

	private bool ShouldExitShort(ICandleMessage candle, decimal? previousVolume)
	{
		// Symmetric exit logic for short positions.
		var exit = false;

		if (_takeProfitAbs > 0m && _entryPrice.HasValue && candle.LowPrice <= _entryPrice.Value - _takeProfitAbs)
		exit = true;

		if (_emergencyLossAbs > 0m && _entryPrice.HasValue && candle.HighPrice >= _entryPrice.Value + _emergencyLossAbs)
		exit = true;

		if (!exit && _stopLossAbs > 0m && _entryReferencePrice.HasValue)
		{
			var loss = _entryReferencePrice.Value - candle.ClosePrice;
			var exitTimeReached = candle.CloseTime.Minute > MinutesEnd;
			var bearishRecovery = candle.ClosePrice <= candle.OpenPrice && candle.ClosePrice <= candle.LowPrice + _slipEndAbs;

			if (loss <= -_stopLossAbs && exitTimeReached && bearishRecovery)
			exit = true;
		}

		if (!exit && previousVolume.HasValue && previousVolume.Value <= MinVolume)
		exit = true;

		return exit;
	}

	private void UpdateOpenHistory(decimal open)
	{
		for (var i = _openHistory.Length - 1; i > 0; i--)
		_openHistory[i] = _openHistory[i - 1];

		_openHistory[0] = open;

		if (_openHistoryCount < _openHistory.Length)
		_openHistoryCount++;
	}

	private void ResetEntryState()
	{
		_entryReferencePrice = null;
		_entryPrice = null;
	}
}
