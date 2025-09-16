using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Billy Expert strategy converted from MetaTrader 5 Expert Advisor.
/// Focuses on buying during pullbacks confirmed by dual timeframe Stochastic signals.
/// </summary>
public class BillyExpertStrategy : Strategy
{
	private const decimal VolumeTolerance = 0.0000001m;

	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TimeSpan> _stochasticTimeFrame1;
	private readonly StrategyParam<TimeSpan> _stochasticTimeFrame2;

	private StochasticOscillator _fastStochastic = null!;
	private StochasticOscillator _slowStochastic = null!;

	private decimal _open1;
	private decimal _open2;
	private decimal _open3;
	private decimal _open4;

	private decimal _high1;
	private decimal _high2;
	private decimal _high3;
	private decimal _high4;

	private int _historyCount;

	private decimal _fastMainCurrent;
	private decimal _fastMainPrevious;
	private decimal _fastSignalCurrent;
	private decimal _fastSignalPrevious;
	private bool _fastHasCurrent;
	private bool _fastHasPrevious;

	private decimal _slowMainCurrent;
	private decimal _slowMainPrevious;
	private decimal _slowSignalCurrent;
	private decimal _slowSignalPrevious;
	private bool _slowHasCurrent;
	private bool _slowHasPrevious;

	private decimal _pipSize;

	/// <summary>
	/// Trade volume used for each entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneous long entries.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Primary candle type that drives the price pattern checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Timeframe for the faster Stochastic oscillator.
	/// </summary>
	public TimeSpan StochasticTimeFrame1
	{
		get => _stochasticTimeFrame1.Value;
		set => _stochasticTimeFrame1.Value = value;
	}

	/// <summary>
	/// Timeframe for the slower Stochastic oscillator.
	/// </summary>
	public TimeSpan StochasticTimeFrame2
	{
		get => _stochasticTimeFrame2.Value;
		set => _stochasticTimeFrame2.Value = value;
	}

	/// <summary>
	/// Initializes parameters for the strategy.
	/// </summary>
	public BillyExpertStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Order size for each entry", "General");

		_stopLossPips = Param(nameof(StopLossPips), 0)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 32)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk");

		_maxPositions = Param(nameof(MaxPositions), 6)
		.SetGreaterThanZero()
		.SetDisplay("Max Positions", "Maximum number of open trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Signal Candle", "Primary timeframe used for price filters", "General");

		_stochasticTimeFrame1 = Param(nameof(StochasticTimeFrame1), TimeSpan.FromMinutes(5))
		.SetDisplay("Fast Stochastic TF", "Timeframe for the fast Stochastic", "Indicators");

		_stochasticTimeFrame2 = Param(nameof(StochasticTimeFrame2), TimeSpan.FromMinutes(6))
		.SetDisplay("Slow Stochastic TF", "Timeframe for the slow Stochastic", "Indicators");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
		(Security, CandleType),
		(Security, StochasticTimeFrame1.TimeFrame()),
		(Security, StochasticTimeFrame2.TimeFrame())
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_open1 = _open2 = _open3 = _open4 = 0m;
		_high1 = _high2 = _high3 = _high4 = 0m;
		_historyCount = 0;

		_fastMainCurrent = _fastMainPrevious = 0m;
		_fastSignalCurrent = _fastSignalPrevious = 0m;
		_fastHasCurrent = false;
		_fastHasPrevious = false;

		_slowMainCurrent = _slowMainPrevious = 0m;
		_slowSignalCurrent = _slowSignalPrevious = 0m;
		_slowHasCurrent = false;
		_slowHasPrevious = false;

		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (StochasticTimeFrame1 >= StochasticTimeFrame2)
		{
			LogError("Fast stochastic timeframe must be shorter than the slow timeframe.");
			Stop();
			return;
		}

		Volume = TradeVolume;

		_fastStochastic = new StochasticOscillator { KPeriod = 5, DPeriod = 3, Smooth = 3 };
		_slowStochastic = new StochasticOscillator { KPeriod = 5, DPeriod = 3, Smooth = 3 };

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
		.Bind(ProcessSignalCandle)
		.Start();

		var fastSubscription = SubscribeCandles(StochasticTimeFrame1.TimeFrame());
		fastSubscription
		.BindEx(_fastStochastic, ProcessFastStochastic)
		.Start();

		var slowSubscription = SubscribeCandles(StochasticTimeFrame2.TimeFrame());
		slowSubscription
		.BindEx(_slowStochastic, ProcessSlowStochastic)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSubscription);
			DrawOwnTrades(area);
		}

		_pipSize = CalculatePipSize();

		var takeProfit = TakeProfitPips > 0 ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Price) : null;
		var stopLoss = StopLossPips > 0 ? new Unit(StopLossPips * _pipSize, UnitTypes.Price) : null;

		if (takeProfit != null || stopLoss != null)
		{
			StartProtection(takeProfit, stopLoss);
		}
	}

	private void ProcessFastStochastic(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!value.IsFinal)
			return;

		if (!_fastStochastic.IsFormed)
			return;

		if (_fastHasCurrent)
		{
			_fastMainPrevious = _fastMainCurrent;
			_fastSignalPrevious = _fastSignalCurrent;
			_fastHasPrevious = true;
		}

		var typed = (StochasticOscillatorValue)value;
		_fastMainCurrent = typed.K;
		_fastSignalCurrent = typed.D;
		_fastHasCurrent = true;
	}

	private void ProcessSlowStochastic(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!value.IsFinal)
			return;

		if (!_slowStochastic.IsFormed)
			return;

		if (_slowHasCurrent)
		{
			_slowMainPrevious = _slowMainCurrent;
			_slowSignalPrevious = _slowSignalCurrent;
			_slowHasPrevious = true;
		}

		var typed = (StochasticOscillatorValue)value;
		_slowMainCurrent = typed.K;
		_slowSignalCurrent = typed.D;
		_slowHasCurrent = true;
	}

	private void ProcessSignalCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_historyCount >= 4 && _fastHasPrevious && _slowHasPrevious)
		{
			var decreasingHighs = _high1 < _high2 && _high2 < _high3 && _high3 < _high4;
			var decreasingOpens = _open1 < _open2 && _open2 < _open3 && _open3 < _open4;
			var fastBullish = _fastMainPrevious > _fastSignalPrevious && _fastMainCurrent > _fastSignalCurrent;
			var slowBullish = _slowMainPrevious > _slowSignalPrevious && _slowMainCurrent > _slowSignalCurrent;

			var maxLongVolume = MaxPositions * TradeVolume;
			var currentLongVolume = Math.Max(Position, 0m);
			var projectedVolume = currentLongVolume + TradeVolume;

			if (decreasingHighs && decreasingOpens && fastBullish && slowBullish && projectedVolume <= maxLongVolume + VolumeTolerance)
			{
				if (IsFormedAndOnlineAndAllowTrading())
				{
					BuyMarket(TradeVolume);
				}
			}
		}

		_high4 = _high3;
		_high3 = _high2;
		_high2 = _high1;
		_high1 = candle.HighPrice;

		_open4 = _open3;
		_open3 = _open2;
		_open2 = _open1;
		_open1 = candle.OpenPrice;

		if (_historyCount < 4)
		{
			_historyCount++;
		}
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;

		if (priceStep <= 0m)
			return 1m;

		var decimals = GetDecimalPlaces(priceStep);
		var adjust = decimals == 3 || decimals == 5 ? 10m : 1m;

		return priceStep * adjust;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}
}
