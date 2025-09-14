namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy placing limit orders on Bollinger Bands.
/// Buys at the lower band and sells at the upper band.
/// Open positions are closed when price crosses the middle band.
/// </summary>
public class BollingerBandsAutomatedStrategy : Strategy
{
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbDeviation;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BbPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation.
	/// </summary>
	public decimal BbDeviation
	{
		get => _bbDeviation.Value;
		set => _bbDeviation.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public BollingerBandsAutomatedStrategy()
	{
		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_bbDeviation = Param(nameof(BbDeviation), 2m)
			.SetDisplay("BB Deviation", "Bollinger Bands deviation", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bb = new BollingerBands
		{
			Length = BbPeriod,
			Width = BbDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bb, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		// Close existing position when price reaches the middle band
		if (Position > 0 && close >= middle)
			ClosePosition();
		else if (Position < 0 && close <= middle)
			ClosePosition();

		// Refresh pending orders every new candle
		CancelActiveOrders();

		// Place limit orders at Bollinger Band extremes
		BuyLimit(lower);
		SellLimit(upper);
	}
}
