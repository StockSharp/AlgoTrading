using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on SimpleBars indicator.
/// Opens position when the indicator changes trend.
/// Signal is executed on the next bar.
/// </summary>
public class SimpleBarsStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<bool> _useClose;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Number of bars used to evaluate trend.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Use closing price instead of high/low.
	/// </summary>
	public bool UseClose
	{
		get => _useClose.Value;
		set => _useClose.Value = value;
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
	/// Initializes parameters.
	/// </summary>
	public SimpleBarsStrategy()
	{
		_period = Param(nameof(Period), 6)
			.SetDisplay("Period", "Number of bars for trend check", "General")
			.SetGreaterThanZero();

		_useClose = Param(nameof(UseClose), true)
			.SetDisplay("Use Close", "Use close price instead of extremes", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		var lowest = new Lowest { Length = Period };
		var highest = new Highest { Length = Period };

		var subscription = SubscribeCandles(CandleType);

		decimal prevMinLow = 0m;
		decimal prevMaxHigh = 0m;
		int prevTrend = 0; // 1 buy, -1 sell
		int? pendingSignal = null;
		bool isInitialized = false;

		subscription
			.Bind(lowest, highest, (candle, minLow, maxHigh) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (pendingSignal.HasValue)
				{
					// Execute signal from previous bar
					if (pendingSignal.Value == 1 && Position <= 0)
					{
						var volume = Volume + Math.Abs(Position);
						BuyMarket(volume);
					}
					else if (pendingSignal.Value == -1 && Position >= 0)
					{
						var volume = Volume + Math.Abs(Position);
						SellMarket(volume);
					}

					pendingSignal = null;
				}

				if (!lowest.IsFormed || !highest.IsFormed)
				{
					prevMinLow = minLow;
					prevMaxHigh = maxHigh;
					return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					prevMinLow = minLow;
					prevMaxHigh = maxHigh;
					return;
				}

				// Determine price points depending on mode
				var buyPrice = UseClose ? candle.ClosePrice : candle.LowPrice;
				var sellPrice = UseClose ? candle.ClosePrice : candle.HighPrice;

				int trend;

				if (!isInitialized)
				{
					trend = candle.ClosePrice > candle.OpenPrice ? 1 : -1;
					isInitialized = true;
				}
				else if (prevTrend == 1)
				{
					trend = buyPrice > prevMinLow ? 1 : -1;
				}
				else
				{
					trend = sellPrice < prevMaxHigh ? -1 : 1;
				}

				// Store signal for next bar
				pendingSignal = trend;
				prevTrend = trend;
				prevMinLow = minLow;
				prevMaxHigh = maxHigh;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, lowest);
			DrawIndicator(area, highest);
			DrawOwnTrades(area);
		}
	}
}
