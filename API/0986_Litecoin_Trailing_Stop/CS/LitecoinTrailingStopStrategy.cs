using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Litecoin trailing stop strategy based on KAMA trend detection.
/// </summary>
public class LitecoinTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<int> _kamaLength;
	private readonly StrategyParam<int> _barsBetweenEntries;
	private readonly StrategyParam<decimal> _trailingStopPercent;
	private readonly StrategyParam<int> _delayBars;
	private readonly StrategyParam<DataType> _candleType;

	private KaufmanAdaptiveMovingAverage _kama;

	private decimal _prevKama;
	private int _barsSinceEntry;
	private int _barsSinceLastTrade;
	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	/// <summary>
	/// KAMA period.
	/// </summary>
	public int KamaLength
	{
		get => _kamaLength.Value;
		set => _kamaLength.Value = value;
	}

	/// <summary>
	/// Minimum bars between entries.
	/// </summary>
	public int BarsBetweenEntries
	{
		get => _barsBetweenEntries.Value;
		set => _barsBetweenEntries.Value = value;
	}

	/// <summary>
	/// Trailing stop percent.
	/// </summary>
	public decimal TrailingStopPercent
	{
		get => _trailingStopPercent.Value;
		set => _trailingStopPercent.Value = value;
	}

	/// <summary>
	/// Bars before trailing starts.
	/// </summary>
	public int DelayBars
	{
		get => _delayBars.Value;
		set => _delayBars.Value = value;
	}

	/// <summary>
	/// Candle data type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public LitecoinTrailingStopStrategy()
	{
		_kamaLength = Param(nameof(KamaLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("KAMA Length", "Period for KAMA indicator", "General")
		.SetCanOptimize(true)
		.SetOptimize(20, 100, 5);

		_barsBetweenEntries = Param(nameof(BarsBetweenEntries), 30)
		.SetGreaterThanZero()
		.SetDisplay("Bars Between Entries", "Minimum bars between new positions", "General")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);

		_trailingStopPercent = Param(nameof(TrailingStopPercent), 12m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Stop %", "Percent for trailing stop", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5m, 20m, 1m);

		_delayBars = Param(nameof(DelayBars), 50)
		.SetGreaterThanZero()
		.SetDisplay("Delay Bars", "Bars before trailing starts", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10, 100, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_prevKama = 0m;
		_barsSinceEntry = 0;
		_barsSinceLastTrade = int.MaxValue;
		_entryPrice = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_kama = new KaufmanAdaptiveMovingAverage { Length = KamaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_kama, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _kama);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal kamaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_kama.IsFormed)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var flat = _prevKama != 0m && kamaValue / _prevKama > 0.999m && kamaValue / _prevKama < 1.001m;
		var bullish = kamaValue > _prevKama && !flat;
		var bearish = kamaValue < _prevKama && !flat;

		_barsSinceLastTrade++;
		if (Position != 0)
		_barsSinceEntry++;
		else
		_barsSinceEntry = 0;

		var canEnter = _barsSinceLastTrade >= BarsBetweenEntries;

		if (bullish && canEnter && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_barsSinceLastTrade = 0;
			_barsSinceEntry = 0;
			_entryPrice = candle.ClosePrice;
			_highestPrice = _entryPrice;
			LogInfo($"Long entry at {candle.ClosePrice}");
		}
		else if (bearish && canEnter && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_barsSinceLastTrade = 0;
			_barsSinceEntry = 0;
			_entryPrice = candle.ClosePrice;
			_lowestPrice = _entryPrice;
			LogInfo($"Short entry at {candle.ClosePrice}");
		}

		if (Position > 0)
		{
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);

			if (_barsSinceEntry >= DelayBars)
			{
				var trailPercent = TrailingStopPercent / 100m;
				var stopPrice = _highestPrice * (1 - trailPercent);

				if (candle.LowPrice <= stopPrice)
				{
					SellMarket(Math.Abs(Position));
					LogInfo($"Long exit by trailing stop at {candle.ClosePrice}");
					_highestPrice = 0m;
				}
			}
		}
		else if (Position < 0)
		{
			_lowestPrice = Math.Min(_lowestPrice, candle.LowPrice);

			if (_barsSinceEntry >= DelayBars)
			{
				var trailPercent = TrailingStopPercent / 100m;
				var stopPrice = _lowestPrice * (1 + trailPercent);

				if (candle.HighPrice >= stopPrice)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Short exit by trailing stop at {candle.ClosePrice}");
					_lowestPrice = 0m;
				}
			}
		}

		_prevKama = kamaValue;
	}
}

