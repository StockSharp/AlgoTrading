using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the MACD Candle indicator.
/// Opens long positions when the MACD computed on candle closes exceeds the MACD on opens.
/// Opens short positions when the MACD on opens exceeds the MACD on closes.
/// </summary>
public class MacdCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macdOpen = null!;
	private MovingAverageConvergenceDivergenceSignal _macdClose = null!;
	private decimal? _previousColor;

	/// <summary>
	/// Fast EMA period for MACD calculation.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow EMA period for MACD calculation.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Signal line period for MACD calculation.
	/// </summary>
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }

	/// <summary>
	/// Candle type used for processing.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MacdCandleStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(8, 16, 2);

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal", "Signal period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(4)))
			.SetDisplay("Candle Type", "Candle type for indicators", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macdOpen = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		_macdClose = new MovingAverageConvergenceDivergenceSignal
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
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openValue = _macdOpen.Process(new CandleIndicatorValue(candle, candle.OpenPrice));
		var closeValue = _macdClose.Process(new CandleIndicatorValue(candle, candle.ClosePrice));

		if (!openValue.IsFinal || !closeValue.IsFinal)
			return;

		var openMacd = ((MovingAverageConvergenceDivergenceSignalValue)openValue).Macd;
		var closeMacd = ((MovingAverageConvergenceDivergenceSignalValue)closeValue).Macd;

		var color = openMacd < closeMacd ? 2m : openMacd > closeMacd ? 0m : 1m;

		if (_previousColor is null)
		{
			_previousColor = color;
			return;
		}

		if (color == 2m && _previousColor < 2m)
		{
			if (Position <= 0)
				BuyMarket();
		}
		else if (color == 0m && _previousColor > 0m)
		{
			if (Position >= 0)
				SellMarket();
		}

		_previousColor = color;
	}
}
