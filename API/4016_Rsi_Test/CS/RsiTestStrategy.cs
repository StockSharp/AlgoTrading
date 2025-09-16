namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// RSI-based strategy with volume sizing and stair-like trailing stop.
/// </summary>
public class RsiTestStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _buyLevel;
	private readonly StrategyParam<decimal> _sellLevel;
	private readonly StrategyParam<decimal> _riskPercentage;
	private readonly StrategyParam<int> _trailingDistanceSteps;
	private readonly StrategyParam<int> _maxOpenPositions;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _baseVolume;

	private RelativeStrengthIndex _rsi;
	private decimal? _previousRsi;
	private decimal? _previousOpen;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private bool _trailingArmed;
	private decimal _priceStep;

	/// <summary>
	/// Initialize <see cref="RsiTestStrategy"/>.
	/// </summary>
	public RsiTestStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Lookback period for RSI", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 1);

		_buyLevel = Param(nameof(BuyLevel), 12m)
			.SetDisplay("RSI Buy Level", "Oversold threshold for long entries", "Trading");

		_sellLevel = Param(nameof(SellLevel), 88m)
			.SetDisplay("RSI Sell Level", "Overbought threshold for short entries", "Trading");

		_riskPercentage = Param(nameof(RiskPercentage), 10m)
			.SetDisplay("Risk Percentage", "Portfolio percentage used for sizing", "Risk");

		_trailingDistanceSteps = Param(nameof(TrailingDistanceSteps), 50)
			.SetDisplay("Trailing Distance Steps", "Steps before activating trailing stop", "Risk");

		_maxOpenPositions = Param(nameof(MaxOpenPositions), 1)
			.SetDisplay("Max Open Positions", "Maximum simultaneous positions. 0 disables the limit.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for calculations", "Data");

		_baseVolume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Fallback volume when risk sizing is unavailable", "Risk");
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal BuyLevel
	{
		get => _buyLevel.Value;
		set => _buyLevel.Value = value;
	}

	public decimal SellLevel
	{
		get => _sellLevel.Value;
		set => _sellLevel.Value = value;
	}

	public decimal RiskPercentage
	{
		get => _riskPercentage.Value;
		set => _riskPercentage.Value = value;
	}

	public int TrailingDistanceSteps
	{
		get => _trailingDistanceSteps.Value;
		set => _trailingDistanceSteps.Value = value;
	}

	public int MaxOpenPositions
	{
		get => _maxOpenPositions.Value;
		set => _maxOpenPositions.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal Volume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousRsi = null;
		_previousOpen = null;
		_entryPrice = null;
		_stopPrice = null;
		_trailingArmed = false;
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_priceStep = Security?.PriceStep ?? 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		// Only react to fully formed candles to match the MQL implementation.
		if (candle.State != CandleStates.Finished)
		return;

		// Manage trailing logic and exits before attempting fresh entries.
		ManagePosition(candle);

		if (!_rsi.IsFormed)
		{
			_previousRsi = rsiValue;
			_previousOpen = candle.OpenPrice;
			return;
		}

		if (_previousRsi is null || _previousOpen is null)
		{
			_previousRsi = rsiValue;
			_previousOpen = candle.OpenPrice;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousRsi = rsiValue;
			_previousOpen = candle.OpenPrice;
			return;
		}

		var rsiRising = rsiValue > _previousRsi.Value;
		var rsiFalling = rsiValue < _previousRsi.Value;
		var openHigher = candle.OpenPrice > _previousOpen.Value;
		var openLower = candle.OpenPrice < _previousOpen.Value;

		if (rsiValue < BuyLevel && rsiRising && openHigher && Position >= 0)
		{
			TryEnterLong(candle);
		}
		else if (rsiValue > SellLevel && rsiFalling && openLower && Position <= 0)
		{
			TryEnterShort(candle);
		}

		_previousRsi = rsiValue;
		_previousOpen = candle.OpenPrice;
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		var volume = CalculateOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
		return;

		if (!HasCapacityForNewPosition(volume))
		return;

		BuyMarket(volume);

		var avgPrice = PositionAvgPrice;
		_entryPrice = avgPrice > 0m ? avgPrice : candle.ClosePrice;
		_stopPrice = null;
		_trailingArmed = false;
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		var volume = CalculateOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
		return;

		if (!HasCapacityForNewPosition(volume))
		return;

		SellMarket(volume);

		var avgPrice = PositionAvgPrice;
		_entryPrice = avgPrice > 0m ? avgPrice : candle.ClosePrice;
		_stopPrice = null;
		_trailingArmed = false;
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position == 0)
		{
			ResetPositionState();
			return;
		}

		var avgPrice = PositionAvgPrice;
		if (avgPrice > 0m)
		_entryPrice = avgPrice;

		if (Position > 0)
		{
			UpdateTrailingForLong(candle);
			TryExitLong(candle);
		}
		else if (Position < 0)
		{
			UpdateTrailingForShort(candle);
			TryExitShort(candle);
		}
	}

	private void UpdateTrailingForLong(ICandleMessage candle)
	{
		if (TrailingDistanceSteps <= 0 || _entryPrice is null || _trailingArmed)
		return;

		var trailingDistance = GetPriceOffset(TrailingDistanceSteps);
		if (trailingDistance <= 0m)
		return;

		var activationPrice = _entryPrice.Value + trailingDistance;
		if (candle.HighPrice < activationPrice)
		return;

		_stopPrice = _entryPrice.Value + trailingDistance;
		_trailingArmed = true;
		LogInfo($"Activated long trailing stop at {_stopPrice:0.#####}.");
	}

	private void UpdateTrailingForShort(ICandleMessage candle)
	{
		if (TrailingDistanceSteps <= 0 || _entryPrice is null || _trailingArmed)
		return;

		var trailingDistance = GetPriceOffset(TrailingDistanceSteps);
		if (trailingDistance <= 0m)
		return;

		var activationPrice = _entryPrice.Value - trailingDistance;
		if (candle.LowPrice > activationPrice)
		return;

		_stopPrice = _entryPrice.Value - trailingDistance;
		_trailingArmed = true;
		LogInfo($"Activated short trailing stop at {_stopPrice:0.#####}.");
	}

	private void TryExitLong(ICandleMessage candle)
	{
		if (_stopPrice is null)
		return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		if (candle.LowPrice > _stopPrice.Value)
		return;

		SellMarket(volume);
		ResetPositionState();
	}

	private void TryExitShort(ICandleMessage candle)
	{
		if (_stopPrice is null)
		return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		if (candle.HighPrice < _stopPrice.Value)
		return;

		BuyMarket(volume);
		ResetPositionState();
	}

	private decimal CalculateOrderVolume(decimal referencePrice)
	{
		var volume = Volume;

		if (RiskPercentage > 0m)
		{
			var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
			var riskCapital = portfolioValue * RiskPercentage / 100m;

			if (riskCapital > 0m)
			{
				var margin = Security?.MarginBuy ?? Security?.MarginSell ?? 0m;

				if (margin > 0m)
				{
					volume = riskCapital / margin;
				}
				else if (referencePrice > 0m)
				{
					volume = riskCapital / referencePrice;
				}
			}
		}

		volume = RoundVolume(volume);

		var minVolume = Security?.MinVolume;
		if (minVolume != null && minVolume.Value > 0m && volume < minVolume.Value)
		{
			volume = minVolume.Value;
		}

		var maxVolume = Security?.MaxVolume;
		if (maxVolume != null && maxVolume.Value > 0m && volume > maxVolume.Value)
		{
			volume = maxVolume.Value;
		}

		return volume;
	}

	private decimal RoundVolume(decimal volume)
	{
		if (volume <= 0m)
		{
			return 0m;
		}

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			if (steps <= 0m)
			{
				return step;
			}

			return steps * step;
		}

		return Math.Round(volume, 2, MidpointRounding.ToZero);
	}

	private bool HasCapacityForNewPosition(decimal volume)
	{
		if (MaxOpenPositions <= 0)
		{
			return true;
		}

		if (volume <= 0m)
		{
			return false;
		}

		var exposure = Math.Abs(Position);
		var maxExposure = MaxOpenPositions * volume;

		return exposure + volume <= maxExposure + volume * 0.0001m;
	}

	private decimal GetPriceOffset(int steps)
	{
		if (steps <= 0)
		{
			return 0m;
		}

		if (_priceStep > 0m)
		{
			return steps * _priceStep;
		}

		return steps;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_trailingArmed = false;
	}
}
