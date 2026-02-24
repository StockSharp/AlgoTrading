using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams AO AC strategy: Awesome Oscillator zero-cross with RSI filter.
/// Buys when AO crosses above zero with RSI below 60.
/// Sells when AO crosses below zero with RSI above 40.
/// </summary>
public class WilliamsAoAcStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;

	private decimal? _prevAo;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public WilliamsAoAcStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI filter period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAo = null;

		var ao = new AwesomeOscillator();
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ao, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal aoValue, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevAo.HasValue)
		{
			// AO crosses above zero
			if (_prevAo.Value <= 0 && aoValue > 0 && rsi < 60m && Position <= 0)
			{
				BuyMarket();
			}
			// AO crosses below zero
			else if (_prevAo.Value >= 0 && aoValue < 0 && rsi > 40m && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevAo = aoValue;
	}
}
