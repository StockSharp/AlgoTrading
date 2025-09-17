using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R cloud breakout strategy converted from the MetaTrader expert advisor.
/// The logic looks for %R crossings above -80 to go long and below -20 to go short.
/// Positions are reversed when an opposite signal appears.
/// </summary>
public class WprCustomCloudSimpleStrategy : Strategy
{
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<DataType> _candleType;

	private WilliamsR? _williamsR;
	private decimal? _previousWpr;
	private decimal? _olderWpr;

	/// <summary>
	/// Williams %R lookback length.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Overbought threshold that triggers short entries when crossed from above.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Oversold threshold that triggers long entries when crossed from below.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the indicator updates.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="WprCustomCloudSimpleStrategy"/> class.
	/// </summary>
	public WprCustomCloudSimpleStrategy()
	{
		_wprPeriod = Param(nameof(WprPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period", "Williams %R lookback length", "Williams %R")
			.SetCanOptimize(true);

		_overboughtLevel = Param(nameof(OverboughtLevel), -20m)
			.SetDisplay("Overbought Level", "%R level that marks overbought conditions", "Williams %R")
			.SetCanOptimize(true);

		_oversoldLevel = Param(nameof(OversoldLevel), -80m)
			.SetDisplay("Oversold Level", "%R level that marks oversold conditions", "Williams %R")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for Williams %R", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_williamsR = new WilliamsR
		{
			Length = WprPeriod
		};

		_previousWpr = null;
		_olderWpr = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_williamsR, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _williamsR);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_williamsR == null || !_williamsR.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var previous = _previousWpr;
		var older = _olderWpr;

		if (previous is decimal prev && older is decimal prevPrev)
		{
			// Detect bullish crossover when the previous bar closes above the oversold boundary.
			var crossedAboveOversold = prevPrev < OversoldLevel && prev > OversoldLevel;

			// Detect bearish crossover when the previous bar closes below the overbought boundary.
			var crossedBelowOverbought = prevPrev > OverboughtLevel && prev < OverboughtLevel;

			if (crossedAboveOversold && Position <= 0)
			{
				var volume = Volume;
				if (Position < 0)
					volume += Math.Abs(Position);

				// Reverse short exposure and go long on the bullish breakout.
				BuyMarket(volume);
			}
			else if (crossedBelowOverbought && Position >= 0)
			{
				var volume = Volume;
				if (Position > 0)
					volume += Math.Abs(Position);

				// Reverse long exposure and go short on the bearish breakout.
				SellMarket(volume);
			}
		}

		// Shift the stored Williams %R values so the next candle can detect fresh crossovers.
		_olderWpr = previous;
		_previousWpr = wprValue;
	}
}
