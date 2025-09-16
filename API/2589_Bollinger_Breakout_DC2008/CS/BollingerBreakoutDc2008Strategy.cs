namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public enum AppliedPriceType
{
	Close,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted,
	Average
}

/// <summary>
/// Bollinger breakout strategy inspired by DC2008 implementation.
/// </summary>
public class BollingerBreakoutDc2008Strategy : Strategy
{
	private readonly StrategyParam<int> _bandsPeriod;
	private readonly StrategyParam<decimal> _bandsDeviation;
	private readonly StrategyParam<AppliedPriceType> _appliedPrice;
	private readonly StrategyParam<DataType> _candleType;

	private BollingerBands? _bollinger;

	public int BandsPeriod
	{
		get => _bandsPeriod.Value;
		set => _bandsPeriod.Value = value;
	}

	public decimal BandsDeviation
	{
		get => _bandsDeviation.Value;
		set => _bandsDeviation.Value = value;
	}

	public AppliedPriceType AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BollingerBreakoutDc2008Strategy()
	{
		_bandsPeriod = Param(nameof(BandsPeriod), 80)
			.SetDisplay("Bands Period", "Number of candles for Bollinger Bands", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_bandsDeviation = Param(nameof(BandsDeviation), 3m)
			.SetDisplay("Deviation", "Standard deviation multiplier", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceType.Close)
			.SetDisplay("Applied Price", "Candle price source for Bollinger Bands", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to analyze", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_bollinger = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create Bollinger Bands indicator with the configured parameters.
		_bollinger = new BollingerBands
		{
			Length = BandsPeriod,
			Width = BandsDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _bollinger is null)
			return;

		// Calculate Bollinger Bands for the selected price source.
		var indicatorValue = _bollinger.Process(GetAppliedPrice(candle), candle.OpenTime, true);

		if (!indicatorValue.IsFinal)
			return;

		if (indicatorValue is not BollingerBandsValue bands)
			return;

		if (bands.UpBand is not decimal upper || bands.LowBand is not decimal lower || bands.MovingAverage is not decimal middle)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		// Determine breakout conditions based on Bollinger structure.
		var buySignal = low < lower && high < middle;
		var sellSignal = high > upper && low > middle;

		if (!buySignal && !sellSignal)
			return;

		// Compute unrealized profit to mimic original position filter.
		var unrealizedPnL = Position == 0 ? 0m : Position * (close - PositionPrice);

		if (buySignal)
		{
			if (Position == 0)
			{
				// No position open, start a new long.
				BuyMarket();
			}
			else
			{
				if (unrealizedPnL < 0m)
					return;

				if (Position < 0)
				{
					// Reverse from short to long while preserving target volume.
					BuyMarket(Volume + Math.Abs(Position));
				}
			}

			return;
		}

		if (sellSignal)
		{
			if (Position == 0)
			{
				// No position open, start a new short.
				SellMarket();
			}
			else
			{
				if (unrealizedPnL < 0m)
					return;

				if (Position > 0)
				{
					// Reverse from long to short while preserving target volume.
					SellMarket(Volume + Math.Abs(Position));
				}
			}
		}
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			AppliedPriceType.Close => candle.ClosePrice,
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + (2m * candle.ClosePrice)) / 4m,
			AppliedPriceType.Average => (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}
}
