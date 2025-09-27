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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 expert advisor "gpfTCPivotLimit".
/// Trades around daily pivot levels using confirmation from the two most recent candles.
/// Includes optional volume adaptation, trailing stop, and end-of-day liquidation.
/// </summary>
public class GpfTcpPivotLimitStrategy : Strategy
{
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<bool> _useDynamicVolume;
	private readonly StrategyParam<decimal> _riskPercentage;
	private readonly StrategyParam<decimal> _drawdownFactor;
	private readonly StrategyParam<int> _targetMode;
	private readonly StrategyParam<int> _trailingPoints;
	private readonly StrategyParam<bool> _closeAtSessionEnd;
	private readonly StrategyParam<bool> _logSignals;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _currentDay;
	private decimal _dayHigh;
	private decimal _dayLow;
	private decimal _dayClose;

	private PivotLevels? _activeLevels;
	private DateTime? _levelsDay;

	private ICandleMessage _previousCandle;
	private ICandleMessage _twoAgoCandle;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	private Sides? _entrySide;
	private decimal? _entryPrice;
	private decimal? _lastTradePrice;

	private int _consecutiveLosses;

	private readonly struct PivotLevels
	{
		public PivotLevels(decimal pivot, decimal r1, decimal r2, decimal r3, decimal s1, decimal s2, decimal s3)
		{
			Pivot = pivot;
			R1 = r1;
			R2 = r2;
			R3 = r3;
			S1 = s1;
			S2 = s2;
			S3 = s3;
		}

		public decimal Pivot { get; }
		public decimal R1 { get; }
		public decimal R2 { get; }
		public decimal R3 { get; }
		public decimal S1 { get; }
		public decimal S2 { get; }
		public decimal S3 { get; }
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GpfTcpPivotLimitStrategy"/> class.
	/// </summary>
	public GpfTcpPivotLimitStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 1m)
		.SetDisplay("Base Volume", "Default order volume before risk adjustments.", "Trading")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_useDynamicVolume = Param(nameof(UseDynamicVolume), false)
		.SetDisplay("Use Dynamic Volume", "Reduce volume after losing streaks.", "Risk");

		_riskPercentage = Param(nameof(RiskPercentage), 0.02m)
		.SetDisplay("Risk Percentage", "Reference risk per trade used to scale the base volume.", "Risk")
		.SetNotNegative()
		.SetCanOptimize(true);

		_drawdownFactor = Param(nameof(DrawdownFactor), 3m)
		.SetDisplay("Drawdown Factor", "Divisor applied when reducing the volume after consecutive losses.", "Risk")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_targetMode = Param(nameof(TargetMode), 1)
		.SetDisplay("Target Mode", "Pivot combination preset replicated from the MT4 input (1-5).", "Logic")
		.SetOptimize(1, 5, 1);

		_trailingPoints = Param(nameof(TrailingPoints), 30)
		.SetDisplay("Trailing Points", "Trailing stop distance expressed in instrument points (0 disables).", "Risk")
		.SetNotNegative()
		.SetCanOptimize(true);

		_closeAtSessionEnd = Param(nameof(CloseAtSessionEnd), false)
		.SetDisplay("Close At 23:00", "Flatten the position at the end of the trading day.", "Risk");

