namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Three Signal Directional Trend Strategy - combines MACD, Stochastic, and RSI signals.
/// </summary>
public class ThreeSignalDirectionalTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdAvgLength;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;

	private decimal _prevMacdSignal;
	private bool _macdInitialized;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	public int MacdAvgLength
	{
		get => _macdAvgLength.Value;
		set => _macdAvgLength.Value = value;
	}

	public int StochKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	public ThreeSignalDirectionalTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetDisplay("MACD Fast", "Fast EMA length", "MACD");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetDisplay("MACD Slow", "Slow EMA length", "MACD");

		_macdAvgLength = Param(nameof(MacdAvgLength), 9)
			.SetDisplay("MACD Signal", "Signal EMA length", "MACD");

		_stochKPeriod = Param(nameof(StochKPeriod), 3)
			.SetDisplay("Stoch %K", "Stochastic smoothing", "Stochastic");

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetDisplay("Stoch %D", "Stochastic %D", "Stochastic");

		_overbought = Param(nameof(Overbought), 80m)
			.SetDisplay("Overbought", "Stochastic overbought level", "Stochastic");

		_oversold = Param(nameof(Oversold), 20m)
			.SetDisplay("Oversold", "Stochastic oversold level", "Stochastic");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMacdSignal = 0;
		_macdInitialized = false;

		var stochastic = new StochasticOscillator
		{
			K = { Length = StochKPeriod },
			D = { Length = StochDPeriod }
		};

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength }
			},
			SignalMa = { Length = MacdAvgLength }
		};

		var rsi = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(stochastic, macd, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue, IIndicatorValue macdValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stochTyped = (StochasticOscillatorValue)stochValue;
		if (stochTyped.K is not decimal stochK)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Signal is not decimal macdSignal)
			return;

		if (rsiValue.IsEmpty)
			return;

		var rsiVal = rsiValue.ToDecimal();

		var longCount = 0;
		var shortCount = 0;

		// MACD signal rising/falling
		if (_macdInitialized)
		{
			if (macdSignal > _prevMacdSignal)
				longCount++;
			else if (macdSignal < _prevMacdSignal)
				shortCount++;
		}
		else
		{
			_macdInitialized = true;
		}
		_prevMacdSignal = macdSignal;

		// Stochastic oversold/overbought
		if (stochK <= Oversold)
			longCount++;
		else if (stochK >= Overbought)
			shortCount++;

		// RSI direction
		if (rsiVal < 40)
			longCount++;
		else if (rsiVal > 60)
			shortCount++;

		// Trade when at least 2 signals agree
		if (longCount >= 2 && Position <= 0)
			BuyMarket();
		else if (shortCount >= 2 && Position >= 0)
			SellMarket();
	}
}
