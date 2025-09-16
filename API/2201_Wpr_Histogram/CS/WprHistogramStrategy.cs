using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Williams %R histogram indicator.
/// Opens a long position when price leaves overbought zone.
/// Opens a short position when price leaves oversold zone.
/// </summary>
public class WprHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;

	// Previous zone state: 0 - overbought, 1 - neutral, 2 - oversold
	private int? _previousZone;

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// High level threshold.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Low level threshold.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public WprHistogramStrategy()
	{
		_wprPeriod = Param(nameof(WprPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("WPR Period", "Period for Williams %R", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(7, 28, 7);

		_highLevel = Param(nameof(HighLevel), -30m)
		.SetDisplay("High Level", "Overbought threshold", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(-20m, -40m, -10m);

		_lowLevel = Param(nameof(LowLevel), -70m)
		.SetDisplay("Low Level", "Oversold threshold", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(-60m, -80m, -10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		_previousZone = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var wpr = new WilliamsR { Length = WprPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(wpr, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, wpr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue)
	{
		// Only finished candles are processed
		if (candle.State != CandleStates.Finished)
		return;

		// Ensure strategy is ready
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var currentZone = 1;
		if (wprValue > HighLevel)
		currentZone = 0;
		else if (wprValue < LowLevel)
		currentZone = 2;

		if (_previousZone == null)
		{
			_previousZone = currentZone;
			return;
		}

		// Leaving overbought zone - open long
		if (_previousZone == 0 && currentZone != 0 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		// Leaving oversold zone - open short
		else if (_previousZone == 2 && currentZone != 2 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_previousZone = currentZone;
	}
}

