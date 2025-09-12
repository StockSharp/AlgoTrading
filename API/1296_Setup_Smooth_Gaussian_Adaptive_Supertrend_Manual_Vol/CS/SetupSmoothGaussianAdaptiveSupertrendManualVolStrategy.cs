using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy entering long when the close is above a double-smoothed moving average trend line.
/// </summary>
public class SetupSmoothGaussianAdaptiveSupertrendManualVolStrategy : Strategy
{
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _volatility;
	private readonly StrategyParam<bool> _enableVolatilityFilter;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma1;
	private SimpleMovingAverage _sma2;

	/// <summary>
	/// Length for the smoothing averages.
	/// </summary>
	public int TrendLength { get => _trendLength.Value; set => _trendLength.Value = value; }

	/// <summary>
	/// Manual volatility value used by the filter.
	/// </summary>
	public int Volatility { get => _volatility.Value; set => _volatility.Value = value; }

	/// <summary>
	/// Enables the volatility filter.
	/// </summary>
	public bool EnableVolatilityFilter { get => _enableVolatilityFilter.Value; set => _enableVolatilityFilter.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="SetupSmoothGaussianAdaptiveSupertrendManualVolStrategy"/>.
	/// </summary>
	public SetupSmoothGaussianAdaptiveSupertrendManualVolStrategy()
	{
		_trendLength = Param(nameof(TrendLength), 75)
			.SetGreaterThanZero()
			.SetDisplay("Trend Length", "Smooth Gaussian trend length", "Parameters")
			.SetCanOptimize(true);

		_volatility = Param(nameof(Volatility), 2)
			.SetDisplay("Volatility", "Manual volatility value", "Parameters")
			.SetCanOptimize(true);

		_enableVolatilityFilter = Param(nameof(EnableVolatilityFilter), true)
			.SetDisplay("Enable Volatility Filter", "Use manual volatility filter", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for strategy", "General");
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
		_sma1?.Reset();
		_sma2?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma1 = new SimpleMovingAverage { Length = TrendLength };
		_sma2 = new SimpleMovingAverage { Length = TrendLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var sma1Value = _sma1.Process(candle);
		var trendValue = _sma2.Process(candle.Time, sma1Value.GetValue<decimal>());
		if (!trendValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var trend = trendValue.GetValue<decimal>();
		var validVolatility = !EnableVolatilityFilter || Volatility == 2 || Volatility == 3;

		if (candle.ClosePrice > trend && Position <= 0 && validVolatility)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (candle.ClosePrice < trend && Position > 0)
		{
			SellMarket(Position);
		}
	}
}
