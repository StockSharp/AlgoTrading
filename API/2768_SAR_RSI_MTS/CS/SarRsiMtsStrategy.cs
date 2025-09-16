using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR and RSI strategy translated from the original MQL implementation.
/// </summary>
public class SarRsiMtsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiNeutralLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _maxPosition;

	private decimal? _previousSar;
	private decimal? _previousRsi;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private decimal _pipSize;

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step distance expressed in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Parabolic SAR acceleration step.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// RSI lookback period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI neutral level used for bullish or bearish confirmation.
	/// </summary>
	public decimal RsiNeutralLevel
	{
		get => _rsiNeutralLevel.Value;
		set => _rsiNeutralLevel.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum absolute net position allowed by the strategy.
	/// </summary>
	public decimal MaxPosition
	{
		get => _maxPosition.Value;
		set => _maxPosition.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SarRsiMtsStrategy"/> class.
	/// </summary>
	public SarRsiMtsStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 10m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 40m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 15m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Trailing step distance in pips", "Risk");

		_sarStep = Param(nameof(SarStep), 0.05m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Parabolic SAR acceleration step", "Indicators");

		_sarMax = Param(nameof(SarMax), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Maximum", "Parabolic SAR maximum acceleration", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Lookback period for RSI", "Indicators");

		_rsiNeutralLevel = Param(nameof(RsiNeutralLevel), 50m)
			.SetDisplay("RSI Neutral", "Neutral RSI threshold separating bullish and bearish bias", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for indicator calculations", "General");

		_maxPosition = Param(nameof(MaxPosition), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Max Position", "Maximum absolute net position allowed", "Risk");
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

		_pipSize = CalculatePipSize();

		var parabolicSar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationStep = SarStep,
			AccelerationMax = SarMax
		};

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(parabolicSar, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (ManageRisk(candle))
			return;

		if (sarValue == 0m || rsiValue == 0m)
			return;

		if (!_previousSar.HasValue || !_previousRsi.HasValue)
		{
			_previousSar = sarValue;
			_previousRsi = rsiValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousSar = sarValue;
			_previousRsi = rsiValue;
			return;
		}

		var sarPrev = _previousSar.Value;
		var rsiPrev = _previousRsi.Value;

		var price = candle.ClosePrice;
		var buySignal = sarPrev < price
			&& !AreClose(sarPrev, price)
			&& sarValue > sarPrev
			&& rsiValue > RsiNeutralLevel
			&& rsiValue > rsiPrev
			&& !AreClose(rsiValue, rsiPrev);

		if (buySignal)
		{
			EnterLong(candle);
		}
		else
		{
			var sellSignal = sarPrev > price
				&& !AreClose(sarPrev, price)
				&& sarValue < sarPrev
				&& rsiValue < RsiNeutralLevel
				&& rsiValue < rsiPrev
				&& !AreClose(rsiValue, rsiPrev);

			if (sellSignal)
				EnterShort(candle);
		}

		_previousSar = sarValue;
		_previousRsi = rsiValue;
	}

	private void EnterLong(ICandleMessage candle)
	{
		var tradeVolume = Volume;
		if (tradeVolume <= 0m)
			return;

		var maxPosition = MaxPosition;
		if (maxPosition <= 0m)
			return;

		var current = Position;
		var target = current < 0 ? Math.Min(maxPosition, tradeVolume) : Math.Min(maxPosition, current + tradeVolume);
		var required = target - current;
		if (required <= 0m)
			return;

		BuyMarket(required);
		_longTrailingStop = null;
		_shortTrailingStop = null;
		LogInfo($"Buy signal at {candle.ClosePrice}, SAR {_previousSar}, RSI {_previousRsi}");
	}

	private void EnterShort(ICandleMessage candle)
	{
		var tradeVolume = Volume;
		if (tradeVolume <= 0m)
			return;

		var maxPosition = MaxPosition;
		if (maxPosition <= 0m)
			return;

		var current = Position;
		var target = current > 0 ? -Math.Min(maxPosition, tradeVolume) : Math.Max(-maxPosition, current - tradeVolume);
		var required = current - target;
		if (required <= 0m)
			return;

		SellMarket(required);
		_longTrailingStop = null;
		_shortTrailingStop = null;
		LogInfo($"Sell signal at {candle.ClosePrice}, SAR {_previousSar}, RSI {_previousRsi}");
	}

	private bool ManageRisk(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			var entryPrice = Position.AveragePrice;
			if (entryPrice <= 0m)
				return false;

			var trailingTriggered = UpdateLongTrailing(candle, entryPrice);
			if (trailingTriggered)
				return true;

			var stopDistance = GetPriceOffset(StopLossPips);
			if (stopDistance > 0m)
			{
				var stopPrice = entryPrice - stopDistance;
				if (candle.LowPrice <= stopPrice)
				{
					SellMarket(Position);
					ResetTrailing();
					return true;
				}
			}

			var takeDistance = GetPriceOffset(TakeProfitPips);
			if (takeDistance > 0m)
			{
				var takePrice = entryPrice + takeDistance;
				if (candle.HighPrice >= takePrice)
				{
					SellMarket(Position);
					ResetTrailing();
					return true;
				}
			}
		}
		else if (Position < 0m)
		{
			var entryPrice = Position.AveragePrice;
			if (entryPrice <= 0m)
				return false;

			var trailingTriggered = UpdateShortTrailing(candle, entryPrice);
			if (trailingTriggered)
				return true;

			var stopDistance = GetPriceOffset(StopLossPips);
			if (stopDistance > 0m)
			{
				var stopPrice = entryPrice + stopDistance;
				if (candle.HighPrice >= stopPrice)
				{
					BuyMarket(Math.Abs(Position));
					ResetTrailing();
					return true;
				}
			}

			var takeDistance = GetPriceOffset(TakeProfitPips);
			if (takeDistance > 0m)
			{
				var takePrice = entryPrice - takeDistance;
				if (candle.LowPrice <= takePrice)
				{
					BuyMarket(Math.Abs(Position));
					ResetTrailing();
					return true;
				}
			}
		}
		else
		{
			ResetTrailing();
		}

		return false;
	}

	private bool UpdateLongTrailing(ICandleMessage candle, decimal entryPrice)
	{
		var trailingDistance = GetPriceOffset(TrailingStopPips);
		if (trailingDistance <= 0m)
		{
			_longTrailingStop = null;
			return false;
		}

		var trailingStep = GetPriceOffset(TrailingStepPips);
		var profit = candle.ClosePrice - entryPrice;
		if (profit >= trailingDistance + trailingStep)
		{
			var candidate = candle.ClosePrice - trailingDistance;
			var threshold = candle.ClosePrice - (trailingDistance + trailingStep);
			if (!_longTrailingStop.HasValue || _longTrailingStop.Value < threshold)
				_longTrailingStop = candidate;
		}

		if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
		{
			SellMarket(Position);
			ResetTrailing();
			return true;
		}

		return false;
	}

	private bool UpdateShortTrailing(ICandleMessage candle, decimal entryPrice)
	{
		var trailingDistance = GetPriceOffset(TrailingStopPips);
		if (trailingDistance <= 0m)
		{
			_shortTrailingStop = null;
			return false;
		}

		var trailingStep = GetPriceOffset(TrailingStepPips);
		var profit = entryPrice - candle.ClosePrice;
		if (profit >= trailingDistance + trailingStep)
		{
			var candidate = candle.ClosePrice + trailingDistance;
			var threshold = candle.ClosePrice + (trailingDistance + trailingStep);
			if (!_shortTrailingStop.HasValue || _shortTrailingStop.Value > threshold)
				_shortTrailingStop = candidate;
		}

		if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
		{
			BuyMarket(Math.Abs(Position));
			ResetTrailing();
			return true;
		}

		return false;
	}

	private decimal GetPriceOffset(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		var pip = _pipSize;
		if (pip <= 0m)
			pip = Security?.PriceStep ?? 1m;

		return pip * pips;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 1m;

		var decimals = Security?.Decimals;
		var adjust = decimals == 3 || decimals == 5 ? 10m : 1m;

		return priceStep * adjust;
	}

	private void ResetTrailing()
	{
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	private bool AreClose(decimal value1, decimal value2)
	{
		var decimals = Security?.Decimals ?? 4;
		return Math.Round(value1 - value2, decimals) == 0m;
	}
}
