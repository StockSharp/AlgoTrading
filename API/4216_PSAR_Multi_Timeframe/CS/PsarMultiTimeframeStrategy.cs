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
/// Multi-timeframe Parabolic SAR strategy converted from the MetaTrader expert EA_PSar_002B.
/// </summary>
public class PsarMultiTimeframeStrategy : Strategy
{
			
	private readonly StrategyParam<DataType> _baseCandleType;
	private readonly StrategyParam<DataType> _fastSarCandleType;
	private readonly StrategyParam<DataType> _mediumSarCandleType;
	private readonly StrategyParam<DataType> _slowSarCandleType;
	private readonly StrategyParam<bool> _enableParabolicFilter;
	private readonly StrategyParam<decimal> _sarAcceleration;
	private readonly StrategyParam<decimal> _sarAccelerationMax;
	private readonly StrategyParam<decimal> _maximumSarSpreadPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _percentMoneyManagement;
	private readonly StrategyParam<decimal> _fixedVolume;

	private ParabolicSar _fastSar;
	private ParabolicSar _mediumSar;
	private ParabolicSar _slowSar;

	private decimal? _fastSarCurrent;
	private decimal? _fastSarPrevious;
	private decimal? _mediumSarCurrent;
	private decimal? _mediumSarPrevious;
	private decimal? _slowSarCurrent;
	private decimal? _slowSarPrevious;

