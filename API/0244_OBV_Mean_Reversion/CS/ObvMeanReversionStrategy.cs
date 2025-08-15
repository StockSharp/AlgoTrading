using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// OBV Mean Reversion Strategy (244).
/// Enter when OBV deviates from its average by a certain multiple of standard deviation.
/// Exit when OBV returns to its average.
/// </summary>
public class ObvMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _averagePeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private OnBalanceVolume _obv;
	private SimpleMovingAverage _obvAverage;
	private StandardDeviation _obvStdDev;
	
	private decimal? _currentObv;
	private decimal? _obvAvgValue;
	private decimal? _obvStdDevValue;

	/// <summary>
	/// Period for OBV average calculation.
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
	/// Initializes a new instance of the <see cref="ObvMeanReversionStrategy"/>.
	/// </summary>
	public ObvMeanReversionStrategy()
	{
		_averagePeriod = Param(nameof(AveragePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Average Period", "Period for OBV average calculation", "Strategy Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);

		_multiplier = Param(nameof(Multiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Multiplier", "Standard deviation multiplier for entry", "Strategy Parameters")
			.SetCanOptimize(true)
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

		_currentObv = default;
		_obvAvgValue = default;
		_obvStdDevValue = default;
	}


	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators
		_obv = new OnBalanceVolume();
		_obvAverage = new SimpleMovingAverage { Length = AveragePeriod };
		_obvStdDev = new StandardDeviation { Length = AveragePeriod };

		// Create candle subscription
		var subscription = SubscribeCandles(CandleType);

		// Create processing chain
		subscription
			.BindEx(_obv, ProcessObv)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _obv);
			DrawOwnTrades(area);
		}

		// Enable position protection
		StartProtection(
			takeProfit: new Unit(5, UnitTypes.Percent),
			stopLoss: new Unit(2, UnitTypes.Percent)
		);
	}

	private void ProcessObv(ICandleMessage candle, IIndicatorValue obvValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Extract OBV value
		_currentObv = obvValue.ToDecimal();

		// Process OBV through average and standard deviation indicators
		var avgIndicatorValue = _obvAverage.Process(obvValue);
		var stdDevIndicatorValue = _obvStdDev.Process(obvValue);
		
		_obvAvgValue = avgIndicatorValue.ToDecimal();
		_obvStdDevValue = stdDevIndicatorValue.ToDecimal();
		
		// Check if strategy is ready for trading
		if (!IsFormedAndOnlineAndAllowTrading() || !_obvAverage.IsFormed || !_obvStdDev.IsFormed)
			return;

		// Ensure we have all needed values
		if (!_currentObv.HasValue || !_obvAvgValue.HasValue || !_obvStdDevValue.HasValue)
			return;

		// Calculate bands
		var upperBand = _obvAvgValue.Value + Multiplier * _obvStdDevValue.Value;
		var lowerBand = _obvAvgValue.Value - Multiplier * _obvStdDevValue.Value;

		LogInfo($"OBV: {_currentObv}, OBV Avg: {_obvAvgValue}, Upper: {upperBand}, Lower: {lowerBand}");

		// Entry logic
		if (Position == 0)
		{
			// Long Entry: OBV is below lower band (OBV oversold)
			if (_currentObv.Value < lowerBand)
			{
				LogInfo($"Buy Signal - OBV ({_currentObv}) < Lower Band ({lowerBand})");
				BuyMarket(Volume);
			}
			// Short Entry: OBV is above upper band (OBV overbought)
			else if (_currentObv.Value > upperBand)
			{
				LogInfo($"Sell Signal - OBV ({_currentObv}) > Upper Band ({upperBand})");
				SellMarket(Volume);
			}
		}
		// Exit logic
		else if (Position > 0 && _currentObv.Value > _obvAvgValue.Value)
		{
			// Exit Long: OBV returned to average
			LogInfo($"Exit Long - OBV ({_currentObv}) > OBV Avg ({_obvAvgValue})");
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && _currentObv.Value < _obvAvgValue.Value)
		{
			// Exit Short: OBV returned to average
			LogInfo($"Exit Short - OBV ({_currentObv}) < OBV Avg ({_obvAvgValue})");
			BuyMarket(Math.Abs(Position));
		}
	}
}
