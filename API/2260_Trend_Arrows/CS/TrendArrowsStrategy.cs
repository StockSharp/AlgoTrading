using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend arrows strategy based on breakouts of recent extremes.
/// Detects when price moves above the highest close or below the lowest close
/// over the specified period and trades in the direction of the breakout.
/// </summary>
public class TrendArrowsStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _canOpenLong;
	private readonly StrategyParam<bool> _canOpenShort;
	private readonly StrategyParam<bool> _canCloseLong;
	private readonly StrategyParam<bool> _canCloseShort;

	private bool _prevTrendUp;
	private bool _prevTrendDown;

	/// <summary>
	/// Period length for highest and lowest calculations.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Type of candles used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool CanOpenLong
	{
		get => _canOpenLong.Value;
		set => _canOpenLong.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool CanOpenShort
	{
		get => _canOpenShort.Value;
		set => _canOpenShort.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool CanCloseLong
	{
		get => _canCloseLong.Value;
		set => _canCloseLong.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool CanCloseShort
	{
		get => _canCloseShort.Value;
		set => _canCloseShort.Value = value;
	}

	/// <summary>
	/// Initializes new instance of <see cref="TrendArrowsStrategy"/>.
	/// </summary>
	public TrendArrowsStrategy()
	{
		_period = Param(nameof(Period), 15)
			 .SetGreaterThanZero()
			 .SetDisplay("Period", "Number of bars for extreme calculation", "Parameters")
			 .SetCanOptimize(true)
			 .SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			 .SetDisplay("Candle Type", "Timeframe of candles", "Parameters");

		_canOpenLong = Param(nameof(CanOpenLong), true)
			 .SetDisplay("Open Long", "Allow opening long positions", "Trading");

		_canOpenShort = Param(nameof(CanOpenShort), true)
			 .SetDisplay("Open Short", "Allow opening short positions", "Trading");

		_canCloseLong = Param(nameof(CanCloseLong), true)
			 .SetDisplay("Close Long", "Allow closing long positions", "Trading");

		_canCloseShort = Param(nameof(CanCloseShort), true)
			 .SetDisplay("Close Short", "Allow closing short positions", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevTrendUp = false;
		_prevTrendDown = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var highest = new Highest { Length = Period };
		var lowest = new Lowest { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			 .Bind(highest, lowest, ProcessCandle)
			 .Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Determine current trend state
		var trendUp = false;
		var trendDown = false;

		if (candle.ClosePrice > highest)
			trendUp = true;
		else if (candle.ClosePrice < lowest)
			trendDown = true;
		else
		{
			trendUp = _prevTrendUp;
			trendDown = _prevTrendDown;
		}

		// Buy when up trend appears
		if (!_prevTrendUp && trendUp)
		{
			if (CanCloseShort && Position < 0)
				BuyMarket(Math.Abs(Position));
			if (CanOpenLong && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}

		// Sell when down trend appears
		if (!_prevTrendDown && trendDown)
		{
			if (CanCloseLong && Position > 0)
				SellMarket(Math.Abs(Position));
			if (CanOpenShort && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevTrendUp = trendUp;
		_prevTrendDown = trendDown;
	}
}
