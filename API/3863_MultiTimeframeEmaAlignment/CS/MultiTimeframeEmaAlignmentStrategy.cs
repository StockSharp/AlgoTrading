using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi timeframe EMA alignment strategy converted from MQL 1h-4h-1d system.
/// </summary>
public class MultiTimeframeEmaAlignmentStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _shiftDepth;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<DataType> _m1CandleType;
	private readonly StrategyParam<DataType> _m5CandleType;
	private readonly StrategyParam<DataType> _m30CandleType;

	private ExponentialMovingAverage _m1Fast = null!;
	private ExponentialMovingAverage _m1Slow = null!;
	private ExponentialMovingAverage _m5Fast = null!;
	private ExponentialMovingAverage _m5Slow = null!;
	private ExponentialMovingAverage _m30Fast = null!;
	private ExponentialMovingAverage _m30Slow = null!;

	private ValueBuffer _m1FastValues = null!;
	private ValueBuffer _m1SlowValues = null!;
	private ValueBuffer _m5FastValues = null!;
	private ValueBuffer _m5SlowValues = null!;
	private ValueBuffer _m30FastValues = null!;
	private ValueBuffer _m30SlowValues = null!;

	private decimal _priceStep;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	public MultiTimeframeEmaAlignmentStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Default trade volume", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_fastLength = Param(nameof(FastLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_slowLength = Param(nameof(SlowLength), 64)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(40, 80, 4);

		_shiftDepth = Param(nameof(ShiftDepth), 3)
			.SetGreaterThanZero()
			.SetDisplay("Shift Depth", "Number of candles for slope checks", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 5, 1);

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Enable fixed stop loss", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 75m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(30m, 150m, 10m);

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Enable fixed take profit", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(60m, 200m, 10m);

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 60m, 5m);

		_m1CandleType = Param(nameof(M1CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("M1 Candles", "Lower timeframe for signals", "Data");

		_m5CandleType = Param(nameof(M5CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("M5 Candles", "Middle timeframe for confirmation", "Data");

		_m30CandleType = Param(nameof(M30CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("M30 Candles", "Higher timeframe for confirmation", "Data");
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public int ShiftDepth
	{
		get => _shiftDepth.Value;
		set => _shiftDepth.Value = value;
	}

	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public DataType M1CandleType
	{
		get => _m1CandleType.Value;
		set => _m1CandleType.Value = value;
	}

	public DataType M5CandleType
	{
		get => _m5CandleType.Value;
		set => _m5CandleType.Value = value;
	}

	public DataType M30CandleType
	{
		get => _m30CandleType.Value;
		set => _m30CandleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		{
			_priceStep = 0.0001m;
		}

		var depth = Math.Max(ShiftDepth, 1);
		var bufferLength = depth + 1;

		_m1Fast = new ExponentialMovingAverage { Length = FastLength };
		_m1Slow = new ExponentialMovingAverage { Length = SlowLength };
		_m5Fast = new ExponentialMovingAverage { Length = FastLength };
		_m5Slow = new ExponentialMovingAverage { Length = SlowLength };
		_m30Fast = new ExponentialMovingAverage { Length = FastLength };
		_m30Slow = new ExponentialMovingAverage { Length = SlowLength };

		_m1FastValues = new ValueBuffer(bufferLength);
		_m1SlowValues = new ValueBuffer(bufferLength);
		_m5FastValues = new ValueBuffer(bufferLength);
		_m5SlowValues = new ValueBuffer(bufferLength);
		_m30FastValues = new ValueBuffer(bufferLength);
		_m30SlowValues = new ValueBuffer(bufferLength);

		var m1Subscription = SubscribeCandles(M1CandleType);
		m1Subscription.Bind(_m1Fast, _m1Slow, ProcessM1Candle).Start();

		var m5Subscription = SubscribeCandles(M5CandleType);
		m5Subscription.Bind(_m5Fast, _m5Slow, ProcessM5Candle).Start();

		var m30Subscription = SubscribeCandles(M30CandleType);
		m30Subscription.Bind(_m30Fast, _m30Slow, ProcessM30Candle).Start();

		StartProtection();
	}

	private void ProcessM30Candle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		_m30FastValues.Push(fastValue);
		_m30SlowValues.Push(slowValue);
	}

	private void ProcessM5Candle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		_m5FastValues.Push(fastValue);
		_m5SlowValues.Push(slowValue);
	}

	private void ProcessM1Candle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		_m1FastValues.Push(fastValue);
		_m1SlowValues.Push(slowValue);

		TryProcessSignals(candle);
	}

	private void TryProcessSignals(ICandleMessage candle)
	{
		if (!HasData(_m1FastValues) || !HasData(_m1SlowValues) ||
			!HasData(_m5FastValues) || !HasData(_m5SlowValues) ||
			!HasData(_m30FastValues) || !HasData(_m30SlowValues))
		{
			return;
		}

		if (ManageOpenPosition(candle))
		{
			return;
		}

		var bullish = IsBullTrend(_m1FastValues, _m1SlowValues) &&
			IsBullTrend(_m5FastValues, _m5SlowValues) &&
			IsBullTrend(_m30FastValues, _m30SlowValues);

		var bearish = IsBearTrend(_m1FastValues, _m1SlowValues) &&
			IsBearTrend(_m5FastValues, _m5SlowValues) &&
			IsBearTrend(_m30FastValues, _m30SlowValues);

		if (bullish && Position <= 0)
		{
			BuyMarket();
			RegisterRiskLevels(candle.ClosePrice, true);
			return;
		}

		if (bearish && Position >= 0)
		{
			SellMarket();
			RegisterRiskLevels(candle.ClosePrice, false);
		}
	}

	private bool ManageOpenPosition(ICandleMessage candle)
	{
		if (Position == 0)
		{
			_entryPrice = null;
			_stopPrice = null;
			_takePrice = null;
			return false;
		}

		var closePrice = candle.ClosePrice;
		var trailingDistance = TrailingStopPips * _priceStep;

		if (Position > 0)
		{
			if (UseStopLoss && _stopPrice is decimal stop && closePrice <= stop)
			{
				SellMarket(Position);
				ClearRiskLevels();
				return true;
			}

			if (UseTakeProfit && _takePrice is decimal take && closePrice >= take)
			{
				SellMarket(Position);
				ClearRiskLevels();
				return true;
			}

			if (UseTrailingStop && trailingDistance > 0m)
			{
				var newStop = closePrice - trailingDistance;
				if (_stopPrice is decimal currentStop)
				{
					if (newStop > currentStop)
					{
						_stopPrice = newStop;
					}
				}
				else
				{
					_stopPrice = newStop;
				}
			}
		}
		else
		{
			if (UseStopLoss && _stopPrice is decimal stop && closePrice >= stop)
			{
				BuyMarket(-Position);
				ClearRiskLevels();
				return true;
			}

			if (UseTakeProfit && _takePrice is decimal take && closePrice <= take)
			{
				BuyMarket(-Position);
				ClearRiskLevels();
				return true;
			}

			if (UseTrailingStop && trailingDistance > 0m)
			{
				var newStop = closePrice + trailingDistance;
				if (_stopPrice is decimal currentStop)
				{
					if (newStop < currentStop)
					{
						_stopPrice = newStop;
					}
				}
				else
				{
					_stopPrice = newStop;
				}
			}
		}

		return false;
	}

	private void RegisterRiskLevels(decimal entryPrice, bool isLong)
	{
		_entryPrice = entryPrice;

		if (UseStopLoss)
		{
			var stopDistance = StopLossPips * _priceStep;
			_stopPrice = isLong ? entryPrice - stopDistance : entryPrice + stopDistance;
		}
		else
		{
			_stopPrice = null;
		}

		if (UseTakeProfit)
		{
			var takeDistance = TakeProfitPips * _priceStep;
			_takePrice = isLong ? entryPrice + takeDistance : entryPrice - takeDistance;
		}
		else
		{
			_takePrice = null;
		}
	}

	private void ClearRiskLevels()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private bool HasData(ValueBuffer buffer)
	{
		return buffer.HasEnoughData;
	}

	private bool IsBullTrend(ValueBuffer fast, ValueBuffer slow)
	{
		var fastCurrent = fast.Get(0);
		var fastPrevious = fast.Get(1);
		var fastShift = fast.Get(Math.Min(ShiftDepth, fast.Capacity - 1));
		var slowCurrent = slow.Get(0);
		var slowShift = slow.Get(Math.Min(ShiftDepth, slow.Capacity - 1));

		return fastCurrent is decimal fc && slowCurrent is decimal sc &&
			fastShift is decimal fs && slowShift is decimal ss &&
			fastPrevious is decimal fp &&
			fc >= sc && fs >= ss && fc >= fp;
	}

	private bool IsBearTrend(ValueBuffer fast, ValueBuffer slow)
	{
		var fastCurrent = fast.Get(0);
		var fastPrevious = fast.Get(1);
		var fastShift = fast.Get(Math.Min(ShiftDepth, fast.Capacity - 1));
		var slowCurrent = slow.Get(0);
		var slowShift = slow.Get(Math.Min(ShiftDepth, slow.Capacity - 1));

		return fastCurrent is decimal fc && slowCurrent is decimal sc &&
			fastShift is decimal fs && slowShift is decimal ss &&
			fastPrevious is decimal fp &&
			fc <= sc && fs <= ss && fc <= fp;
	}

	private sealed class ValueBuffer
	{
		private readonly decimal?[] _values;

		public ValueBuffer(int length)
		{
			if (length < 2)
			{
				length = 2;
			}

			_values = new decimal?[length];
		}

		public int Capacity => _values.Length;

		public bool HasEnoughData => _values[_values.Length - 1].HasValue;

		public void Push(decimal value)
		{
			for (var i = _values.Length - 1; i > 0; i--)
			{
				_values[i] = _values[i - 1];
			}

			_values[0] = value;
		}

		public decimal? Get(int index)
		{
			if (index < 0 || index >= _values.Length)
			{
				return null;
			}

			return _values[index];
		}
	}
}
