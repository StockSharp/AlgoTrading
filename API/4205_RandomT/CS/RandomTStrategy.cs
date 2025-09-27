using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Translation of the MetaTrader "RandomT" expert advisor.
/// Combines ZigZag-like swing confirmation with fractals and a MACD filter.
/// </summary>
public class RandomTStrategy : Strategy
{
private readonly StrategyParam<decimal> _tradeVolume;
private readonly StrategyParam<int> _barWatch;
private readonly StrategyParam<int> _shift;
private readonly StrategyParam<bool> _useTrailing;
private readonly StrategyParam<bool> _autoStopLevel;
private readonly StrategyParam<decimal> _startStopLevelPoints;
private readonly StrategyParam<decimal> _stopLevelPoints;
private readonly StrategyParam<decimal> _minProfit;
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _macdFastLength;
private readonly StrategyParam<int> _macdSlowLength;
private readonly StrategyParam<int> _macdSignalLength;
private readonly StrategyParam<int> _fractalWing;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private readonly List<CandleSnapshot> _candles = new();
	private readonly List<MacdSnapshot> _macdHistory = new();

	private decimal? _entryPrice;
	private decimal? _trailingStopPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="RandomTStrategy"/>.
	/// </summary>
public RandomTStrategy()
{
_tradeVolume = Param(nameof(TradeVolume), 0.1m)
.SetGreaterThanZero()
.SetDisplay("Trade Volume", "Base order size in lots", "Trading");

		_barWatch = Param(nameof(BarWatch), 12)
			.SetGreaterThanZero()
			.SetDisplay("ZigZag Depth", "Lookback used to validate swing extremes", "Signals");

		_shift = Param(nameof(Shift), 2)
			.SetGreaterOrEqual(2)
			.SetDisplay("Signal Shift", "Number of bars to look back when evaluating signals", "Signals");

		_useTrailing = Param(nameof(UseTrailingProfit), true)
			.SetDisplay("Use Trailing", "Enable trailing stop logic", "Risk");

		_autoStopLevel = Param(nameof(AutoStopLevel), false)
			.SetDisplay("Auto Stop Level", "Use the alternate stop level distance", "Risk");

		_startStopLevelPoints = Param(nameof(StartStopLevelPoints), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Start Stop Level", "Alternate trailing distance in points", "Risk");

		_stopLevelPoints = Param(nameof(StopLevelPoints), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Level", "Trailing stop distance in points", "Risk");

		_minProfit = Param(nameof(MinProfit), 0.4m)
			.SetNotNegative()
			.SetDisplay("Min Profit", "Floating profit required before trailing activates", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for analysis", "General");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period", "Indicators");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period", "Indicators");

_macdSignalLength = Param(nameof(MacdSignalLength), 9)
.SetGreaterThanZero()
.SetDisplay("MACD Signal", "Signal EMA period", "Indicators");

_fractalWing = Param(nameof(FractalWing), 2)
.SetGreaterOrEqual(1)
.SetDisplay("Fractal Wing", "Number of candles on each side for fractal confirmation", "Signals")
.SetCanOptimize(true);
}

	/// <summary>
	/// Base order size in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// ZigZag style depth in bars.
	/// </summary>
	public int BarWatch
	{
		get => _barWatch.Value;
		set => _barWatch.Value = value;
	}

	/// <summary>
	/// Number of bars to shift indicator readings back in time.
	/// </summary>
	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}

	/// <summary>
	/// Whether the trailing stop logic is enabled.
	/// </summary>
	public bool UseTrailingProfit
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
	}

	/// <summary>
	/// Whether to use the alternate stop level distance.
	/// </summary>
	public bool AutoStopLevel
	{
		get => _autoStopLevel.Value;
		set => _autoStopLevel.Value = value;
	}

	/// <summary>
	/// Alternate trailing distance in points.
	/// </summary>
	public decimal StartStopLevelPoints
	{
		get => _startStopLevelPoints.Value;
		set => _startStopLevelPoints.Value = value;
	}

