using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// GO-based trading strategy.
/// Calculates a composite GO value from EMA of OHLC prices and volume.
/// Opens long when GO rises above OpenLevel, and short when below -OpenLevel.
/// Positions are closed when GO crosses back inside closing levels.
/// </summary>
public class GoStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _openLevel;
	private readonly StrategyParam<decimal> _closeLevelDiff;
	private readonly StrategyParam<bool> _showGo;
	private readonly StrategyParam<DataType> _candleType;

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public decimal OpenLevel { get => _openLevel.Value; set => _openLevel.Value = value; }
	public decimal CloseLevelDiff { get => _closeLevelDiff.Value; set => _closeLevelDiff.Value = value; }
	public bool ShowGo { get => _showGo.Value; set => _showGo.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public GoStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 174)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Length of EMA for price smoothing", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 50);

		_openLevel = Param(nameof(OpenLevel), 0m)
			.SetDisplay("Open Level", "GO level to open positions", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(-10m, 10m, 1m);

		_closeLevelDiff = Param(nameof(CloseLevelDiff), 0m)
			.SetDisplay("Close Level Diff", "Difference between open and close levels", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0m, 5m, 0.5m);

		_showGo = Param(nameof(ShowGo), true)
			.SetDisplay("Show GO", "Log GO values", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var highMa = new EMA { Length = MaPeriod };
		var lowMa = new EMA { Length = MaPeriod };
		var openMa = new EMA { Length = MaPeriod };
		var closeMa = new EMA { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(closeMa, (candle, closeVal) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			// Update EMAs for other price parts
			var highVal = highMa.Process(candle.OpenTime, candle.HighPrice);
			var lowVal = lowMa.Process(candle.OpenTime, candle.LowPrice);
			var openVal = openMa.Process(candle.OpenTime, candle.OpenPrice);

			if (!highVal.IsFinal || !lowVal.IsFinal || !openVal.IsFinal)
				return;

			if (highVal.Value is not decimal high ||
				lowVal.Value is not decimal low ||
				openVal.Value is not decimal open)
				return;

			var go = ((closeVal - open) + (high - open) + (low - open) + (closeVal - low) + (closeVal - high)) * candle.TotalVolume;

			if (ShowGo)
				LogInfo($"GO={go}");

			var closeBuy = go < (OpenLevel - CloseLevelDiff);
			var closeSell = go > -(OpenLevel - CloseLevelDiff);
			var openBuy = go > OpenLevel;
			var openSell = go < -OpenLevel;

			if (Position > 0 && closeBuy)
			{
				SellMarket(Position);
			}
			else if (Position < 0 && closeSell)
			{
				BuyMarket(-Position);
			}
			else if (Position == 0)
			{
				if (openBuy && !openSell)
				BuyMarket(Volume);
				else if (openSell && !openBuy)
				SellMarket(Volume);
			}
		}).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, closeMa);
			DrawOwnTrades(area);
		}
	}
}
