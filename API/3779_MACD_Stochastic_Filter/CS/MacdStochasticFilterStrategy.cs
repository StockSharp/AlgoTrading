using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD strategy with stochastic confirmation and EMA trend filter.
/// Buy when MACD crosses above signal with stochastic K > D and price above EMA.
/// Sell when MACD crosses below signal with stochastic K < D and price below EMA.
/// </summary>
public class MacdStochasticFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _stochKLength;
	private readonly StrategyParam<int> _stochDLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevMacd;
	private decimal? _prevSignal;

	/// <summary>
	/// Fast EMA period inside MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period inside MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Trend EMA period used as directional filter.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %K length.
	/// </summary>
	public int StochKLength
	{
		get => _stochKLength.Value;
		set => _stochKLength.Value = value;
	}

	/// <summary>
	/// Stochastic %D length.
	/// </summary>
	public int StochDLength
	{
		get => _stochDLength.Value;
		set => _stochDLength.Value = value;
	}

	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public MacdStochasticFilterStrategy()
	{
		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal", "Signal EMA period for MACD", "Indicators");

		_emaPeriod = Param(nameof(EmaPeriod), 26)
			.SetDisplay("Trend EMA", "EMA period for trend filter", "Indicators");

		_stochKLength = Param(nameof(StochKLength), 14)
			.SetDisplay("Stochastic K", "Look-back length for stochastic K", "Indicators");

		_stochDLength = Param(nameof(StochDLength), 3)
			.SetDisplay("Stochastic D", "Smoothing for D line", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for price data", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMacd = null;
		_prevSignal = null;

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod }
			},
			SignalMa = { Length = MacdSignalPeriod }
		};

		var stochastic = new StochasticOscillator
		{
			K = { Length = StochKLength },
			D = { Length = StochDLength }
		};

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, stochastic, ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue stochValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal || !stochValue.IsFinal || !emaValue.IsFinal)
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdData)
			return;

		if (macdData.Macd is not decimal macd || macdData.Signal is not decimal signal)
			return;

		if (stochValue is not StochasticOscillatorValue stochData)
			return;

		if (stochData.K is not decimal kVal || stochData.D is not decimal dVal)
			return;

		var emaVal = emaValue.ToDecimal();

		if (_prevMacd is not decimal prevMacd || _prevSignal is not decimal prevSignal)
		{
			_prevMacd = macd;
			_prevSignal = signal;
			return;
		}

		// MACD crossover signals
		var macdBullishCross = prevMacd <= prevSignal && macd > signal;
		var macdBearishCross = prevMacd >= prevSignal && macd < signal;

		// Long: MACD bullish cross + stochastic K > D + price above EMA
		if (Position <= 0 && macdBullishCross && kVal > dVal && candle.ClosePrice > emaVal)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Short: MACD bearish cross + stochastic K < D + price below EMA
		else if (Position >= 0 && macdBearishCross && kVal < dVal && candle.ClosePrice < emaVal)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevMacd = macd;
		_prevSignal = signal;
	}
}