	/// <summary>
	/// Primary trailing distance in points.
	/// </summary>
	public decimal StopLevelPoints
	{
		get => _stopLevelPoints.Value;
		set => _stopLevelPoints.Value = value;
	}

	/// <summary>
	/// Floating profit threshold in account currency.
	/// </summary>
	public decimal MinProfit
	{
		get => _minProfit.Value;
		set => _minProfit.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA period of the MACD filter.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period of the MACD filter.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal EMA period of the MACD filter.
	/// </summary>
public int MacdSignalLength
{
get => _macdSignalLength.Value;
set => _macdSignalLength.Value = value;
}

public int FractalWing
{
get => _fractalWing.Value;
set => _fractalWing.Value = value;
}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_candles.Clear();
		_macdHistory.Clear();
		_entryPrice = null;
		_trailingStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_macd = new()
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength },
			},
			SignalMa = { Length = MacdSignalLength },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Cache the most recent candle and indicator values.
		var snapshot = new CandleSnapshot(candle);
		var macdSnapshot = ExtractMacd(macdValue);

		AppendData(snapshot, macdSnapshot);

		// Reset trailing data whenever the position is flat.
		if (Position == 0m)
		{
			_entryPrice = null;
			_trailingStopPrice = null;
		}
		else if (_entryPrice is null)
		{
			_entryPrice = candle.ClosePrice;
		}

		var shift = Shift;
		var index = _candles.Count - 1 - shift;

		// Evaluate the bar that matches the requested shift.
