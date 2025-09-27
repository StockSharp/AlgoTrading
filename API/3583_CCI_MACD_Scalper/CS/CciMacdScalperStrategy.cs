namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class CciMacdScalperStrategy : Strategy
{
	private readonly StrategyParam<int> _cooldownBars;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _minHour;
	private readonly StrategyParam<int> _maxHour;
	private readonly StrategyParam<decimal> _minimalStopLossPoints;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPoints;

	private decimal? _previousEma;
	private decimal? _previousCci;
	private decimal? _previousPreviousCci;
	private decimal? _previousMacd;
	private decimal? _previousPreviousMacd;
	private decimal? _previousSignal;
	private decimal? _previousPreviousSignal;

	private readonly Queue<decimal> _recentLows = new();
	private readonly Queue<decimal> _recentHighs = new();

	private decimal? _longStopLoss;
	private decimal? _longTakeProfit;
	private decimal? _shortStopLoss;
	private decimal? _shortTakeProfit;

	private decimal? _entryVolume;
	private int _trailingMoves;

	private DateTimeOffset? _nextEntryTime;
	private TimeSpan _baseFrame = TimeSpan.Zero;

	public CciMacdScalperStrategy()
	{
		_cooldownBars = Param(nameof(CooldownBars), 5)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown bars", "Number of completed candles required before reopening trades.", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle type", "Timeframe processed by the strategy.", "General");

		_riskPercent = Param(nameof(RiskPercent), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Risk %", "Percentage of equity risked per trade.", "Money Management");

		_riskReward = Param(nameof(RiskReward), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Risk/Reward", "Reward-to-risk ratio for profit target placement.", "Money Management");

		_emaPeriod = Param(nameof(EmaPeriod), 34)
		.SetGreaterThanZero()
		.SetDisplay("EMA period", "Length of the exponential moving average used as trend filter.", "Indicator");

		_cciPeriod = Param(nameof(CciPeriod), 50)
		.SetGreaterThanZero()
		.SetDisplay("CCI period", "Length of the Commodity Channel Index used for zero-line crosses.", "Indicator");

		_minHour = Param(nameof(MinHour), 0)
		.SetDisplay("Start hour", "Hour from which trading is allowed (inclusive).", "Session");

		_maxHour = Param(nameof(MaxHour), 24)
		.SetDisplay("End hour", "Hour until which trading is allowed (inclusive).", "Session");

		_minimalStopLossPoints = Param(nameof(MinimalStopLossPoints), 100m)
		.SetNotNegative()
		.SetDisplay("Min SL points", "Minimal distance between entry and stop loss expressed in points.", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
		.SetDisplay("Use trailing", "Enable dynamic stop loss trailing.", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 100m)
		.SetNotNegative()
		.SetDisplay("Trailing points", "Distance in points used when trailing a protective stop.", "Risk");
	}

	/// <summary>
	/// Number of completed candles required before a new entry can be opened.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	public int MinHour
	{
		get => _minHour.Value;
		set => _minHour.Value = value;
	}

	public int MaxHour
	{
		get => _maxHour.Value;
		set => _maxHour.Value = value;
	}

	public decimal MinimalStopLossPoints
	{
		get => _minimalStopLossPoints.Value;
		set => _minimalStopLossPoints.Value = value;
	}

	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = 1m;
		ResetState();

		_baseFrame = TryGetTimeFrame(CandleType, out var frame) ? frame : TimeSpan.Zero;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(ema, cci, macd, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, cci);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue cciValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		ManageActivePosition(candle);

		if (!emaValue.IsFinal || !cciValue.IsFinal || !macdValue.IsFinal)
		{
			UpdatePriceExtremes(candle);
			return;
		}

		var emaCurrent = emaValue.ToDecimal();
		var cciCurrent = cciValue.ToDecimal();
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macdCurrent || macdTyped.Signal is not decimal signalCurrent)
		{
			UpdatePriceExtremes(candle);
			return;
		}

		var prevEma = _previousEma;
		var prevCci = _previousCci;
		var prevPrevCci = _previousPreviousCci;
		var prevMacd = _previousMacd;
		var prevPrevMacd = _previousPreviousMacd;
		var prevSignal = _previousSignal;
		var prevPrevSignal = _previousPreviousSignal;

		_previousPreviousCci = prevCci;
		_previousCci = cciCurrent;

		_previousPreviousMacd = prevMacd;
		_previousMacd = macdCurrent;

		_previousPreviousSignal = prevSignal;
		_previousSignal = signalCurrent;

		_previousEma = emaCurrent;

		var hasEnoughHistory = prevEma.HasValue && prevCci.HasValue && prevPrevCci.HasValue && prevMacd.HasValue && prevPrevMacd.HasValue && prevSignal.HasValue && prevPrevSignal.HasValue;

		if (!hasEnoughHistory)
		{
			UpdatePriceExtremes(candle);
			return;
		}

		var recentLow = GetRecentExtreme(_recentLows, true);
		var recentHigh = GetRecentExtreme(_recentHighs, false);

		var hasExtremes = recentLow.HasValue && recentHigh.HasValue;
		if (!hasExtremes)
		{
			UpdatePriceExtremes(candle);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdatePriceExtremes(candle);
			return;
		}

		if (!IsWithinTradingHours(candle.CloseTime))
		{
			UpdatePriceExtremes(candle);
			return;
		}

		if (_nextEntryTime.HasValue && candle.CloseTime < _nextEntryTime.Value)
		{
			UpdatePriceExtremes(candle);
			return;
		}

		var minimalStop = GetPriceByPoints(MinimalStopLossPoints);

		var macdOversold = prevPrevMacd.Value < 0m && prevPrevSignal.Value < 0m && prevMacd.Value < 0m && prevSignal.Value < 0m && prevPrevSignal.Value < prevPrevMacd.Value && prevSignal.Value > prevMacd.Value;
		var macdOverbought = prevPrevMacd.Value > 0m && prevPrevSignal.Value > 0m && prevMacd.Value > 0m && prevSignal.Value > 0m && prevPrevSignal.Value > prevPrevMacd.Value && prevSignal.Value < prevMacd.Value;

		var cciCrossUp = prevPrevCci.Value < 0m && prevCci.Value > 0m;
		var cciCrossDown = prevPrevCci.Value > 0m && prevCci.Value < 0m;

		if (Position <= 0m && candle.ClosePrice > prevEma.Value && cciCrossUp && macdOversold)
		{
			TryEnterLong(candle, recentLow.Value, minimalStop);
		}
		else if (Position >= 0m && candle.ClosePrice < prevEma.Value && cciCrossDown && macdOverbought)
		{
			TryEnterShort(candle, recentHigh.Value, minimalStop);
		}

		UpdatePriceExtremes(candle);
	}

	private void TryEnterLong(ICandleMessage candle, decimal stopCandidate, decimal minimalStop)
	{
		var entryPrice = candle.ClosePrice;
		var stopDistance = entryPrice - stopCandidate;
		if (stopDistance <= 0m || stopDistance < minimalStop)
		return;

		var volume = CalculatePositionSize(stopDistance);
		if (volume <= 0m)
		return;

		var takeProfitDistance = stopDistance * RiskReward;
		if (takeProfitDistance <= 0m)
		return;

		var totalVolume = volume + Math.Max(0m, -Position);
		if (totalVolume <= 0m)
		return;

		BuyMarket(totalVolume);

		_longStopLoss = stopCandidate;
		_longTakeProfit = entryPrice + takeProfitDistance;
		_shortStopLoss = null;
		_shortTakeProfit = null;
		_entryVolume = volume;
		_trailingMoves = 0;
		_nextEntryTime = candle.CloseTime + GetCooldownSpan();
	}

	private void TryEnterShort(ICandleMessage candle, decimal stopCandidate, decimal minimalStop)
	{
		var entryPrice = candle.ClosePrice;
		var stopDistance = stopCandidate - entryPrice;
		if (stopDistance <= 0m || stopDistance < minimalStop)
		return;

		var volume = CalculatePositionSize(stopDistance);
		if (volume <= 0m)
		return;

		var takeProfitDistance = stopDistance * RiskReward;
		if (takeProfitDistance <= 0m)
		return;

		var totalVolume = volume + Math.Max(0m, Position);
		if (totalVolume <= 0m)
		return;

		SellMarket(totalVolume);

		_shortStopLoss = stopCandidate;
		_shortTakeProfit = entryPrice - takeProfitDistance;
		_longStopLoss = null;
		_longTakeProfit = null;
		_entryVolume = volume;
		_trailingMoves = 0;
		_nextEntryTime = candle.CloseTime + GetCooldownSpan();
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longStopLoss is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetLongState();
				return;
			}

			if (_longTakeProfit is decimal takeProfit && candle.HighPrice >= takeProfit)
			{
				SellMarket(Position);
				ResetLongState();
				return;
			}

			if (UseTrailingStop)
			{
				var trailingDistance = GetPriceByPoints(TrailingStopPoints);
				if (trailingDistance > 0m)
				{
					var newStop = candle.ClosePrice - trailingDistance;
					if (_longStopLoss is decimal currentStop && newStop > currentStop)
					{
						_longStopLoss = newStop;
						HandlePartialProfit(true);
					}
				}
			}
		}
		else if (Position < 0m)
		{
			var absPosition = Math.Abs(Position);
			if (_shortStopLoss is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(absPosition);
				ResetShortState();
				return;
			}

			if (_shortTakeProfit is decimal takeProfit && candle.LowPrice <= takeProfit)
			{
				BuyMarket(absPosition);
				ResetShortState();
				return;
			}

			if (UseTrailingStop)
			{
				var trailingDistance = GetPriceByPoints(TrailingStopPoints);
				if (trailingDistance > 0m)
				{
					var newStop = candle.ClosePrice + trailingDistance;
					if (_shortStopLoss is decimal currentStop && newStop < currentStop)
					{
						_shortStopLoss = newStop;
						HandlePartialProfit(false);
					}
				}
			}
		}
		else
		{
			ResetPositionState();
		}
	}

	private void HandlePartialProfit(bool isLong)
	{
		if (_trailingMoves > 0)
		return;

		if (_entryVolume is not decimal entryVolume || entryVolume <= 0m)
		{
			_trailingMoves++;
			return;
		}

		var halfVolume = entryVolume / 2m;
		if (halfVolume <= 0m)
		{
			_trailingMoves++;
			return;
		}

		if (isLong)
		{
			var volumeToClose = Math.Min(halfVolume, Position);
			if (volumeToClose > 0m)
			SellMarket(volumeToClose);
		}
		else
		{
			var volumeToClose = Math.Min(halfVolume, Math.Abs(Position));
			if (volumeToClose > 0m)
			BuyMarket(volumeToClose);
		}

		_trailingMoves++;
	}

	private void UpdatePriceExtremes(ICandleMessage candle)
	{
		if (_recentLows.Count == 5)
		_recentLows.Dequeue();

		if (_recentHighs.Count == 5)
		_recentHighs.Dequeue();

		_recentLows.Enqueue(candle.LowPrice);
		_recentHighs.Enqueue(candle.HighPrice);
	}

	private decimal? GetRecentExtreme(Queue<decimal> values, bool isLow)
	{
		if (values.Count < 5)
		return null;

		var enumerator = values.GetEnumerator();
		if (!enumerator.MoveNext())
		return null;

		var extreme = enumerator.Current;

		while (enumerator.MoveNext())
		{
			var value = enumerator.Current;
			if (isLow)
			{
				if (value < extreme)
				extreme = value;
			}
			else if (value > extreme)
			{
				extreme = value;
			}
		}

		return extreme;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var hour = time.LocalDateTime.Hour;
		return hour >= MinHour && hour <= MaxHour;
	}

	private decimal GetPriceByPoints(decimal points)
	{
		if (points <= 0m)
		return 0m;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		step = 1m;

		return points * step;
	}

	private decimal CalculatePositionSize(decimal stopDistance)
	{
		if (stopDistance <= 0m)
		return 0m;

		var security = Security;
		if (security == null)
		return Volume;

		var portfolio = Portfolio;
		var equity = portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
		equity = portfolio?.BeginValue ?? 0m;

		if (equity <= 0m)
		return Volume;

		var riskAmount = equity * (RiskPercent / 100m);
		if (riskAmount <= 0m)
		return Volume;

		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
		priceStep = 1m;

		var stepPrice = security.StepPrice ?? priceStep;
		if (stepPrice <= 0m)
		stepPrice = priceStep;

		var volumeStep = security.StepVolume ?? 1m;
		if (volumeStep <= 0m)
		volumeStep = 1m;

		var steps = stopDistance / priceStep;
		if (steps <= 0m)
		return Volume;

		var riskPerUnit = steps * stepPrice;
		if (riskPerUnit <= 0m)
		return Volume;

		var rawVolume = riskAmount / riskPerUnit;
		if (rawVolume <= 0m)
		return Volume;

		var stepCount = Math.Floor((double)(rawVolume / volumeStep));
		var normalized = (decimal)stepCount * volumeStep;
		if (normalized <= 0m)
		normalized = volumeStep;

		var minVolume = security.MinVolume;
		if (minVolume > 0m && normalized < minVolume.Value)
		normalized = minVolume.Value;

		var maxVolume = security.MaxVolume;
		if (maxVolume > 0m && normalized > maxVolume.Value)
		normalized = maxVolume.Value;

		return normalized;
	}

	private TimeSpan GetCooldownSpan()
	{
		var frame = _baseFrame > TimeSpan.Zero ? _baseFrame : TimeSpan.FromMinutes(1);
		return TimeSpan.FromTicks(frame.Ticks * CooldownBars);
	}

	private void ResetState()
	{
		_previousEma = null;
		_previousCci = null;
		_previousPreviousCci = null;
		_previousMacd = null;
		_previousPreviousMacd = null;
		_previousSignal = null;
		_previousPreviousSignal = null;

		_recentLows.Clear();
		_recentHighs.Clear();

		ResetPositionState();

		_nextEntryTime = null;
	}

	private void ResetPositionState()
	{
		ResetLongState();
		ResetShortState();
		_entryVolume = null;
		_trailingMoves = 0;
	}

	private void ResetLongState()
	{
		_longStopLoss = null;
		_longTakeProfit = null;
	}

	private void ResetShortState()
	{
		_shortStopLoss = null;
		_shortTakeProfit = null;
	}

	private static bool TryGetTimeFrame(DataType type, out TimeSpan frame)
	{
		if (type.MessageType == typeof(TimeFrameCandleMessage) && type.Arg is TimeSpan span)
		{
			frame = span;
			return true;
		}

		frame = default;
		return false;
	}
}
