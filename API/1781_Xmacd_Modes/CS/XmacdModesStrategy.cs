using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on different MACD events.
/// </summary>
public class XmacdModesStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<XmacdMode> _mode;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private decimal _prevMacd;
	private decimal _prevMacd2;
	private decimal _prevSignal;
	private decimal _prevSignal2;

	public int FastEmaPeriod { get => _fastEmaPeriod.Value; set => _fastEmaPeriod.Value = value; }
	public int SlowEmaPeriod { get => _slowEmaPeriod.Value; set => _slowEmaPeriod.Value = value; }
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public XmacdMode Mode { get => _mode.Value; set => _mode.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	public XmacdModesStrategy()
	{
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 12)
			.SetDisplay("Fast EMA Period", "Period for fast EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(8, 16, 2);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 26)
			.SetDisplay("Slow EMA Period", "Period for slow EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 32, 2);

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetDisplay("Signal Period", "Period for signal line", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 13, 2);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_mode = Param(nameof(Mode), XmacdMode.MacdDisposition)
			.SetDisplay("Mode", "Trading mode", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss (%)", "Stop loss as percent of entry price", "Risk parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetDisplay("Take Profit (%)", "Take profit as percent of entry price", "Risk parameters")
			.SetCanOptimize(true)
			.SetOptimize(2m, 6m, 1m);

		Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Trade volume", "General")
			.SetGreaterThanZero();
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

		_prevMacd = _prevMacd2 = 0m;
		_prevSignal = _prevSignal2 = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastEmaPeriod },
				LongMa = { Length = SlowEmaPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macd = typed.Macd;
		var signal = typed.Signal;

		var buy = false;
		var sell = false;

		switch (Mode)
		{
			case XmacdMode.Breakdown:
				var crossUp = macd > 0m && _prevMacd <= 0m;
				var crossDown = macd < 0m && _prevMacd >= 0m;
				buy = crossUp;
				sell = crossDown;
				break;

			case XmacdMode.MacdTwist:
				var wasDecreasing = _prevMacd < _prevMacd2;
				var nowIncreasing = macd > _prevMacd;
				var wasIncreasing = _prevMacd > _prevMacd2;
				var nowDecreasing = macd < _prevMacd;
				buy = wasDecreasing && nowIncreasing;
				sell = wasIncreasing && nowDecreasing;
				break;

			case XmacdMode.SignalTwist:
				var sigWasDecreasing = _prevSignal < _prevSignal2;
				var sigNowIncreasing = signal > _prevSignal;
				var sigWasIncreasing = _prevSignal > _prevSignal2;
				var sigNowDecreasing = signal < _prevSignal;
				buy = sigWasDecreasing && sigNowIncreasing;
				sell = sigWasIncreasing && sigNowDecreasing;
				break;

			case XmacdMode.MacdDisposition:
				var crossAbove = macd > signal && _prevMacd <= _prevSignal;
				var crossBelow = macd < signal && _prevMacd >= _prevSignal;
				buy = crossAbove;
				sell = crossBelow;
				break;
		}

		if (buy && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (sell && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevMacd2 = _prevMacd;
		_prevMacd = macd;
		_prevSignal2 = _prevSignal;
		_prevSignal = signal;
	}
}

public enum XmacdMode
{
	Breakdown,
	MacdTwist,
	SignalTwist,
	MacdDisposition
}
