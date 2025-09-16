using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on RMACD indicator with several reversal entry modes.
/// </summary>
public class RmacdReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<AlgMode> _mode;

	private decimal _prevMacd;
	private decimal _prevMacd2;
	private decimal _prevSignal;
	private decimal _prevSignal2;
	private int _initialized;

	/// <summary>
	/// Fast period for MACD calculation.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow period for MACD calculation.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Signal line period.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Timeframe used for MACD.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Entry mode selection.
	/// </summary>
	public AlgMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public RmacdReversalStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA period", "Indicator");

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA period", "Indicator");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Signal smoothing period", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");

		_mode = Param(nameof(Mode), AlgMode.MacdDisposition)
			.SetDisplay("Mode", "Entry algorithm", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(macd, ProcessMacd)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (!macdValue.IsFinal)
			return;

		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macd = typed.Macd;
		var signal = typed.Signal;

		// Initialize previous values during first ticks
		if (_initialized == 0)
		{
			_prevMacd = macd;
			_prevSignal = signal;
			_initialized = 1;
			return;
		}
		else if (_initialized == 1)
		{
			_prevMacd2 = _prevMacd;
			_prevSignal2 = _prevSignal;
			_prevMacd = macd;
			_prevSignal = signal;
			_initialized = 2;
			return;
		}

		var buy = false;
		var sell = false;

		switch (Mode)
		{
			case AlgMode.Breakdown:
				buy = _prevMacd > 0m && macd <= 0m;
				sell = _prevMacd < 0m && macd >= 0m;
				break;

			case AlgMode.MacdTwist:
				buy = _prevMacd < _prevMacd2 && macd > _prevMacd;
				sell = _prevMacd > _prevMacd2 && macd < _prevMacd;
				break;

			case AlgMode.SignalTwist:
				buy = _prevSignal < _prevSignal2 && signal > _prevSignal;
				sell = _prevSignal > _prevSignal2 && signal < _prevSignal;
				break;

			case AlgMode.MacdDisposition:
				buy = _prevMacd > _prevSignal && macd <= signal;
				sell = _prevMacd < _prevSignal && macd >= signal;
				break;
		}

		if (buy && Position <= 0)
		{
			// Enter long position on buy signal
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (sell && Position >= 0)
		{
			// Enter short position on sell signal
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevMacd2 = _prevMacd;
		_prevSignal2 = _prevSignal;
		_prevMacd = macd;
		_prevSignal = signal;
	}

	/// <summary>
	/// Entry modes for RMACD strategy.
	/// </summary>
	public enum AlgMode
	{
		/// <summary>
		/// MACD histogram crossing the zero line.
		/// </summary>
		Breakdown,

		/// <summary>
		/// MACD histogram changes direction.
		/// </summary>
		MacdTwist,

		/// <summary>
		/// Signal line changes direction.
		/// </summary>
		SignalTwist,

		/// <summary>
		/// MACD histogram crosses the signal line.
		/// </summary>
		MacdDisposition
	}
}
