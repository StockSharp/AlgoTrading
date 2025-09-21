using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Russian20 Time Filter Momentum strategy converted from MetaTrader 4 (Russian20-hp1.mq4).
/// Combines a 20-period simple moving average with a 5-period momentum filter and optional trading hours restriction.
/// </summary>
public class Russian20TimeFilterMomentumStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _movingAverageLength;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;

	private SimpleMovingAverage _movingAverage;
	private Momentum _momentum;
	private decimal? _previousClose;
	private decimal? _entryPrice;
	private decimal _pipSize;
	private decimal _takeProfitOffset;

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period of the simple moving average filter.
	/// </summary>
	public int MovingAverageLength
	{
		get => _movingAverageLength.Value;
		set => _movingAverageLength.Value = value;
	}

	/// <summary>
	/// Lookback period for the momentum indicator.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Neutral momentum level used for entry and exit decisions.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips for both long and short trades.
	/// Set to zero to disable the profit target.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enables the optional trading session filter.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Start hour (inclusive) of the allowed trading window.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// End hour (inclusive) of the allowed trading window.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults aligned with the original expert advisor.
	/// </summary>
	public Russian20TimeFilterMomentumStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for analysis", "General");

		_movingAverageLength = Param(nameof(MovingAverageLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Simple moving average lookback", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_momentumPeriod = Param(nameof(MomentumPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum indicator lookback", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 12, 1);

		_momentumThreshold = Param(nameof(MomentumThreshold), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Threshold", "Neutral momentum level for signals", "Indicators");

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_useTimeFilter = Param(nameof(UseTimeFilter), false)
			.SetDisplay("Use Time Filter", "Restrict trading to a session", "Session");

		_startHour = Param(nameof(StartHour), 14)
			.SetDisplay("Start Hour", "Inclusive start hour of the trading session", "Session")
			.SetRange(0, 23);

		_endHour = Param(nameof(EndHour), 16)
			.SetDisplay("End Hour", "Inclusive end hour of the trading session", "Session")
			.SetRange(0, 23);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousClose = null;
		_entryPrice = null;
		_pipSize = 0m;
		_takeProfitOffset = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdatePipSettings();

		_movingAverage = new SimpleMovingAverage
		{
			Length = MovingAverageLength,
		};

		_momentum = new Momentum
		{
			Length = MomentumPeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_movingAverage, _momentum, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _movingAverage);
			DrawIndicator(area, _momentum);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal momentumValue)
	{
		// Ignore incomplete candles to mirror the bar-close execution of the MQL script.
		if (candle.State != CandleStates.Finished)
			return;

		// Honour trading session boundaries when the filter is enabled.
		if (UseTimeFilter)
		{
			var hour = candle.OpenTime.Hour;
			if (hour < StartHour || hour > EndHour)
			{
				_previousClose = candle.ClosePrice;
				return;
			}
		}

		// Ensure the infrastructure allows trading and indicators are ready.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_movingAverage.IsFormed || !_momentum.IsFormed)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		if (_pipSize == 0m)
			UpdatePipSettings();

		var closePrice = candle.ClosePrice;

		if (_previousClose is null)
		{
			_previousClose = closePrice;
			return;
		}

		var entryPrice = _entryPrice;

		if (Position == 0 && entryPrice.HasValue)
		{
			// Reset entry price if an external action flattened the position.
			_entryPrice = null;
			entryPrice = null;
		}

		if (Position == 0)
		{
			// Evaluate entry conditions only when flat.
			var bullishSignal = closePrice > maValue && momentumValue > MomentumThreshold && closePrice > _previousClose.Value;
			var bearishSignal = closePrice < maValue && momentumValue < MomentumThreshold && closePrice < _previousClose.Value;

			if (bullishSignal)
			{
				// Enter long on a bullish alignment of filters.
				BuyMarket();
				_entryPrice = closePrice;
			}
			else if (bearishSignal)
			{
				// Enter short on a bearish alignment of filters.
				SellMarket();
				_entryPrice = closePrice;
			}
		}
		else if (Position > 0)
		{
			// Exit long when momentum weakens or the take profit target is achieved.
			var exitByMomentum = momentumValue <= MomentumThreshold;
			var exitByTake = entryPrice.HasValue && _takeProfitOffset > 0m && closePrice >= entryPrice.Value + _takeProfitOffset;

			if (exitByMomentum || exitByTake)
			{
				ClosePosition();
				_entryPrice = null;
			}
		}
		else
		{
			// Exit short when momentum strengthens or the profit target is touched.
			var exitByMomentum = momentumValue >= MomentumThreshold;
			var exitByTake = entryPrice.HasValue && _takeProfitOffset > 0m && closePrice <= entryPrice.Value - _takeProfitOffset;

			if (exitByMomentum || exitByTake)
			{
				ClosePosition();
				_entryPrice = null;
			}
		}

		_previousClose = closePrice;
	}

	private void UpdatePipSettings()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
		{
			_pipSize = 1m;
		}
		else
		{
			var decimals = GetDecimalPlaces(step);
			var multiplier = decimals == 3 || decimals == 5 ? 10m : 1m;
			_pipSize = step * multiplier;
		}

		_takeProfitOffset = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}
}
