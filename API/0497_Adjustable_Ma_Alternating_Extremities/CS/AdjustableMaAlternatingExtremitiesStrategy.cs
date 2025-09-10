using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adjustable MA & Alternating Extremities Strategy.
/// Buys when price crosses above the upper band and sells when it crosses below the lower band.
/// Bands are calculated using Bollinger Bands.
/// </summary>
public class AdjustableMaAlternatingExtremitiesStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _isUpper;

	/// <summary>
	/// Period length for Bollinger Bands.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Width multiplier for Bollinger Bands.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// The type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public AdjustableMaAlternatingExtremitiesStrategy()
	{
		_length = Param(nameof(Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Periods for Bollinger Bands", "General")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Bollinger band width", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_isUpper = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bands = new BollingerBands
		{
			Length = Length,
			Width = Multiplier
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(bands, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bands);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.High > upper && _isUpper != true)
		{
			_isUpper = true;

			if (Position <= 0)
				BuyMarket();
		}
		else if (candle.Low < lower && _isUpper != false)
		{
			_isUpper = false;

			if (Position >= 0)
				SellMarket();
		}
	}
}
