using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that mirrors the Parabolic SAR trailing stop expert advisor.
/// It does not generate entries and instead manages open positions by
/// tightening stop levels based on the Parabolic SAR indicator.
/// </summary>
public class ParabolicTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaxStep;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _longStopLevel;
	private decimal? _shortStopLevel;
	private DateTimeOffset? _longEntryTime;
	private DateTimeOffset? _shortEntryTime;
	private decimal _previousSarValue;
	private DateTimeOffset _previousSarTime;
	private decimal _previousHigh;
	private decimal _previousLow;
	private decimal _previousPosition;
	private bool _hasPreviousCandle;

	/// <summary>
	/// Acceleration step for Parabolic SAR.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal SarMaxStep
	{
		get => _sarMaxStep.Value;
		set => _sarMaxStep.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate the trailing logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ParabolicTrailingStopStrategy"/>.
	/// </summary>
	public ParabolicTrailingStopStrategy()
	{
		_sarStep = Param(nameof(SarStep), 0.1m)
			.SetDisplay("SAR Step", "Acceleration step for Parabolic SAR", "Parabolic SAR")
			.SetCanOptimize(true)
			.SetOptimize(0.02m, 0.2m, 0.02m);

		_sarMaxStep = Param(nameof(SarMaxStep), 0.11m)
			.SetDisplay("SAR Max", "Maximum acceleration factor for Parabolic SAR", "Parabolic SAR")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.5m, 0.05m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles processed by the trailing logic", "General");
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

		_longStopLevel = null;
		_shortStopLevel = null;
		_longEntryTime = null;
		_shortEntryTime = null;
		_previousSarValue = 0m;
		_previousSarTime = default;
		_previousHigh = 0m;
		_previousLow = 0m;
		_previousPosition = 0m;
		_hasPreviousCandle = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var parabolicSar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationStep = SarStep,
			AccelerationMax = SarMaxStep
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(parabolicSar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var position = Position;

		if (position > 0m && _previousPosition <= 0m)
		{
			_longEntryTime = candle.CloseTime;
			_longStopLevel = null;
			_shortEntryTime = null;
			_shortStopLevel = null;
		}
		else if (position < 0m && _previousPosition >= 0m)
		{
			_shortEntryTime = candle.CloseTime;
			_shortStopLevel = null;
			_longEntryTime = null;
			_longStopLevel = null;
		}
		else if (position == 0m)
		{
			if (_previousPosition > 0m)
			{
				_longEntryTime = null;
				_longStopLevel = null;
			}

			if (_previousPosition < 0m)
			{
				_shortEntryTime = null;
				_shortStopLevel = null;
			}
		}

		if (_hasPreviousCandle)
		{
			if (position > 0m && _longEntryTime != null && _previousSarTime > _longEntryTime.Value)
			{
				var entryPrice = PositionPrice;

				if (_previousSarValue > entryPrice && _previousSarValue < _previousLow)
				{
					if (_longStopLevel == null || _previousSarValue > _longStopLevel.Value)
					{
						_longStopLevel = _previousSarValue;
						LogInfo($"Updated long stop to {_longStopLevel} using Parabolic SAR");
					}
				}
			}
			else if (position < 0m && _shortEntryTime != null && _previousSarTime > _shortEntryTime.Value)
			{
				var entryPrice = PositionPrice;

				if (_previousSarValue < entryPrice && _previousSarValue > _previousHigh)
				{
					if (_shortStopLevel == null || _previousSarValue < _shortStopLevel.Value)
					{
						_shortStopLevel = _previousSarValue;
						LogInfo($"Updated short stop to {_shortStopLevel} using Parabolic SAR");
					}
				}
			}
		}

		var canTrade = IsFormedAndOnlineAndAllowTrading();

		if (position > 0m && _longStopLevel != null && canTrade)
		{
			if (candle.LowPrice <= _longStopLevel.Value)
			{
				CancelActiveOrders();
				SellMarket(position);
				LogInfo($"Long trailing stop hit at {_longStopLevel}. Candle low {candle.LowPrice}");
				_longStopLevel = null;
				_longEntryTime = null;
			}
		}
		else if (position < 0m && _shortStopLevel != null && canTrade)
		{
			if (candle.HighPrice >= _shortStopLevel.Value)
			{
				CancelActiveOrders();
				BuyMarket(Math.Abs(position));
				LogInfo($"Short trailing stop hit at {_shortStopLevel}. Candle high {candle.HighPrice}");
				_shortStopLevel = null;
				_shortEntryTime = null;
			}
		}

		_previousSarValue = sarValue;
		_previousSarTime = candle.CloseTime;
		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
		_previousPosition = position;
		_hasPreviousCandle = true;
	}
}
