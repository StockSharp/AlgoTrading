using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on MACD line/signal crossovers.
/// </summary>
public class XmacdModesStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private decimal _prevMacd;
	private decimal _prevSignal;

	public int FastEmaPeriod { get => _fastEmaPeriod.Value; set => _fastEmaPeriod.Value = value; }
	public int SlowEmaPeriod { get => _slowEmaPeriod.Value; set => _slowEmaPeriod.Value = value; }
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	public XmacdModesStrategy()
	{
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 12)
			.SetDisplay("Fast EMA Period", "Period for fast EMA", "Indicators");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 26)
			.SetDisplay("Slow EMA Period", "Period for slow EMA", "Indicators");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetDisplay("Signal Period", "Period for signal line", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss (%)", "Stop loss as percent of entry price", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetDisplay("Take Profit (%)", "Take profit as percent of entry price", "Risk");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMacd = 0;
		_prevSignal = 0;

		var macd = new MovingAverageConvergenceDivergenceSignal();
		macd.Macd.ShortMa.Length = FastEmaPeriod;
		macd.Macd.LongMa.Length = SlowEmaPeriod;
		macd.SignalMa.Length = SignalPeriod;

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(macd, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (typed.Macd is not decimal macdLine || typed.Signal is not decimal signalLine)
			return;

		// MACD/Signal crossover mode
		if (_prevMacd != 0 || _prevSignal != 0)
		{
			var crossAbove = macdLine > signalLine && _prevMacd <= _prevSignal;
			var crossBelow = macdLine < signalLine && _prevMacd >= _prevSignal;

			if (crossAbove && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();
				BuyMarket();
			}
			else if (crossBelow && Position >= 0)
			{
				if (Position > 0)
					SellMarket();
				SellMarket();
			}
		}

		_prevMacd = macdLine;
		_prevSignal = signalLine;
	}
}