if (index >= FractalWing && index <= _candles.Count - FractalWing - 1)
		{
			var macdAtShift = _macdHistory[index];

			if (macdAtShift.Macd is decimal macdLine && macdAtShift.Signal is decimal signalLine)
			{
				var hasFractalHigh = TryGetFractalHigh(index, out _);
				var hasFractalLow = TryGetFractalLow(index, out _);

				// Confirm that the fractal aligns with a local ZigZag-style extreme.
				var hasZigZagHigh = hasFractalHigh && IsZigZagHigh(index);
				var hasZigZagLow = hasFractalLow && IsZigZagLow(index);

				// MACD agreement filters out random swings.
				var sellSignal = hasZigZagHigh && macdLine > signalLine;
				var buySignal = hasZigZagLow && macdLine < signalLine;

				if (IsFormedAndOnlineAndAllowTrading())
				{
					if (sellSignal && Position >= 0m)
					{
						var volume = Volume + Math.Max(Position, 0m);

						if (volume > 0m)
						{
							SellMarket(volume);
							_entryPrice = candle.ClosePrice;
							_trailingStopPrice = null;
						}
					}
					else if (buySignal && Position <= 0m)
					{
						var volume = Volume + Math.Max(-Position, 0m);

						if (volume > 0m)
						{
							BuyMarket(volume);
							_entryPrice = candle.ClosePrice;
							_trailingStopPrice = null;
						}
					}
				}
			}
		}

		ApplyTrailingStop(candle);
	}

	private void AppendData(CandleSnapshot candle, MacdSnapshot macd)
	{
		// Keep rolling buffers in sync for the swing calculations.
		_candles.Add(candle);
		_macdHistory.Add(macd);

var maxSize = Math.Max(BarWatch * 2 + Shift + 6, Shift + 6 + FractalWing);

		if (_candles.Count > maxSize)
		{
			_candles.RemoveAt(0);
			_macdHistory.RemoveAt(0);
		}
	}

	private static MacdSnapshot ExtractMacd(IIndicatorValue macdValue)
	{
		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue typed)
			return default;

		// Accessing the typed structure gives us both MACD and signal lines at once.

		var macdLine = typed.Macd as decimal?;
		var signalLine = typed.Signal as decimal?;

		return new MacdSnapshot(macdLine, signalLine);
	}

	private bool TryGetFractalHigh(int index, out decimal price)
	{
		var candidate = _candles[index].High;

for (var offset = -FractalWing; offset <= FractalWing; offset++)
		{
			if (offset == 0)
				continue;

			if (_candles[index + offset].High >= candidate)
			{
				price = default;
				return false;
			}
		}

		price = candidate;
		return true;
	}

	private bool TryGetFractalLow(int index, out decimal price)
	{
		var candidate = _candles[index].Low;

for (var offset = -FractalWing; offset <= FractalWing; offset++)
		{
			if (offset == 0)
				continue;

			if (_candles[index + offset].Low <= candidate)
			{
				price = default;
				return false;
			}
		}

		price = candidate;
		return true;
	}

	private bool IsZigZagHigh(int index)
	{
		var target = _candles[index].High;
		var start = Math.Max(0, index - BarWatch);
		var end = Math.Min(_candles.Count - 1, index + BarWatch);

		for (var i = start; i <= end; i++)
		{
			if (i == index)
				continue;

			if (_candles[i].High > target)
				return false;
		}

		return true;
	}

	private bool IsZigZagLow(int index)
	{
		var target = _candles[index].Low;
		var start = Math.Max(0, index - BarWatch);
		var end = Math.Min(_candles.Count - 1, index + BarWatch);

		for (var i = start; i <= end; i++)
		{
			if (i == index)
				continue;

			if (_candles[i].Low < target)
				return false;
		}

		return true;
	}

	private void ApplyTrailingStop(ICandleMessage candle)
	{
		if (!UseTrailingProfit)
			return;

		// Reset trailing data whenever the position is flat.
		if (Position == 0m)
		{
			_trailingStopPrice = null;
			return;
		}

		if (_entryPrice is null)
			return;

		var step = GetPriceStep();
		var stepPrice = GetStepPrice();

		if (step <= 0m || stepPrice <= 0m)
			return;

		var positionDirection = Math.Sign(Position);
		var volume = Math.Abs(Position);

		var profit = (candle.ClosePrice - _entryPrice.Value) * positionDirection;
		profit = profit / step * stepPrice * volume;

		if (profit < MinProfit)
			return;

		var trailingPoints = AutoStopLevel ? StartStopLevelPoints : StopLevelPoints;

		if (trailingPoints <= 0m)
			return;

		var trailingDistance = trailingPoints * step;

		// Move the trailing stop in the direction of profit and exit when touched.
		if (positionDirection > 0)
		{
			var candidate = candle.ClosePrice - trailingDistance;

			if (!_trailingStopPrice.HasValue || candidate > _trailingStopPrice.Value)
				_trailingStopPrice = candidate;

			if (_trailingStopPrice.HasValue && candle.LowPrice <= _trailingStopPrice.Value)
			{
				ClosePosition();
				_entryPrice = null;
				_trailingStopPrice = null;
			}
		}
		else if (positionDirection < 0)
		{
			var candidate = candle.ClosePrice + trailingDistance;

			if (!_trailingStopPrice.HasValue || candidate < _trailingStopPrice.Value)
				_trailingStopPrice = candidate;

			if (_trailingStopPrice.HasValue && candle.HighPrice >= _trailingStopPrice.Value)
			{
				ClosePosition();
				_entryPrice = null;
				_trailingStopPrice = null;
			}
		}
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 1m;
	}

	private decimal GetStepPrice()
	{
		var stepPrice = Security?.StepPrice ?? 0m;

		if (stepPrice > 0m)
			return stepPrice;

		var step = Security?.PriceStep ?? 1m;
		return step > 0m ? step : 1m;
	}

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(ICandleMessage candle)
		{
			Time = candle.OpenTime;
			Open = candle.OpenPrice;
			High = candle.HighPrice;
			Low = candle.LowPrice;
			Close = candle.ClosePrice;
		}

		public DateTimeOffset Time { get; }
		public decimal Open { get; }
		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
	}

	private readonly struct MacdSnapshot
	{
		public MacdSnapshot(decimal? macd, decimal? signal)
		{
			Macd = macd;
			Signal = signal;
		}

		public decimal? Macd { get; }
		public decimal? Signal { get; }
	}
}
