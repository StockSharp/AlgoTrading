using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe stochastic crossover strategy converted from the MetaTrader 4 expert advisor "STO_m5xm15xm30".
/// </summary>
public class StoM5xM15xM30Strategy : Strategy
{
	private const int SignalPeriod = 3;
	private const int SlowingPeriod = 3;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _middleCandleType;
	private readonly StrategyParam<DataType> _slowCandleType;
	private readonly StrategyParam<int> _fastKPeriod;
	private readonly StrategyParam<int> _middleKPeriod;
	private readonly StrategyParam<int> _slowKPeriod;
	private readonly StrategyParam<int> _exitKPeriod;
	private readonly StrategyParam<int> _shiftBars;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _tradeVolume;

	private StochasticOscillator _fastStochastic;
	private StochasticOscillator _middleStochastic;
	private StochasticOscillator _slowStochastic;
	private StochasticOscillator _exitStochastic;
	private StochasticShiftBuffer _fastShiftBuffer;
	private StochasticShiftBuffer _exitShiftBuffer;

	private decimal? _fastKCurrent;
	private decimal? _fastDCurrent;
	private decimal? _fastKShifted;
	private decimal? _fastDShifted;
	private decimal? _middleK;
	private decimal? _middleD;
	private decimal? _slowK;
	private decimal? _slowD;
	private decimal? _exitK;
	private decimal? _exitD;
	private decimal? _previousClose;
	private decimal? _currentCandlePreviousClose;
	private DateTimeOffset? _currentCandleTime;
	private DateTimeOffset? _evaluatedCandleTime;
	private bool _hasFastValue;
	private bool _hasExitValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="StoM5xM15xM30Strategy"/> class.
	/// </summary>
	public StoM5xM15xM30Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Primary Timeframe", "Base timeframe used for trade execution", "General");

