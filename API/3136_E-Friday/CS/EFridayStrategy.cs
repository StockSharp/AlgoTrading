namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Contrarian intraday strategy that avoids trading on Fridays.
/// </summary>
public class EFridayStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<decimal> _volumeParam;
	private readonly StrategyParam<decimal> _stopLossPipsParam;
	private readonly StrategyParam<decimal> _takeProfitPipsParam;
	private readonly StrategyParam<decimal> _trailingStopPipsParam;
	private readonly StrategyParam<decimal> _trailingStepPipsParam;
	private readonly StrategyParam<int> _startHourParam;
	private readonly StrategyParam<bool> _useCloseHourParam;
	private readonly StrategyParam<int> _endHourParam;

	private TimeSpan _timeFrame;
	private decimal _pipSize;

	private bool _timeFrameWarningIssued;
	private bool _fridayMessageShown;
	private bool _beforeStartMessageShown;
	private bool _afterEndMessageShown;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;

	public EFridayStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used to evaluate reversal candles", "General");

		_volumeParam = Param(nameof(TradeVolume), 1m)
		.SetDisplay("Trade Volume", "Order volume expressed in lots", "Trading")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_stopLossPipsParam = Param(nameof(StopLossPips), 50m)
		.SetDisplay("Stop Loss (pips)", "Distance for the protective stop in pips", "Risk")
		.SetNotNegative()
		.SetCanOptimize(true);

		_takeProfitPipsParam = Param(nameof(TakeProfitPips), 50m)
		.SetDisplay("Take Profit (pips)", "Distance for the profit target in pips", "Risk")
		.SetNotNegative()
		.SetCanOptimize(true);

		_trailingStopPipsParam = Param(nameof(TrailingStopPips), 5m)
		.SetDisplay("Trailing Stop (pips)", "Initial trailing stop distance in pips", "Risk")
		.SetNotNegative()
		.SetCanOptimize(true);

		_trailingStepPipsParam = Param(nameof(TrailingStepPips), 5m)
		.SetDisplay("Trailing Step (pips)", "Minimal advance required to tighten the trailing stop", "Risk")
		.SetNotNegative()
		.SetCanOptimize(true);

		_startHourParam = Param(nameof(StartHour), 5)
		.SetDisplay("Start Hour", "Hour (exchange time) when trading becomes allowed", "Sessions")
		.SetMinMax(0, 23)
		.SetCanOptimize(true);

		_useCloseHourParam = Param(nameof(UseCloseHour), true)
		.SetDisplay("Use Close Hour", "Enable forced position closing after the configured hour", "Sessions");

		_endHourParam = Param(nameof(EndHour), 10)
		.SetDisplay("End Hour", "Hour (exchange time) when positions are closed", "Sessions")
		.SetMinMax(0, 23)
		.SetCanOptimize(true);
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public decimal TradeVolume
	{
		get => _volumeParam.Value;
		set => _volumeParam.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPipsParam.Value;
		set => _stopLossPipsParam.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPipsParam.Value;
		set => _takeProfitPipsParam.Value = value;
	}

	public decimal TrailingStopPips
	{
		get => _trailingStopPipsParam.Value;
		set => _trailingStopPipsParam.Value = value;
	}

	public decimal TrailingStepPips
	{
		get => _trailingStepPipsParam.Value;
		set => _trailingStepPipsParam.Value = value;
	}

	public int StartHour
	{
		get => _startHourParam.Value;
		set => _startHourParam.Value = value;
	}

	public bool UseCloseHour
	{
		get => _useCloseHourParam.Value;
		set => _useCloseHourParam.Value = value;
	}

	public int EndHour
	{
		get => _endHourParam.Value;
		set => _endHourParam.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
			throw new InvalidOperationException("Trailing step must be positive when the trailing stop is enabled.");

		_timeFrame = GetTimeFrame();
		UpdatePipSize();

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

		if (_pipSize <= 0m)
			UpdatePipSize();

		Volume = TradeVolume;

		if (ManageExistingPosition(candle))
			return;

		if (_timeFrame > TimeSpan.FromHours(1))
		{
			if (!_timeFrameWarningIssued)
			{
				this.LogWarning("The original expert advisor only works on H1 or lower periods. Entries are disabled.");
				_timeFrameWarningIssued = true;
			}

			return;
		}

		_timeFrameWarningIssued = false;

		if (!IsTradingSessionActive(candle))
			return;

		TryEnter(candle);
	}

	private bool ManageExistingPosition(ICandleMessage candle)
	{
		UpdateEntrySnapshot();

		if (Position > 0m)
			return HandleLongPosition(candle);

		if (Position < 0m)
			return HandleShortPosition(candle);

		ResetLong();
		ResetShort();
		return false;
	}

	private bool HandleLongPosition(ICandleMessage candle)
	{
		if (_longEntryPrice is not decimal entryPrice)
			return false;

		UpdateLongTrailing(candle, entryPrice);

		if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(Position);
			ResetLong();
			return true;
		}

		if (_longTakeProfitPrice is decimal take && candle.HighPrice >= take)
		{
			SellMarket(Position);
			ResetLong();
			return true;
		}

		return false;
	}

	private bool HandleShortPosition(ICandleMessage candle)
	{
		if (_shortEntryPrice is not decimal entryPrice)
			return false;

		UpdateShortTrailing(candle, entryPrice);

		if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
		{
			BuyMarket(-Position);
			ResetShort();
			return true;
		}

		if (_shortTakeProfitPrice is decimal take && candle.LowPrice <= take)
		{
			BuyMarket(-Position);
			ResetShort();
			return true;
		}

		return false;
	}

	private void UpdateLongTrailing(ICandleMessage candle, decimal entryPrice)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m || _pipSize <= 0m)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var stepDistance = TrailingStepPips * _pipSize;

		// Adjust the trailing stop only when the price advanced far enough.
		if (candle.ClosePrice - entryPrice <= trailingDistance + stepDistance)
			return;

		var threshold = candle.ClosePrice - (trailingDistance + stepDistance);
		if (_longStopPrice is decimal currentStop && currentStop >= threshold)
			return;

		_longStopPrice = candle.ClosePrice - trailingDistance;
	}

	private void UpdateShortTrailing(ICandleMessage candle, decimal entryPrice)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m || _pipSize <= 0m)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var stepDistance = TrailingStepPips * _pipSize;

		// Tighten the trailing stop only after the price moves in favour of the position.
		if (entryPrice - candle.ClosePrice <= trailingDistance + stepDistance)
			return;

		var threshold = candle.ClosePrice + trailingDistance + stepDistance;
		if (_shortStopPrice is decimal currentStop && currentStop <= threshold)
			return;

		_shortStopPrice = candle.ClosePrice + trailingDistance;
	}

	private bool IsTradingSessionActive(ICandleMessage candle)
	{
		var time = candle.CloseTime;

		if (time.DayOfWeek == DayOfWeek.Friday)
		{
			if (!_fridayMessageShown)
			{
				this.LogInfo("Trading on Friday is disabled by design.");
				_fridayMessageShown = true;
			}

			return false;
		}

		_fridayMessageShown = false;

		var hour = time.Hour;

		if (hour < StartHour)
		{
			if (!_beforeStartMessageShown)
			{
				this.LogInfo($"Trading window starts at {StartHour:00}:00. Current hour: {hour:00}.");
				_beforeStartMessageShown = true;
			}

			return false;
		}

		_beforeStartMessageShown = false;

		if (UseCloseHour && hour > EndHour)
		{
			if (!_afterEndMessageShown)
			{
				this.LogInfo($"Trading window ended at {EndHour:00}:00. Current hour: {hour:00}. Closing open positions.");
				_afterEndMessageShown = true;
			}

			CloseActivePosition();
			return false;
		}

		_afterEndMessageShown = false;

		return true;
	}

	private void CloseActivePosition()
	{
		// Flatten any exposure to respect the configured trading window.
		if (Position > 0m)
		{
			SellMarket(Position);
			ResetLong();
		}
		else if (Position < 0m)
		{
			BuyMarket(-Position);
			ResetShort();
		}
	}

	private void TryEnter(ICandleMessage candle)
	{
		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		// A bearish candle triggers a long contrarian entry.
		if (open > close)
		{
			if (Position < 0m)
			{
				BuyMarket(-Position);
				ResetShort();
				return;
			}

			if (Position == 0m && TradeVolume > 0m)
				BuyMarket(TradeVolume);

			return;
		}

		// A bullish candle signals a short entry.
		if (open < close)
		{
			if (Position > 0m)
			{
				SellMarket(Position);
				ResetLong();
				return;
			}

			if (Position == 0m && TradeVolume > 0m)
				SellMarket(TradeVolume);
		}
	}

	private void UpdateEntrySnapshot()
	{
		if (Position > 0m)
		{
			var averagePrice = Position.AveragePrice;
			if (averagePrice > 0m)
			{
				_longEntryPrice = averagePrice;

				if (StopLossPips > 0m && _pipSize > 0m)
				{
					_longStopPrice ??= averagePrice - StopLossPips * _pipSize;
				}
				else if (TrailingStopPips <= 0m)
				{
					_longStopPrice = null;
				}

				if (TakeProfitPips > 0m && _pipSize > 0m)
				{
					_longTakeProfitPrice ??= averagePrice + TakeProfitPips * _pipSize;
				}
				else
				{
					_longTakeProfitPrice = null;
				}
			}
		}
		else
		{
			ResetLong();
		}

		if (Position < 0m)
		{
			var averagePrice = Position.AveragePrice;
			if (averagePrice > 0m)
			{
				_shortEntryPrice = averagePrice;

				if (StopLossPips > 0m && _pipSize > 0m)
				{
					_shortStopPrice ??= averagePrice + StopLossPips * _pipSize;
				}
				else if (TrailingStopPips <= 0m)
				{
					_shortStopPrice = null;
				}

				if (TakeProfitPips > 0m && _pipSize > 0m)
				{
					_shortTakeProfitPrice ??= averagePrice - TakeProfitPips * _pipSize;
				}
				else
				{
					_shortTakeProfitPrice = null;
				}
			}
		}
		else
		{
			ResetShort();
		}
	}

	private void ResetLong()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
	}

	private void ResetShort()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
	}

	private TimeSpan GetTimeFrame()
	{
		if (CandleType.Arg is not TimeSpan frame)
			throw new InvalidOperationException("The candle type must define a time frame.");

		if (frame <= TimeSpan.Zero)
			throw new InvalidOperationException("The candle time frame must be positive.");

		return frame;
	}

	private void UpdatePipSize()
	{
		var security = Security;
		if (security == null)
		{
			_pipSize = 0m;
			return;
		}

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			_pipSize = 0m;
			return;
		}

		var decimals = security.Decimals;
		var multiplier = decimals is 3 or 5 ? 10m : 1m;
		_pipSize = step * multiplier;
	}
}

