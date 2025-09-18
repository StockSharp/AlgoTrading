using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Advanced EA Panel adaptation that emulates the original MQL5 control panel.
/// The strategy aggregates multi-timeframe indicator votes, calculates pivot levels,
/// tracks ATR volatility, and executes directional trades with automatic risk controls.
/// </summary>
public class AdvancedEaPanelStrategy : Strategy
{
	private static readonly TimeSpan[] PanelTimeFrames =
	{
		TimeSpan.FromMinutes(1),
		TimeSpan.FromMinutes(5),
		TimeSpan.FromMinutes(15),
		TimeSpan.FromMinutes(30),
		TimeSpan.FromHours(1),
		TimeSpan.FromHours(4),
		TimeSpan.FromDays(1),
		TimeSpan.FromDays(7),
		TimeSpan.FromDays(30)
	};

	private readonly TimeFrameState[] _timeFrameStates;

	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<int> _volatilityPeriod;
	private readonly StrategyParam<DataType> _primaryCandleType;
	private readonly StrategyParam<DataType> _pivotCandleType;
	private readonly StrategyParam<int> _directionalThreshold;
	private readonly StrategyParam<bool> _autoTrading;
	private readonly StrategyParam<PivotFormulaSet> _pivotFormulaSet;

	private AverageTrueRange _atr;
	private decimal _pipSize;
	private decimal _lastAtrValue;
	private PivotLevels _pivotLevels;
	private PanelAction _currentSignal = PanelAction.None;
	private PanelAction _queuedAction = PanelAction.None;
	private PanelAction _executedSignal = PanelAction.None;
	private bool _isClosingPosition;
	private int _lastDirectionalScore;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal? _currentRiskPips;
	private decimal? _currentRewardPips;
	private decimal? _currentRiskReward;

	/// <summary>
	/// Available pivot formula presets.
	/// </summary>
	public enum PivotFormulaSet
	{
		/// <summary>
		/// Classic floor trader pivots.
		/// </summary>
		Classic,

		/// <summary>
		/// Woodie pivot calculations that emphasize the open price.
		/// </summary>
		Woodie,

		/// <summary>
		/// Camarilla pivots focused on range expansion ratios.
		/// </summary>
		Camarilla
	}

	private enum PanelAction
	{
		None,
		Buy,
		Sell
	}

	/// <summary>
	/// Trading volume in lots.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps (pips).
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps (pips).
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// ATR lookback period for volatility estimation.
	/// </summary>
	public int VolatilityPeriod
	{
		get => _volatilityPeriod.Value;
		set => _volatilityPeriod.Value = value;
	}

