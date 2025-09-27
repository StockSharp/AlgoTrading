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
/// Multi-indicator voting strategy that aggregates MACD, Awesome Oscillator,
/// OsMA, Williams %R, and Stochastic Oscillator signals.
/// </summary>
public class MultiIndicatorOptimizerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<decimal> _macdWeight;

	private readonly StrategyParam<int> _aoShort;
	private readonly StrategyParam<int> _aoLong;
	private readonly StrategyParam<decimal> _aoWeight;

	private readonly StrategyParam<int> _osmaFast;
	private readonly StrategyParam<int> _osmaSlow;
	private readonly StrategyParam<int> _osmaSignal;
	private readonly StrategyParam<decimal> _osmaWeight;

	private readonly StrategyParam<int> _williamsPeriod;
	private readonly StrategyParam<decimal> _williamsLower;
	private readonly StrategyParam<decimal> _williamsUpper;
	private readonly StrategyParam<decimal> _williamsWeight;

	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<int> _stochSlowing;
	private readonly StrategyParam<decimal> _stochLower;
	private readonly StrategyParam<decimal> _stochUpper;
	private readonly StrategyParam<decimal> _stochWeight;

	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;

	private decimal? _prevMacdMain;
	private decimal? _prevMacdSignal;
	private decimal? _prevOsma;

	private decimal? _prevAo;
	private decimal? _prevPrevAo;

	private decimal? _prevWilliams;
	private decimal? _prevPrevWilliams;

	private decimal? _prevStochK;
	private decimal? _prevPrevStochK;
	private decimal? _prevStochSignal;

	private decimal _lastSignal;

	/// <summary>
	/// Initializes a new instance of <see cref="MultiIndicatorOptimizerStrategy"/>.
	/// </summary>
	public MultiIndicatorOptimizerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for indicator calculations", "General");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(6, 24, 2);

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line period for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_macdWeight = Param(nameof(MacdWeight), 1m)
			.SetDisplay("MACD Weight", "Voting weight of MACD block", "Weights")
			.SetCanOptimize(true)
			.SetOptimize(-2m, 2m, 0.5m);

		_aoShort = Param(nameof(AoShortPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("AO Short", "Short moving average for Awesome Oscillator", "Awesome")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_aoLong = Param(nameof(AoLongPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("AO Long", "Long moving average for Awesome Oscillator", "Awesome")
			.SetCanOptimize(true)
			.SetOptimize(20, 50, 2);

		_aoWeight = Param(nameof(AoWeight), 1m)
			.SetDisplay("AO Weight", "Voting weight of Awesome Oscillator block", "Weights")
			.SetCanOptimize(true)
			.SetOptimize(-2m, 2m, 0.5m);

		_osmaFast = Param(nameof(OsmaFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("OsMA Fast", "Fast EMA period for OsMA histogram", "OsMA")
			.SetCanOptimize(true)
			.SetOptimize(6, 24, 2);

		_osmaSlow = Param(nameof(OsmaSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("OsMA Slow", "Slow EMA period for OsMA histogram", "OsMA")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_osmaSignal = Param(nameof(OsmaSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("OsMA Signal", "Signal EMA period for OsMA histogram", "OsMA")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_osmaWeight = Param(nameof(OsmaWeight), 1m)
			.SetDisplay("OsMA Weight", "Voting weight of OsMA block", "Weights")
			.SetCanOptimize(true)
			.SetOptimize(-2m, 2m, 0.5m);

		_williamsPeriod = Param(nameof(WilliamsPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Lookback for Williams %R", "Williams %R")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_williamsLower = Param(nameof(WilliamsLowerLevel), -80m)
			.SetDisplay("Williams Lower", "Oversold boundary for Williams %R", "Williams %R");

		_williamsUpper = Param(nameof(WilliamsUpperLevel), -20m)
			.SetDisplay("Williams Upper", "Overbought boundary for Williams %R", "Williams %R");

		_williamsWeight = Param(nameof(WilliamsWeight), 1m)
			.SetDisplay("Williams Weight", "Voting weight of Williams %R block", "Weights")
			.SetCanOptimize(true)
			.SetOptimize(-2m, 2m, 0.5m);

		_stochKPeriod = Param(nameof(StochasticKPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "%K period for Stochastic Oscillator", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 1);

		_stochDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "%D period for Stochastic Oscillator", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(2, 9, 1);

		_stochSlowing = Param(nameof(StochasticSlowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Smoothing", "Smoothing applied to %K", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(1, 9, 1);

		_stochLower = Param(nameof(StochasticLowerLevel), 20m)
			.SetDisplay("Stochastic Lower", "Oversold threshold for Stochastic", "Stochastic");

		_stochUpper = Param(nameof(StochasticUpperLevel), 80m)
			.SetDisplay("Stochastic Upper", "Overbought threshold for Stochastic", "Stochastic");

		_stochWeight = Param(nameof(StochasticWeight), 1m)
			.SetDisplay("Stochastic Weight", "Voting weight of Stochastic block", "Weights")
			.SetCanOptimize(true)
			.SetOptimize(-2m, 2m, 0.5m);

		_entryThreshold = Param(nameof(EntryThreshold), 0.5m)
			.SetDisplay("Entry Threshold", "Minimum aggregated signal required to open a position", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.25m, 2m, 0.25m);

		_exitThreshold = Param(nameof(ExitThreshold), 0.1m)
			.SetDisplay("Exit Threshold", "Signal absolute value required to flat the position", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 1m, 0.05m);
	}

	/// <summary>
	/// Candle type for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA period for the MACD block.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow EMA period for the MACD block.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal EMA period for the MACD block.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Weight of the MACD voting block.
	/// </summary>
	public decimal MacdWeight
	{
		get => _macdWeight.Value;
		set => _macdWeight.Value = value;
	}

	/// <summary>
	/// Short period used by the Awesome Oscillator.
	/// </summary>
	public int AoShortPeriod
	{
		get => _aoShort.Value;
		set => _aoShort.Value = value;
	}

	/// <summary>
	/// Long period used by the Awesome Oscillator.
	/// </summary>
	public int AoLongPeriod
	{
		get => _aoLong.Value;
		set => _aoLong.Value = value;
	}

	/// <summary>
	/// Weight of the Awesome Oscillator block.
	/// </summary>
	public decimal AoWeight
	{
		get => _aoWeight.Value;
		set => _aoWeight.Value = value;
	}

	/// <summary>
	/// Fast EMA period for the OsMA histogram.
	/// </summary>
	public int OsmaFastPeriod
	{
		get => _osmaFast.Value;
		set => _osmaFast.Value = value;
	}

	/// <summary>
	/// Slow EMA period for the OsMA histogram.
	/// </summary>
	public int OsmaSlowPeriod
	{
		get => _osmaSlow.Value;
		set => _osmaSlow.Value = value;
	}

	/// <summary>
	/// Signal EMA period for the OsMA histogram.
	/// </summary>
	public int OsmaSignalPeriod
	{
		get => _osmaSignal.Value;
		set => _osmaSignal.Value = value;
	}

	/// <summary>
	/// Weight of the OsMA voting block.
	/// </summary>
	public decimal OsmaWeight
	{
		get => _osmaWeight.Value;
		set => _osmaWeight.Value = value;
	}

	/// <summary>
	/// Lookback length for Williams %R.
	/// </summary>
	public int WilliamsPeriod
	{
		get => _williamsPeriod.Value;
		set => _williamsPeriod.Value = value;
	}

	/// <summary>
	/// Oversold level for Williams %R.
	/// </summary>
	public decimal WilliamsLowerLevel
	{
		get => _williamsLower.Value;
		set => _williamsLower.Value = value;
	}

	/// <summary>
	/// Overbought level for Williams %R.
	/// </summary>
	public decimal WilliamsUpperLevel
	{
		get => _williamsUpper.Value;
		set => _williamsUpper.Value = value;
	}

	/// <summary>
	/// Weight of the Williams %R block.
	/// </summary>
	public decimal WilliamsWeight
	{
		get => _williamsWeight.Value;
		set => _williamsWeight.Value = value;
	}

	/// <summary>
	/// %K period for the Stochastic Oscillator.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	/// <summary>
	/// %D period for the Stochastic Oscillator.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing factor applied to %K.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochSlowing.Value;
		set => _stochSlowing.Value = value;
	}

	/// <summary>
	/// Oversold threshold for the Stochastic Oscillator.
	/// </summary>
	public decimal StochasticLowerLevel
	{
		get => _stochLower.Value;
		set => _stochLower.Value = value;
	}

	/// <summary>
	/// Overbought threshold for the Stochastic Oscillator.
	/// </summary>
	public decimal StochasticUpperLevel
	{
		get => _stochUpper.Value;
		set => _stochUpper.Value = value;
	}

	/// <summary>
	/// Weight of the Stochastic voting block.
	/// </summary>
	public decimal StochasticWeight
	{
		get => _stochWeight.Value;
		set => _stochWeight.Value = value;
	}

	/// <summary>
	/// Minimum aggregated score required to open a position.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	/// <summary>
	/// Maximum absolute score to keep an existing position open.
	/// </summary>
	public decimal ExitThreshold
	{
		get => _exitThreshold.Value;
		set => _exitThreshold.Value = value;
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

		_prevMacdMain = null;
		_prevMacdSignal = null;
		_prevOsma = null;

		_prevAo = null;
		_prevPrevAo = null;

		_prevWilliams = null;
		_prevPrevWilliams = null;

		_prevStochK = null;
		_prevPrevStochK = null;
		_prevStochSignal = null;

		_lastSignal = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow }
			},
			SignalMa = { Length = MacdSignal }
		};

		var osma = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = OsmaFastPeriod },
				LongMa = { Length = OsmaSlowPeriod }
			},
			SignalMa = { Length = OsmaSignalPeriod }
		};

		var awesome = new AwesomeOscillator
		{
			ShortPeriod = AoShortPeriod,
			LongPeriod = AoLongPeriod
		};

		var williams = new WilliamsR { Length = WilliamsPeriod };

		var stochastic = new StochasticOscillator
		{
			Length = StochasticKPeriod,
			K = { Length = StochasticSlowing },
			D = { Length = StochasticDPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, osma, awesome, williams, stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawIndicator(area, awesome);
			DrawIndicator(area, osma);

			var extraArea = CreateChartArea();
			if (extraArea != null)
			{
				DrawIndicator(extraArea, williams);
				DrawIndicator(extraArea, stochastic);
			}

			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue macdValue,
		IIndicatorValue osmaValue,
		IIndicatorValue awesomeValue,
		IIndicatorValue williamsValue,
		IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal || !osmaValue.IsFinal || !awesomeValue.IsFinal || !williamsValue.IsFinal || !stochasticValue.IsFinal)
			return;

		var macdSignal = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var osmaSignal = (MovingAverageConvergenceDivergenceSignalValue)osmaValue;

		if (macdSignal.Macd is not decimal currentMacd || macdSignal.Signal is not decimal currentMacdSignal)
			return;

		var currentOsma = osmaSignal.Macd is decimal osmaMacd && osmaSignal.Signal is decimal osmaSignalLine
			? osmaMacd - osmaSignalLine
			: (decimal?)null;

		if (currentOsma is null)
			return;

		var currentAo = awesomeValue.ToDecimal();
		var currentWilliams = williamsValue.ToDecimal();

		var stoch = (StochasticOscillatorValue)stochasticValue;
		if (stoch.K is not decimal currentStochK || stoch.D is not decimal currentStochD)
			return;

		decimal signal = 0m;

		if (_prevMacdMain is decimal prevMacd && _prevMacdSignal is decimal prevSignal)
		{
			var mainScore = prevMacd > 0m ? 1m : prevMacd < 0m ? -1m : 0m;
			var crossScore = prevMacd > prevSignal ? 1m : prevMacd < prevSignal ? -1m : 0m;
			signal += (mainScore + crossScore) / 2m * MacdWeight;
		}

		if (_prevAo is decimal prevAo)
		{
			var directionScore = prevAo > 0m ? 1m : prevAo < 0m ? -1m : 0m;
			var momentumScore = _prevPrevAo is decimal prevPrevAo
				? prevAo > prevPrevAo ? 1m : prevAo < prevPrevAo ? -1m : 0m
				: 0m;
			signal += (directionScore + momentumScore) / 2m * AoWeight;
		}

		if (_prevOsma is decimal prevOsma)
		{
			var osmaScore = prevOsma > 0m ? 1m : prevOsma < 0m ? -1m : 0m;
			signal += osmaScore * OsmaWeight;
		}

		if (_prevWilliams is decimal prevWilliams)
		{
			var wprScore = 0m;
			if (_prevPrevWilliams is decimal prevPrevWilliams)
			{
				if (prevWilliams > WilliamsLowerLevel && prevPrevWilliams <= WilliamsLowerLevel)
					wprScore = 1m;
				else if (prevWilliams < WilliamsUpperLevel && prevPrevWilliams >= WilliamsUpperLevel)
					wprScore = -1m;
			}

			signal += wprScore * WilliamsWeight;
		}

		if (_prevStochK is decimal prevStochK && _prevStochSignal is decimal prevStochSignal)
		{
			var stochScore1 = 0m;
			if (_prevPrevStochK is decimal prevPrevStochK)
			{
				if (prevStochK > StochasticLowerLevel && prevPrevStochK <= StochasticLowerLevel)
					stochScore1 = 1m;
				else if (prevStochK < StochasticUpperLevel && prevPrevStochK >= StochasticUpperLevel)
					stochScore1 = -1m;
			}

			var stochScore2 = prevStochK > prevStochSignal ? 1m : prevStochK < prevStochSignal ? -1m : 0m;
			signal += (stochScore1 + stochScore2) / 2m * StochasticWeight;
		}

		_lastSignal = signal;

		ExecuteTradingLogic(signal);

		_prevMacdMain = currentMacd;
		_prevMacdSignal = currentMacdSignal;
		_prevOsma = currentOsma;

		_prevPrevAo = _prevAo;
		_prevAo = currentAo;

		_prevPrevWilliams = _prevWilliams;
		_prevWilliams = currentWilliams;

		_prevPrevStochK = _prevStochK;
		_prevStochK = currentStochK;
		_prevStochSignal = currentStochD;
	}

	private void ExecuteTradingLogic(decimal signal)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Volume <= 0)
			return;

		if (signal >= EntryThreshold)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));

			return;
		}

		if (signal <= -EntryThreshold)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));

			return;
		}

		if (Math.Abs(signal) <= ExitThreshold && Position != 0)
		{
			if (Position > 0)
				SellMarket(Position);
			else
				BuyMarket(Math.Abs(Position));
		}
	}
}

