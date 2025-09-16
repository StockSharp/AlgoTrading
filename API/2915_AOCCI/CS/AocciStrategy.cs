using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Awesome Oscillator + CCI strategy with pivot and jump filters.
/// </summary>
public class AocciStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _signalCandleShift;
	private readonly StrategyParam<decimal> _bigJumpPips;
	private readonly StrategyParam<decimal> _doubleJumpPips;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;

	private AwesomeOscillator _ao;
	private CommodityChannelIndex _cci;

	private decimal? _lastAoValue;
	private readonly Queue<decimal> _cciValues = new();
	private int _maxCciValues;

	private readonly Queue<ICandleMessage> _recentCandles = new();
	private int _maxRecentCandles;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal _pipSize;
	private decimal? _lastHigherClose;

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// CCI indicator length.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Offset applied when reading the CCI values.
	/// </summary>
	public int SignalCandleShift
	{
		get => _signalCandleShift.Value;
		set => _signalCandleShift.Value = value;
	}

	/// <summary>
	/// Maximum allowed jump between consecutive opens in pips.
	/// </summary>
	public decimal BigJumpPips
	{
		get => _bigJumpPips.Value;
		set => _bigJumpPips.Value = value;
	}

	/// <summary>
	/// Maximum allowed jump between every second open in pips.
	/// </summary>
	public decimal DoubleJumpPips
	{
		get => _doubleJumpPips.Value;
		set => _doubleJumpPips.Value = value;
	}

	/// <summary>
	/// Working candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for confirmation.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AocciStrategy"/>.
	/// </summary>
	public AocciStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Base order volume", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Trailing step distance", "Risk");

		_cciPeriod = Param(nameof(CciPeriod), 55)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Commodity Channel Index period", "Indicators");

		_signalCandleShift = Param(nameof(SignalCandleShift), 0)
			.SetDisplay("Signal Candle Shift", "Offset for reading indicator values", "Logic");

		_bigJumpPips = Param(nameof(BigJumpPips), 100m)
			.SetNotNegative()
			.SetDisplay("Big Jump (pips)", "Maximum allowed consecutive open gap", "Filters");

		_doubleJumpPips = Param(nameof(DoubleJumpPips), 100m)
			.SetNotNegative()
			.SetDisplay("Double Jump (pips)", "Maximum allowed two-bar open gap", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Higher Candle", "Higher timeframe for confirmation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> new[] { (Security, CandleType), (Security, HigherCandleType) };

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ao = null;
		_cci = null;
		_lastAoValue = null;
		_cciValues.Clear();
		_maxCciValues = 0;
		_recentCandles.Clear();
		_maxRecentCandles = 0;
		ResetLongState();
		ResetShortState();
		_pipSize = 0m;
		_lastHigherClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		_pipSize = CalculatePipSize();

		_ao = new AwesomeOscillator();
		_cci = new CommodityChannelIndex { Length = CciPeriod };

		_maxCciValues = Math.Max(SignalCandleShift + 2, 2);
		_maxRecentCandles = Math.Max(SignalCandleShift + 2, 6);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ao, _cci, ProcessCandle)
			.Start();

		var higherSubscription = SubscribeCandles(HigherCandleType);
		higherSubscription
			.Bind(ProcessHigherCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ao);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessHigherCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Store the last closed higher timeframe candle for pivot confirmation.
		_lastHigherClose = candle.ClosePrice;
	}

	private void ProcessCandle(ICandleMessage candle, decimal aoValue, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Maintain sliding windows for candles and indicator values.
		UpdateRecentCandles(candle);
		UpdateCciQueue(cciValue);

		var closedPosition = HandleActivePositions(candle);

		if (_ao == null || _cci == null)
		{
			_lastAoValue = aoValue;
			return;
		}

		if (!_ao.IsFormed || !_cci.IsFormed)
		{
			_lastAoValue = aoValue;
			return;
		}

		if (_lastAoValue is null)
		{
			_lastAoValue = aoValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_lastAoValue = aoValue;
			return;
		}

		if (_cciValues.Count <= SignalCandleShift + 1)
		{
			_lastAoValue = aoValue;
			return;
		}

		if (_recentCandles.Count < 6)
		{
			_lastAoValue = aoValue;
			return;
		}

		if (!TryGetCciValue(SignalCandleShift, out var cciShift0) ||
		!TryGetCciValue(SignalCandleShift + 1, out var cciShift1))
		{
			_lastAoValue = aoValue;
			return;
		}

		if (!TryGetRecentCandle(SignalCandleShift + 1, out var pivotSource))
		{
			_lastAoValue = aoValue;
			return;
		}

		if (_lastHigherClose is null)
		{
			_lastAoValue = aoValue;
			return;
		}

		if (ShouldSkipDueToJumps())
		{
			_lastAoValue = aoValue;
			return;
		}

		if (closedPosition || Position != 0)
		{
			_lastAoValue = aoValue;
			return;
		}

		var pivot = (pivotSource.HighPrice + pivotSource.LowPrice + pivotSource.ClosePrice) / 3m;
		var aoPrev = _lastAoValue.Value;
		var higherClose = _lastHigherClose.Value;
		var price = candle.ClosePrice;

		// Long condition from original MQL logic.
		var openLong = aoValue > 0m && cciShift0 >= 0m && price > pivot &&
		(aoPrev < 0m || cciShift1 <= 0m || higherClose < pivot);

		// Short condition mirrors the original code (identical filters).
		var openShort = aoValue > 0m && cciShift0 >= 0m && price > pivot &&
		(aoPrev < 0m || cciShift1 <= 0m || higherClose < pivot);

		if (openLong)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
				_longEntryPrice = price;
				_longStop = StopLossPips > 0m ? price - StopLossPips * _pipSize : null;
				_longTake = TakeProfitPips > 0m ? price + TakeProfitPips * _pipSize : null;
				ResetShortState();
			}
		}
		else if (openShort)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
			{
				SellMarket(volume);
				_shortEntryPrice = price;
				_shortStop = StopLossPips > 0m ? price + StopLossPips * _pipSize : null;
				_shortTake = TakeProfitPips > 0m ? price - TakeProfitPips * _pipSize : null;
				ResetLongState();
			}
		}

		_lastAoValue = aoValue;
	}

	private bool HandleActivePositions(ICandleMessage candle)
	{
		var closed = false;

		if (Position > 0)
		{
			_longEntryPrice ??= candle.ClosePrice;
			UpdateTrailingForLong(candle);

			if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				closed = true;
			}
			else if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				closed = true;
			}
		}
		else if (Position < 0)
		{
			_shortEntryPrice ??= candle.ClosePrice;
			UpdateTrailingForShort(candle);

			if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				closed = true;
			}
			else if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				closed = true;
			}
		}
		else
		{
			ResetLongState();
			ResetShortState();
		}

		return closed;
	}

	private void UpdateTrailingForLong(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m || !_longEntryPrice.HasValue)
		return;

		var trailingStop = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;
		var price = candle.ClosePrice;
		var entry = _longEntryPrice.Value;

		if (price - entry > trailingStop + trailingStep)
		{
			var minimal = price - (trailingStop + trailingStep);
			if (!_longStop.HasValue || _longStop.Value < minimal)
			_longStop = price - trailingStop;
		}
	}

	private void UpdateTrailingForShort(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m || !_shortEntryPrice.HasValue)
		return;

		var trailingStop = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;
		var price = candle.ClosePrice;
		var entry = _shortEntryPrice.Value;

		if (entry - price > trailingStop + trailingStep)
		{
			var maximal = price + (trailingStop + trailingStep);
			if (!_shortStop.HasValue || _shortStop.Value > maximal)
			_shortStop = price + trailingStop;
		}
	}

	private void UpdateCciQueue(decimal value)
	{
		var required = Math.Max(SignalCandleShift + 2, 2);
		if (_maxCciValues != required)
		{
			_maxCciValues = required;
			while (_cciValues.Count > _maxCciValues)
			_cciValues.Dequeue();
		}

		_cciValues.Enqueue(value);
		while (_cciValues.Count > _maxCciValues)
		_cciValues.Dequeue();
	}

	private void UpdateRecentCandles(ICandleMessage candle)
	{
		var required = Math.Max(SignalCandleShift + 2, 6);
		if (_maxRecentCandles != required)
		{
			_maxRecentCandles = required;
			while (_recentCandles.Count > _maxRecentCandles)
			_recentCandles.Dequeue();
		}

		_recentCandles.Enqueue(candle);
		while (_recentCandles.Count > _maxRecentCandles)
		_recentCandles.Dequeue();
	}

	private bool TryGetCciValue(int shift, out decimal value)
	{
		value = 0m;
		if (shift < 0 || shift >= _cciValues.Count)
		return false;

		var targetIndex = _cciValues.Count - 1 - shift;
		var index = 0;
		foreach (var item in _cciValues)
		{
			if (index == targetIndex)
			{
				value = item;
				return true;
			}
			index++;
		}

		return false;
	}

	private bool TryGetRecentCandle(int shift, out ICandleMessage candle)
	{
		candle = null;
		if (shift < 0 || shift >= _recentCandles.Count)
		return false;

		var targetIndex = _recentCandles.Count - 1 - shift;
		var index = 0;
		foreach (var item in _recentCandles)
		{
			if (index == targetIndex)
			{
				candle = item;
				return candle != null;
			}
			index++;
		}

		return false;
	}

	private bool ShouldSkipDueToJumps()
	{
		if (_pipSize <= 0m)
		return false;

		var bigJump = BigJumpPips * _pipSize;
		var doubleJump = DoubleJumpPips * _pipSize;

		if (BigJumpPips > 0m)
		{
			if (Math.Abs(GetOpenDifference(0, 1)) >= bigJump ||
			Math.Abs(GetOpenDifference(1, 2)) >= bigJump ||
			Math.Abs(GetOpenDifference(2, 3)) >= bigJump ||
			Math.Abs(GetOpenDifference(3, 4)) >= bigJump ||
			Math.Abs(GetOpenDifference(4, 5)) >= bigJump)
			return true;
		}

		if (DoubleJumpPips > 0m)
		{
			if (Math.Abs(GetOpenDifference(0, 2)) >= doubleJump ||
			Math.Abs(GetOpenDifference(1, 3)) >= doubleJump ||
			Math.Abs(GetOpenDifference(2, 4)) >= doubleJump ||
			Math.Abs(GetOpenDifference(3, 5)) >= doubleJump)
			return true;
		}

		return false;
	}

	private decimal GetOpenDifference(int firstShift, int secondShift)
	{
		if (!TryGetRecentCandle(firstShift, out var first) ||
		!TryGetRecentCandle(secondShift, out var second))
		return 0m;

		return second.OpenPrice - first.OpenPrice;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
		priceStep = 1m;

		var decimals = GetDecimalPlaces(priceStep);
		var factor = decimals == 3 || decimals == 5 ? 10m : 1m;
		return priceStep * factor;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		value = Math.Abs(value);
		if (value == 0m)
		return 0;

		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}

	private void ResetLongState()
	{
		_longStop = null;
		_longTake = null;
		_longEntryPrice = null;
	}

	private void ResetShortState()
	{
		_shortStop = null;
		_shortTake = null;
		_shortEntryPrice = null;
	}
}
