using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Three Line Break indicator.
/// </summary>
public class ThreeLineBreakStrategy : Strategy
{
	private readonly StrategyParam<int> _linesBreak;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _trendUp = true;

	/// <summary>
	/// Number of lines used to detect breakouts.
	/// </summary>
	public int LinesBreak
	{
		get => _linesBreak.Value;
		set => _linesBreak.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ThreeLineBreakStrategy()
	{
		_linesBreak = Param(nameof(LinesBreak), 3)
			.SetGreaterThanZero()
			.SetDisplay("Lines Break", "Number of lines for trend detection", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(12).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
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

		_highest = new Highest { Length = LinesBreak };
		_lowest = new Lowest { Length = LinesBreak };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_highest.IsFormed && _lowest.IsFormed)
		{
			var trendUp = _trendUp;

			if (trendUp && candle.LowPrice < _prevLow)
				trendUp = false;
			else if (!trendUp && candle.HighPrice > _prevHigh)
				trendUp = true;

			if (trendUp != _trendUp)
			{
				if (trendUp && Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
				}
				else if (!trendUp && Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
				}
			}

			_trendUp = trendUp;
		}

		_prevHigh = _highest.Process(candle).ToDecimal();
		_prevLow = _lowest.Process(candle).ToDecimal();
	}
}