		_logSignals = Param(nameof(LogSignals), false)
		.SetDisplay("Verbose Logging", "Write pivot updates and trade events to the log.", "Diagnostics");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Time-frame used to calculate pivots and signals.", "Data");
	}

	/// <summary>
	/// Base order volume before any risk adjustments.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Enable or disable the adaptive volume algorithm.
	/// </summary>
	public bool UseDynamicVolume
	{
		get => _useDynamicVolume.Value;
		set => _useDynamicVolume.Value = value;
	}

	/// <summary>
	/// Reference risk percentage per trade used when scaling the base volume.
	/// </summary>
	public decimal RiskPercentage
	{
		get => _riskPercentage.Value;
		set => _riskPercentage.Value = value;
	}

	/// <summary>
	/// Factor applied when reducing the volume after a losing streak.
	/// </summary>
	public decimal DrawdownFactor
	{
		get => _drawdownFactor.Value;
		set => _drawdownFactor.Value = value;
	}

	/// <summary>
	/// Pivot level preset that selects which support/resistance combination will be traded.
	/// </summary>
	public int TargetMode
	{
		get => _targetMode.Value;
		set => _targetMode.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in raw instrument points.
	/// </summary>
	public int TrailingPoints
	{
		get => _trailingPoints.Value;
		set => _trailingPoints.Value = value;
	}

	/// <summary>
	/// Close open positions at the end of the day (23:00 platform time).
	/// </summary>
	public bool CloseAtSessionEnd
	{
		get => _closeAtSessionEnd.Value;
		set => _closeAtSessionEnd.Value = value;
	}

	/// <summary>
	/// Write diagnostic messages with pivot values and trade actions.
	/// </summary>
	public bool LogSignals
	{
		get => _logSignals.Value;
		set => _logSignals.Value = value;
	}

	/// <summary>
	/// Candle type used for the high-level subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentDay = null;
		_dayHigh = _dayLow = _dayClose = 0m;
		_activeLevels = null;
		_levelsDay = null;
		_previousCandle = null;
		_twoAgoCandle = null;
		_longStop = _longTake = _shortStop = _shortTake = null;
		_entrySide = null;
		_entryPrice = null;
		_lastTradePrice = null;
		_consecutiveLosses = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = BaseVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateDailyLevels(candle);

		if (_twoAgoCandle == null || _previousCandle == null || _activeLevels == null)
		{
			ShiftCandles(candle);
			return;
		}

		ManagePosition(candle);

		if (Position == 0m)
			EvaluateEntry(candle);

		ShiftCandles(candle);
	}

	private void UpdateDailyLevels(ICandleMessage candle)
	{
		var candleDay = candle.OpenTime.UtcDateTime.Date;

		if (_currentDay == null)
		{
			_currentDay = candleDay;
			_dayHigh = candle.HighPrice;
			_dayLow = candle.LowPrice;
			_dayClose = candle.ClosePrice;
			return;
		}

		if (_currentDay == candleDay)
		{
			_dayHigh = Math.Max(_dayHigh, candle.HighPrice);
			_dayLow = Math.Min(_dayLow, candle.LowPrice);
			_dayClose = candle.ClosePrice;
			return;
		}

		if (_dayHigh > 0m && _dayLow > 0m)
		{
			var levels = BuildPivotLevels();
			_activeLevels = levels;
			_levelsDay = candleDay;

			if (LogSignals)
			{
				LogInfo($"Pivot levels for {candleDay:yyyy-MM-dd}: P={levels.Pivot:F5}, R1={levels.R1:F5}, R2={levels.R2:F5}, R3={levels.R3:F5}, S1={levels.S1:F5}, S2={levels.S2:F5}, S3={levels.S3:F5}");
			}
		}

		_currentDay = candleDay;
		_dayHigh = candle.HighPrice;
		_dayLow = candle.LowPrice;
		_dayClose = candle.ClosePrice;
	}

	private PivotLevels BuildPivotLevels()
	{
		var pivot = (_dayHigh + _dayLow + _dayClose) / 3m;
		var range = _dayHigh - _dayLow;
		var r1 = 2m * pivot - _dayLow;
		var s1 = 2m * pivot - _dayHigh;
		var r2 = pivot + range;
		var s2 = pivot - range;
		var r3 = _dayHigh + 2m * (pivot - _dayLow);
		var s3 = _dayLow - 2m * (_dayHigh - pivot);
		return new PivotLevels(pivot, r1, r2, r3, s1, s2, s3);
	}

	private void EvaluateEntry(ICandleMessage candle)
	{
		if (_activeLevels == null)
			return;

		var levels = _activeLevels.Value;
		var plan = CalculateTradePlan(levels);

		var volume = CalculateTradeVolume();
		if (volume <= 0m)
			return;

		if (plan.BuyTrigger.HasValue && _twoAgoCandle != null && _previousCandle != null)
		{
			var trigger = plan.BuyTrigger.Value;
			if (((_twoAgoCandle.LowPrice < trigger) || (_twoAgoCandle.ClosePrice <= trigger)) && _twoAgoCandle.OpenPrice > trigger && _previousCandle.ClosePrice >= trigger)
			{
				var order = BuyMarket(volume);
				if (order != null)
				{
					_longStop = plan.BuyStop;
					_longTake = plan.BuyTake;
					_shortStop = null;
					_shortTake = null;
					if (LogSignals)
						LogInfo($"BUY triggered at {trigger:F5} using TargetMode {TargetMode}.");
				}
			}
		}

		if (plan.SellTrigger.HasValue && _twoAgoCandle != null && _previousCandle != null && Position == 0m)
		{
			var trigger = plan.SellTrigger.Value;
			if (((_twoAgoCandle.HighPrice > trigger) || (_twoAgoCandle.ClosePrice >= trigger)) && _twoAgoCandle.OpenPrice < trigger && _previousCandle.ClosePrice <= trigger)
			{
				var order = SellMarket(volume);
				if (order != null)
				{
					_shortStop = plan.SellStop;
					_shortTake = plan.SellTake;
					_longStop = null;
					_longTake = null;
					if (LogSignals)
						LogInfo($"SELL triggered at {trigger:F5} using TargetMode {TargetMode}.");
				}
			}
		}
	}

	private (decimal? BuyTrigger, decimal? BuyStop, decimal? BuyTake, decimal? SellTrigger, decimal? SellStop, decimal? SellTake) CalculateTradePlan(PivotLevels levels)
	{
		decimal? buyTrigger = null;
		decimal? buyStop = null;
		decimal? buyTake = null;
		decimal? sellTrigger = null;
		decimal? sellStop = null;
		decimal? sellTake = null;

		switch (Math.Max(1, Math.Min(5, TargetMode)))
		{
			case 1:
				buyTrigger = levels.S1;
				buyStop = levels.S2;
				buyTake = levels.R1;
				sellTrigger = levels.R1;
				sellStop = levels.R2;
				sellTake = levels.S1;
				break;
			case 2:
				buyTrigger = levels.S1;
				buyStop = levels.S2;
				buyTake = levels.R2;
				sellTrigger = levels.R1;
				sellStop = levels.R2;
				sellTake = levels.S2;
				break;
			case 3:
				buyTrigger = levels.S2;
				buyStop = levels.S3;
				buyTake = levels.R1;
				sellTrigger = levels.R2;
				sellStop = levels.R3;
				sellTake = levels.S1;
				break;
			case 4:
				buyTrigger = levels.S2;
				buyStop = levels.S3;
				buyTake = levels.R2;
				sellTrigger = levels.R2;
				sellStop = levels.R3;
				sellTake = levels.S2;
				break;
			case 5:
				buyTrigger = levels.S2;
				buyStop = levels.S3;
				buyTake = levels.R3;
				sellTrigger = levels.R2;
				sellStop = levels.R3;
				sellTake = levels.S3;
				break;
		}

		return (buyTrigger, buyStop, buyTake, sellTrigger, sellStop, sellTake);
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position == 0m)
		{
			return;
		}

		ApplyTrailing(candle);

		if (Position > 0m)
		{
			if (_longStop is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(Position));
				if (LogSignals)
					LogInfo($"Long stop triggered at {stop:F5}.");
				_longStop = null;
				_longTake = null;
				return;
			}

			if (_longTake is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Math.Abs(Position));
				if (LogSignals)
					LogInfo($"Long target reached at {take:F5}.");
				_longStop = null;
				_longTake = null;
				return;
			}
		}
		else if (Position < 0m)
		{
			if (_shortStop is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				if (LogSignals)
					LogInfo($"Short stop triggered at {stop:F5}.");
				_shortStop = null;
				_shortTake = null;
				return;
			}

			if (_shortTake is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				if (LogSignals)
					LogInfo($"Short target reached at {take:F5}.");
				_shortStop = null;
				_shortTake = null;
				return;
			}
		}

		if (CloseAtSessionEnd)
		{
			var closeTime = candle.CloseTime.UtcDateTime.TimeOfDay;
			if (closeTime >= new TimeSpan(23, 0, 0))
			{
				if (Position > 0m)
				{
					SellMarket(Math.Abs(Position));
				}
				else if (Position < 0m)
				{
					BuyMarket(Math.Abs(Position));
				}

				_longStop = _longTake = _shortStop = _shortTake = null;

				if (LogSignals)
					LogInfo("Position closed at session end.");
			}
		}
	}

	private void ApplyTrailing(ICandleMessage candle)
	{
		var trailingDistance = GetTrailingDistance();
		if (trailingDistance <= 0m || _entryPrice == null || _entrySide == null)
			return;

		if (Position > 0m)
		{
			var entryPrice = _entryPrice.Value;
			var candidate = candle.ClosePrice - trailingDistance;
			if (candle.ClosePrice - entryPrice > trailingDistance && (_longStop == null || candidate > _longStop.Value))
			{
				_longStop = candidate;
				if (LogSignals)
					LogInfo($"Trailing stop for long moved to {candidate:F5}.");
			}
		}
		else if (Position < 0m)
		{
			var entryPrice = _entryPrice.Value;
			var candidate = candle.ClosePrice + trailingDistance;
			if (entryPrice - candle.ClosePrice > trailingDistance && (_shortStop == null || candidate < _shortStop.Value))
			{
				_shortStop = candidate;
				if (LogSignals)
					LogInfo($"Trailing stop for short moved to {candidate:F5}.");
			}
		}
	}

	private decimal GetTrailingDistance()
	{
		if (TrailingPoints <= 0)
			return 0m;

		var security = Security;
		if (security == null)
			return 0m;

		var step = security.PriceStep ?? security.Step ?? 0m;
		return step > 0m ? TrailingPoints * step : 0m;
	}

	private decimal CalculateTradeVolume()
	{
		var volume = BaseVolume;

		if (RiskPercentage > 0m)
		{
			volume *= RiskPercentage / 0.02m;
		}

		if (UseDynamicVolume && _consecutiveLosses > 1 && DrawdownFactor > 0m)
		{
			var reduction = volume * _consecutiveLosses / DrawdownFactor;
			volume -= reduction;
		}

		var security = Security;
		if (security != null)
		{
			var minVolume = security.MinVolume ?? security.VolumeStep ?? 0.01m;
			if (volume < minVolume)
				volume = minVolume;
		}

		return volume;
	}

	private void ShiftCandles(ICandleMessage candle)
	{
		_twoAgoCandle = _previousCandle;
		_previousCandle = candle;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null)
			return;

		_lastTradePrice = trade.Trade.Price;

		if (Position != 0m)
		{
			_entrySide ??= Position > 0m ? Sides.Buy : Sides.Sell;
			_entryPrice ??= trade.Trade.Price;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			if (_entrySide != null && _entryPrice != null && _lastTradePrice != null)
			{
				var entry = _entryPrice.Value;
				var exit = _lastTradePrice.Value;
				var pnl = _entrySide == Sides.Buy ? exit - entry : entry - exit;

				if (pnl > 0m)
				{
					_consecutiveLosses = 0;
				}
				else if (pnl < 0m)
				{
					_consecutiveLosses++;
				}
			}

			_entrySide = null;
			_entryPrice = null;
			_lastTradePrice = null;
		}
		else if (_entrySide == null && _lastTradePrice != null)
		{
			_entrySide = Position > 0m ? Sides.Buy : Sides.Sell;
			_entryPrice = _lastTradePrice;
		}
	}
}