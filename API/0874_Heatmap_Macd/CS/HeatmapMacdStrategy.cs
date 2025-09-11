using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe MACD heatmap strategy.
/// Enters when all monitored MACD histograms cross above/below zero.
/// </summary>
public class HeatmapMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _candleType1;
	private readonly StrategyParam<DataType> _candleType2;
	private readonly StrategyParam<DataType> _candleType3;
	private readonly StrategyParam<DataType> _candleType4;
	private readonly StrategyParam<DataType> _candleType5;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private bool _hist1;
	private bool _hist2;
	private bool _hist3;
	private bool _hist4;
	private bool _hist5;
	private bool _prevAllBull;
	private bool _prevAllBear;

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }

	/// <summary>
	/// Primary candle type.
	/// </summary>
	public DataType CandleType1 { get => _candleType1.Value; set => _candleType1.Value = value; }

	/// <summary>
	/// Second candle type.
	/// </summary>
	public DataType CandleType2 { get => _candleType2.Value; set => _candleType2.Value = value; }

	/// <summary>
	/// Third candle type.
	/// </summary>
	public DataType CandleType3 { get => _candleType3.Value; set => _candleType3.Value = value; }

	/// <summary>
	/// Fourth candle type.
	/// </summary>
	public DataType CandleType4 { get => _candleType4.Value; set => _candleType4.Value = value; }

	/// <summary>
	/// Fifth candle type.
	/// </summary>
	public DataType CandleType5 { get => _candleType5.Value; set => _candleType5.Value = value; }

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public HeatmapMacdStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 20)
		.SetDisplay("Fast Period", "Fast EMA period for MACD", "MACD")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 2);

		_slowPeriod = Param(nameof(SlowPeriod), 50)
		.SetDisplay("Slow Period", "Slow EMA period for MACD", "MACD")
		.SetCanOptimize(true)
		.SetOptimize(40, 80, 2);

		_signalPeriod = Param(nameof(SignalPeriod), 50)
		.SetDisplay("Signal Period", "Signal line period for MACD", "MACD")
		.SetCanOptimize(true)
		.SetOptimize(40, 80, 2);

		_candleType1 = Param(nameof(CandleType1), TimeSpan.FromMinutes(60).TimeFrame())
		.SetDisplay("Timeframe 1", "First timeframe", "Timeframes");
		_candleType2 = Param(nameof(CandleType2), TimeSpan.FromMinutes(120).TimeFrame())
		.SetDisplay("Timeframe 2", "Second timeframe", "Timeframes");
		_candleType3 = Param(nameof(CandleType3), TimeSpan.FromMinutes(240).TimeFrame())
		.SetDisplay("Timeframe 3", "Third timeframe", "Timeframes");
		_candleType4 = Param(nameof(CandleType4), TimeSpan.FromMinutes(240).TimeFrame())
		.SetDisplay("Timeframe 4", "Fourth timeframe", "Timeframes");
		_candleType5 = Param(nameof(CandleType5), TimeSpan.FromMinutes(480).TimeFrame())
		.SetDisplay("Timeframe 5", "Fifth timeframe", "Timeframes");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
		.SetDisplay("Take Profit %", "Take profit percentage", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
		.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.5m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType1), (Security, CandleType2), (Security, CandleType3), (Security, CandleType4), (Security, CandleType5)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_hist1 = _hist2 = _hist3 = _hist4 = _hist5 = false;
		_prevAllBull = _prevAllBear = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd1 = CreateMacd();
		var macd2 = CreateMacd();
		var macd3 = CreateMacd();
		var macd4 = CreateMacd();
		var macd5 = CreateMacd();

		SubscribeCandles(CandleType1).BindEx(macd1, Process1).Start();
		SubscribeCandles(CandleType2).BindEx(macd2, Process2).Start();
		SubscribeCandles(CandleType3).BindEx(macd3, Process3).Start();
		SubscribeCandles(CandleType4).BindEx(macd4, Process4).Start();
		SubscribeCandles(CandleType5).BindEx(macd5, Process5).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			var sub = SubscribeCandles(CandleType1);
			DrawCandles(area, sub);
			DrawIndicator(area, macd1);
			DrawOwnTrades(area);
		}

		StartProtection(
		new Unit(TakeProfitPercent, UnitTypes.Percent),
		new Unit(StopLossPercent, UnitTypes.Percent)
		);
	}

	private MovingAverageConvergenceDivergenceSignal CreateMacd()
	{
		return new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};
	}

	private void Process1(ICandleMessage candle, IIndicatorValue value) => Process(1, candle, value);
	private void Process2(ICandleMessage candle, IIndicatorValue value) => Process(2, candle, value);
	private void Process3(ICandleMessage candle, IIndicatorValue value) => Process(3, candle, value);
	private void Process4(ICandleMessage candle, IIndicatorValue value) => Process(4, candle, value);
	private void Process5(ICandleMessage candle, IIndicatorValue value) => Process(5, candle, value);

	private void Process(int idx, ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var macd = (MovingAverageConvergenceDivergenceSignalValue)value;
		if (macd.Macd is not decimal macdValue || macd.Signal is not decimal signalValue)
		return;

		var hist = macdValue - signalValue;

		switch (idx)
		{
			case 1:
			_hist1 = hist > 0;
			break;
			case 2:
			_hist2 = hist > 0;
			break;
			case 3:
			_hist3 = hist > 0;
			break;
			case 4:
			_hist4 = hist > 0;
			break;
			case 5:
			_hist5 = hist > 0;
			break;
		}

		CheckSignals();
	}

	private void CheckSignals()
	{
		var allBull = _hist1 && _hist2 && _hist3 && _hist4 && _hist5;
		var allBear = !_hist1 && !_hist2 && !_hist3 && !_hist4 && !_hist5;

		if (allBull && !_prevAllBull && Position <= 0)
		BuyMarket(Volume);
		else if (allBear && !_prevAllBear && Position >= 0)
		SellMarket(Volume);
		else if (Position > 0 && !allBull)
		SellMarket(Position);
		else if (Position < 0 && !allBear)
		BuyMarket(Math.Abs(Position));

		_prevAllBull = allBull;
		_prevAllBear = allBear;
	}
}
