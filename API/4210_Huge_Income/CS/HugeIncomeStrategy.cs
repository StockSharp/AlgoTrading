namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Port of the MetaTrader expert advisor "Huge Income".
/// Exploits intraday extensions away from the daily open and closes positions ahead of the configured market close.
/// </summary>
public class HugeIncomeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _minimumRangePips;
	private readonly StrategyParam<int> _buyCutoffHour;
	private readonly StrategyParam<int> _sellCutoffHour;
	private readonly StrategyParam<int> _marketCloseHour;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _currentDay;
	private decimal _dailyOpen;
	private decimal _dailyHigh;
	private decimal _dailyLow;
	private bool _hasDailyData;

	/// <summary>
	/// Initializes a new instance of <see cref="HugeIncomeStrategy"/>.
	/// </summary>
	public HugeIncomeStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order size used for entries", "General");

		_minimumRangePips = Param(nameof(MinimumRangePips), 15m)
			.SetNotNegative()
			.SetDisplay("Minimum Range (pips)", "Minimum daily distance from the open before trading", "Filters");

		_buyCutoffHour = Param(nameof(BuyCutoffHour), 22)
			.SetNotNegative()
			.SetLessOrEqual(23)
			.SetDisplay("Buy Cutoff Hour", "Latest hour when new long trades are allowed", "Timing");

		_sellCutoffHour = Param(nameof(SellCutoffHour), 16)
			.SetNotNegative()
			.SetLessOrEqual(23)
			.SetDisplay("Sell Cutoff Hour", "Latest hour when new short trades are allowed", "Timing");

		_marketCloseHour = Param(nameof(MarketCloseHour), 23)
			.SetNotNegative()
			.SetLessOrEqual(23)
			.SetDisplay("Market Close Hour", "Hour when all open positions must be closed", "Timing");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to build daily statistics", "General");
	}

	/// <summary>
	/// Order volume used for market entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Minimum distance in pips required between the daily open and the daily extreme.
	/// </summary>
	public decimal MinimumRangePips
	{
		get => _minimumRangePips.Value;
		set => _minimumRangePips.Value = value;
	}

	/// <summary>
	/// Latest hour when a new long position can be initiated.
	/// </summary>
	public int BuyCutoffHour
	{
		get => _buyCutoffHour.Value;
		set => _buyCutoffHour.Value = value;
	}

	/// <summary>
	/// Latest hour when a new short position can be initiated.
	/// </summary>
	public int SellCutoffHour
	{
		get => _sellCutoffHour.Value;
		set => _sellCutoffHour.Value = value;
	}

	/// <summary>
	/// Hour at which all open trades are forcefully closed.
	/// </summary>
	public int MarketCloseHour
	{
		get => _marketCloseHour.Value;
		set => _marketCloseHour.Value = value;
	}

	/// <summary>
	/// Candle type that drives intraday updates.
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

		_currentDay = null;
		_dailyOpen = 0m;
		_dailyHigh = 0m;
		_dailyLow = 0m;
		_hasDailyData = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with finished candles to avoid partial-day noise.
		if (candle.State != CandleStates.Finished)
			return;

		UpdateDailyLevels(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasDailyData)
			return;

		var candleHour = candle.CloseTime.Hour;

		// Always protect open exposure near the configured market close.
		if (candleHour >= MarketCloseHour && Position != 0m)
		{
			ClosePosition();
			return;
		}

		if (!CanOpenNewTrade())
			return;

		var closePrice = candle.ClosePrice;
		var longRange = _dailyOpen - _dailyLow;
		var shortRange = _dailyHigh - _dailyOpen;
		var requiredRange = GetRequiredRange();

		if (ShouldEnterLong(closePrice, longRange, requiredRange, candleHour))
		{
			BuyMarket(TradeVolume);
			return;
		}

		if (ShouldEnterShort(closePrice, shortRange, requiredRange, candleHour))
		{
			SellMarket(TradeVolume);
		}
	}

	private void UpdateDailyLevels(ICandleMessage candle)
	{
		var candleDate = candle.OpenTime.Date;

		if (_currentDay != candleDate)
		{
			_currentDay = candleDate;
			_dailyOpen = candle.OpenPrice;
			_dailyHigh = candle.HighPrice;
			_dailyLow = candle.LowPrice;
			_hasDailyData = true;
			return;
		}

		if (!_hasDailyData)
			return;

		// Track the running extremes for the active day.
		if (candle.HighPrice > _dailyHigh)
			_dailyHigh = candle.HighPrice;

		if (candle.LowPrice < _dailyLow)
			_dailyLow = candle.LowPrice;
	}

	private bool CanOpenNewTrade()
	{
		if (TradeVolume <= 0m)
			return false;

		if (Position != 0m)
			return false;

		return !HasActiveOrders();
	}

	private bool ShouldEnterLong(decimal closePrice, decimal longRange, decimal requiredRange, int hour)
	{
		if (hour >= BuyCutoffHour)
			return false;

		if (closePrice <= _dailyOpen)
			return false;

		if (longRange <= 0m)
			return false;

		if (requiredRange > 0m)
			return longRange > requiredRange;

		return true;
	}

	private bool ShouldEnterShort(decimal closePrice, decimal shortRange, decimal requiredRange, int hour)
	{
		if (hour >= SellCutoffHour)
			return false;

		if (closePrice >= _dailyOpen)
			return false;

		if (shortRange <= 0m)
			return false;

		if (requiredRange > 0m)
			return shortRange > requiredRange;

		return true;
	}

	private decimal GetRequiredRange()
	{
		if (MinimumRangePips <= 0m)
			return 0m;

		var step = Security?.PriceStep ?? Security?.Step ?? 0m;
		if (step <= 0m)
			return 0m;

		return step * MinimumRangePips;
	}

	private bool HasActiveOrders()
	{
		return Orders.Any(o => o.State.IsActive());
	}
}
