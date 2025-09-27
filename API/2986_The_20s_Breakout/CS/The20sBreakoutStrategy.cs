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
/// Conversion of the "The_20s_v020" expert advisor that waits for volatility squeeze breakouts.
/// Generates long and short signals based on the original indicator buffers and applies optional stop/target levels.
/// </summary>
public class The20sBreakoutStrategy : Strategy
{
	/// <summary>
	/// Indicator mode options for the The 20s breakout strategy.
	/// </summary>
	public enum The20sModes
	{
		Mode1,
		Mode2,
	}

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _levelPoints;
	private readonly StrategyParam<decimal> _ratio;
	private readonly StrategyParam<bool> _isDirect;
	private readonly StrategyParam<The20sModes> _mode;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _candles = new();

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Volume used when opening a new position after a signal.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance measured in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance measured in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Allow opening long positions on bullish signals.
	/// </summary>
	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	/// <summary>
	/// Allow opening short positions on bearish signals.
	/// </summary>
	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	/// <summary>
	/// Allow closing long positions when an opposite signal appears.
	/// </summary>
	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	/// <summary>
	/// Allow closing short positions when an opposite signal appears.
	/// </summary>
	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	/// <summary>
	/// Number of bars to delay the signal (0 = current bar, 1 = previous bar).
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Distance threshold in points used for confirming breakouts.
	/// </summary>
	public int LevelPoints
	{
		get => _levelPoints.Value;
		set => _levelPoints.Value = value;
	}

	/// <summary>
	/// Ratio used when splitting the previous bar's range into 20% bands.
	/// </summary>
	public decimal Ratio
	{
		get => _ratio.Value;
		set => _ratio.Value = value;
	}

	/// <summary>
	/// Switches the direction of generated signals.
	/// </summary>
	public bool IsDirect
	{
		get => _isDirect.Value;
		set => _isDirect.Value = value;
	}

