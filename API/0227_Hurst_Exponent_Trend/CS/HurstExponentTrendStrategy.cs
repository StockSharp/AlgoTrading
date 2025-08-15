using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hurst Exponent Trend strategy.
/// Uses Hurst exponent to identify trending markets.
/// </summary>
public class HurstExponentTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _hurstPeriodParam;
	private readonly StrategyParam<int> _maPeriodParam;
	private readonly StrategyParam<decimal> _hurstThresholdParam;
	private readonly StrategyParam<DataType> _candleTypeParam;

	private HurstExponent _hurst;
	private SimpleMovingAverage _sma;

	/// <summary>
	/// Hurst exponent calculation period.
	/// </summary>
	public int HurstPeriod
	{
		get => _hurstPeriodParam.Value;
		set => _hurstPeriodParam.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriodParam.Value;
		set => _maPeriodParam.Value = value;
	}

	/// <summary>
	/// Hurst exponent threshold for trend identification.
	/// </summary>
	public decimal HurstThreshold
	{
		get => _hurstThresholdParam.Value;
		set => _hurstThresholdParam.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HurstExponentTrendStrategy()
	{
		_hurstPeriodParam = Param(nameof(HurstPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Hurst Period", "Period for Hurst exponent calculation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 150, 25);

		_maPeriodParam = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for Moving Average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 10);

		_hurstThresholdParam = Param(nameof(HurstThreshold), 0.55m)
			.SetRange(0.1m, 0.9m)
			.SetDisplay("Hurst Threshold", "Threshold value for trend identification", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 0.6m, 0.05m);

		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "Common");
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

		_hurst = null;
		_sma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators
		_hurst = new HurstExponent { Length = HurstPeriod };
		_sma = new SimpleMovingAverage { Length = MaPeriod };

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_hurst, _sma, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
		
		// Enable position protection
		StartProtection(
			takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
			stopLoss: new Unit(2, UnitTypes.Percent) // 2% stop loss
		);
	}

	private void ProcessCandle(ICandleMessage candle, decimal hurstValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		// Check if market is trending (Hurst > 0.5 indicates trending market)
		bool isTrending = hurstValue > HurstThreshold;
		
		if (isTrending)
		{
			// In trending markets, use price relative to MA to determine direction
			
			// Long setup - trending market with price above MA
			if (candle.ClosePrice > smaValue && Position <= 0)
			{
				// Buy signal - trending market with price above MA
				BuyMarket(Volume + Math.Abs(Position));
			}
			// Short setup - trending market with price below MA
			else if (candle.ClosePrice < smaValue && Position >= 0)
			{
				// Sell signal - trending market with price below MA
				SellMarket(Volume + Math.Abs(Position));
			}
		}
		else
		{
			// In non-trending markets, exit positions
			if (Position > 0)
			{
				SellMarket(Position);
			}
			else if (Position < 0)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}