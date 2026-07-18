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
/// Stochastic Mean Reversion Strategy.
/// Enter when Stochastic %K deviates from its average by a certain multiple of standard deviation.
/// Exit when Stochastic %K returns to its average.
/// </summary>
public class StochasticMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _averagePeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private StochasticOscillator _stochastic;
	private SimpleMovingAverage _stochAverage;
	private StandardDeviation _stochStdDev;
	
	private decimal _prevStochKValue;

	/// <summary>
	/// Stochastic period.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Period for Stochastic average calculation.
	/// </summary>
	public int AveragePeriod
	{
		get => _averagePeriod.Value;
		set => _averagePeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for entry.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StochasticMeanReversionStrategy"/>.
	/// </summary>
	public StochasticMeanReversionStrategy()
	{
		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Period for Stochastic calculation", "Strategy Parameters")
			
			.SetOptimize(10, 20, 2);

		_kPeriod = Param(nameof(KPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("K Period", "Period for %K calculation", "Strategy Parameters")
			
			.SetOptimize(2, 5, 1);

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "Period for %D calculation", "Strategy Parameters")
			
			.SetOptimize(2, 5, 1);

		_averagePeriod = Param(nameof(AveragePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Average Period", "Period for Stochastic average calculation", "Strategy Parameters")
			
			.SetOptimize(10, 30, 5);

		_multiplier = Param(nameof(Multiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Multiplier", "Standard deviation multiplier for entry", "Strategy Parameters")
			
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Strategy Parameters");
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
		_stochastic = null;
		_stochAverage = null;
		_stochStdDev = null;
		_prevStochKValue = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);


		_stochastic = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod }
		};

		_stochAverage = new SimpleMovingAverage { Length = AveragePeriod };
		_stochStdDev = new StandardDeviation { Length = AveragePeriod };

		Indicators.Add(_stochastic);
		Indicators.Add(_stochAverage);
		Indicators.Add(_stochStdDev);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessStochastic)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessStochastic(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stochResult = _stochastic.Process(candle);
		if (!_stochastic.IsFormed)
			return;

		if (stochResult is not StochasticOscillatorValue stochTyped || stochTyped.K is not decimal kValue)
			return;

		var stochAvgValue = _stochAverage.Process(new DecimalIndicatorValue(_stochAverage, kValue, candle.OpenTime) { IsFinal = true }).ToDecimal();
		var stochStdDevValue = _stochStdDev.Process(new DecimalIndicatorValue(_stochStdDev, kValue, candle.OpenTime) { IsFinal = true }).ToDecimal();

		if (!_stochAverage.IsFormed || !_stochStdDev.IsFormed)
		{
			_prevStochKValue = kValue;
			return;
		}

		var effectiveStdDev = Math.Max(1m, stochStdDevValue);
		var upperBand = stochAvgValue + Multiplier * effectiveStdDev;
		var lowerBand = stochAvgValue - Multiplier * effectiveStdDev;

		if (Position == 0)
		{
			if (kValue < lowerBand || kValue < 20m)
				BuyMarket();
			else if (kValue > upperBand || kValue > 80m)
				SellMarket();
		}

		_prevStochKValue = kValue;
	}
}