	/// <summary>
	/// Selects between the two indicator algorithms described in the original source.
	/// </summary>
	public The20sModes Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Type of candles used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes configurable parameters.
	/// </summary>
	public The20sBreakoutStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume used for new entries", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 1m);

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Protective stop distance in points", "Risk Management");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Target distance in points", "Risk Management");

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
		.SetDisplay("Allow Long Entry", "Enable opening long trades", "Trading Permissions");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
		.SetDisplay("Allow Short Entry", "Enable opening short trades", "Trading Permissions");

		_allowLongExit = Param(nameof(AllowLongExit), true)
		.SetDisplay("Allow Long Exit", "Permit closing long trades", "Trading Permissions");

		_allowShortExit = Param(nameof(AllowShortExit), true)
		.SetDisplay("Allow Short Exit", "Permit closing short trades", "Trading Permissions");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetNotNegative()
		.SetDisplay("Signal Bar", "Bars to wait before acting on a signal", "Indicator");

		_levelPoints = Param(nameof(LevelPoints), 100)
		.SetNotNegative()
		.SetDisplay("Level Points", "Breakout threshold in points", "Indicator");

		_ratio = Param(nameof(Ratio), 0.2m)
		.SetDisplay("Ratio", "Band ratio derived from the previous bar range", "Indicator");

		_isDirect = Param(nameof(IsDirect), false)
		.SetDisplay("Direct Signals", "Match original or inverted signals", "Indicator");

		_mode = Param(nameof(Mode), The20sModes.Mode1)
		.SetDisplay("Indicator Mode", "Choose between algorithm option 1 or 2", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for analysis", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_candles.Clear();
		ResetLongProtection();
		ResetShortProtection();
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

		// Store the most recent candle for indicator calculations.
		_candles.Add(candle);

		var maxCandles = Math.Max(SignalBar + 10, 10);
		while (_candles.Count > maxCandles)
		_candles.RemoveAt(0);

		// Manage protective stop-loss and take-profit exits before acting on new signals.
		CheckProtectiveLevels(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0)
		priceStep = 1m;

		if (_candles.Count < SignalBar + 5)
		return;

		var (buySignal, sellSignal) = CalculateSignals(SignalBar, priceStep);

		if (buySignal)
		ProcessBuySignal(candle, priceStep);

		if (sellSignal)
		ProcessSellSignal(candle, priceStep);
	}

	private (bool buy, bool sell) CalculateSignals(int shift, decimal priceStep)
	{
		var latestIndex = _candles.Count - 1;
		var index = latestIndex - shift;

		if (index < 4)
		return (false, false);

		var current = _candles[index];
		var prev1 = _candles[index - 1];
		var prev2 = _candles[index - 2];
		var prev3 = _candles[index - 3];
		var prev4 = _candles[index - 4];

		var lastRange = prev1.HighPrice - prev1.LowPrice;
		if (lastRange <= 0)
		return (false, false);

		var top20 = prev1.HighPrice - lastRange * Ratio;
		var bottom20 = prev1.LowPrice + lastRange * Ratio;
		var levelDistance = LevelPoints * priceStep;

		bool buySignal = false;
		bool sellSignal = false;

		if (Mode == The20sModes.Mode1)
		{
			var compressionBreakdown = prev1.OpenPrice >= top20 && prev1.ClosePrice <= bottom20 &&
			current.LowPrice <= prev1.LowPrice - levelDistance;
			var compressionBreakout = prev1.OpenPrice <= bottom20 && prev1.ClosePrice >= top20 &&
			current.HighPrice >= prev1.HighPrice + levelDistance;

			if (IsDirect)
			{
				if (compressionBreakdown)
				buySignal = true;

				if (compressionBreakout)
				sellSignal = true;
			}
			else
			{
				if (compressionBreakdown)
				sellSignal = true;

				if (compressionBreakout)
				buySignal = true;
			}
		}
		else
		{
			var range4 = prev4.HighPrice - prev4.LowPrice;
			var range3 = prev3.HighPrice - prev3.LowPrice;
			var range2 = prev2.HighPrice - prev2.LowPrice;

			var volatilityExpansion = range4 > lastRange && range3 > lastRange && range2 > lastRange &&
			prev2.HighPrice > prev1.HighPrice && prev2.LowPrice < prev1.LowPrice;

			if (volatilityExpansion)
			{
				if (current.OpenPrice <= bottom20)
				{
					if (IsDirect)
					buySignal = true;
					else
					sellSignal = true;
				}

				if (current.OpenPrice >= top20)
				{
					if (IsDirect)
					sellSignal = true;
					else
					buySignal = true;
				}
			}
		}

		return (buySignal, sellSignal);
	}

	private void ProcessBuySignal(ICandleMessage candle, decimal priceStep)
	{
		var hadShort = Position < 0;
		var closeRequested = false;

		if (hadShort)
		{
			if (AllowShortExit)
			{
				var volumeToCover = Math.Abs(Position);
				if (volumeToCover > 0)
				{
					BuyMarket(volumeToCover);
					LogInfo($"Closing short at {candle.OpenTime:O} due to bullish signal.");
					ResetShortProtection();
					closeRequested = true;
				}
			}
			else
			{
				return;
			}
		}

		if (!AllowLongEntry || OrderVolume <= 0)
			return;

		if (Position > 0 && !closeRequested)
			return;

		BuyMarket(OrderVolume);
		LogInfo($"Opening long position at {candle.OpenTime:O}. Close={candle.ClosePrice}");
		SetLongProtection(candle.ClosePrice, priceStep);
	}

	private void ProcessSellSignal(ICandleMessage candle, decimal priceStep)
	{
		var hadLong = Position > 0;
		var closeRequested = false;

		if (hadLong)
		{
			if (AllowLongExit)
			{
				var volumeToClose = Math.Abs(Position);
				if (volumeToClose > 0)
				{
					SellMarket(volumeToClose);
					LogInfo($"Closing long at {candle.OpenTime:O} due to bearish signal.");
					ResetLongProtection();
					closeRequested = true;
				}
			}
			else
			{
				return;
			}
		}

		if (!AllowShortEntry || OrderVolume <= 0)
			return;

		if (Position < 0 && !closeRequested)
			return;

		SellMarket(OrderVolume);
		LogInfo($"Opening short position at {candle.OpenTime:O}. Close={candle.ClosePrice}");
		SetShortProtection(candle.ClosePrice, priceStep);
	}

	private void CheckProtectiveLevels(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var exitVolume = Math.Abs(Position);
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(exitVolume);
				LogInfo($"Long stop-loss triggered at {candle.OpenTime:O}. Low={candle.LowPrice}");
				ResetLongProtection();
			}
			else if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				SellMarket(exitVolume);
				LogInfo($"Long take-profit triggered at {candle.OpenTime:O}. High={candle.HighPrice}");
				ResetLongProtection();
			}
		}
		else if (Position < 0)
		{
			var exitVolume = Math.Abs(Position);
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(exitVolume);
				LogInfo($"Short stop-loss triggered at {candle.OpenTime:O}. High={candle.HighPrice}");
				ResetShortProtection();
			}
			else if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				BuyMarket(exitVolume);
				LogInfo($"Short take-profit triggered at {candle.OpenTime:O}. Low={candle.LowPrice}");
				ResetShortProtection();
			}
		}
	}

	private void SetLongProtection(decimal entryPrice, decimal priceStep)
	{
		if (StopLossPoints > 0)
		_longStop = entryPrice - StopLossPoints * priceStep;
		else
		_longStop = null;

		if (TakeProfitPoints > 0)
		_longTake = entryPrice + TakeProfitPoints * priceStep;
		else
		_longTake = null;
	}

	private void SetShortProtection(decimal entryPrice, decimal priceStep)
	{
		if (StopLossPoints > 0)
		_shortStop = entryPrice + StopLossPoints * priceStep;
		else
		_shortStop = null;

		if (TakeProfitPoints > 0)
		_shortTake = entryPrice - TakeProfitPoints * priceStep;
		else
		_shortTake = null;
	}

	private void ResetLongProtection()
	{
		_longStop = null;
		_longTake = null;
	}

	private void ResetShortProtection()
	{
		_shortStop = null;
		_shortTake = null;
	}
}