	/// <summary>
	/// Primary candle type that feeds ATR and general monitoring.
	/// </summary>
	public DataType PrimaryCandleType
	{
		get => _primaryCandleType.Value;
		set => _primaryCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used to recalculate pivot levels.
	/// </summary>
	public DataType PivotCandleType
	{
		get => _pivotCandleType.Value;
		set => _pivotCandleType.Value = value;
	}

	/// <summary>
	/// Minimal absolute multi-timeframe score required to trigger a signal.
	/// </summary>
	public int DirectionalThreshold
	{
		get => _directionalThreshold.Value;
		set => _directionalThreshold.Value = value;
	}

	/// <summary>
	/// Enables or disables automatic execution of detected signals.
	/// </summary>
	public bool AutoTradingEnabled
	{
		get => _autoTrading.Value;
		set => _autoTrading.Value = value;
	}

	/// <summary>
	/// Pivot formula preset.
	/// </summary>
	public PivotFormulaSet PivotFormula
	{
		get => _pivotFormulaSet.Value;
		set => _pivotFormulaSet.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters and internal state containers.
	/// </summary>
	public AdvancedEaPanelStrategy()
	{
		_timeFrameStates = new TimeFrameState[PanelTimeFrames.Length];
		for (var i = 0; i < _timeFrameStates.Length; i++)
		{
			_timeFrameStates[i] = new TimeFrameState();
		}

		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Trading volume in lots", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetGreaterThanOrEqual(0m)
		.SetDisplay("Stop Loss (pips)", "Stop loss distance in price steps", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 150m, 10m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
		.SetGreaterThanOrEqual(0m)
		.SetDisplay("Take Profit (pips)", "Take profit distance in price steps", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(20m, 300m, 20m);

		_volatilityPeriod = Param(nameof(VolatilityPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Lookback for ATR volatility", "Volatility");

		_primaryCandleType = Param(nameof(PrimaryCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Primary Candle", "Timeframe for ATR and monitoring", "General");

		_pivotCandleType = Param(nameof(PivotCandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Pivot Candle", "Timeframe used for pivot levels", "General");

		_directionalThreshold = Param(nameof(DirectionalThreshold), 3)
		.SetGreaterThanZero()
		.SetDisplay("Directional Threshold", "Minimum score to act", "Signals");

		_autoTrading = Param(nameof(AutoTradingEnabled), true)
		.SetDisplay("Auto Trading", "Automatically execute signals", "Signals");

		_pivotFormulaSet = Param(nameof(PivotFormula), PivotFormulaSet.Classic)
		.SetDisplay("Pivot Formula", "Pivot calculation preset", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var seen = new HashSet<DataType>();

		IEnumerable<(Security, DataType)> Enumerate()
		{
			if (Security != null)
			{
				if (seen.Add(PrimaryCandleType))
				yield return (Security, PrimaryCandleType);
				if (seen.Add(PivotCandleType))
				yield return (Security, PivotCandleType);

				foreach (var frame in PanelTimeFrames)
				{
					var type = frame.TimeFrame();
					if (seen.Add(type))
					yield return (Security, type);
				}
			}
		}

		return Enumerate();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		foreach (var state in _timeFrameStates)
		{
			state.Reset();
		}

		_currentSignal = PanelAction.None;
		_executedSignal = PanelAction.None;
		_queuedAction = PanelAction.None;
		_isClosingPosition = false;
		_lastDirectionalScore = 0;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_currentRiskPips = null;
		_currentRewardPips = null;
		_currentRiskReward = null;
		_lastAtrValue = 0m;
		_pivotLevels = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0.0001m;

		InitializeProtection();

		_atr = new AverageTrueRange { Length = VolatilityPeriod };

		var primarySubscription = SubscribeCandles(PrimaryCandleType);
		primarySubscription
		.Bind(_atr, ProcessPrimaryCandle)
		.Start();

		var pivotSubscription = SubscribeCandles(PivotCandleType);
		pivotSubscription
		.Bind(ProcessPivotCandle)
		.Start();

		for (var i = 0; i < PanelTimeFrames.Length; i++)
		{
			var index = i;
			var candleType = PanelTimeFrames[index].TimeFrame();
			var ema3 = new ExponentialMovingAverage { Length = 3 };
			var ema6 = new ExponentialMovingAverage { Length = 6 };
			var ema9 = new ExponentialMovingAverage { Length = 9 };
			var sma50 = new SimpleMovingAverage { Length = 50 };
			var sma200 = new SimpleMovingAverage { Length = 200 };
			var cci14 = new CommodityChannelIndex { Length = 14 };
			var rsi21 = new RelativeStrengthIndex { Length = 21 };

			var subscription = SubscribeCandles(candleType);
			subscription
			.Bind(ema3, ema6, ema9, sma50, sma200, cci14, rsi21,
			(candle, ema3Value, ema6Value, ema9Value, sma50Value, sma200Value, cciValue, rsiValue) =>
			{
				if (candle.State != CandleStates.Finished)
				return;

				if (!ema3.IsFormed || !ema6.IsFormed || !ema9.IsFormed || !sma50.IsFormed || !sma200.IsFormed || !cci14.IsFormed || !rsi21.IsFormed)
				return;

				UpdateTimeFrameState(index, candle.ClosePrice, ema3Value, ema6Value, ema9Value, sma50Value, sma200Value, cciValue, rsiValue);
			})
			.Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessPrimaryCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_atr.IsFormed)
		{
			var roundedAtr = Math.Round(atrValue, 6);
			if (roundedAtr != _lastAtrValue)
			{
				_lastAtrValue = roundedAtr;
				LogInfo($"ATR({VolatilityPeriod}) updated to {roundedAtr:0.######} on {candle.OpenTime:yyyy-MM-dd HH:mm}.");
			}
		}

		UpdateRiskRewardState();
	}

	private void ProcessPivotCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_pivotLevels = CalculatePivotLevels(candle);
		LogInfo($"Pivot recalculated ({PivotFormula}) for {candle.OpenTime:yyyy-MM-dd HH:mm}. PP={_pivotLevels.Pp:0.#####}, R1={_pivotLevels.R1:0.#####}, R2={_pivotLevels.R2:0.#####}, R3={_pivotLevels.R3:0.#####}, R4={_pivotLevels.R4:0.#####}, S1={_pivotLevels.S1:0.#####}, S2={_pivotLevels.S2:0.#####}, S3={_pivotLevels.S3:0.#####}, S4={_pivotLevels.S4:0.#####}.");
	}

	private void UpdateTimeFrameState(int index, decimal close, decimal ema3, decimal ema6, decimal ema9, decimal sma50, decimal sma200, decimal cci, decimal rsi)
	{
		var state = _timeFrameStates[index];
		state.Close = close;
		state.Ema3 = ema3;
		state.Ema6 = ema6;
		state.Ema9 = ema9;
		state.Sma50 = sma50;
		state.Sma200 = sma200;
		state.Cci14 = cci;
		state.Rsi21 = rsi;
		state.IsReady = true;

		RecalculateSignal();
	}

	private void RecalculateSignal()
	{
		var score = EvaluateDirectionalScore(out var readyCount);
		if (readyCount == 0)
		return;

		var signal = PanelAction.None;
		if (score >= DirectionalThreshold)
		signal = PanelAction.Buy;
		else if (score <= -DirectionalThreshold)
		signal = PanelAction.Sell;

		if (signal != _currentSignal)
		{
			_currentSignal = signal;
			_lastDirectionalScore = score;

			if (signal == PanelAction.None)
			{
				LogInfo($"Directional score neutralized at {score} across {readyCount} timeframes.");
			}
			else
			{
				LogInfo($"Directional score {score} across {readyCount} timeframes -> {signal} signal.");

				if (AutoTradingEnabled)
				RequestExecution(signal);
			}
		}
		else if (score != _lastDirectionalScore)
		{
			_lastDirectionalScore = score;
			LogInfo($"Directional score adjusted to {score} across {readyCount} timeframes.");
		}
	}

	private int EvaluateDirectionalScore(out int readyCount)
	{
		readyCount = 0;
		var score = 0;

		foreach (var state in _timeFrameStates)
		{
			if (!state.IsReady)
			continue;

			readyCount++;
			score += GetDirectionalVote(state);
		}

		return score;
	}

	private static int GetDirectionalVote(TimeFrameState state)
	{
		var vote = 0;

		vote += state.Close > state.Ema3 ? 1 : -1;
		vote += state.Close > state.Ema6 ? 1 : -1;
		vote += state.Close > state.Ema9 ? 1 : -1;
		vote += state.Close > state.Sma50 ? 1 : -1;
		vote += state.Close > state.Sma200 ? 1 : -1;

		if (state.Cci14 >= 100m)
		vote++;
		else if (state.Cci14 <= -100m)
		vote--;

		if (state.Rsi21 >= 60m)
		vote++;
		else if (state.Rsi21 <= 40m)
		vote--;

		if (vote > 0)
		return 1;
		if (vote < 0)
		return -1;
		return 0;
	}

	private void RequestExecution(PanelAction action)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_isClosingPosition)
		{
			_queuedAction = action;
			return;
		}

		if (Position == 0)
		{
			SendOrder(action);
			return;
		}

		if ((action == PanelAction.Buy && Position > 0) || (action == PanelAction.Sell && Position < 0))
		{
			LogInfo("Position already aligned with signal direction.");
			return;
		}

		_queuedAction = action;
		_isClosingPosition = true;
		LogInfo("Closing opposite position before reversing.");
		ClosePosition();
	}

	private void SendOrder(PanelAction action)
	{
		var volume = AdjustVolume(Volume);
		if (volume <= 0)
		{
			LogWarning("Calculated trading volume is non-positive. Order skipped.");
			return;
		}

		switch (action)
		{
			case PanelAction.Buy:
				LogInfo($"Sending BUY market order for {volume}.");
				BuyMarket(volume);
				break;

			case PanelAction.Sell:
				LogInfo($"Sending SELL market order for {volume}.");
				SellMarket(volume);
				break;

			default:
				return;
		}

		_executedSignal = action;
		_queuedAction = PanelAction.None;
	}

	private decimal AdjustVolume(decimal desiredVolume)
	{
		var step = Security?.VolumeStep;
		if (step is null || step <= 0)
		return desiredVolume;

		var rounded = Math.Round(desiredVolume / step.Value, MidpointRounding.AwayFromZero) * step.Value;
		if (rounded <= 0)
		rounded = step.Value;

		return rounded;
	}

	private void InitializeProtection()
	{
		Unit? takeProfit = null;
		Unit? stopLoss = null;

		if (TakeProfitPips > 0m)
		takeProfit = new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute);

		if (StopLossPips > 0m)
		stopLoss = new Unit(StopLossPips * _pipSize, UnitTypes.Absolute);

		if (takeProfit != null || stopLoss != null)
		{
			StartProtection(takeProfit, stopLoss, useMarketOrders: true);
		}
	}

	private PivotLevels CalculatePivotLevels(ICandleMessage candle)
	{
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;
		var open = candle.OpenPrice;
		var range = high - low;

		return PivotFormula switch
		{
			PivotFormulaSet.Woodie => CalculateWoodiePivots(open, high, low, range),
			PivotFormulaSet.Camarilla => CalculateCamarillaPivots(close, range),
			_ => CalculateClassicPivots(high, low, close, range)
		};
	}

	private static PivotLevels CalculateClassicPivots(decimal high, decimal low, decimal close, decimal range)
	{
		var pp = (high + low + close) / 3m;

		return new PivotLevels
		{
			Pp = pp,
			R1 = 2m * pp - low,
			R2 = pp + range,
			R3 = pp + 2m * range,
			R4 = pp + 3m * range,
			S1 = 2m * pp - high,
			S2 = pp - range,
			S3 = pp - 2m * range,
			S4 = pp - 3m * range
		};
	}

	private static PivotLevels CalculateWoodiePivots(decimal open, decimal high, decimal low, decimal range)
	{
		var pp = (high + low + 2m * open) / 4m;

		return new PivotLevels
		{
			Pp = pp,
			R1 = 2m * pp - low,
			R2 = pp + range,
			R3 = pp + 2m * range,
			R4 = pp + 3m * range,
			S1 = 2m * pp - high,
			S2 = pp - range,
			S3 = low - 2m * (high - pp),
			S4 = pp - 3m * range
		};
	}

	private static PivotLevels CalculateCamarillaPivots(decimal close, decimal range)
	{
		var ratio = 1.1m;

		return new PivotLevels
		{
			Pp = close,
			R1 = close + range * ratio / 12m,
			R2 = close + range * ratio / 6m,
			R3 = close + range * ratio / 4m,
			R4 = close + range * ratio / 2m,
			S1 = close - range * ratio / 12m,
			S2 = close - range * ratio / 6m,
			S3 = close - range * ratio / 4m,
			S4 = close - range * ratio / 2m
		};
	}

	private void UpdateRiskRewardState()
	{
		if (_entryPrice is not decimal entry || _pipSize <= 0)
		return;

		_currentRiskPips = _stopPrice is decimal stop
		? Math.Abs(entry - stop) / _pipSize
		: null;

		_currentRewardPips = _takePrice is decimal take
		? Math.Abs(take - entry) / _pipSize
		: null;

		_currentRiskReward = _currentRiskPips is decimal risk and risk > 0m && _currentRewardPips is decimal reward
		? reward / risk
		: null;
	}

	private void LogPositionSnapshot()
	{
		if (_entryPrice is null)
		return;

		var riskText = _currentRiskPips is decimal risk ? $"{risk:0.##} pips" : "N/A";
		var rewardText = _currentRewardPips is decimal reward ? $"{reward:0.##} pips" : "N/A";
		var ratioText = _currentRiskReward is decimal ratio ? $"1:{ratio:0.##}" : "N/A";

		LogInfo($"Position snapshot -> Entry: {_entryPrice:0.#####}, Stop: {_stopPrice:0.#####}, Take: {_takePrice:0.#####}, Risk: {riskText}, Reward: {rewardText}, R/R: {ratioText}.");
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade?.Order?.Direction is null)
		return;

		if (Position == 0m)
		{
			_entryPrice = null;
			_stopPrice = null;
			_takePrice = null;
			_currentRiskPips = null;
			_currentRewardPips = null;
			_currentRiskReward = null;
			LogInfo("Position flattened.");
			return;
		}

		var direction = trade.Order.Direction;

		if (_entryPrice is null && ((Position > 0m && direction == Sides.Buy) || (Position < 0m && direction == Sides.Sell)))
		{
			_entryPrice = trade.Trade.Price;
			_stopPrice = StopLossPips > 0m
			? direction == Sides.Buy
			? _entryPrice - StopLossPips * _pipSize
			: _entryPrice + StopLossPips * _pipSize
			: null;
			_takePrice = TakeProfitPips > 0m
			? direction == Sides.Buy
			? _entryPrice + TakeProfitPips * _pipSize
			: _entryPrice - TakeProfitPips * _pipSize
			: null;

			UpdateRiskRewardState();
			LogInfo($"New position opened at {_entryPrice:0.#####} ({direction}).");
			LogPositionSnapshot();
		}
		else
		{
			UpdateRiskRewardState();
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged()
	{
		base.OnPositionChanged();

		if (Position != 0m)
		return;

		if (_isClosingPosition)
		{
			_isClosingPosition = false;
			if (_queuedAction != PanelAction.None && AutoTradingEnabled)
			{
				SendOrder(_queuedAction);
			}
			else
			{
				_queuedAction = PanelAction.None;
			}
		}
	}

	private struct PivotLevels
	{
		public decimal Pp;
		public decimal R1;
		public decimal R2;
		public decimal R3;
		public decimal R4;
		public decimal S1;
		public decimal S2;
		public decimal S3;
		public decimal S4;
	}

	private sealed class TimeFrameState
	{
		public decimal Close { get; set; }
		public decimal Ema3 { get; set; }
		public decimal Ema6 { get; set; }
		public decimal Ema9 { get; set; }
		public decimal Sma50 { get; set; }
		public decimal Sma200 { get; set; }
		public decimal Cci14 { get; set; }
		public decimal Rsi21 { get; set; }
		public bool IsReady { get; set; }

		public void Reset()
		{
			Close = 0m;
			Ema3 = 0m;
			Ema6 = 0m;
			Ema9 = 0m;
			Sma50 = 0m;
			Sma200 = 0m;
			Cci14 = 0m;
			Rsi21 = 0m;
			IsReady = false;
		}
	}
}
