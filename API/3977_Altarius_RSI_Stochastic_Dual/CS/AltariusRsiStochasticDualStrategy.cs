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
/// Strategy converted from AltariusRSIxampnSTOH MQL4 expert advisor.
/// Combines dual stochastic filters with RSI based exits and dynamic position sizing.
/// </summary>
public class AltariusRsiStochasticDualStrategy : Strategy
{
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _maximumRiskPercent;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _slowStochPeriod;
	private readonly StrategyParam<int> _slowStochK;
	private readonly StrategyParam<int> _slowStochD;
	private readonly StrategyParam<int> _fastStochPeriod;
	private readonly StrategyParam<int> _fastStochK;
	private readonly StrategyParam<int> _fastStochD;
	private readonly StrategyParam<decimal> _differenceThreshold;
	private readonly StrategyParam<decimal> _buyLimit;
	private readonly StrategyParam<decimal> _sellLimit;
	private readonly StrategyParam<decimal> _exitRsiHigh;
	private readonly StrategyParam<decimal> _exitRsiLow;
	private readonly StrategyParam<decimal> _exitStochHigh;
	private readonly StrategyParam<decimal> _exitStochLow;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousSlowSignal;
	private bool _hasPreviousSlowSignal;
	private decimal _lastRealizedPnL;
	private int _consecutiveLosses;
	/// <summary>
	/// Base order volume before risk and loss adjustments.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Maximum share of account equity allowed to be lost before forcing an exit.
	/// </summary>
	public decimal MaximumRiskPercent
	{
		get => _maximumRiskPercent.Value;
		set => _maximumRiskPercent.Value = value;
	}

	/// <summary>
	/// Factor controlling how quickly the volume shrinks after consecutive losses.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Period for the RSI exit filter.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Period for the slow stochastic oscillator used to open trades.
	/// </summary>
	public int SlowStochasticPeriod
	{
		get => _slowStochPeriod.Value;
		set => _slowStochPeriod.Value = value;
	}

	/// <summary>
	/// %K smoothing length for the slow stochastic.
	/// </summary>
	public int SlowStochasticK
	{
		get => _slowStochK.Value;
		set => _slowStochK.Value = value;
	}

	/// <summary>
	/// %D smoothing length for the slow stochastic.
	/// </summary>
	public int SlowStochasticD
	{
		get => _slowStochD.Value;
		set => _slowStochD.Value = value;
	}

	/// <summary>
	/// Period for the fast stochastic oscillator used as momentum filter.
	/// </summary>
	public int FastStochasticPeriod
	{
		get => _fastStochPeriod.Value;
		set => _fastStochPeriod.Value = value;
	}

	/// <summary>
	/// %K smoothing length for the fast stochastic.
	/// </summary>
	public int FastStochasticK
	{
		get => _fastStochK.Value;
		set => _fastStochK.Value = value;
	}

	/// <summary>
	/// %D smoothing length for the fast stochastic.
	/// </summary>
	public int FastStochasticD
	{
		get => _fastStochD.Value;
		set => _fastStochD.Value = value;
	}

	/// <summary>
	/// Minimum distance between fast stochastic main and signal lines to allow entries.
	/// </summary>
	public decimal StochasticDifferenceThreshold
	{
		get => _differenceThreshold.Value;
		set => _differenceThreshold.Value = value;
	}

	/// <summary>
	/// Upper bound on the slow stochastic main line when opening long trades.
	/// </summary>
	public decimal BuyStochasticLimit
	{
		get => _buyLimit.Value;
		set => _buyLimit.Value = value;
	}

	/// <summary>
	/// Lower bound on the slow stochastic main line when opening short trades.
	/// </summary>
	public decimal SellStochasticLimit
	{
		get => _sellLimit.Value;
		set => _sellLimit.Value = value;
	}

	/// <summary>
	/// RSI threshold that triggers exit for long positions.
	/// </summary>
	public decimal ExitRsiHigh
	{
		get => _exitRsiHigh.Value;
		set => _exitRsiHigh.Value = value;
	}

	/// <summary>
	/// RSI threshold that triggers exit for short positions.
	/// </summary>
	public decimal ExitRsiLow
	{
		get => _exitRsiLow.Value;
		set => _exitRsiLow.Value = value;
	}

	/// <summary>
	/// Stochastic level that confirms the exit of long positions.
	/// </summary>
	public decimal ExitStochasticHigh
	{
		get => _exitStochHigh.Value;
		set => _exitStochHigh.Value = value;
	}

