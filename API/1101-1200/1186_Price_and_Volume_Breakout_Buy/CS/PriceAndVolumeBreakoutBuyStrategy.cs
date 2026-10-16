using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades breakouts of the previous price and volume ranges in the direction of the trend SMA.
/// </summary>
public class PriceAndVolumeBreakoutBuyStrategy : Strategy
{
	private readonly StrategyParam<int> _priceBreakoutPeriod;
	private readonly StrategyParam<int> _volumeBreakoutPeriod;
	private readonly StrategyParam<int> _trendlineLength;
	private readonly StrategyParam<string> _orderDirection;
	private readonly StrategyParam<DataType> _candleType;
	private readonly Queue<(decimal high, decimal low, decimal volume)> _previousBars = new();
	private int _oppositeCloses;
	private bool _entryRequested;
	private bool _exitRequested;

	public int PriceBreakoutPeriod { get => _priceBreakoutPeriod.Value; set => _priceBreakoutPeriod.Value = value; }
	public int VolumeBreakoutPeriod { get => _volumeBreakoutPeriod.Value; set => _volumeBreakoutPeriod.Value = value; }
	public int TrendlineLength { get => _trendlineLength.Value; set => _trendlineLength.Value = value; }
	public string OrderDirection { get => _orderDirection.Value; set => _orderDirection.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PriceAndVolumeBreakoutBuyStrategy()
	{
		_priceBreakoutPeriod = Param(nameof(PriceBreakoutPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("Price Breakout Period", "Number of preceding candles used for the price extreme.", "Breakout");

		_volumeBreakoutPeriod = Param(nameof(VolumeBreakoutPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("Volume Breakout Period", "Number of preceding candles used for maximum volume.", "Breakout");

		_trendlineLength = Param(nameof(TrendlineLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Trendline Length", "Trend SMA period.", "Trend");

		_orderDirection = Param(nameof(OrderDirection), "Long")
			.SetDisplay("Order Direction", "Long, Short, or Both.", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type.", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_previousBars.Clear();
		_oppositeCloses = 0;
		_entryRequested = false;
		_exitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (OrderDirection is not ("Long" or "Short" or "Both"))
			throw new ArgumentOutOfRangeException(nameof(OrderDirection), OrderDirection, "Supported values are Long, Short and Both.");

		var trend = new SimpleMovingAverage { Length = TrendlineLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(trend, (candle, average) => ProcessCandle(candle, average, trend)).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, trend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal average, SimpleMovingAverage trend)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The current bar must never contribute to its own lookback extrema.
		if (trend.IsFormed && _previousBars.Count >= Math.Max(PriceBreakoutPeriod, VolumeBreakoutPeriod))
		{
			if (Position != 0m)
			{
				_entryRequested = false;
				if (!_exitRequested)
				{
					var opposite = Position > 0m ? candle.ClosePrice < average : candle.ClosePrice > average;
					_oppositeCloses = opposite ? _oppositeCloses + 1 : 0;
					if (_oppositeCloses == 5)
					{
						_exitRequested = true;
						_oppositeCloses = 0;
						if (Position > 0m)
							SellMarket(Position);
						else
							BuyMarket(Math.Abs(Position));
					}
				}
			}
			else
			{
				_exitRequested = false;
				_oppositeCloses = 0;
				if (!_entryRequested)
				{
					var priceBars = _previousBars.TakeLast(PriceBreakoutPeriod).ToArray();
					var maximumVolume = _previousBars.TakeLast(VolumeBreakoutPeriod).Max(x => x.volume);
					var longSignal = candle.ClosePrice > priceBars.Max(x => x.high)
						&& candle.TotalVolume > maximumVolume && candle.ClosePrice > average;
					var shortSignal = candle.ClosePrice < priceBars.Min(x => x.low)
						&& candle.TotalVolume > maximumVolume && candle.ClosePrice < average;

					if (longSignal && OrderDirection is "Long" or "Both")
					{
						_entryRequested = true;
						BuyMarket(Volume);
					}
					else if (shortSignal && OrderDirection is "Short" or "Both")
					{
						_entryRequested = true;
						SellMarket(Volume);
					}
				}
			}
		}

		_previousBars.Enqueue((candle.HighPrice, candle.LowPrice, candle.TotalVolume));
		while (_previousBars.Count > Math.Max(PriceBreakoutPeriod, VolumeBreakoutPeriod))
			_previousBars.Dequeue();
	}
}
