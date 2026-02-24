using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Tri-Monthly BTC Swing strategy.
/// Goes long when price is above slow EMA, fast EMA above slow EMA and RSI above threshold.
/// Limits trade frequency via a minimum time interval.
/// </summary>
public class TriMonthlyBtcSwingStrategy : Strategy
{
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiThreshold;
	private readonly StrategyParam<int> _tradeIntervalBars;
	private readonly StrategyParam<DataType> _candleType;

	private int _barsSinceLastTrade;

	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiThreshold { get => _rsiThreshold.Value; set => _rsiThreshold.Value = value; }
	public int TradeIntervalBars { get => _tradeIntervalBars.Value; set => _tradeIntervalBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TriMonthlyBtcSwingStrategy()
	{
		_slowEmaLength = Param(nameof(SlowEmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "General");

		_fastEmaLength = Param(nameof(FastEmaLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "General");

		_rsiThreshold = Param(nameof(RsiThreshold), 50m)
			.SetDisplay("RSI Threshold", "RSI level for entry", "General");

		_tradeIntervalBars = Param(nameof(TradeIntervalBars), 20)
			.SetGreaterThanZero()
			.SetDisplay("Trade Interval Bars", "Min bars between entries", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_barsSinceLastTrade = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(slowEma, fastEma, rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, slowEma);
			DrawIndicator(area, fastEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal slowEma, decimal fastEma, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceLastTrade++;

		var canTrade = _barsSinceLastTrade >= TradeIntervalBars;
		var longCondition = candle.ClosePrice > slowEma && fastEma > slowEma && rsiVal > RsiThreshold && canTrade;
		var exitCondition = fastEma < slowEma || rsiVal < RsiThreshold;

		if (longCondition && Position <= 0)
		{
			BuyMarket();
			_barsSinceLastTrade = 0;
		}
		else if (exitCondition && Position > 0)
		{
			SellMarket();
		}
	}
}
