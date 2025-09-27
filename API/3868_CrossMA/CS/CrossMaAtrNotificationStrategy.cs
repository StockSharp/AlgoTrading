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
/// Conversion of the MetaTrader "CrossMA" expert advisor.
/// Trades the crossover between a fast and a slow simple moving average and sets a stop loss one ATR away from the entry price.
/// Position size is determined by the configured maximum risk with optional reduction after consecutive losing trades.
/// Informational notifications are written to the log when trading actions occur.
/// </summary>
public class CrossMaAtrNotificationStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<bool> _enableNotifications;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _stopPrice;
	private bool _isFastBelowSlow;
	private bool _isInitialized;
	private Sides? _lastEntrySide;
	private decimal _lastEntryPrice;
	private int _consecutiveLosses;
	private decimal _signedPosition;

	/// <summary>
	/// Initializes a new instance of the <see cref="CrossMaAtrNotificationStrategy"/> class.
	/// </summary>
	public CrossMaAtrNotificationStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 4)
		.SetGreaterThanZero()
		.SetDisplay("Fast SMA Period", "Length of the fast SMA", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(2, 30, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("Slow SMA Period", "Length of the slow SMA", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 1);

		_atrPeriod = Param(nameof(AtrPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Length of the ATR stop calculator", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(3, 30, 1);

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Minimum volume used when risk based size is not available", "Risk");

		_maximumRisk = Param(nameof(MaximumRisk), 0.02m)
		.SetNotNegative()
		.SetDisplay("Maximum Risk", "Fraction of equity risked on each trade", "Risk");

		_decreaseFactor = Param(nameof(DecreaseFactor), 3m)
		.SetNotNegative()
		.SetDisplay("Decrease Factor", "Reduces position size after losing trades", "Risk");

		_enableNotifications = Param(nameof(EnableNotifications), true)
		.SetDisplay("Enable Notifications", "Write informational messages when actions happen", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candles processed by the strategy", "General");
	}

	/// <summary>
	/// Period for the fast SMA.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Period for the slow SMA.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// ATR period for the stop loss calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Minimum volume used when risk calculations are not possible.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Fraction of account equity risked on a single trade.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Factor that decreases volume after consecutive losses.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Enables informational notifications in the logs.
	/// </summary>
	public bool EnableNotifications
	{
		get => _enableNotifications.Value;
		set => _enableNotifications.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
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

		_stopPrice = null;
		_isInitialized = false;
		_lastEntrySide = null;
		_lastEntryPrice = 0m;
		_consecutiveLosses = 0;
		_signedPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastSma = new SimpleMovingAverage
		{
			Length = FastPeriod
		};

		var slowSma = new SimpleMovingAverage
		{
			Length = SlowPeriod
		};

		var atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(fastSma, slowSma, atr, ProcessCandle)
		.Start();

		StartProtection();
	}

	/// <summary>
	/// Processes each finished candle together with indicator values.
	/// </summary>
	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_isInitialized)
		{
			// Store the initial relation between fast and slow averages to detect future crossings.
			_isFastBelowSlow = fast < slow;
			_isInitialized = true;
			return;
		}

		// Check whether protective stops should close the position.
		if (Position > 0 && _stopPrice is decimal longStop && candle.LowPrice <= longStop)
		{
			SellMarket(Position);
			_stopPrice = null;
			Notify($"Long position closed by stop at {longStop:F4}.");
		}
		else if (Position < 0 && _stopPrice is decimal shortStop && candle.HighPrice >= shortStop)
		{
			BuyMarket(-Position);
			_stopPrice = null;
			Notify($"Short position closed by stop at {shortStop:F4}.");
		}

		var fastBelowSlow = fast < slow;

		if (_isFastBelowSlow && !fastBelowSlow)
		{
			// Fast SMA crossed above the slow SMA -> open or flip to a long position.
			EnterLong(candle, atr);
		}
		else if (!_isFastBelowSlow && fastBelowSlow)
		{
			// Fast SMA crossed below the slow SMA -> open or flip to a short position.
			EnterShort(candle, atr);
		}

		_isFastBelowSlow = fastBelowSlow;
	}

	private void EnterLong(ICandleMessage candle, decimal atr)
	{
		var price = candle.ClosePrice;
		var volume = CalculateTradeVolume(price);
		if (volume <= 0)
		return;

		var totalVolume = volume + (Position < 0 ? Math.Abs(Position) : 0m);
		if (totalVolume <= 0)
		return;

		BuyMarket(totalVolume);
		_stopPrice = price - atr;
		Notify($"Buy signal at {price:F4} with stop {(_stopPrice ?? 0m):F4}.");
	}

	private void EnterShort(ICandleMessage candle, decimal atr)
	{
		var price = candle.ClosePrice;
		var volume = CalculateTradeVolume(price);
		if (volume <= 0)
		return;

		var totalVolume = volume + (Position > 0 ? Position : 0m);
		if (totalVolume <= 0)
		return;

		SellMarket(totalVolume);
		_stopPrice = price + atr;
		Notify($"Sell signal at {price:F4} with stop {(_stopPrice ?? 0m):F4}.");
	}

	private decimal CalculateTradeVolume(decimal price)
	{
		var baseVolume = BaseVolume > 0 ? BaseVolume : 1m;
		var volume = baseVolume;

		if (price > 0m)
		{
			var equity = Portfolio?.CurrentValue ?? 0m;
			if (equity > 0m && MaximumRisk > 0m)
			{
				volume = equity * MaximumRisk / price;
				if (volume < baseVolume)
				volume = baseVolume;
			}
		}

		if (DecreaseFactor > 0m && _consecutiveLosses > 1)
		{
			// Reduce volume similarly to the MetaTrader decrease factor logic.
			var reduction = volume * _consecutiveLosses / DecreaseFactor;
			volume -= reduction;
		}

		if (volume <= 0m)
		volume = baseVolume;

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security != null)
		{
			var step = security.VolumeStep ?? 1m;
			if (step <= 0m)
			step = 1m;

			if (volume < step)
			volume = step;

			var steps = Math.Floor(volume / step);
			if (steps < 1m)
			steps = 1m;

			volume = steps * step;
		}

		if (volume <= 0m)
		volume = 1m;

		return volume;
	}

	private void Notify(string message)
	{
		if (!EnableNotifications)
		return;

		LogInfo(message);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var volume = trade.Trade.Volume;
		if (volume <= 0m)
		return;

		var delta = trade.Order.Side == Sides.Buy ? volume : -volume;
		var previousPosition = _signedPosition;
		_signedPosition += delta;

		if (previousPosition == 0m && _signedPosition != 0m)
		{
			// A new position has been opened, remember direction and price.
			_lastEntrySide = delta > 0m ? Sides.Buy : Sides.Sell;
			_lastEntryPrice = trade.Trade.Price;
		}
		else if (previousPosition != 0m && _signedPosition == 0m)
		{
			// Position closed, evaluate profit to update the loss streak.
			var exitPrice = trade.Trade.Price;
			if (_lastEntrySide != null && _lastEntryPrice != 0m)
			{
				var profit = _lastEntrySide == Sides.Buy
				? exitPrice - _lastEntryPrice
				: _lastEntryPrice - exitPrice;

				if (profit > 0m)
				{
					_consecutiveLosses = 0;
				}
				else if (profit < 0m)
				{
					_consecutiveLosses++;
				}
			}

			_lastEntrySide = null;
			_lastEntryPrice = 0m;
			_stopPrice = null;
		}
	}
}

