using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD crossover strategy that trades when MACD line crosses its signal.
/// </summary>
public class MacdVsSignalStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private decimal? _prevMacd;
	private decimal? _prevSignal;

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
	/// Stop loss size in points.
	/// </summary>
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take profit size in points.
	/// </summary>
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public int TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MacdVsSignalStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetDisplay("Fast Period", "Short EMA period for MACD", "MACD");
		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetDisplay("Slow Period", "Long EMA period for MACD", "MACD");
		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetDisplay("Signal Period", "Signal line period for MACD", "MACD");
		_stopLoss = Param(nameof(StopLoss), 50)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 999)
			.SetDisplay("Take Profit", "Take profit in points", "Risk");
		_trailingStop = Param(nameof(TrailingStop), 0)
			.SetDisplay("Trailing Stop", "Trailing stop in points", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
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

		_prevMacd = null;
		_prevSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		Unit? tp = TakeProfit > 0 ? new Unit(TakeProfit, UnitTypes.Point) : null;
		Unit? sl = StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Point) :
			TrailingStop > 0 ? new Unit(TrailingStop, UnitTypes.Point) : null;

		if (tp != null || sl != null)
			StartProtection(tp, sl, isStopTrailing: TrailingStop > 0);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (typed.Macd is not decimal macdLine || typed.Signal is not decimal signalLine)
			return;

		if (_prevMacd is decimal prevMacd && _prevSignal is decimal prevSignal)
		{
			var crossUp = prevMacd < prevSignal && macdLine > signalLine;
			var crossDown = prevMacd > prevSignal && macdLine < signalLine;

			var volume = Volume + Math.Abs(Position);

			if (crossUp && Position <= 0)
				BuyMarket(volume);
			else if (crossDown && Position >= 0)
				SellMarket(volume);
		}

		_prevMacd = macdLine;
		_prevSignal = signalLine;
	}
}
