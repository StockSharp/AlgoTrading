using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe MACD strategy supporting crossover or zero-line entries.
/// </summary>
public class MultiTimeframeMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<bool> _showCurrentTf;
	private readonly StrategyParam<bool> _showHigherTf;
	private readonly StrategyParam<EntryType> _entryType;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPercent;

	private MovingAverageConvergenceDivergence _currentMacd;
	private MovingAverageConvergenceDivergence _higherMacd;

	private decimal _currentMacdValue;
	private decimal _currentSignalValue;
	private decimal _prevCurrentMacd;
	private decimal _prevCurrentSignal;

	private decimal _higherMacdValue;
	private decimal _higherSignalValue;
	private decimal _prevHigherMacd;
	private decimal _prevHigherSignal;

	private decimal _longStopPrice;
	private decimal _shortStopPrice;
	private decimal _entryPrice;

	/// <summary>
	/// Fast length for MACD.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow length for MACD.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Signal smoothing length.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Working timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Use current timeframe MACD.
	/// </summary>
	public bool ShowCurrentTimeframe
	{
		get => _showCurrentTf.Value;
		set => _showCurrentTf.Value = value;
	}

	/// <summary>
	/// Use higher timeframe MACD.
	/// </summary>
	public bool ShowHigherTimeframe
	{
		get => _showHigherTf.Value;
		set => _showHigherTf.Value = value;
	}

	/// <summary>
	/// Entry signal type.
	/// </summary>
	public EntryType Entry
	{
		get => _entryType.Value;
		set => _entryType.Value = value;
	}

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop percent.
	/// </summary>
	public decimal TrailingStopPercent
	{
		get => _trailingStopPercent.Value;
		set => _trailingStopPercent.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MultiTimeframeMacdStrategy"/>.
	/// </summary>
	public MultiTimeframeMacdStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
						  .SetGreaterThanZero()
						  .SetDisplay("Fast Length", "Fast length for MACD", "MACD")
						  .SetCanOptimize(true);

		_slowLength = Param(nameof(SlowLength), 26)
						  .SetGreaterThanZero()
						  .SetDisplay("Slow Length", "Slow length for MACD", "MACD")
						  .SetCanOptimize(true);

		_signalLength = Param(nameof(SignalLength), 9)
							.SetGreaterThanZero()
							.SetDisplay("Signal Length", "Signal length for MACD", "MACD")
							.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						  .SetDisplay("Candle Type", "Working timeframe", "General");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromDays(1).TimeFrame())
								.SetDisplay("Higher TF", "Higher timeframe", "General");

		_showCurrentTf = Param(nameof(ShowCurrentTimeframe), true)
							 .SetDisplay("Use Current TF", "Include current timeframe MACD", "Logic");

		_showHigherTf = Param(nameof(ShowHigherTimeframe), true)
							.SetDisplay("Use Higher TF", "Include higher timeframe MACD", "Logic");

		_entryType = Param(nameof(Entry), EntryType.Crossover).SetDisplay("Entry Type", "Signal type", "Logic");

		_useTrailingStop =
			Param(nameof(UseTrailingStop), false).SetDisplay("Use Trailing", "Enable trailing stop", "Risk");

		_trailingStopPercent = Param(nameof(TrailingStopPercent), 2m)
								   .SetNotNegative()
								   .SetDisplay("Trailing Stop %", "Trailing stop percent", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType),
																						(Security, HigherCandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_currentMacd = null;
		_higherMacd = null;
		_currentMacdValue = _currentSignalValue = 0m;
		_prevCurrentMacd = _prevCurrentSignal = 0m;
		_higherMacdValue = _higherSignalValue = 0m;
		_prevHigherMacd = _prevHigherSignal = 0m;
		_longStopPrice = _shortStopPrice = 0m;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_currentMacd = CreateMacd();
		_higherMacd = CreateMacd();

		var currentSubscription = SubscribeCandles(CandleType);
		currentSubscription.Bind(_currentMacd, ProcessCurrent).Start();

		SubscribeCandles(HigherCandleType).Bind(_higherMacd, ProcessHigher).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, currentSubscription);
			DrawIndicator(area, _currentMacd);
			DrawOwnTrades(area);
		}
	}

	private MovingAverageConvergenceDivergence CreateMacd() => new()
	{
		ShortPeriod = FastLength,
		LongPeriod = SlowLength,
		SignalPeriod = SignalLength
	};

	private void ProcessHigher(ICandleMessage candle, decimal macd, decimal signal, decimal _)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_prevHigherMacd = _higherMacdValue;
		_prevHigherSignal = _higherSignalValue;
		_higherMacdValue = macd;
		_higherSignalValue = signal;
	}

	private void ProcessCurrent(ICandleMessage candle, decimal macd, decimal signal, decimal _)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_prevCurrentMacd = _currentMacdValue;
		_prevCurrentSignal = _currentSignalValue;
		_currentMacdValue = macd;
		_currentSignalValue = signal;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var currentBullish = _currentMacdValue > _currentSignalValue;
		var currentBearish = _currentMacdValue < _currentSignalValue;
		var higherBullish = _higherMacdValue > _higherSignalValue;
		var higherBearish = _higherMacdValue < _higherSignalValue;

		var currentAboveZero = _currentMacdValue > 0m;
		var currentBelowZero = _currentMacdValue < 0m;
		var higherAboveZero = _higherMacdValue > 0m;
		var higherBelowZero = _higherMacdValue < 0m;

		var longCross = false;
		var shortCross = false;
		var longZero = false;
		var shortZero = false;

		if (ShowCurrentTimeframe && ShowHigherTimeframe)
		{
			longCross = currentBullish && higherBullish && _prevCurrentMacd <= _prevCurrentSignal &&
						_currentMacdValue > _currentSignalValue;
			shortCross = currentBearish && higherBearish && _prevCurrentMacd >= _prevCurrentSignal &&
						 _currentMacdValue < _currentSignalValue;
		}
		else if (ShowCurrentTimeframe && !ShowHigherTimeframe)
		{
			longCross =
				currentBullish && _prevCurrentMacd <= _prevCurrentSignal && _currentMacdValue > _currentSignalValue;
			shortCross =
				currentBearish && _prevCurrentMacd >= _prevCurrentSignal && _currentMacdValue < _currentSignalValue;
		}
		else if (!ShowCurrentTimeframe && ShowHigherTimeframe)
		{
			longCross = higherBullish && _prevHigherMacd <= _prevHigherSignal && _higherMacdValue > _higherSignalValue;
			shortCross = higherBearish && _prevHigherMacd >= _prevHigherSignal && _higherMacdValue < _higherSignalValue;
		}

		if (ShowCurrentTimeframe && ShowHigherTimeframe)
		{
			longZero = currentAboveZero && higherAboveZero && _prevCurrentMacd <= 0m && _currentMacdValue > 0m;
			shortZero = currentBelowZero && higherBelowZero && _prevCurrentMacd >= 0m && _currentMacdValue < 0m;
		}
		else if (ShowCurrentTimeframe && !ShowHigherTimeframe)
		{
			longZero = currentAboveZero && _prevCurrentMacd <= 0m && _currentMacdValue > 0m;
			shortZero = currentBelowZero && _prevCurrentMacd >= 0m && _currentMacdValue < 0m;
		}
		else if (!ShowCurrentTimeframe && ShowHigherTimeframe)
		{
			longZero = higherAboveZero && _prevHigherMacd <= 0m && _higherMacdValue > 0m;
			shortZero = higherBelowZero && _prevHigherMacd >= 0m && _higherMacdValue < 0m;
		}

		var entryLong = (Entry == EntryType.Crossover && longCross) || (Entry == EntryType.ZeroCross && longZero) ||
						(Entry == EntryType.Both && (longCross || longZero));

		var entryShort = (Entry == EntryType.Crossover && shortCross) || (Entry == EntryType.ZeroCross && shortZero) ||
						 (Entry == EntryType.Both && (shortCross || shortZero));

		if (entryLong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			if (UseTrailingStop)
			{
				var perc = TrailingStopPercent / 100m;
				_longStopPrice = _entryPrice * (1 - perc);
				_shortStopPrice = 0m;
			}
		}
		else if (entryShort && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			if (UseTrailingStop)
			{
				var perc = TrailingStopPercent / 100m;
				_shortStopPrice = _entryPrice * (1 + perc);
				_longStopPrice = 0m;
			}
		}

		if (UseTrailingStop)
		{
			var perc = TrailingStopPercent / 100m;
			if (Position > 0)
			{
				var newStop = candle.ClosePrice * (1 - perc);
				_longStopPrice = Math.Max(_longStopPrice, newStop);
				if (candle.LowPrice <= _longStopPrice)
				{
					SellMarket(Math.Abs(Position));
					_longStopPrice = 0m;
					_entryPrice = 0m;
				}
			}
			else if (Position < 0)
			{
				var newStop = candle.ClosePrice * (1 + perc);
				_shortStopPrice = Math.Min(_shortStopPrice, newStop);
				if (candle.HighPrice >= _shortStopPrice)
				{
					BuyMarket(Math.Abs(Position));
					_shortStopPrice = 0m;
					_entryPrice = 0m;
				}
			}
		}
		else
		{
			var exitLong = ((Entry == EntryType.Crossover || Entry == EntryType.Both) &&
							_prevCurrentMacd >= _prevCurrentSignal && _currentMacdValue < _currentSignalValue) ||
						   ((Entry == EntryType.ZeroCross || Entry == EntryType.Both) && _prevCurrentMacd >= 0m &&
							_currentMacdValue < 0m);

			var exitShort = ((Entry == EntryType.Crossover || Entry == EntryType.Both) &&
							 _prevCurrentMacd <= _prevCurrentSignal && _currentMacdValue > _currentSignalValue) ||
							((Entry == EntryType.ZeroCross || Entry == EntryType.Both) && _prevCurrentMacd <= 0m &&
							 _currentMacdValue > 0m);

			if (Position > 0 && exitLong)
			{
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && exitShort)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}

	/// <summary>
	/// Entry types.
	/// </summary>
	public enum EntryType
	{
		/// <summary>
		/// MACD line crossing signal line.
		/// </summary>
		Crossover,

		/// <summary>
		/// MACD line crossing zero line.
		/// </summary>
		ZeroCross,

		/// <summary>
		/// Either crossover or zero-line cross.
		/// </summary>
		Both
	}
}
