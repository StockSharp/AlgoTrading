using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD-based reversal strategy that reproduces the FORTRADER AOP pattern.
/// </summary>
public class MacdAoPatternStrategy : Strategy
{
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _bearishExtremeLevel;
	private readonly StrategyParam<decimal> _bearishNeutralLevel;
	private readonly StrategyParam<decimal> _bullishExtremeLevel;
	private readonly StrategyParam<decimal> _bullishNeutralLevel;
	private readonly StrategyParam<DataType> _candleType;

	private MACD _macd = null!;

	private decimal? _macdPrev1;
	private decimal? _macdPrev2;
	private decimal? _macdPrev3;

	private bool _bearishStageArmed;
	private bool _bearishTriggerReady;
	private bool _bearishSignalPending;

	private bool _bullishStageArmed;
	private bool _bullishTriggerReady;
	private bool _bullishSignalPending;

	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Distance to the take-profit level measured in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Distance to the stop-loss level measured in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Volume used for each market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Fast EMA period for the MACD indicator.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for the MACD indicator.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line EMA period for the MACD indicator.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// MACD level that arms the bearish setup when the oscillator stretches deeply negative.
	/// </summary>
	public decimal BearishExtremeLevel
	{
		get => _bearishExtremeLevel.Value;
		set => _bearishExtremeLevel.Value = value;
	}

	/// <summary>
	/// MACD level that confirms the bearish hook back toward the zero line.
	/// </summary>
	public decimal BearishNeutralLevel
	{
		get => _bearishNeutralLevel.Value;
		set => _bearishNeutralLevel.Value = value;
	}

	/// <summary>
	/// MACD level that arms the bullish setup when the oscillator stretches deeply positive.
	/// </summary>
	public decimal BullishExtremeLevel
	{
		get => _bullishExtremeLevel.Value;
		set => _bullishExtremeLevel.Value = value;
	}

	/// <summary>
	/// MACD level that confirms the bullish hook back toward the zero line.
	/// </summary>
	public decimal BullishNeutralLevel
	{
		get => _bullishNeutralLevel.Value;
		set => _bullishNeutralLevel.Value = value;
	}

