using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Weekly rebound strategy that evaluates price gaps between the previous close and the open 24 bars ago.
/// </summary>
public class WeeklyReboundCorridorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _additionalBuyTakeProfitPoints;

	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _corridorPoints;
	private readonly StrategyParam<int> _tradeDayOfWeek;
	private readonly StrategyParam<DataType> _candleType;

	private ShiftBuffer _openShiftBuffer;
	private decimal _previousClose;
	private bool _hasPreviousClose;
	private bool _positionIsLong;
	private decimal _entryPrice;
	private decimal _takeProfitPrice;
	private decimal _stopLossPrice;
	private int _lastEntryYmd;
	private TimeZoneInfo _timeZone;

	/// <summary>
	/// Take-profit value expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss value expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Additional take-profit points added when buying.
	/// </summary>
	public decimal AdditionalBuyTakeProfitPoints
	{
		get => _additionalBuyTakeProfitPoints.Value;
		set => _additionalBuyTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trading volume used for entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Corridor threshold that must be exceeded by the gap.
	/// </summary>
	public decimal CorridorPoints
	{
		get => _corridorPoints.Value;
		set => _corridorPoints.Value = value;
	}

	/// <summary>
	/// Day of week for entries. 0 - Sunday, 6 - Saturday.
	/// </summary>
	public int TradeDayOfWeek
	{
		get => _tradeDayOfWeek.Value;
		set => _tradeDayOfWeek.Value = value;
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
	/// Initializes a new instance of the <see cref="WeeklyReboundCorridorStrategy"/> class.
	/// </summary>
	public WeeklyReboundCorridorStrategy()
	{
		_additionalBuyTakeProfitPoints = Param(nameof(AdditionalBuyTakeProfitPoints), 3m)
		.SetGreaterThanOrEqualTo(0m)
		.SetDisplay("Buy Bonus Take Profit", "Additional points added to long take profits", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0m, 10m, 0.5m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Take-profit size in points", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(5m, 30m, 5m);

		_stopLossPoints = Param(nameof(StopLossPoints), 49m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Stop-loss size in points", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(20m, 80m, 5m);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Trading volume for market entries", "General");

		_corridorPoints = Param(nameof(CorridorPoints), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Corridor", "Required gap between the previous close and the historical open", "Signal")
		.SetCanOptimize(true)
		.SetOptimize(5m, 25m, 1m);

		_tradeDayOfWeek = Param(nameof(TradeDayOfWeek), 5)
		.SetDisplay("Trade Day", "Day of week when the setup is active (0=Sunday)", "Signal")
		.SetCanOptimize(true)
		.SetOptimize(0, 6, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle type for price analysis", "General");
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

		_openShiftBuffer = null;
		_previousClose = 0m;
		_hasPreviousClose = false;
		_positionIsLong = false;
		_entryPrice = 0m;
		_takeProfitPrice = 0m;
		_stopLossPrice = 0m;
		_lastEntryYmd = 0;
		_timeZone = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_openShiftBuffer = new ShiftBuffer(24);

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_openShiftBuffer is null)
		return;

		var currentClose = candle.ClosePrice;
		var localTime = ConvertToLocalTime(candle.OpenTime.UtcDateTime);

		HandleActivePosition(candle, localTime);

		if (Position != 0)
		return;

		if (_entryPrice != 0m)
		ResetPositionState();

		if (!_openShiftBuffer.TryGetValue(candle.OpenPrice, out var openShifted))
		{
			_previousClose = currentClose;
			return;
		}

		if (!_hasPreviousClose)
		{
			_previousClose = currentClose;
			_hasPreviousClose = true;
			return;
		}

		var corridorValue = GetPriceOffset(CorridorPoints);

		if (corridorValue <= 0m)
		{
			_previousClose = currentClose;
			return;
		}

		var opCl = openShifted - _previousClose;
		var clOp = _previousClose - openShifted;

		var ymd = localTime.Year * 10000 + localTime.Month * 100 + localTime.Day;

		if (ymd == _lastEntryYmd)
		{
			_previousClose = currentClose;
			return;
		}

		if ((int)localTime.DayOfWeek != TradeDayOfWeek)
		{
			_previousClose = currentClose;
			return;
		}

		if (localTime.Hour == 0 && localTime.Minute <= 3)
		{
			var volume = GetAdjustedVolume(TradeVolume);

			if (volume <= 0m)
			{
				_previousClose = currentClose;
				return;
			}

			if (opCl > corridorValue)
			{
				BuyMarket(volume);
				_positionIsLong = true;
				_entryPrice = candle.OpenPrice;
				_takeProfitPrice = candle.OpenPrice + GetPriceOffset(TakeProfitPoints + AdditionalBuyTakeProfitPoints);
				_stopLossPrice = candle.OpenPrice - GetPriceOffset(StopLossPoints);
				_lastEntryYmd = ymd;
				LogInfo($"Opened long position at {_entryPrice} with TP {_takeProfitPrice} and SL {_stopLossPrice}.");
			}
			else if (clOp > corridorValue)
			{
				SellMarket(volume);
				_positionIsLong = false;
				_entryPrice = candle.OpenPrice;
				_takeProfitPrice = candle.OpenPrice - GetPriceOffset(TakeProfitPoints);
				_stopLossPrice = candle.OpenPrice + GetPriceOffset(StopLossPoints);
				_lastEntryYmd = ymd;
				LogInfo($"Opened short position at {_entryPrice} with TP {_takeProfitPrice} and SL {_stopLossPrice}.");
			}
		}

		_previousClose = currentClose;
	}

	private void HandleActivePosition(ICandleMessage candle, DateTime localTime)
	{
		if (Position == 0)
		return;

		if (_takeProfitPrice == 0m || _stopLossPrice == 0m)
		return;

		if (_positionIsLong)
		{
			if (candle.LowPrice <= _stopLossPrice)
			{
				ClosePosition();
				LogInfo($"Long stop-loss triggered at {_stopLossPrice}.");
				ResetPositionState();
				return;
			}

			if (candle.HighPrice >= _takeProfitPrice)
			{
				ClosePosition();
				LogInfo($"Long take-profit triggered at {_takeProfitPrice}.");
				ResetPositionState();
				return;
			}
		}
		else
		{
			if (candle.HighPrice >= _stopLossPrice)
			{
				ClosePosition();
				LogInfo($"Short stop-loss triggered at {_stopLossPrice}.");
				ResetPositionState();
				return;
			}

			if (candle.LowPrice <= _takeProfitPrice)
			{
				ClosePosition();
				LogInfo($"Short take-profit triggered at {_takeProfitPrice}.");
				ResetPositionState();
				return;
			}
		}

		if (localTime.Hour == 22 && localTime.Minute >= 45)
		{
			ClosePosition();
			LogInfo("Closed position due to end-of-day exit rule.");
			ResetPositionState();
		}
	}

	private void ResetPositionState()
	{
		_positionIsLong = false;
		_entryPrice = 0m;
		_takeProfitPrice = 0m;
		_stopLossPrice = 0m;
	}

	private decimal GetAdjustedVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var step = Security?.VolumeStep ?? 0m;

		if (step <= 0m)
		return volume;

		var steps = Math.Max(1m, Math.Round(volume / step));
		return steps * step;
	}

	private decimal GetPriceOffset(decimal points)
	{
		var pointSize = GetPointSize();
		return pointSize > 0m ? points * pointSize : 0m;
	}

	private decimal GetPointSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		return priceStep > 0m ? priceStep : 0m;
	}

	private DateTime ConvertToLocalTime(DateTime utcTime)
	{
		_timeZone ??= Security?.Board?.TimeZone ?? TimeZoneInfo.Utc;
		return TimeZoneInfo.ConvertTimeFromUtc(utcTime, _timeZone);
	}

	private sealed class ShiftBuffer
	{
		private readonly decimal[] _buffer;
		private int _index;
		private int _count;
		private readonly int _shift;

		public ShiftBuffer(int shift)
		{
			_shift = Math.Max(0, shift);
			_buffer = new decimal[_shift + 1];
		}

		public bool TryGetValue(decimal value, out decimal shifted)
		{
			_buffer[_index] = value;

			if (_count < _buffer.Length)
			_count++;

			_index++;

			if (_index >= _buffer.Length)
			_index = 0;

			if (_count > _shift)
			{
				var idx = _index - 1 - _shift;

				if (idx < 0)
				idx += _buffer.Length;

				shifted = _buffer[idx];
				return true;
			}

			if (_shift == 0 && _count > 0)
			{
				shifted = value;
				return true;
			}

			shifted = 0m;
			return false;
		}
	}
}
