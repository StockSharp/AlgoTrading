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
/// Moving average crossover strategy with adjustable gap, session control, and optional trailing stop.
/// </summary>
public class AdjustableMovingAverageStrategy : Strategy
{
	private readonly StrategyParam<TimeFrame> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<decimal> _minGapPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingPoints;
	private readonly StrategyParam<EntryMode> _entryMode;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<bool> _closeOutsideSession;
	private readonly StrategyParam<bool> _trailOutsideSession;
	private readonly StrategyParam<decimal> _fixedLot;
	private readonly StrategyParam<bool> _enableAutoLot;
	private readonly StrategyParam<decimal> _lotPer10k;
	private readonly StrategyParam<int> _maxSlippage;
	private readonly StrategyParam<string> _tradeComment;

	private LengthIndicator<decimal> _fastMa;
	private LengthIndicator<decimal> _slowMa;
	private decimal _pointValue;
	private decimal _minGapThreshold;
	private int _previousSignal;
	private bool _hasInitialSignal;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public AdjustableMovingAverageStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle timeframe", "Timeframe used to build moving averages", "General")
			.SetCanOptimize(true);

		_fastPeriod = Param(nameof(FastPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast period", "Short moving average length", "Moving averages")
			.SetCanOptimize(true)
			.SetOptimize(2, 30, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Slow period", "Long moving average length", "Moving averages")
			.SetCanOptimize(true)
			.SetOptimize(3, 60, 1);

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.Exponential)
			.SetDisplay("MA method", "Moving average calculation method", "Moving averages")
			.SetCanOptimize(true);

		_minGapPoints = Param(nameof(MinGapPoints), 3m)
			.SetNotNegative()
			.SetDisplay("Minimum gap (points)", "Required distance between fast and slow MAs before signalling", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0m, 20m, 1m);

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Stop loss (points)", "Protective stop distance in price points", "Risk management");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Take profit (points)", "Profit target distance in price points", "Risk management");

		_trailingPoints = Param(nameof(TrailStopPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Trailing stop (points)", "Trailing stop distance in price points", "Risk management");

		_entryMode = Param(nameof(Mode), EntryMode.Both)
			.SetDisplay("Entry mode", "Allowed trade direction", "Trading");

		_sessionStart = Param(nameof(SessionStart), TimeSpan.Zero)
			.SetDisplay("Session start", "Trading session start time (platform time)", "Session");

		_sessionEnd = Param(nameof(SessionEnd), new TimeSpan(23, 59, 0))
			.SetDisplay("Session end", "Trading session end time (platform time)", "Session");

		_closeOutsideSession = Param(nameof(CloseOutsideSession), true)
			.SetDisplay("Close outside session", "Allow closing positions when the session filter is inactive", "Session");

		_trailOutsideSession = Param(nameof(TrailOutsideSession), true)
			.SetDisplay("Trail outside session", "Continue trailing even when trading session is closed", "Session");

		_fixedLot = Param(nameof(FixedLot), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Fixed lot", "Volume used when auto lot sizing is disabled", "Money management");

		_enableAutoLot = Param(nameof(EnableAutoLot), false)
			.SetDisplay("Enable auto lot", "Approximate AccountFreeMargin based sizing", "Money management");

		_lotPer10k = Param(nameof(LotPer10kFreeMargin), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Lots per 10k", "Lots per 10,000 of account value when auto lot is enabled", "Money management");

		_maxSlippage = Param(nameof(MaxSlippage), 3)
			.SetNotNegative()
			.SetDisplay("Max slippage", "Placeholder parameter retained from the MQL version", "Trading");

		_tradeComment = Param(nameof(TradeComment), "AdjustableMovingAverageEA")
			.SetDisplay("Trade comment", "Tag applied to diagnostic messages", "General");
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public TimeFrame CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Moving average calculation method.
	/// </summary>
	public MovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Minimum distance between fast and slow moving averages in instrument points.
	/// </summary>
	public decimal MinGapPoints
	{
		get => _minGapPoints.Value;
		set => _minGapPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in instrument points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in instrument points.
	/// </summary>
	public decimal TrailStopPoints
	{
		get => _trailingPoints.Value;
		set => _trailingPoints.Value = value;
	}

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public EntryMode Mode
	{
		get => _entryMode.Value;
		set => _entryMode.Value = value;
	}

	/// <summary>
	/// Session start time in platform time zone.
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// Session end time in platform time zone.
	/// </summary>
	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
	}

	/// <summary>
	/// Close positions even when the session filter is inactive.
	/// </summary>
	public bool CloseOutsideSession
	{
		get => _closeOutsideSession.Value;
		set => _closeOutsideSession.Value = value;
	}

	/// <summary>
	/// Continue updating the trailing stop outside the session window.
	/// </summary>
	public bool TrailOutsideSession
	{
		get => _trailOutsideSession.Value;
		set => _trailOutsideSession.Value = value;
	}

	/// <summary>
	/// Fixed order volume used when auto lot sizing is disabled.
	/// </summary>
	public decimal FixedLot
	{
		get => _fixedLot.Value;
		set => _fixedLot.Value = value;
	}

	/// <summary>
	/// Toggle automatic lot sizing based on approximate free margin.
	/// </summary>
	public bool EnableAutoLot
	{
		get => _enableAutoLot.Value;
		set => _enableAutoLot.Value = value;
	}

	/// <summary>
	/// Lots allocated per 10,000 units of portfolio value.
	/// </summary>
	public decimal LotPer10kFreeMargin
	{
		get => _lotPer10k.Value;
		set => _lotPer10k.Value = value;
	}

	/// <summary>
	/// Placeholder for the original slippage tolerance.
	/// </summary>
	public int MaxSlippage
	{
		get => _maxSlippage.Value;
		set => _maxSlippage.Value = value;
	}

	/// <summary>
	/// Comment attached to log messages when orders are placed.
	/// </summary>
	public string TradeComment
	{
		get => _tradeComment.Value;
		set => _tradeComment.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastMa = null;
		_slowMa = null;
		_pointValue = 0m;
		_minGapThreshold = 0m;
		_previousSignal = 0;
		_hasInitialSignal = false;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastLength = Math.Min(FastPeriod, SlowPeriod);
		var slowLength = Math.Max(FastPeriod, SlowPeriod);

		if (fastLength == slowLength)
		{
			LogWarning("Fast and slow periods must differ.");
			Stop();
			return;
		}

		_fastMa = CreateMovingAverage(MaMethod, fastLength);
		_slowMa = CreateMovingAverage(MaMethod, slowLength);

		_pointValue = CalculatePointValue();
		_minGapThreshold = MinGapPoints * _pointValue;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var inSession = InSession(candle.OpenTime);
		var allowTrading = inSession && IsFormedAndOnlineAndAllowTrading();

		UpdateTrailing(candle, inSession || TrailOutsideSession);
		HandleProtectiveExits(candle);

		if (_fastMa == null || _slowMa == null)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
			return;

		if (!IsFormedAndOnline())
			return;

		var gapUp = fast - slow;
		var gapDown = slow - fast;

		if (!_hasInitialSignal)
		{
			if (gapUp >= _minGapThreshold)
			{
				_previousSignal = 1;
				_hasInitialSignal = true;
			}
			else if (gapDown >= _minGapThreshold)
			{
				_previousSignal = -1;
				_hasInitialSignal = true;
			}
			return;
		}

		if (_previousSignal > 0)
		{
			if (gapDown >= _minGapThreshold)
			{
				if (CloseOutsideSession || inSession)
					CloseCurrentPosition();

				if (allowTrading && Mode != EntryMode.BuyOnly)
				{
					OpenShort(candle.ClosePrice);
				}

				_previousSignal = -1;
				ResetTrailing();
			}
		}
		else if (_previousSignal < 0)
		{
			if (gapUp >= _minGapThreshold)
			{
				if (CloseOutsideSession || inSession)
					CloseCurrentPosition();

				if (allowTrading && Mode != EntryMode.SellOnly)
				{
					OpenLong(candle.ClosePrice);
				}

				_previousSignal = 1;
				ResetTrailing();
			}
		}
	}

	private void OpenLong(decimal price)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = CalculateOrderVolume(price);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		LogInfo($"{TradeComment}: opened long, volume={volume:0.###}");
	}

	private void OpenShort(decimal price)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = CalculateOrderVolume(price);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		LogInfo($"{TradeComment}: opened short, volume={volume:0.###}");
	}

	private void CloseCurrentPosition()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
			LogInfo($"{TradeComment}: closed existing long");
		}
		else if (Position < 0m)
		{
			BuyMarket(-Position);
			LogInfo($"{TradeComment}: closed existing short");
		}
	}

	private void UpdateTrailing(ICandleMessage candle, bool allowUpdate)
	{
		if (TrailStopPoints <= 0m || _pointValue <= 0m)
			return;

		var distance = TrailStopPoints * _pointValue;

		if (Position > 0m)
		{
			if (allowUpdate)
			{
				var move = candle.ClosePrice - PositionPrice;
				if (move >= distance)
				{
					var newStop = candle.ClosePrice - distance;
					if (!_longTrailingStop.HasValue || newStop > _longTrailingStop.Value)
						_longTrailingStop = newStop;
				}
			}

			if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
			{
				SellMarket(Position);
				LogInfo($"{TradeComment}: trailing stop hit (long)");
				ResetTrailing();
			}
		}
		else if (Position < 0m)
		{
			var absPosition = -Position;

			if (allowUpdate)
			{
				var move = PositionPrice - candle.ClosePrice;
				if (move >= distance)
				{
					var newStop = candle.ClosePrice + distance;
					if (!_shortTrailingStop.HasValue || newStop < _shortTrailingStop.Value)
						_shortTrailingStop = newStop;
				}
			}

			if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
			{
				BuyMarket(absPosition);
				LogInfo($"{TradeComment}: trailing stop hit (short)");
				ResetTrailing();
			}
		}
		else
		{
			ResetTrailing();
		}
	}

	private void HandleProtectiveExits(ICandleMessage candle)
	{
		if (_pointValue <= 0m)
			return;

		if (Position > 0m)
		{
			var stop = StopLossPoints > 0m ? PositionPrice - StopLossPoints * _pointValue : (decimal?)null;
			var target = TakeProfitPoints > 0m ? PositionPrice + TakeProfitPoints * _pointValue : (decimal?)null;

			if (stop.HasValue && candle.LowPrice <= stop.Value)
			{
				SellMarket(Position);
				LogInfo($"{TradeComment}: stop-loss hit (long)");
				ResetTrailing();
				return;
			}

			if (target.HasValue && candle.HighPrice >= target.Value)
			{
				SellMarket(Position);
				LogInfo($"{TradeComment}: take-profit hit (long)");
				ResetTrailing();
			}
		}
		else if (Position < 0m)
		{
			var absPosition = -Position;
			var stop = StopLossPoints > 0m ? PositionPrice + StopLossPoints * _pointValue : (decimal?)null;
			var target = TakeProfitPoints > 0m ? PositionPrice - TakeProfitPoints * _pointValue : (decimal?)null;

			if (stop.HasValue && candle.HighPrice >= stop.Value)
			{
				BuyMarket(absPosition);
				LogInfo($"{TradeComment}: stop-loss hit (short)");
				ResetTrailing();
				return;
			}

			if (target.HasValue && candle.LowPrice <= target.Value)
			{
				BuyMarket(absPosition);
				LogInfo($"{TradeComment}: take-profit hit (short)");
				ResetTrailing();
			}
		}
		else
		{
			ResetTrailing();
		}
	}

	private decimal CalculateOrderVolume(decimal price)
	{
		var desired = FixedLot;

		if (EnableAutoLot)
		{
			var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
			if (equity is decimal value && value > 0m && price > 0m)
			{
				var lots = Math.Round((value / 10000m) * LotPer10kFreeMargin, 1, MidpointRounding.AwayFromZero);
				if (lots > 0m)
					desired = lots;
			}
		}

		var adjusted = AdjustVolume(desired);
		return adjusted > 0m ? adjusted : 0m;
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		var minVolume = security.MinVolume;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume.Value;

		var maxVolume = security.MaxVolume;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume.Value;

		return volume;
	}

	private bool InSession(DateTimeOffset time)
	{
		var start = SessionStart;
		var end = SessionEnd;
		var current = time.TimeOfDay;

		if (end < start)
		{
			return current >= start || current <= end;
		}

		return current >= start && current <= end;
	}

	private decimal CalculatePointValue()
	{
		var step = Security?.PriceStep ?? Security?.Step ?? 0m;
		if (step <= 0m)
			return 0m;

		var point = step;
		if (point == 0.00001m || point == 0.001m)
			point *= 10m;

		return point;
	}

	private LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int length)
	{
		LengthIndicator<decimal> indicator = method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length, CandlePrice = CandlePrice.Close },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length, CandlePrice = CandlePrice.Close },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length, CandlePrice = CandlePrice.Close },
			MovingAverageMethod.Weighted => new WeightedMovingAverage { Length = length, CandlePrice = CandlePrice.Close },
			_ => new ExponentialMovingAverage { Length = length, CandlePrice = CandlePrice.Close }
		};

		return indicator;
	}

	private void ResetTrailing()
	{
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}
}

/// <summary>
/// Moving average calculation methods supported by the strategy.
/// </summary>
public enum MovingAverageMethod
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	Simple,

	/// <summary>
	/// Exponential moving average.
	/// </summary>
	Exponential,

	/// <summary>
	/// Smoothed moving average.
	/// </summary>
	Smoothed,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Weighted
}

/// <summary>
/// Directional filter for new positions.
/// </summary>
public enum EntryMode
{
	/// <summary>
	/// Allow both long and short entries.
	/// </summary>
	Both,

	/// <summary>
	/// Allow only long entries.
	/// </summary>
	BuyOnly,

	/// <summary>
	/// Allow only short entries.
	/// </summary>
	SellOnly
}

