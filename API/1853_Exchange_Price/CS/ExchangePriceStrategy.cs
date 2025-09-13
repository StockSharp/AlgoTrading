using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on comparing current price with prices from short and long lookback periods.
/// Buys when the short-term change crosses above the long-term change and sells on the opposite cross.
/// </summary>
public class ExchangePriceStrategy : Strategy
{
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;

	private readonly List<decimal> _prices = new();
	private decimal? _prevUpDiff;
	private decimal? _prevDownDiff;

	/// <summary>
	/// Number of bars for short lookback period.
	/// </summary>
	public int ShortPeriod
	{
		get => _shortPeriod.Value;
		set => _shortPeriod.Value = value;
	}

	/// <summary>
	/// Number of bars for long lookback period.
	/// </summary>
	public int LongPeriod
	{
		get => _longPeriod.Value;
		set => _longPeriod.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ExchangePriceStrategy"/>.
	/// </summary>
	public ExchangePriceStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 96)
			.SetGreaterThanZero()
			.SetDisplay("Short Period", "Bars for short lookback", "General")
			.SetCanOptimize(true);

		_longPeriod = Param(nameof(LongPeriod), 288)
			.SetGreaterThanZero()
			.SetDisplay("Long Period", "Bars for long lookback", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
			.SetDisplay("Allow Long Entry", "Open long positions", "Trading");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
			.SetDisplay("Allow Short Entry", "Open short positions", "Trading");

		_allowLongExit = Param(nameof(AllowLongExit), true)
			.SetDisplay("Allow Long Exit", "Close long positions", "Trading");

		_allowShortExit = Param(nameof(AllowShortExit), true)
			.SetDisplay("Allow Short Exit", "Close short positions", "Trading");
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

		_prices.Clear();
		_prevUpDiff = null;
		_prevDownDiff = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with finished candles
		if (candle.State != CandleStates.Finished)
			return;

		_prices.Add(candle.ClosePrice);

		// Keep only required number of prices
		if (_prices.Count > LongPeriod + 1)
			_prices.RemoveAt(0);

		// Wait until enough data is collected
		if (_prices.Count <= LongPeriod)
			return;

		var priceShort = _prices[^1 - ShortPeriod];
		var priceLong = _prices[^1 - LongPeriod];

		var upDiff = candle.ClosePrice - priceShort;
		var downDiff = candle.ClosePrice - priceLong;

		if (_prevUpDiff is decimal prevUp && _prevDownDiff is decimal prevDown)
		{
			var crossUp = prevUp <= prevDown && upDiff > downDiff;
			var crossDown = prevUp >= prevDown && upDiff < downDiff;

			if (crossUp)
			{
				// Close short position if needed
				if (AllowShortExit && Position < 0)
					BuyMarket(-Position);

				// Open long position if allowed
				if (AllowLongEntry && Position <= 0 && IsFormedAndOnlineAndAllowTrading())
					BuyMarket(Volume);
			}
			else if (crossDown)
			{
				// Close long position if needed
				if (AllowLongExit && Position > 0)
					SellMarket(Position);

				// Open short position if allowed
				if (AllowShortEntry && Position >= 0 && IsFormedAndOnlineAndAllowTrading())
					SellMarket(Volume);
			}
		}

		_prevUpDiff = upDiff;
		_prevDownDiff = downDiff;
	}
}