	/// <summary>
	/// Candle data type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MacdAoPatternStrategy"/>.
	/// </summary>
	public MacdAoPatternStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 60)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk")
			.SetCanOptimize();

		_stopLossPips = Param(nameof(StopLossPips), 70)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")
			.SetCanOptimize();

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume for every market order", "Orders");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA length", "Indicators");

		_bearishExtremeLevel = Param(nameof(BearishExtremeLevel), -0.0015m)
			.SetDisplay("Bearish Extreme", "Negative MACD level that arms shorts", "Signals");

		_bearishNeutralLevel = Param(nameof(BearishNeutralLevel), -0.0005m)
			.SetDisplay("Bearish Neutral", "Negative MACD level that confirms the hook", "Signals");

		_bullishExtremeLevel = Param(nameof(BullishExtremeLevel), 0.0015m)
			.SetDisplay("Bullish Extreme", "Positive MACD level that arms longs", "Signals");

		_bullishNeutralLevel = Param(nameof(BullishNeutralLevel), 0.0005m)
			.SetDisplay("Bullish Neutral", "Positive MACD level that confirms the hook", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Source series for the strategy", "General");
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

		_macdPrev1 = null;
		_macdPrev2 = null;
		_macdPrev3 = null;

		_bearishStageArmed = false;
		_bearishTriggerReady = false;
		_bearishSignalPending = false;

		_bullishStageArmed = false;
		_bullishTriggerReady = false;
		_bullishSignalPending = false;

		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_macd = new MACD
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_macd, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdLine, decimal signalLine)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The signal line is not required for this pattern, but binding keeps both buffers available.
		_ = signalLine;

		// First handle protective exits using the finished candle range.
		HandlePositionExit(candle);

		if (!_macd.IsFormed)
		{
			UpdateMacdHistory(macdLine);
			return;
		}

		if (_macdPrev1 is null || _macdPrev2 is null || _macdPrev3 is null)
		{
			UpdateMacdHistory(macdLine);
			return;
		}

		var macd1 = _macdPrev1.Value;
		var macd2 = _macdPrev2.Value;
		var macd3 = _macdPrev3.Value;

		// --- Bearish sequence --------------------------------------------------

		if (macd1 < BearishExtremeLevel && !_bearishStageArmed)
		{
			// Arm the bearish setup after a deep negative MACD reading.
			_bearishStageArmed = true;
		}

		if (macd1 > BearishNeutralLevel && _bearishStageArmed)
		{
			// MACD returned toward zero, prepare for the hook confirmation.
			_bearishStageArmed = false;
			_bearishTriggerReady = true;
		}

		var bearishHook = _bearishTriggerReady &&
			macd1 < macd2 &&
			macd2 > macd3 &&
			macd1 < BearishNeutralLevel &&
			macd2 > BearishNeutralLevel;

		if (bearishHook)
		{
			// Confirm the bearish hook pattern.
			_bearishTriggerReady = false;
			_bearishSignalPending = true;
		}

		if (macd1 > 0)
		{
			// Positive MACD invalidates the bearish scenario.
			ResetBearishState();
		}

		if (_bearishSignalPending && Position <= 0)
		{
			// Execute the short entry with predefined stop-loss and take-profit.
			SellMarket(OrderVolume);

			var pip = GetPipSize();
			var entryPrice = candle.ClosePrice;
			_stopPrice = entryPrice + StopLossPips * pip;
			_takePrice = entryPrice - TakeProfitPips * pip;

			ResetBearishState();
		}

		// --- Bullish sequence --------------------------------------------------

		if (macd1 > BullishExtremeLevel && !_bullishStageArmed)
		{
			// Arm the bullish setup after a strong positive MACD expansion.
			_bullishStageArmed = true;
		}

		if (macd1 < 0)
		{
			// Negative MACD cancels the bullish scenario immediately.
			ResetBullishState();
		}
		else if (macd1 < BullishNeutralLevel && _bullishStageArmed)
		{
			// MACD retraced toward zero, allow the hook confirmation.
			_bullishStageArmed = false;
			_bullishTriggerReady = true;
		}

		var bullishHook = _bullishTriggerReady &&
			macd1 > macd2 &&
			macd2 < macd3 &&
			macd1 > BullishNeutralLevel &&
			macd2 < BullishNeutralLevel;

		if (bullishHook)
		{
			// Confirm the bullish hook pattern.
			_bullishTriggerReady = false;
			_bullishSignalPending = true;
		}

		if (_bullishSignalPending && Position >= 0)
		{
			// Execute the long entry with the configured targets.
			BuyMarket(OrderVolume);

			var pip = GetPipSize();
			var entryPrice = candle.ClosePrice;
			_stopPrice = entryPrice - StopLossPips * pip;
			_takePrice = entryPrice + TakeProfitPips * pip;

			ResetBullishState();
		}

		UpdateMacdHistory(macdLine);
	}

	private void HandlePositionExit(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var exitVolume = Math.Abs(Position);

			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				// Long stop-loss hit inside the finished candle range.
				SellMarket(exitVolume);
				ResetProtectionLevels();
				return;
			}

			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				// Long take-profit reached.
				SellMarket(exitVolume);
				ResetProtectionLevels();
			}
		}
		else if (Position < 0)
		{
			var exitVolume = Math.Abs(Position);

			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				// Short stop-loss triggered within the candle.
				BuyMarket(exitVolume);
				ResetProtectionLevels();
				return;
			}

			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				// Short take-profit reached.
				BuyMarket(exitVolume);
				ResetProtectionLevels();
			}
		}
	}

	private void UpdateMacdHistory(decimal macdValue)
	{
		_macdPrev3 = _macdPrev2;
		_macdPrev2 = _macdPrev1;
		_macdPrev1 = macdValue;
	}

	private void ResetBearishState()
	{
		_bearishStageArmed = false;
		_bearishTriggerReady = false;
		_bearishSignalPending = false;
	}

	private void ResetBullishState()
	{
		_bullishStageArmed = false;
		_bullishTriggerReady = false;
		_bullishSignalPending = false;
	}

	private void ResetProtectionLevels()
	{
		_stopPrice = null;
		_takePrice = null;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0.0001m;
		var decimals = Security?.Decimals;

		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}
}
