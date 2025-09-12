using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Price and volume breakout strategy with SMA filter and optional direction.
/// Enters when both price and volume break above previous highs (or below lows).
/// Exits after five consecutive closes beyond the trend SMA.
/// </summary>
public class PriceAndVolumeBreakoutBuyStrategy : Strategy
{
	private readonly StrategyParam<int> _priceBreakoutPeriod;
	private readonly StrategyParam<int> _volumeBreakoutPeriod;
	private readonly StrategyParam<int> _trendlineLength;
	private readonly StrategyParam<string> _orderDirection;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _priceHighest = null!;
	private Lowest _priceLowest = null!;
	private Highest _volumeHighest = null!;
	private SMA _sma = null!;

	private decimal _prevPriceHigh;
	private decimal _prevPriceLow;
	private decimal _prevVolumeHigh;
	private int _belowSmaCount;
	private int _aboveSmaCount;

	/// <summary>
	/// Price breakout lookback period.
	/// </summary>
	public int PriceBreakoutPeriod
	{
		get => _priceBreakoutPeriod.Value;
		set => _priceBreakoutPeriod.Value = value;
	}

	/// <summary>
	/// Volume breakout lookback period.
	/// </summary>
	public int VolumeBreakoutPeriod
	{
		get => _volumeBreakoutPeriod.Value;
		set => _volumeBreakoutPeriod.Value = value;
	}

	/// <summary>
	/// Trendline SMA length.
	/// </summary>
	public int TrendlineLength
	{
		get => _trendlineLength.Value;
		set => _trendlineLength.Value = value;
	}

	/// <summary>
	/// Trading direction.
	/// </summary>
	public string OrderDirection
	{
		get => _orderDirection.Value;
		set => _orderDirection.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="PriceAndVolumeBreakoutBuyStrategy"/>.
	/// </summary>
	public PriceAndVolumeBreakoutBuyStrategy()
	{
		_priceBreakoutPeriod = Param(nameof(PriceBreakoutPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("Price Breakout Period", "Lookback for price breakout", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 20);

		_volumeBreakoutPeriod = Param(nameof(VolumeBreakoutPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("Volume Breakout Period", "Lookback for volume breakout", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 20);

		_trendlineLength = Param(nameof(TrendlineLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Trendline Length", "Length of SMA filter", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 50);

		_orderDirection = Param(nameof(OrderDirection), "Long")
			.SetDisplay("Order Direction", "Trading direction", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
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

		_prevPriceHigh = 0m;
		_prevPriceLow = 0m;
		_prevVolumeHigh = 0m;
		_belowSmaCount = 0;
		_aboveSmaCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceHighest = new Highest { Length = PriceBreakoutPeriod };
		_priceLowest = new Lowest { Length = PriceBreakoutPeriod };
		_volumeHighest = new Highest { Length = VolumeBreakoutPeriod };
		_sma = new SMA { Length = TrendlineLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_priceHighest, _priceLowest, _sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal priceHigh, decimal priceLow, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeHigh = _volumeHighest.Process(candle.TotalVolume).ToDecimal();

		if (!_priceHighest.IsFormed || !_priceLowest.IsFormed || !_volumeHighest.IsFormed || !_sma.IsFormed)
		{
			_prevPriceHigh = priceHigh;
			_prevPriceLow = priceLow;
			_prevVolumeHigh = volumeHigh;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevPriceHigh = priceHigh;
			_prevPriceLow = priceLow;
			_prevVolumeHigh = volumeHigh;
			return;
		}

		if (Position > 0)
		{
			if (candle.ClosePrice < smaValue)
			{
				_belowSmaCount++;
				if (_belowSmaCount >= 5)
				{
					SellMarket(Position);
					_belowSmaCount = 0;
				}
			}
			else
			{
				_belowSmaCount = 0;
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice > smaValue)
			{
				_aboveSmaCount++;
				if (_aboveSmaCount >= 5)
				{
					BuyMarket(Math.Abs(Position));
					_aboveSmaCount = 0;
				}
			}
			else
			{
				_aboveSmaCount = 0;
			}
		}

		if (Position == 0)
		{
			var longCondition = candle.ClosePrice > _prevPriceHigh && candle.TotalVolume > _prevVolumeHigh && candle.ClosePrice > smaValue && OrderDirection != "Short";
			var shortCondition = candle.ClosePrice < _prevPriceLow && candle.TotalVolume > _prevVolumeHigh && candle.ClosePrice < smaValue && OrderDirection != "Long";

			if (longCondition)
				BuyMarket();
			else if (shortCondition)
				SellMarket();
		}

		_prevPriceHigh = priceHigh;
		_prevPriceLow = priceLow;
		_prevVolumeHigh = volumeHigh;
	}
}
