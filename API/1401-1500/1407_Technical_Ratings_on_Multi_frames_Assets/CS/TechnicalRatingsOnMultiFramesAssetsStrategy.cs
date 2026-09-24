using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Aggregates SMA/RSI technical ratings from 1h, 4h and daily time frames.
/// </summary>
public class TechnicalRatingsOnMultiFramesAssetsStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _bullRsi;
	private readonly StrategyParam<decimal> _bearRsi;
	private readonly StrategyParam<DataType> _hourly;
	private readonly StrategyParam<DataType> _fourHourly;
	private readonly StrategyParam<DataType> _daily;

	private readonly Dictionary<string, decimal> _ratings = [];

	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal BullRsiThreshold { get => _bullRsi.Value; set => _bullRsi.Value = value; }
	public decimal BearRsiThreshold { get => _bearRsi.Value; set => _bearRsi.Value = value; }
	public DataType HourlyCandleType { get => _hourly.Value; set => _hourly.Value = value; }
	public DataType FourHourCandleType { get => _fourHourly.Value; set => _fourHourly.Value = value; }
	public DataType DailyCandleType { get => _daily.Value; set => _daily.Value = value; }

	public TechnicalRatingsOnMultiFramesAssetsStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 20).SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA period used by each rating.", "Indicators");
		_rsiPeriod = Param(nameof(RsiPeriod), 14).SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period used by each rating.", "Indicators");
		_bullRsi = Param(nameof(BullRsiThreshold), 55m)
			.SetDisplay("Bull RSI", "RSI threshold contributing a bullish vote.", "Indicators");
		_bearRsi = Param(nameof(BearRsiThreshold), 45m)
			.SetDisplay("Bear RSI", "RSI threshold contributing a bearish vote.", "Indicators");
		_hourly = Param(nameof(HourlyCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("1h", "Primary technical-rating timeframe.", "Timeframes");
		_fourHourly = Param(nameof(FourHourCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("4h", "Confirmation technical-rating timeframe.", "Timeframes");
		_daily = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("1d", "Daily technical-rating timeframe.", "Timeframes");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, HourlyCandleType), (Security, FourHourCandleType), (Security, DailyCandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_ratings.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartRating("1h", HourlyCandleType);
		StartRating("4h", FourHourCandleType);
		StartRating("1d", DailyCandleType);
	}

	private void StartRating(string key, DataType candleType)
	{
		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(candleType);
		subscription.Bind(sma, rsi, (candle, smaValue, rsiValue) =>
		{
			if (candle.State != CandleStates.Finished || !sma.IsFormed || !rsi.IsFormed)
				return;

			var maVote = candle.ClosePrice > smaValue ? 1m : candle.ClosePrice < smaValue ? -1m : 0m;
			var rsiVote = rsiValue >= BullRsiThreshold ? 1m : rsiValue <= BearRsiThreshold ? -1m : 0m;
			_ratings[key] = (maVote + rsiVote) / 2m;
			Evaluate();
		}).Start();

		var area = CreateChartArea();
		if (area != null)
			DrawCandles(area, subscription);
	}

	private void Evaluate()
	{
		if (_ratings.Count < 3)
			return;

		var average = (_ratings["1h"] + _ratings["4h"] + _ratings["1d"]) / 3m;

		if (average > 0m && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (average < 0m && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