		_middleCandleType = Param(nameof(MiddleCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Middle Timeframe", "Secondary timeframe used for confirmation", "General");

		_slowCandleType = Param(nameof(SlowCandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Slow Timeframe", "Third timeframe that validates the trend", "General");

		_fastKPeriod = Param(nameof(FastKPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast %K", "Stochastic %K period on the primary timeframe", "Indicators");

		_middleKPeriod = Param(nameof(MiddleKPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Middle %K", "Stochastic %K period on the middle timeframe", "Indicators");

		_slowKPeriod = Param(nameof(SlowKPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slow %K", "Stochastic %K period on the slow timeframe", "Indicators");

		_exitKPeriod = Param(nameof(ExitKPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Exit %K", "Stochastic %K period used for exit decisions", "Indicators");

		_shiftBars = Param(nameof(ShiftBars), 3)
			.SetNotNegative()
			.SetDisplay("Shift Bars", "Number of bars used to detect the crossing on the fast timeframe", "Logic");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 30m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Protective take profit distance in price points", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 10m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Protective stop loss distance in price points", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume submitted with each position", "Orders");
	}

	/// <summary>
	/// Primary candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Middle confirmation timeframe.
	/// </summary>
	public DataType MiddleCandleType
	{
		get => _middleCandleType.Value;
		set => _middleCandleType.Value = value;
	}

	/// <summary>
	/// Slow confirmation timeframe.
	/// </summary>
	public DataType SlowCandleType
	{
		get => _slowCandleType.Value;
		set => _slowCandleType.Value = value;
	}

	/// <summary>
	/// %K period for the fast timeframe stochastic oscillator.
	/// </summary>
	public int FastKPeriod
	{
		get => _fastKPeriod.Value;
		set => _fastKPeriod.Value = value;
	}

	/// <summary>
	/// %K period for the middle timeframe stochastic oscillator.
	/// </summary>
	public int MiddleKPeriod
	{
		get => _middleKPeriod.Value;
		set => _middleKPeriod.Value = value;
	}

	/// <summary>
	/// %K period for the slow timeframe stochastic oscillator.
	/// </summary>
	public int SlowKPeriod
	{
		get => _slowKPeriod.Value;
		set => _slowKPeriod.Value = value;
	}

	/// <summary>
	/// %K period used by the exit stochastic oscillator.
	/// </summary>
	public int ExitKPeriod
	{
		get => _exitKPeriod.Value;
		set => _exitKPeriod.Value = value;
	}

	/// <summary>
	/// Number of bars between the current and reference %K/%D crossover on the fast timeframe.
	/// </summary>
	public int ShiftBars
	{
		get => _shiftBars.Value;
		set => _shiftBars.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Order volume submitted when a new position is opened.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			yield break;

		yield return (Security, CandleType);

		if (CandleType != MiddleCandleType)
			yield return (Security, MiddleCandleType);

		if (SlowCandleType != CandleType && SlowCandleType != MiddleCandleType)
			yield return (Security, SlowCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastStochastic = null;
		_middleStochastic = null;
		_slowStochastic = null;
		_exitStochastic = null;
		_fastShiftBuffer = null;
		_exitShiftBuffer = null;

		_fastKCurrent = null;
		_fastDCurrent = null;
		_fastKShifted = null;
		_fastDShifted = null;
		_middleK = null;
		_middleD = null;
		_slowK = null;
		_slowD = null;
		_exitK = null;
		_exitD = null;
		_previousClose = null;
		_currentCandlePreviousClose = null;
		_currentCandleTime = null;
		_evaluatedCandleTime = null;
		_hasFastValue = false;
		_hasExitValue = false;

		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Align the strategy volume with the user parameter before any orders are generated.
		Volume = TradeVolume;

		// Instantiate stochastic oscillators for every timeframe used by the original expert advisor.
		_fastStochastic = CreateStochastic(FastKPeriod);
		_middleStochastic = CreateStochastic(MiddleKPeriod);
		_slowStochastic = CreateStochastic(SlowKPeriod);
		_exitStochastic = CreateStochastic(ExitKPeriod);
		_fastShiftBuffer = new StochasticShiftBuffer(ShiftBars);
		_exitShiftBuffer = new StochasticShiftBuffer(1);

		// Subscribe to candles and bind indicators using the high-level API.
		var fastSubscription = SubscribeCandles(CandleType);
		fastSubscription
			.BindEx(_exitStochastic, ProcessExitStochastic)
			.BindEx(_fastStochastic, ProcessFastStochastic)
			.Start();

		var middleSubscription = SubscribeCandles(MiddleCandleType);
		middleSubscription
			.BindEx(_middleStochastic, ProcessMiddleStochastic)
			.Start();

		var slowSubscription = SubscribeCandles(SlowCandleType);
		slowSubscription
			.BindEx(_slowStochastic, ProcessSlowStochastic)
			.Start();

		// Map MT4 point-based protections to StockSharp protective orders.
		var take = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints, UnitTypes.Point) : default;
		var stop = StopLossPoints > 0m ? new Unit(StopLossPoints, UnitTypes.Point) : default;

		StartProtection(take, stop, useMarketOrders: true);
	}

	private void ProcessFastStochastic(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		// Process only completed candles to mimic MT4 behaviour.
		if (candle.State != CandleStates.Finished)
			return;

		if (!indicatorValue.IsFinal)
			return;

		// Ensure the per-candle context is updated before using any cached values.
		EnsureCandleContext(candle);

		var stoch = (StochasticOscillatorValue)indicatorValue;
		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		// Store the current %K/%D values for entry calculations.
	_fastKCurrent = k;
	_fastDCurrent = d;

		if (_fastShiftBuffer != null)
		{
			// Obtain the value from ShiftBars candles ago to replicate iStochastic(..., shift).
			if (_fastShiftBuffer.TryGetShifted(k, d, out var shiftedK, out var shiftedD))
			{
				_fastKShifted = shiftedK;
				_fastDShifted = shiftedD;
			}
			else
			{
				_fastKShifted = null;
				_fastDShifted = null;
			}
		}
		else
		{
			_fastKShifted = k;
			_fastDShifted = d;
		}

		_hasFastValue = true;

		// Try to evaluate trading rules once both fast and exit values are ready.
		if (_hasExitValue)
			TryExecuteSignals(candle);

		// Cache the close price for the next candle comparison.
		_previousClose = candle.ClosePrice;
	}

	private void ProcessExitStochastic(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!indicatorValue.IsFinal)
			return;

		EnsureCandleContext(candle);

		var stoch = (StochasticOscillatorValue)indicatorValue;
		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		// The exit oscillator uses the previous candle (shift = 1) from the MT4 implementation.
		if (_exitShiftBuffer != null && _exitShiftBuffer.TryGetShifted(k, d, out var shiftedK, out var shiftedD))
		{
			_exitK = shiftedK;
			_exitD = shiftedD;
			_hasExitValue = true;
		}
		else
		{
			_exitK = null;
			_exitD = null;
			_hasExitValue = false;
		}

		if (_hasFastValue)
			TryExecuteSignals(candle);
	}

	private void ProcessMiddleStochastic(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!indicatorValue.IsFinal)
			return;

		var stoch = (StochasticOscillatorValue)indicatorValue;
		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		// Update the middle timeframe confirmation values.
	_middleK = k;
	_middleD = d;
	}

	private void ProcessSlowStochastic(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!indicatorValue.IsFinal)
			return;

		var stoch = (StochasticOscillatorValue)indicatorValue;
		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		// Update the slow timeframe confirmation values.
	_slowK = k;
	_slowD = d;
	}

	private void EnsureCandleContext(ICandleMessage candle)
	{
		var openTime = candle.OpenTime;
		if (_currentCandleTime == openTime)
			return;

		// Store per-candle state so the previous close remains available even after buffers advance.
		_currentCandleTime = openTime;
		_currentCandlePreviousClose = _previousClose;
		_hasFastValue = false;
		_hasExitValue = false;
		_evaluatedCandleTime = null;
	}

	private void TryExecuteSignals(ICandleMessage candle)
	{
		// Prevent duplicate processing on the same candle.
		if (_evaluatedCandleTime == candle.OpenTime)
			return;

		if (_fastKCurrent is not decimal fastK || _fastDCurrent is not decimal fastD)
			return;

		if (_fastKShifted is not decimal fastKShift || _fastDShifted is not decimal fastDShift)
			return;

		if (_middleK is not decimal middleK || _middleD is not decimal middleD)
			return;

		if (_slowK is not decimal slowK || _slowD is not decimal slowD)
			return;

		if (_exitK is not decimal exitK || _exitD is not decimal exitD)
			return;

		var previousClose = _currentCandlePreviousClose;
		if (previousClose is null)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Replicate the MT4 exit logic before considering new entries.
		var closedPosition = false;

		if (Position > 0 && exitK < exitD)
		{
			SellMarket(Position);
			closedPosition = true;
		}
		else if (Position < 0 && exitK > exitD)
		{
			BuyMarket(Math.Abs(Position));
			closedPosition = true;
		}

		if (!closedPosition)
		{
			// Entry requires a fresh crossover plus both higher timeframe confirmations and momentum from price change.
			var bullish = fastK > fastD && fastKShift < fastDShift && middleK > middleD && slowK > slowD && candle.ClosePrice > previousClose.Value;
			var bearish = fastK < fastD && fastKShift > fastDShift && middleK < middleD && slowK < slowD && candle.ClosePrice < previousClose.Value;

			if (bullish && Position <= 0)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));

				BuyMarket(Volume);
			}
			else if (bearish && Position >= 0)
			{
				if (Position > 0)
					SellMarket(Position);

				SellMarket(Volume);
			}
		}

		_evaluatedCandleTime = candle.OpenTime;
		_previousClose = candle.ClosePrice;
	}

	private static StochasticOscillator CreateStochastic(int kLength)
	{
		// Configure the oscillator with matching smoothing constants from the MT4 script.
		return new StochasticOscillator
		{
			Length = kLength,
			K = { Length = SlowingPeriod },
			D = { Length = SignalPeriod },
		};
	}

	private sealed class StochasticShiftBuffer
	{
		private readonly (decimal K, decimal D)?[] _buffer;
		private readonly int _shift;
		private int _index;
		private int _count;

		public StochasticShiftBuffer(int shift)
		{
			_shift = Math.Max(0, shift);
			_buffer = new (decimal K, decimal D)?[_shift + 1];
			_index = 0;
			_count = 0;
		}

		public bool TryGetShifted(decimal k, decimal d, out decimal shiftedK, out decimal shiftedD)
		{
			// Store the current value and return the element Shift bars back when available.
			_buffer[_index] = (k, d);

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

				var value = _buffer[idx];
				if (value.HasValue)
				{
					shiftedK = value.Value.K;
					shiftedD = value.Value.D;
					return true;
				}
			}

			shiftedK = 0m;
			shiftedD = 0m;
			return false;
		}
	}
}
