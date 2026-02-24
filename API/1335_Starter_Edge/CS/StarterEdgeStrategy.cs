using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Starter Edge strategy based on MACD crossover with EMA filter.
/// Uses MACD line crossing signal line for entries, with EMA trend filter.
/// </summary>
public class StarterEdgeStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevMacd;
	private decimal? _prevSignal;

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public StarterEdgeStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetDisplay("MACD Fast", "MACD Fast EMA Length", "General");

		_slowLength = Param(nameof(SlowLength), 26)
			.SetDisplay("MACD Slow", "MACD Slow EMA Length", "General");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetDisplay("Signal Length", "Signal SMA Length", "General");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetDisplay("EMA Length", "EMA trend filter length", "General");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetDisplay("Take Profit %", "Take Profit %", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetDisplay("Stop Loss %", "Stop Loss %", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle Type", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMacd = null;
		_prevSignal = null;

		var macdHist = new MovingAverageConvergenceDivergenceHistogram
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macdHist, ema, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceHistogramValue)macdValue;
		var macd = macdTyped.Macd;
		var signal = macdTyped.Signal;
		var emaVal = emaValue.IsFormed ? emaValue.GetValue<decimal>() : (decimal?)null;

		if (macd == null || signal == null || emaVal == null)
			return;

		var close = candle.ClosePrice;

		if (_prevMacd != null && _prevSignal != null)
		{
			// MACD cross up + price above EMA => buy
			if (_prevMacd <= _prevSignal && macd > signal && close > emaVal && Position <= 0)
			{
				BuyMarket();
			}
			// MACD cross down + price below EMA => sell
			else if (_prevMacd >= _prevSignal && macd < signal && close < emaVal && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevMacd = macd;
		_prevSignal = signal;
	}
}
