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
/// RSI and dual EMA based trading bot.
/// Buys when RSI crosses above oversold and fast EMA > slow EMA (price above MA).
/// Sells when RSI crosses below overbought or fast EMA < slow EMA.
/// </summary>
public class UltimateTradingBotStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private decimal _prevFast;
	private decimal _prevSlow;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public UltimateTradingBotStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period of RSI", "General");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "Overbought level", "General");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "Oversold level", "General");

		_fastEmaLength = Param(nameof(FastEmaLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "General");

		_slowEmaLength = Param(nameof(SlowEmaLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0;
		_prevFast = 0;
		_prevSlow = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var emaFast = new ExponentialMovingAverage { Length = FastEmaLength };
		var emaSlow = new ExponentialMovingAverage { Length = SlowEmaLength };

		_prevRsi = 0;
		_prevFast = 0;
		_prevSlow = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, emaFast, emaSlow, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawIndicator(area, emaFast);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevRsi == 0)
		{
			_prevRsi = rsi;
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var rsiCrossUp = _prevRsi <= RsiOversold && rsi > RsiOversold;
		var rsiCrossDown = _prevRsi >= RsiOverbought && rsi < RsiOverbought;
		var emaBullish = fast > slow;
		var emaCrossDown = _prevFast >= _prevSlow && fast < slow;

		// Long entry: RSI crosses above oversold while EMA trend is bullish
		if (rsiCrossUp && emaBullish && Position <= 0)
			BuyMarket();
		// Exit long: RSI crosses below overbought or EMA bearish cross
		else if ((rsiCrossDown || emaCrossDown) && Position > 0)
			SellMarket();

		_prevRsi = rsi;
		_prevFast = fast;
		_prevSlow = slow;
	}
}
