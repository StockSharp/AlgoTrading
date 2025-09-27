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
/// Stochastic crossover strategy that reacts on OHLC momentum and maintains a trailing stop.
/// The strategy buys when the %K line crosses above %D in the oversold area and sells on the opposite setup.
/// It also manages a trailing stop measured in price steps to protect profits.
/// </summary>
public class OhlcStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<decimal> _levelUp;
	private readonly StrategyParam<decimal> _levelDown;
	private readonly StrategyParam<decimal> _trailingStopSteps;
	private readonly StrategyParam<decimal> _trailingStepSteps;

	private decimal? _entryPrice;
	private decimal? _trailingStopPrice;
	private Sides? _currentSide;

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for the %K calculation.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Period for the %D smoothing.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Final smoothing period for %K.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Overbought level that filters short entries.
	/// </summary>
	public decimal LevelUp
	{
		get => _levelUp.Value;
		set => _levelUp.Value = value;
	}

	/// <summary>
	/// Oversold level that filters long entries.
	/// </summary>
	public decimal LevelDown
	{
		get => _levelDown.Value;
		set => _levelDown.Value = value;
	}

	/// <summary>
	/// Distance for the trailing stop expressed in price steps.
	/// </summary>
	public decimal TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
	}

	/// <summary>
	/// Minimal improvement required before the trailing stop is moved.
	/// </summary>
	public decimal TrailingStepSteps
	{
		get => _trailingStepSteps.Value;
		set => _trailingStepSteps.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OhlcStochasticStrategy"/>.
	/// </summary>
	public OhlcStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(12).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for analysis", "General");

		_kPeriod = Param(nameof(KPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("%K Period", "Number of bars for %K", "Stochastic")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_dPeriod = Param(nameof(DPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("%D Period", "Smoothing period for %D", "Stochastic")
		.SetCanOptimize(true)
		.SetOptimize(2, 10, 1);

		_slowing = Param(nameof(Slowing), 3)
		.SetGreaterThanZero()
		.SetDisplay("Slowing", "Final smoothing for %K", "Stochastic")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_levelUp = Param(nameof(LevelUp), 70m)
		.SetNotNegative()
		.SetDisplay("Overbought", "Trigger level for shorts", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(60m, 90m, 5m);

		_levelDown = Param(nameof(LevelDown), 30m)
		.SetNotNegative()
		.SetDisplay("Oversold", "Trigger level for longs", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(10m, 40m, 5m);

		_trailingStopSteps = Param(nameof(TrailingStopSteps), 5m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop Steps", "Stop distance in price steps", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 20m, 1m);

		_trailingStepSteps = Param(nameof(TrailingStepSteps), 2m)
		.SetNotNegative()
		.SetDisplay("Trailing Step Steps", "Minimal progress before trailing", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 10m, 1m);
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

		ResetTrailing();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var stochastic = new StochasticOscillator
		{
			KPeriod = KPeriod,
			DPeriod = DPeriod,
			Slowing = Slowing
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
		.BindEx(stochastic, ProcessCandle)
		.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var stochValue = (StochasticOscillatorValue)stochasticValue;
		if (stochValue.K is not decimal k || stochValue.D is not decimal d)
		return;

		var longSignal = k > d && (k < LevelDown || d < LevelDown);
		var shortSignal = k < d && (k > LevelUp || d > LevelUp);

		if (longSignal && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			SetupTrailing(Sides.Buy, candle.ClosePrice);
		}
		else if (shortSignal && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			SetupTrailing(Sides.Sell, candle.ClosePrice);
		}
		else
		{
			UpdateTrailing(candle);
		}
	}

	private void SetupTrailing(Sides side, decimal entryPrice)
	{
		_currentSide = side;
		_entryPrice = entryPrice;

		var stopDistance = GetTrailingStopDistance();
		if (stopDistance <= 0)
		{
			_trailingStopPrice = null;
			return;
		}

		_trailingStopPrice = side == Sides.Buy
		? entryPrice - stopDistance
		: entryPrice + stopDistance;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (_currentSide == null || _entryPrice == null)
		return;

		var stopDistance = GetTrailingStopDistance();
		if (stopDistance <= 0)
		{
			return;
		}

		var stepDistance = GetTrailingStepDistance();

		if (_currentSide == Sides.Buy)
		{
			if (Position <= 0)
			{
				ResetTrailing();
				return;
			}

			var priceAdvance = candle.ClosePrice - _entryPrice.Value;
			if (priceAdvance > stopDistance + stepDistance)
			{
				var newStop = candle.ClosePrice - stopDistance;
				if (_trailingStopPrice == null || newStop - _trailingStopPrice.Value >= stepDistance)
				_trailingStopPrice = newStop;
			}

			if (_trailingStopPrice != null && candle.LowPrice <= _trailingStopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetTrailing();
			}
		}
		else if (_currentSide == Sides.Sell)
		{
			if (Position >= 0)
			{
				ResetTrailing();
				return;
			}

			var priceAdvance = _entryPrice.Value - candle.ClosePrice;
			if (priceAdvance > stopDistance + stepDistance)
			{
				var newStop = candle.ClosePrice + stopDistance;
				if (_trailingStopPrice == null || _trailingStopPrice.Value - newStop >= stepDistance)
				_trailingStopPrice = newStop;
			}

			if (_trailingStopPrice != null && candle.HighPrice >= _trailingStopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetTrailing();
			}
		}
	}

	private decimal GetTrailingStopDistance()
	{
		var step = Security?.PriceStep ?? 1m;
		return step * TrailingStopSteps;
	}

	private decimal GetTrailingStepDistance()
	{
		var step = Security?.PriceStep ?? 1m;
		return step * TrailingStepSteps;
	}

	private void ResetTrailing()
	{
		_entryPrice = null;
		_trailingStopPrice = null;
		_currentSide = null;
	}
}

