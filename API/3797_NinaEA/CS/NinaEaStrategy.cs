using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SuperTrend-based strategy from NinaEA.
/// Trades on SuperTrend direction flips - buy on up trend, sell on down trend.
/// </summary>
public class NinaEaStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _previousTrendUp;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public NinaEaStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetDisplay("ATR Period", "ATR length for SuperTrend", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1m)
			.SetDisplay("ATR Multiplier", "SuperTrend multiplier", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousTrendUp = null;

		var superTrend = new SuperTrend
		{
			Length = AtrPeriod,
			Multiplier = AtrMultiplier
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(superTrend, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue superTrendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!superTrendValue.IsFinal)
			return;

		var stValue = (SuperTrendIndicatorValue)superTrendValue;
		var isUpTrend = stValue.IsUpTrend;

		if (_previousTrendUp is bool prevUp)
		{
			// Trend flipped to up - go long
			if (isUpTrend && !prevUp)
			{
				if (Position < 0)
					BuyMarket();
				if (Position <= 0)
					BuyMarket();
			}
			// Trend flipped to down - go short
			else if (!isUpTrend && prevUp)
			{
				if (Position > 0)
					SellMarket();
				if (Position >= 0)
					SellMarket();
			}
		}

		_previousTrendUp = isUpTrend;
	}
}