	/// <summary>
	/// Stochastic level that confirms the exit of short positions.
	/// </summary>
	public decimal ExitStochasticLow
	{
		get => _exitStochLow.Value;
		set => _exitStochLow.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public AltariusRsiStochasticDualStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 1m)
		.SetNotNegative()
		.SetDisplay("Base Volume", "Initial volume before money management rules", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 2m, 0.1m);

		_maximumRiskPercent = Param(nameof(MaximumRiskPercent), 0.1m)
		.SetNotNegative()
		.SetDisplay("Max Risk %", "Equity drawdown percentage that forces position closure", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.05m, 0.3m, 0.05m);

		_decreaseFactor = Param(nameof(DecreaseFactor), 3m)
		.SetNotNegative()
		.SetDisplay("Decrease Factor", "Loss streak divider applied to volume", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 1m);

		_rsiPeriod = Param(nameof(RsiPeriod), 4)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Length of RSI used for exits", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(2, 8, 1);

		_slowStochPeriod = Param(nameof(SlowStochasticPeriod), 15)
		.SetGreaterThanZero()
		.SetDisplay("Slow Stochastic Period", "Main period of slow stochastic", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 25, 1);

		_slowStochK = Param(nameof(SlowStochasticK), 8)
		.SetGreaterThanZero()
		.SetDisplay("Slow Stochastic %K", "Smoothing of %K for slow stochastic", "Indicators");

		_slowStochD = Param(nameof(SlowStochasticD), 8)
		.SetGreaterThanZero()
		.SetDisplay("Slow Stochastic %D", "Smoothing of %D for slow stochastic", "Indicators");

