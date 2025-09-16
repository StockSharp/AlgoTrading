
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple screen style daily breakout strategy using MACD slope and Williams %R.
/// Places stop orders beyond the prior day's extremes and manages trailing exits.
/// </summary>
public class TdsGlobalStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<int> _williamsLength;
	private readonly StrategyParam<decimal> _williamsBuyLevel;
	private readonly StrategyParam<decimal> _williamsSellLevel;
	private readonly StrategyParam<int> _entryBufferSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _trailingStopSteps;
	private readonly StrategyParam<bool> _useSymbolStagger;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _priceStep;

	private decimal? _macdPrev1;
	private decimal? _macdPrev2;
	private decimal? _williamsPrev1;
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevClose;

	private bool _hasPendingOrder;
	private Sides? _pendingSide;
	private decimal? _pendingEntryPrice;
	private decimal? _pendingStopPrice;
	private decimal? _pendingTakePrice;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Fast EMA period used by MACD.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period used by MACD.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal period for MACD.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Williams %R lookback length.
	/// </summary>
	public int WilliamsLength
	{
		get => _williamsLength.Value;
		set => _williamsLength.Value = value;
	}

	/// <summary>
	/// Oversold threshold for Williams %R.
	/// </summary>
	public decimal WilliamsBuyLevel
	{
		get => _williamsBuyLevel.Value;
		set => _williamsBuyLevel.Value = value;
	}

	/// <summary>
	/// Overbought threshold for Williams %R.
	/// </summary>
	public decimal WilliamsSellLevel
	{
		get => _williamsSellLevel.Value;
		set => _williamsSellLevel.Value = value;
	}

	/// <summary>
	/// Minimum distance from market price when placing stop entries (in steps).
	/// </summary>
	public int EntryBufferSteps
	{
		get => _entryBufferSteps.Value;
		set => _entryBufferSteps.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public decimal TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
	}

	/// <summary>
	/// Enable symbol specific minute windows for order placement.
	/// </summary>
	public bool UseSymbolStagger
	{
		get => _useSymbolStagger.Value;
		set => _useSymbolStagger.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations (defaults to daily candles).
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TdsGlobalStrategy"/> class.
	/// </summary>
	public TdsGlobalStrategy()
	{
		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length", "MACD");

		_macdSlowLength = Param(nameof(MacdSlowLength), 23)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length", "MACD");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line length", "MACD");

		_williamsLength = Param(nameof(WilliamsLength), 24)
			.SetGreaterThanZero()
			.SetDisplay("Williams Length", "%R lookback", "Filters");

		_williamsBuyLevel = Param(nameof(WilliamsBuyLevel), -75m)
			.SetDisplay("Williams Buy", "Oversold threshold", "Filters");

		_williamsSellLevel = Param(nameof(WilliamsSellLevel), -25m)
			.SetDisplay("Williams Sell", "Overbought threshold", "Filters");

		_entryBufferSteps = Param(nameof(EntryBufferSteps), 16)
			.SetGreaterThanZero()
			.SetDisplay("Entry Buffer", "Minimum distance from market in steps", "Risk");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 999m)
			.SetDisplay("Take Profit Steps", "Target distance in price steps", "Risk");

		_trailingStopSteps = Param(nameof(TrailingStopSteps), 10m)
			.SetDisplay("Trailing Stop Steps", "Trailing stop distance in steps", "Risk");

		_useSymbolStagger = Param(nameof(UseSymbolStagger), false)
			.SetDisplay("Use Time Windows", "Apply symbol specific minute windows", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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

		_priceStep = 0m;

		_macdPrev1 = null;
		_macdPrev2 = null;
		_williamsPrev1 = null;
		_prevHigh = null;
		_prevLow = null;
		_prevClose = null;

		ResetPendingOrder();
		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;
		if (_priceStep <= 0)
			_priceStep = 1m;

		var macd = new MACD
		{
			ShortPeriod = MacdFastLength,
			LongPeriod = MacdSlowLength,
			SignalPeriod = MacdSignalLength
		};

		var williams = new WilliamsR { Length = WilliamsLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, williams, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdLine, decimal signalLine, decimal williams)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateHistory(macdLine, williams, candle);
			return;
		}

		ManageOpenPosition(candle);

		if (Math.Abs(Position) > 0)
		{
			UpdateHistory(macdLine, williams, candle);
			return;
		}

		if (_macdPrev1 is null || _macdPrev2 is null || _williamsPrev1 is null ||
			_prevHigh is null || _prevLow is null || _prevClose is null)
		{
			UpdateHistory(macdLine, williams, candle);
			return;
		}

		if (UseSymbolStagger && !IsWithinAllowedWindow(candle.CloseTime))
		{
			UpdateHistory(macdLine, williams, candle);
			return;
		}

		var direction = _macdPrev1 > _macdPrev2 ? 1 : _macdPrev1 < _macdPrev2 ? -1 : 0;
		var oversold = _williamsPrev1 <= WilliamsBuyLevel;
		var overbought = _williamsPrev1 >= WilliamsSellLevel;

		var volume = Volume;
		if (volume <= 0)
			volume = 1m;

		if (direction > 0 && oversold)
		{
			PlaceBuyStop(volume);
		}
		else if (direction < 0 && overbought)
		{
			PlaceSellStop(volume);
		}
		else if (_hasPendingOrder)
		{
			if ((_pendingSide == Sides.Buy && direction < 0) ||
				(_pendingSide == Sides.Sell && direction > 0))
			{
				CancelActiveOrders();
				ResetPendingOrder();
			}
		}

		UpdateHistory(macdLine, williams, candle);
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		var position = Position;

		if (position > 0)
		{
			if (_entryPrice is null)
			{
				_entryPrice = _pendingEntryPrice ?? candle.ClosePrice;
				_stopPrice = _pendingStopPrice;
				_takePrice = _pendingTakePrice;
				ResetPendingOrder();
			}

			if (TrailingStopSteps > 0)
			{
				var trailing = candle.ClosePrice - TrailingStopSteps * _priceStep;
				if (_stopPrice is null || trailing > _stopPrice)
					_stopPrice = trailing;
			}

			if (_stopPrice is not null && candle.LowPrice <= _stopPrice)
			{
				ClosePosition();
				ResetPositionState();
				ResetPendingOrder();
				return;
			}

			if (_takePrice is not null && candle.HighPrice >= _takePrice)
			{
				ClosePosition();
				ResetPositionState();
				ResetPendingOrder();
			}
		}
		else if (position < 0)
		{
			if (_entryPrice is null)
			{
				_entryPrice = _pendingEntryPrice ?? candle.ClosePrice;
				_stopPrice = _pendingStopPrice;
				_takePrice = _pendingTakePrice;
				ResetPendingOrder();
			}

			if (TrailingStopSteps > 0)
			{
				var trailing = candle.ClosePrice + TrailingStopSteps * _priceStep;
				if (_stopPrice is null || trailing < _stopPrice)
					_stopPrice = trailing;
			}

			if (_stopPrice is not null && candle.HighPrice >= _stopPrice)
			{
				ClosePosition();
				ResetPositionState();
				ResetPendingOrder();
				return;
			}

			if (_takePrice is not null && candle.LowPrice <= _takePrice)
			{
				ClosePosition();
				ResetPositionState();
				ResetPendingOrder();
			}
		}
		else
		{
			ResetPositionState();
		}
	}

	private void PlaceBuyStop(decimal volume)
	{
		if (_prevHigh is null || _prevLow is null || _prevClose is null)
			return;

		var entryPrice = Math.Max(_prevHigh.Value + _priceStep, _prevClose.Value + EntryBufferSteps * _priceStep);
		var stopPrice = _prevLow.Value - _priceStep;
		var takePrice = TakeProfitSteps > 0 ? entryPrice + TakeProfitSteps * _priceStep : (decimal?)null;

		if (_hasPendingOrder && _pendingSide == Sides.Buy &&
			_pendingEntryPrice == entryPrice &&
			_pendingStopPrice == stopPrice &&
			_pendingTakePrice == takePrice)
		{
			return;
		}

		CancelActiveOrders();
		BuyStop(volume, entryPrice);

		_pendingSide = Sides.Buy;
		_pendingEntryPrice = entryPrice;
		_pendingStopPrice = stopPrice;
		_pendingTakePrice = takePrice;
		_hasPendingOrder = true;
	}

	private void PlaceSellStop(decimal volume)
	{
		if (_prevHigh is null || _prevLow is null || _prevClose is null)
			return;

		var entryPrice = Math.Min(_prevLow.Value - _priceStep, _prevClose.Value - EntryBufferSteps * _priceStep);
		var stopPrice = _prevHigh.Value + _priceStep;
		var takePrice = TakeProfitSteps > 0 ? entryPrice - TakeProfitSteps * _priceStep : (decimal?)null;

		if (_hasPendingOrder && _pendingSide == Sides.Sell &&
			_pendingEntryPrice == entryPrice &&
			_pendingStopPrice == stopPrice &&
			_pendingTakePrice == takePrice)
		{
			return;
		}

		CancelActiveOrders();
		SellStop(volume, entryPrice);

		_pendingSide = Sides.Sell;
		_pendingEntryPrice = entryPrice;
		_pendingStopPrice = stopPrice;
		_pendingTakePrice = takePrice;
		_hasPendingOrder = true;
	}

	private void CancelActiveOrders()
	{
		foreach (var order in Orders)
		{
			if (order.State.IsActive())
				CancelOrder(order);
		}
	}

	private void ResetPendingOrder()
	{
		_hasPendingOrder = false;
		_pendingSide = null;
		_pendingEntryPrice = null;
		_pendingStopPrice = null;
		_pendingTakePrice = null;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private void UpdateHistory(decimal macdLine, decimal williams, ICandleMessage candle)
	{
		if (_macdPrev1 is not null)
			_macdPrev2 = _macdPrev1;

		_macdPrev1 = macdLine;
		_williamsPrev1 = williams;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = candle.ClosePrice;
	}

	private bool IsWithinAllowedWindow(DateTimeOffset time)
	{
		if (!UseSymbolStagger)
			return true;

		var minute = time.Minute;
		var code = Security?.Code?.ToUpperInvariant();

		switch (code)
		{
			case "USDCHF":
				return (minute >= 0 && minute <= 1) || (minute >= 8 && minute <= 9) ||
					(minute >= 16 && minute <= 17) || (minute >= 24 && minute <= 25) ||
					(minute >= 32 && minute <= 33) || (minute >= 40 && minute <= 41) ||
					(minute >= 48 && minute <= 49);

			case "GBPUSD":
				return (minute >= 2 && minute <= 3) || (minute >= 10 && minute <= 11) ||
					(minute >= 18 && minute <= 19) || (minute >= 26 && minute <= 27) ||
					(minute >= 34 && minute <= 35) || (minute >= 42 && minute <= 43) ||
					(minute >= 50 && minute <= 51);

			case "USDJPY":
				return (minute >= 4 && minute <= 5) || (minute >= 12 && minute <= 13) ||
					(minute >= 20 && minute <= 21) || (minute >= 28 && minute <= 29) ||
					(minute >= 36 && minute <= 37) || (minute >= 44 && minute <= 45) ||
					(minute >= 52 && minute <= 53);

			case "EURUSD":
				return (minute >= 6 && minute <= 7) || (minute >= 14 && minute <= 15) ||
					(minute >= 22 && minute <= 23) || (minute >= 30 && minute <= 31) ||
					(minute >= 38 && minute <= 39) || (minute >= 46 && minute <= 47) ||
					(minute >= 54 && minute <= 59);

			default:
				return true;
		}
	}
}
