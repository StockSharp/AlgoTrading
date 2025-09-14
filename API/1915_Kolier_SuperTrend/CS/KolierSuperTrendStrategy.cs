using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Kolier SuperTrend strategy based on ATR bands.
/// The strategy enters long when price crosses above the SuperTrend line and enters short when crossing below.
/// </summary>
public class KolierSuperTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	// Track previous state of price relative to SuperTrend line
	private bool _prevPriceAbove;
	private decimal _prevSupertrend;

	/// <summary>
	/// ATR period used in SuperTrend calculation.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR for band width.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Candle type used for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters for Kolier SuperTrend strategy.
	/// </summary>
	public KolierSuperTrendStrategy()
	{
		_period = Param(nameof(Period), 10)
			.SetDisplay("ATR Period", "ATR period for SuperTrend", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_multiplier = Param(nameof(Multiplier), 3.0m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for SuperTrend", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2.0m, 4.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_prevPriceAbove = false;
		_prevSupertrend = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var upper = median + Multiplier * atrValue;
		var lower = median - Multiplier * atrValue;

		decimal supertrend;

		if (_prevSupertrend == 0m)
		{
			supertrend = candle.ClosePrice > median ? lower : upper;
			_prevSupertrend = supertrend;
			_prevPriceAbove = candle.ClosePrice > supertrend;
			return;
		}

		if (_prevSupertrend <= candle.HighPrice)
			supertrend = Math.Max(lower, _prevSupertrend);
		else if (_prevSupertrend >= candle.LowPrice)
			supertrend = Math.Min(upper, _prevSupertrend);
		else
			supertrend = candle.ClosePrice > _prevSupertrend ? lower : upper;

		var priceAbove = candle.ClosePrice > supertrend;
		var crossUp = priceAbove && !_prevPriceAbove;
		var crossDown = !priceAbove && _prevPriceAbove;

		if (crossUp && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (crossDown && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		_prevSupertrend = supertrend;
		_prevPriceAbove = priceAbove;
	}
}