		_fastStochPeriod = Param(nameof(FastStochasticPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Fast Stochastic Period", "Main period of fast stochastic", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 15, 1);

		_fastStochK = Param(nameof(FastStochasticK), 3)
		.SetGreaterThanZero()
		.SetDisplay("Fast Stochastic %K", "Smoothing of %K for fast stochastic", "Indicators");

		_fastStochD = Param(nameof(FastStochasticD), 3)
		.SetGreaterThanZero()
		.SetDisplay("Fast Stochastic %D", "Smoothing of %D for fast stochastic", "Indicators");

		_differenceThreshold = Param(nameof(StochasticDifferenceThreshold), 5m)
		.SetNotNegative()
		.SetDisplay("Momentum Threshold", "Minimum difference between fast stochastic lines", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(2m, 10m, 1m);

		_buyLimit = Param(nameof(BuyStochasticLimit), 50m)
		.SetDisplay("Buy Stochastic Limit", "Upper bound of slow stochastic for longs", "Trading");

		_sellLimit = Param(nameof(SellStochasticLimit), 55m)
		.SetDisplay("Sell Stochastic Limit", "Lower bound of slow stochastic for shorts", "Trading");

		_exitRsiHigh = Param(nameof(ExitRsiHigh), 60m)
		.SetDisplay("Exit RSI High", "RSI threshold to exit longs", "Exits");

		_exitRsiLow = Param(nameof(ExitRsiLow), 40m)
		.SetDisplay("Exit RSI Low", "RSI threshold to exit shorts", "Exits");

		_exitStochHigh = Param(nameof(ExitStochasticHigh), 70m)
		.SetDisplay("Exit Stochastic High", "Slow stochastic signal level confirming long exit", "Exits");

		_exitStochLow = Param(nameof(ExitStochasticLow), 30m)
		.SetDisplay("Exit Stochastic Low", "Slow stochastic signal level confirming short exit", "Exits");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Candles used for calculations", "Market Data");
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

		_previousSlowSignal = 0m;
		_hasPreviousSlowSignal = false;
		_lastRealizedPnL = PnLManager?.RealizedPnL ?? 0m;
		_consecutiveLosses = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_lastRealizedPnL = PnLManager?.RealizedPnL ?? 0m;

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod,
		};

		var slowStochastic = new StochasticOscillator
		{
			Length = SlowStochasticPeriod,
			K = { Length = SlowStochasticK },
			D = { Length = SlowStochasticD },
		};

		var fastStochastic = new StochasticOscillator
		{
			Length = FastStochasticPeriod,
			K = { Length = FastStochasticK },
			D = { Length = FastStochasticD },
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
		.BindEx(rsi, slowStochastic, fastStochastic, ProcessIndicators)
		.Start();

		var chartArea = CreateChartArea();
		if (chartArea != null)
		{
			DrawCandles(chartArea, subscription);
			DrawIndicator(chartArea, rsi);
			DrawIndicator(chartArea, slowStochastic);
			DrawIndicator(chartArea, fastStochastic);
			DrawOwnTrades(chartArea);
		}
	}

	private void ProcessIndicators(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue slowValue, IIndicatorValue fastValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (MaximumRiskPercent > 0m)
		{
			var unrealizedPnL = PnLManager?.UnrealizedPnL ?? 0m;
			if (unrealizedPnL < 0m)
			{
				var portfolio = Portfolio;
				var equity = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;
				if (equity > 0m)
				{
					var allowedLoss = equity * MaximumRiskPercent;
					if (Math.Abs(unrealizedPnL) >= allowedLoss)
					{
						CloseCurrentPosition();
						return;
					}
				}
			}
		}

		var rsi = rsiValue.ToDecimal();
		var slow = (StochasticOscillatorValue)slowValue;
		var fast = (StochasticOscillatorValue)fastValue;

		if (slow.K is not decimal slowMainValue ||
		slow.D is not decimal slowSignalValue ||
		fast.K is not decimal fastMainValue ||
		fast.D is not decimal fastSignalValue)
		{
			return;
		}

		if (!_hasPreviousSlowSignal)
		{
			_previousSlowSignal = slowSignalValue;
			_hasPreviousSlowSignal = true;
			return;
		}

		if (Position == 0m)
		{
			var momentum = Math.Abs(fastMainValue - fastSignalValue);
			if (slowMainValue > slowSignalValue && slowMainValue < BuyStochasticLimit && momentum > StochasticDifferenceThreshold)
			{
				EnterPosition(Sides.Buy);
			}
			else if (slowMainValue < slowSignalValue && slowMainValue > SellStochasticLimit && momentum > StochasticDifferenceThreshold)
			{
				EnterPosition(Sides.Sell);
			}
		}
		else if (Position > 0m)
		{
			if (rsi > ExitRsiHigh && slowSignalValue < _previousSlowSignal && slowSignalValue > ExitStochasticHigh)
			{
				ExitPosition(Sides.Buy);
			}
		}
		else if (Position < 0m)
		{
			if (rsi < ExitRsiLow && slowSignalValue > _previousSlowSignal && slowSignalValue < ExitStochasticLow)
			{
				ExitPosition(Sides.Sell);
			}
		}

		_previousSlowSignal = slowSignalValue;
	}

	private void EnterPosition(Sides side)
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
		return;

		if (side == Sides.Buy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}

	}

	private void ExitPosition(Sides side)
	{
		var position = Position;
		if (position == 0m)
		return;

		if (side == Sides.Buy && position > 0m)
		{
			SellMarket(position);
		}
		else if (side == Sides.Sell && position < 0m)
		{
			BuyMarket(Math.Abs(position));
		}
	}

	private void CloseCurrentPosition()
	{
		var position = Position;
		if (position > 0m)
		{
			SellMarket(position);
		}
		else if (position < 0m)
		{
			BuyMarket(Math.Abs(position));
		}
	}

	private decimal CalculateOrderVolume()
	{
		var volume = BaseVolume;

		if (DecreaseFactor > 0m && _consecutiveLosses > 1)
		{
			var reduction = volume * _consecutiveLosses / DecreaseFactor;
			volume -= reduction;
		}

		if (volume <= 0m)
		volume = BaseVolume;

		var security = Security;
		if (security != null)
		{
			var step = security.VolumeStep ?? 0m;
			if (step <= 0m)
			step = 0.1m;

			var minVolume = security.MinVolume ?? step;
			var maxVolume = security.MaxVolume;

			var steps = decimal.Floor(volume / step);
			if (steps < 1m)
			steps = 1m;

			volume = steps * step;

			if (volume < minVolume)
			volume = minVolume;

			if (maxVolume is decimal max && max > 0m && volume > max)
			volume = max;
		}

		return volume;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			var realizedPnL = PnLManager?.RealizedPnL ?? 0m;
			var gain = realizedPnL - _lastRealizedPnL;
			_lastRealizedPnL = realizedPnL;

			if (gain > 0m)
			{
				_consecutiveLosses = 0;
			}
			else if (gain < 0m)
			{
				_consecutiveLosses++;
			}

		}
	}
}

