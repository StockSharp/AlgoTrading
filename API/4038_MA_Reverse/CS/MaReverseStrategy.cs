using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average reversal strategy converted from the MetaTrader MA_Reverse expert advisor.
/// Counts how many consecutive closes remain on one side of the SMA and opens a trade once the streak is long enough.
/// </summary>
public class MaReverseStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _streakThreshold;
	private readonly StrategyParam<decimal> _minimumDeviation;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private SMA? _sma;

	/// <summary>
	/// Initializes a new instance of the <see cref="MaReverseStrategy"/> class.
	/// </summary>
	public MaReverseStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetDisplay("Trade Volume", "Lot size used for market orders", "Trading")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_smaPeriod = Param(nameof(SmaPeriod), 14)
		.SetDisplay("SMA Period", "Number of candles used by the moving average", "Indicator")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_streakThreshold = Param(nameof(StreakThreshold), 150)
		.SetDisplay("Streak Threshold", "Number of consecutive closes required before reversing", "Logic")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_minimumDeviation = Param(nameof(MinimumDeviation), 0.004m)
		.SetDisplay("Minimum Deviation", "Minimum distance between price and SMA to confirm the reversal", "Logic")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 30m)
		.SetDisplay("Take Profit (points)", "Take-profit distance expressed in price steps", "Risk")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for SMA calculation", "General");
	}

	/// <summary>
	/// Trade volume expressed in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Number of candles used by the moving average.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// Number of consecutive closes required before reversing.
	/// </summary>
	public int StreakThreshold
	{
		get => _streakThreshold.Value;
		set => _streakThreshold.Value = value;
	}

	/// <summary>
	/// Minimum distance between price and the SMA to confirm the reversal.
	/// </summary>
	public decimal MinimumDeviation
	{
		get => _minimumDeviation.Value;
		set => _minimumDeviation.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for SMA calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_sma = null;
		_streak = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Synchronize the base Strategy volume with the configured trade volume.
		Volume = TradeVolume;

		// Configure automatic profit taking that mirrors the 30-point target of the original expert.
		var pointSize = CalculatePointSize();
		if (pointSize > 0m && TakeProfitPoints > 0m)
		{
			var takeProfitDistance = new Unit(TakeProfitPoints * pointSize, UnitTypes.Absolute);
			StartProtection(
			takeProfit: takeProfitDistance,
			isStopTrailing: false,
			useMarketOrders: true);
		}

		// Create the moving average indicator.
		_sma = new SMA
		{
			Length = SmaPeriod
		};

		// Subscribe to candles and bind the indicator to receive calculated values.
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_sma, ProcessCandle)
		.Start();

		// Plot candles, the SMA and the strategy trades when a chart is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private int _streak;

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_sma == null || !_sma.IsFormed)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var closePrice = candle.ClosePrice;
		var deviation = closePrice - smaValue;

		// Reset the streak when the close matches the moving average exactly.
		if (deviation == 0m)
		{
			_streak = 0;
			return;
		}

		if (deviation > 0m)
		{
			// Price is above the SMA, count how long the bullish streak lasts.
			if (_streak < 0)
			_streak = 0;

			_streak++;

			if (_streak == StreakThreshold && deviation > MinimumDeviation)
			{
				EnterShort(closePrice, smaValue);
			}
		}
		else
		{
			// Price is below the SMA, count how long the bearish streak lasts.
			if (_streak > 0)
			_streak = 0;

			_streak--;

			if (_streak == -StreakThreshold && deviation < -MinimumDeviation)
			{
				EnterLong(closePrice, smaValue);
			}
		}
	}

	private void EnterShort(decimal price, decimal smaValue)
	{
		var volume = TradeVolume;
		if (volume <= 0m)
		return;

		var closingVolume = Position > 0m ? Position : 0m;

		// Reverse to a short position by closing longs and adding the configured trade volume.
		SellMarket(volume + closingVolume);
		LogInfo($"Short entry after {_streak} bullish closes. Price={price:0.#####}, SMA={smaValue:0.#####}");
	}

	private void EnterLong(decimal price, decimal smaValue)
	{
		var volume = TradeVolume;
		if (volume <= 0m)
		return;

		var closingVolume = Position < 0m ? -Position : 0m;

		// Reverse to a long position by covering shorts and adding the configured trade volume.
		BuyMarket(volume + closingVolume);
		LogInfo($"Long entry after {-_streak} bearish closes. Price={price:0.#####}, SMA={smaValue:0.#####}");
	}

	private decimal CalculatePointSize()
	{
		if (Security == null)
		return 0m;

		var step = Security.PriceStep ?? Security.Step ?? 0m;
		return step > 0m ? step : 0m;
	}
}
