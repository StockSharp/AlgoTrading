using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI based strategy that mirrors the logic of the original "RSI Eraser" Expert Advisor.
/// It trades on hourly candles, checks the previous daily range, and uses fixed risk sizing.
/// </summary>
public class RsiEraserStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiNeutralLevel;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	private readonly StrategyParam<decimal> _dailyBufferPips;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _dailyCandleType;

	private RelativeStrengthIndex _rsi;

	private decimal? _previousDailyLow;
	private decimal? _previousDailyHigh;
	private DateTime? _lastBuyDate;
	private DateTime? _lastSellDate;

	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _stopDistance;
	private bool _isBreakEvenActivated;
	private decimal _pipSize;

	/// <summary>
	/// RSI averaging period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Neutral level for RSI comparisons.
	/// </summary>
	public decimal RsiNeutralLevel
	{
		get => _rsiNeutralLevel.Value;
		set => _rsiNeutralLevel.Value = value;
	}

	/// <summary>
	/// Stop-loss size in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Risk per trade in percent of equity.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Take-profit multiplier relative to stop size.
	/// </summary>
	public decimal TakeProfitMultiplier
	{
		get => _takeProfitMultiplier.Value;
		set => _takeProfitMultiplier.Value = value;
	}

	/// <summary>
	/// Additional buffer applied to yesterday's high/low in pips.
	/// </summary>
	public decimal DailyBufferPips
	{
		get => _dailyBufferPips.Value;
		set => _dailyBufferPips.Value = value;
	}

	/// <summary>
	/// Working candle type (hourly by default).
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Daily candle type used to capture previous highs and lows.
	/// </summary>
	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="RsiEraserStrategy"/>.
	/// </summary>
	public RsiEraserStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Number of periods used for RSI calculation", "Indicators");

		_rsiNeutralLevel = Param(nameof(RsiNeutralLevel), 50m)
		.SetRange(0m, 100m)
		.SetDisplay("RSI Neutral", "Neutral level used to detect direction", "Indicators");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetRange(1m, 500m)
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance expressed in pips", "Risk Management");

		_riskPercent = Param(nameof(RiskPercent), 5m)
		.SetRange(0.1m, 50m)
		.SetDisplay("Risk %", "Risk percentage applied to equity for sizing", "Risk Management");

		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 3m)
		.SetGreaterThanZero()
		.SetDisplay("TP Multiplier", "Take-profit multiple of stop distance", "Risk Management");

		_dailyBufferPips = Param(nameof(DailyBufferPips), 10m)
		.SetRange(0m, 100m)
		.SetDisplay("Daily Buffer (pips)", "Extra pips added to yesterday's range", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe for signals", "General");

		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Daily Candle", "Timeframe used to read yesterday's range", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
		yield break;

		yield return (Security, CandleType);

		if (DailyCandleType != CandleType)
		yield return (Security, DailyCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsi = null;
		_previousDailyLow = null;
		_previousDailyHigh = null;
		_lastBuyDate = null;
		_lastSellDate = null;
		_stopPrice = null;
		_takePrice = null;
		_stopDistance = 0m;
		_isBreakEvenActivated = false;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
		.Bind(_rsi, ProcessMainCandle)
		.Start();

		var dailySubscription = SubscribeCandles(DailyCandleType);
		dailySubscription
		.Bind(ProcessDailyCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSubscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Store the most recent completed daily range for entry validation.
		_previousDailyHigh = candle.HighPrice;
		_previousDailyLow = candle.LowPrice;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_rsi == null || !_rsi.IsFormed)
		return;

		// Manage any open position before looking for new entries.
		if (HandleOpenPosition(candle))
		return;

		var signal = GetSignal(rsiValue);
		if (signal == 0)
		return;

		if (signal > 0)
		TryEnterLong(candle, rsiValue);
		else
		TryEnterShort(candle, rsiValue);
	}

	private bool HandleOpenPosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			InitializeRiskLevelsIfNeeded(isLong: true, candle.ClosePrice);

			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Exit long via stop at {_stopPrice:0.#####}.");
				ResetRiskLevels();
				return true;
			}

			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Exit long via take-profit at {_takePrice:0.#####}.");
				ResetRiskLevels();
				return true;
			}

			if (!_isBreakEvenActivated && _stopDistance > 0m)
			{
				var entryPrice = PositionPrice;
				if (candle.ClosePrice - entryPrice >= _stopDistance)
				{
					_stopPrice = entryPrice;
					_isBreakEvenActivated = true;
					LogInfo($"Moved long stop to break-even at {entryPrice:0.#####}.");
				}
			}
		}
		else if (Position < 0)
		{
			InitializeRiskLevelsIfNeeded(isLong: false, candle.ClosePrice);

			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short via stop at {_stopPrice:0.#####}.");
				ResetRiskLevels();
				return true;
			}

			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short via take-profit at {_takePrice:0.#####}.");
				ResetRiskLevels();
				return true;
			}

			if (!_isBreakEvenActivated && _stopDistance > 0m)
			{
				var entryPrice = PositionPrice;
				if (entryPrice - candle.ClosePrice >= _stopDistance)
				{
					_stopPrice = entryPrice;
					_isBreakEvenActivated = true;
					LogInfo($"Moved short stop to break-even at {entryPrice:0.#####}.");
				}
			}
		}
		else if (_stopPrice.HasValue || _takePrice.HasValue)
		{
			// No open position: clear any residual risk levels.
			ResetRiskLevels();
		}

		return false;
	}

	private void InitializeRiskLevelsIfNeeded(bool isLong, decimal referencePrice)
	{
		if (_stopPrice.HasValue && _takePrice.HasValue)
		return;

		var entryPrice = PositionPrice;
		if (entryPrice == 0m)
		entryPrice = referencePrice;

		var stopDistance = GetStopDistance();
		if (stopDistance <= 0m)
		return;

		_stopDistance = stopDistance;

		if (isLong)
		{
			_stopPrice = entryPrice - stopDistance;
			_takePrice = entryPrice + stopDistance * TakeProfitMultiplier;
		}
		else
		{
			_stopPrice = entryPrice + stopDistance;
			_takePrice = entryPrice - stopDistance * TakeProfitMultiplier;
		}

		_isBreakEvenActivated = false;
	}

	private void TryEnterLong(ICandleMessage candle, decimal rsiValue)
	{
		if (_previousDailyLow == null)
		return;

		if (Position > 0)
		return;

		var today = candle.OpenTime.Date;
		if (_lastBuyDate.HasValue && _lastBuyDate.Value >= today)
		return;

		var stopDistance = GetStopDistance();
		if (stopDistance <= 0m)
		return;

		var entryPrice = candle.ClosePrice;
		var stopPrice = entryPrice - stopDistance;

		var buffer = DailyBufferPips * _pipSize;
		var adjustedLow = _previousDailyLow.Value - buffer;
		if (adjustedLow < stopPrice)
		return;

		var volume = CalculatePositionSize(entryPrice, stopPrice);
		if (volume <= 0m)
		return;

		ResetRiskLevels();

		var tradeVolume = volume + Math.Abs(Position);
		BuyMarket(tradeVolume);

		_lastBuyDate = today;
		_stopPrice = stopPrice;
		_takePrice = entryPrice + stopDistance * TakeProfitMultiplier;
		_stopDistance = stopDistance;
		_isBreakEvenActivated = false;

		LogInfo($"Buy signal: RSI {rsiValue:F2} > {RsiNeutralLevel:F2}, entry {entryPrice:0.#####}, stop {_stopPrice:0.#####}, take {_takePrice:0.#####}. Volume {tradeVolume:0.#####}.");
	}

	private void TryEnterShort(ICandleMessage candle, decimal rsiValue)
	{
		if (_previousDailyHigh == null)
		return;

		if (Position < 0)
		return;

		var today = candle.OpenTime.Date;
		if (_lastSellDate.HasValue && _lastSellDate.Value >= today)
		return;

		var stopDistance = GetStopDistance();
		if (stopDistance <= 0m)
		return;

		var entryPrice = candle.ClosePrice;
		var stopPrice = entryPrice + stopDistance;

		var buffer = DailyBufferPips * _pipSize;
		var adjustedHigh = _previousDailyHigh.Value + buffer;
		if (adjustedHigh > stopPrice)
		return;

		var volume = CalculatePositionSize(entryPrice, stopPrice);
		if (volume <= 0m)
		return;

		ResetRiskLevels();

		var tradeVolume = volume + Math.Abs(Position);
		SellMarket(tradeVolume);

		_lastSellDate = today;
		_stopPrice = stopPrice;
		_takePrice = entryPrice - stopDistance * TakeProfitMultiplier;
		_stopDistance = stopDistance;
		_isBreakEvenActivated = false;

		LogInfo($"Sell signal: RSI {rsiValue:F2} < {RsiNeutralLevel:F2}, entry {entryPrice:0.#####}, stop {_stopPrice:0.#####}, take {_takePrice:0.#####}. Volume {tradeVolume:0.#####}.");
	}

	private int GetSignal(decimal rsiValue)
	{
		if (rsiValue == 0m)
		return 0;

		return rsiValue > RsiNeutralLevel ? 1 : -1;
	}

	private decimal CalculatePositionSize(decimal entryPrice, decimal stopPrice)
	{
		var stopDistance = Math.Abs(entryPrice - stopPrice);
		if (stopDistance <= 0m)
		return Volume;

		var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (equity <= 0m)
		return Volume;

		var riskAmount = equity * (RiskPercent / 100m);
		if (riskAmount <= 0m)
		return Volume;

		var volume = riskAmount / stopDistance;
		return volume > 0m ? volume : Volume;
	}

	private decimal GetStopDistance()
	{
		if (_pipSize <= 0m)
		_pipSize = CalculatePipSize();

		return StopLossPips * _pipSize;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;
		var digits = GetDecimalDigits(step);

		return digits is 3 or 5
		? step * 10m
		: step;
	}

	private static int GetDecimalDigits(decimal value)
	{
		var digits = 0;
		var normalized = value;

		while (normalized != Math.Truncate(normalized) && digits < 10)
		{
			normalized *= 10m;
			digits++;
		}

		return digits;
	}

	private void ResetRiskLevels()
	{
		_stopPrice = null;
		_takePrice = null;
		_stopDistance = 0m;
		_isBreakEvenActivated = false;
	}
}
