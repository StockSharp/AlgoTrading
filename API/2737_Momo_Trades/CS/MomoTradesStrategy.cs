using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum strategy based on MACD momentum and distance from SMA similar to the original MQL logic.
/// </summary>
public class MomoTradesStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _maBarShift;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _macdBarShift;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _breakevenPips;
	private readonly StrategyParam<decimal> _priceShiftPips;
	private readonly StrategyParam<bool> _closeEndDay;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _sma;
	private MACD _macd;
	// Indicators follow the same configuration as in the MQL script.

	private readonly decimal[] _macdHistory = new decimal[64];
	private readonly decimal[] _maHistory = new decimal[64];
	private readonly decimal[] _closeHistory = new decimal[64];
	// Buffers store the recent history required for shifted indicator access.

	private int _macdCount;
	private int _maCount;
	private int _closeCount;

	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal? _breakevenTrigger;
	private decimal? _trailingDistance;
	private decimal? _trailingStep;
	private bool _isLongPosition;
	// Position management state persists between candles.

	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	public int MaBarShift
	{
		get => _maBarShift.Value;
		set => _maBarShift.Value = value;
	}

	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	public int MacdBarShift
	{
		get => _macdBarShift.Value;
		set => _macdBarShift.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	public decimal BreakevenPips
	{
		get => _breakevenPips.Value;
		set => _breakevenPips.Value = value;
	}

	public decimal PriceShiftPips
	{
		get => _priceShiftPips.Value;
		set => _priceShiftPips.Value = value;
	}

	public bool CloseEndDay
	{
		get => _closeEndDay.Value;
		set => _closeEndDay.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public MomoTradesStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 22).SetGreaterThanZero().SetDisplay("SMA Period", "Period of the moving average", "Indicators");
		_maBarShift = Param(nameof(MaBarShift), 6).SetGreaterOrEqualZero().SetDisplay("MA Bar Shift", "Bar shift used for SMA comparison", "Indicators");
		_macdFast = Param(nameof(MacdFast), 12).SetGreaterThanZero().SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators");
		_macdSlow = Param(nameof(MacdSlow), 26).SetGreaterThanZero().SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicators");
		_macdSignal = Param(nameof(MacdSignal), 9).SetGreaterThanZero().SetDisplay("MACD Signal", "Signal SMA period for MACD", "Indicators");
		_macdBarShift = Param(nameof(MacdBarShift), 2).SetGreaterOrEqualZero().SetDisplay("MACD Bar Shift", "Offset applied to MACD values", "Indicators");
		_stopLossPips = Param(nameof(StopLossPips), 25m).SetGreaterOrEqualZero().SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk");
		_takeProfitPips = Param(nameof(TakeProfitPips), 0m).SetGreaterOrEqualZero().SetDisplay("Take Profit", "Take profit distance in pips", "Risk");
		_trailingStopPips = Param(nameof(TrailingStopPips), 0m).SetGreaterOrEqualZero().SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk");
		_trailingStepPips = Param(nameof(TrailingStepPips), 5m).SetGreaterOrEqualZero().SetDisplay("Trailing Step", "Trailing step distance in pips", "Risk");
		_breakevenPips = Param(nameof(BreakevenPips), 10m).SetGreaterOrEqualZero().SetDisplay("Breakeven", "Distance to move stop to breakeven", "Risk");
		_priceShiftPips = Param(nameof(PriceShiftPips), 5m).SetGreaterOrEqualZero().SetDisplay("Price Shift", "Required price distance from SMA", "Filters");
		_closeEndDay = Param(nameof(CloseEndDay), true).SetDisplay("Close End Of Day", "Close positions near session end", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Source candles for calculations", "General");

		Volume = 1m;
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

		Array.Clear(_macdHistory, 0, _macdHistory.Length);
		Array.Clear(_maHistory, 0, _maHistory.Length);
		Array.Clear(_closeHistory, 0, _closeHistory.Length);

		_macdCount = 0;
		_maCount = 0;
		_closeCount = 0;

		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
		_breakevenTrigger = null;
		_trailingDistance = null;
		_trailingStep = null;
		_isLongPosition = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma = new SMA { Length = SmaPeriod };
		_macd = new MACD { ShortPeriod = MacdFast, LongPeriod = MacdSlow, SignalPeriod = MacdSignal };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_sma, _macd, ProcessCandle).Start();
	}

	// Process each finished candle to evaluate entries with indicator filters.
	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal macdValue, decimal macdSignal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma.IsFormed || !_macd.IsFormed)
			return;

		PushValue(_closeHistory, ref _closeCount, candle.ClosePrice);
		PushValue(_maHistory, ref _maCount, smaValue);
		PushValue(_macdHistory, ref _macdCount, macdValue);
		// Cache the latest values so shifted lookups mimic the MQL buffer usage.

		ManageActivePosition(candle);

		if (Position != 0)
			return;

		if (CloseEndDay && ShouldCloseForDay(candle))
			return;

		if (!TryGetHistoryValue(_closeHistory, _closeCount, MaBarShift, out var shiftedClose))
			return;

		if (!TryGetHistoryValue(_maHistory, _maCount, MaBarShift, out var shiftedMa))
			return;

		var priceShift = GetPipValue(PriceShiftPips);

		var emaBuy = shiftedClose - shiftedMa > priceShift;
		var emaSell = shiftedMa - shiftedClose > priceShift;

		var macdBuy = CheckMacdPattern(true);
		var macdSell = CheckMacdPattern(false);

		if (macdBuy && emaBuy)
		{
			EnterLong(candle.ClosePrice);
		}
		else if (macdSell && emaSell)
		{
			EnterShort(candle.ClosePrice);
		}
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position == 0)
		{
			return;
		}

		if (CloseEndDay && ShouldCloseForDay(candle))
		{
			ClosePosition();
			return;
		}

		var close = candle.ClosePrice;
		// Adjust stop levels according to trailing or breakeven rules before exits.

		if (_trailingDistance.HasValue && _trailingStep.HasValue)
		{
			if (_isLongPosition)
			{
				if (close - _entryPrice > _trailingDistance.Value + _trailingStep.Value)
				{
					var newStop = close - _trailingDistance.Value;
					if (!_stopPrice.HasValue || newStop > _stopPrice.Value)
						_stopPrice = newStop;
				}
			}
			else
			{
				if (_entryPrice - close > _trailingDistance.Value + _trailingStep.Value)
				{
					var newStop = close + _trailingDistance.Value;
					if (!_stopPrice.HasValue || newStop < _stopPrice.Value)
						_stopPrice = newStop;
				}
			}
		}
		else if (_breakevenTrigger.HasValue)
		{
			if (_isLongPosition)
			{
				if (close > _breakevenTrigger.Value)
				{
					_stopPrice = _entryPrice;
					_breakevenTrigger = null;
				}
			}
			else
			{
				if (close < _breakevenTrigger.Value)
				{
					_stopPrice = _entryPrice;
					_breakevenTrigger = null;
				}
			}
		}

		if (_isLongPosition)
		{
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}
		else
		{
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}
	}

	private void EnterLong(decimal price)
	// Configure protective levels immediately after a long entry.
	{
		BuyMarket(Volume);

		_entryPrice = price;
		_isLongPosition = true;

		var stop = GetPipValue(StopLossPips);
		var take = GetPipValue(TakeProfitPips);
		var trail = GetPipValue(TrailingStopPips);
		var step = GetPipValue(TrailingStepPips);
		var breakeven = GetPipValue(BreakevenPips);

		_stopPrice = StopLossPips > 0m ? price - stop : null;
		_takePrice = TakeProfitPips > 0m ? price + take : null;

		if (TakeProfitPips <= 0m && BreakevenPips > 0m)
			_breakevenTrigger = price + breakeven;
		else
			_breakevenTrigger = null;

		if (TakeProfitPips > 0m && TrailingStopPips > 0m && TrailingStepPips > 0m)
		{
			_trailingDistance = trail;
			_trailingStep = step;
		}
		else
		{
			_trailingDistance = null;
			_trailingStep = null;
		}
	}

	private void EnterShort(decimal price)
	// Configure protective levels immediately after a short entry.
	{
		SellMarket(Volume);

		_entryPrice = price;
		_isLongPosition = false;

		var stop = GetPipValue(StopLossPips);
		var take = GetPipValue(TakeProfitPips);
		var trail = GetPipValue(TrailingStopPips);
		var step = GetPipValue(TrailingStepPips);
		var breakeven = GetPipValue(BreakevenPips);

		_stopPrice = StopLossPips > 0m ? price + stop : null;
		_takePrice = TakeProfitPips > 0m ? price - take : null;

		if (TakeProfitPips <= 0m && BreakevenPips > 0m)
			_breakevenTrigger = price - breakeven;
		else
			_breakevenTrigger = null;

		if (TakeProfitPips > 0m && TrailingStopPips > 0m && TrailingStepPips > 0m)
		{
			_trailingDistance = trail;
			_trailingStep = step;
		}
		else
		{
			_trailingDistance = null;
			_trailingStep = null;
		}
	}

	private bool CheckMacdPattern(bool isLong)
	// MACD momentum pattern replicates the original conditional cascade.
	{
		var baseIndex = MacdBarShift;
		var required = baseIndex + 8;

		if (_macdCount <= required)
			return false;

		var v3 = _macdHistory[baseIndex + 3];
		var v4 = _macdHistory[baseIndex + 4];
		var v5 = _macdHistory[baseIndex + 5];
		var v6 = _macdHistory[baseIndex + 6];
		var v7 = _macdHistory[baseIndex + 7];
		var v8 = _macdHistory[baseIndex + 8];

		if (isLong)
			return v3 > v4 && v4 > v5 && v5 >= 0m && v6 <= 0m && v6 > v7 && v7 > v8;

		return v3 < v4 && v4 < v5 && v5 <= 0m && v6 >= 0m && v6 < v7 && v7 < v8;
	}

	private void PushValue(decimal[] buffer, ref int count, decimal value)
	{
		if (count < buffer.Length)
			count += 1;

		for (var i = count - 1; i > 0; i--)
		{
			buffer[i] = buffer[i - 1];
		}

		buffer[0] = value;
	}

	private bool TryGetHistoryValue(decimal[] buffer, int count, int index, out decimal value)
	{
		if (index < 0 || index >= count)
		{
			value = 0m;
			return false;
		}

		value = buffer[index];
		return true;
	}

	private decimal GetPipValue(decimal pips)
	// Convert pip-based settings into price units using the instrument step.
	{
		var step = Security?.PriceStep ?? 0.0001m;
		return pips * step * 10m;
	}

	private bool ShouldCloseForDay(ICandleMessage candle)
	{
		var time = candle.CloseTime.UtcDateTime;
		var endHour = time.DayOfWeek == DayOfWeek.Friday ? 21 : 23;
		return time.Hour >= endHour;
	}

	private void ClosePosition()
	{
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		ResetPositionState();
	}

	private void ResetPositionState()
	// Reset cached trading state once the position has been closed.
	{
		_stopPrice = null;
		_takePrice = null;
		_breakevenTrigger = null;
		_trailingDistance = null;
		_trailingStep = null;
		_isLongPosition = false;
		_entryPrice = 0m;
	}
}
