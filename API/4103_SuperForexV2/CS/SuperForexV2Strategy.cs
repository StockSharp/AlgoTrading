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
/// SuperForexV2 strategy converted from MetaTrader 4.
/// Uses RSI thresholds for entries, closes on opposite signals, and manages risk through pip-based stops.
/// </summary>
public class SuperForexV2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _balanceToVolumeDivider;

	private RelativeStrengthIndex _rsi = null!;
	private decimal _pipSize;
	private decimal _takeProfitDistance;
	private decimal _stopLossDistance;
	private decimal _trailingDistance;
	private decimal _entryPrice;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public SuperForexV2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for RSI calculations", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Number of bars used by the RSI", "Indicators");

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 62m)
			.SetDisplay("RSI Upper", "Overbought threshold used for shorts and long exits", "Indicators");

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 42m)
			.SetDisplay("RSI Lower", "Oversold threshold used for longs and short exits", "Indicators");

		_takeProfitPips = Param(nameof(TakeProfitPips), 109m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Distance of the take-profit order", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 9m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Distance of the protective stop", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 6m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing distance applied once price moves in favor", "Risk");

		_initialVolume = Param(nameof(InitialVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Fallback order volume when balance is unknown", "Money Management");

		_maxVolume = Param(nameof(MaxVolume), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Max Volume", "Upper cap for the calculated trade volume", "Money Management");

		_balanceToVolumeDivider = Param(nameof(BalanceToVolumeDivider), 10000m)
			.SetGreaterThanZero()
			.SetDisplay("Balance Divider", "Account balance divisor used to compute volume", "Money Management");
	}

	/// <summary>
	/// Candle type used for the RSI calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI lookback length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Upper RSI threshold that triggers shorts and exits longs.
	/// </summary>
	public decimal RsiUpperLevel
	{
		get => _rsiUpperLevel.Value;
		set => _rsiUpperLevel.Value = value;
	}

	/// <summary>
	/// Lower RSI threshold that triggers longs and exits shorts.
	/// </summary>
	public decimal RsiLowerLevel
	{
		get => _rsiLowerLevel.Value;
		set => _rsiLowerLevel.Value = value;
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
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
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
	/// Minimum order volume used when account balance information is unavailable.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Maximum order volume allowed by the strategy.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Divider applied to the account balance to derive the working volume.
	/// </summary>
	public decimal BalanceToVolumeDivider
	{
		get => _balanceToVolumeDivider.Value;
		set => _balanceToVolumeDivider.Value = value;
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

		_rsi = null!;
		_pipSize = 0m;
		_takeProfitDistance = 0m;
		_stopLossDistance = 0m;
		_trailingDistance = 0m;
		_entryPrice = 0m;
		_longStop = 0m;
		_longTake = 0m;
		_shortStop = 0m;
		_shortTake = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Cache pip size and convert pip-based settings into absolute prices.
		_pipSize = CalculatePipSize();
		RecalculateDistances();

		// Prepare the RSI indicator that drives entries and exits.
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Keep distance cache synced with current parameters and metadata.
		RecalculateDistances();

		// Update trailing stops before evaluating exits.
		UpdateTrailing(candle);

		if (Position > 0m)
		{
			if (HandleLongExit(candle, rsiValue))
				return;
		}
		else if (Position < 0m)
		{
			if (HandleShortExit(candle, rsiValue))
				return;
		}

		if (Position != 0m)
			return;

		if (!_rsi.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = GetTradeVolume();
		if (volume <= 0m)
			return;

		// Original EA opens longs when RSI is below the lower band.
		if (rsiValue < RsiLowerLevel)
		{
			EnterLong(candle, volume);
		}
		// Shorts are triggered when RSI exceeds the upper band.
		else if (rsiValue > RsiUpperLevel)
		{
			EnterShort(candle, volume);
		}
	}

	private bool HandleLongExit(ICandleMessage candle, decimal rsiValue)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return false;

		// Exit if price hits the trailing-adjusted stop level.
		if (_longStop > 0m && candle.LowPrice <= _longStop)
		{
			SellMarket(volume);
			ResetPositionState();
			return true;
		}

		// Exit if the take-profit was reached during the bar.
		if (_longTake > 0m && candle.HighPrice >= _longTake)
		{
			SellMarket(volume);
			ResetPositionState();
			return true;
		}

		// Close on opposite RSI signal just like the MT4 script.
		if (rsiValue > RsiUpperLevel)
		{
			SellMarket(volume);
			ResetPositionState();
			return true;
		}

		return false;
	}

	private bool HandleShortExit(ICandleMessage candle, decimal rsiValue)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return false;

		if (_shortStop > 0m && candle.HighPrice >= _shortStop)
		{
			BuyMarket(volume);
			ResetPositionState();
			return true;
		}

		if (_shortTake > 0m && candle.LowPrice <= _shortTake)
		{
			BuyMarket(volume);
			ResetPositionState();
			return true;
		}

		if (rsiValue < RsiLowerLevel)
		{
			BuyMarket(volume);
			ResetPositionState();
			return true;
		}

		return false;
	}

	private void EnterLong(ICandleMessage candle, decimal volume)
	{
		_entryPrice = candle.ClosePrice;
		_longStop = _stopLossDistance > 0m ? _entryPrice - _stopLossDistance : 0m;
		_longTake = _takeProfitDistance > 0m ? _entryPrice + _takeProfitDistance : 0m;
		_shortStop = 0m;
		_shortTake = 0m;

		// Send the market order using the computed lot size.
		BuyMarket(volume);
	}

	private void EnterShort(ICandleMessage candle, decimal volume)
	{
		_entryPrice = candle.ClosePrice;
		_shortStop = _stopLossDistance > 0m ? _entryPrice + _stopLossDistance : 0m;
		_shortTake = _takeProfitDistance > 0m ? _entryPrice - _takeProfitDistance : 0m;
		_longStop = 0m;
		_longTake = 0m;

		SellMarket(volume);
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (_trailingDistance <= 0m || _entryPrice <= 0m)
			return;

		if (Position > 0m)
		{
			var move = candle.ClosePrice - _entryPrice;
			if (move > _trailingDistance)
			{
				var currentDistance = _longStop > 0m ? candle.ClosePrice - _longStop : decimal.MaxValue;
				if (_longStop == 0m || currentDistance > _trailingDistance)
					_longStop = candle.ClosePrice - _trailingDistance;
			}
		}
		else if (Position < 0m)
		{
			var move = _entryPrice - candle.ClosePrice;
			if (move > _trailingDistance)
			{
				var currentDistance = _shortStop > 0m ? _shortStop - candle.ClosePrice : decimal.MaxValue;
				if (_shortStop == 0m || currentDistance > _trailingDistance)
					_shortStop = candle.ClosePrice + _trailingDistance;
			}
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_longStop = 0m;
		_longTake = 0m;
		_shortStop = 0m;
		_shortTake = 0m;
	}

	private void RecalculateDistances()
	{
		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		_takeProfitDistance = ToPrice(TakeProfitPips);
		_stopLossDistance = ToPrice(StopLossPips);
		_trailingDistance = ToPrice(TrailingStopPips);
	}

	private decimal ToPrice(decimal pips)
	{
		if (pips <= 0m || _pipSize <= 0m)
			return 0m;

		return pips * _pipSize;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = GetDecimalPlaces(step);
		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		value = Math.Abs(value);
		var decimals = 0;
		var scaled = value;

		while (scaled != Math.Floor(scaled) && decimals < 10)
		{
			scaled *= 10m;
			decimals++;
		}

		return decimals;
	}

	private decimal GetTradeVolume()
	{
		var volume = InitialVolume;

		var balance = GetPortfolioValue();
		if (balance > 0m && BalanceToVolumeDivider > 0m)
		{
			volume = balance / BalanceToVolumeDivider;
			if (volume <= 0m)
				volume = InitialVolume;
		}

		if (MaxVolume > 0m)
			volume = Math.Min(volume, MaxVolume);

		return AlignVolume(volume);
	}

	private decimal GetPortfolioValue()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return 0m;

		if (portfolio.CurrentValue > 0m)
			return portfolio.CurrentValue;

		return portfolio.BeginValue;
	}

	private decimal AlignVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		var min = security.MinVolume ?? 0m;
		var max = security.MaxVolume ?? decimal.MaxValue;

		if (step > 0m)
		{
			var steps = Math.Round(volume / step, MidpointRounding.AwayFromZero);
			volume = steps * step;
		}

		if (min > 0m && volume < min)
			volume = min;

		if (max > 0m && volume > max)
			volume = max;

		return volume;
	}
}
