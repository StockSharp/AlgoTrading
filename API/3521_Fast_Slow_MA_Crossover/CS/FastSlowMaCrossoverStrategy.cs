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
/// Fast and slow moving average crossover strategy with intraday time filter and pip-based risk controls.
/// </summary>
public class FastSlowMaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _stopTime;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;

	private decimal _pipSize;
	private decimal? _previousFast;
	private decimal? _previousSlow;
	private DateTimeOffset? _lastSignalTime;
	private bool _hasActivePosition;
	private bool _isLongPosition;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="FastSlowMaCrossoverStrategy"/> class.
	/// </summary>
	public FastSlowMaCrossoverStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Length of the fast moving average", "Parameters")
			.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Length of the slow moving average", "Parameters")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 10)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Distance in pips for profit taking", "Risk Management")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 10)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Distance in pips for protective stop", "Risk Management")
			.SetCanOptimize(true);

		_startTime = Param(nameof(StartTime), new TimeSpan(0, 0, 0))
			.SetDisplay("Start Time", "Start of the allowed trading window", "Schedule");

		_stopTime = Param(nameof(StopTime), new TimeSpan(23, 59, 0))
			.SetDisplay("Stop Time", "End of the allowed trading window", "Schedule");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for calculations", "General");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Volume of each market order", "Trading")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Period of the fast moving average.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the slow moving average.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Profit target distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Start time of the allowed trading window.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Stop time of the allowed trading window.
	/// </summary>
	public TimeSpan StopTime
	{
		get => _stopTime.Value;
		set => _stopTime.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Volume used for each market order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
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

		_previousFast = null;
		_previousSlow = null;
		_lastSignalTime = null;
		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		_pipSize = CalculatePipSize();

		var fastMa = new EMA { Length = FastMaPeriod };
		var slowMa = new EMA { Length = SlowMaPeriod };

		SubscribeCandles(CandleType)
			.Bind(fastMa, slowMa, (candle, fastValue, slowValue) => ProcessCandle(candle, fastValue, slowValue, fastMa.IsFormed && slowMa.IsFormed))
			.Start();
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return 0.0001m;

		var decimals = Security?.Decimals ?? 0;
		var factor = (decimals == 3 || decimals == 5) ? 10m : 1m;
		return priceStep * factor;
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, bool indicatorsFormed)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var timeOfDay = candle.CloseTime.TimeOfDay;
		if (!IsWithinTradingWindow(timeOfDay))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ManageExistingPosition(candle);

		if (!indicatorsFormed)
		{
			_previousFast = fastValue;
			_previousSlow = slowValue;
			return;
		}

		if (_previousFast is null || _previousSlow is null)
		{
			_previousFast = fastValue;
			_previousSlow = slowValue;
			return;
		}

		var crossUp = _previousFast <= _previousSlow && fastValue > slowValue;
		var crossDown = _previousFast >= _previousSlow && fastValue < slowValue;

		if (_pipSize <= 0m)
		{
			_previousFast = fastValue;
			_previousSlow = slowValue;
			return;
		}

		var currentCandleTime = candle.CloseTime;

		if (crossUp && Position <= 0m && _lastSignalTime != currentCandleTime)
		{
			var volume = TradeVolume;

			if (Position < 0m)
				volume += -Position;

			if (volume > 0m)
			{
				BuyMarket(volume);
				RecordEntryState(candle.ClosePrice, true);
				_lastSignalTime = currentCandleTime;
			}
		}
		else if (crossDown && Position >= 0m && _lastSignalTime != currentCandleTime)
		{
			var volume = TradeVolume;

			if (Position > 0m)
				volume += Position;

			if (volume > 0m)
			{
				SellMarket(volume);
				RecordEntryState(candle.ClosePrice, false);
				_lastSignalTime = currentCandleTime;
			}
		}

		_previousFast = fastValue;
		_previousSlow = slowValue;
	}

	private void ManageExistingPosition(ICandleMessage candle)
	{
		if (!_hasActivePosition || _pipSize <= 0m)
			return;

		var high = candle.HighPrice;
		var low = candle.LowPrice;

		if (_isLongPosition)
		{
			if (StopLossPips > 0 && low <= _stopPrice)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (TakeProfitPips > 0 && high >= _targetPrice)
			{
				SellMarket(Position);
				ResetPositionState();
			}
		}
		else
		{
			if (StopLossPips > 0 && high >= _stopPrice)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return;
			}

			if (TakeProfitPips > 0 && low <= _targetPrice)
			{
				BuyMarket(-Position);
				ResetPositionState();
			}
		}
	}

	private void RecordEntryState(decimal closePrice, bool isLong)
	{
		_hasActivePosition = true;
		_isLongPosition = isLong;
		_entryPrice = closePrice;

		var takeOffset = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;
		var stopOffset = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;

		if (isLong)
		{
			_targetPrice = takeOffset > 0m ? NormalizePrice(_entryPrice + takeOffset) : 0m;
			_stopPrice = stopOffset > 0m ? NormalizePrice(_entryPrice - stopOffset) : 0m;
		}
		else
		{
			_targetPrice = takeOffset > 0m ? NormalizePrice(_entryPrice - takeOffset) : 0m;
			_stopPrice = stopOffset > 0m ? NormalizePrice(_entryPrice + stopOffset) : 0m;
		}
	}

	private bool IsWithinTradingWindow(TimeSpan current)
	{
		var start = StartTime;
		var stop = StopTime;

		if (start == stop)
			return true;

		if (start < stop)
			return current >= start && current <= stop;

		return current >= start || current <= stop;
	}

	private void ResetPositionState()
	{
		_hasActivePosition = false;
		_isLongPosition = false;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_targetPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetPositionState();
		}
		else
		{
			_hasActivePosition = true;
			_isLongPosition = Position > 0m;
		}
	}
}

