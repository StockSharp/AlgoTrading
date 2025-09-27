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
/// Martingale trade simulator converted from the MetaTrader expert "Martingale Trade Simulator".
/// </summary>
public class MartingaleTradeSimulatorStrategy : Strategy
{
	private enum TradeDirection
	{
		None,
		Buy,
		Sell
	}

	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<decimal> _stepPips;
	private readonly StrategyParam<decimal> _takeProfitOffsetPips;
	private readonly StrategyParam<bool> _enableMartingale;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<TradeDirection> _initialDirection;
	private readonly StrategyParam<DataType> _candleType;

	private TradeDirection _currentDirection;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _currentVolume;
	private int _martingaleCount;
	private decimal _longNextAddPrice;
	private decimal _shortNextAddPrice;
	private bool _initialOrderSent;

	/// <summary>
	/// Initializes a new instance of <see cref="MartingaleTradeSimulatorStrategy"/>.
	/// </summary>
	public MartingaleTradeSimulatorStrategy()
	{
		_initialVolume = Param(nameof(InitialVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Base lot size for the first trade", "Trading")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 500m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance expressed in pips", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 500m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Take Profit (pips)", "Take-profit distance expressed in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 50m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Trailing Stop (pips)", "Distance used to activate trailing", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 20m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Trailing Step (pips)", "Increment used when trailing the stop", "Risk");

		_lotMultiplier = Param(nameof(LotMultiplier), 1.2m)
			.SetGreaterThanOrEqualTo(1m)
			.SetDisplay("Lot Multiplier", "Volume multiplier applied after each addition", "Martingale")
			.SetCanOptimize(true);

		_stepPips = Param(nameof(StepPips), 150m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Step (pips)", "Price distance before adding the next position", "Martingale")
			.SetCanOptimize(true);

		_takeProfitOffsetPips = Param(nameof(TakeProfitOffsetPips), 50m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Average TP Offset (pips)", "Additional take-profit distance after averaging", "Martingale")
			.SetCanOptimize(true);

		_enableMartingale = Param(nameof(EnableMartingale), true)
			.SetDisplay("Enable Martingale", "Allow averaged additions when price moves against the position", "Martingale");

		_enableTrailing = Param(nameof(EnableTrailing), true)
			.SetDisplay("Enable Trailing", "Enable trailing stop management", "Risk");

		_initialDirection = Param(nameof(InitialDirection), TradeDirection.None)
			.SetDisplay("Initial Direction", "Direction of the very first market order", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series used for management", "General");
	}

	/// <summary>
	/// Base order volume used for the very first trade.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing activation distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing increment expressed in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Volume multiplier applied after every martingale addition.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Price distance in pips required before placing another averaging trade.
	/// </summary>
	public decimal StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Additional take-profit offset applied once averaging positions exist.
	/// </summary>
	public decimal TakeProfitOffsetPips
	{
		get => _takeProfitOffsetPips.Value;
		set => _takeProfitOffsetPips.Value = value;
	}

	/// <summary>
	/// Enables martingale averaging behaviour.
	/// </summary>
	public bool EnableMartingale
	{
		get => _enableMartingale.Value;
		set => _enableMartingale.Value = value;
	}

	/// <summary>
	/// Enables trailing stop management.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Direction of the first manual-like trade.
	/// </summary>
	public TradeDirection InitialDirection
	{
		get => _initialDirection.Value;
		set => _initialDirection.Value = value;
	}

	/// <summary>
	/// Candle type used for periodic management checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_currentDirection = TradeDirection.None;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_currentVolume = 0m;
		_martingaleCount = 0;
		_longNextAddPrice = 0m;
		_shortNextAddPrice = 0m;
		_initialOrderSent = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		TrySendInitialOrder();
		UpdateTrailing(candle);
		TryAddMartingale(candle);
		HandleTargets(candle);
	}

	private void TrySendInitialOrder()
	{
		if (_initialOrderSent)
			return;

		if (InitialDirection == TradeDirection.None)
			return;

		var volume = NormalizeVolume(InitialVolume);
		if (volume <= 0m)
		{
			_initialOrderSent = true;
			return;
		}

		switch (InitialDirection)
		{
			case TradeDirection.Buy when AllowLong():
				BuyMarket(volume);
				break;
			case TradeDirection.Sell when AllowShort():
				SellMarket(volume);
				break;
		}

		_initialOrderSent = true;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (!EnableTrailing || Position == 0m)
			return;

		var trailingDistance = GetOffset(TrailingStopPips);
		if (trailingDistance <= 0m)
			return;

		var trailingStep = GetOffset(TrailingStepPips);
		var positionDirection = Math.Sign(Position);

		if (positionDirection > 0)
		{
			var profitDistance = candle.ClosePrice - _entryPrice;
			if (profitDistance <= 0m)
				return;

			if (_stopPrice <= _entryPrice && profitDistance > trailingDistance + trailingStep)
			{
				_stopPrice = candle.ClosePrice - trailingDistance;
			}
			else if (_stopPrice > _entryPrice && candle.ClosePrice - _stopPrice > trailingStep)
			{
				_stopPrice = candle.ClosePrice - trailingStep;
			}
		}
		else if (positionDirection < 0)
		{
			var profitDistance = _entryPrice - candle.ClosePrice;
			if (profitDistance <= 0m)
				return;

			if (_stopPrice >= _entryPrice && profitDistance > trailingDistance + trailingStep)
			{
				_stopPrice = candle.ClosePrice + trailingDistance;
			}
			else if ((_stopPrice == 0m || _stopPrice < _entryPrice) && _entryPrice - _stopPrice > trailingStep)
			{
				_stopPrice = candle.ClosePrice + trailingStep;
			}
		}
	}

	private void TryAddMartingale(ICandleMessage candle)
	{
		if (!EnableMartingale)
			return;

		var stepDistance = GetOffset(StepPips);
		if (stepDistance <= 0m)
			return;

		if (Position > 0m)
		{
			if (_martingaleCount <= 0)
				return;

			if (_longNextAddPrice == 0m)
				_longNextAddPrice = Math.Min(_entryPrice, candle.ClosePrice) - stepDistance;

			if (candle.LowPrice <= _longNextAddPrice && AllowLong())
			{
				var volume = CalculateNextVolume();
				if (volume > 0m)
				{
					BuyMarket(volume);
					_longNextAddPrice -= stepDistance;
				}
			}
		}
		else if (Position < 0m)
		{
			if (_martingaleCount <= 0)
				return;

			if (_shortNextAddPrice == 0m)
				_shortNextAddPrice = Math.Max(_entryPrice, candle.ClosePrice) + stepDistance;

			if (candle.HighPrice >= _shortNextAddPrice && AllowShort())
			{
				var volume = CalculateNextVolume();
				if (volume > 0m)
				{
					SellMarket(volume);
					_shortNextAddPrice += stepDistance;
				}
			}
		}
	}

	private void HandleTargets(ICandleMessage candle)
	{
		if (Position == 0m)
			return;

		if (Position > 0m)
		{
			if (_stopPrice > 0m && candle.LowPrice <= _stopPrice && AllowShort())
			{
				SellMarket(Position);
			}
			else if (_takePrice > 0m && candle.HighPrice >= _takePrice && AllowShort())
			{
				SellMarket(Position);
			}
		}
		else if (Position < 0m)
		{
			var absPosition = Math.Abs(Position);
			if (_stopPrice > 0m && candle.HighPrice >= _stopPrice && AllowLong())
			{
				BuyMarket(absPosition);
			}
			else if (_takePrice > 0m && candle.LowPrice <= _takePrice && AllowLong())
			{
				BuyMarket(absPosition);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged()
	{
		base.OnPositionChanged();

		if (Position == 0m)
		{
			_currentDirection = TradeDirection.None;
			_entryPrice = 0m;
			_stopPrice = 0m;
			_takePrice = 0m;
			_currentVolume = 0m;
			_martingaleCount = 0;
			_longNextAddPrice = 0m;
			_shortNextAddPrice = 0m;
			return;
		}

		var direction = Position > 0m ? TradeDirection.Buy : TradeDirection.Sell;
		var absPosition = Math.Abs(Position);
		var epsilon = GetVolumeEpsilon();

		if (direction != _currentDirection)
		{
			_currentDirection = direction;
			_martingaleCount = 1;
			_currentVolume = absPosition;
			_entryPrice = PositionPrice ?? 0m;
			UpdateTargets();
			UpdateNextAddLevels();
			return;
		}

		if (absPosition > _currentVolume + epsilon)
		{
			_martingaleCount++;
			_currentVolume = absPosition;
			_entryPrice = PositionPrice ?? _entryPrice;
			UpdateTargets();
			UpdateNextAddLevels();
		}
		else if (absPosition < _currentVolume - epsilon)
		{
			_currentVolume = absPosition;
			_entryPrice = PositionPrice ?? _entryPrice;
			UpdateTargets();
		}
	}

	private void UpdateTargets()
	{
		var stopOffset = GetOffset(StopLossPips);
		var takeOffset = GetOffset(TakeProfitPips);
		var averagingOffset = GetOffset(TakeProfitOffsetPips);

		if (_currentDirection == TradeDirection.Buy)
		{
			_stopPrice = stopOffset > 0m ? _entryPrice - stopOffset : 0m;

			if (_martingaleCount >= 2 && averagingOffset > 0m)
			{
				_takePrice = _entryPrice + averagingOffset;
			}
			else
			{
				_takePrice = takeOffset > 0m ? _entryPrice + takeOffset : 0m;
			}
		}
		else if (_currentDirection == TradeDirection.Sell)
		{
			_stopPrice = stopOffset > 0m ? _entryPrice + stopOffset : 0m;

			if (_martingaleCount >= 2 && averagingOffset > 0m)
			{
				_takePrice = _entryPrice - averagingOffset;
			}
			else
			{
				_takePrice = takeOffset > 0m ? _entryPrice - takeOffset : 0m;
			}
		}
	}

	private void UpdateNextAddLevels()
	{
		var stepDistance = GetOffset(StepPips);
		if (stepDistance <= 0m)
		{
			_longNextAddPrice = 0m;
			_shortNextAddPrice = 0m;
			return;
		}

		if (_currentDirection == TradeDirection.Buy)
		{
			var candidate = _entryPrice - stepDistance;
			if (_longNextAddPrice == 0m || candidate < _longNextAddPrice)
				_longNextAddPrice = candidate;
		}
		else if (_currentDirection == TradeDirection.Sell)
		{
			var candidate = _entryPrice + stepDistance;
			if (_shortNextAddPrice == 0m || candidate > _shortNextAddPrice)
				_shortNextAddPrice = candidate;
		}
	}

	private decimal CalculateNextVolume()
	{
		var exponent = _martingaleCount;
		var volume = InitialVolume * (decimal)Math.Pow((double)LotMultiplier, exponent);
		return NormalizeVolume(volume);
	}

	private decimal GetOffset(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? pips * step : pips;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (Security == null)
			return volume;

		var step = Security.VolumeStep ?? 0m;
		var min = Security.VolumeMin ?? 0m;
		var max = Security.VolumeMax ?? decimal.MaxValue;

		if (step <= 0m)
			return Math.Min(Math.Max(volume, min), max);

		var rounded = Math.Round(volume / step, MidpointRounding.AwayFromZero) * step;
		if (rounded < min)
			rounded = min;
		if (rounded > max)
			rounded = max;
		return rounded;
	}

	private decimal GetVolumeEpsilon()
	{
		var step = Security?.VolumeStep ?? 0m;
		return step > 0m ? step / 2m : 0.0000001m;
	}
}

