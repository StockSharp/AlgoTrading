using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Crypto MVRV ZScore Strategy (642).
/// Converts the MVRV Z-Score concept into a trading system.
/// </summary>
public class CryptoMvrvZScoreStrategy : Strategy
{
	private readonly StrategyParam<int> _zScorePeriod;
	private readonly StrategyParam<decimal> _longEntryThreshold;
	private readonly StrategyParam<decimal> _shortEntryThreshold;
	private readonly StrategyParam<Sides?> _direction;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _realMarketCap;
	private StandardDeviation _marketCapStdDev;
	private SimpleMovingAverage _spreadAverage;
	private StandardDeviation _spreadStdDev;
	private SimpleMovingAverage _spreadZScoreAverage;

	private decimal _prevSpreadZScore;

	/// <summary>
	/// Period for z-score calculation.
	/// </summary>
	public int ZScoreCalculationPeriod
	{
		get => _zScorePeriod.Value;
		set => _zScorePeriod.Value = value;
	}

	/// <summary>
	/// Long entry threshold.
	/// </summary>
	public decimal LongEntryThreshold
	{
		get => _longEntryThreshold.Value;
		set => _longEntryThreshold.Value = value;
	}

	/// <summary>
	/// Short entry threshold.
	/// </summary>
	public decimal ShortEntryThreshold
	{
		get => _shortEntryThreshold.Value;
		set => _shortEntryThreshold.Value = value;
	}

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public Sides? Direction
       {
               get => _direction.Value;
               set => _direction.Value = value;
       }

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CryptoMvrvZScoreStrategy"/>.
	/// </summary>
	public CryptoMvrvZScoreStrategy()
	{
		_zScorePeriod = Param(nameof(ZScoreCalculationPeriod), 252)
			.SetGreaterThanZero()
			.SetDisplay("Z-Score Period", "Period for z-score calculation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(100, 300, 50);

		_longEntryThreshold = Param(nameof(LongEntryThreshold), 0.382m)
			.SetDisplay("Long Entry Threshold", "Threshold for long entries", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.2m, 0.5m, 0.05m);

		_shortEntryThreshold = Param(nameof(ShortEntryThreshold), -0.382m)
			.SetDisplay("Short Entry Threshold", "Threshold for short entries", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(-0.5m, -0.2m, 0.05m);

               _direction = Param(nameof(Direction), (Sides?)null)
                       .SetDisplay("Trade Direction", "Allowed trade direction", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Parameters");
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

		_prevSpreadZScore = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_realMarketCap = new SimpleMovingAverage { Length = ZScoreCalculationPeriod };
		_marketCapStdDev = new StandardDeviation { Length = ZScoreCalculationPeriod };
		_spreadAverage = new SimpleMovingAverage { Length = ZScoreCalculationPeriod };
		_spreadStdDev = new StandardDeviation { Length = ZScoreCalculationPeriod };
		_spreadZScoreAverage = new SimpleMovingAverage { Length = 2 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_realMarketCap, _marketCapStdDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _spreadZScoreAverage, "Spread Z-Score");
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal realCap, decimal capStdDev)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_realMarketCap.IsFormed || !_marketCapStdDev.IsFormed || capStdDev == 0)
			return;

		var price = candle.ClosePrice;
		var mvrvRatio = price / realCap;
		var marketZscore = (price - realCap) / capStdDev;
		var spread = marketZscore - mvrvRatio;

		var spreadAvgVal = _spreadAverage.Process(spread, candle.ServerTime, true);
		var spreadStdVal = _spreadStdDev.Process(spread, candle.ServerTime, true);

		var spreadAvg = spreadAvgVal.ToDecimal();
		var spreadStd = spreadStdVal.ToDecimal();

		if (!_spreadAverage.IsFormed || !_spreadStdDev.IsFormed || spreadStd == 0)
			return;

		var spreadZScoreRaw = (spread - spreadAvg) / spreadStd;
		var spreadZScoreVal = _spreadZScoreAverage.Process(spreadZScoreRaw, candle.ServerTime, true);
		var spreadZScore = spreadZScoreVal.ToDecimal();

		if (!_spreadZScoreAverage.IsFormed)
		{
			_prevSpreadZScore = spreadZScore;
			return;
		}

		var longEntry = _prevSpreadZScore <= LongEntryThreshold && spreadZScore > LongEntryThreshold;
		var longExit = _prevSpreadZScore >= ShortEntryThreshold && spreadZScore < ShortEntryThreshold;
		var shortEntry = longExit;
		var shortExit = longEntry;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		if (longEntry && (Direction is null or Sides.Buy) && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (longExit && (Direction is null or Sides.Buy) && Position > 0)
		{
			SellMarket(Math.Abs(Position));
		}
		if (shortEntry && (Direction is null or Sides.Sell) && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (shortExit && (Direction is null or Sides.Sell) && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		_prevSpreadZScore = spreadZScore;
	}
}
