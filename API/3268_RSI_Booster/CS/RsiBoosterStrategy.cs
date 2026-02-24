using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI Booster strategy: dual RSI divergence.
/// Uses a fast RSI and a slow RSI. Buys when fast RSI crosses above slow RSI from oversold.
/// Sells when fast RSI crosses below slow RSI from overbought.
/// </summary>
public class RsiBoosterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastRsiPeriod;
	private readonly StrategyParam<int> _slowRsiPeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;

	private decimal? _prevFastRsi;
	private decimal? _prevSlowRsi;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastRsiPeriod
	{
		get => _fastRsiPeriod.Value;
		set => _fastRsiPeriod.Value = value;
	}

	public int SlowRsiPeriod
	{
		get => _slowRsiPeriod.Value;
		set => _slowRsiPeriod.Value = value;
	}

	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	public RsiBoosterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_fastRsiPeriod = Param(nameof(FastRsiPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("Fast RSI", "Fast RSI period", "Indicators");

		_slowRsiPeriod = Param(nameof(SlowRsiPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow RSI", "Slow RSI period", "Indicators");

		_oversoldLevel = Param(nameof(OversoldLevel), 40m)
			.SetDisplay("Oversold", "Oversold RSI level", "Levels");

		_overboughtLevel = Param(nameof(OverboughtLevel), 60m)
			.SetDisplay("Overbought", "Overbought RSI level", "Levels");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFastRsi = null;
		_prevSlowRsi = null;

		var fastRsi = new RelativeStrengthIndex { Length = FastRsiPeriod };
		var slowRsi = new RelativeStrengthIndex { Length = SlowRsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastRsi, slowRsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastRsi, decimal slowRsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevFastRsi.HasValue && _prevSlowRsi.HasValue)
		{
			// Buy: fast RSI crosses above slow RSI while slow RSI is below oversold
			if (_prevFastRsi.Value <= _prevSlowRsi.Value && fastRsi > slowRsi && slowRsi < OversoldLevel && Position <= 0)
			{
				BuyMarket();
			}
			// Sell: fast RSI crosses below slow RSI while slow RSI is above overbought
			else if (_prevFastRsi.Value >= _prevSlowRsi.Value && fastRsi < slowRsi && slowRsi > OverboughtLevel && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevFastRsi = fastRsi;
		_prevSlowRsi = slowRsi;
	}
}
