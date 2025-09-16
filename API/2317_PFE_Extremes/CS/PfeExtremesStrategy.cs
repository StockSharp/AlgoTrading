using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Polarized Fractal Efficiency breakout strategy.
/// Opens long positions when PFE crosses above the upper level and
/// opens short positions when it crosses below the lower level.
/// </summary>
public class PfeExtremesStrategy : Strategy
{
	private readonly StrategyParam<int> _pfePeriod;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _downLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevPfe;

	/// <summary>
	/// PFE calculation period.
	/// </summary>
	public int PfePeriod
	{
		get => _pfePeriod.Value;
		set => _pfePeriod.Value = value;
	}

	/// <summary>
	/// Upper threshold for long signals.
	/// </summary>
	public decimal UpLevel
	{
		get => _upLevel.Value;
		set => _upLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold for short signals.
	/// </summary>
	public decimal DownLevel
	{
		get => _downLevel.Value;
		set => _downLevel.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="PfeExtremesStrategy"/>.
	/// </summary>
	public PfeExtremesStrategy()
	{
		_pfePeriod = Param(nameof(PfePeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("PFE Period", "Number of bars for PFE calculation", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_upLevel = Param(nameof(UpLevel), 0.5m)
			.SetDisplay("Upper Level", "PFE value to trigger long entries", "Signal");

		_downLevel = Param(nameof(DownLevel), -0.5m)
			.SetDisplay("Lower Level", "PFE value to trigger short entries", "Signal");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator calculation", "General");
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
		_prevPfe = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var pfe = new PolarizedFractalEfficiency { Length = PfePeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(pfe, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, pfe);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal pfeValue)
	{
		// Only work with finished candles
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Close short positions when PFE moves above the upper level
		if (pfeValue > UpLevel && Position < 0)
			BuyMarket(Math.Abs(Position));

		// Close long positions when PFE moves below the lower level
		if (pfeValue < DownLevel && Position > 0)
			SellMarket(Position);

		if (_prevPfe is decimal prev)
		{
			// Upward crossover triggers a long entry
			if (prev <= UpLevel && pfeValue > UpLevel && Position <= 0)
				BuyMarket(Volume);

			// Downward crossover triggers a short entry
			if (prev >= DownLevel && pfeValue < DownLevel && Position >= 0)
				SellMarket(Volume);
		}

		// Save current value for next comparison
		_prevPfe = pfeValue;
	}
}