	private ICandleMessage _fastCurrentCandle;
	private ICandleMessage _fastPreviousCandle;
	private ICandleMessage _mediumCurrentCandle;
	private ICandleMessage _mediumPreviousCandle;
	private ICandleMessage _slowCurrentCandle;
	private ICandleMessage _slowPreviousCandle;
	private	ICandleMessage _previousBaseCandle;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of <see cref="PsarMultiTimeframeStrategy"/>.
	/// </summary>
	public PsarMultiTimeframeStrategy()
	{
		_baseCandleType = Param(nameof(BaseCandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Base Timeframe", "Primary timeframe used for trade management", "General");

		_fastSarCandleType = Param(nameof(FastSarCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Fast SAR Timeframe", "Timeframe used for the fastest Parabolic SAR", "General");

		_mediumSarCandleType = Param(nameof(MediumSarCandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Medium SAR Timeframe", "Timeframe used for the middle Parabolic SAR", "General");

		_slowSarCandleType = Param(nameof(SlowSarCandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Slow SAR Timeframe", "Timeframe used for the slow Parabolic SAR", "General");

		_enableParabolicFilter = Param(nameof(EnableParabolicFilter), true)
		.SetDisplay("Enable Multi-timeframe Filter", "Disables trading when set to false", "Signals");

		_sarAcceleration = Param(nameof(SarAcceleration), 0.06m)
			.SetRange(0m, 1m)
			.SetDisplay("SAR Acceleration", "Initial acceleration value for Parabolic SAR", "Indicators");

		_sarAccelerationMax = Param(nameof(SarAccelerationMax), 0.1m)
			.SetRange(0m, 1m)
			.SetDisplay("SAR Acceleration Max", "Maximum acceleration allowed for Parabolic SAR", "Indicators");

		_maximumSarSpreadPoints = Param(nameof(MaximumSarSpreadPoints), 19m)
			.SetRange(0m, 1000m)
			.SetDisplay("Max SAR Spread (points)", "Maximum difference between SAR values to accept a trade", "Indicators");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 999m)
		.SetDisplay("Take Profit (points)", "Distance to the target expressed in minimum price increments", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 399m)
		.SetDisplay("Stop Loss (points)", "Distance to the protective stop expressed in minimum price increments", "Risk");

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
		.SetDisplay("Enable Money Management", "Replaces fixed volume with percentage based sizing", "Money Management");

		_percentMoneyManagement = Param(nameof(PercentMoneyManagement), 10m)
		.SetDisplay("Risk Percent", "Percentage of free capital used to estimate the order volume", "Money Management");

		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
		.SetDisplay("Fixed Volume", "Volume submitted when money management is disabled", "Trading");
	}

	/// <summary>
	/// Base timeframe used for trade management.
	/// </summary>
	public DataType BaseCandleType
	{
		get => _baseCandleType.Value;
		set => _baseCandleType.Value = value;
	}

	/// <summary>
	/// Fast Parabolic SAR timeframe.
	/// </summary>
	public DataType FastSarCandleType
	{
		get => _fastSarCandleType.Value;
		set => _fastSarCandleType.Value = value;
	}

	/// <summary>
	/// Medium Parabolic SAR timeframe.
	/// </summary>
	public DataType MediumSarCandleType
	{
		get => _mediumSarCandleType.Value;
		set => _mediumSarCandleType.Value = value;
	}

	/// <summary>
	/// Slow Parabolic SAR timeframe.
	/// </summary>
	public DataType SlowSarCandleType
	{
		get => _slowSarCandleType.Value;
		set => _slowSarCandleType.Value = value;
	}

	/// <summary>
	/// Enables the multi-timeframe Parabolic SAR filter.
	/// </summary>
	public bool EnableParabolicFilter
	{
		get => _enableParabolicFilter.Value;
		set => _enableParabolicFilter.Value = value;
	}

	/// <summary>
	/// Initial acceleration value applied to Parabolic SAR calculations.
	/// </summary>
	public decimal SarAcceleration
	{
		get => _sarAcceleration.Value;
		set => _sarAcceleration.Value = value;
	}

	/// <summary>
	/// Maximum acceleration value permitted for Parabolic SAR calculations.
	/// </summary>
	public decimal SarAccelerationMax
	{
		get => _sarAccelerationMax.Value;
		set => _sarAccelerationMax.Value = value;
	}

	/// <summary>
	/// Maximum spread between Parabolic SAR values allowed before skipping signals.
	/// </summary>
	public decimal MaximumSarSpreadPoints
	{
		get => _maximumSarSpreadPoints.Value;
		set => _maximumSarSpreadPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in minimum price increments.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in minimum price increments.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Enables percentage based position sizing.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Percentage of available capital used to calculate the trade volume.
	/// </summary>
	public decimal PercentMoneyManagement
	{
		get => _percentMoneyManagement.Value;
		set => _percentMoneyManagement.Value = value;
	}

	/// <summary>
	/// Fixed order volume used when money management is disabled.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastSar = null;
		_mediumSar = null;
		_slowSar = null;

		_fastSarCurrent = null;
		_fastSarPrevious = null;
		_mediumSarCurrent = null;
		_mediumSarPrevious = null;
		_slowSarCurrent = null;
		_slowSarPrevious = null;

		_fastCurrentCandle = null;
		_fastPreviousCandle = null;
		_mediumCurrentCandle = null;
		_mediumPreviousCandle = null;
		_slowCurrentCandle = null;
		_slowPreviousCandle = null;
		_previousBaseCandle = null;

		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_pipSize = CalculatePipSize();
		Volume = FixedVolume;

		_fastSar = CreateParabolicSar();
		_mediumSar = CreateParabolicSar();
		_slowSar = CreateParabolicSar();

		var baseSubscription = SubscribeCandles(BaseCandleType);
		baseSubscription
		.Bind(ProcessBaseCandle)
		.Start();

		var fastSubscription = SubscribeCandles(FastSarCandleType);
		fastSubscription
		.Bind(_fastSar, ProcessFastSar)
		.Start();

		var mediumSubscription = SubscribeCandles(MediumSarCandleType);
		mediumSubscription
		.Bind(_mediumSar, ProcessMediumSar)
		.Start();

		var slowSubscription = SubscribeCandles(SlowSarCandleType);
		slowSubscription
		.Bind(_slowSar, ProcessSlowSar)
		.Start();
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			_entryPrice = null;
			_stopPrice = null;
			_takeProfitPrice = null;
			return;
		}

		var averagePrice = Position.AveragePrice ?? 0m;
		if (averagePrice <= 0m || _pipSize <= 0m)
		return;

		_entryPrice = averagePrice;

		if (Position > 0m)
		{
			_stopPrice = StopLossPoints > 0m ? averagePrice - StopLossPoints * _pipSize : null;
			_takeProfitPrice = TakeProfitPoints > 0m ? averagePrice + TakeProfitPoints * _pipSize : null;
		}
		else if (Position < 0m)
		{
			_stopPrice = StopLossPoints > 0m ? averagePrice + StopLossPoints * _pipSize : null;
			_takeProfitPrice = TakeProfitPoints > 0m ? averagePrice - TakeProfitPoints * _pipSize : null;
		}
	}

	private void ProcessBaseCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_pipSize <= 0m)
		{
			var recalculated = CalculatePipSize();
			if (recalculated > 0m)
				_pipSize = recalculated;
		}

		var previousCandle = _previousBaseCandle;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			ManageOpenPosition(candle, previousCandle);

			if (Position == 0m && !HasActiveOrders())
				TryOpenPosition(candle);
		}

		_previousBaseCandle = candle;
	}

	private void ProcessFastSar(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateIndicatorState(ref _fastPreviousCandle, ref _fastCurrentCandle, ref _fastSarPrevious, ref _fastSarCurrent, candle, sarValue);
	}

	private void ProcessMediumSar(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateIndicatorState(ref _mediumPreviousCandle, ref _mediumCurrentCandle, ref _mediumSarPrevious, ref _mediumSarCurrent, candle, sarValue);
	}

	private void ProcessSlowSar(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateIndicatorState(ref _slowPreviousCandle, ref _slowCurrentCandle, ref _slowSarPrevious, ref _slowSarCurrent, candle, sarValue);
	}

	private void UpdateIndicatorState(ref ICandleMessage previousCandle, ref ICandleMessage currentCandle, ref decimal? previousSar, ref decimal? currentSar, ICandleMessage candle, decimal sarValue)
	{
		previousCandle = currentCandle;
		currentCandle = candle;
		previousSar = currentSar;
		currentSar = sarValue;
	}

	private void TryOpenPosition(ICandleMessage candle)
	{
		if (!EnableParabolicFilter)
		return;

		if (!_fastSarCurrent.HasValue || !_mediumSarCurrent.HasValue || !_slowSarCurrent.HasValue)
		return;

		if (_fastCurrentCandle == null || _mediumCurrentCandle == null || _slowCurrentCandle == null)
		return;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return;

		var sarMax = Math.Max(Math.Max(_fastSarCurrent.Value, _mediumSarCurrent.Value), _slowSarCurrent.Value);
		var sarMin = Math.Min(Math.Min(_fastSarCurrent.Value, _mediumSarCurrent.Value), _slowSarCurrent.Value);
		var sarSpread = (sarMax - sarMin) / priceStep;

		if (sarSpread > MaximumSarSpreadPoints)
		return;

		var buySignal = IsBuySignal();
		var sellSignal = IsSellSignal();

		if (!buySignal && !sellSignal)
		return;

		var volume = CalculateVolume();
		if (volume <= 0m)
		return;

		if (buySignal)
		{
			BuyMarket(volume);
		}
		else if (sellSignal)
		{
			SellMarket(volume);
		}
	}

	private bool IsBuySignal()
	{
		if (!_fastSarCurrent.HasValue || !_mediumSarCurrent.HasValue || !_slowSarCurrent.HasValue)
		return false;

		var fastCandle = _fastCurrentCandle;
		var mediumCandle = _mediumCurrentCandle;
		var slowCandle = _slowCurrentCandle;

		if (fastCandle == null || mediumCandle == null || slowCandle == null)
		return false;

		var condition1 = false;
		if (_slowSarPrevious.HasValue && _slowPreviousCandle != null)
		{
			condition1 = _fastSarCurrent.Value < fastCandle.LowPrice &&
			_mediumSarCurrent.Value < mediumCandle.LowPrice &&
			_slowSarPrevious.Value > _slowPreviousCandle.HighPrice &&
			_slowSarCurrent.Value < slowCandle.LowPrice;
		}

		var condition2 = false;
		if (_mediumSarPrevious.HasValue && _mediumPreviousCandle != null)
		{
			condition2 = _fastSarCurrent.Value < fastCandle.LowPrice &&
			_slowSarCurrent.Value < slowCandle.LowPrice &&
			_mediumSarPrevious.Value > _mediumPreviousCandle.HighPrice &&
			_mediumSarCurrent.Value < mediumCandle.LowPrice;
		}

		var condition3 = false;
		if (_fastSarPrevious.HasValue && _fastPreviousCandle != null)
		{
			condition3 = _mediumSarCurrent.Value < mediumCandle.LowPrice &&
			_slowSarCurrent.Value < slowCandle.LowPrice &&
			_fastSarPrevious.Value > _fastPreviousCandle.HighPrice &&
			_fastSarCurrent.Value < fastCandle.LowPrice;
		}

		return condition1 || condition2 || condition3;
	}

	private bool IsSellSignal()
	{
		if (!_fastSarCurrent.HasValue || !_mediumSarCurrent.HasValue || !_slowSarCurrent.HasValue)
		return false;

		var fastCandle = _fastCurrentCandle;
		var mediumCandle = _mediumCurrentCandle;
		var slowCandle = _slowCurrentCandle;

		if (fastCandle == null || mediumCandle == null || slowCandle == null)
		return false;

		var condition1 = false;
		if (_slowSarPrevious.HasValue && _slowPreviousCandle != null)
		{
			condition1 = _fastSarCurrent.Value > fastCandle.HighPrice &&
			_mediumSarCurrent.Value > mediumCandle.HighPrice &&
			_slowSarPrevious.Value < _slowPreviousCandle.LowPrice &&
			_slowSarCurrent.Value > slowCandle.HighPrice;
		}

		var condition2 = false;
		if (_mediumSarPrevious.HasValue && _mediumPreviousCandle != null)
		{
			condition2 = _fastSarCurrent.Value > fastCandle.HighPrice &&
			_slowSarCurrent.Value > slowCandle.HighPrice &&
			_mediumSarPrevious.Value < _mediumPreviousCandle.LowPrice &&
			_mediumSarCurrent.Value > mediumCandle.HighPrice;
		}

		var condition3 = false;
		if (_fastSarPrevious.HasValue && _fastPreviousCandle != null)
		{
			condition3 = _mediumSarCurrent.Value > mediumCandle.HighPrice &&
			_slowSarCurrent.Value > slowCandle.HighPrice &&
			_fastSarPrevious.Value < _fastPreviousCandle.LowPrice &&
			_fastSarCurrent.Value > fastCandle.HighPrice;
		}

		return condition1 || condition2 || condition3;
	}

	private void ManageOpenPosition(ICandleMessage candle, ICandleMessage previousBaseCandle)
	{
		if (Position == 0m)
		return;

		if (HasActiveOrders())
		return;

		var positionVolume = Math.Abs(Position);
		if (positionVolume <= 0m)
		return;

		if (Position > 0m)
		{
			if (_takeProfitPrice is decimal takeProfit && candle.HighPrice >= takeProfit)
			{
				SellMarket(positionVolume);
				return;
			}

			if (_stopPrice is decimal stopLoss && candle.LowPrice <= stopLoss)
			{
				SellMarket(positionVolume);
				return;
			}

			if (_mediumSarPrevious.HasValue && _mediumSarCurrent.HasValue && previousBaseCandle != null)
			{
				if (_mediumSarPrevious.Value < previousBaseCandle.LowPrice && _mediumSarCurrent.Value > candle.HighPrice)
				{
					SellMarket(positionVolume);
					return;
				}
			}

			if (_mediumSarCurrent.HasValue && _entryPrice.HasValue && _mediumSarCurrent.Value >= _entryPrice.Value)
			{
				if (_stopPrice is decimal existingStop)
				{
					if (_mediumSarCurrent.Value > existingStop)
					_stopPrice = _mediumSarCurrent.Value;
				}
				else
				{
					_stopPrice = _mediumSarCurrent.Value;
				}
			}
		}
		else if (Position < 0m)
		{
			if (_takeProfitPrice is decimal takeProfit && candle.LowPrice <= takeProfit)
			{
				BuyMarket(positionVolume);
				return;
			}

			if (_stopPrice is decimal stopLoss && candle.HighPrice >= stopLoss)
			{
				BuyMarket(positionVolume);
				return;
			}

			if (_mediumSarPrevious.HasValue && _mediumSarCurrent.HasValue && previousBaseCandle != null)
			{
				if (_mediumSarPrevious.Value > previousBaseCandle.HighPrice && _mediumSarCurrent.Value < candle.LowPrice)
				{
					BuyMarket(positionVolume);
					return;
				}
			}

			if (_mediumSarCurrent.HasValue && _entryPrice.HasValue && _mediumSarCurrent.Value <= _entryPrice.Value)
			{
				if (_stopPrice is decimal existingStop)
				{
					if (_mediumSarCurrent.Value < existingStop)
					_stopPrice = _mediumSarCurrent.Value;
				}
				else
				{
					_stopPrice = _mediumSarCurrent.Value;
				}
			}
		}
	}

	private decimal CalculateVolume()
	{
		if (!UseMoneyManagement)
		return FixedVolume;

		var portfolio = Portfolio;
		if (portfolio == null)
		return FixedVolume;

		var freeCapital = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		if (freeCapital <= 0m)
		return FixedVolume;

		var volume = PercentMoneyManagement / 100m * freeCapital / 100000m;

		var volumeStep = Security?.VolumeStep ?? 0m;
		if (volumeStep > 0m)
		volume = Math.Floor(volume / volumeStep) * volumeStep;

		var minVolume = Security?.VolumeMin ?? (volumeStep > 0m ? volumeStep : FixedVolume);
		if (minVolume > 0m && volume < minVolume)
		volume = minVolume;

		var maxVolume = Security?.VolumeMax;
		if (maxVolume.HasValue && volume > maxVolume.Value)
		volume = maxVolume.Value;

		if (volume <= 0m)
		return FixedVolume;

		return volume;
	}

	private bool HasActiveOrders()
	{
		return Orders.Any(o => o.State.IsActive());
	}

	private ParabolicSar CreateParabolicSar()
	{
		return new ParabolicSar
		{
			Acceleration = SarAcceleration,
			AccelerationMax = SarAccelerationMax
		};
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return 1m;

		var decimals = Security?.Decimals ?? GetDecimalsFromStep(priceStep);
		var coefficient = decimals == 3 || decimals == 5 ? 10m : 1m;
		return priceStep * coefficient;
	}

	private static int GetDecimalsFromStep(decimal step)
	{
		if (step <= 0m)
		return 0;

		var decimals = 0;
		var value = step;

		while (value < 1m && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}
