using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA triple crossover strategy converted from the MetaTrader 5 "up3x1" expert.
/// Uses three exponential moving averages with optional stop loss, take profit and trailing logic.
/// Position size is reduced after losing trades similar to the original lot optimization routine.
/// </summary>
public class Up3x1Strategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _mediumPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _takeProfitOffset;
	private readonly StrategyParam<decimal> _stopLossOffset;
	private readonly StrategyParam<decimal> _trailingStopOffset;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _riskFraction;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _mediumEma;
	private ExponentialMovingAverage _slowEma;

	private bool _hasPrevValues;
	private decimal _prevFast;
	private decimal _prevMedium;
	private decimal _prevSlow;

	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	private int _losses;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Medium EMA period.
	/// </summary>
	public int MediumPeriod
	{
		get => _mediumPeriod.Value;
		set => _mediumPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Absolute price distance for take profit.
	/// </summary>
	public decimal TakeProfitOffset
	{
		get => _takeProfitOffset.Value;
		set => _takeProfitOffset.Value = value;
	}

	/// <summary>
	/// Absolute price distance for stop loss.
	/// </summary>
	public decimal StopLossOffset
	{
		get => _stopLossOffset.Value;
		set => _stopLossOffset.Value = value;
	}

	/// <summary>
	/// Absolute trailing stop distance.
	/// </summary>
	public decimal TrailingStopOffset
	{
		get => _trailingStopOffset.Value;
		set => _trailingStopOffset.Value = value;
	}

	/// <summary>
	/// Base volume used when dynamic sizing cannot be calculated.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Fraction of portfolio value used for dynamic position sizing.
	/// </summary>
	public decimal RiskFraction
	{
		get => _riskFraction.Value;
		set => _riskFraction.Value = value;
	}

	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public Up3x1Strategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 24)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Period of the fastest EMA", "Indicators");

		_mediumPeriod = Param(nameof(MediumPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("Medium EMA", "Period of the middle EMA", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 120)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Period of the slowest EMA", "Indicators");

		_takeProfitOffset = Param(nameof(TakeProfitOffset), 0.015m)
			.SetDisplay("Take Profit", "Absolute take profit distance in price units", "Risk");

		_stopLossOffset = Param(nameof(StopLossOffset), 0.01m)
			.SetDisplay("Stop Loss", "Absolute stop loss distance in price units", "Risk");

		_trailingStopOffset = Param(nameof(TrailingStopOffset), 0.004m)
			.SetDisplay("Trailing", "Trailing stop distance that follows price", "Risk");

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetDisplay("Base Volume", "Fallback trade volume if dynamic sizing fails", "Money Management");

		_riskFraction = Param(nameof(RiskFraction), 0.02m)
			.SetDisplay("Risk Fraction", "Fraction of portfolio value used for sizing", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for calculations", "General");
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

		Volume = BaseVolume;
		ResetState();
		_losses = 0;
		_hasPrevValues = false;
		_prevFast = 0m;
		_prevMedium = 0m;
		_prevSlow = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = BaseVolume;

		_fastEma = new ExponentialMovingAverage
		{
			Length = FastPeriod,
			CandlePrice = CandlePrice.Close
		};

		_mediumEma = new ExponentialMovingAverage
		{
			Length = MediumPeriod,
			CandlePrice = CandlePrice.Close
		};

		_slowEma = new ExponentialMovingAverage
		{
			Length = SlowPeriod,
			CandlePrice = CandlePrice.Close
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_fastEma, _mediumEma, _slowEma, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal mediumValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastEma.IsFormed || !_mediumEma.IsFormed || !_slowEma.IsFormed)
			return;

		if (!_hasPrevValues)
		{
			_prevFast = fastValue;
			_prevMedium = mediumValue;
			_prevSlow = slowValue;
			_hasPrevValues = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFast = fastValue;
			_prevMedium = mediumValue;
			_prevSlow = slowValue;
			return;
		}

		if (Position > 0)
		{
			if (TryHandleLongExit(candle, fastValue, mediumValue, slowValue))
			{
				_prevFast = fastValue;
				_prevMedium = mediumValue;
				_prevSlow = slowValue;
				return;
			}
		}
		else if (Position < 0)
		{
			if (TryHandleShortExit(candle, fastValue, mediumValue, slowValue))
			{
				_prevFast = fastValue;
				_prevMedium = mediumValue;
				_prevSlow = slowValue;
				return;
			}
		}
		else
		{
			var bullishSetup = _prevFast < _prevMedium && _prevMedium < _prevSlow && mediumValue < fastValue && fastValue < slowValue;
			var bearishSetup = _prevFast > _prevMedium && _prevMedium > _prevSlow && mediumValue > fastValue && fastValue > slowValue;

			if (bullishSetup)
			{
				TryEnterLong(candle);
			}
			else if (bearishSetup)
			{
				TryEnterShort(candle);
			}
		}

		_prevFast = fastValue;
		_prevMedium = mediumValue;
		_prevSlow = slowValue;
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		var volume = CalculateOrderVolume();

		if (volume <= 0m)
		{
			LogInfo("Skipped long entry because calculated volume is below minimum.");
			return;
		}

		BuyMarket(volume);

		_entryPrice = candle.ClosePrice;
		_highestPrice = candle.HighPrice;
		_lowestPrice = candle.LowPrice;

		LogInfo($"Enter long at {candle.ClosePrice} with volume {volume}. Loss counter: {_losses}.");
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		var volume = CalculateOrderVolume();

		if (volume <= 0m)
		{
			LogInfo("Skipped short entry because calculated volume is below minimum.");
			return;
		}

		SellMarket(volume);

		_entryPrice = candle.ClosePrice;
		_highestPrice = candle.HighPrice;
		_lowestPrice = candle.LowPrice;

		LogInfo($"Enter short at {candle.ClosePrice} with volume {volume}. Loss counter: {_losses}.");
	}

	private bool TryHandleLongExit(ICandleMessage candle, decimal fastValue, decimal mediumValue, decimal slowValue)
	{
		if (_entryPrice <= 0m)
			return false;

		var exitPrice = 0m;
		var reason = string.Empty;

		if (TakeProfitOffset > 0m)
		{
			var target = _entryPrice + TakeProfitOffset;
			if (candle.HighPrice >= target)
			{
				exitPrice = target;
				reason = "Take profit reached";
			}
		}

		if (exitPrice == 0m && StopLossOffset > 0m)
		{
			var stop = _entryPrice - StopLossOffset;
			if (candle.LowPrice <= stop)
			{
				exitPrice = stop;
				reason = "Stop loss triggered";
			}
		}

		_highestPrice = candle.HighPrice > _highestPrice ? candle.HighPrice : _highestPrice;

		if (exitPrice == 0m && TrailingStopOffset > 0m && _highestPrice - _entryPrice > TrailingStopOffset)
		{
			var trail = _highestPrice - TrailingStopOffset;
			if (candle.LowPrice <= trail)
			{
				exitPrice = trail;
				reason = "Trailing stop hit";
			}
		}

		if (exitPrice == 0m)
		{
			var reversal = _prevFast > _prevMedium && _prevMedium > _prevSlow && slowValue < fastValue && fastValue < mediumValue;
			if (reversal)
			{
				exitPrice = candle.ClosePrice;
				reason = "EMA reversal";
			}
		}

		if (exitPrice == 0m)
			return false;

		ExitPosition(exitPrice, reason);
		return true;
	}

	private bool TryHandleShortExit(ICandleMessage candle, decimal fastValue, decimal mediumValue, decimal slowValue)
	{
		if (_entryPrice <= 0m)
			return false;

		var exitPrice = 0m;
		var reason = string.Empty;

		if (TakeProfitOffset > 0m)
		{
			var target = _entryPrice - TakeProfitOffset;
			if (candle.LowPrice <= target)
			{
				exitPrice = target;
				reason = "Take profit reached";
			}
		}

		if (exitPrice == 0m && StopLossOffset > 0m)
		{
			var stop = _entryPrice + StopLossOffset;
			if (candle.HighPrice >= stop)
			{
				exitPrice = stop;
				reason = "Stop loss triggered";
			}
		}

		_lowestPrice = _lowestPrice == 0m || candle.LowPrice < _lowestPrice ? candle.LowPrice : _lowestPrice;

		if (exitPrice == 0m && TrailingStopOffset > 0m && _entryPrice - _lowestPrice > TrailingStopOffset)
		{
			var trail = _lowestPrice + TrailingStopOffset;
			if (candle.HighPrice >= trail)
			{
				exitPrice = trail;
				reason = "Trailing stop hit";
			}
		}

		if (exitPrice == 0m)
		{
			var reversal = _prevFast > _prevMedium && _prevMedium > _prevSlow && slowValue < fastValue && fastValue < mediumValue;
			if (reversal)
			{
				exitPrice = candle.ClosePrice;
				reason = "EMA reversal";
			}
		}

		if (exitPrice == 0m)
			return false;

		ExitPosition(exitPrice, reason);
		return true;
	}

	private void ExitPosition(decimal exitPrice, string reason)
	{
		var isLong = Position > 0;
		var volume = Math.Abs(Position);

		if (volume <= 0m)
			return;

		var pnl = isLong
			? (exitPrice - _entryPrice) * volume
			: (_entryPrice - exitPrice) * volume;

		if (isLong)
		{
			SellMarket(volume);
		}
		else
		{
			BuyMarket(volume);
		}

		LogInfo($"Exit {(isLong ? "long" : "short")} at {exitPrice} because {reason}. Approx PnL: {pnl}.");

		if (pnl < 0m)
			_losses++;

		ResetState();
	}

	private decimal CalculateOrderVolume()
	{
		var volume = 0m;

		var portfolioValue = Portfolio?.CurrentValue ?? 0m;

		if (portfolioValue > 0m && RiskFraction > 0m)
			volume = portfolioValue * RiskFraction / 1000m;

		if (volume <= 0m)
			volume = BaseVolume;

		if (_losses > 1)
		{
			var reduction = volume * _losses / 3m;
			volume -= reduction;

			if (volume <= 0m)
				volume = BaseVolume;
		}

		volume = AdjustVolumeToInstrument(volume);

		return volume;
	}

	private decimal AdjustVolumeToInstrument(decimal volume)
	{
		var security = Security;

		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;

		if (step > 0m)
			volume = Math.Floor(volume / step) * step;

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			return 0m;

		var maxVolume = security.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	private void ResetState()
	{
		_entryPrice = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
	}
}
